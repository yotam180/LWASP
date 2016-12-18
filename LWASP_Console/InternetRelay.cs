using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Collections.Specialized;
using System.Threading;
using System.Reflection;
using System.Web;
using System.Text.RegularExpressions;

namespace LWASP_Console
{
    public abstract class Exchange
    {
        public static HttpListener _server;
        public string requestedResource;
        public CookieCollection responseCookies, requestCookies;
        public Dictionary<string, string> queryString, formData, responseHeaders, requestHeaders;
        public Encoding responseEncoding = Encoding.UTF8;
        public string HttpMethod;
        public string HttpVersion;
        public Uri HttpURL;
        public bool HeadersSent { get; protected set; }
        public static bool HTTP_WORKING = false, HTTP_WORK = false;

        public static void Run() { }
        public static void Stop() { }
        public Exchange()
        { }
        public virtual void SealHeaders(string ver = null, int code = 200, string desc = "OK") { }
        public virtual void Write(byte[] b) { }
        public virtual void Redirect(string path) { }
        public virtual void oWrite(string s, Encoding enc = null) { }
        public virtual Dictionary<string, string> QueryString() { return null; }
        public virtual Dictionary<string, string> FormData() { return null; }
        public virtual Stream PostData() { return null; }
        public virtual void oClose() { }
        public virtual void ProcessResponse(string res) { }
    }


    public class HttpExchange : Exchange
    {
        HttpListenerContext context;
        public Stream inputStream;

        public new static void Run()
        {
            if (!HttpListener.IsSupported)
            {
                Console.WriteLine("HttpListener is not supported on this OS");
                return;
            }
            HttpListener server = new HttpListener();
            _server = server;
            bool _a; int _b;

            // Setting up HTTP & HTTPS ports
            if (ConfigurationManager.SETTINGS.ContainsKey("HTTP_PORT") && ConfigurationManager.SETTINGS.ContainsKey("ALLOW_HTTP") &&
                int.TryParse(ConfigurationManager.SETTINGS["HTTP_PORT"], out _b) && bool.TryParse(ConfigurationManager.SETTINGS["ALLOW_HTTP"], out _a) &&
                bool.Parse(ConfigurationManager.SETTINGS["ALLOW_HTTP"]) == true)
            {
                server.Prefixes.Add("http://*:" + ConfigurationManager.SETTINGS["HTTP_PORT"] + "/");
            }
            if (ConfigurationManager.SETTINGS.ContainsKey("HTTPS_PORT") && ConfigurationManager.SETTINGS.ContainsKey("ALLOW_HTTPS") &&
                int.TryParse(ConfigurationManager.SETTINGS["HTTPS_PORT"], out _b) && bool.TryParse(ConfigurationManager.SETTINGS["ALLOW_HTTPS"], out _a) &&
                bool.Parse(ConfigurationManager.SETTINGS["ALLOW_HTTPS"]) == true)
            {
                server.Prefixes.Add("https://*:" + ConfigurationManager.SETTINGS["HTTPS_PORT"] + "/");
            }

            server.Start();
            HTTP_WORKING = true;
            HTTP_WORK = true;
            while (HTTP_WORK)
            {
                //var asyncres = server.BeginGetContext(StartRequestProcessing, server);
                //asyncres.AsyncWaitHandle.WaitOne(1000, true);
                try
                {
                    HttpListenerContext ctx = server.GetContext();
                    new HttpExchange(ctx).ProcessRequest();
                }
                catch { }
            }
            HTTP_WORKING = false;
        }

        public static void StartRequestProcessing(IAsyncResult res)
        {
            if (res.AsyncState is HttpListener)
            {
                HttpListenerContext ctx = ((HttpListener)res.AsyncState).GetContext();
                new HttpExchange(ctx).ProcessRequest();
            }
        }

        public new static void Stop()
        {
            
            HTTP_WORK = false;
            while (HTTP_WORKING) Thread.Sleep(10);
            return;
        }

        public HttpExchange(HttpListenerContext ctx)
        {
            HeadersSent = false;
            context = ctx;
        }

        public override void Redirect(string path)
        {
            responseHeaders["Location"] = path;
            SealHeaders(null, 302, "Found");
            oClose();
        }

        public override void SealHeaders(string ver = null, int code = 200, string desc = "OK")
        {
            if (HeadersSent) return;
            HeadersSent = true;
            if (ver != null) context.Response.ProtocolVersion = new Version(ver);
            context.Response.StatusCode = code;
            context.Response.StatusDescription = desc;
            foreach (KeyValuePair<string, string> hdr in responseHeaders)
            {
                context.Response.Headers.Add(hdr.Key, hdr.Value);
            }
        }

        public override void Write(byte[] b)
        {
            try
            {
                context.Response.OutputStream.Write(b, 0, b.Length);
                context.Response.ContentLength64 += b.Length;
            }
            catch { }
        }

        public override void oWrite(string s, Encoding enc = null)
        {
            Write((enc ?? Encoding.UTF8).GetBytes(s));
        }

        static string UrlDecode(string url)
        {
            return new Regex(@"%[0-9]{2}").Replace(url, x => ((char)int.Parse(x.ToString().Substring(1), System.Globalization.NumberStyles.HexNumber)).ToString());
        }

