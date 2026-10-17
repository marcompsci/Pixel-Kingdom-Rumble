using System.Collections.Generic;

namespace PKR.Core
{
    public enum CosmeticSlot { Tracer, Title }

    /// <summary>What the unlock rules look at (filled from the save).</summary>
    public struct CosmeticStats
    {
        public int kills, rank, bestRound, bestStreak, weeklyWins, revives, bossKills, prestige, golds;
        public List<string> secrets;
        public bool chapter1, chapter2, chapter3, chapter4;

        public static CosmeticStats From(ZombieCareer c, ZombieLandProgress zl)
        {
            var s = new CosmeticStats { secrets = new List<string>() };
            if (c != null)
            {
                c.Sanitize();
                s.kills = c.totalKills;
                s.rank = SurvivorRank.RankFor(c.xp);
                foreach (var r in c.bestRounds) if (r > s.bestRound) s.bestRound = r;
                s.bestStreak = c.bestStreak;
                s.weeklyWins = c.weeklyWins;
                s.revives = c.revives;
                s.bossKills = c.bossKills;
                s.prestige = c.prestige;
                s.golds = WeaponMastery.Golds(c);
                s.secrets.AddRange(c.secrets);
            }
            if (zl != null) { s.chapter1 = zl.bossDefeated; s.chapter2 = zl.chapter2Done; s.chapter3 = zl.chapter3Done; s.chapter4 = zl.chapter4Done; }
            return s;
        }

        public bool Has(string secret) => secrets != null && secrets.Contains(secret);
    }

    /// <summary>One cosmetic: a bullet tracer colour or a title shown under your name. Earned by playing, never sold.</summary>
    public sealed class Cosmetic
    {
        public string Id, Name, HowTo;
        public CosmeticSlot Slot;
        public byte R = 255, G = 255, B = 255;
        public System.Func<CosmeticStats, bool> Rule;
        public bool Unlocked(CosmeticStats s) => Rule == null || Rule(s);
    }

