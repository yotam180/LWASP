using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace LWASP_Console
{
    /// <summary>
    /// Class to load data from settings.config
    /// Loads the data once the server is started.
    /// </summary>
    public class ConfigurationManager
    {
        /// <summary>
        /// Holds the settings in a key-value pairs.
        /// </summary>
        public static Dictionary<string, string> SETTINGS = new Dictionary<string, string>();

        /// <summary>
        /// Loads and parses the settings from the file into the SETTINGS dictionary
        /// </summary>
        public static void LoadSettings()
        {
            try
            {
                string[] s = File.ReadAllText("web_settings/settings.config").Split("\n".ToCharArray());
                foreach (string set in s)
                {
                    if (!set.Contains(":")) continue;
                    SETTINGS[set.Split(new char[] { ':' })[0].Trim()] = set.Split(new char[] { ':' })[1].Trim();
                }
            }
            catch (Exception exx)
            {
                System.Windows.Forms.MessageBox.Show("Error while loading settings. Check that your settings.config file is placed where it should be.\r\nError message: " + exx.Message);
            }
        }

        public static void ExportSettings(Dictionary<string, string> a)
        {
            foreach (string s in a.Keys)
            {
                SETTINGS[s] = a[s]; 
            }
            File.WriteAllText("web_settings/settings.config", GenerateConfig());
        }

        public static string GenerateConfig()
        {
            StringBuilder sb = new StringBuilder(string.Empty);
            foreach (string s in SETTINGS.Keys)
            {
                sb.Append(s);
                sb.Append(": ");
                sb.Append(SETTINGS[s]);
                sb.Append("\n");
            }
            return sb.ToString();
        }
    }
}
