using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PKR.Core
{
    /// <summary>
    /// A small, strict JSON reader for data the game itself doesn't write (the Quick Game widget's stats). Objects become
    /// Dictionary&lt;string, object&gt;, arrays List&lt;object&gt;, numbers double, plus string, bool and null.
    /// Returns null for anything that isn't valid JSON instead of throwing.
    /// </summary>
    public static class TinyJson
    {
        public static object Parse(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                int i = 0;
                var v = Value(json, ref i);
                Skip(json, ref i);
                return i == json.Length ? v : null;
            }
            catch (FormatException) { return null; }
            catch (IndexOutOfRangeException) { return null; }
            catch (ArgumentOutOfRangeException) { return null; }
        }

        /// <summary>The widget's stats (QGStats in QuickGameEngine.swift), or null if the text isn't a stats object.</summary>
        public static QuickGameStats QuickGameStatsFrom(string json)
        {
            if (!(Parse(json) is Dictionary<string, object> d)) return null;
            int I(string k) => d.TryGetValue(k, out var v) && v is double n ? (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, n)) : 0;
            string S(string k) => d.TryGetValue(k, out var v) && v is string s ? s : "";
            var st = new QuickGameStats
            {
                runs = I("runs"), clears = I("clears"), losses = I("losses"), shards = I("shards"), stomps = I("stomps"),
                taps = I("taps"), streak = I("streak"), bestStreak = I("bestStreak"), lastDay = S("lastDay"), lastStage = S("lastStage"),
            };
            if (d.TryGetValue("best", out var b) && b is List<object> list)
                foreach (var item in list)
                    if (item is Dictionary<string, object> e && e.TryGetValue("id", out var id) && id is string sid &&
                        e.TryGetValue("steps", out var steps) && steps is double sn)
                        st.best.Add(new QGBestRecord { id = sid, steps = (int)sn });
            st.Sanitize();
            return st;
        }

        static void Skip(string s, ref int i)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
        }

        static object Value(string s, ref int i)
        {
            Skip(s, ref i);
            if (i >= s.Length) throw new FormatException();
            char c = s[i];
            if (c == '{') return Obj(s, ref i);
            if (c == '[') return Arr(s, ref i);
            if (c == '"') return Str(s, ref i);
            if (s.Length - i >= 4 && string.CompareOrdinal(s, i, "true", 0, 4) == 0) { i += 4; return true; }
            if (s.Length - i >= 5 && string.CompareOrdinal(s, i, "false", 0, 5) == 0) { i += 5; return false; }
            if (s.Length - i >= 4 && string.CompareOrdinal(s, i, "null", 0, 4) == 0) { i += 4; return null; }
            return Num(s, ref i);
        }

        static Dictionary<string, object> Obj(string s, ref int i)
        {
            var d = new Dictionary<string, object>();
            i++; Skip(s, ref i);
            if (s[i] == '}') { i++; return d; }
            while (true)
            {
                Skip(s, ref i);
                if (s[i] != '"') throw new FormatException();
                string k = Str(s, ref i);
                Skip(s, ref i);
                if (s[i] != ':') throw new FormatException();
                i++;
                d[k] = Value(s, ref i);
                Skip(s, ref i);
                if (s[i] == ',') { i++; continue; }
                if (s[i] == '}') { i++; return d; }
                throw new FormatException();
            }
        }

        static List<object> Arr(string s, ref int i)
        {
            var l = new List<object>();
            i++; Skip(s, ref i);
            if (s[i] == ']') { i++; return l; }
            while (true)
            {
                l.Add(Value(s, ref i));
                Skip(s, ref i);
                if (s[i] == ',') { i++; continue; }
                if (s[i] == ']') { i++; return l; }
                throw new FormatException();
            }
        }

        static string Str(string s, ref int i)
        {
            var sb = new StringBuilder();
            i++;
            while (true)
            {
                char c = s[i++];
                if (c == '"') return sb.ToString();
                if (c != '\\') { sb.Append(c); continue; }
                char e = s[i++];
                switch (e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        sb.Append((char)int.Parse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                        i += 4;
                        break;
                    default: throw new FormatException();
                }
            }
        }

        static double Num(string s, ref int i)
        {
            int start = i;
            while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            if (i == start) throw new FormatException();
            return double.Parse(s.Substring(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture);
        }
    }
}
