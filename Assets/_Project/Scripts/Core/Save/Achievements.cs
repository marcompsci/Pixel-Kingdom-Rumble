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
        };

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
