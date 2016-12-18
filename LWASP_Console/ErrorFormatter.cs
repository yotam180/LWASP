using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.CodeDom.Compiler;
using System.Text.RegularExpressions;

namespace LWASP_Console
{
    /// <summary>
    /// A helper class to format Compiler, Inclusion, Runtime and Server Errors to the client (or the developer, actually)
    /// </summary>
    public static class ErrorFormatter
    {
        /// <summary>
        /// Formats a Runtime Error
        /// </summary>
        /// <param name="exx">The Exception object</param>
        /// <param name="code">The compiled code that caused the exception</param>
        /// <returns>A formatted Exception String</returns>
        public static string FormatException (Exception exx, string code)
        {
            string err = "<b><span style='color: red'>Runtime Error:</span></b><br/><pre>";
            int[] lineNums = exx.LineNumber();
            for (int i = 0; i < lineNums.Length; i++)
            {
                err += "At <i>" + CodeProcessor.GetStackTrace(code, lineNums[i]) + "</i>\n    <b>" + new Regex(@"/\* File (.+?) Line [0-9]{1,} \*/").Replace(code.Split(new char[] { '\n' })[lineNums[i] - 1].Trim(), "").Replace("<", "&lt;") + "</b>\n";
            }
            err += "<br/><u>" + exx.GetType().ToString() + ":</u> " + exx.Message + "</pre><br/><b>Raw Exception:</b> <br/><pre>" + exx.ToString() + "</pre>";
            return err;
        }

        /// <summary>
        /// Formats a Compilation Error
        /// </summary>
        /// <param name="err">The compiler error</param>
        /// <param name="code">The code that caused the compilation error</param>
        /// <returns>A formatted Error String</returns>
        public static string FormatCompilerError (CompilerError err, string code)
        {
            return "<b><span style='color: " + (err.IsWarning ? "GoldenRod" : "red") + ";'>Compilation " + (err.IsWarning ? "Warning" : "Error") + " " + err.ErrorNumber + "</span></b><pre style='white-space: pre-wrap; word-wrap: break-word;'>at <i>" + (CodeProcessor.GetStackTrace(code, err.Line) ?? "Generated code") + "</i>: \n    <b>" + new Regex(@"/\* File (.+?) Line [0-9]{1,} \*/").Replace(code.Split(new char[] { '\n' })[err.Line - 1].Trim(), "").Replace("<", "&lt;") + " </b>\n<u>Info:</u> " + err.ErrorText + "</pre>";
        }

        /// <summary>
        /// Formats and Inclusion error (caused when an included page is not found)
        /// </summary>
        /// <param name="resourceName">The resource name that was not found (I think)</param>
        /// <returns>A formatted Inclusion Error</returns>
        public static string FormatIncludeError (List<string> resourceName)
        {
            string s = "<b>Inclusion Error:</b> Error including " + resourceName[resourceName.Count - 1] + " - the resource was not found.<pre>";
            for (int i = resourceName.Count - 2; i >= 0; i--)
            {
                s += "\n   in " + resourceName[i];
            }
            return s + "</pre>";
        }

        /// <summary>
        /// Formats a server error. This is usually caused by bugs in the server :P OR exit status codes
        /// </summary>
        /// <param name="errorCode"></param>
        /// <param name="errorName"></param>
        /// <param name="errorDescription"></param>
        /// <returns></returns>
        public static string FormatServerError(int errorCode, string errorName, string errorDescription)
        {
            return string.Format("<h1>{0} {1}</h1>{2}", errorCode, errorName, errorDescription);
        }

        /// <summary>
        /// Gets the line number(s) from an Exception
        /// </summary>
        /// <param name="e">The exception to look for line number of</param>
        /// <returns>The line number(s)</returns>
        public static int[] LineNumber(this Exception e)
        {
            List<int> lineNums = new List<int>();
            try
            {
                foreach (string line in e.StackTrace.Split("\n".ToCharArray()))
                {
                    lineNums.Add(Convert.ToInt32(line.Substring(line.LastIndexOf(' '))));
                }

                //For Localized Visual Studio ... In other languages stack trace  doesn't end with ":Line 12"
                //linenum = Convert.ToInt32(e.StackTrace.Substring(e.StackTrace.LastIndexOf(' ')));

            }


            catch
            {
                //Stack trace is not available!
            }
            return lineNums.ToArray();
        }
    }
}
