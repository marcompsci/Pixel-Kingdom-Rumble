using System;
using System.Collections.Generic;

namespace PKR.Core
{
    /// <summary>One year's run of one season: tokens found (by level), and what was done in Story Quest.</summary>
    [Serializable]
    public class SeasonRecord
    {
        public string key = "";
        public List<string> tokenLevels = new List<string>();
        public int levelsCleared;
        public int shards;
        public int foes;
        /// <summary>Challenge indexes whose reward was paid.</summary>
        public List<int> claimed = new List<int>();

        public void Sanitize()
        {
            if (tokenLevels == null) tokenLevels = new List<string>();
            tokenLevels.RemoveAll(string.IsNullOrEmpty);
            if (claimed == null) claimed = new List<int>();
            if (levelsCleared < 0) levelsCleared = 0;
            if (shards < 0) shards = 0;
            if (foes < 0) foes = 0;
        }
    }

    /// <summary>Everything the live seasons keep in the save: per-season records and the keepsakes earned.</summary>
    [Serializable]
    public class SeasonSave
    {
        public List<SeasonRecord> records = new List<SeasonRecord>();
        /// <summary>Keepsake ids earned (they stay forever, even after the season ends).</summary>
        public List<string> keepsakes = new List<string>();
        /// <summary>The keepsake the hero wears in Story Quest ("" = none).</summary>
        public string worn = "";

        public void Sanitize()
        {
            if (records == null) records = new List<SeasonRecord>();
            records.RemoveAll(r => r == null || string.IsNullOrEmpty(r.key));
            foreach (var r in records) r.Sanitize();
            if (keepsakes == null) keepsakes = new List<string>();
            keepsakes.RemoveAll(k => string.IsNullOrEmpty(k) || !Seasons.IsKeepsake(k));
            if (worn == null || (worn.Length > 0 && !keepsakes.Contains(worn))) worn = "";
        }
    }

    public enum SeasonGoal { Tokens, LevelsCleared, Shards, Foes }

    public struct SeasonChallenge
    {
        public string text;
        public SeasonGoal goal;
        public int target;
        public int shards;
    }

    public struct Keepsake
    {
        public string id, name, blurb;
        public SeasonEvent ev;
    }

    /// <summary>
    /// The rules of a live season: one hidden token per Story level, limited-time challenges paid in Star Shards, and a
    /// keepsake (an accessory the hero wears) for finding enough tokens before the season ends.
    /// </summary>
    public static class Seasons
    {
        /// <summary>Tokens needed for the season's keepsake (or every level, if the game has fewer).</summary>
        public const int KeepsakeTokens = 8;

        public static int KeepsakeGoal(int storyLevels) => Math.Max(1, Math.Min(KeepsakeTokens, storyLevels));

        public static string TokenName(SeasonEvent ev) => ev switch
        {
            SeasonEvent.Autumn => "Maple Leaf",
            SeasonEvent.Harvest => "Golden Acorn",
            SeasonEvent.Winterfest => "Gift",
            SeasonEvent.Frost => "Snowflake",
            _ => "",
        };

        public static readonly Keepsake[] Keepsakes =
        {
            new Keepsake { id = "keepsake_leaf_crown", name = "Leaf Crown", blurb = "A crown of red and gold leaves", ev = SeasonEvent.Autumn },
            new Keepsake { id = "keepsake_harvest_scarf", name = "Harvest Scarf", blurb = "A long knitted scarf in pumpkin orange", ev = SeasonEvent.Harvest },
            new Keepsake { id = "keepsake_holly_beanie", name = "Holly Beanie", blurb = "A red bobble hat with a sprig of holly", ev = SeasonEvent.Winterfest },
            new Keepsake { id = "keepsake_frost_earmuffs", name = "Frost Earmuffs", blurb = "Fluffy ice-blue earmuffs", ev = SeasonEvent.Frost },
        };

        public static Keepsake? KeepsakeFor(SeasonEvent ev)
        {
            foreach (var k in Keepsakes) if (k.ev == ev) return k;
            return null;
        }

        public static Keepsake? FindKeepsake(string id)
        {
            foreach (var k in Keepsakes) if (k.id == id) return k;
            return null;
        }

        public static bool IsKeepsake(string id) => FindKeepsake(id).HasValue;

        public static SeasonChallenge[] Challenges(SeasonEvent ev)
        {
            if (ev == SeasonEvent.None) return Array.Empty<SeasonChallenge>();
            string token = TokenName(ev);
            return new[]
            {
                new SeasonChallenge { text = $"Find 3 {token}s", goal = SeasonGoal.Tokens, target = 3, shards = 150 },
                new SeasonChallenge { text = "Clear 5 Story levels", goal = SeasonGoal.LevelsCleared, target = 5, shards = 200 },
                new SeasonChallenge { text = "Collect 150 Star Shards in Story", goal = SeasonGoal.Shards, target = 150, shards = 150 },
                new SeasonChallenge { text = "Defeat 40 foes in Story", goal = SeasonGoal.Foes, target = 40, shards = 150 },
            };
        }

