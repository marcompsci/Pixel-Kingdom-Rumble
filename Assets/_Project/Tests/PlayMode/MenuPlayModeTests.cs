using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PKR.Tests
{
    /// <summary>Pause state, pause menu visibility, and the level-complete screen building from an event.</summary>
    public class MenuPlayModeTests
    {
        GameObject _host;

        [TearDown]
        public void TearDown()
        {
            if (_host != null) Object.Destroy(_host);
            if (Services.State != null) Services.State.SetState(GameState.Menu);
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator PauseAndResume_FreezeAndRestoreTime()
        {
            Assert.IsNotNull(Services.State, "GameBootstrap should create services in play mode");
            Services.State.SetState(GameState.Playing);
            Services.State.Pause();
            Assert.IsTrue(Services.State.IsPaused);
            Assert.AreEqual(0f, Time.timeScale);
            yield return null;
            Services.State.Resume();
            Assert.AreEqual(GameState.Playing, Services.State.State);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator PauseMenu_ShowsOnPause_HidesOnResume()
        {
            Services.State.SetState(GameState.Playing);
            _host = new GameObject("PauseHost");
            _host.AddComponent<PauseMenu>();
            yield return null;
            var menu = _host.transform.Find("PauseCanvas/SafeArea/Menu");
            Assert.IsNotNull(menu);
            Assert.IsFalse(menu.gameObject.activeSelf);

            Services.State.Pause();
            yield return null;
            Assert.IsTrue(menu.gameObject.activeSelf, "menu opens on pause");
            Assert.Greater(menu.GetComponentsInChildren<Button>().Length, 3);

            var group = menu.GetComponent<CanvasGroup>();
            Assert.IsNotNull(group, "the menu fades in");
            float t = 0f;
            while (group.alpha < 1f && t < 1f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.AreEqual(1f, group.alpha, 0.001f, "fully shown within the entrance time");
            Assert.Less(t, 0.5f);

            Services.State.Resume();
            yield return null;
            Assert.IsFalse(group.blocksRaycasts, "stops taking taps the moment the game resumes");
            t = 0f;
            while (menu.gameObject.activeSelf && t < 1f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsFalse(menu.gameObject.activeSelf, "menu closes on resume");
            Assert.Less(t, 0.4f, "a quick exit");

            Services.State.Pause();
            yield return null;
            Assert.IsTrue(menu.gameObject.activeSelf, "opens again");
            Assert.IsTrue(group.blocksRaycasts);
            Services.State.Resume();
        }

        [UnityTest]
        public IEnumerator LevelCompleteScreen_BuildsFromEvent()
        {
            _host = new GameObject("ResultsHost");
            var screen = _host.AddComponent<LevelCompleteScreen>();
            screen.showDelay = 0f;
            yield return null;
            EventBus<LevelCompleted>.Raise(new LevelCompleted
            {
                levelId = "test", levelName = "Test Level", timeSeconds = 75f, parTimeSeconds = 120f,
                newBestTime = true, shards = 10, totalShards = 20, secrets = 1, totalSecrets = 1,
                deaths = 0, starShardReward = 40
            });
            yield return null;
            yield return null;
            var root = _host.transform.Find("ResultsCanvas/SafeArea/Results");
            Assert.IsNotNull(root);
            Assert.IsTrue(root.gameObject.activeSelf);
            Assert.Greater(root.GetComponentsInChildren<Text>().Length, 5);
        }
    }
}
