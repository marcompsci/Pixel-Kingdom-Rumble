using System.Runtime.InteropServices;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Game Center: signs the player in at launch, awards achievements (Achievements.ForClear etc.) and posts the
    /// Shadow Contracts total to the leaderboard. Earned achievements are stored in the save first, so anything earned
    /// offline or before sign-in is re-sent once the player is signed in. In the Editor and on other platforms it only
    /// records to the save and logs.
    /// </summary>
    public class GameCenterService : MonoBehaviour
    {
        public static GameCenterService Instance { get; private set; }

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void PKR_GC_Authenticate();
        [DllImport("__Internal")] static extern int PKR_GC_AuthState();
        [DllImport("__Internal")] static extern void PKR_GC_ReportScore(string leaderboardId, long score);
        [DllImport("__Internal")] static extern void PKR_GC_ReportAchievement(string achievementId, double percent);
        [DllImport("__Internal")] static extern void PKR_GC_ShowDashboard();
#endif

        bool _synced;

        /// <summary>Signed in to Game Center (online co-op needs it).</summary>
        public static bool SignedIn { get; private set; }
        float _nextPoll;

        public static bool Available
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        void Awake() => Instance = this;

        void OnDestroy() { if (Instance == this) Instance = null; }

        void OnEnable()
        {
            EventBus<LevelCompleted>.Subscribe(OnLevelCompleted);
            EventBus<SecretFound>.Subscribe(OnSecret);
            EventBus<VersusMatchEnded>.Subscribe(OnVersus);
        }

        void OnDisable()
        {
            EventBus<LevelCompleted>.Unsubscribe(OnLevelCompleted);
            EventBus<SecretFound>.Unsubscribe(OnSecret);
            EventBus<VersusMatchEnded>.Unsubscribe(OnVersus);
        }

        void Start()
        {
#if UNITY_IOS && !UNITY_EDITOR
            PKR_GC_Authenticate();
#endif
        }

        void Update()
        {
            if (_synced || Time.unscaledTime < _nextPoll) return;
            _nextPoll = Time.unscaledTime + 2f;
#if UNITY_IOS && !UNITY_EDITOR
            int state = PKR_GC_AuthState();
            if (state == 2) { _synced = true; return; } // declined or unavailable: try again next launch
            if (state != 1) return;
            _synced = true;
            SignedIn = true;
            SyncAll();
#endif
        }

        /// <summary>Re-sends everything in the save (after sign-in): earned achievements and the contracts total.</summary>
        void SyncAll()
        {
            var save = Services.Save != null ? Services.Save.Data : null;
            if (save == null) return;
            foreach (var id in save.achievements) Send(id);
            int total = MissionBoard.TotalScore(save);
            if (total > 0) SendScore(Achievements.LeaderboardContracts, total);
            if (save.rumble != null && save.rumble.bestRating > 0) SendScore(Achievements.LeaderboardRumble, save.rumble.bestRating);
            if (save.zombieCareer != null)
            {
                foreach (var map in LastStandMap.All)
                {
                    int best = System.Math.Max(save.zombieCareer.Best(map.Id, SurvivalMode.Endless), save.zombieCareer.Best(map.Id, SurvivalMode.Hardcore));
                    if (best > 0) SendScore(map.Leaderboard, best);
                }
                int manor = System.Math.Max(save.zombieBestRound, System.Math.Max(save.zombieCareer.Best("manor", SurvivalMode.Endless), save.zombieCareer.Best("manor", SurvivalMode.Hardcore)));
                if (manor > 0) SendScore(Achievements.LeaderboardManor, manor);
                if (save.zombieCareer.xp > 0) SendScore(Achievements.LeaderboardSurvivorXp, save.zombieCareer.xp);
            }
        }

        /// <summary>Posts a score to any leaderboard (Game Center keeps each player's best).</summary>
        public static void SubmitScore(string board, long score)
        {
            if (score > 0) SendScore(board, score);
        }

        /// <summary>Posts the Rumble Rating (Game Center keeps each player's best).</summary>
        public static void SubmitRumble(int rating)
        {
            if (rating > 0) SendScore(Achievements.LeaderboardRumble, rating);
        }

        public void Earn(string id)
        {
            var save = Services.Save;
            if (save == null || !save.Data.EarnAchievement(id)) return;
            save.SaveNow();
            Debug.Log("[PKR GameCenter] Achievement earned: " + id);
            Send(id);
        }

        static void Send(string id)
        {
#if UNITY_IOS && !UNITY_EDITOR
            PKR_GC_ReportAchievement(id, 100.0);
#endif
        }

        static void SendScore(string board, long score)
        {
#if UNITY_IOS && !UNITY_EDITOR
            PKR_GC_ReportScore(board, score);
#else
            Debug.Log($"[PKR GameCenter] Score {score} -> {board}");
#endif
        }

        /// <summary>Opens the Game Center dashboard (leaderboards and achievements); signs in first if needed.</summary>
        public static void ShowDashboard()
        {
#if UNITY_IOS && !UNITY_EDITOR
            PKR_GC_ShowDashboard();
#else
            Debug.Log("[PKR GameCenter] Dashboard is only available on iPhone.");
#endif
        }

        void OnLevelCompleted(LevelCompleted e)
        {
            var earned = Achievements.ForClear(new ClearInfo
            {
                levelId = e.levelId, isContract = e.isContract, stealthRank = e.stealthRank,
                shards = e.shards, totalShards = e.totalShards, deaths = e.deaths,
                relics = e.relics, totalRelics = e.totalRelics, clueFound = e.clueFound
            });
            foreach (var id in earned) Earn(id);
            if (e.isContract && Services.Save != null)
                SendScore(Achievements.LeaderboardContracts, MissionBoard.TotalScore(Services.Save.Data));
        }

        void OnSecret(SecretFound e) => Earn(Achievements.SecretSeeker);

        void OnVersus(VersusMatchEnded e) { if (e.winner == 0) Earn(Achievements.DojoChampion); }
    }
}
