using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Data.OleDb;

namespace LWASP_Console
{
    /// <summary>
    /// Many many magic methods that might shorten your time writing code ;)
    /// You're welcomed
    /// </summary>
    public class LwaspMagic
    {
        public static OleDbConnection conn;
        public static HttpExchange _CONNECTION;
        public static Dictionary<string, string> _GET, _POST;
        public static Stream _POSTDATA;
        public static double Max(double a, double b) { return a > b ? a : b; }
        public static int Max(int a, int b) { return a > b ? a : b; }
        public static double Min(double a, double b) { return a < b ? a : b; }
        public static int Min(int a, int b) { return a < b ? a : b; }
        public static double Abs(double a) { return a > 0 ? a : -a; }
        public static int Abs(int a) { return a > 0 ? a : -a; }
        public static double Nabs(double a) { return a < 0 ? a : -a; }
        public static int Nabs(int a) { return a < 0 ? a : -a; }
        public static double Acos(double a) { return Math.Acos(a); }
        public static double Asin(double a) { return Math.Asin(a); }
        public static string AddCSlashes(string s) { return new Regex(s).Replace(s, x => "\\" + x); }
        public static string AddSlashes(string s) { return new Regex("(\'|\"|\\)").Replace(s, x => "\\" + x); }
        public static void LwaspTerminate() { Environment.Exit(-1); }
        public static string LwaspVersion() { return LWASP.VERSION; }
        public static T[][] ArrayChunk<T>(T[] arr) { return arr.Select((s, i) => new { Value = s, Index = i }).GroupBy(x => x.Index / 100).Select(grp => grp.Select(x => x.Value).ToArray()).ToArray(); }
        public static K[] ArrayColumn<T, K> (List<Dictionary<T, K>> inp, T col) { return inp.Where(x => x.ContainsKey(col)).Select(x => x[col]).ToArray(); }
        public static string GetCookie(string cookie)
        {
            foreach (System.Net.Cookie ck in _CONNECTION.requestCookies)
            {
                if (ck.Name == cookie) return ck.Value;
            }
            return null;
        }
        public static void SetCookie(string cookie, string value)
        {
            System.Net.Cookie ck = new System.Net.Cookie(cookie, value);
            ck.Expires = DateTime.Now.AddDays(30);
            _CONNECTION.responseCookies.Add(ck);
        }
        public static void DeleteCookie(string cookie)
        {
            System.Net.Cookie ck = new System.Net.Cookie(cookie, "[]");
            ck.Expires = DateTime.Now.AddDays(-100);
            _CONNECTION.responseCookies.Add(ck);
        }

        public static OleDbConnection MakeConnection()
        {
            try
            {
                return new OleDbConnection(ConfigurationManager.SETTINGS["ACCESS_CONNSTR"]);
            }
            catch
            {
                return null;
            }
        }

        public static void CloseConnection(OleDbConnection b)
        {
            try
            {
                b.Close();
            }
            catch { }
        }

        public static List<Dictionary<string, string>> Query(string query, OleDbConnection connection = null)
        {
            if (query.Trim().StartsWith("SELECT"))
            {
                return ReadAction(query, connection);
            }
            else
            {
                WriteAction(query, connection);
                return null;
            }
        }

        static bool WriteAction(string query, OleDbConnection connt = null)
        {
            OleDbConnection conn = null;
            OleDbCommand cmd;
            try
            {
                if (connt == null)
                {
                    conn = new OleDbConnection(ConfigurationManager.SETTINGS["ACCESS_CONNSTR"]);
                    conn.Open();
                }
                else
                    conn = connt;
                cmd = new OleDbCommand(query, conn);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception exx)
            {
                return false;
            }
            finally
            {
                if (conn != null && connt == null) conn.Close();
            }
        }

        static List<Dictionary<string, string>> ReadAction(string query, OleDbConnection connt = null)
        {
            OleDbConnection conn = null;
            OleDbCommand cmd;
            OleDbDataReader dr;
            List<Dictionary<string, string>> res = new List<Dictionary<string, string>>();
            try
            {
                if (connt == null)
                {
                    conn = new OleDbConnection(ConfigurationManager.SETTINGS["ACCESS_CONNSTR"]);
                    conn.Open();
                }
                else
                    conn = connt;
                cmd = new OleDbCommand(query, conn);
                dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    Dictionary<string, string> row = new Dictionary<string, string>();
                    for (int i = 0; i < dr.FieldCount; i++)
                    {
                        row[dr.GetName(i).ToString()] = dr.GetValue(i).ToString();
                    }
                    res.Add(row);
                }
                return res;
            }
            catch (Exception exx)
            {
                return null;
            }
            finally
            {
                if (conn != null && connt == null) conn.Close();
            }
        }
    }
}
