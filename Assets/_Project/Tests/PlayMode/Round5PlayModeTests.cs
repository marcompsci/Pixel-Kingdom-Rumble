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
    /// Round 5 in the built scenes: Story attempts, world completion on level select, the menu's NEXT BADGE and tip
    /// cycling, profile badge progress and ABOUT, and Versus hero records. Save fields these tests touch are put back.
    /// </summary>
    public class Round5PlayModeTests
    {
        const string StoryLevelId = "sq_sunspire_test";
        int _attempts;
        bool _hadRecord;
        List<HeadToHeadRecord> _heroRecords, _headToHead;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (Services.Save != null)
            {
                var d = Services.Save.Data;
                var r = d.GetLevel(StoryLevelId);
                _hadRecord = r != null;
                _attempts = r != null ? r.attempts : 0;
                _heroRecords = Copy(d.lifetime.heroRecords);
                _headToHead = Copy(d.lifetime.headToHead);
            }
            yield return null;
        }

        static List<HeadToHeadRecord> Copy(List<HeadToHeadRecord> l)
        {
            var c = new List<HeadToHeadRecord>();
            foreach (var r in l) c.Add(new HeadToHeadRecord { id = r.id, wins = r.wins, losses = r.losses });
            return c;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (Services.Save != null)
            {
                var d = Services.Save.Data;
                var r = d.GetLevel(StoryLevelId);
                if (r != null)
                {
                    if (_hadRecord) r.attempts = _attempts;
                    else if (!r.completed) d.levels.Remove(r);
                }
                if (_heroRecords != null) d.lifetime.heroRecords = _heroRecords;
                if (_headToHead != null) d.lifetime.headToHead = _headToHead;
            }
            if (Services.State != null && Services.State.IsPaused) Services.State.Resume();
            GameStateManager.BaseTimeScale = 1f;
            Time.timeScale = 1f;
            var old = SceneManager.GetActiveScene();
            var empty = SceneManager.CreateScene("Round5Done" + Time.frameCount);
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

        static Text FindText(string contains)
        {
            foreach (var t in Object.FindObjectsByType<Text>())
                if (t.isActiveAndEnabled && t.text != null && t.text.Contains(contains)) return t;
            return null;
        }

        static bool HasText(string contains) => FindText(contains) != null;

        [UnityTest]
        public IEnumerator StoryLevel_CountsAnAttempt()
        {
            Assert.IsNotNull(Services.Save);
            yield return Load(SceneIds.StoryTest);
            var flow = Object.FindAnyObjectByType<LevelFlowController>();
            Assert.IsNotNull(flow);
            yield return null;
            var r = Services.Save.Data.GetLevel(flow.Level.id);
            Assert.IsNotNull(r, "starting a level makes its record");
            Assert.AreEqual((_hadRecord && flow.Level.id == StoryLevelId ? _attempts : 0) + 1, r.attempts);
        }

        [UnityTest]
        public IEnumerator LevelSelect_ShowsWorldCompletion()
        {
            yield return Load(SceneIds.LevelSelect);
            yield return null;
            Assert.IsTrue(HasText("% COMPLETE"));
        }

        [UnityTest]
        public IEnumerator MainMenu_NextBadge_TipCycles()
        {
            yield return Load(SceneIds.MainMenu);
            foreach (var g in Object.FindObjectsByType<WelcomeGuide>()) Object.Destroy(g.gameObject);
            foreach (var g in Object.FindObjectsByType<WhatsNewCard>()) Object.Destroy(g.gameObject);
            foreach (var g in Object.FindObjectsByType<InfoCard>()) Object.Destroy(g.gameObject);
            yield return null;
            Assert.IsTrue(HasText("NEXT BADGE") || HasText("Every badge earned"));
            var tip = FindText("TIP: ");
            Assert.IsNotNull(tip);
            string before = tip.text;
            tip.GetComponent<Button>().onClick.Invoke();
            Assert.AreNotEqual(before, tip.text, "tapping the tip shows the next one");
        }

        [UnityTest]
        public IEnumerator Profile_BadgeProgress_AndAbout()
        {
            yield return Load(SceneIds.MainMenu);
            var host = new GameObject("ProfileHost");
            var root = UIFactory.CreateCanvas(host.transform, "ProfileCanvas", 95, landscape: false, out _);
            var profile = host.AddComponent<ProfilePanel>();
            profile.Open(root, null, null);
            yield return null;
            Assert.IsTrue(HasText($"/{Badges.All.Length}"));
            Assert.IsTrue(HasText("Closest:") || HasText("Earned:"));
            Assert.IsTrue(HasText("ABOUT"));
            profile.ShowAbout();
            yield return null;
            Assert.IsTrue(HasText("No tracking"));
            Assert.IsTrue(HasText(Application.version));
        }

        [UnityTest]
        public IEnumerator Versus_RecordsYourHero()
        {
            yield return Load(SceneIds.MainMenu);
            MetaTracker.Versus("pkr_test_rival", true, "pkr_test_hero");
            MetaTracker.Versus("pkr_test_rival", false, "pkr_test_hero");
            var r = HeroRecords.Get(Services.Save.Data, "pkr_test_hero");
            Assert.IsNotNull(r);
            Assert.AreEqual(1, r.wins);
            Assert.AreEqual(1, r.losses);
        }
    }
}
