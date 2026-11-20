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
    /// The 20-phase round in the built scenes: CONTINUE and PROFILE on the main menu, WHAT'S NEW, the ASSISTS settings
    /// page, the invincible assist and fall markers in Story Quest, and the training lab's RESET. Settings and the save
    /// fields these tests touch are put back afterwards.
    /// </summary>
    public class PlayerMetaPlayModeTests
    {
        bool _invincible;
        int _lastKind;
        string _lastScene, _lastLevel;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (Services.Settings != null) _invincible = Services.Settings.Data.storyInvincible;
            if (Services.Save != null)
            {
                var l = Services.Save.Data.lifetime;
                _lastKind = l.lastKind; _lastScene = l.lastScene; _lastLevel = l.lastLevelId;
            }
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (Services.Settings != null) Services.Settings.Data.storyInvincible = _invincible;
            if (Services.Save != null)
            {
                var l = Services.Save.Data.lifetime;
                l.lastKind = _lastKind; l.lastScene = _lastScene; l.lastLevelId = _lastLevel;
            }
            GameStateManager.BaseTimeScale = 1f;
            Time.timeScale = 1f;
            var old = SceneManager.GetActiveScene();
            var empty = SceneManager.CreateScene("MetaDone" + Time.frameCount);
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
        public IEnumerator StoryLevel_IsRememberedForContinue_InvincibleKeepsHearts_FallsAreMarked()
        {
            Assert.IsNotNull(Services.Settings);
            Services.Settings.Data.storyInvincible = true;
            yield return Load(SceneIds.StoryTest);
            var flow = Object.FindAnyObjectByType<LevelFlowController>();
            Assert.IsNotNull(flow);
            yield return null;
            Assert.AreEqual((int)ContinueKind.StoryLevel, Services.Save.Data.lifetime.lastKind);
            Assert.AreEqual(SceneIds.StoryTest, Services.Save.Data.lifetime.lastScene);
            int hp = flow.PlayerHealth.Health;
            flow.PlayerHealth.TakeDirectDamage(1);
            Assert.AreEqual(hp, flow.PlayerHealth.Health, "invincible: no heart lost");
            Assert.IsTrue(flow.Run.AssistSpeed, "an assisted run sets no records");
            Services.Settings.Data.storyInvincible = false;
            flow.PlayerHealth.keepHealth = false;
            int before = FallMemory.Get(Services.Save.Data, "sq_sunspire_test")?.xs.Count ?? 0;
            flow.PlayerHealth.TakeDirectDamage(flow.PlayerHealth.Health); // dies: remembered as a fall spot
            yield return null;
            Assert.IsNotNull(FallMemory.Get(Services.Save.Data, "sq_sunspire_test"), "the fall spot is remembered");
            Assert.GreaterOrEqual(FallMemory.Get(Services.Save.Data, "sq_sunspire_test").xs.Count, System.Math.Min(before + 1, FallMemory.PerLevel));
        }

        [UnityTest]
        public IEnumerator MainMenu_ContinueAndProfile()
        {
            Services.Save.Data.lifetime.lastKind = (int)ContinueKind.ArenaClash;
            yield return Load(SceneIds.MainMenu);
            foreach (var g in Object.FindObjectsByType<WelcomeGuide>()) Object.Destroy(g.gameObject);
            foreach (var g in Object.FindObjectsByType<WhatsNewCard>()) Object.Destroy(g.gameObject);
            yield return null;
            Assert.IsTrue(HasText("CONTINUE"), "CONTINUE when something was played");
            Assert.IsTrue(HasText("PROFILE"));
            var host = new GameObject("ProfileHost");
            var root = UIFactory.CreateCanvas(host.transform, "ProfileCanvas", 95, landscape: false, out _);
            host.AddComponent<ProfilePanel>().Open(root, null, null);
            yield return null;
            Assert.IsTrue(HasText("HERO MASTERY"));
            Assert.IsTrue(HasText("Time played"));
            Assert.IsTrue(HasText("Daily gift streak"));
        }

        [UnityTest]
        public IEnumerator WhatsNew_ShowsAndCloses()
        {
            yield return Load(SceneIds.MainMenu);
            var card = WhatsNewCard.Show(new GameObject("Host").transform);
            yield return null;
            Assert.IsTrue(HasText("WHAT'S NEW"));
            card.Close();
            yield return null;
            Assert.AreEqual(0, Object.FindObjectsByType<WhatsNewCard>().Length);
            Assert.AreEqual(WhatsNew.Version, Services.Save.Data.lifetime.whatsNewSeen);
        }

        [UnityTest]
        public IEnumerator Settings_AssistsPage()
        {
            yield return Load(SceneIds.MainMenu);
            var host = new GameObject("SettingsHost");
            var root = UIFactory.CreateCanvas(host.transform, "SettingsCanvas", 95, landscape: true, out _);
            var panel = host.AddComponent<SettingsPanel>();
            panel.Open(root, landscape: true, onBack: null, onEditLayout: null);
            yield return null;
            Assert.IsTrue(HasText("ASSISTS"), "a third settings tab");
            Button tab = null;
            foreach (var b in Object.FindObjectsByType<Button>())
                if (b.GetComponentInChildren<Text>() != null && b.GetComponentInChildren<Text>().text == "ASSISTS") tab = b;
            Assert.IsNotNull(tab);
            tab.onClick.Invoke();
            yield return null;
            Assert.IsTrue(HasText("Comfort preset"));
            Assert.IsTrue(HasText("Story: invincible"));
            Assert.IsTrue(HasText("RESET HINTS"));
        }

        [UnityTest]
        public IEnumerator TrainingLab_ResetPutsEveryoneBack()
        {
            yield return Load(SceneIds.ArenaTest);
            var a = Object.FindAnyObjectByType<ArenaMatchController>();
            a.grantRewards = false;
            a.StartMatch(new ArenaMatchConfig { mode = MatchMode.Training, botCount = 1, trainingAct = TrainingAct.Walk });
            yield return null;
            var dummy = a.Fighters[1];
            Vector2 start = dummy.go.transform.position;
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.IsTrue(HasText("RESET"));
            Object.FindAnyObjectByType<TrainingLab>().ResetLab();
            yield return null;
            Assert.Less(Vector2.Distance(dummy.go.transform.position, start), 1f, "back at the start spot");
            a.EndMatch();
        }
    }
}
