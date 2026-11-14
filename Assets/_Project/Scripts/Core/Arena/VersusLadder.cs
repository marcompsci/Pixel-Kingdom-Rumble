using System;
using System.Collections.Generic;

namespace PKR.Core
{
    /// <summary>
    /// VERSUS LADDER: beat a run of CPU rivals in a row (best of 3 each), getting harder as you climb. A loss ends
    /// the climb; you can retry the rung you fell on. Pure and deterministic for a seed.
    /// </summary>
    [Serializable]
    public class VersusLadder
    {
        public const int MaxRungs = 5;

        public string player;
        public string[] rivals = new string[0];
        public BotLevel[] levels = new BotLevel[0];
        public int rung;           // 0-based rung being fought
        public bool over;          // lost (and not retried) or cleared
        public bool cleared;

        public int Length => rivals.Length;
        public bool Active => !over && rung < rivals.Length;
        public string CurrentRival => rung < rivals.Length ? rivals[rung] : null;
        public BotLevel CurrentLevel => rung < levels.Length ? levels[rung] : BotLevel.Hard;
        public bool OnLastRung => rung == rivals.Length - 1;

        /// <summary>Builds a ladder for this player from the selectable fighter ids (the player's own id is skipped
        /// unless they are the only fighter, which makes a mirror match).</summary>
        public static VersusLadder Build(string player, IList<string> selectable, int seed)
        {
            var pool = new List<string>();
            if (selectable != null)
                foreach (var id in selectable)
                    if (!string.IsNullOrEmpty(id) && id != player && !pool.Contains(id)) pool.Add(id);
            if (pool.Count == 0 && !string.IsNullOrEmpty(player)) pool.Add(player);
            var rng = new Random(seed);
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }
            // Short rosters repeat rivals so a climb is always at least 3 rungs.
            int n = Math.Min(MaxRungs, Math.Max(3, pool.Count));
            var rivals = new string[n];
            for (int i = 0; i < n; i++) rivals[i] = pool.Count > 0 ? pool[i % pool.Count] : player;
            var levels = new BotLevel[n];
            for (int i = 0; i < n; i++) levels[i] = LevelFor(i, n);
            return new VersusLadder { player = player, rivals = rivals, levels = levels };
        }

        /// <summary>The first rung is Easy, the last one (or two, on long ladders) Hard, the rest Normal.</summary>
        public static BotLevel LevelFor(int rung, int length)
        {
            if (rung <= 0) return BotLevel.Easy;
            int hardFrom = length >= 5 ? length - 2 : length - 1;
            return rung >= hardFrom ? BotLevel.Hard : BotLevel.Normal;
        }

        /// <summary>Reports the result of the current rung. Returns true when this win cleared the whole ladder.</summary>
        public bool Report(bool won)
        {
            if (!Active) return false;
            if (!won) { over = true; return false; }
            if (OnLastRung)
            {
                rung++;
                over = cleared = true;
                return true;
            }
            rung++;
            return false;
        }

        /// <summary>Try the rung you lost on again.</summary>
        public void Retry()
        {
            if (cleared) return;
            over = false;
        }

        /// <summary>Star Shards for clearing a ladder of this length.</summary>
        public static int ClearReward(int length) => 100 + 40 * Math.Max(0, length);
    }

    /// <summary>Ladder records in the save.</summary>
    [Serializable]
    public class LadderSave
    {
        public int clears;
        public int bestRungs;              // most rungs won in one climb
        public List<string> clearedWith = new List<string>(); // fighters you've cleared a ladder with

        public void Sanitize()
        {
            clears = Math.Max(0, clears);
            bestRungs = Math.Max(0, Math.Min(VersusLadder.MaxRungs, bestRungs));
            if (clearedWith == null) clearedWith = new List<string>();
            clearedWith.RemoveAll(string.IsNullOrEmpty);
        }

        /// <summary>Records a finished climb (rungs won); pays the clear reward the caller adds. Returns true on a clear.</summary>
        public bool Record(VersusLadder ladder)
        {
            if (ladder == null) return false;
            int won = Math.Min(ladder.rung, ladder.Length);
            if (won > bestRungs) bestRungs = Math.Min(VersusLadder.MaxRungs, won);
            if (!ladder.cleared) return false;
            clears++;
            if (!string.IsNullOrEmpty(ladder.player) && !clearedWith.Contains(ladder.player)) clearedWith.Add(ladder.player);
            return true;
        }
    }
}
