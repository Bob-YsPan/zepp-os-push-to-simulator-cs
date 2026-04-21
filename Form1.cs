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
        private SocketIO _socket;
        private int _previewId = 1;
        private string _previewMethod = "ide.simulator.preview";
        private string appId;
        private string projectName;
        private int primarySource;
        private byte[] zpkBuffer;
        private List<int> devSources;

        public void SimulatorInit(string url)
        {
            if (_socket != null) return;

            // 根據官方 Options 表格調整
            var options = new SocketIOOptions
            {
                // 如果 V4 (預設) 連不上，請務必改成 V3 試試看
                EIO = EngineIO.V4,

                // 如果模擬器只接受 WebSocket，設為 WebSocket；否則維持 Polling + AutoUpgrade
                Transport = TransportProtocol.WebSocket,
                AutoUpgrade = true,
                Reconnection = true,
                ReconnectionAttempts = 5
            };

            _socket = new SocketIO(new Uri(url), options);

            // 官方 On 語法：接收事件 (如果模擬器會回傳資料)
            _socket.On("message", ctx =>
            {
                Console.WriteLine($"收到模擬器回傳: {ctx.RawText}");
                return Task.CompletedTask;
            });

            _socket.OnConnected += (sender, e) =>
            {
                this.BeginInvoke((MethodInvoker)delegate {
                    MessageBox.Show("Connected to Simulator!");
                });
            };

            // 官方建議的連線方式
            Task.Run(async () => {
                try
                {
                    await _socket.ConnectAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Connect Error: {ex.Message}");
                }
            });
        }

        public async Task Upload(byte[] data, string projectName, string target, string appid, List<int> devices)
        {
            if (!_socket.Connected)
            {
                MessageBox.Show("請先確認 Socket 已連線");
                return;
            }

            var dataArr = Array.ConvertAll(data, b => (int)b);

            // 1. 構建與 JS encodeMessage 完全一致的物件
            var payload = new
            {
                jsonrpc = "2.0",
                method = _previewMethod,
                @params = new
                {
                    target = target,
                    projectName = projectName,
                    appid = appid,
                    size = data.Length,
                    data = dataArr,
                    devices = devices
                },
                id = _previewId
            };


            // 2. 將物件序列化成 JSON 字串 (這是模擬器 encodeMessage 做的事)
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = null // 保持匿名物件定義的大小寫
            };
            string jsonMessage = JsonSerializer.Serialize(payload, options);

            Console.WriteLine($"[DEBUG] 發送 JSON 字串至 'message' 事件");

            // 4. 使用 EmitAsync 發送 "message" 事件，參數放該字串
            // 這裡使用 new[] { jsonString } 是為了符合 IEnumerable<object> 的參數要求
            await _socket.EmitAsync("message", new[] { jsonMessage });
        }

        /// <summary>
        /// 讀取並解析 JSON 設定檔 (處理了 UTF-8 BOM)
        /// </summary>
        public (string AppName, string AppId, int FirstDeviceSource) ParseSimulatorConfig(string jsonPath)
        {
            try
            {
                // 1. 讀取檔案並處理 UTF-8 BOM
                string jsonString = File.ReadAllText(jsonPath).TrimStart('\uFEFF');

                // 2. 解析為 JsonDocument (不需定義 Class 模型)
                using (JsonDocument doc = JsonDocument.Parse(jsonString))
                {
                    JsonElement root = doc.RootElement;

                    // 3. 提取 appName (路徑: app -> appName)
                    string appName = root.GetProperty("app").GetProperty("appName").GetString();

                    // 4. 提取 appId (路徑: app -> appId)
                    // 注意：JSON 裡 appId 是數字，這裡轉成字串方便後續使用
                    string appId = root.GetProperty("app").GetProperty("appId").ToString();

                    // 5. 提取第一個 platforms 中的 deviceSource (路徑: platforms[0] -> deviceSource)
                    int firstDeviceSource = 229; // 給予預設值
                    if (root.TryGetProperty("platforms", out JsonElement platforms) && platforms.GetArrayLength() > 0)
                    {
                        firstDeviceSource = platforms[0].GetProperty("deviceSource").GetInt32();

                        // 確保這裡正確賦值給全域變數
                        devSources = platforms.EnumerateArray()
                            .Select(p => p.GetProperty("deviceSource").GetInt32()) // 保持數字
                            .ToList();
                    }

                    return (appName, appId, firstDeviceSource);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"解析設定檔失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 讀取 ZPK 二進制文件
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
            openFileDialog1.Filter = "Zepp OS Package (*.zpk)|*.zpk|ZIP package (*.zip)|*.zip";
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

                // 1. 讀取配置與參數
                var parseResult = ParseSimulatorConfig(openFileDialog1.FileName);
                appId = parseResult.AppId;
                projectName = parseResult.AppName;
                primarySource = parseResult.FirstDeviceSource;
                content_Label.Text = $"Content: \nApp ID: {appId}\nProject Name: {projectName}\nPrimary Device Source: {primarySource}";
            }

        }

        private void send_Btn_Click(object sender, EventArgs e)
        {
            // 如果還沒初始化或連線，先初始化
            if (_socket == null)
            {
                SimulatorInit("http://localhost:7650");
                return;
            }

            // 檢查資料是否準備好
            if (zpkBuffer == null || string.IsNullOrEmpty(appId))
            {
                MessageBox.Show("Please select ZPK and app.json first!");
                return;
            }

            _ = Upload(zpkBuffer, projectName, "watch", appId, devSources);
            MessageBox.Show("Push Sent!");
        }
    }
}
