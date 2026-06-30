using SocketIOClient;
using SocketIOClient.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace zepp_os_push_to_simulator_cs
{
    public partial class MainWindow : Form
    {
        public class DeviceInfo
        {
            public string Name { get; set; }
            public List<int> Sources { get; set; }
        }

        public class SimulatorResponse
        {
            public string jsonrpc { get; set; }
            public int id { get; set; }
            public SimulatorResult result { get; set; }
        }

        public class SimulatorResult
        {
            public bool success { get; set; }
            public string message { get; set; }
        }

        private SocketIO socket;
        private const int previewId = 1;
        private const string previewMethod = "ide.simulator.preview";
        private int appId;
        private string projectName;
        private byte[] zpkBuffer;
        private List<int> devSources;
        private List<DeviceInfo> deviceInfo;

        private static readonly HttpClient httpClient = new HttpClient();

        /// <summary>
        /// Token generator
        /// </summary>
        /// <param name="length">Token length</param>
        /// <returns>Token string</returns>
        private string GenerateRandomToken(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            Random random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        /// <summary>
        /// Download file to the location
        /// </summary>
        /// <param name="url">Download URL</param>
        /// <param name="destinationPath">Save path</param>
        /// <param name="fallbackFilename">Filename to use if can't determine from headers or URL</param>
        /// <param name="withHeader">Whether to include headers in the request</param>
        private async Task<string> DownloadFileAsync(string url, string destinationFolder, string fallbackFilename, bool withHeader)
        {
            string token = "";

            if (withHeader)
            {
                // Read token from the UI, if it's "random" then generate a random token and update the UI with it
                this.Invoke((MethodInvoker)delegate
                {
                    if (tokenTextBox.Text == "random")
                    {
                        token = GenerateRandomToken(255);
                        tokenTextBox.Text = token;
                    }
                    else
                    {
                        token = tokenTextBox.Text.Trim();
                    }
                });
            }

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                if (withHeader)
                {
                    request.Headers.Add("Accept-Encoding", "gzip");
                    request.Headers.Add("apptoken", token);
                    request.Headers.Add("Connection", "Keep-Alive");
                    request.Headers.TryAddWithoutValidation("User-Agent", "Dart/3.1 (dart:io)");
                    request.Headers.Accept.Clear();
                }

                using (var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    // Ready to read the filename from the response headers or URL
                    string fileName = null;

                    // 1. Get filename from Content-Disposition header if available
                    var contentDisposition = response.Content.Headers.ContentDisposition;
                    if (contentDisposition != null && !string.IsNullOrEmpty(contentDisposition.FileName))
                    {
                        fileName = contentDisposition.FileName.Trim('\"');
                    }

                    // 2. If filename is not in headers, try to extract it from the URL
                    if (string.IsNullOrEmpty(fileName))
                    {
                        try
                        {
                            fileName = Path.GetFileName(new Uri(url).LocalPath);
                        }
                        catch { fileName = null; }
                    }

                    // 3. Default filename if all else fails
                    if (string.IsNullOrEmpty(fileName))
                    {
                        fileName = fallbackFilename;
                    }

                    string finalFullPath = Path.Combine(destinationFolder, fileName);

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(finalFullPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await stream.CopyToAsync(fileStream);
                    }

                    Console.WriteLine($"[DEBUG] File fully written to: {finalFullPath}");
                    return finalFullPath; // Return the full path of the downloaded file
                }
            }
            catch (Exception ex)
            {
                this.BeginInvoke((MethodInvoker)delegate {
                    MessageBox.Show($"File Download Error\n{ex.Message}", "Message", MessageBoxButtons.OK, MessageBoxIcon.Error);
                });
                return null;
            }
        }

        public static List<DeviceInfo> ParseDeviceTable(string jsonContent)
        {
            var rawList = JsonSerializer.Deserialize<List<JsonElement>>(jsonContent);

            // Use a dictionary to group device sources by product name
            var deviceMap = new Dictionary<string, HashSet<int>>();

            foreach (var item in rawList)
            {
                // 1. Get the productName and deviceSource from each item
                if (item.TryGetProperty("productName", out var nameProp))
                {
                    string name = nameProp.GetString();
                    int source = item.GetProperty("deviceSource").GetInt32();

                    // 2. If the productName is not already in the dictionary, add it with a new HashSet for sources
                    if (!deviceMap.ContainsKey(name))
                    {
                        deviceMap[name] = new HashSet<int>();
                    }
                    deviceMap[name].Add(source);
                }
            }

            // 3. Convert the dictionary into a list of DeviceInfo objects
            return deviceMap.Select(kvp => new DeviceInfo
            {
                Name = kvp.Key,
                Sources = kvp.Value.ToList()
            }).ToList();
        }

        public static string GetDeviceNameBySource(List<DeviceInfo> devices, int sourceToFind)
        {
            // Use LINQ to lookup device name by source
            var device = devices.FirstOrDefault(d => d.Sources.Any(s => s == sourceToFind));

            // Returns Unknown when not found the name
            return device != null ? device.Name : "Unknown";
        }

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
                try
                {
                    // 1. Read json strings
                    string jsonPayload = ctx.GetValue<string>(0);
                    Console.WriteLine($"[DEBUG] Simulator payload: {jsonPayload}");
                    // 2. Parse JSON
                    var response = JsonSerializer.Deserialize<SimulatorResponse>(jsonPayload);
                    // 3. Process the result
                    if (response?.result?.success == true)
                    {
                        this.BeginInvoke((MethodInvoker)delegate
                        {
                            MessageBox.Show("Push success!", "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        });
                    }
                    else
                    {
                        throw new Exception("Receives error message from the simulator");
                    }
                }
                catch (Exception ex)
                {
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        MessageBox.Show($"Error on processing message:\n{ex.Message}", "Message", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    });
                }
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
                    Console.WriteLine($"[DEBUG] Connect Error: {ex.Message}");
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

                devSources = new List<int>();

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

        private void ParseAndRefreshUI(string path)
        {
            var parseResult = ParseSimulatorConfig(path);
            appId = parseResult.AppId;
            projectName = parseResult.AppName;
            string devSourcesStr = "";
            foreach (var item in parseResult.DeviceSource)
            {
                devSourcesStr += GetDeviceNameBySource(deviceInfo ,item);
                devSourcesStr += $"({item.ToString()})";
                devSourcesStr += ", ";
            }
            this.BeginInvoke((MethodInvoker)delegate {
                content_Label.Text = $"Selected Package: \nApp ID: {appId}\nProject Name: {projectName}\nDevice Source: ";
                content_Label.Text += devSourcesStr;
            });
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
                zpkBuffer = ReadZpkFile(openFileDialog1.FileName);

                string zpkDirectory = Path.GetDirectoryName(openFileDialog1.FileName);
                string zpkName = Path.GetFileNameWithoutExtension(openFileDialog1.FileName);
                // Target: zipDirectory / zipName / device / app.json
                string jsonFilePath = Path.Combine(zpkDirectory, zpkName, "device", "app.json");

                if (File.Exists(jsonFilePath))
                {
                    // Read and parse the app.json to get appId, projectName, and deviceSource
                    ParseAndRefreshUI(jsonFilePath);
                    zpk_loc_Label.Text = $"Package: \n{openFileDialog1.FileName}";
                    json_loc_Label.Text = $"JSON: \n{jsonFilePath}";
                }
                else
                {
                    MessageBox.Show("app.json not Exist!\n" +
                        "Seems you not convert your install package!\n" +
                        "Please use the Pick and Convert function first!", "Message", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
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

        /// <summary>
        /// Remove UTF-8 BOM from the specified file if it exists.
        /// </summary>
        private void StripBomFromFile(string filePath)
        {
            byte[] fileBytes = File.ReadAllBytes(filePath);
            if (fileBytes.Length >= 3 && fileBytes[0] == 0xEF && fileBytes[1] == 0xBB && fileBytes[2] == 0xBF)
            {
                byte[] newBytes = new byte[fileBytes.Length - 3];
                Buffer.BlockCopy(fileBytes, 3, newBytes, 0, newBytes.Length);
                File.WriteAllBytes(filePath, newBytes);
            }
        }

        public async void convert_zpk(string zpkPath)
        {
            try
            {
                // Create a base working directory for downloads and conversions
                string basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DownloadPackages");
                if (!Directory.Exists(basePath)) Directory.CreateDirectory(basePath);

                string zpkName = Path.GetFileNameWithoutExtension(zpkPath);
                string ext = Path.GetExtension(zpkPath);

                // 2. If it's an image, scan the QR code to get the download URL and download the ZPK file to the working directory.
                if (ext != ".zpk" && ext != ".zip")
                {
                    content_Label.Text = "Status: Scanning QR...";
                    var reader = new ZXing.Windows.Compatibility.BarcodeReader();
                    using (var bitmap = (System.Drawing.Bitmap)System.Drawing.Image.FromFile(zpkPath))
                    {
                        var result = reader.Decode(bitmap);
                        if (result == null)
                        {
                            throw new Exception("No QR code found in the image!");
                        }
                        string downloadUrl = result.Text.Replace("zpkd1://", "https://");

                        content_Label.Text = "Status: Downloading...";
                        string filepath = await DownloadFileAsync(downloadUrl, basePath, "download_watchface.zpk", true);
                        if (filepath == null)
                        {
                            return; // Download failed, error message already shown in DownloadFileAsync
                        }
                        zpkPath = filepath;
                        zpkName = Path.GetFileNameWithoutExtension(zpkPath);
                        ext = Path.GetExtension(zpkPath);

                    }
                }

                // Creating working directory for the conversion process (will be deleted and recreated if already exists)
                string workDir = Path.Combine(basePath, zpkName + "-mod");
                string tempDir = Path.Combine(basePath, "temp");
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                if (Directory.Exists(workDir)) Directory.Delete(workDir, true);
                Directory.CreateDirectory(workDir);
                Directory.CreateDirectory(tempDir);

                // 3. Extract the outer ZPK/ZIP to the working directory
                ZipFile.ExtractToDirectory(zpkPath, workDir);

                // 4. Extract the inner device.zip to a folder for editing
                string deviceZipPath = Path.Combine(workDir, "device.zip");
                string deviceContentDir = Path.Combine(tempDir, "device");
                bool hasAppJson = false;
                if (File.Exists(deviceZipPath))
                {
                    ZipFile.ExtractToDirectory(deviceZipPath, deviceContentDir);
                }
                else if (File.Exists(Path.Combine(workDir, "app.json")))
                {
                    hasAppJson = true;
                    // Move the whole folder to temp if device.zip doesn't exist but app.json exists (Exported watchface)
                    Directory.Move(workDir, deviceContentDir);
                    // Recreate the workDir
                    Directory.CreateDirectory(workDir);
                }
                else
                {
                    throw new Exception("Neither device.zip nor app.json found in the package!");
                }


                // 5. Edit the app.json in the extracted device folder
                // (open in Notepad, wait for user to save and close, then continue)
                string appJsonPath = Path.Combine(deviceContentDir, "app.json");

                Process.Start("explorer.exe", $@"{tempDir}\device\assets");
                MessageBox.Show("Please convert all of the image backs to the normal png (under \"program_folder\\DownloadPackages\\temp\\device\\assets\"!\n\n" +
                    "Comfirm this dialog to continue the upload process!", "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);

                
                // Step to edit the app.json file, includes the selected device
                // Read and trims the UTF-8 BOM (3 extra bytes at the start of the file) if it exists

                // Read the app.json file
                string jsonString = File.ReadAllText(appJsonPath).TrimStart('\uFEFF');

                // Parse the JSON string into a JsonNode for manipulation
                JsonNode rootNode = JsonNode.Parse(jsonString);

                // Access the "platforms" array and modify it based on the selected device
                var platformsNode = rootNode["platforms"].AsArray();

                // Update the "platforms" array with the selected device's information
                if (platformsNode != null && platformsNode.Count > 0)
                {
                    int index = devlistCombo.SelectedIndex;
                    rootNode["platforms"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["name"] = deviceInfo[index].Name,
                            ["deviceSource"] = deviceInfo[index].Sources[0]
                        }
                    };
                }

                string updatedJson = rootNode.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(appJsonPath, updatedJson);

                // Removes the UTF-8 BOM
                StripBomFromFile(appJsonPath);

                string backupDeviceFileName = Path.GetFileName(deviceZipPath) + ".old";

                // If has app.json, creates an app-side.zip with the app.json only for later use
                // (Watchface Editor Exports)
                if (hasAppJson)
                {
                    File.Copy(Path.Combine(deviceContentDir, "app.json"), Path.Combine(workDir, "app.json"));
                    // Create the app-side.zip with the app.json for later use
                    ZipFile.CreateFromDirectory(workDir, Path.Combine(tempDir, "app-side.zip"));
                    File.Move(Path.Combine(tempDir, "app-side.zip"), Path.Combine(workDir, "app-side.zip"));
                    File.Delete(Path.Combine(workDir, "app.json"));
                }
                else
                {
                    // Backup the original device.zip into temp folder
                    Directory.CreateDirectory(tempDir);
                    File.Move(deviceZipPath,
                    Path.Combine(tempDir, backupDeviceFileName));
                }

                // 6. Pack device.zip
                ZipFile.CreateFromDirectory(deviceContentDir, deviceZipPath);

                // 7. Repacks the final zpk
                string finalZpkName = Path.GetFileNameWithoutExtension(zpkPath) + "-mod.zpk";
                string finalZpkPath = Path.Combine(basePath, finalZpkName);
                // If a file with the final name already exists, delete it before creating a new one
                if (File.Exists(finalZpkPath)) File.Delete(finalZpkPath);
                ZipFile.CreateFromDirectory(workDir, finalZpkPath);
                if (!hasAppJson)
                {
                    // Moves the backup of original device.zip and unziped device folder back to the working directory
                    File.Move(Path.Combine(tempDir, backupDeviceFileName),
                        Path.Combine(workDir, backupDeviceFileName));
                }
                Directory.Move(deviceContentDir, Path.Combine(workDir, "device"));
                // Updates the JSON path
                appJsonPath = Path.Combine(Path.Combine(workDir, "device"), "app.json");

                // 8. Read the final ZPK and JSON automatically, and refresh the UI with the new info
                this.zpkBuffer = ReadZpkFile(finalZpkPath);
                zpk_loc_Label.Text = $"Package: \n{finalZpkPath}";
                json_loc_Label.Text = $"JSON: \n{appJsonPath}";
                ParseAndRefreshUI(appJsonPath);

                MessageBox.Show($"Convert done!", "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Convert error: {ex.Message}\n{ex.StackTrace}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void convertBtn_Click(object sender, EventArgs e)
        {
            // 1. Pick a file (either image with QR code or a ZPK/ZIP)
            openFileDialog1.Filter = "QR Picture or zip/zpk|*.png;*.jpg;*.jpeg;*.zpk;*.zip";
            if (openFileDialog1.ShowDialog() != DialogResult.OK) return;

            convert_zpk(openFileDialog1.FileName);
        }

        private async void MainWindow_Load(object sender, EventArgs e)
        {
            string filePath = await DownloadFileAsync("https://upload-cdn.zepp.com/zeppos/devkit/zeus/v1/devices.json", 
                AppDomain.CurrentDomain.BaseDirectory, "devices.json", false);

            if(string.IsNullOrEmpty(filePath))
            {
                MessageBox.Show($"Failed to download devices.json!\n" +
                    $"Will use the downloaded version.", "Message", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                // Load the devtable.html file from the application directory
                filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "devices.json");
            }
            // 2. Check if the file exists
            if (File.Exists(filePath))
            {
                try
                {
                    // 3. Read the JSON content from the file
                    string jsonContent = File.ReadAllText(filePath);
                    // 4. Call the ParseDeviceTable method to parse the JSON content and get the list of devices
                    deviceInfo = ParseDeviceTable(jsonContent);
                    foreach (var device in deviceInfo)
                    {
                        devlistCombo.Items.Add(device.Name);
                    }
                    devlistCombo.SelectedIndex = 0; // Select the first device by default
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Parse devtable error: {ex.Message}\n{ex.StackTrace}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Application.Exit();
                }
            }
            else
            {
                MessageBox.Show($"devices.json not found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }

        private void devlistCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = devlistCombo.SelectedIndex;
            Console.WriteLine($"[DEBUG] Selected device: {deviceInfo[index].Name}, Sources: {string.Join(", ", deviceInfo[index].Sources)}");
        }
    }
}
