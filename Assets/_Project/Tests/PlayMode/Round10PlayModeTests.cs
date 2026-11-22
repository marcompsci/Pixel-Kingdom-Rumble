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
    /// Round 10: the LAST HEART hint and FLAWLESS SO FAR, the profile title, ALL BADGES' TO EARN filter and the
    /// training lab's saved record. Settings these tests touch are put back.
    /// </summary>
    public class Round10PlayModeTests
    {
        bool _brave, _invincible, _assist;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (Services.Settings != null)
            {
                var d = Services.Settings.Data;
                _brave = d.storyBrave; _invincible = d.storyInvincible; _assist = d.assistMode;
                d.storyBrave = false; d.storyInvincible = false;
            }
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (Services.Settings != null)
            {
                var d = Services.Settings.Data;
                d.storyBrave = _brave; d.storyInvincible = _invincible; d.assistMode = _assist;
            }
            if (Services.State != null && Services.State.IsPaused) Services.State.Resume();
            GameStateManager.BaseTimeScale = 1f;
            Time.timeScale = 1f;
            var old = SceneManager.GetActiveScene();
            var empty = SceneManager.CreateScene("Round10Done" + Time.frameCount);
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
        public IEnumerator Story_FlawlessSoFar_ThenLastHeart()
        {
            yield return Load(SceneIds.StoryTest);
            var flow = Object.FindAnyObjectByType<LevelFlowController>();
            Assert.IsNotNull(flow);
            yield return new WaitForSeconds(5.5f);
            Services.State.Pause();
            yield return null;
            yield return null;
            Assert.IsTrue(HasText("FLAWLESS SO FAR"));
            Services.State.Resume();
            yield return null;
            var hp = flow.PlayerHealth;
            if (hp.MaxHealth < 2) Assert.Ignore("needs at least two hearts");
            hp.TakeDirectDamage(hp.Health - 1);
            yield return null;
            yield return null;
            Assert.AreEqual(1, hp.Health);
            Assert.IsTrue(HasText("LAST HEART"));
        }

        [UnityTest]
        public IEnumerator Profile_ShowsTitle_BadgesFilter()
        {
            yield return Load(SceneIds.MainMenu);
            var host = new GameObject("ProfileHost");
            var root = UIFactory.CreateCanvas(host.transform, "ProfileCanvas", 95, landscape: false, out _);
            host.AddComponent<ProfilePanel>().Open(root, null, null);
            yield return null;
            Assert.IsTrue(HasText(RankTitle.For(Services.Save.Data.lifetime.badges.Count)));
            var panel = BadgesPanel.Show(new GameObject("BadgesHost").transform);
            yield return null;
            Assert.IsTrue(HasText("TO EARN"));
            panel.SetUnearnedOnly(true);
            yield return null;
            Assert.IsTrue(panel.UnearnedOnly);
            Assert.IsTrue(HasText("SHOW ALL"));
            panel.Close();
        }

        [UnityTest]
        public IEnumerator TrainingLab_ShowsSavedRecord()
        {
            yield return Load(SceneIds.ArenaTest);
            var a = Object.FindAnyObjectByType<ArenaMatchController>();
            a.grantRewards = false;
            a.StartMatch(new ArenaMatchConfig { mode = MatchMode.Training, botCount = 1, trainingAct = TrainingAct.Walk });
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsTrue(HasText("RECORD"));
            a.EndMatch();
        }
    }
}
