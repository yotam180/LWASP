using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Web;
using System.Threading;
using static LWASP_Console.LWASP;

namespace LWASP_Console
{
    /// <summary>
    /// DO NOT, I REPEAT, DO NOT, USE OR TOUCH THIS GARBAGE CLASS!
    /// </summary>
    class HttpExchange3 : Exchange
    {
        public static int MAX_POST_SIZE = 32 * 1024 * 1024;
        public static int BUFFER_SIZE = 4096;

        public TcpClient socket;

        private Stream inputStream, outputStream;

        public string __input__ = string.Empty;

        public Dictionary<string, string> requestHeaders, responseHeaders = new Dictionary<string, string>()
        {
            { "Connection", "close" },
            { "Content-Type", "text/html; charset=utf-8" },
            { "Server", "LWASP-Express" }
        };
        public CookieCollection requestCookies, responseCookies;

        public HttpExchange3(TcpClient s)
        {
            socket = s;
            __input__ = string.Empty;
        }

        public string ReadLine(Stream s)
        {
            int nc;
            StringBuilder data = new StringBuilder();
            while (true)
            {
                nc = s.ReadByte();
                if (nc == '\n') break;
                if (nc == '\r') continue;
                if (nc == -1) { Thread.Sleep(1); continue; }
                data.Append(Convert.ToChar(nc));
            }
            __input__ += data.ToString() + "\r\n";
            return data.ToString();
        }

        public void ProcessRequest()
        {
            requestHeaders = new Dictionary<string, string>();
            responseHeaders = new Dictionary<string, string>();
            requestCookies = new CookieCollection();
            if (requestHeaders.ContainsKey("Cookie")) requestHeaders["Cookie"].GetHttpCookiesFromHeader(out requestCookies);
            responseCookies = new CookieCollection();
            inputStream = new BufferedStream(socket.GetStream());
            outputStream = socket.GetStream();
            try
            {
                ParseRequest();
                ReadHeaders();
                if (HttpMethod.ToUpper().Equals("GET"))
                {
                    HandleGET();
                }
                else if (HttpMethod.ToUpper().Equals("POST"))
                {
                    HandlePost();
                }
                else
                {
                    SealHeaders(null, 501, "Not Implemented"); // 501 not implemented
                    CloseConnection();
                }
            }
            catch (Exception e)
            {
                // Processing Error
                CloseConnection();
            }
        }

        public void CloseConnection()
        {
            try
            {
                outputStream.Flush();
                inputStream = null;
                outputStream = null;
                socket.Close();
            }
            catch { }
        }

        public void ParseRequest()
        {
            string request = ReadLine(inputStream);
            string[] tokens = request.Split(' ');
            if (tokens.Length != 3)
            {
                throw new Exception("Invalid HTTP Request line");
            }
            HttpMethod = tokens[0];
            HttpURL = new Uri("http://localhost:" + ConfigurationManager.SETTINGS["HTTP_PORT"] + tokens[1]);
            HttpVersion = tokens[2].Replace("HTTP/", "").Trim();
        }

        public void ReadHeaders()
        {
            string line;
            while ((line = ReadLine(inputStream)) != null)
            {
                if (line.Equals(""))
                {
                    // Finished getting the headers!
                    return;
                }
                int sep = line.IndexOf(":");
                if (sep == -1)
                {
                    // Invalid header line
                    continue;
                }
                string name = line.Substring(0, sep);
                int pos = sep + 1;
                while (pos < line.Length && line[pos] == ' ') // This is the most idiotic way I have ever seen to do that -_-
                {
                    pos++;
                }
                string value = line.Substring(pos, line.Length - pos);
                requestHeaders[name] = value;
            }
        }

        public void HandleGET()
        {
            HandleHttpRequest(null);
        }

        public void HandlePost()
        {
            int cl = 0;
            MemoryStream ms = new MemoryStream();
            if (requestHeaders.ContainsKey("Content-Length"))
            {
                cl = Convert.ToInt32(requestHeaders["Content-Length"]);
                if (cl > MAX_POST_SIZE)
                {
                    throw new Exception("Max POST size exceeded"); // Max POST size exceeded
                }
                byte[] buf = new byte[BUFFER_SIZE];
                int tr = cl;
                while (tr > 0)
                {
                    int nr = inputStream.Read(buf, 0, Math.Min(tr, BUFFER_SIZE));
                    if (nr == 0)
                    {
                        if (tr == 0) break;
                        else throw new Exception("Client disconnected during POST"); // Client disconnected during POST
                    }
                    tr -= nr;
                    ms.Write(buf, 0, nr);
                }
                ms.Seek(0, SeekOrigin.Begin);
            }
            HandleHttpRequest(ms);
        }

        public void HandleHttpRequest(Stream pStream)
        {
            SealHeaders();
            oWrite("<h1>Hello World!</h1>");
            CloseConnection();
        }

        public static new void Run()
        {
            TcpListener lis = null;
            try
            {
                int port = int.Parse(ConfigurationManager.SETTINGS["HTTP_PORT"]);
                lis = new TcpListener(IPAddress.Any, port);
                lis.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error opening TCP server: " + e.Message);
                return;
            }
            HTTP_WORKING = true;
            HTTP_WORK = true;
            while (HTTP_WORK)
            {
                try
                {
                    TcpClient client = lis.AcceptTcpClient();
                    new HttpExchange3(client).ProcessRequest();
                }
                catch { }
            }
        }

        public static new void Stop()
        {
            HTTP_WORK = false;
            while (HTTP_WORKING)
            {
                Thread.Sleep(10);
            }
        }

        public override void SealHeaders(string ver = null, int code = 200, string desc = "OK")
        {
            if (HeadersSent) return;
            HeadersSent = true;
            if (ver == null) ver = "1.1";
            oWrite("HTTP/" + ver + " " + code + " " + desc + "\r\n");
            foreach (string header in responseHeaders.Keys)
                oWrite(header + ": " + responseHeaders[header]);
            foreach (Cookie ck in responseCookies)
                oWrite("Set-Cookie: " + ck.ToHeaderString());
            oWrite("\r\n");
        }

        public override void Write(byte[] b)
        {
            try
            {
                socket.GetStream().Write(b, 0, b.Length);
            }
            catch { }
        }

        public override void oWrite(string s, Encoding enc = null)
        {
            Write((enc ?? Encoding.UTF8).GetBytes(s));
        }

        public override void Redirect(string path)
        {
            responseHeaders["Location"] = path;
            SealHeaders(null, 302, "Found");
        }
    }

    class HttpServer
    {
        /*public override Dictionary<string, string> QueryString() { return null; }
        public override Dictionary<string, string> FormData() { return null; }
        public override Stream PostData() { return null; }
        public override void oClose() { }
        public void HandleGetRequest(HttpProcessor p) { }
        public void HandlePostRequest(HttpProcessor p, Stream inputStream) { }
        public override void ProcessResponse(string res) { }*/
    }
}
