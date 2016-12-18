using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Diagnostics;
using System.Security.Principal;
using System.Threading;
using SimpleWebServer;
using System.Windows.Forms;
using System.IO;

namespace LWASP_Console
{
    public /*Is public really neccesary here???*/ class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            /*WebServer server = null;
            try
            {
                //if (!IsAdmin()) RestartAsAdmin();
                Thread wsThread = new Thread(WebScriptManager.RunExecutionQueue);
                wsThread.IsBackground = true;
                wsThread.Start();
                ConfigurationManager.LoadSettings();
                server = new WebServer(HandleIDE, "http://localhost:" + ConfigurationManager.SETTINGS["IDE_PORT"] + "/");
                server.Run();
                HttpExchange.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured! " + ex.Message);
                Console.ReadLine();
            }
            finally
            {
                HttpExchange.Stop();
                if (server != null) server.Stop();
            }*/
            //if (!IsAdmin()) RestartAsAdmin();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        static bool IsAdmin()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        static void RestartAsAdmin()
        {
            try
            {
                string fN = File.Exists("LWASP.exe") ? "LWASP.exe" : "LWASP_Console.exe";
                var startInfo = new ProcessStartInfo(fN) { Verb = "runas" };
                Process.Start(startInfo);
            }
            catch { }
            finally
            {
                Environment.Exit(0);
            }
        }
    }
}
