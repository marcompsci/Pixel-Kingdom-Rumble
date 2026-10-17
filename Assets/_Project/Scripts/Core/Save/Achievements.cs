using System.Collections.Generic;

namespace PKR.Core
{
    /// <summary>What a finished level or contract looked like, for deciding which achievements it earned.</summary>
    public struct ClearInfo
    {
        public string levelId;
        public bool isContract;
        public string stealthRank;
        public int shards, totalShards;
        public int deaths;
        public int relics, totalRelics;
        public bool clueFound;
    }

    /// <summary>
    /// Game Center ids (must match App Store Connect exactly) and the pure rules for when each achievement is earned.
    /// </summary>
    public static class Achievements
    {
        public const string LeaderboardContracts = "pkr.lb.contracts";
        /// <summary>Rumble Rating (Arena Clash / Versus / Daily Rumble). Best score, high to low.</summary>
        public const string LeaderboardRumble = "pkr.lb.rumble";
        /// <summary>Zombie leaderboards (best round reached in Endless or Hardcore; Survivor Rank XP). High to low.</summary>
        public const string LeaderboardLastStand = "pkr.lb.laststand";
        public const string LeaderboardManor = "pkr.lb.manor";
        public const string LeaderboardSurvivorXp = "pkr.lb.survivor_xp";
        public const string LeaderboardPier = "pkr.lb.pier";
        public const string LeaderboardRail = "pkr.lb.rail";
        public const string LeaderboardGravemoor = "pkr.lb.gravemoor";

        public const string FirstSteps = "pkr.ach.first_steps";
        public const string Warden = "pkr.ach.warden";
        public const string SecretSeeker = "pkr.ach.secret_seeker";
        public const string ShardHoarder = "pkr.ach.every_shard";
        public const string Untouchable = "pkr.ach.untouchable";
        public const string FirstContract = "pkr.ach.first_contract";
        public const string Ghost = "pkr.ach.ghost";
        public const string RelicHunter = "pkr.ach.relic_hunter";
        public const string Cipher = "pkr.ach.cipher";
        public const string DojoChampion = "pkr.ach.versus_win";

        // zombies
        public const string ZRound10 = "pkr.ach.z_round10";
        public const string ZRound20 = "pkr.ach.z_round20";
        public const string ZHollowKing = "pkr.ach.z_hollow_king";
        public const string ZTideMother = "pkr.ach.z_tide_mother";
        public const string ZIronWidow = "pkr.ach.z_iron_widow";
        public const string ZBriarQueen = "pkr.ach.z_briar_queen";
        public const string ZChapter1 = "pkr.ach.z_chapter1";
        public const string ZChapter2 = "pkr.ach.z_chapter2";
        public const string ZChapter3 = "pkr.ach.z_chapter3";
        public const string ZChapter4 = "pkr.ach.z_chapter4";
        public const string ZPrestige = "pkr.ach.z_prestige";
        public const string ZGoldFinish = "pkr.ach.z_gold";
        public const string ZWeekly = "pkr.ach.z_weekly";
        public const string ZStreak = "pkr.ach.z_streak7";
        public const string ZMedic = "pkr.ach.z_medic";

