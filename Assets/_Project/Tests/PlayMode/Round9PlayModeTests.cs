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
    /// Round 9: hero select status and RANDOM, ALL BADGES paging, the pause menu's tap-twice confirm, the shop's
    /// affordability lines and Story Stats SHARE.
    /// </summary>
    public class Round9PlayModeTests
    {
        int _shards;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (Services.Save != null) _shards = Services.Save.Data.starShards;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (Services.Save != null) Services.Save.Data.starShards = _shards;
            if (Services.State != null && Services.State.IsPaused) Services.State.Resume();
            GameStateManager.BaseTimeScale = 1f;
            Time.timeScale = 1f;
            var old = SceneManager.GetActiveScene();
            var empty = SceneManager.CreateScene("Round9Done" + Time.frameCount);
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

        static Button FindButton(string startsWith)
        {
            foreach (var b in Object.FindObjectsByType<Button>())
            {
                var t = b.GetComponentInChildren<Text>();
                if (b.isActiveAndEnabled && t != null && t.text.StartsWith(startsWith)) return b;
            }
            return null;
        }

        static bool HasText(string contains)
        {
            foreach (var t in Object.FindObjectsByType<Text>())
                if (t.isActiveAndEnabled && t.text != null && t.text.Contains(contains)) return true;
            return false;
        }

        [UnityTest]
        public IEnumerator HeroSelect_StatusAndRandom()
        {
            yield return Load(SceneIds.CharacterSelect);
            var ui = Object.FindAnyObjectByType<CharacterSelectUI>();
            Assert.IsNotNull(ui);
            yield return null;
            Assert.IsTrue(HasText("MASTERY"), "status shows mastery");
            Assert.IsNotNull(FindButton("RANDOM"));
            ui.PickRandom();
            yield return null;
            Assert.IsTrue(HasText("READY"), "random lands on a playable hero");
        }

        [UnityTest]
        public IEnumerator AllBadges_PagesThrough()
        {
            yield return Load(SceneIds.MainMenu);
            var panel = BadgesPanel.Show(new GameObject("BadgesHost").transform);
            yield return null;
            Assert.IsTrue(HasText("ALL BADGES"));
            Assert.IsTrue(HasText(Badges.All[0].name));
            panel.SetPage(1);
            yield return null;
            Assert.AreEqual(1, panel.Page);
            Assert.IsTrue(HasText(Badges.All[BadgesPanel.PerPage].name));
            panel.SetPage(-1);
            Assert.AreEqual(BadgesPanel.Pages - 1, panel.Page, "wraps around");
            panel.Close();
        }

        [UnityTest]
        public IEnumerator Pause_RestartAsksToTapAgain()
        {
            yield return Load(SceneIds.StoryTest);
            yield return new WaitForSeconds(3.5f);
            Services.State.Pause();
            yield return null;
            yield return null;
            var restart = FindButton("RESTART LEVEL");
            Assert.IsNotNull(restart);
            restart.onClick.Invoke();
            Assert.IsTrue(HasText("TAP AGAIN: RESTART LEVEL"), "the first tap only arms it");
            Assert.IsTrue(Services.State.IsPaused, "nothing happened yet");
        }

        [UnityTest]
        public IEnumerator Shop_ShowsWhatYouCanAfford()
        {
            yield return Load(SceneIds.MainMenu);
            Services.Save.Data.starShards = 1;
            var host = new GameObject("ShopHost");
            var root = UIFactory.CreateCanvas(host.transform, "ShopCanvas", 95, landscape: false, out _);
            var shop = host.AddComponent<ShopPanel>();
            shop.Open(root, null, null);
            shop.SetMode(true);
            yield return null;
            Assert.IsTrue(HasText("Saving for:") || HasText("MAXED"));
        }
    }
}