    /// <summary>
    /// The LOCKER: earned tracers and titles. Players liked showing off what they'd done (a hard boss, a long streak)
    /// far more than buying looks, so every item here is a record of something played. Purely visual: no stats.
    /// </summary>
    public static class ZombieCosmetics
    {
        public static readonly Cosmetic[] All =
        {
            // tracers
            new Cosmetic { Id = "tr_toxic", Name = "TOXIC TRACERS", Slot = CosmeticSlot.Tracer, R = 120, G = 255, B = 90, HowTo = "Kill 500 zombies", Rule = s => s.kills >= 500 },
            new Cosmetic { Id = "tr_frost", Name = "FROST TRACERS", Slot = CosmeticSlot.Tracer, R = 140, G = 220, B = 255, HowTo = "Reach round 15 on any map", Rule = s => s.bestRound >= 15 },
            new Cosmetic { Id = "tr_crimson", Name = "CRIMSON TRACERS", Slot = CosmeticSlot.Tracer, R = 255, G = 60, B = 70, HowTo = "Keep a 7-day daily orders streak", Rule = s => s.bestStreak >= 7 },
            new Cosmetic { Id = "tr_neon", Name = "NEON TRACERS", Slot = CosmeticSlot.Tracer, R = 255, G = 90, B = 230, HowTo = "Finish 3 weekly challenges", Rule = s => s.weeklyWins >= 3 },
            new Cosmetic { Id = "tr_gold", Name = "GOLD TRACERS", Slot = CosmeticSlot.Tracer, R = 255, G = 210, B = 70, HowTo = "Beat every Last Stand hidden boss",
                           Rule = s => { foreach (var m in LastStandMap.All) if (!s.Has(m.HiddenSecret)) return false; return true; } },
            new Cosmetic { Id = "tr_prism", Name = "PRISM TRACERS", Slot = CosmeticSlot.Tracer, R = 120, G = 255, B = 240, HowTo = "Reach prestige 3", Rule = s => s.prestige >= 3 },
            new Cosmetic { Id = "tr_obsidian", Name = "OBSIDIAN TRACERS", Slot = CosmeticSlot.Tracer, R = 150, G = 110, B = 255, HowTo = "Gold finish on six guns", Rule = s => s.golds >= WeaponMastery.ObsidianGolds },
            new Cosmetic { Id = "tr_void", Name = "VOID TRACERS", Slot = CosmeticSlot.Tracer, R = 150, G = 90, B = 255, HowTo = "Seal the Sublevel (Zombie Land chapter 3)", Rule = s => s.chapter3 },
            // titles
            new Cosmetic { Id = "ti_survivor", Name = "SURVIVOR", Slot = CosmeticSlot.Title, HowTo = "Reach Survivor Rank 10", Rule = s => s.rank >= 10 },
            new Cosmetic { Id = "ti_medic", Name = "FIELD MEDIC", Slot = CosmeticSlot.Title, HowTo = "Revive teammates 25 times", Rule = s => s.revives >= 25 },
            new Cosmetic { Id = "ti_crownbreaker", Name = "CROWNBREAKER", Slot = CosmeticSlot.Title, HowTo = "Beat the Hollow King", Rule = s => s.Has(LastStandRules.HiddenBossSecret) },
            new Cosmetic { Id = "ti_tidebreaker", Name = "TIDEBREAKER", Slot = CosmeticSlot.Title, HowTo = "Beat the Tide Mother", Rule = s => s.Has(LastStandMap.SaltPier.HiddenSecret) },
            new Cosmetic { Id = "ti_widowmaker", Name = "WIDOWMAKER", Slot = CosmeticSlot.Title, HowTo = "Beat the Iron Widow", Rule = s => s.Has(LastStandMap.RailYard.HiddenSecret) },
            new Cosmetic { Id = "ti_thornless", Name = "THORNLESS", Slot = CosmeticSlot.Title, HowTo = "Beat the Briar Queen", Rule = s => s.Has(LastStandMap.Gravemoor.HiddenSecret) },
            new Cosmetic { Id = "ti_cityclear", Name = "CITY SWEEPER", Slot = CosmeticSlot.Title, HowTo = "Beat the First Turned (Zombie Land)", Rule = s => s.chapter1 },
            new Cosmetic { Id = "ti_curatorsbane", Name = "CURATOR'S BANE", Slot = CosmeticSlot.Title, HowTo = "Beat the Curator (Zombie Land chapter 3)", Rule = s => s.chapter3 },
            new Cosmetic { Id = "ti_wheelsup", Name = "WHEELS UP", Slot = CosmeticSlot.Title, HowTo = "Beat the Marshal (Zombie Land chapter 4)", Rule = s => s.chapter4 },
            new Cosmetic { Id = "ti_onthelock", Name = "ON THE CLOCK", Slot = CosmeticSlot.Title, HowTo = "Keep a 7-day daily orders streak", Rule = s => s.bestStreak >= 7 },
            new Cosmetic { Id = "ti_undertaker", Name = "UNDERTAKER", Slot = CosmeticSlot.Title, HowTo = "Kill 2,500 zombies", Rule = s => s.kills >= 2500 },
            new Cosmetic { Id = "ti_reborn", Name = "REBORN", Slot = CosmeticSlot.Title, HowTo = "Prestige once", Rule = s => s.prestige >= 1 },
            new Cosmetic { Id = "ti_ascendant", Name = "ASCENDANT", Slot = CosmeticSlot.Title, HowTo = "Reach prestige 5", Rule = s => s.prestige >= 5 },
            new Cosmetic { Id = "ti_eternal", Name = "ETERNAL", Slot = CosmeticSlot.Title, HowTo = "Reach prestige 10", Rule = s => s.prestige >= ZombiePrestige.Max },
            new Cosmetic { Id = "ti_armorer", Name = "ARMORER", Slot = CosmeticSlot.Title, HowTo = "Gold finish on three guns", Rule = s => s.golds >= 3 },
        };

        public static Cosmetic Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var c in All) if (c.Id == id) return c;
            return null;
        }

        public static int UnlockedCount(CosmeticStats s)
        {
            int n = 0;
            foreach (var c in All) if (c.Unlocked(s)) n++;
            return n;
        }

        /// <summary>Equip (or, with an id already worn, take off). Locked or unknown items do nothing. True if changed.</summary>
        public static bool Toggle(ZombieCareer career, string id, CosmeticStats s)
        {
            var c = Get(id);
            if (career == null || c == null || !c.Unlocked(s)) return false;
            if (c.Slot == CosmeticSlot.Tracer) career.tracer = career.tracer == id ? "" : id;
            else career.title = career.title == id ? "" : id;
            return true;
        }

        /// <summary>The equipped item in a slot, only if still unlocked (a reset save can't keep wearing it).</summary>
        public static Cosmetic Worn(ZombieCareer career, CosmeticSlot slot, CosmeticStats s)
        {
            if (career == null) return null;
            var c = Get(slot == CosmeticSlot.Tracer ? career.tracer : career.title);
            return c != null && c.Slot == slot && c.Unlocked(s) ? c : null;
        }

        /// <summary>Items that became unlocked between two snapshots (for "NEW IN THE LOCKER" toasts).</summary>
        public static List<Cosmetic> NewlyUnlocked(CosmeticStats before, CosmeticStats after)
        {
            var list = new List<Cosmetic>();
            foreach (var c in All) if (!c.Unlocked(before) && c.Unlocked(after)) list.Add(c);
            return list;
        }
    }
}