        /// <summary>Every achievement: id, title, points, how to earn it (also used for App Store Connect setup docs).</summary>
        public static readonly (string id, string title, int points, string howTo)[] All =
        {
            (FirstSteps, "First Steps", 10, "Clear your first story level."),
            (ShardHoarder, "Shard Hoarder", 20, "Collect every Star Shard in a story level."),
            (Untouchable, "Untouchable", 30, "Clear a story level without losing a life."),
            (SecretSeeker, "Secret Seeker", 20, "Find a secret area."),
            (Warden, "Gears Unwound", 40, "Defeat the Clockwork Warden."),
            (FirstContract, "Contract Signed", 20, "Complete a Shadow Contract."),
            (Ghost, "Ghost in the Gears", 50, "Finish a stealth level or contract without being spotted or taking anyone down."),
            (RelicHunter, "Relic Hunter", 40, "Collect every ancient relic in one contract."),
            (Cipher, "Cipher Breaker", 30, "Find a cipher scroll and reveal a secret contract."),
            (DojoChampion, "Dojo Champion", 20, "Win a Versus match."),
            (ZRound10, "Hold the Line", 20, "Reach round 10 in Last Stand."),
            (ZRound20, "Still Standing", 40, "Reach round 20 in Last Stand."),
            (ZHollowKing, "Crownbreaker", 40, "Defeat the Hollow King on Hollow Block."),
            (ZTideMother, "Low Tide", 40, "Defeat the Tide Mother on Salt Pier."),
            (ZIronWidow, "End of the Line", 40, "Defeat the Iron Widow on the Rail Yard."),
            (ZBriarQueen, "Thornless", 40, "Defeat the Briar Queen in Gravemoor Park."),
            (ZChapter1, "Patient Zero", 30, "Defeat the First Turned in Zombie Land."),
            (ZChapter2, "Blackwater", 30, "Defeat Mother Brine in Zombie Land chapter 2."),
            (ZChapter3, "Sublevel Sealed", 40, "Defeat the Curator in Zombie Land chapter 3."),
            (ZChapter4, "Wheels Up", 40, "Defeat the Marshal in Zombie Land chapter 4."),
            (ZPrestige, "Born Again", 30, "Prestige for the first time."),
            (ZGoldFinish, "Golden Gun", 20, "Earn a gold finish on any gun."),
            (ZWeekly, "Weekly Warrior", 20, "Finish a weekly challenge."),
            (ZStreak, "Orders Received", 30, "Finish every daily order 7 days in a row."),
            (ZMedic, "No One Left Behind", 20, "Revive teammates 25 times."),
        };

        /// <summary>The zombie achievements a career and Zombie Land save have earned (the save filters owned ones).</summary>
        public static List<string> ForZombies(ZombieCareer c, ZombieLandProgress zl)
        {
            var list = new List<string>();
            if (c != null)
            {
                c.Sanitize();
                int best = 0;
                for (int i = 0; i < c.bestKeys.Count && i < c.bestRounds.Count; i++)
                    if (!c.bestKeys[i].StartsWith("manor_")) best = System.Math.Max(best, c.bestRounds[i]);
                if (best >= 10) list.Add(ZRound10);
                if (best >= 20) list.Add(ZRound20);
                if (c.Has(LastStandRules.HiddenBossSecret)) list.Add(ZHollowKing);
                if (c.Has(LastStandMap.SaltPier.HiddenSecret)) list.Add(ZTideMother);
                if (c.Has(LastStandMap.RailYard.HiddenSecret)) list.Add(ZIronWidow);
                if (c.Has(LastStandMap.Gravemoor.HiddenSecret)) list.Add(ZBriarQueen);
                if (c.weeklyWins > 0) list.Add(ZWeekly);
                if (c.bestStreak >= 7) list.Add(ZStreak);
                if (c.revives >= 25) list.Add(ZMedic);
                if (c.prestige >= 1) list.Add(ZPrestige);
                if (WeaponMastery.Golds(c) >= 1) list.Add(ZGoldFinish);
            }
            if (zl != null)
            {
                if (zl.bossDefeated) list.Add(ZChapter1);
                if (zl.chapter2Done) list.Add(ZChapter2);
                if (zl.chapter3Done) list.Add(ZChapter3);
                if (zl.chapter4Done) list.Add(ZChapter4);
            }
            return list;
        }

        /// <summary>Achievements a finished level or contract earns (some may already be owned; the save filters them).</summary>
        public static List<string> ForClear(ClearInfo c)
        {
            var list = new List<string>();
            if (c.isContract)
            {
                list.Add(FirstContract);
                if (c.totalRelics > 0 && c.relics >= c.totalRelics) list.Add(RelicHunter);
                if (c.clueFound) list.Add(Cipher);
            }
            else
            {
                list.Add(FirstSteps);
                if (c.totalShards > 0 && c.shards >= c.totalShards) list.Add(ShardHoarder);
                if (c.deaths == 0) list.Add(Untouchable);
                if (!string.IsNullOrEmpty(c.levelId) && c.levelId.ToLowerInvariant().Contains("warden")) list.Add(Warden);
            }
            if (c.stealthRank == "GHOST") list.Add(Ghost);
            return list;
        }
    }
}
