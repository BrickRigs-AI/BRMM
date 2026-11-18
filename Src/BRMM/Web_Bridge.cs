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
        public int version { get; set; }
        public int ModVersion { get; set; } = 0;
        public bool Enable { get; set; }
    }

    public class Mod_Cache
    {
        public string Modid { get; set; }
        public string Mod_filename { get; set; }
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
                        string jsonParam = JsonConvert.SerializeObject(text ?? "");
                        await main.webView.CoreWebView2.ExecuteScriptAsync($"{function}({jsonParam})");
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
            main.id_gl = "";
            await main.Getdata();
            ConsoleLog("huj");
            Customfunction("console.log", token);
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

        public void AddModToCache(string modid, string filename)
        {
            string cacheFilePath = Path.Combine(Application.StartupPath, "mods.config");
            List<Mod_Cache> modCacheList;
            if (File.Exists(cacheFilePath))
            {
                string json = File.ReadAllText(cacheFilePath);
                modCacheList = JsonConvert.DeserializeObject<List<Mod_Cache>>(json) ?? new List<Mod_Cache>();
            }
            else
            {
                modCacheList = new List<Mod_Cache>();
            }
            var existingMod = modCacheList.FirstOrDefault(m => m.Modid == modid);
            if (existingMod != null)
            {
                existingMod.Mod_filename = filename;
            }
            else
            {
                modCacheList.Add(new Mod_Cache { Modid = modid, Mod_filename = filename });
            }
            string updatedJson = JsonConvert.SerializeObject(modCacheList, Formatting.Indented);
            File.WriteAllText(cacheFilePath, updatedJson);
        }


        public void CheckModsCache()
        {
            string cacheFilePath = Path.Combine(
                Application.StartupPath,
                "mods.config");

            if (!File.Exists(cacheFilePath))
                return;

            try
            {
                string json = File.ReadAllText(cacheFilePath);

                List<Mod_Cache> modCacheList =
                    JsonConvert.DeserializeObject<List<Mod_Cache>>(json)
                    ?? new List<Mod_Cache>();

                int removedCount = 0;

                modCacheList.RemoveAll(mod =>
                {
                    if (string.IsNullOrWhiteSpace(mod.Mod_filename))
                        return true;

                    bool exists =
                        File.Exists(mod.Mod_filename) ||
                        Directory.Exists(mod.Mod_filename);

                    if (!exists)
                    {
                        removedCount++;
                        return true;
                    }

                    return false;
                });

                if (removedCount > 0)
                {
                    string updatedJson = JsonConvert.SerializeObject(
                        modCacheList,
                        Formatting.Indented);

                    File.WriteAllText(
                        cacheFilePath,
                        updatedJson);

                    ConsoleLog(
                        $"Removed {removedCount} missing mods from cache.");
                }
            }
            catch (Exception ex)
            {
                ConsoleLog(
                    $"Error while checking mods cache: {ex.Message}");
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
            string url = "https://exp.brmm.ovh/quicklogin";
            System.Diagnostics.Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
            main.Close();
        }


        public async Task DownloadModpackAsync(string token, string modpackId)
        {
            string tokenweb = WebUtility.UrlEncode(token);

            string url =
                $"https://exp.brmm.ovh/api/download/{modpackId}?version=0&token={tokenweb}";
            var httpClient = new HttpClient();

            var response = await httpClient.GetAsync(
                url,
                HttpCompletionOption.ResponseHeadersRead
            );

            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                ConsoleLog(error);
                return;
            }

            string modpackDirectory = Path.Combine(
                Application.StartupPath,
                "Modpacks"
            );

            Directory.CreateDirectory(modpackDirectory);

            string finalFilePath = Path.Combine(
                modpackDirectory,
                $"{modpackId}.brm"
            );

            using (var contentStream = await response.Content.ReadAsStreamAsync())
            using (var fileStream = new FileStream(
                finalFilePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                81920,
                true))
            {
                await contentStream.CopyToAsync(fileStream);
            }

            string json = File.ReadAllText(finalFilePath);

            Modpack modpack = JsonConvert.DeserializeObject<Modpack>(json);

            if (modpack == null || string.IsNullOrWhiteSpace(modpack.Name))
            {
                return;
            }

            string newPath = Path.Combine(
                modpackDirectory,
                $"{modpack.Name}.brm"
            );

            if (File.Exists(newPath))
                File.Delete(newPath);

            if (!string.Equals(finalFilePath, newPath,
                StringComparison.OrdinalIgnoreCase))
            {
                File.Move(finalFilePath, newPath);
            }

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
            string url = "https://github.com/BrickRigs-AI/BRMM/releases/download/v0.4.6-exp-alpha/Updater.zip";
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
                    mod.version = 0;

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

        public async void AddMod(string modpack_name, string modid, int modversion, int beta)
        {
            if (File.Exists(Application.StartupPath + "/Modpacks/" + modpack_name + ".brm"))
            {
                string json = File.ReadAllText(Application.StartupPath + "/Modpacks/" + modpack_name + ".brm");
                var modPack = JsonConvert.DeserializeObject<Modpack>(json);

                Mod mod = new Mod();
                mod.Modid = modid;
                mod.Enable = true;
                mod.ModVersion = modversion;
                mod.version = beta;

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

        public void RemoveModFromCache(string modid)
        {
            string cacheFilePath = Path.Combine(Application.StartupPath, "mods.config");
            if (!File.Exists(cacheFilePath)) return;
            string json = File.ReadAllText(cacheFilePath);
            List<Mod_Cache> modCacheList = JsonConvert.DeserializeObject<List<Mod_Cache>>(json) ?? new List<Mod_Cache>();
            var modToRemove = modCacheList.FirstOrDefault(m => m.Modid == modid);
            if (modToRemove != null)
            {
                modCacheList.Remove(modToRemove);
                string updatedJson = JsonConvert.SerializeObject(modCacheList, Formatting.Indented);
                File.WriteAllText(cacheFilePath, updatedJson);
            }
        }

        public void RemoveInstalledMod(string modid)
        {
            string cacheFilePath = Path.Combine(
                Application.StartupPath,
                "mods.config");

            if (!File.Exists(cacheFilePath))
                return;

            try
            {
                string json = File.ReadAllText(cacheFilePath);

                List<Mod_Cache> cache =
                    JsonConvert.DeserializeObject<List<Mod_Cache>>(json)
                    ?? new List<Mod_Cache>();

                var cachedMod = cache.FirstOrDefault(
                    m => m.Modid == modid);

                if (cachedMod != null &&
                    !string.IsNullOrWhiteSpace(cachedMod.Mod_filename))
                {
                    string path = cachedMod.Mod_filename;

                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                    else if (Directory.Exists(path))
                    {
                        Directory.Delete(path, true);
                    }

                    RemoveModFromCache(modid);
                }

                string pakFolder = Path.Combine(
                    main.GamePath,
                    "BrickRigs/Content/Paks/~mods");

                if (Directory.Exists(pakFolder))
                {
                    string pakPath = Path.Combine(
                        pakFolder,
                        modid + ".pak");

                    string disabledPakPath = pakPath + "_disable";

                    if (File.Exists(pakPath))
                        File.Delete(pakPath);

                    if (File.Exists(disabledPakPath))
                        File.Delete(disabledPakPath);
                }
            }
            catch (Exception ex)
            {
                ConsoleLog(
                    $"Error removing mod '{modid}': {ex.Message}");
            }
        }


        public async Task LaunchModpack(
            string modpack,
            string token,
            string id,
            string modsupdate)
        {
            try
            {
                string modpackPath = Path.Combine(
                    Application.StartupPath,
                    "Modpacks",
                    modpack + ".brm");

                if (!File.Exists(modpackPath))
                {
                    ConsoleLog("Invalid Modpack");
                    return;
                }

                string json = File.ReadAllText(modpackPath);

                var modPack = JsonConvert.DeserializeObject<Modpack>(json);

                if (modPack == null || modPack.ModList == null)
                {
                    ConsoleLog("Invalid Modpack data");
                    return;
                }

                string[] updateMod = modsupdate.Split(',');

                if (updateMod.Length != modPack.ModList.Count)
                {
                    ConsoleLog("Invalid mods update data");
                    return;
                }

                string modsDirectory = Path.Combine(
                    Application.StartupPath,
                    "Mods");

                Directory.CreateDirectory(modsDirectory);

                string pakFolder = Path.Combine(
                    main.GamePath,
                    "BrickRigs/Content/Paks/~mods");

                string modsFolder = Path.Combine(
                    main.GamePath,
                    "BrickRigs/Mods");

                Directory.CreateDirectory(pakFolder);
                Directory.CreateDirectory(modsFolder);

                CheckModsCache();

                List<Task> downloadTasks = new List<Task>();

                for (int i = 0; i < modPack.ModList.Count; i++)
                {
                    var mod = modPack.ModList[i];

                    string zipPath = Path.Combine(
                        modsDirectory,
                        mod.Modid + ".zip");

                    string pakPath = Path.Combine(
                        modsDirectory,
                        mod.Modid + ".pak");

                    if (mod.Modid.StartsWith("LOCAL&"))
                    {
                        ConsoleLog($"Local mod {mod.Modid}, skipping download.");
                        continue;
                    }

                    if (!int.TryParse(updateMod[i], out int newVersion))
                    {
                        ConsoleLog($"Invalid version for mod {mod.Modid}");
                        continue;
                    }

                    bool needsUpdate =
                        newVersion != mod.ModVersion ||
                        (!File.Exists(zipPath) && !File.Exists(pakPath));

                    if (!needsUpdate)
                    {
                        ConsoleLog(
                            $"Mod {mod.Modid} already exists, skipping download.");

                        continue;
                    }

                    ConsoleLog($"Updating mod: {mod.Modid}");

                    RemoveInstalledMod(mod.Modid);

                    if (File.Exists(zipPath))
                        File.Delete(zipPath);

                    if (File.Exists(pakPath))
                        File.Delete(pakPath);

                    mod.ModVersion = newVersion;

                    string version = mod.version.ToString();

                    if (string.IsNullOrEmpty(version))
                        version = "1";

                    string tokenWeb = WebUtility.UrlEncode(token);

                    Uri modUrl = new Uri(
                        $"https://exp.brmm.ovh/api/download/{mod.Modid}/?version={version}&token={tokenWeb}");

                    ConsoleLog("Downloading: " + modUrl);

                    downloadTasks.Add(
                        DownloadFileAsync(modUrl, mod.Modid));
                }


                await Task.WhenAll(downloadTasks);

                ConsoleLog("All downloads completed.");
                ConsoleLog("Setting Up Mods");

                string json2 = JsonConvert.SerializeObject(
                    modPack,
                    Formatting.Indented);

                File.WriteAllText(modpackPath, json2);

                DisableAllPakFiles();

                InstallModsInThread(modPack, modsDirectory);
            }
            catch (Exception ex)
            {
                ConsoleLog(
                    $"Error while trying to download/setup temp modpack: {ex.Message}");
            }
        }


        public void DisableAllPakFiles()
        {
            string pakFolder = Path.Combine(
                main.GamePath, "BrickRigs/Content/Paks/~mods");

            string modsFolder = Path.Combine(
                main.GamePath, "BrickRigs/Mods");

            string[] foldersToScan = { pakFolder, modsFolder };

            foreach (string folder in foldersToScan)
            {
                if (!Directory.Exists(folder))
                    continue;

                foreach (string pakFile in Directory.GetFiles(
                    folder, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        if (!pakFile.EndsWith(
                            ".pak", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (pakFile.EndsWith(
                            ".pak_disable", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        string disabledPath = pakFile + "_disable";

                        if (!File.Exists(disabledPath))
                        {
                            File.Move(pakFile, disabledPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        ConsoleLog(
                            $"Error disabling pak '{pakFile}': {ex.Message}");
                    }
                }
            }
        }

        public void EnableModFiles(string modid, string modsFolder, string pakFolder)
        {
            if (Directory.Exists(pakFolder))
            {
                foreach (string disabledPak in Directory.GetFiles(
                    pakFolder,
                    "*.pak_disable",
                    SearchOption.TopDirectoryOnly))
                {
                    string enabledPak = disabledPak.Substring(
                        0,
                        disabledPak.Length - "_disable".Length);

                    if (!File.Exists(enabledPak))
                    {
                        File.Move(disabledPak, enabledPak);
                    }
                }
            }
            string cacheFilePath = Path.Combine(
                Application.StartupPath,
                "mods.config");

            if (!File.Exists(cacheFilePath))
                return;

            string json = File.ReadAllText(cacheFilePath);

            List<Mod_Cache> cache =
                JsonConvert.DeserializeObject<List<Mod_Cache>>(json)
                ?? new List<Mod_Cache>();

            var cachedMod = cache.FirstOrDefault(
                m => m.Modid == modid);

            if (cachedMod == null)
                return;

            string modFolder = cachedMod.Mod_filename;

            if (File.Exists(modFolder))
            {
                return;
            }

            if (!Directory.Exists(modFolder))
                return;

            foreach (string disabledPak in Directory.GetFiles(
                modFolder,
                "*.pak_disable",
                SearchOption.AllDirectories))
            {
                string enabledPak = disabledPak.Substring(
                    0,
                    disabledPak.Length - "_disable".Length);

                if (!File.Exists(enabledPak))
                {
                    File.Move(disabledPak, enabledPak);
                }
            }
        }

        public void InstallModsInThread(Modpack modPack, string modsDirectory)
        {
            Thread thread = new Thread(() =>
            {
                string pakFolder = Path.Combine(
                    main.GamePath, "BrickRigs/Content/Paks/~mods");

                string modsFolder = Path.Combine(
                    main.GamePath, "BrickRigs/Mods");

                try
                {
                    Directory.CreateDirectory(pakFolder);
                    Directory.CreateDirectory(modsFolder);

                    foreach (var mod in modPack.ModList)
                    {
                        if (!mod.Enable)
                            continue;

                        string zipPath = Path.Combine(
                            modsDirectory, mod.Modid + ".zip");

                        string pakPath = Path.Combine(
                            modsDirectory, mod.Modid + ".pak");

                        try
                        {
                            EnableModFiles(mod.Modid, modsFolder, pakFolder);

                            if (File.Exists(pakPath))
                            {
                                string originalPakName =
                                    Path.GetFileName(pakPath);

                                string pakTargetPath = Path.Combine(
                                    pakFolder, originalPakName);

                                string disabledPakTarget =
                                    pakTargetPath + "_disable";

                                if (File.Exists(disabledPakTarget))
                                {
                                    if (!File.Exists(pakTargetPath))
                                    {
                                        File.Move(
                                            disabledPakTarget,
                                            pakTargetPath);
                                    }
                                }

                                if (!File.Exists(pakTargetPath))
                                {
                                    File.Copy(pakPath, pakTargetPath);
                                }

                                if (File.Exists(pakTargetPath))
                                {
                                    AddModToCache(
                                        mod.Modid,
                                        pakTargetPath);
                                }
                            }
                            else
                            {
                                if (File.Exists(zipPath))
                                {
                                    string tempDir = Path.Combine(
                                        Path.GetTempPath(),
                                        Guid.NewGuid().ToString());

                                    Directory.CreateDirectory(tempDir);

                                    try
                                    {
                                        ZipFile.ExtractToDirectory(
                                            zipPath, tempDir);

                                        string[] rootDirs =
                                            Directory.GetDirectories(tempDir);

                                        string[] rootFiles =
                                            Directory.GetFiles(tempDir);

                                        string targetPath;

                                        if (rootDirs.Length == 1 &&
                                            rootFiles.Length == 0)
                                        {
                                            string originalFolderName =
                                                Path.GetFileName(rootDirs[0]);

                                            targetPath = Path.Combine(
                                                modsFolder,
                                                originalFolderName);

                                            if (!Directory.Exists(targetPath))
                                            {
                                                Directory.Move(
                                                    rootDirs[0],
                                                    targetPath);
                                            }
                                        }
                                        else
                                        {
                                            string originalFolderName =
                                                Path.GetFileNameWithoutExtension(
                                                    zipPath);

                                            targetPath = Path.Combine(
                                                modsFolder,
                                                originalFolderName);

                                            if (!Directory.Exists(targetPath))
                                            {
                                                Directory.Move(
                                                    tempDir,
                                                    targetPath);

                                                tempDir = null;
                                            }
                                        }

                                        if (Directory.Exists(targetPath))
                                        {
                                            AddModToCache(
                                                mod.Modid,
                                                targetPath);
                                        }
                                    }
                                    finally
                                    {
                                        if (tempDir != null &&
                                            Directory.Exists(tempDir))
                                        {
                                            Directory.Delete(tempDir, true);
                                        }
                                    }
                                }
                                else
                                {
                                    string existingModFolder = Path.Combine(
                                        modsFolder, mod.Modid);

                                    if (Directory.Exists(existingModFolder))
                                    {
                                        AddModToCache(
                                            mod.Modid,
                                            existingModFolder);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            ConsoleLog(
                                $"Error while working on mod '{mod.Modid}': {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    ConsoleLog(
                        $"Error while preparing mods: {ex.Message}");
                }

                main.Invoke((Action)(() =>
                    Customfunction("ResetModpack", "")));

                Process.Start(
                    Path.Combine(main.SteamPath, "Steam.exe"),
                    "-applaunch 552100");
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
