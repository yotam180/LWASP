using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;

namespace LWASP_Console
{
    static class Utils
    {
        public static string GetRequest(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            return new StreamReader(response.GetResponseStream()).ReadToEnd();
        }

        public static void AutoUpdate()
        {
            new Thread(() =>
            {
                try
                {
                    string UpdatedVersion = GetRequest("http://presentit.co.il/LWASP2/VERSION.txt");
                    if (LWASP.VERSION != UpdatedVersion)
                    {
                        DialogResult update = MessageBox.Show("There's a new version of LWASP available! (version " + UpdatedVersion + ")\r\nWould you like to install it?", "Auto Update!", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                        if (update == DialogResult.Yes)
                        {
                            ProcessStartInfo inf = new ProcessStartInfo();
                            inf.FileName = @"cmd";
                            inf.Arguments = "/C start autoupdater";
                            Process.Start(inf);
                            Environment.Exit(0);
                        }
                    }
                }
                catch (Exception e)
                {
                    LWASP.MainForm.textBox6.BeginInvoke(new Action(() =>
                    {
                        LWASP.MainForm.textBox6.Text += "[LWASP] Could not autoupdate or check for new versions! Error message: " + e + "\r\n";
                    }));
                }
            }).Start();
        }

        public static void GetChangelog()
        {
            new Thread(() =>
            {
                    LWASP.MainForm.textBox6.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            LWASP.MainForm.textBox6.Text = GetRequest("http://presentit.co.il/LWASP2/CHANGELOG_" + LWASP.VERSION + ".txt").Replace("\n", "\r\n");
                        }
                        catch { }
                    }));
            }).Start();
        }

        public static string HandleIDE(HttpListenerRequest req)
        {
            if (req.RawUrl == "/status")
            {
                List<string> files = ResourceLoader.DirSearch(Directory.GetCurrentDirectory());
                string response = "{\"files\": [";
                for (int i = 0; i < files.Count; i++)
                    response += (i == 0 ? "" : ", ") + "\"" + files[i].Replace("\\", "\\\\") + "\"";
                response += "]}";
                return response;
            }
            else if (req.RawUrl.StartsWith("/get_file"))
            {
                try
                {
                    return File.ReadAllText(Directory.GetCurrentDirectory() + req.RawUrl.Replace("/get_file", ""));
                }
                catch (Exception e) { return "Error: " + e.Message; }
            }
            else if (req.RawUrl.StartsWith("/save_file"))
            {
                try
                {
                    File.WriteAllText(Directory.GetCurrentDirectory() + req.RawUrl.Replace("/save_file", ""), new StreamReader(req.InputStream).ReadToEnd());
                    return "success";
                }
                catch (Exception e) { return "Error: " + e.Message; }
            }

            try
            {
                return File.ReadAllText("studio" + (req.RawUrl == "/" ? "/ace.html" : req.RawUrl));
            }
            catch { return "<h1>Error while displaying LWASP Studio</h1>"; }
        }
    }
}
