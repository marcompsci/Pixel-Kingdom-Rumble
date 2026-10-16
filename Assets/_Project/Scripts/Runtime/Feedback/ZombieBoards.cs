using PKR.Core;

namespace PKR
{
    /// <summary>Zombie leaderboards on Game Center: best Last Stand (per map) and Manor rounds (Endless and Hardcore), Survivor XP.</summary>
    public static class ZombieBoards
    {
        /// <summary>The Manor has its own board; every Last Stand map has one (LastStandMap.Leaderboard).</summary>
        public static string BoardFor(string map) => map == "manor" ? Achievements.LeaderboardManor : LastStandMap.Get(map).Leaderboard;

        /// <summary>A run ended: post the round (Raids end at 10, so they don't count).</summary>
        public static void Submit(string map, SurvivalMode mode, int round)
        {
            if (mode == SurvivalMode.Raid || round <= 0) return;
            GameCenterService.SubmitScore(BoardFor(map), round);
        }

        public static void SubmitXp(int xp) => GameCenterService.SubmitScore(Achievements.LeaderboardSurvivorXp, xp);
    }
}
