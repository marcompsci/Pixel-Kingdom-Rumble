using System;

namespace PKR.Core
{
    public enum ClearRank { C, B, A, S }

    /// <summary>Formatting and grading for the level-complete screen.</summary>
    public static class ResultsMath
    {
        /// <summary>"m:ss.t" (e.g. 1:05.3). Negative and NaN become 0.</summary>
        public static string FormatTime(float seconds)
        {
            if (float.IsNaN(seconds) || seconds < 0f) seconds = 0f;
            int tenths = (int)Math.Floor(seconds * 10f + 0.0001f);
            int m = tenths / 600;
            int s = (tenths / 10) % 60;
            int t = tenths % 10;
            return $"{m}:{s:00}.{t}";
        }

        /// <summary>Whole-number percentage, clamped to 0..100. Total 0 counts as 100%.</summary>
        public static int Percent(int got, int total)
        {
            if (total <= 0) return 100;
            int p = (int)Math.Floor(100.0 * Math.Max(0, got) / total);
            return Math.Min(100, p);
        }

        /// <summary>
        /// Rank from four 0-1 scores: time vs par (full marks at or under par, zero at 2x par), shards collected,
        /// secrets found, and deaths (each death costs 25%). Weighted 35/35/15/15.
        /// </summary>
        public static ClearRank Rank(float timeSeconds, float parSeconds, int shards, int totalShards,
                                     int secrets, int totalSecrets, int deaths)
        {
            float timeScore = 1f;
            if (parSeconds > 0f && timeSeconds > parSeconds)
                timeScore = Math.Max(0f, 1f - (timeSeconds - parSeconds) / parSeconds);
            float shardScore = Percent(shards, totalShards) / 100f;
            float secretScore = Percent(secrets, totalSecrets) / 100f;
            float deathScore = Math.Max(0f, 1f - 0.25f * Math.Max(0, deaths));

            float score = 0.35f * timeScore + 0.35f * shardScore + 0.15f * secretScore + 0.15f * deathScore;
            if (score >= 0.9f) return ClearRank.S;
            if (score >= 0.75f) return ClearRank.A;
            if (score >= 0.55f) return ClearRank.B;
            return ClearRank.C;
        }
    }
}
