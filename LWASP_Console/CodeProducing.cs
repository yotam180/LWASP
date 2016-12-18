using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Reflection;
using System.CodeDom.Compiler;
using System.IO;
using CSHARP = Microsoft.CSharp;
namespace LWASP_Console
{
    /// <summary>
    /// Provides a collection of static methods for webscript extraction and processing.
    /// </summary>
    public static class CodeProcessor
    {
        static string sChar = "@";
        /// <summary>
        /// Replaces the first occurance of the pattern in the string
        /// </summary>
        /// <param name="src">The source string</param>
        /// <param name="ptrn">The string to be replaced</param>
        /// <param name="dst">The string to replace ptrn</param>
        /// <returns>The newly created string</returns>
        public static string ReplaceFirst(this string src, string ptrn, string dst)
        {
            return new Regex(Regex.Escape(ptrn)).Replace(src, dst, 1);
        }

        /// <summary>
        /// This function extracts code segments wrapped in @ from the code
        /// </summary>
        /// <returns>List of code segments (strings)</returns>
        public static List<string> ExtractCode(ref string code)
        {
            sChar = ConfigurationManager.SETTINGS["CODE_SNIPPET_CHAR"];
            List<string> cs = new List<string>();
            Regex r = new Regex(string.Format(@"<{0}(.+?){0}>", Regex.Escape(sChar)), RegexOptions.Singleline | RegexOptions.IgnoreCase);
            Match m = r.Match(code);
            int matchNum = 0;
            while (m.Success)
            {
                cs.Add(m.Value.Substring(2, m.Value.Length - 4).Trim());
                code = code.ReplaceFirst(m.Value, m.Value.StartsWith("<" + sChar + sChar) ? "" : "{LWASP_CODE_SEGMENT_" + matchNum + "_}");
                if (!m.Value.StartsWith("<" + sChar + sChar)) matchNum++;
                m = m.NextMatch();
            }
            return cs;
        }

        /// <summary>
        /// Provides an enum for triple @@@ double @@ and interpreted @@ blocks
        /// </summary>
        public enum BlockType
        {
            INTERPRETED_BLOCK,
            DOUBLE_BLOCK,
            TRIPLE_BLOCK
        }

        /// <summary>
        /// Sorts code segments to different block types (filters specific block type)
        /// </summary>
        /// <param name="codeSegments">List of code segments (strings)</param>
        /// <param name="type">The type of blocks to return</param>
        /// <returns>The code segments of that specific block type</returns>
        public static List<string> SortCode(List<string> codeSegments, BlockType type)
        {
            List<string> code = new List<string>();
            foreach (string s in codeSegments)
            {
                if (type == BlockType.TRIPLE_BLOCK && s.StartsWith(sChar + sChar) && !s.StartsWith(sChar + sChar + sChar))
                {
                    code.Add(s.Substring(2));
                }
                else if (type == BlockType.DOUBLE_BLOCK && s.StartsWith(sChar) && !s.StartsWith(sChar + sChar))
                {
                    code.Add(s.Substring(1));
                }
                else if (type == BlockType.INTERPRETED_BLOCK && !s.StartsWith(sChar))
                {
                    code.Add(s);
                }
            }
            return code;
        }

