using DiscordRPC;
using DiscordRPC.Logging;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;

namespace BRMM
{
    public partial class Main : Form
    {
        public DiscordRpcClient client;
        Web_Bridge web_Bridge = new Web_Bridge();
        public WebView2 webView;
        public string GamePath, SteamPath = "";
        public string id_gl, token_gl = "";

        bool IsValidId(string id) => !string.IsNullOrEmpty(id) && Regex.IsMatch(id, @"^[A-Za-z0-9_-]{3,64}$");
        bool IsValidToken(string token) => !string.IsNullOrEmpty(token) && token.Length <= 70;

        public bool CDATA, DRPC = false;

        [DllImport("user32.DLL", EntryPoint = "ReleaseCapture")]
        private extern static void ReleaseCapture();
        [DllImport("user32.DLL", EntryPoint = "SendMessage")]
        private extern static void SendMessage(System.IntPtr one, int two, int three, int four);

        private const int grip = 16;

        private const int caption = 32;

        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]

        
        private static extern IntPtr CreateRoundRectRgn
        (
            int nLeftRect,        // x-coordinate of upper-left corner 
            int nTopRect,         // y-coordinate of upper-left corner 
            int nRightRect,       // x-coordinate of lower-right corner
            int nBottomRect,      // y-coordinate of lower-right corner
            int nWidthEllipse,    // width of ellipse                  
            int nHeightEllipse    // height of ellipse                 
        );



        public Main(string[] args)
        {
            InitializeComponent();
            InitializeWeb(args);
            InitializeDirs();
            SteamPath = GetSteamPath();
            GamePath = GetGameInstallPath(SteamPath, "552100");
            this.SetStyle(ControlStyles.ResizeRedraw, true);
            this.DoubleBuffered = true;
        }

        public void InitializeDirs()
        {
            if (!Directory.Exists(Application.StartupPath + "/Mods")){
                Directory.CreateDirectory((Application.StartupPath + "/Mods"));
            }

            if (!Directory.Exists(Application.StartupPath + "/Modpacks"))
            {
                Directory.CreateDirectory((Application.StartupPath + "/Modpacks"));
            }

            if (!Directory.Exists(Application.StartupPath + "/Imgs"))
            {
                Directory.CreateDirectory((Application.StartupPath + "/Imgs"));
            }
        }
        public void InitializeRPC()
        {

            client = new DiscordRpcClient("1370904178523635802");

            client.Logger = new ConsoleLogger() { Level = LogLevel.Warning };

            client.Initialize();

            client.SetPresence(new RichPresence()
            {
                Details = "Just Started BRMM",
                State = "Idle",
                //Assets = new Assets()
                //{
                //    LargeImageKey = "image_large",
                //    LargeImageText = "Lachee's Discord IPC Library",
                //    SmallImageKey = "image_small"
                //}
            });

            web_Bridge.ConsoleLog("Starting Discord RPC");

        }


