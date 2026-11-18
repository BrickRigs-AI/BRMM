using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BRMM
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            InitializeURLProtocol();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Main(args));
        }

        static void InitializeURLProtocol()
        {
            string protocol = "brmm";
            string appPath = Assembly.GetExecutingAssembly().Location;


            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{protocol}"))
                {
                    if (key == null) return;

                    key.SetValue("", "URL:BRMM Protocol");
                    key.SetValue("URL Protocol", "");

                    using (RegistryKey defaultIcon = key.CreateSubKey("DefaultIcon"))
                    {
                        defaultIcon?.SetValue("", $"\"{appPath}\",1");
                    }

                    using (RegistryKey commandKey = key.CreateSubKey(@"shell\open\command"))
                    {
                        commandKey?.SetValue("", $"\"{appPath}\" \"%1\"");
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("Invalid permissions.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
