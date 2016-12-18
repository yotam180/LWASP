using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace LWASP_Console
{
    public partial class Form1 : Form
    {
        public Dictionary<string, List<Control>> tabs;
        Thread serverThread;
        SimpleWebServer.WebServer IDEServer;
        bool isActivated = false;
        public Form1()
        { 
            InitializeComponent();
            LWASP.MainForm = this;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            tabs = new Dictionary<string, List<Control>>()
            {
                {
                    "Settings", new List<Control> ()
                    {
                        label2,
                        label3,
                        label4,
                        label5,
                        label6,
                        label7,
                        textBox1,
                        textBox2,
                        textBox3,
                        textBox4, 
                        textBox5,
                        checkBox1
                    }
                },
                {
                    "License Agreement", new List<Control> ()
                    {
                        label8,
                        button4,
                        button5
                    }
                },
                {
                    "Debug Console", new List<Control> ()
                    {
                        richTextBox1
                    }
                },
                {
                    "Changelog", new List<Control>()
                    {
                        textBox6
                    }
                }
            };
            Utils.AutoUpdate();
            Utils.GetChangelog();
            PassToTab(null, null);
            Text = "LWASP " + LWASP.VERSION;
            LoadConfig();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DumpConfig();
        }

        private void DumpConfig()
        {
            try
            {
                ConfigurationManager.ExportSettings(new Dictionary<string, string>()
            {
                { "HTTP_PORT", textBox1.Text },
                { "CODE_SNIPPET_CHAR", textBox2.Text },
                { "REGEX_URL", checkBox1.Checked.ToString() },
                { "PRINT_FUNCTION", textBox3.Text },
                { "WEBSCRIPT_EXTENSION", textBox4.Text },
                { "ASSEMBLIES", textBox5.Text }
            });
                LoadConfig();
                MessageBox.Show("Settings successfully saved!");
            }
            catch
            {
                MessageBox.Show("Problem occured while saving settings.", "Error from LWASP", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadConfig ()
        {
            ConfigurationManager.LoadSettings();
            textBox1.Text = ConfigurationManager.SETTINGS["HTTP_PORT"];
            textBox2.Text = ConfigurationManager.SETTINGS["CODE_SNIPPET_CHAR"];
            checkBox1.Checked = Boolean.Parse(ConfigurationManager.SETTINGS["REGEX_URL"]);
            textBox3.Text = ConfigurationManager.SETTINGS["PRINT_FUNCTION"];
            textBox4.Text = ConfigurationManager.SETTINGS["WEBSCRIPT_EXTENSION"];
            textBox5.Text = ConfigurationManager.SETTINGS["ASSEMBLIES"];
        }

        private void button3_Click(object sender, EventArgs e)
        { 
            LoadConfig();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (!isActivated)
            {
                Thread wsThread = new Thread(WebScriptManager.RunExecutionQueue);
                wsThread.IsBackground = true;
                wsThread.Start();
                Thread servThread = new Thread(() =>
                {
                    HttpExchange.Run();
                });
                servThread.IsBackground = true;
                servThread.Start();
                serverThread = servThread;
                try
                {
                    IDEServer = new SimpleWebServer.WebServer(Utils.HandleIDE, "http://localhost:" + ConfigurationManager.SETTINGS["IDE_PORT"] + "/");
                    IDEServer.Run();
                }
                catch { MessageBox.Show("Failed to initialize IDE server!"); }
                isActivated = true;
                button2.Text = "Stop server";
            }
            else
            {
                try
                {
                    IDEServer.Stop();
                }
                catch { MessageBox.Show("Failed to stop IDE server!"); }
                WebScriptManager.StopExecutionQueue();
                try
                {
                    HttpExchange._server.Close();
                    serverThread.Abort();
                    serverThread.Interrupt();
                }
                catch { MessageBox.Show("LWASP was unable to stop the server!\r\nPlease try again or restart LWASP"); }
                isActivated = false;
                button2.Text = "Start server";
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            label8.Text =
@"Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following 
conditions are met:

1. Redistributions of source code must retain the above copyright notice, this list of conditions and the following 
disclaimer.
2. Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following 
disclaimer in the documentation and/or other materials provided with the distribution.
3. The end-user documentation included with the redistribution, if any, must include the following acknowledgment: 
	""This product includes software developed by Salmon Software foundation."" 
Alternately, this acknowledgment may appear in the software itself, if and wherever such third - party acknowledgments normally
appear.
4.The ""LWASP"" and ""Salmon Software foundation."" must not be used to endorse or promote products derived from this software without
prior written permission.For written permission, please contact yotam.salmon @gmail.com
5.Products derived from this software may not be called ""LWASP"" [ex. ""Lightweight WebScript processor,"" ""LWASP,"" or ""LWASP web 
script, ""] nor may ""LWASP"" [ex. the names] appear in their name, without prior written permission of the Salmon Software Foundation.
6.Products derived or created by this software must contain a ""Created by LWASP"" note.You may not remove the note without prior
written permission of the Salmon Software foundation.";
        }

        private void button5_Click(object sender, EventArgs e)
        {
            label8.Text =
@"THIS SOFTWARE IS PROVIDED ""AS IS"" AND ANY EXPRESSED OR IMPLIED WARRANTIES, 
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND 
FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE SALMON 
SOFTWARE FOUNDATION OR ITS CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, 
INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, 
OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF 
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING 
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, 
EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.";
        }

        private void PassToTab(object sender, EventArgs e)
        {
            foreach (string s in tabs.Keys)
                foreach (Control c in tabs[s])
                    if (s == tabControl1.SelectedTab.Text)
                        c.Visible = true;
                    else
                        c.Visible = false;
        }

        private void frmResize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                notifyIcon1.Visible = true;
                notifyIcon1.BalloonTipTitle = "LWASP is still here!";
                notifyIcon1.BalloonTipText = "You can still access your web_docs on your server! :-)";
                notifyIcon1.BalloonTipIcon = ToolTipIcon.Info;
                notifyIcon1.ShowBalloonTip(300);
                Hide();
            }
        }

        private void notifyIcon1_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
            Show();
            WindowState = FormWindowState.Normal;
            //Activate();
            //Focus();
            notifyIcon1.Visible = false;
        }
    }
}
