using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
using System.Web;
using System.Net;
using System.Globalization;

namespace LWASP_Console
{
    /// <summary>
    /// An internal API for working with cookies!
    /// I LOVE COOKIES!
    /// </summary>
    public static class LWASPCookieAPI
    {
        // Some complexed and random REGEXes!
        static Regex rxCookieParts = new Regex(@"(?<name>.*?)\=(?<value>.*?)\;|(?<name>\bsecure\b|\bhttponly\b)", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
        static Regex rxRemoveCommaFromDate = new Regex(@"\bexpires\b\=.*?(\;|$)", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.Multiline);

        /// <summary>
        /// I have no idea what this function does. ~this is kinda stolen from SO, I think~s
        /// </summary>
        /// <param name="collection">Idk</param>
        /// <param name="index">No idea</param>
        /// <param name="cookies">Wtf is that?</param>
        /// <returns>Some boolean I don't know</returns>
        public static bool GetHttpCookies(this NameValueCollection collection, int index, out List<HttpCookie> cookies)
        {
            cookies = new List<HttpCookie>();

            if (collection.AllKeys[index].ToLower() != "set-cookie") return false;
            try
            {

                string rawcookieString = rxRemoveCommaFromDate.Replace(collection[index], new MatchEvaluator(RemoveComma));

                string[] rawCookies = rawcookieString.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var rawCookie in rawCookies)
                {
                    cookies.Add(rawCookie.ToHttpCookie());
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Extension method!
        /// Converts a cookie object to it's header representation that can be sent to the client's browser
        /// </summary>
        /// <param name="c">The cookie object to convert</param>
        /// <returns>The header string. Doesn't include the Set-Cookie: part, Only the value</returns>
        public static string ToHeaderString(this Cookie c)
        {
            string s = c.Name + "=" + c.Value;
            if (c.Path != null && c.Path != string.Empty)
            {
                s += "; path=" + c.Path;
            }
            if (c.Domain != null && c.Domain != string.Empty)
            {
                s += "; domain=" + c.Domain;
            }
            if (c.Expires != null)
            {
                s += "; expires=" + c.Expires.ToString("ddd, d MMM yyyy HH:mm:ss", new CultureInfo("en-US")) + " GMT";
            }
            return s;
        }

        public static bool GetHttpCookiesFromHeader(this string cookieHeader, out CookieCollection cookies)
        {
            cookies = new CookieCollection();


            try
            {

                string rawcookieString = rxRemoveCommaFromDate.Replace(cookieHeader, new MatchEvaluator(RemoveComma));

                string[] rawCookies = rawcookieString.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                if (rawCookies.Length == 0)
                {
                    cookies.Add(rawcookieString.ToCookie());
                }
                else
                {
                    foreach (var rawCookie in rawCookies)
                    {
                        cookies.Add(rawCookie.ToCookie());
                    }
                }

                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }



        /// <summary>
        /// The opposite of ToHeaderString()
        /// </summary>
        /// <param name="rawCookie">The string to parse to cookie</param>
        /// <returns>The result of the parsing - a cookie.</returns>
        public static Cookie ToCookie(this string rawCookie)
        {

            if (!rawCookie.EndsWith(";")) rawCookie += ";";

            MatchCollection maches = rxCookieParts.Matches(rawCookie);

            Cookie cookie = new Cookie(maches[0].Groups["name"].Value.Trim(), maches[0].Groups["value"].Value.Trim());

            for (int i = 1; i < maches.Count; i++)
            {
                switch (maches[i].Groups["name"].Value.ToLower().Trim())
                {
                    case "domain":
                        cookie.Domain = maches[i].Groups["value"].Value;
                        break;
                    case "expires":

                        DateTime dt;

                        if (DateTime.TryParse(maches[i].Groups["value"].Value, out dt))
                        {
                            cookie.Expires = dt;
                        }
                        else
                        {
                            cookie.Expires = DateTime.Now.AddDays(2);
                        }
                        break;
                    case "path":
                        cookie.Path = maches[i].Groups["value"].Value;
                        break;
                    case "secure":
                        cookie.Secure = true;
                        break;
                    case "httponly":
                        cookie.HttpOnly = true;
                        break;
                }
            }
            return cookie;


        }

        public static HttpCookie ToHttpCookie(this string rawCookie)
        {
            MatchCollection maches = rxCookieParts.Matches(rawCookie);

            HttpCookie cookie = new HttpCookie(maches[0].Groups["name"].Value, maches[0].Groups["value"].Value);

            for (int i = 1; i < maches.Count; i++)
            {
                switch (maches[i].Groups["name"].Value.ToLower().Trim())
                {
                    case "domain":
                        cookie.Domain = maches[i].Groups["value"].Value;
                        break;
                    case "expires":

                        DateTime dt;

                        if (DateTime.TryParse(maches[i].Groups["value"].Value, out dt))
                        {
                            cookie.Expires = dt;
                        }
                        else
                        {
                            cookie.Expires = DateTime.Now.AddDays(2);
                        }
                        break;
                    case "path":
                        cookie.Path = maches[i].Groups["value"].Value;
                        break;
                    case "secure":
                        cookie.Secure = true;
                        break;
                    case "httponly":
                        cookie.HttpOnly = true;
                        break;
                }
            }
            return cookie;
        }

        private static KeyValuePair<string, string> SplitToPair(this string input)
        {
            string[] parts = input.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
            return new KeyValuePair<string, string>(parts[0], parts[1]);
        }

        private static string RemoveComma(Match match)
        {
            return match.Value.Replace(',', ' ');
        }
    }
}
