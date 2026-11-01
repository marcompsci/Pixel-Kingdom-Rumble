using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PKR.Tests
{
    /// <summary>Story Quest > STORY STATS (with the Quick Game widget section) and the pause menu's run progress.</summary>
    public class StoryStatsPlayModeTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (Services.State != null && Services.State.IsPaused) Services.State.Resume();
            Time.timeScale = 1f;
            var old = SceneManager.GetActiveScene();
            var empty = SceneManager.CreateScene("StoryStatsDone" + Time.frameCount);
            SceneManager.SetActiveScene(empty);
            if (old.IsValid() && old.isLoaded && old != empty) yield return SceneManager.UnloadSceneAsync(old);
        }

        static bool HasText(string contains)
        {
            foreach (var t in Object.FindObjectsByType<Text>())
                if (t.isActiveAndEnabled && t.text != null && t.text.Contains(contains)) return true;
            return false;
        }

        static IEnumerator Load(string scene)
        {
            if (!Application.CanStreamedLevelBeLoaded(scene)) Assert.Ignore($"{scene} not built; run PKR > Build All Scenes.");
            SceneManager.LoadScene(scene, LoadSceneMode.Single);
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator LevelSelect_OpensStoryStats_WithTheQuickGame()
        {
            yield return Load(SceneIds.LevelSelect);
            var ui = Object.FindAnyObjectByType<LevelSelectUI>();
            Assert.IsNotNull(ui);
            Assert.IsTrue(HasText("STORY STATS"), "a STORY STATS button under the map");
            ui.ShowStats();
            yield return null;
            Assert.IsTrue(HasText("Levels cleared"));
            Assert.IsTrue(HasText("Time medals"));
            Assert.IsTrue(HasText("QUICK GAME"), "the widget section");
        }

        [UnityTest]
        public IEnumerator PauseMenu_ShowsTheRunsProgress()
        {
            yield return Load(SceneIds.SunspireHeights);
            if (Services.State == null) Assert.Ignore("needs the game state service");
            yield return new WaitForSecondsRealtime(0.5f);
            Services.State.Pause();
            yield return null;
            yield return null;
            Assert.IsTrue(HasText("SHARDS "), "shards so far on the pause screen");
            Assert.IsTrue(HasText("PAR "), "time against par on the pause screen");
        }
    }
}
