using SocketIOClient;
using SocketIOClient.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace zepp_os_push_to_simulator_cs
{
    public partial class MainWindow : Form
    {
        private SocketIO socket;
        private const int previewId = 1;
        private const string previewMethod = "ide.simulator.preview";
        private int appId;
        private string projectName;
        private int primarySource;
        private byte[] zpkBuffer;
        private List<int> devSources;

        public void SimulatorInit(string url)
        {
            if (socket != null) return;

            // Setup SocketIO options based on the simulator's requirements
            var options = new SocketIOOptions
            {
                EIO = EngineIO.V4,
                Transport = TransportProtocol.WebSocket,
            };

            socket = new SocketIO(new Uri(url), options);

            // Socket on message listener
            socket.On("message", ctx =>
            {
                Console.WriteLine($"[DEBUG] Simulator reutrns at socket on: {ctx.RawText}");
                return Task.CompletedTask;
            });

            // Socket on connect listener
            socket.OnConnected += (sender, e) =>
            {
                this.BeginInvoke((MethodInvoker)delegate {
                    MessageBox.Show("Connected to Simulator!", "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    send_Btn.Text = "Send";
                    addressTextbox.Enabled = false;
                });
            };

            // Connect to the simulator asynchronously
            Task.Run(async () => {
                try
                {
                    await socket.ConnectAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Connect Error: {ex.Message}");
                }
            });
        }

        public async Task Upload(byte[] data, string projectName, string target, int appid, List<int> devices)
        {
            var dataArr = Array.ConvertAll(data, b => (int)b);

            // 1. Make a payload object that matches the simulator's expected structure
            var payload = new
            {
                jsonrpc = "2.0",
                method = previewMethod,
                @params = new
                {
                    target = target,
                    projectName = projectName,
                    appid = appid,
                    size = data.Length,
                    data = dataArr,
                    devices = devices
                },
                id = previewId
            };


            // 2. Serialize the payload to a JSON string using System.Text.Json
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = null // Keep naming policy as the node.js version
            };
            string jsonMessage = JsonSerializer.Serialize(payload, options);

            Console.WriteLine($"[DEBUG] Send 'message' event!");

            // 4. Send the JSON string to the simulator use same method like the Node via Socket.IO
            // Uses new[] { jsonString } to matching IEnumerable<object> for EmitAsync
            await socket.EmitAsync("message", new[] { jsonMessage });
        }

        /// <summary>
        /// Read and parse the package's app.json file to extract appName, appId, and deviceSource from platforms.
        /// </summary>
        public (string AppName, int AppId, List<int> DeviceSource) ParseSimulatorConfig(string jsonPath)
        {
            try
            {
                // 1. Read and trims the UTF-8 BOM (3 extra bytes at the start of the file) if it exists
                string jsonString = File.ReadAllText(jsonPath).TrimStart('\uFEFF');

                List<int> devSources = new List<int>();

                // 2. Parse the JSON string using System.Text.Json
                using (JsonDocument doc = JsonDocument.Parse(jsonString))
                {
                    JsonElement root = doc.RootElement;

                    // 3. Extract appName (app -> appName)
                    string appName = root.GetProperty("app").GetProperty("appName").GetString();

                    // 4. Extract appId (app -> appId)
                    int appId = root.GetProperty("app").GetProperty("appId").GetInt32();

                    // 5. Extract the deviceSource under platforms
                    if (root.TryGetProperty("platforms", out JsonElement platforms) && platforms.GetArrayLength() > 0)
                    {
                        devSources = platforms.EnumerateArray()
                            .Select(p => p.GetProperty("deviceSource").GetInt32())
                            .ToList();
                    }

                    return (appName, appId, devSources);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Parse JSON failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Read the ZPK file as a byte array to be sent to the simulator.
        /// </summary>
        public byte[] ReadZpkFile(string zpkPath)
        {
            string fullPath = Path.GetFullPath(zpkPath);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"ZPK file not found: {fullPath}");

            return File.ReadAllBytes(fullPath);
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void pick_zpk_Btn_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Zepp OS Package (*.zpk *.zip)|*.zpk; *.zip";
            if(openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                zpk_loc_Label.Text = $"Package: \n{openFileDialog1.FileName}";
                zpkBuffer = ReadZpkFile(openFileDialog1.FileName);
            }
        }

        private void pick_json_Btn_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "app.json|app.json";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                json_loc_Label.Text = $"JSON: \n{openFileDialog1.FileName}";

                // 1. Read and parse the app.json to get appId, projectName, and deviceSource
                var parseResult = ParseSimulatorConfig(openFileDialog1.FileName);
                appId = parseResult.AppId;
                projectName = parseResult.AppName;
                content_Label.Text = $"Content: \nApp ID: {appId}\nProject Name: {projectName}\nPrimary Device Source: ";
                string devSourcesStr = "";
                foreach (var item in parseResult.DeviceSource)
                {
                    devSourcesStr += item.ToString();
                    devSourcesStr += ", ";
                }
                content_Label.Text += devSourcesStr;
            }

        }

        private void send_Btn_Click(object sender, EventArgs e)
        {
            // If the socket is not initialized, initialize it with the provided address.
            // If it's still connecting, show a message and return.
            if (socket == null)
            {
                string sim_addr = addressTextbox.Text.Trim();
                SimulatorInit(sim_addr);
                return;
            }
            else if (!socket.Connected)
            {
                MessageBox.Show("Still connecting... Please wait.", "Message", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Check if the ZPK buffer and appId are available before sending
            if (zpkBuffer == null || appId == 0)
            {
                MessageBox.Show("Please select ZPK and app.json first!", "Message", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Send the data to the simulator using the Upload method
            _ = Upload(zpkBuffer, projectName, "watch", appId, devSources);
            MessageBox.Show("Push Sent!", "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
