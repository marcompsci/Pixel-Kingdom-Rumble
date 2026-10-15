using System;
using System.Collections.Generic;

namespace PKR.Core
{
    [Serializable]
    public class UpgradeRecord
    {
        public string id = "";
        public int level;
    }

    /// <summary>
    /// Permanent Star Shard upgrades for Story Quest and Shadow Contracts (never Arena or Versus, so ranked play
    /// stays even). Each has two levels. Pure rules; the shop and the level flow apply them.
    /// </summary>
    public static class Upgrades
    {
        public const string ExtraHeart = "upg_heart";
        public const string ShardMagnet = "upg_magnet";
        public const string LuckyShards = "upg_lucky";

        public struct Spec
        {
            public string id, name, description;
            public int[] costs; // cost of level 1, level 2, ...
            public int MaxLevel => costs.Length;
        }

        public static readonly Spec[] All =
        {
            new Spec { id = ExtraHeart, name = "Extra Heart", description = "+1 max health in Story Quest and Shadow Contracts per level.", costs = new[] { 150, 300 } },
            new Spec { id = ShardMagnet, name = "Shard Magnet", description = "Star Shards fly to you from farther away.", costs = new[] { 100, 200 } },
            new Spec { id = LuckyShards, name = "Lucky Shards", description = "+10% Star Shards for clearing a level per level.", costs = new[] { 120, 240 } },
        };

        public static Spec? Find(string id)
        {
            foreach (var s in All) if (s.id == id) return s;
            return null;
        }

        public static int Level(SaveData save, string id)
        {
            if (save?.upgrades == null) return 0;
            foreach (var u in save.upgrades) if (u != null && u.id == id) return u.level;
            return 0;
        }

        /// <summary>Price of the next level, or -1 when maxed or unknown.</summary>
        public static int NextCost(SaveData save, string id)
        {
            var s = Find(id);
            if (!s.HasValue) return -1;
            int lvl = Level(save, id);
            return lvl >= s.Value.MaxLevel ? -1 : s.Value.costs[lvl];
        }

        public static PurchaseResult Buy(SaveData save, string id)
        {
            if (save == null || !Find(id).HasValue) return PurchaseResult.InvalidItem;
            int cost = NextCost(save, id);
            if (cost < 0) return PurchaseResult.AlreadyOwned;
            if (save.starShards < cost) return PurchaseResult.NotEnoughShards;
            save.starShards -= cost;
            if (save.upgrades == null) save.upgrades = new List<UpgradeRecord>();
            var rec = save.upgrades.Find(u => u != null && u.id == id);
            if (rec == null) save.upgrades.Add(rec = new UpgradeRecord { id = id });
            rec.level++;
            return PurchaseResult.Success;
        }

        // ---- Effects -----------------------------------------------------------------------------------

        public static int BonusHealth(SaveData save) => Level(save, ExtraHeart);

        /// <summary>Distance (world units) from which shards fly to the hero; 0 = only on touch.</summary>
        public static float MagnetRadius(SaveData save)
        {
            int l = Level(save, ShardMagnet);
            return l <= 0 ? 0f : l == 1 ? 1.6f : 3f;
        }

        /// <summary>Extra shards on top of a level-clear reward (rounded up, at least 1 when the upgrade is owned and reward &gt; 0).</summary>
        public static int LuckyBonus(SaveData save, int reward)
        {
            int l = Level(save, LuckyShards);
            if (l <= 0 || reward <= 0) return 0;
            return Math.Max(1, (int)Math.Ceiling(reward * 0.1 * l));
        }
    }

    /// <summary>Story Quest worlds as the reward system sees them, and the one-time world-completion bonus.</summary>
    public static class StoryWorlds
    {
        public const int CompletionBonus = 200;

        public static readonly (string name, string[] levels)[] All =
        {
            ("Sunspire Isles", new[] { "sq_sunspire_test", "sq_sunspire_heights", "sq_gearfall_caverns", "sq_gearwork_maze", "sq_clockwork_warden", "sq_rooftop_run", "sq_night_market" }),
            ("Frostgear Peaks", new[] { "sq_frostgear_ascent", "sq_clockwork_glacier", "sq_frozen_vault" }),
            ("The Shattered Realms", new[] { "sq_ember_wastes", "sq_sky_gardens", "sq_sunken_clockworks", "sq_toy_fortress", "sq_shattered_crossing", "sq_shattered_labyrinth", "sq_fourfold_sovereign" }),
            ("Starfall Observatory", new[] { "sq_starfall_steps", "sq_orrery_gardens", "sq_celestial_maze", "sq_comet_run", "sq_orrery_sentinel" }),
        };

        public static int WorldOf(string levelId)
        {
            for (int i = 0; i < All.Length; i++)
                if (Array.IndexOf(All[i].levels, levelId) >= 0) return i;
            return -1;
        }

        public static bool IsComplete(SaveData save, int world)
        {
            if (save == null || world < 0 || world >= All.Length) return false;
            foreach (var id in All[world].levels)
            {
                var rec = save.GetLevel(id);
                if (rec == null || !rec.completed) return false;
            }
            return true;
        }

        /// <summary>Grants the bonus the first time every level of the world is cleared. Returns the world index or -1.</summary>
        public static int ClaimIfComplete(SaveData save, string levelId)
        {
            int w = WorldOf(levelId);
            if (w < 0 || !IsComplete(save, w)) return -1;
            if (save.claimedWorldRewards == null) save.claimedWorldRewards = new List<int>();
            if (save.claimedWorldRewards.Contains(w)) return -1;
            save.claimedWorldRewards.Add(w);
            Economy.Grant(save, CompletionBonus);
            return w;
        }
    }
}
