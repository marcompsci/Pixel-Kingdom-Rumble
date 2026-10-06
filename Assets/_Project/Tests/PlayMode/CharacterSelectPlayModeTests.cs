using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PKR.Tests
{
    /// <summary>Character select with a runtime-built roster: playable vs locked heroes.</summary>
    public class CharacterSelectPlayModeTests
    {
        GameObject _host;
        CharacterRoster _roster;

        [TearDown]
        public void TearDown()
        {
            if (_host != null) Object.Destroy(_host);
            if (_roster != null)
            {
                foreach (var h in _roster.heroes) Object.Destroy(h);
                Object.Destroy(_roster);
            }
        }

        static CharacterDefinition Hero(string id, bool playable)
        {
            var d = ScriptableObject.CreateInstance<CharacterDefinition>();
            d.id = id;
            d.displayName = id;
            d.playableInThisBuild = playable;
            d.unlockedByDefault = playable;
            return d;
        }

        [UnityTest]
        public IEnumerator LockedHero_CannotBeConfirmed_PlayableCan()
        {
            _roster = ScriptableObject.CreateInstance<CharacterRoster>();
            _roster.heroes.Add(Hero("nova", true));
            _roster.heroes.Add(Hero("brick", false));

            _host = new GameObject("Select");
            _host.SetActive(false);
            var ui = _host.AddComponent<CharacterSelectUI>();
            ui.SetRoster(_roster);
            _host.SetActive(true);
            yield return null; // Start builds the screen

            Assert.AreEqual(0, ui.SelectedIndex);
            Assert.IsTrue(ui.ConfirmEnabled, "Nova is playable");
            ui.Show(1);
            Assert.IsFalse(ui.ConfirmEnabled, "Brick is locked in Phase 1");
            ui.Show(2); // wraps back to Nova
            Assert.AreEqual(0, ui.SelectedIndex);
            Assert.IsTrue(ui.ConfirmEnabled);
        }
    }
}
