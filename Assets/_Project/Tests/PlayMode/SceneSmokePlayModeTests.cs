using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PKR.Tests
{
    /// <summary>
    /// Smoke tests for the generated scenes (PKR > Build All Scenes must have run; otherwise these are skipped).
    /// Each scene is loaded and left running for a few seconds; any exception or error log fails the test
    /// (Unity Test Framework does that automatically). The Story level also runs the hero to the right so
    /// movement, enemies and pickups tick for real.
    /// Note: loading a Story level records its Codex place in the Editor's save file.
    /// </summary>
    public class SceneSmokePlayModeTests
    {
        static readonly string[] Scenes =
        {
            SceneIds.Boot, SceneIds.MainMenu, SceneIds.CharacterSelect, SceneIds.LevelSelect,
            SceneIds.StoryTest, SceneIds.BossTest, SceneIds.ArenaTest, SceneIds.VersusSelect, SceneIds.VersusStage,
            SceneIds.SunspireHeights, SceneIds.GearfallCaverns, SceneIds.RooftopRun, SceneIds.NightMarketHeist,
            SceneIds.MissionBoard, "MS_FirstLight", "MS_ClocktowerShadow"
        };

        [TearDown]
        public void TearDown() => Time.timeScale = 1f;

        IEnumerator Load(string scene)
        {
            if (!Application.CanStreamedLevelBeLoaded(scene))
                Assert.Ignore($"{scene} is not in Build Settings; run PKR > Build All Scenes first.");
            SceneManager.LoadScene(scene, LoadSceneMode.Single);
            yield return null;
            yield return null;
            Assert.AreEqual(scene, SceneManager.GetActiveScene().name);
        }

        [UnityTest]
        public IEnumerator Scene_LoadsAndRunsWithoutErrors([ValueSource(nameof(Scenes))] string scene)
        {
            yield return Load(scene);
            yield return new WaitForSecondsRealtime(3f);
        }

        [UnityTest]
        public IEnumerator StoryLevel_HeroRunsRightWithoutErrors()
        {
            yield return Load(SceneIds.StoryTest);
            var flow = LevelFlowController.Current;
            Assert.IsNotNull(flow, "the Story level has a LevelFlowController");
            var hero = Object.FindAnyObjectByType<PlayerInputRouter>();
            Assert.IsNotNull(hero, "the Story level has a player");
            var motor = hero.GetComponent<PlatformerMotor2D>();
            hero.enabled = false; // drive the motor directly
            float startX = motor.transform.position.x;

            for (int i = 0; i < 200; i++)
            {
                motor.Intent.Move = new Vector2(1f, 0f);
                if (i % 40 == 0) motor.Intent.Jump.Press(Time.time);
                yield return new WaitForFixedUpdate();
            }
            Assert.Greater(motor.transform.position.x, startX + 2f, "the hero moved right");
            Assert.IsNotNull(flow.Run);
            Assert.Greater(flow.Run.ElapsedSeconds, 0f, "the level timer runs");
        }

        // Props must survive being saved into a scene (a MonoBehaviour in a file named after another class doesn't).
        [UnityTest]
        public IEnumerator MapLevels_KeepTheirProps()
        {
            yield return Load(SceneIds.SunspireHeights);
            Assert.Greater(Object.FindObjectsByType<SpringPad>().Length, 0, "springs");
            Assert.Greater(Object.FindObjectsByType<ShardCrate>().Length, 0, "crates");
            Assert.Greater(Object.FindObjectsByType<ClimbZone>().Length, 0, "vines");
            yield return Load(SceneIds.RooftopRun);
            Assert.Greater(Object.FindObjectsByType<HideSpot>().Length, 0, "hiding spots");
            Assert.AreEqual(1, Object.FindObjectsByType<StealthObjective>().Length, "objective");
            Assert.IsNotNull(StealthTracker.Current);
            yield return Load("MS_BellKeeper");
            Assert.Greater(Object.FindObjectsByType<RelicPickup>().Length, 3, "relics");
            Assert.AreEqual(1, Object.FindObjectsByType<ClueScroll>().Length, "cipher scroll");
            Assert.IsNotNull(MissionTracker.Current);
            Assert.AreEqual(1, StealthTracker.Current.TargetsTotal, "one target");
        }
    }
}
