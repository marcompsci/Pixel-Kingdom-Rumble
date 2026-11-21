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
    /// Round 8: the Arena results MVP and career bests, Story's ONE SHARD LEFT hint, and the two new message settings.
    /// Settings these tests touch are put back.
    /// </summary>
    public class Round8PlayModeTests
    {
        GameObject _host;
        bool _long, _quiet;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (Services.Settings != null) { _long = Services.Settings.Data.longToasts; _quiet = Services.Settings.Data.quietCelebrations; }
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_host != null) Object.Destroy(_host);
            if (Services.Settings != null) { Services.Settings.Data.longToasts = _long; Services.Settings.Data.quietCelebrations = _quiet; }
            Time.timeScale = 1f;
            var old = SceneManager.GetActiveScene();
            var empty = SceneManager.CreateScene("Round8Done" + Time.frameCount);
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
        public IEnumerator ArenaResults_ShowMvpAndNewBest()
        {
            yield return Load(SceneIds.MainMenu);
            _host = new GameObject("ArenaResultsHost");
            var screen = _host.AddComponent<ArenaResultsScreen>();
            screen.showDelay = 0f;
            yield return null;
            EventBus<ArenaMatchEnded>.Raise(new ArenaMatchEnded
            {
                mode = MatchMode.Stock, placements = new[] { 1, 2 }, names = new[] { "YOU", "CPU" }, knockouts = new[] { 5, 1 },
                falls = new[] { 1, 5 }, scores = new[] { 0, 0 }, playerPlacement = 1, humans = 1, bestCombo = new[] { 6, 2 },
                newBestKOs = true, newBestCombo = true, party = PartyMode.None,
            });
            yield return null;
            yield return null;
            Assert.IsTrue(HasText("MVP"));
            Assert.IsTrue(HasText("NEW BEST: 5 KOs IN A MATCH!"));
            Assert.IsTrue(HasText("NEW BEST COMBO: 6!"));
        }

        [UnityTest]
        public IEnumerator Story_OneShardLeftHint()
        {
            yield return Load(SceneIds.StoryTest);
            var flow = Object.FindAnyObjectByType<LevelFlowController>();
            Assert.IsNotNull(flow);
            yield return null;
            if (flow.TotalShards < 2) Assert.Ignore("test level has fewer than 2 shards");
            for (int i = flow.Run.Shards; i < flow.TotalShards - 1; i++) flow.OnPickupCollected(PickupKind.StarShard, 1);
            yield return null;
            Assert.IsTrue(HasText("ONE SHARD LEFT!"));
        }

        [UnityTest]
        public IEnumerator Settings_MessageOptions()
        {
            yield return Load(SceneIds.MainMenu);
            var host = new GameObject("SettingsHost");
            var root = UIFactory.CreateCanvas(host.transform, "SettingsCanvas", 95, landscape: true, out _);
            host.AddComponent<SettingsPanel>().Open(root, landscape: true, onBack: null, onEditLayout: null);
            yield return null;
            foreach (var label in new[] { "DISPLAY & ACCESS", "ASSISTS" })
            {
                Button tab = null;
                foreach (var b in Object.FindObjectsByType<Button>())
                    if (b.GetComponentInChildren<Text>() != null && b.GetComponentInChildren<Text>().text == label) tab = b;
                Assert.IsNotNull(tab, label);
                tab.onClick.Invoke();
                yield return null;
                Assert.IsTrue(HasText(label == "ASSISTS" ? "Quiet celebrations" : "Longer messages"), label);
            }
            Services.Settings.SetQuietCelebrations(true);
            Assert.IsTrue(Services.Settings.Data.quietCelebrations);
        }
    }
}
