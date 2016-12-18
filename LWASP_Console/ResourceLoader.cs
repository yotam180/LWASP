using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Net;
using System.IO;

namespace LWASP_Console
{
    class ResourceLoader
    {
        public static string GetFileByURL(HttpListenerContext url)
        {
            try
            {
                string a = url.Request.Url.AbsolutePath.StartsWith("/") ? "web_docs" + url.Request.Url.AbsolutePath : "web_docs/" + url.Request.Url.AbsolutePath;
                FileAttributes attr = File.GetAttributes(a);
                if ((attr & FileAttributes.Directory) == FileAttributes.Directory && !File.Exists(a.EndsWith("/") ? a + "index" + ConfigurationManager.SETTINGS["WEBSCRIPT_EXTENSION"] : a + "/index" + ConfigurationManager.SETTINGS["WEBSCRIPT_EXTENSION"])) return null;
                else if ((attr & FileAttributes.Directory) != FileAttributes.Directory && !File.Exists(a)) return null;
                return (attr & FileAttributes.Directory) == FileAttributes.Directory ? (a.EndsWith("/") ? a + "index" + ConfigurationManager.SETTINGS["WEBSCRIPT_EXTENSION"] : a + "/index" + ConfigurationManager.SETTINGS["WEBSCRIPT_EXTENSION"]) : a;
            }
            catch { return null; }
        }

        public static List<string> DirSearch(string sDir)
        {
            List<string> a = new List<string>();
            try
            {
                foreach (string f in Directory.GetFiles(sDir))
                {
                    a.Add(GetRelativePath(f, Directory.GetCurrentDirectory()));
                }
                foreach (string d in Directory.GetDirectories(sDir))
                {
                    a.AddRange(DirSearch(d));
                }
            }
            catch (System.Exception excpt)
            {
                Console.WriteLine(excpt.Message);
                return new List<string>();
            }
            return a;
        }

        public static string GetRelativePath(string filespec, string folder)
        {
            Uri pathUri = new Uri(filespec);
            // Folders must end in a slash
            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                folder += Path.DirectorySeparatorChar;
            }
            Uri folderUri = new Uri(folder);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