        static SeasonSave Of(SaveData save)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));
            if (save.season == null) save.season = new SeasonSave();
            return save.season;
        }

        /// <summary>This season's record, or null if nothing has happened in it yet.</summary>
        public static SeasonRecord Find(SaveData save, SeasonState s)
        {
            if (save == null || save.season == null || !s.Active) return null;
            foreach (var r in save.season.records) if (r != null && r.key == s.Key) return r;
            return null;
        }

        static SeasonRecord Get(SaveData save, SeasonState s)
        {
            var r = Find(save, s);
            if (r != null) return r;
            r = new SeasonRecord { key = s.Key };
            Of(save).records.Add(r);
            return r;
        }

        public static int Tokens(SaveData save, SeasonState s) => Find(save, s)?.tokenLevels.Count ?? 0;

        public static bool HasToken(SaveData save, SeasonState s, string levelId) =>
            Find(save, s)?.tokenLevels.Contains(levelId) ?? false;

        /// <summary>
        /// The hero picked up this level's token. Returns true if it is new; then also hands out the keepsake when the
        /// goal is reached (<paramref name="keepsakeEarned"/>).
        /// </summary>
        public static bool AddToken(SaveData save, SeasonState s, string levelId, int storyLevels, out bool keepsakeEarned)
        {
            keepsakeEarned = false;
            if (!s.Active || string.IsNullOrEmpty(levelId)) return false;
            var r = Get(save, s);
            if (r.tokenLevels.Contains(levelId)) return false;
            r.tokenLevels.Add(levelId);
            if (r.tokenLevels.Count >= KeepsakeGoal(storyLevels))
            {
                var k = KeepsakeFor(s.Event);
                if (k.HasValue && !Of(save).keepsakes.Contains(k.Value.id))
                {
                    Of(save).keepsakes.Add(k.Value.id);
                    keepsakeEarned = true;
                }
            }
            return true;
        }

        /// <summary>A Story level was cleared in season, with this many shards and foes.</summary>
        public static void RecordLevel(SaveData save, SeasonState s, int shards, int foes)
        {
            if (!s.Active) return;
            var r = Get(save, s);
            r.levelsCleared++;
            r.shards += Math.Max(0, shards);
            r.foes += Math.Max(0, foes);
        }

        public static int Progress(SeasonRecord r, SeasonGoal goal)
        {
            if (r == null) return 0;
            return goal switch
            {
                SeasonGoal.Tokens => r.tokenLevels.Count,
                SeasonGoal.LevelsCleared => r.levelsCleared,
                SeasonGoal.Shards => r.shards,
                SeasonGoal.Foes => r.foes,
                _ => 0,
            };
        }

        public static bool IsDone(SaveData save, SeasonState s, int index)
        {
            var list = Challenges(s.Event);
            if (index < 0 || index >= list.Length) return false;
            return Progress(Find(save, s), list[index].goal) >= list[index].target;
        }

        public static bool IsClaimed(SaveData save, SeasonState s, int index) => Find(save, s)?.claimed.Contains(index) ?? false;

        /// <summary>Pays a finished challenge's shards once. Returns the shards paid (0 if not finished or already paid).</summary>
        public static int Claim(SaveData save, SeasonState s, int index)
        {
            if (!IsDone(save, s, index) || IsClaimed(save, s, index)) return 0;
            var c = Challenges(s.Event)[index];
            Get(save, s).claimed.Add(index);
            Economy.Grant(save, c.shards);
            return c.shards;
        }

        /// <summary>Finished challenges not yet claimed (the menu shows a dot).</summary>
        public static int Unclaimed(SaveData save, SeasonState s)
        {
            int n = 0;
            var list = Challenges(s.Event);
            for (int i = 0; i < list.Length; i++) if (IsDone(save, s, i) && !IsClaimed(save, s, i)) n++;
            return n;
        }

        /// <summary>Wear an earned keepsake (or "" for none). Returns false if it isn't owned.</summary>
        public static bool Wear(SaveData save, string id)
        {
            var ss = Of(save);
            if (string.IsNullOrEmpty(id)) { ss.worn = ""; return true; }
            if (!ss.keepsakes.Contains(id)) return false;
            ss.worn = id;
            return true;
        }

        public static bool Owns(SaveData save, string id) => save?.season != null && save.season.keepsakes.Contains(id);
    }
}
