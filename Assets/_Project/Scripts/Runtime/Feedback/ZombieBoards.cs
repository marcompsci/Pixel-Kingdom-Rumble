using PKR.Core;

namespace PKR
{
    /// <summary>Zombie leaderboards on Game Center: best Last Stand and Manor rounds (Endless and Hardcore), Survivor XP.</summary>
    public static class ZombieBoards
    {
        public static string BoardFor(string map) => map == LastStandRules.MapId ? Achievements.LeaderboardLastStand : Achievements.LeaderboardManor;

        /// <summary>A run ended: post the round (Raids end at 10, so they don't count).</summary>
        public static void Submit(string map, SurvivalMode mode, int round)
        {
            if (mode == SurvivalMode.Raid || round <= 0) return;
            GameCenterService.SubmitScore(BoardFor(map), round);
        }

        public static void SubmitXp(int xp) => GameCenterService.SubmitScore(Achievements.LeaderboardSurvivorXp, xp);
    }
}
