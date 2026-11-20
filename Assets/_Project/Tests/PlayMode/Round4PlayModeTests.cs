using System.Collections;
using NUnit.Framework;
using PKR.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PKR.Tests
{
    /// <summary>
    /// Round 4 in the built scenes: the BRAVE one-heart challenge, BACK TO CHECKPOINT in the pause menu, Versus
    /// best-of-5 and RANDOM MATCHUP, profile badges, the recap card and the field guide count. Settings and save fields
    /// these tests touch are put back afterwards.
    /// </summary>
    public class Round4PlayModeTests
    {
        bool _brave, _breaks;
        int _profileSeen, _rounds;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (Services.Settings != null) { _brave = Services.Settings.Data.storyBrave; _breaks = Services.Settings.Data.breakReminder; }
            if (Services.Save != null) _profileSeen = Services.Save.Data.lifetime.profileSeen;
            _rounds = GameSession.VersusRoundsToWin;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (Services.Settings != null) { Services.Settings.Data.storyBrave = _brave; Services.Settings.Data.breakReminder = _breaks; }
            if (Services.Save != null) Services.Save.Data.lifetime.profileSeen = _profileSeen;
            GameSession.VersusRoundsToWin = _rounds;
            if (Services.State != null && Services.State.IsPaused) Services.State.Resume();
            GameStateManager.BaseTimeScale = 1f;
            Time.timeScale = 1f;
            var old = SceneManager.GetActiveScene();
            var empty = SceneManager.CreateScene("Round4Done" + Time.frameCount);
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
        public IEnumerator Brave_OneHeart_AndPauseBackToCheckpoint()
        {
            Assert.IsNotNull(Services.Settings);
            Services.Settings.Data.storyInvincible = false;
            Services.Settings.Data.storyBrave = true;
            yield return Load(SceneIds.StoryTest);
            var flow = Object.FindAnyObjectByType<LevelFlowController>();
            Assert.IsNotNull(flow);
            yield return null;
            Assert.IsTrue(flow.BraveRun, "BRAVE is on for this run");
            Assert.AreEqual(1, flow.PlayerHealth.MaxHealth, "one heart");

            Assert.IsNotNull(Services.State);
            Services.State.Pause();
            yield return null;
            yield return null;
            Assert.IsTrue(HasText("BACK TO CHECKPOINT"), "story pause offers the checkpoint");
            flow.BackToCheckpoint();
            yield return null;
            Assert.IsFalse(Services.State.IsPaused, "going back to the checkpoint resumes play");
        }

        [UnityTest]
        public IEnumerator Versus_BestOfFiveAndRandomMatchup()
        {
            GameSession.VersusRoundsToWin = 3;
            yield return Load(SceneIds.VersusSelect);
            Assert.IsNotNull(Object.FindAnyObjectByType<VersusSelectUI>());
            Assert.IsTrue(HasText("BEST OF 5"));
            Assert.IsTrue(HasText("RANDOM MATCHUP"));
        }

        [UnityTest]
        public IEnumerator Profile_ShowsBadges_AndMarksSeen()
        {
            yield return Load(SceneIds.MainMenu);
            Services.Save.Data.lifetime.profileSeen = 0;
            var host = new GameObject("ProfileHost");
            var root = UIFactory.CreateCanvas(host.transform, "ProfileCanvas", 95, landscape: false, out _);
            host.AddComponent<ProfilePanel>().Open(root, null, null);
            yield return null;
            Assert.IsTrue(HasText("BADGES"));
            Assert.IsTrue(HasText("Story challenges"));
            Assert.AreEqual(Badges.ProfileScore(Services.Save.Data), Services.Save.Data.lifetime.profileSeen);
        }

        [UnityTest]
        public IEnumerator RecapCard_ShowsAndCloses_FieldGuideCount()
        {
            yield return Load(SceneIds.MainMenu);
            Assert.IsTrue(HasText("TIP: "), "tip of the day on the menu");
            bool closed = false;
            var card = InfoCard.Show(new GameObject("Host").transform, "LAST WEEK", "sub", new[] { "Zombies put down:  12" }, onClose: () => closed = true);
            yield return null;
            Assert.IsTrue(HasText("LAST WEEK"));
            card.Close();
            yield return null;
            Assert.IsTrue(closed);
            Assert.AreEqual(0, Object.FindObjectsByType<InfoCard>().Length);

            var host = new GameObject("Zombies");
            var root = UIFactory.CreateCanvas(host.transform, "ZCanvas", 95, landscape: false, out _);
            host.AddComponent<ZombiesMenuPanel>().Open(root, null);
            yield return null;
            Assert.IsTrue(HasText("/" + ZombieFieldGuide.All.Length), "field guide shows a count");
        }
    }
}
