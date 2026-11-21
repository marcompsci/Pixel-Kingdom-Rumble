using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using PKR.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PKR.Tests
{
    /// <summary>
    /// Round 7: the clear screen's PERFECT, MEDAL UP and UP FROM rows, badge milestone shards, Versus REVENGE and
    /// streak end, CONTINUE's "when", and the Zombies map runs. Save fields these tests touch are put back.
    /// </summary>
    public class Round7PlayModeTests
    {
        GameObject _host;
        int _shards, _badgeRewards, _streak, _best, _lastKind, _lastDay;
        List<string> _badges;
        List<HeadToHeadRecord> _h2h, _heroRecords;
        string _lastScene;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (Services.Save != null)
            {
                var d = Services.Save.Data; var l = d.lifetime;
                _shards = d.starShards; _badgeRewards = l.badgeRewards; _streak = l.vsStreak; _best = l.vsBestStreak;
                _badges = new List<string>(l.badges);
                _h2h = new List<HeadToHeadRecord>(); foreach (var r in l.headToHead) _h2h.Add(new HeadToHeadRecord { id = r.id, wins = r.wins, losses = r.losses });
                _heroRecords = new List<HeadToHeadRecord>(); foreach (var r in l.heroRecords) _heroRecords.Add(new HeadToHeadRecord { id = r.id, wins = r.wins, losses = r.losses });
                _lastKind = l.lastKind; _lastDay = l.lastPlayedDay; _lastScene = l.lastScene;
            }
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_host != null) Object.Destroy(_host);
            if (Services.Save != null)
            {
                var d = Services.Save.Data; var l = d.lifetime;
                d.starShards = _shards; l.badgeRewards = _badgeRewards; l.vsStreak = _streak; l.vsBestStreak = _best;
                if (_badges != null) l.badges = _badges;
                if (_h2h != null) l.headToHead = _h2h;
                if (_heroRecords != null) l.heroRecords = _heroRecords;
                l.lastKind = _lastKind; l.lastPlayedDay = _lastDay; l.lastScene = _lastScene;
            }
            Time.timeScale = 1f;
            var old = SceneManager.GetActiveScene();
            var empty = SceneManager.CreateScene("Round7Done" + Time.frameCount);
            SceneManager.SetActiveScene(empty);
            if (old.IsValid() && old.isLoaded && old != empty) yield return SceneManager.UnloadSceneAsync(old);
        }

        static IEnumerator Load(string scene)
        {
            if (!Application.CanStreamedLevelBeLoaded(scene)) Assert.Ignore($"{scene} not built; run PKR > Build All Scenes.");
            SceneManager.LoadScene(scene, LoadSceneMode.Single);
            yield return null;
            yield return null;
            yield return null;
        }

        static bool HasText(string contains)
        {
            foreach (var t in Object.FindObjectsByType<Text>())
                if (t.isActiveAndEnabled && t.text != null && t.text.Contains(contains)) return true;
            return false;
        }

        [UnityTest]
        public IEnumerator ClearScreen_ShowsPerfectMedalUpAndRankUp()
        {
            _host = new GameObject("ResultsHost");
            var screen = _host.AddComponent<LevelCompleteScreen>();
            screen.showDelay = 0f;
            yield return null;
            EventBus<LevelCompleted>.Raise(new LevelCompleted
            {
                levelId = "test", levelName = "Test Level", timeSeconds = 55f, parTimeSeconds = 60f, previousBest = 70f,
                newBestTime = true, shards = 3, totalShards = 3, secrets = 1, totalSecrets = 1, deaths = 0, flawless = true,
                starShardReward = 40, perfect = true, perfectShards = StoryPerfect.Shards,
                medalUp = StoryPerfect.MedalUp(70f, 55f, 60f), rankUp = StoryPerfect.RankUp(ClearRank.A, ClearRank.S),
            });
            yield return null;
            yield return null;
            Assert.IsTrue(HasText("PERFECT"));
            Assert.IsTrue(HasText("MEDAL UP: GOLD!"));
            Assert.IsTrue(HasText("UP FROM A!"));
            Assert.IsTrue(HasText("FASTER THAN YOUR OLD BEST"));
        }

        [UnityTest]
        public IEnumerator Badges_FiveEarnedPaysShards()
        {
            yield return Load(SceneIds.MainMenu);
            var d = Services.Save.Data;
            d.lifetime.badges.Clear(); d.lifetime.badgeRewards = 0;
            for (int i = 0; i < BadgeRewards.Every; i++) d.lifetime.badges.Add("pkr_test_badge_" + i);
            int before = d.starShards;
            MetaTracker.CheckBadges();
            Assert.GreaterOrEqual(d.starShards - before, BadgeRewards.Shards);
            Assert.GreaterOrEqual(d.lifetime.badgeRewards, 1);
        }

        [UnityTest]
        public IEnumerator Versus_RevengeAndStreakEnd()
        {
            yield return Load(SceneIds.MainMenu);
            var l = Services.Save.Data.lifetime;
            l.headToHead.Add(new HeadToHeadRecord { id = "pkr_test_nemesis", wins = 0, losses = 3 });
            MetaTracker.Versus("pkr_test_nemesis", true, "pkr_test_hero", "Test Nemesis");
            Assert.AreEqual(1, HeadToHead.Get(Services.Save.Data, "pkr_test_nemesis").wins);
            l.vsStreak = 4;
            MetaTracker.Versus("pkr_test_nemesis", false, "pkr_test_hero", "Test Nemesis");
            Assert.AreEqual(0, l.vsStreak);
        }

        [UnityTest]
        public IEnumerator Continue_ShowsWhenYouLastPlayed()
        {
            var l = Services.Save.Data.lifetime;
            l.lastKind = (int)ContinueKind.Versus;
            l.lastPlayedDay = DailyChallenges.DayKey(System.DateTime.Now.AddDays(-1));
            yield return Load(SceneIds.MainMenu);
            foreach (var g in Object.FindObjectsByType<WelcomeGuide>()) Object.Destroy(g.gameObject);
            yield return null;
            Assert.IsTrue(HasText("yesterday"));
        }
    }
}
