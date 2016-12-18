using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.CodeDom.Compiler;
using System.Threading;

namespace LWASP_Console
{
    public class WebScriptManager
    {
        public static bool WORK = false, WORKING = false;
        public static CodeExecuter currentProcessor = null;
        static ConcurrentQueue<HttpExchange> ExecutionQueue;
        public static void RunExecutionQueue()
        {
            WORKING = true;
            WORK = true;
            ExecutionQueue = new ConcurrentQueue<HttpExchange>();
            while (WORK)
            {
                while (ExecutionQueue.Count > 0)
                {
                    HttpExchange next;
                    while (!ExecutionQueue.TryDequeue(out next)) ;
                    Execute(next);
                }
            }
            WORKING = false;
        }

        public static void StopExecutionQueue()
        {
            WORK = false;
            while (WORKING) Thread.Sleep(10);
        }

        public static void Enqueue(HttpExchange hx)
        {
            ExecutionQueue.Enqueue(hx);
        }

        public static void Execute(HttpExchange ex)
        {
            string rawCode = File.ReadAllText(ex.requestedResource);
            CodeExecuter ce = new CodeExecuter(ex, rawCode);
            currentProcessor = ce;
            Exception e;
            if (!ce.Process(out e))
            {
                ex.ProcessResponse(ErrorFormatter.FormatServerError(500, "Internal Server Error", "The LWASP Server had a problem while processing " + ex.requestedResource + ".<br/>Message: " + e.Message));
            }
            else
            {
                object o;
                string[] output = ce.Execute(out o);
                if (output == null) // TODO complete this code
                {
                    if (o is CompilerErrorCollection) // Compilation error
                    {
                        string errorCollection = "";
                        foreach (CompilerError er in (CompilerErrorCollection)o)
                        {
                            errorCollection += ErrorFormatter.FormatCompilerError(er, ce.FinalCode) + "<br/>";
                        }
                        ex.ProcessResponse(errorCollection);
                    }
                    else if (o is Exception) // Runtime error
                    {
                        if (((Exception)o).Message == "LWASP Error")
                        {
                            ex.oWrite("<h1>500 Internal Server Error</h1><br/>Server error message was: " + e.Message);
                            ex.oClose();
                        }
                        else
                            ex.ProcessResponse(ErrorFormatter.FormatException((Exception)o, ce.FinalCode));
                    }
                }
                else
                {
                    string fCode = ce.BaseCode;
                    for (int i = 0; i < output.Length; i++)
                    {
                        fCode = fCode.ReplaceFirst("{LWASP_CODE_SEGMENT_" + i + "_}", output[i]);
                    }
                    currentProcessor.connection.ProcessResponse(fCode.Trim());
                }
            }
        }
    }
}
