using System;
using System.Text;

namespace PKR.Core
{
    /// <summary>What one finished match looks like on a share card (from the player's point of view).</summary>
    public struct ShareCardInput
    {
        public int day;            // yyyymmdd
        public bool daily;         // today's Daily Rumble
        public string twist;       // e.g. "ORBIT"
        public string hero;
        public int placement;      // 1-based
        public int fighters;
        public int knockouts;
        public int falls;
        public int points;         // Rumble Points earned
        public int streak;         // Daily Rumble streak
        public string tier;        // e.g. "GOLD" (empty when unrated)
    }

    /// <summary>
    /// Builds the spoiler-free result text people paste into chats (a Wordle-style card): the day's number and twist,
    /// placement, a row of KO and fall squares, streak and points, then where to get the game.
    /// </summary>
    public static class ShareCard
    {
        public const string Link = "https://testflight.apple.com/join/U6MgRyyP";
        public const int MaxSquares = 8;

        /// <summary>Daily Rumble #1 was 1 October 2026; the number goes up by one each calendar day.</summary>
        public static int DailyNumber(int dayKey)
        {
            int y = dayKey / 10000, m = dayKey / 100 % 100, d = dayKey % 100;
            try { return (int)(new DateTime(y, m, d) - new DateTime(2026, 10, 1)).TotalDays + 1; }
            catch (ArgumentOutOfRangeException) { return 0; }
        }

        public static string Medal(int placement) =>
            placement == 1 ? "\U0001F947" : placement == 2 ? "\U0001F948" : placement == 3 ? "\U0001F949" : "\U0001F480";

        public static string Ordinal(int n) => n == 1 ? "1st" : n == 2 ? "2nd" : n == 3 ? "3rd" : n + "th";

        /// <summary>count coloured squares (capped, with a "+N" overflow), or one white square for zero.</summary>
        public static string Squares(string square, int count)
        {
            if (count <= 0) return "⬜";
            var sb = new StringBuilder();
            for (int i = 0; i < Math.Min(count, MaxSquares); i++) sb.Append(square);
            if (count > MaxSquares) sb.Append(" +").Append(count - MaxSquares);
            return sb.ToString();
        }

        public static string Format(ShareCardInput x)
        {
            var sb = new StringBuilder();
            sb.Append("Pixel Kingdom Rumble");
            if (x.daily) sb.Append(" · Daily #").Append(DailyNumber(x.day));
            sb.Append('\n');
            if (!string.IsNullOrEmpty(x.twist)) sb.Append("\U0001F300 ").Append(x.twist).Append('\n');
            sb.Append(Medal(x.placement)).Append(' ').Append(Ordinal(x.placement)).Append(" of ").Append(Math.Max(x.fighters, x.placement));
            if (!string.IsNullOrEmpty(x.hero)) sb.Append(" · ").Append(x.hero);
            sb.Append('\n');
            sb.Append(Squares("\U0001F7E9", x.knockouts)).Append(" KOs\n");
            sb.Append(Squares("\U0001F7E5", x.falls)).Append(" falls\n");
            var tail = new StringBuilder();
            if (x.daily && x.streak > 0) tail.Append("\U0001F525 ").Append(x.streak).Append("-day streak");
            if (x.points > 0) { if (tail.Length > 0) tail.Append(" · "); tail.Append('+').Append(x.points).Append(" pts"); }
            if (!string.IsNullOrEmpty(x.tier)) { if (tail.Length > 0) tail.Append(" · "); tail.Append(x.tier); }
            if (tail.Length > 0) sb.Append(tail).Append('\n');
            sb.Append(Link);
            return sb.ToString();
        }
    }
}