        /// <summary>
        /// Generates c# compile-ready code from web-script segments
        /// </summary>
        /// <param name="codeSegments">List of code segments</param>
        /// <returns></returns>
        public static string GenerateCode(List<string> codeSegments, out int segN)
        {
            List<string> triples = SortCode(codeSegments, BlockType.TRIPLE_BLOCK);
            List<string> doubles = SortCode(codeSegments, BlockType.DOUBLE_BLOCK);
            List<string> interpreted = SortCode(codeSegments, BlockType.INTERPRETED_BLOCK);
            segN = interpreted.Count;
            StringBuilder b = new StringBuilder();
            b.Append("using System;\nusing System.Collections.Generic;\nusing System.Text;\nusing System.Linq;\nusing System.IO;\nusing LWASP_Console;\n");
            foreach (string s in triples)
            {
                b.Append(s);
                b.Append("\n");
            }
            b.Append(
@"namespace LWASP_Console {
    class Console {
        public static void Write(string s) {
            LWASP.Write(s);
        }
        public static void WriteLine(string s) {
            Write(s + ""<br/>"");
        }
    }

    class WebApp : LwaspMagic {
        /*static HttpExchange _CONNECTION;
        static Dictionary<string, string> _GET, _POST;
        static Stream _POSTDATA;*/
public static void " + ConfigurationManager.SETTINGS["PRINT_FUNCTION"] + @"(object s, params object[] values)
{
    LWASP.Write(String.Format(s.ToString(), values));
}
"
                );
            foreach (string s in doubles)
            {
                b.Append(s);
                b.Append("\n\n");
            }
            b.Append(
@"        public static void Rain(HttpExchange _LWASP_C, Dictionary<string, string> _LWASP_G, Dictionary<string, string> _LWASP_P, Stream _LWASP_PD) {
LwaspMagic._GET = _GET = _LWASP_G; LwaspMagic._POST = _POST = _LWASP_P; LwaspMagic._CONNECTION = _CONNECTION = _LWASP_C; LwaspMagic._POSTDATA = _POSTDATA = _LWASP_PD;
"
                );
            foreach (string s in interpreted)
            {
                b.Append("\nLWASP.NextSnippet();\n");
                b.Append(s);
            }
            b.Append("\n        }\n    }\n}");

            return b.ToString();
        }

        /// <summary>
        /// Removes multiline comments /* */ from files to prevent tracing errors
        /// </summary>
        /// <param name="code">The code to uncommentize</param>
        /// <returns>The uncommentized code</returns>
        public static string Uncommentize (string code)
        {
            Regex r = new Regex(@"/\*(.+?)\*/", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            Match m = r.Match(code);
            while (m.Success)
            {
                code = code.ReplaceFirst(m.Value, string.Concat(Enumerable.Repeat("\n", m.Value.ToString().Count(y => y == '\n'))));
                m = m.NextMatch();
            }
            return code;
        }

        /// <summary>
        /// Adds traces to the code lines. Every line is added /*FILE X LINE N*/ suffix
        /// </summary>
        /// <param name="code">The code to be stack-traced</param>
        /// <param name="resource">The file name (X in FILE X LINE N)</param>
        /// <returns>The traced code</returns>
        public static string TraceWebpage(string code, string resource)
        {
            code = Uncommentize(code);
            string csc = ConfigurationManager.SETTINGS["CODE_SNIPPET_CHAR"];
            Regex r = new Regex(@"<" + csc + "(.+?)" + csc + ">", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            Match m = r.Match(code);
            string rcode = code;
            while (m.Success)
            {
                string ac = m.Value.Substring(2, m.Value.Length - 4);
                int sl = code.CountLineBreaks(m.Index) + 1;
                string[] lines = ac.Replace("\r", "").Split(new char[] { '\n' });
                StringBuilder fc = new StringBuilder(string.Empty);
                for (int i = 0; i < lines.Length; i++)
                {
                    lines[i] += string.Format("  /* File {0} Line {1} */", resource, sl + i);
                    fc.Append(lines[i]);
                    fc.Append('\n');
                }
                rcode = rcode.ReplaceFirst(ac.Trim(), fc.ToString().Trim());
                m = m.NextMatch();
            }
            return rcode;
        }

        /// <summary>
        /// Counts the line breaks (\n's) in a string until a given char
        /// </summary>
        /// <param name="s">String to count</param>
        /// <param name="pos">Position of character to count to</param>
        /// <returns>The number of line breaks before the char index</returns>
        public static int CountLineBreaks(this string s, int pos)
        {
            int newlines = 0;
            for (int i = 0; i < pos; i++)
            {
                if (s[i] == '\n') newlines++;
            }
            return newlines;
        }

        /// <summary>
        /// Includes other files in the code, using the include tag 
        /// </summary>
        /// <param name="code">The code to be included</param>
        /// <param name="resourceName">The list of resources, starting from the requested resource and ending with the current</param>
        /// <returns>The final code (without include tags)</returns>
        public static string GenerateInclusions (string code, List<string> resourceName)
        {
            Regex r = new Regex(@"<%include (.+?)%>", RegexOptions.IgnoreCase);
            Match m = r.Match(code);
            while (m.Success)
            {
                string included = m.Value.ReplaceFirst("<%include", "").ReplaceFirst("%>", "").Trim();
                string filePath = "web_docs" + (included.StartsWith("/") ? "" : "/") + included;
                try
                {
                    resourceName.Add(included);
                    code = code.Replace(m.Value, GenerateInclusions(TraceWebpage(File.ReadAllText(filePath), resourceName[resourceName.Count - 1]), resourceName));
                }
                catch
                {
                    code = code.Replace(m.Value, ErrorFormatter.FormatIncludeError(resourceName));
                }
                resourceName.Remove(included);
                m = m.NextMatch();
            }
            return code;
        }

        /// <summary>
        /// Gets the file name and line number of a given line of error
        /// </summary>
        /// <param name="code">Code where error occured</param>
        /// <param name="errorLineNum">The line to get</param>
        /// <returns>The file name and line number</returns>
        public static string GetStackTrace(string code, int errorLineNum)
        {
            string errorLine = code.Split(new char[] { '\n' })[errorLineNum - 1];
            Regex stackTraceRegex = new Regex(@"/\* File (.+?) Line [0-9]{1,} \*/");
            Match m = stackTraceRegex.Match(errorLine);
            if (m.Success)
            {
                return m.Value.Replace("/* ", "").Replace("*/", "");
            }
            return null;
        }
    }

    /// <summary>
    /// Wrapper class that uses CodeProducer. Higher level for web-script interpretation.
    /// </summary>
    public class CodeExecuter
    {
        public HttpExchange connection;
        public string BaseCode, FinalCode;
        public int segmentNum;
        public string lastLineRun = "";
        public string[] segmentsOutput;

        public CodeExecuter(HttpExchange conn, string code = "")
        {
            connection = conn;
            BaseCode = code;
        }

        /// <summary>
        /// Given a HttpExchange, it processes the requested resource based on the HTTP parameters
        /// </summary>
        /// <param name="ex">Exception, if something goes wrong</param>
        /// <returns>True if processing was successful, otherwise false and out Exception ex</returns>
        public bool Process(out Exception ex)
        {
            ex = null;
            try
            {
                BaseCode = CodeProcessor.TraceWebpage(BaseCode, connection.requestedResource.Replace("web_docs/", ""));
                BaseCode = CodeProcessor.GenerateInclusions(BaseCode, new List<string>() { connection.requestedResource.Replace("web_docs/", "") });
                List<string> codeSegments = CodeProcessor.ExtractCode(ref BaseCode);
                FinalCode = CodeProcessor.GenerateCode(codeSegments, out segmentNum).Replace("\n", "\n");
                return true;
            }
            catch (Exception exx)
            {
                ex = exx;
                return false;
            }
        }

        /// <summary>
        /// Executes the processed code from Process() function.
        /// </summary>
        /// <param name="errors">If execution was not successful, returns CompilerErrorCollection or Exception</param>
        /// <returns>Values to put in each code segment (output) or null if there were errors</returns>
        public string[] Execute(out object errors)
        {
            errors = null;
            segmentsOutput = new string[segmentNum];
            for (int i = 0; i < segmentNum; i++)
                segmentsOutput[i] = "";
            segmentNum = -1;
            CSHARP.CSharpCodeProvider provider = new CSHARP.CSharpCodeProvider();
            

            CompilerParameters parms = new CompilerParameters();
            parms.GenerateInMemory = true;
            parms.TreatWarningsAsErrors = false;
            parms.TempFiles = new TempFileCollection(Environment.GetEnvironmentVariable("TEMP"), true);
            parms.IncludeDebugInformation = true;
            parms.ReferencedAssemblies.Add(Assembly.GetEntryAssembly().Location);
            try
            {
                string[] assms = File.ReadAllText(ConfigurationManager.SETTINGS["ASSEMBLIES"]).Split(new char[] { '\n' });
                for (int i = 0; i < assms.Length; i++)
                {
                    parms.ReferencedAssemblies.Add(assms[i].Trim());
                }
            }
            catch { }

            CompilerResults res = provider.CompileAssemblyFromSource(parms, FinalCode);
            if (res.Errors.HasErrors) // Handling compiler errors
            {
                errors = res.Errors;
                return null;
            }

            Assembly ass = res.CompiledAssembly;
            Type prog = ass.GetType("LWASP_Console.WebApp");
            MethodInfo main = prog.GetMethod("Rain");

            try
            {
                main.Invoke(null, new object[] { connection, connection.queryString, connection.formData, connection.PostData() });
            }
            catch (Exception exx)
            {
                errors = exx != null ? exx.InnerException ?? exx : new Exception("LWASP Error");
                return null;
            }

            return segmentsOutput;
        }
        
        /// <summary>
        /// The core function of echo() and LWASP.Write(). Just writes into the current snippet
        /// </summary>
        /// <param name="s">String to write</param>
        public void Write(string s)
        {
            segmentsOutput[segmentNum] += s;
        }

        /// <summary>
        /// The core function that tells LWASP that the next statements belong to the next code segment.
        /// Basically separates between segments
        /// </summary>
        public void Next()
        {
            segmentNum++;
        }

    }
}
