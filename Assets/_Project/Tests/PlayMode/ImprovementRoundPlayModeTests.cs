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
    /// The November improvement round in the built scenes: Arena Clash items and CPU hero picks, the training lab, the
    /// Versus ladder, Story Quest extras (game speed, FLAWLESS, the time-trial ghost), the welcome guide, the zombie
    /// FIELD GUIDE and ALSO TODAY. Rewards go to throwaway saves, never the real one.
    /// </summary>
    public class ImprovementRoundPlayModeTests
    {
        int _speedBefore = 100;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (Services.Settings != null) _speedBefore = Services.Settings.Data.gameSpeed;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            GameSession.Ladder = null;
            if (Services.Settings != null) Services.Settings.Data.gameSpeed = _speedBefore;
            GameStateManager.BaseTimeScale = 1f;
            Time.timeScale = 1f;
            var old = SceneManager.GetActiveScene();
            var empty = SceneManager.CreateScene("RoundDone" + Time.frameCount);
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

        static int Count(string prefix)
        {
            int n = 0;
            foreach (var t in Object.FindObjectsByType<Transform>()) if (t.name.StartsWith(prefix)) n++;
            return n;
        }

        static bool HasText(string contains)
        {
            foreach (var t in Object.FindObjectsByType<Text>())
                if (t.isActiveAndEnabled && t.text != null && t.text.Contains(contains)) return true;
            return false;
        }

        static ArenaMatchController Arena()
        {
            var a = Object.FindAnyObjectByType<ArenaMatchController>();
            Assert.IsNotNull(a);
            a.grantRewards = false;
            return a;
        }

        [UnityTest]
        public IEnumerator ArenaClash_ItemsOnlyWhenTurnedOn()
        {
            yield return Load(SceneIds.ArenaTest);
            var a = Arena();
            a.StartMatch(new ArenaMatchConfig { mode = MatchMode.Stock, botCount = 2, stocks = 3, items = false });
            yield return null;
            Assert.AreEqual(0, Count("ItemBox"));
            a.StartMatch(new ArenaMatchConfig { mode = MatchMode.Stock, botCount = 2, stocks = 3, items = true });
            yield return null;
            Assert.AreEqual(3, Count("ItemBox"), "ITEMS: ON drops the boxes into a normal match");
            Assert.IsFalse(a.Config.Rated);
            Assert.IsFalse(HasText("YOUR BALLOONS"), "no party HUD in a normal match");
            yield return new WaitForSecondsRealtime(1f);
            a.EndMatch();
        }

        [UnityTest]
        public IEnumerator ArenaClash_EveryCpuFightsAsTheChosenHero()
        {
            yield return Load(SceneIds.ArenaTest);
            var a = Arena();
            CharacterDefinition pick = null;
            foreach (var h in a.Roster.heroes) if (h != null && h.playableInThisBuild && a.Roster.IsSelectable(h)) { pick = h; break; }
            Assert.IsNotNull(pick);
            a.StartMatch(new ArenaMatchConfig { mode = MatchMode.Stock, botCount = 3, stocks = 3, cpuCharacterId = pick.id });
            yield return null;
            for (int i = 1; i < a.Fighters.Count; i++)
                Assert.AreEqual(pick.id, a.Fighters[i].go.GetComponent<FighterVisual>().Character.id);
            a.EndMatch();
        }

        [UnityTest]
        public IEnumerator Training_LabCounts_AndWalkingDummyMoves()
        {
            yield return Load(SceneIds.ArenaTest);
            var a = Arena();
            a.StartMatch(new ArenaMatchConfig { mode = MatchMode.Training, botCount = 1, trainingAct = TrainingAct.Walk });
            yield return null;
            var lab = Object.FindAnyObjectByType<TrainingLab>();
            Assert.IsNotNull(lab);
            Assert.IsTrue(lab.Running);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsTrue(HasText("TRAINING LAB"));
            Assert.IsTrue(HasText("COMBO"));
            var dummy = a.Fighters[1].go;
            float x0 = dummy.transform.position.x;
            yield return new WaitForSecondsRealtime(2f);
            Assert.Greater(Mathf.Abs(dummy.transform.position.x - x0), 0.5f, "WALK paces the stage");
            a.EndMatch();
            Assert.IsFalse(lab.Running);
        }

        [UnityTest]
        public IEnumerator VersusLadder_FirstRungIsTheLaddersRival()
        {
            var ids = new System.Collections.Generic.List<string> { "nova", "brick", "luma" };
            GameSession.Ladder = VersusLadder.Build("nova", ids, 3);
            yield return Load(SceneIds.VersusStage);
            var vs = Object.FindAnyObjectByType<VersusController>();
            Assert.IsNotNull(vs);
            vs.grantRewards = false;
            float end = Time.realtimeSinceStartup + 6f;
            while (vs.Opponent == null && Time.realtimeSinceStartup < end) yield return null;
            Assert.IsNotNull(vs.Opponent);
            Assert.AreEqual(GameSession.Ladder.CurrentRival, vs.Opponent.def.id);
        }

        [UnityTest]
        public IEnumerator Story_GameSpeed_Flawless_AndGhost()
        {
            if (Services.Settings != null) Services.Settings.Data.gameSpeed = 80;
            yield return Load(SceneIds.StoryTest);
            var flow = Object.FindAnyObjectByType<LevelFlowController>();
            Assert.IsNotNull(flow);
            Assert.IsTrue(flow.IsStoryLevel);
            yield return null;
            Assert.AreEqual(0.8f, Time.timeScale, 0.01f, "Story game speed 80%");
            Assert.IsTrue(flow.Run.AssistSpeed, "a slowed run doesn't set a best time");
            var extras = flow.GetComponent<StoryRunExtras>();
            Assert.IsNotNull(extras);
            yield return new WaitForSeconds(1.2f);
            Assert.IsFalse(extras.TookHit);
            var scratch = SaveData.CreateNew();
            var r = extras.OnCleared(scratch, "sq_sunspire_test", 9f, assistSpeed: false);
            Assert.IsTrue(r.flawless);
            Assert.AreEqual(StoryFlawless.Shards, r.flawlessShards);
            Assert.IsTrue(r.newGhost, "a first clear saves a ghost");
            Assert.IsNotNull(StoryGhosts.Find(scratch, "sq_sunspire_test"));
            flow.PlayerHealth.TakeDirectDamage(1);
            yield return null;
            yield return null;
            Assert.IsTrue(extras.TookHit, "losing a heart breaks FLAWLESS");
            Services.Settings.Data.gameSpeed = 100;
            flow.ApplyGameSpeed();
            Assert.AreEqual(1f, Time.timeScale, 0.01f);
        }

        [UnityTest]
        public IEnumerator WelcomeGuide_ThreeCards_ThenCloses()
        {
            yield return Load(SceneIds.MainMenu);
            foreach (var old in Object.FindObjectsByType<WelcomeGuide>()) Object.Destroy(old.gameObject);
            yield return null;
            bool story = false, done = false;
            var g = WelcomeGuide.Show(new GameObject("Host").transform, () => story = true, () => done = true);
            yield return null;
            Assert.IsTrue(HasText("WELCOME, HERO!"));
            Assert.IsTrue(HasText("1 / 3"));
            g.Finish(story: false);
            yield return null;
            Assert.IsTrue(done);
            Assert.IsFalse(story);
            Assert.AreEqual(0, Object.FindObjectsByType<WelcomeGuide>().Length);
        }

        [UnityTest]
        public IEnumerator FieldGuide_AndAlsoToday_Pages()
        {
            yield return Load(SceneIds.MainMenu);
            var host = new GameObject("Pages");
            var root = UIFactory.CreateCanvas(host.transform, "PagesCanvas", 95, landscape: false, out _);
            var zombies = host.AddComponent<ZombiesMenuPanel>();
            zombies.Open(root, null);
            zombies.OpenFieldGuide();
            yield return null;
            Assert.IsTrue(HasText("FIELD GUIDE"));
            Assert.IsTrue(HasText("found"));
            var events = host.AddComponent<EventMenuPanel>();
            events.OpenDaily(root, null);
            yield return null;
            Assert.IsTrue(HasText("ALSO TODAY"));
            Assert.IsTrue(HasText("DAILY RUMBLE"));
            Assert.IsTrue(HasText("DAILY ORDERS"));
        }
    }
}
