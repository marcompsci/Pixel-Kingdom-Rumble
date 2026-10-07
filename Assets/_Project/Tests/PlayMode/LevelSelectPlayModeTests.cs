using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PKR.Tests
{
    /// <summary>Level select with a runtime-built world: one card per level, in order; later levels start locked.</summary>
    public class LevelSelectPlayModeTests
    {
        GameObject _host;
        WorldDefinition _world;

        [TearDown]
        public void TearDown()
        {
            if (_host != null) Object.Destroy(_host);
            if (_world != null)
            {
                foreach (var l in _world.levels) Object.Destroy(l);
                Object.Destroy(_world);
            }
        }

        static LevelDefinition Level(string id, string name)
        {
            var l = ScriptableObject.CreateInstance<LevelDefinition>();
            l.id = id; l.displayName = name; l.sceneName = "PKR_Test_NotInBuild_" + id;
            return l;
        }

        [UnityTest]
        public IEnumerator FirstLevelOpen_SecondLocked_OneCardEach()
        {
            _world = ScriptableObject.CreateInstance<WorldDefinition>();
            // Ids no real save will contain, so the result doesn't depend on local progress.
            _world.levels.Add(Level("pkr_test_level_a", "Test Meadow"));
            _world.levels.Add(Level("pkr_test_level_b", "Test Boss"));

            _host = new GameObject("LevelSelect");
            _host.SetActive(false);
            var ui = _host.AddComponent<LevelSelectUI>();
            ui.SetWorld(_world);
            _host.SetActive(true);
            yield return null; // Start builds the screen

            Assert.AreEqual(2, ui.Cards.Count);
            Assert.IsTrue(ui.Slots[0].unlocked);
            Assert.IsFalse(ui.Slots[1].unlocked);
            Assert.AreEqual(0, ui.SuggestedIndex);
            Assert.IsFalse(ui.Cards[1].interactable, "locked level can't be started");
            StringAssert.Contains("LOCKED", ui.Cards[1].GetComponentInChildren<UnityEngine.UI.Text>().text);
        }
    }
}
