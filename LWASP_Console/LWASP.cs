using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LWASP_Console
{
    /// <summary>
    /// A class for some services and wrapper APIs
    /// </summary>
    public static class LWASP
    {
        public static Form1 MainForm;
        public static string VERSION = "2.0.3",
                             AUTHOR = "Yotam Salmon",
                             RELEASE_DATE = "10/11/16";

        public static void Write(string s)
        {
            WebScriptManager.currentProcessor.Write(s);
        }

        public static void NextSnippet()
        {
            WebScriptManager.currentProcessor.Next();
        }

        public static void e(string l)
        {
            WebScriptManager.currentProcessor.lastLineRun = l;
        }

        public static void Debug(string s)
        {
            MainForm.richTextBox1.BeginInvoke(new Action(() =>
            {
                MainForm.richTextBox1.Text += "[" + DateTime.Now.ToShortTimeString() + "] >> " + s + "\r\n";
            }));
        }
    }
}
