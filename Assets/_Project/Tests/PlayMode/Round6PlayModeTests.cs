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
    /// Round 6 in the built scenes: Versus streaks, the menu greeting and its setting, PROFILE's badge count, the pause
    /// menu's TRY count, the Zombies menu's last run and the ASSISTS toggle. Save and settings fields are put back.
    /// </summary>
    public class Round6PlayModeTests
    {
        int _streak, _bestStreak;
        bool _menuTips, _hadLevel;
        int _attempts;
        List<HeadToHeadRecord> _h2h, _heroRecords;
        List<StandRunRecord> _runs;
        const string StoryLevelId = "sq_sunspire_test";

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (Services.Save != null)
            {
                var l = Services.Save.Data.lifetime;
                _streak = l.vsStreak; _bestStreak = l.vsBestStreak;
                _h2h = new List<HeadToHeadRecord>(); foreach (var r in l.headToHead) _h2h.Add(new HeadToHeadRecord { id = r.id, wins = r.wins, losses = r.losses });
                _heroRecords = new List<HeadToHeadRecord>(); foreach (var r in l.heroRecords) _heroRecords.Add(new HeadToHeadRecord { id = r.id, wins = r.wins, losses = r.losses });
                _runs = new List<StandRunRecord>(l.standRuns);
                var rec = Services.Save.Data.GetLevel(StoryLevelId);
                _hadLevel = rec != null; _attempts = rec != null ? rec.attempts : 0;
            }
            if (Services.Settings != null) _menuTips = Services.Settings.Data.menuTips;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (Services.Save != null)
            {
                var d = Services.Save.Data; var l = d.lifetime;
                l.vsStreak = _streak; l.vsBestStreak = _bestStreak;
                if (_h2h != null) l.headToHead = _h2h;
                if (_heroRecords != null) l.heroRecords = _heroRecords;
                if (_runs != null) l.standRuns = _runs;
                var rec = d.GetLevel(StoryLevelId);
                if (rec != null) { if (_hadLevel) rec.attempts = _attempts; else if (!rec.completed) d.levels.Remove(rec); }
            }
            if (Services.Settings != null) Services.Settings.Data.menuTips = _menuTips;
            if (Services.State != null && Services.State.IsPaused) Services.State.Resume();
            GameStateManager.BaseTimeScale = 1f;
            Time.timeScale = 1f;
            var old = SceneManager.GetActiveScene();
            var empty = SceneManager.CreateScene("Round6Done" + Time.frameCount);
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
        public IEnumerator Versus_StreakCountsAndResets()
        {
            yield return Load(SceneIds.MainMenu);
            var l = Services.Save.Data.lifetime;
            l.vsStreak = 0;
            for (int i = 0; i < 3; i++) MetaTracker.Versus("pkr_test_rival", true, "pkr_test_hero", "Test Rival");
            Assert.AreEqual(3, l.vsStreak);
            Assert.GreaterOrEqual(l.vsBestStreak, 3);
            MetaTracker.Versus("pkr_test_rival", false, "pkr_test_hero", "Test Rival");
            Assert.AreEqual(0, l.vsStreak);
        }

        [UnityTest]
        public IEnumerator MainMenu_GreetingAndProfileCount_SettingHidesThem()
        {
            Services.Settings.Data.menuTips = true;
            yield return Load(SceneIds.MainMenu);
            Assert.IsTrue(HasText(RankTitle.For(Services.Save.Data.lifetime.badges.Count)), "a greeting with your title");
            Assert.IsTrue(HasText("PROFILE"));
            Assert.IsTrue(HasText("TIP: "));
            Services.Settings.Data.menuTips = false;
            yield return Load(SceneIds.MainMenu);
            Assert.IsFalse(HasText("TIP: "), "menu tips off");
            Assert.IsFalse(HasText("NEXT BADGE"));
        }

        [UnityTest]
        public IEnumerator Pause_ShowsTryCount()
        {
            Services.Save.Data.GetOrAddLevel(StoryLevelId).attempts = 4;
            yield return Load(SceneIds.StoryTest);
            yield return null;
            Services.State.Pause();
            yield return null;
            yield return null;
            Assert.IsTrue(HasText("TRY 5"));
        }

        [UnityTest]
        public IEnumerator ZombiesMenu_ShowsLastRun()
        {
            yield return Load(SceneIds.MainMenu);
            StandHistory.Add(Services.Save.Data, new StandRunRecord { map = LastStandMap.All[0].Id, round = 7 });
            var host = new GameObject("Zombies");
            var root = UIFactory.CreateCanvas(host.transform, "ZCanvas", 95, landscape: false, out _);
            host.AddComponent<ZombiesMenuPanel>().Open(root, null);
            yield return null;
            Assert.IsTrue(HasText("last run: round 7"));
        }

        [UnityTest]
        public IEnumerator Settings_AssistsHasMenuTipsToggle()
        {
            yield return Load(SceneIds.MainMenu);
            var host = new GameObject("SettingsHost");
            var root = UIFactory.CreateCanvas(host.transform, "SettingsCanvas", 95, landscape: true, out _);
            var panel = host.AddComponent<SettingsPanel>();
            panel.Open(root, landscape: true, onBack: null, onEditLayout: null);
            yield return null;
            Button tab = null;
            foreach (var b in Object.FindObjectsByType<Button>())
                if (b.GetComponentInChildren<Text>() != null && b.GetComponentInChildren<Text>().text == "ASSISTS") tab = b;
            Assert.IsNotNull(tab);
            tab.onClick.Invoke();
            yield return null;
            Assert.IsTrue(HasText("Menu tips & goals"));
        }
    }
}
