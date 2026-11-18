using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Diagnostics;
using System.Threading.Tasks;

class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Please provide a ZIP file URL as the first argument!");
            Console.WriteLine("Optional: Provide a target directory as the second argument.");
            Console.WriteLine("Example:");
            Console.WriteLine("  updaterbrmm.exe https://github.com/BrickRigs-AI/BRMM/archive/refs/tags/v0.2.4-alpha.zip C:\\Games\\BRMM");
            return 1;
        }

        string url = args[0];

        string updaterDir = AppDomain.CurrentDomain.BaseDirectory;
        string rootDir = args[1];

        if (args.Length > 1)
        {
            rootDir = Path.GetFullPath(args[1]);
        }

        string tempZip = Path.Combine(Path.GetTempPath(), "update.zip");
        string tempExtract = Path.Combine(Path.GetTempPath(), "update_extract");

        Console.WriteLine("Downloading file from: " + url);
        Console.WriteLine("Target directory: " + rootDir);

        try
        {
            using (HttpClient client = new HttpClient())
            {
                var data = await client.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync(tempZip, data);
            }

            Console.WriteLine("ZIP file downloaded.");

            if (Directory.Exists(tempExtract))
                Directory.Delete(tempExtract, true);

            ZipFile.ExtractToDirectory(tempZip, tempExtract);
            Console.WriteLine("Extracted to: " + tempExtract);

            foreach (string file in Directory.GetFiles(tempExtract, "*", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(tempExtract, file);
                string destPath = Path.Combine(rootDir, relativePath);

                Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
                File.Copy(file, destPath, true);
                Console.WriteLine("Updated: " + relativePath);
            }

            Console.WriteLine("Update completed.");

            string exePath = Path.Combine(rootDir, "brmm.exe");
            if (File.Exists(exePath))
            {
                Console.WriteLine("Launching brmm.exe...");
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = rootDir
                });
            }
            else
            {
                Console.WriteLine("Could not find brmm.exe in " + rootDir);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error during update: " + ex.Message);
            return 1;
        }
        finally
        {
            if (File.Exists(tempZip)) File.Delete(tempZip);
            if (Directory.Exists(tempExtract)) Directory.Delete(tempExtract, true);
        }

        return 0;
    }
}
