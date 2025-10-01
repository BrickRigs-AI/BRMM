using DiscordRPC;
using Microsoft.Web.WebView2.WinForms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace BRMM
{
    public class Modpack
    {
        public string Name { get; set; }

        public string Description { get; set; }
        public string Author { get; set; }
        public string Imge { get; set; }
        public string Banner { get; set; }
        public string Version { get; set; }
        public List<Mod> ModList { get; set; }
    }

    public class Mod
    {
        public string Modid { get; set; }

        public int ModVersion { get; set; } = 0;
        public bool Enable { get; set; }
    }

    public class brmmconfig
    {
        public bool DCRPC { get; set; }

        public bool CDATA { get; set; }
        public string FrameColor { get; set; }
        public string CTheme { get; set; }
        public bool FirstTime { get; set; }
    }

    [System.Runtime.InteropServices.ComVisible(true)]

    public class Web_Bridge
    {
        public Main main;

        public void ConsoleLog(string text)
        {
            if (main == null || main.webView == null)
                return;

            if (main.InvokeRequired)
            {
                main.Invoke(new Action(() => ConsoleLog(text)));
                return;
            }

            try
            {
                string escapedText = text
                    .Replace("\\", "\\\\")
                    .Replace("'", "\\'")
                    .Replace("\"", "\\\"");

                main.webView.CoreWebView2.ExecuteScriptAsync($@"Console('{escapedText}')");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in WebView2: {ex.Message}");
            }
        }

        public async void Customfunction(string function, string text)
        {
            try
            {
                if (main?.webView?.CoreWebView2 != null)
                {
                    try
                    {
                        await main.webView.CoreWebView2.ExecuteScriptAsync($@"{function}('{text}')");
                    }
                    catch (Exception ex)
                    {
                        ConsoleLog($"Error executing script: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleLog($"Error executing script: {ex.Message}");
            }
        }
        public async void SetupMainData(string token, string id)
        {
            main.token_gl = token;
            main.id_gl = id;
            main.Getdata();
        }
        public void UpdateFrameStyle(string colors)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(colors)) return;

                string[] rgb = colors.Split(',');
                if (rgb.Length != 3) return;

                int r = int.Parse(rgb[0].Trim());
                int g = int.Parse(rgb[1].Trim());
                int b = int.Parse(rgb[2].Trim());

                string json = File.ReadAllText(Application.StartupPath + "/brmm.config");
                var brmmconf = JsonConvert.DeserializeObject<brmmconfig>(json);

                brmmconf.FrameColor = colors;

                main.BackColor = Color.FromArgb(r, g, b);

                string json2 = JsonConvert.SerializeObject(brmmconf, Formatting.Indented);
                File.WriteAllText(Application.StartupPath + "/brmm.config", json2);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd przy aktualizacji koloru: " + ex.Message);
            }
        }

        public void UpdateConfig(bool DR,bool CD,string th, bool ft)
        {
            string json = File.ReadAllText(Application.StartupPath + "/brmm.config");
            var brmmconf = JsonConvert.DeserializeObject<brmmconfig>(json);
            brmmconf.CDATA = CD;
            brmmconf.DCRPC = DR;
            brmmconf.CTheme = th;
            brmmconf.FirstTime = ft;

            string json2 = JsonConvert.SerializeObject(brmmconf);
            File.WriteAllText(Application.StartupPath + "/brmm.config", json2);
        }

        public void OpenLinkWebview(string link)
        {
            System.Diagnostics.Process.Start(new ProcessStartInfo
            {
                FileName = link,
                UseShellExecute = true
            });
        }
        public void BRMMLogin()
        {
            string url = "https://beta.brmm.ovh/quicklogin";
            System.Diagnostics.Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
            main.Close();
        }


        public async void DownloadModpackAsync(string token, string id, string modpackId)
        {
            string tokenweb = WebUtility.UrlEncode(token);

            var httpClient = new HttpClient();
            var uri = new Uri($"https://service.brmm.ovh/api/mod/download/{tokenweb}/{id}/{modpackId}");
            var bytes = await httpClient.GetByteArrayAsync(uri);
            string modpackDirectory = Path.Combine(Application.StartupPath, "Modpacks");
            Directory.CreateDirectory(modpackDirectory);
            string finalFilePath = Path.Combine(modpackDirectory, $"{modpackId}.brm");

            File.WriteAllBytes(finalFilePath, bytes);

            string json = File.ReadAllText(finalFilePath);
            Modpack modpack = JsonConvert.DeserializeObject<Modpack>(json);
            string finalFilePateh = Path.Combine(modpackDirectory, $"{modpack.Name}.brm");

            if (File.Exists(finalFilePateh))
                File.Delete(finalFilePateh);

            File.Move(finalFilePath, finalFilePateh);

            GetAllModpacks();
            Customfunction("OpenModpackPage", modpack.Name);
        }



        public async void GetAllModpacks()
        {
            string[] Modpacks = Directory.GetFiles(Application.StartupPath + "/Modpacks/");

            List<Modpack> mopakcs_l = new List<Modpack>();

            for (int i = 0; i < Modpacks.Length; i++)
            {
                if (File.Exists(Modpacks[i]))
                {
                    string json = File.ReadAllText(Modpacks[i]);
                    mopakcs_l.Add(JsonConvert.DeserializeObject<Modpack>(json));
                }
                else
                {
                    ConsoleLog("Invalid Modpack");
                }
            }

            var modPack_w = JsonConvert.SerializeObject(mopakcs_l);

            ConsoleLog(modPack_w.ToString());

            Customfunction("SetModpacksLocal", modPack_w.ToString());

            Customfunction("UpdateData", "");
        }


        public async void DeleteModpack(string modpack_name)
        {
            string modpackPath = Path.Combine(Application.StartupPath, "Modpacks", modpack_name + ".brm");

            if (!File.Exists(modpackPath))
            {
                ConsoleLog("Modpack dosent exist.");
                return;
            }
            File.Delete(modpackPath);

            ConsoleLog($"Removed modpack '{modpack_name}'.");
            Customfunction("UpdateData", "");
            Customfunction("window.location.reload", "true");
            //Customfunction("BRMMReload", "");
        }
        public void moushandle()
        {
            main.MouseDownHandlerWeb();
        }

        public void updateBRMM()
        {
            string url = "https://github.com/BrickRigs-AI/BRMM/releases/download/v0.4.3-alpha-merge/Updater.zip";

            string appStartupPath = Application.StartupPath;

            string updaterPath = Path.Combine(appStartupPath, "updaterbrmm", "BRMMupdater.exe");

            if (!File.Exists(updaterPath))
            {
                ConsoleLog("No such file as BRMMupdater.exe!");
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = updaterPath,
                    Arguments = $"{url} \"{appStartupPath}\"",
                    WorkingDirectory = Path.GetDirectoryName(updaterPath),
                    UseShellExecute = true
                });

                main.Close();
            }
            catch (Exception ex)
            {
                ConsoleLog("Error while updating: " + ex.Message);
            }
        }
        public async void ImportModpack()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Brick Rigs Modpack (*.brmp;*.brm)|*.brmp;*.brm";
            openFileDialog.Title = "Choose the modpack to import.";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string selectedFile = openFileDialog.FileName;
                string extension = Path.GetExtension(selectedFile).ToLower();

                string modpacksFolder = Path.Combine(Application.StartupPath, "Modpacks");
                string modsFolder = Path.Combine(Application.StartupPath, "Mods");
                Directory.CreateDirectory(modpacksFolder);
                Directory.CreateDirectory(modsFolder);

                if (extension == ".brm")
                {
                    try
                    {
                        string dest = Path.Combine(modpacksFolder, Path.GetFileName(selectedFile));
                        if (File.Exists(dest))
                            File.Delete(dest);

                        File.Copy(selectedFile, dest);
                        ConsoleLog($"Imported single modpack: {selectedFile}");
                    }
                    catch (Exception ex)
                    {
                        ConsoleLog($"Error while importing .brm: {ex.Message}");
                    }
                }
                else if (extension == ".brmp")
                {
                    string tempFolder = Path.Combine(Path.GetTempPath(), "ModpackImport_" + Guid.NewGuid());
                    Directory.CreateDirectory(tempFolder);

                    try
                    {
                        ZipFile.ExtractToDirectory(selectedFile, tempFolder);

                        foreach (string file in Directory.GetFiles(tempFolder, "*.brm"))
                        {
                            string dest = Path.Combine(modpacksFolder, Path.GetFileName(file));
                            if (File.Exists(dest))
                                File.Delete(dest);
                            File.Move(file, dest);
                        }

                        foreach (string file in Directory.GetFiles(tempFolder))
                        {
                            string ext = Path.GetExtension(file).ToLower();
                            if (ext != ".brm")
                            {
                                string dest = Path.Combine(modsFolder, Path.GetFileName(file));
                                if (File.Exists(dest))
                                    File.Delete(dest);
                                File.Move(file, dest);
                            }
                        }

                        ConsoleLog($"Imported modpack from: {selectedFile}");
                    }
                    catch (Exception ex)
                    {
                        ConsoleLog($"Error while importing: {ex.Message}");
                    }
                    finally
                    {
                        if (Directory.Exists(tempFolder))
                            Directory.Delete(tempFolder, true);
                    }
                }
            }
            else
            {
                ConsoleLog("Import canceled.");
            }

            GetAllModpacks();
        }



        //Peak C# code.
        //yes we all know.
        public async void ExportModpack(string modpack_name)
        {
            string modpackPath = Path.Combine(Application.StartupPath, "Modpacks", modpack_name + ".brm");

            if (!File.Exists(modpackPath))
            {
                ConsoleLog("Modpack doesn't exist.");
                return;
            }

            string json = File.ReadAllText(modpackPath);
            var modPack = JsonConvert.DeserializeObject<Modpack>(json);

            bool hasLocalMods = modPack.ModList.Any(m => m.Modid.StartsWith("LOCAL&"));

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Choose the location to save the exported modpack.";

            if (hasLocalMods)
            {
                saveFileDialog.Filter = "Brick Rigs Modpack Package (*.brmp)|*.brmp";
                saveFileDialog.FileName = modpack_name + ".brmp";
            }
            else
            {
                saveFileDialog.Filter = "Brick Rigs Modpack (*.brm)|*.brm";
                saveFileDialog.FileName = modpack_name + ".brm";
            }

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                if (hasLocalMods)
                {
                    string tempFolder = Path.Combine(Path.GetTempPath(), "ModpackExport_" + Guid.NewGuid());
                    Directory.CreateDirectory(tempFolder);

                    File.Copy(modpackPath, Path.Combine(tempFolder, modpack_name + ".brm"), true);

                    string modsFolder = Path.Combine(Application.StartupPath, "Mods");

                    foreach (var mod in modPack.ModList)
                    {
                        if (mod.Modid.StartsWith("LOCAL&"))
                        {
                            string localFileName = mod.Modid.Replace("LOCAL&", "");
                            string sourcePak = Path.Combine(modsFolder, "LOCAL&" + localFileName + ".pak");
                            string sourceZip = Path.Combine(modsFolder, "LOCAL&" + localFileName + ".zip");

                            if (File.Exists(sourcePak))
                                File.Copy(sourcePak, Path.Combine(tempFolder, Path.GetFileName(sourcePak)), true);

                            if (File.Exists(sourceZip))
                                File.Copy(sourceZip, Path.Combine(tempFolder, Path.GetFileName(sourceZip)), true);
                        }
                    }

                    if (File.Exists(saveFileDialog.FileName))
                        File.Delete(saveFileDialog.FileName);

                    ZipFile.CreateFromDirectory(tempFolder, saveFileDialog.FileName);
                    Directory.Delete(tempFolder, true);

                    ConsoleLog($"Exported modpack '{modpack_name}' (with local mods) to: {saveFileDialog.FileName}");
                }
                else
                {
                    File.Copy(modpackPath, saveFileDialog.FileName, true);
                    ConsoleLog($"Exported modpack '{modpack_name}' (no local mods) to: {saveFileDialog.FileName}");
                }
            }
            else
            {
                ConsoleLog("Export canceled.");
            }
        }


        public async void AddModLocal(string modpack_name)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Mod Files (*.pak;*.zip)|*.pak;*.zip";
            openFileDialog.Title = "Select File Mod (.pak or .zip)";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string selectedFile = openFileDialog.FileName;
                string fileNamenull= Path.GetFileName(selectedFile);
                string fileName = Path.GetFileNameWithoutExtension(selectedFile);
                string modsFolder = Path.Combine(Application.StartupPath, "Mods");
                string destinationPath = Path.Combine(modsFolder, "LOCAL&" + fileNamenull);

                if (!Directory.Exists(modsFolder))
                {
                    Directory.CreateDirectory(modsFolder);
                }

                File.Copy(selectedFile, destinationPath, true);

                if (File.Exists(Application.StartupPath + "/Modpacks/" + modpack_name + ".brm"))
                {
                    string json = File.ReadAllText(Application.StartupPath + "/Modpacks/" + modpack_name + ".brm");
                    var modPack = JsonConvert.DeserializeObject<Modpack>(json);

                    Mod mod = new Mod();
                    mod.Modid = "LOCAL&" + fileName;
                    mod.Enable = true;

                    modPack.ModList.Add(mod);

                    string json2 = JsonConvert.SerializeObject(modPack, Formatting.Indented);
                    File.WriteAllText(Application.StartupPath + "/Modpacks/" + modpack_name + ".brm", json2);
                }
                else
                {
                    ConsoleLog("Invalid Modpack");
                }

                Customfunction("UpdateData", modpack_name);

            }
            else
            {
                ConsoleLog("Cancled file import.");
            }

        }

        public void Cache()
        {
            Directory.Delete(Application.StartupPath + "/Mods", true);
            Directory.CreateDirectory(Application.StartupPath + "/Mods");
            Customfunction("window.location.reload", "true");
        }

        public async void AddMod(string modpack_name, string modid, int modversion)
        {
            if (File.Exists(Application.StartupPath + "/Modpacks/" + modpack_name + ".brm"))
            {
                string json = File.ReadAllText(Application.StartupPath + "/Modpacks/" + modpack_name + ".brm");
                var modPack = JsonConvert.DeserializeObject<Modpack>(json);

                Mod mod = new Mod();
                mod.Modid = modid;
                mod.Enable = true;
                mod.ModVersion = modversion;



                modPack.ModList.Add(mod);
                string json2 = JsonConvert.SerializeObject(modPack);
                File.WriteAllText(Application.StartupPath + "/Modpacks/" + modpack_name + ".brm", json2);
            }
            else
            {
                ConsoleLog("Invalid Modpack");
            }
            Customfunction("UpdateData", "");
        }

        public async void RemoveMod(string modpack_name, string modid)
        {
            string path = Application.StartupPath + "/Modpacks/" + modpack_name + ".brm";

            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                var modPack = JsonConvert.DeserializeObject<Modpack>(json);

                var modToRemove = modPack.ModList.FirstOrDefault(m => m.Modid == modid);
                if (modToRemove != null)
                {
                    modPack.ModList.Remove(modToRemove);
                    string updatedJson = JsonConvert.SerializeObject(modPack, Formatting.Indented);
                    File.WriteAllText(path, updatedJson);
                    ConsoleLog($"Removed mod: {modid} from modpack: {modpack_name}");
                }
                else
                {
                    ConsoleLog($"Mod {modid} dosent exist in modpack {modpack_name}");
                }
            }
            else
            {
                ConsoleLog("Invalid Modpack");
            }

            Customfunction("UpdateData", "");
        }

        public async void EnableMod(string modpack_name, string modid)
        {
            string path = Application.StartupPath + "/Modpacks/" + modpack_name + ".brm";

            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                var modPack = JsonConvert.DeserializeObject<Modpack>(json);

                var modToEdit = modPack.ModList.FirstOrDefault(m => m.Modid == modid);
                if (modToEdit != null)
                {
                    modToEdit.Enable = !modToEdit.Enable;
                    string updatedJson = JsonConvert.SerializeObject(modPack, Formatting.Indented);
                    File.WriteAllText(path, updatedJson);
                    ConsoleLog($"Mod {modid} {(modToEdit.Enable ? "on" : "off")} in modpack {modpack_name}");
                }
                else
                {
                    ConsoleLog($"Mod {modid} dosent exist in modpack {modpack_name}");
                }
            }
            else
            {
                ConsoleLog("Invalid Modpack");
            }

            Customfunction("UpdateData", "");
        }


        public async Task LaunchModpack(string modpack, string token, string id, string modsupdate)
        {

            string[] updatemod = modsupdate.Split(',');


            string modpackPath = Path.Combine(Application.StartupPath, "Modpacks", modpack + ".brm");

            if (!File.Exists(modpackPath))
            {
                ConsoleLog("Invalid Modpack");
                return;
            }

            string json = File.ReadAllText(modpackPath);
            var modPack = JsonConvert.DeserializeObject<Modpack>(json);

            string modsDirectory = Path.Combine(Application.StartupPath, "Mods");
            Directory.CreateDirectory(modsDirectory);

            List<Task> downloadTasks = new List<Task>();

            for (var i = 0; i < modPack.ModList.Count; i++) 
            {
                var y = i;

                if (i > 0)
                    y++;

                string zipPath = Path.Combine(modsDirectory, modPack.ModList[i].Modid + ".zip");
                string pakPath = Path.Combine(modsDirectory, modPack.ModList[i].Modid + ".pak");
                if (Int32.Parse(updatemod[i]) == modPack.ModList[i].ModVersion)
                {
                    if (File.Exists(zipPath) || File.Exists(pakPath) || modPack.ModList[i].Modid.StartsWith("LOCAL&"))
                    {
                        ConsoleLog($"Mod {modPack.ModList[i].Modid} already exists, skipping download.");
                        continue;
                    }
                }

                modPack.ModList[i].ModVersion = Int32.Parse(updatemod[i]);



                string tokenweb = WebUtility.UrlEncode(token);
                Uri modUrl = new Uri($"https://service.brmm.ovh/api/mod/download/{tokenweb}/{id}/{modPack.ModList[i].Modid}");
                ConsoleLog("Downloading: " + modUrl);

                downloadTasks.Add(DownloadFileAsync(modUrl, modPack.ModList[i].Modid));
                ConsoleLog($"All download Tasks: {downloadTasks.Count}");
            }
            try
            {

                await Task.WhenAll(downloadTasks);
                ConsoleLog($"Setting Up Mods");

                string json2 = JsonConvert.SerializeObject(modPack);
                File.WriteAllText(modpackPath, json2);
                if (!Directory.Exists(main.GamePath + "/BrickRigs/Mods"))
                    Directory.CreateDirectory(main.GamePath + "/BrickRigs/Mods");
                else
                    Directory.Delete(main.GamePath + "/BrickRigs/Mods", true);
                    Directory.CreateDirectory(main.GamePath + "/BrickRigs/Mods");

                if (!Directory.Exists(main.GamePath + "/BrickRigs/Content/Paks/~mods"))
                    Directory.CreateDirectory(main.GamePath + "/BrickRigs/Content/Paks/~mods");
                else
                    Directory.Delete(main.GamePath + "/BrickRigs/Content/Paks/~mods", true);
                    Directory.CreateDirectory(main.GamePath + "/BrickRigs/Content/Paks/~mods");



                InstallModsInThread(modPack, modsDirectory);
            }
            catch (Exception ex)
            {
                ConsoleLog($"Error while trying to download/setup temp modpack: {ex.Message}");
            }
        }

        public void InstallModsInThread(Modpack modPack, string modsDirectory)
        {
            Thread thread = new Thread(() =>
            {
                foreach (var mod in modPack.ModList)
                {

                    if (mod.Enable)
                    {
                        string zipPath = Path.Combine(modsDirectory, mod.Modid + ".zip");
                        string pakPath = Path.Combine(modsDirectory, mod.Modid + ".pak");

                        string pakTargetPath = Path.Combine(main.GamePath, "BrickRigs/Content/Paks/~mods", mod.Modid + "_P.pak");
                        string modsTargetDir = Path.Combine(main.GamePath, "BrickRigs/Mods");

                        try
                        {
                            if (Directory.Exists(modsTargetDir))
                                Directory.CreateDirectory(Path.GetDirectoryName(modsTargetDir));

                            if (Directory.Exists(pakTargetPath))
                                Directory.CreateDirectory(Path.GetDirectoryName(pakTargetPath));


                            if (File.Exists(pakPath) && !File.Exists(pakTargetPath))
                            {
                                File.Copy(pakPath, pakTargetPath);
                            }

                            if (File.Exists(zipPath))
                            {
                                ZipFile.ExtractToDirectory(zipPath, modsTargetDir);
                            }
                        }
                        catch (Exception ex)
                        {
                            ConsoleLog($"Error while working on mod '{mod.Modid}': {ex.Message}");
                        }
                    }
                }

                main.Invoke((Action)(() => Customfunction("ResetModpack", "")));



                var gameExe = Path.Combine(main.GamePath, "BrickRigs.exe");
                Process.Start(gameExe);

                //main.Invoke((Action)(() => Customfunction("window.location.reload", "true")));
                //Process.Start($"{main.SteamPath}/Steam.exe", "-applaunch 552100");

            });

            thread.IsBackground = true;
            thread.Start();
        }

        public void UpdateRPC(string maint, string status)
        {
            if (main.client == null)
                return;

            main.client.SetPresence(new RichPresence()
            {
                Details = maint,
                State = status,
            });
        }

        public async void CreateModPack(string name, string description, string author,string imge, string banner ,string version)
        {
            ConsoleLog("Creating Modpack");
            ConsoleLog(imge);
            string file_name = "brmm.png";
            file_name = imge;

            if (!imge.EndsWith("brmm.png"))
            {
                string base64Data = imge.Substring(imge.IndexOf(",") + 1);
                
                byte[] imageBytes = Convert.FromBase64String(base64Data);
                
                File.WriteAllBytes(Application.StartupPath + "/Imgs/" + name + ".png", imageBytes);

                file_name = "file:///" + Uri.EscapeUriString(Application.StartupPath.Replace("\\", "/") + "/Imgs/" + name + ".png");
            }



            Modpack modpack = new Modpack();
            modpack.Name = name;
            modpack.Version = version;
            modpack.Imge = file_name;
            modpack.Description = description;
            modpack.Author = author;
            modpack.Banner = banner;
            List<Mod> mods = new List<Mod>();
            modpack.ModList = mods;

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };


            string json = JsonConvert.SerializeObject(modpack);
            File.WriteAllText(Application.StartupPath + "/Modpacks/" + name + ".brm", json);

            GetAllModpacks();
        }
        public async void Setup()
        {
            if (main?.webView?.CoreWebView2 != null)
            {
                try
                {
                    await main.webView.CoreWebView2.ExecuteScriptAsync($@"SetupBrmm()");
                    ConsoleLog(main.SteamPath);
                    ConsoleLog(main.GamePath);
                }
                catch (Exception ex)
                {
                    ConsoleLog($"Error executing script: {ex.Message}");
                }
            }
        }

        public void WindowState(int state)
        {
            switch (state)
            {
                case 0:
                    main.Close();
                    break;
                case 1:

                    main.WindowState = FormWindowState.Minimized;
                    break;
                case 2:
                    main.WindowState = FormWindowState.Maximized;
                    break;
            }
        }

        private async Task DownloadFileAsync(Uri uri, string modid)
        {
            try
            {
                string modsDirectory = Path.Combine(Application.StartupPath, "Mods");
                Directory.CreateDirectory(modsDirectory);

                using (var client = new HttpClient())
                using (var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    string extension = ".pak";
                    if (response.Content.Headers.ContentDisposition != null)
                    {
                        extension = Path.GetExtension(response.Content.Headers.ContentDisposition.FileName.Trim('"'));
                    }

                    string finalFilePath = Path.Combine(modsDirectory, modid + extension);

                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    long totalRead = 0L;

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var fs = new FileStream(finalFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var buffer = new byte[81920];
                        int bytesRead;
                        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fs.WriteAsync(buffer, 0, bytesRead);
                            totalRead += bytesRead;

                            if (totalBytes > 0)
                            {
                                int progress = (int)((totalRead * 100) / totalBytes);
                                Customfunction("managemods", $"{modid} {progress}");
                            }
                            else
                            {
                                Customfunction("managemods", $"{modid} ?");
                            }
                        }
                    }

                    ConsoleLog($"Downloaded: {modid}{extension}");
                }
            }
            catch (Exception ex)
            {
                ConsoleLog($"Error downloading {modid}: {ex.Message}");
            }
        }


    }
}