        public async void InitializeWeb(string[] args)
        {
            await webView21.EnsureCoreWebView2Async();
            webView21.CoreWebView2.AddHostObjectToScript("dotnet", web_Bridge);
            webView21.CoreWebView2.NavigationCompleted += async (sender, eventArgs) =>
            {
                if (args.Length > 0)
                {
                    if (!Uri.TryCreate(args[0], UriKind.Absolute, out Uri uri))
                    {
                        web_Bridge.ConsoleLog("Invalid URI");
                        return;
                    }

                    var allowedSchemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "brmm", "https", "http" };
                    if (!allowedSchemes.Contains(uri.Scheme))
                    {
                        web_Bridge.ConsoleLog($"Unsupported URI scheme: {uri.Scheme}");
                        return;
                    }

                    NameValueCollection query = HttpUtility.ParseQueryString(uri.Query);
                    string token = (query["token"] ?? string.Empty).Trim();
                    string id = (query["id"] ?? string.Empty).Trim();

                    if (!IsValidId(id) || !IsValidToken(token))
                    {
                        web_Bridge.ConsoleLog("Invalid id/token format");
                        return;
                    }

                    id_gl = id;
                    token_gl = token;

                    string jso3n = File.ReadAllText(Application.StartupPath + "/brmm.config");
                    var brmmconf = JsonConvert.DeserializeObject<brmmconfig>(jso3n);

                    web_Bridge.UpdateConfig(brmmconf.DCRPC, brmmconf.CDATA, "normal", false);

                    await Getdata();
                    var payload = new { id = id, token = token };
                    string json = JsonConvert.SerializeObject(payload);
                    web_Bridge.Customfunction("SetCredits", json);

                }
                else
                {
                    web_Bridge.ConsoleLog("No args in URL");
                }
                
                web_Bridge.ConsoleLog(Path.Combine(Application.StartupPath, "brmm.ico"));
                if (!File.Exists(Application.StartupPath + "/brmm.config"))
                {
                    brmmconfig conf = new brmmconfig();

                    conf.DCRPC = true;
                    conf.FirstTime = true;
                    conf.CDATA = true;
                    conf.CTheme = "normal";

                    string jsonconf = JsonConvert.SerializeObject(conf);
                    File.WriteAllText(Application.StartupPath + "/brmm.config", jsonconf);
                }

                if (File.Exists(Application.StartupPath + "/brmm.config"))
                {
                    string json = File.ReadAllText(Application.StartupPath + "/brmm.config");
                    var brmmconf = JsonConvert.DeserializeObject<brmmconfig>(json);
                    CDATA = brmmconf.CDATA;
                    DRPC = brmmconf.DCRPC;
                    web_Bridge.Customfunction("UpdateConfigWeb", json);
                    if (brmmconf.FirstTime)
                    {
                        web_Bridge.Customfunction("FirstTimeinbrmm","");
                    }
                }

                if (DRPC)
                {
                    InitializeRPC();
                }

                web_Bridge.Setup();
            };
            webView21.CoreWebView2.Navigate($"file:///{Application.StartupPath}/Web/Main-Body/Main.html");
            web_Bridge.main = this;
            webView = webView21;
        }


        //Steam Scary!!

        static string GetSteamPath()
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam"))
            {
                if (key != null)
                {
                    Object o = key.GetValue("InstallPath");
                    if (o != null)
                    {
                        return o as string;
                    }
                }
            }
            return null;
        }

        static string GetGameInstallPath(string steamPath, string gameID)
        {
            string libraryFoldersPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");

            if (!File.Exists(libraryFoldersPath))
            {
                return null;
            }

            string[] libraryPaths = ParseLibraryFolders(libraryFoldersPath);

            foreach (string libraryPath in libraryPaths)
            {
                string gameManifestPath = Path.Combine(libraryPath, "steamapps", $"appmanifest_{gameID}.acf");

                if (File.Exists(gameManifestPath))
                {
                    string installDir = ParseInstallDir(gameManifestPath);
                    return Path.Combine(libraryPath, "steamapps", "common", installDir);
                }
            }

            return null;
        }

        static string[] ParseLibraryFolders(string libraryFoldersPath)
        {
            var libraryPaths = File.ReadAllLines(libraryFoldersPath)
                .Where(line => line.Contains("path"))
                .Select(line => line.Split(new[] { '"' }, StringSplitOptions.RemoveEmptyEntries)[3])
                .ToArray();

            return libraryPaths;
        }

        static string ParseInstallDir(string manifestPath)
        {
            var lines = File.ReadAllLines(manifestPath);

            foreach (var line in lines)
            {
                if (line.Contains("\"installdir\""))
                {
                    return line.Split(new[] { '"' }, StringSplitOptions.RemoveEmptyEntries)[3];
                }
            }

            return null;
        }

        public async Task Getdata()
        {
            try
            {

                using (HttpClient client = new HttpClient())
                {
                    try
                    {
                        using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Head, "https://github.com/BrickRigs-AI/BRMM/releases/download/v0.4.4-alpha-merge/Updater.zip"))
                        using (HttpResponseMessage response = await client.SendAsync(request))
                        {
                            if (response.IsSuccessStatusCode)
                            {
                                web_Bridge.Customfunction("showSection", "Autoupdate");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        
                    }
                }

                using (HttpClient client = new HttpClient())
                {
                    var data = new
                    {
                        token = token_gl,
                        id = id_gl
                    };

                    string json = System.Text.Json.JsonSerializer.Serialize(data);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAsync("https://service.brmm.ovh/api/get_all_mods", content);
                    string responseString = await response.Content.ReadAsStringAsync();

                    var doc = System.Text.Json.JsonDocument.Parse(responseString);
                    var root = doc.RootElement;

                    var modsArray = root.GetProperty("mods");
                    var modifiedMods = new List<object>();

                    foreach (var mod in modsArray.EnumerateArray())
                    {
                        var descriptionmd = mod.GetProperty("descriptionmd").GetString();
                        var escapedDescriptionmd = descriptionmd
                            .Replace("\\", "\\\\")
                            .Replace("\r", "\\r")
                            .Replace("\n", "\\n")
                            .Replace("}", "")
                            .Replace("{", "")
                            .Replace("\"", " ")
                            .Replace("\t", "\\t");

                        var modDict = mod.EnumerateObject().ToDictionary(p => p.Name, p => p.Name == "descriptionmd" ? (object)escapedDescriptionmd : p.Value.Clone());
                        modifiedMods.Add(modDict);
                    }

                    var finalJson = System.Text.Json.JsonSerializer.Serialize(new { mods = modifiedMods });

                    web_Bridge.Customfunction("SetMods", finalJson);
                    web_Bridge.ConsoleLog(finalJson);
                }




                using (HttpClient client = new HttpClient())
                {
                    var data = new
                    {
                        token = token_gl,
                        id = id_gl
                    };

                    string json = System.Text.Json.JsonSerializer.Serialize(data);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAsync("https://service.brmm.ovh/api/get_all_users", content);

                    string responseString = await response.Content.ReadAsStringAsync();
                    web_Bridge.ConsoleLog(responseString);
                    web_Bridge.Customfunction("SetAllUsers", responseString);
                }





                using (HttpClient client = new HttpClient())
                {
                    var data = new
                    {
                        token = token_gl,
                        id = id_gl
                    };

                    string json = System.Text.Json.JsonSerializer.Serialize(data);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAsync("https://service.brmm.ovh/api/brmm/tags", content);

                    string responseString = await response.Content.ReadAsStringAsync();
                    web_Bridge.ConsoleLog(responseString);
                    web_Bridge.Customfunction("SetTagsVersions", responseString);
                }


                using (HttpClient client = new HttpClient())
                {
                    var data = new
                    {
                        token = token_gl,
                        id = id_gl
                    };

                    string json = System.Text.Json.JsonSerializer.Serialize(data);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAsync("https://service.brmm.ovh/api/get_all_organizations", content);

                    string responseString = await response.Content.ReadAsStringAsync();
                    web_Bridge.ConsoleLog(responseString);
                    web_Bridge.Customfunction("SetOrganizations", responseString);
                }

                using (HttpClient client = new HttpClient())
                {
                    var data = new
                    {
                        token = token_gl,
                        id = id_gl,
                        userid = id_gl

                    };

                    string json = System.Text.Json.JsonSerializer.Serialize(data);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAsync("https://service.brmm.ovh/api/user_details", content);

                    string responseString = await response.Content.ReadAsStringAsync();
                    web_Bridge.ConsoleLog(responseString);
                    web_Bridge.Customfunction("SetUser", responseString);
                }
            }
            catch (Exception ex) 
            {
                web_Bridge.ConsoleLog(ex.Message);
            }
        }

        //Ui Stuff

        public void MouseDownHandlerWeb()
        {
            ReleaseCapture();
            SendMessage(Handle, 0x112, 0xf012, 0);
        }

        //kill then kill them all!
        protected override void WndProc(ref Message m)
        {
            const int HTLEFT = 10;
            const int HTRIGHT = 11;
            const int HTTOP = 12;
            const int HTTOPLEFT = 13;
            const int HTTOPRIGHT = 14;
            const int HTBOTTOM = 15;
            const int HTBOTTOMLEFT = 16;
            const int HTBOTTOMRIGHT = 17;
            const int HTCAPTION = 2;

            if (m.Msg == 0x84) // WM_NCHITTEST
            {
                Point cursor = new Point(m.LParam.ToInt32());
                cursor = this.PointToClient(cursor);
                int w = this.ClientSize.Width;
                int h = this.ClientSize.Height;

                if (cursor.X <= grip && cursor.Y <= grip)
                {
                    m.Result = (IntPtr)HTTOPLEFT;
                    return;
                }
                if (cursor.X >= w - grip && cursor.Y <= grip)
                {
                    m.Result = (IntPtr)HTTOPRIGHT;
                    return;
                }
                if (cursor.X <= grip && cursor.Y >= h - grip)
                {
                    m.Result = (IntPtr)HTBOTTOMLEFT;
                    return;
                }
                if (cursor.X >= w - grip && cursor.Y >= h - grip)
                {
                    m.Result = (IntPtr)HTBOTTOMRIGHT;
                    return;
                }
                if (cursor.X <= grip)
                {
                    m.Result = (IntPtr)HTLEFT;
                    return;
                }
                if (cursor.X >= w - grip)
                {
                    m.Result = (IntPtr)HTRIGHT;
                    return;
                }
                if (cursor.Y <= grip)
                {
                    m.Result = (IntPtr)HTTOP;
                    return;
                }
                if (cursor.Y >= h - grip)
                {
                    m.Result = (IntPtr)HTBOTTOM;
                    return;
                }
                if (cursor.Y <= caption)
                {
                    m.Result = (IntPtr)HTCAPTION;
                    return;
                }
            }

            base.WndProc(ref m);
        }

        private void Main_SizeChanged(object sender, EventArgs e)
        {
            Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, this.Width, this.Height, 20, 20));
        }
    }
}