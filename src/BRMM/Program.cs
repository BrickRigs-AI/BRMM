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
        /// <summary>
        /// Główny punkt wejścia dla aplikacji.
        /// </summary>
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


            //req fixing small exploits

            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{protocol}"))
                {
                    key.SetValue("", "URL:BRMM Protocol");
                    key.SetValue("URL Protocol", "");

                    using (RegistryKey defaultIcon = key.CreateSubKey("DefaultIcon"))
                    {
                        defaultIcon.SetValue("", $"\"{appPath}\",1");
                    }

                    using (RegistryKey commandKey = key.CreateSubKey(@"shell\open\command"))
                    {
                        commandKey.SetValue("", $"\"{appPath}\" \"%1\"");
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }
    }
}