        Dictionary<string, string> ProcessQueryString(string url)
        {
            Dictionary<string, string> qs = new Dictionary<string, string>();
            if (!url.Contains("?")) return qs;
            foreach(string pair in url.Split(new char[] { '?' })[1].Split('&'))
            {
                if (pair.Contains("="))
                    qs[UrlDecode(UrlDecode(pair.Split(new char[] { '=' })[0]))] = UrlDecode(UrlDecode(pair.Split(new char[] { '=' })[1]));
            }
            return qs;
        }

        Dictionary<string, string> ProcessFormData(Stream form)
        {
            MemoryStream ms = new MemoryStream();
            form.CopyTo(ms);
            ms.Seek(0, SeekOrigin.Begin);
            Dictionary<string, string> a = ProcessQueryString("?" + new StreamReader(ms).ReadToEnd());
            context.Request.GetType().InvokeMember("m_RequestStream", BindingFlags.SetField | BindingFlags.Instance | BindingFlags.NonPublic, null, context.Request, new object[] { ms });
            return a;
        }

        /*Stream CloneStream(Stream f)
        {
            MemoryStream ms = new MemoryStream();
            f.CopyTo(ms);
            ms.Seek(0, SeekOrigin.Begin);
            context.Request.GetType().InvokeMember("m_RequestStream", BindingFlags.SetField | BindingFlags.Instance | BindingFlags.NonPublic, null, context.Request, new object[] { ms });
        }*/

        public override Dictionary<string, string> QueryString()
        {
            return ProcessQueryString(context.Request.Url.AbsoluteUri);
        }

        public override Dictionary<string, string> FormData()
        {
            return ProcessFormData(context.Request.InputStream);
        }

        public override Stream PostData()
        {
            return inputStream;
        }

        public override void oClose()
        {
            try
            {
                context.Response.OutputStream.Close();
                context.Response.Close();
            }
            catch { }
        }

        void SealResponse()
        {
            context.Response.Headers.Add("X-Powered-By", "LWASP 2");
            oClose();
        }

        void ProcessRequest()
        {
            try
            {
                if (ConfigurationManager.SETTINGS["REGEX_URL"].ToUpper().Contains("FALSE"))
                {
                    requestedResource = ResourceLoader.GetFileByURL(context);
                    if (requestedResource == null)
                    {
                        context.Response.Headers.Add("Content-Type", "text/html; charset=utf-8");
                        oWrite("<h1>404 Shayse!</h1>We couldn't find the resource for you");
                        oClose();
                    }
                    else if (ConfigurationManager.SETTINGS["WEBSCRIPT_EXTENSION"].ToUpper().Contains(Path.GetExtension(requestedResource).ToUpper()))
                    {
                        inputStream = context.Request.InputStream;
                        requestCookies = context.Request.Cookies;
                        responseCookies = new CookieCollection();
                        queryString = ProcessQueryString(context.Request.RawUrl);
                        formData = context.Request.HttpMethod.ToUpper() == "POST" && context.Request.ContentType.Contains("application/x-www-form-urlencoded") ? ProcessFormData(context.Request.InputStream) : new Dictionary<string, string>();
                        requestHeaders = new Dictionary<string, string>();
                        foreach (string k in context.Request.Headers.AllKeys)
                            requestHeaders[k] = context.Request.Headers[k];
                        responseHeaders = new Dictionary<string, string>()
                    {
                        { "Content-Type", "text/html; charset=utf-8" },
                        { "Connection", "close" }
                    };
                        HttpMethod = context.Request.HttpMethod;
                        HttpURL = context.Request.Url;
                        HttpVersion = context.Request.ProtocolVersion.ToString();
                        WebScriptManager.Enqueue(this);
                        // Our script request was sent to the configuration manager for further care
                    }
                    else
                    {
                        // Our static file request will be immediately handeled
                        byte[] buf = File.ReadAllBytes(requestedResource);
                        string type = Mime.GetMimeType(Path.GetExtension(requestedResource));
                        if (type.Contains("text"))
                            context.Response.Headers.Add("Content-Type", type + "; charset=utf-8");
                        else
                            context.Response.Headers.Add("Content-Type", type);
                        context.Response.ContentLength64 += buf.Length;
                        context.Response.OutputStream.Write(buf, 0, buf.Length);
                        oClose();
                    }
                }
                else
                {
                    //Not implemented!
                }
            }
            catch (Exception e)
            {
                context.Response.Headers.Add("Content-Type", "text/html; charset=utf-8");
                oWrite("<h1>500 Internal Server Error</h1><br/>Server error message was: " + e.Message);
                oClose();
            }
        }

        public override void ProcessResponse(string response)
        {
            try
            { 
                if (!HeadersSent)
                {
                    if (responseHeaders != null)
                        foreach (string header in responseHeaders.Keys)
                            context.Response.Headers.Add(header, responseHeaders[header]);
                    for (int i = 0; i < responseCookies.Count; i++)
                    {
                        context.Response.Headers.Add("Set-Cookie", responseCookies[i].ToHeaderString());
                    }
                    oWrite(response, responseEncoding);
                    oClose();
                }
                else
                {
                    oClose();
                }
            }
            catch (Exception e)
            {
                context.Response.Headers.Add("Content-Type", "text/html; charset=utf-8");
                oWrite("<h1>500 Internal Server Error</h1><br/>Server error message was: " + e.Message);
                oClose();
            }
        }
    }
}
