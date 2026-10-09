using System.Collections;
using NUnit.Framework;
using PKR.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PKR.Tests
{
    /// <summary>
    /// Versus mode in the built scenes (skipped if PKR > Build All Scenes hasn't run): the select screen picks both
    /// fighters, and in the dojo a K.O. wins the round and the next round starts with full health.
    /// No match is finished, so no Star Shards are granted.
    /// </summary>
    public class VersusPlayModeTests
    {
        static IEnumerator Load(string scene)
        {
            if (!Application.CanStreamedLevelBeLoaded(scene)) Assert.Ignore($"{scene} is not built; run PKR > Build All Scenes.");
            SceneManager.LoadScene(scene, LoadSceneMode.Single);
            yield return null;
            yield return null;
        }

        static IEnumerator WaitUntil(System.Func<bool> condition, float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < end) yield return null;
        }

        [UnityTest]
        public IEnumerator Select_PicksPlayerThenOpponent()
        {
            yield return Load(SceneIds.VersusSelect);
            var ui = Object.FindAnyObjectByType<VersusSelectUI>();
            Assert.IsNotNull(ui);
            ui.Pick(0);
            Assert.IsFalse(string.IsNullOrEmpty(ui.PlayerChoice));
            Assert.AreEqual(1, ui.Picking);
            ui.Pick(-1); // random opponent
            Assert.IsTrue(ui.OpponentChosen);
            Assert.IsNull(ui.OpponentChoice);
            ui.Back();
            Assert.IsFalse(ui.OpponentChosen, "back undoes the opponent pick");
        }

        [UnityTest]
        public IEnumerator Dojo_KOWinsTheRound_NextRoundResetsHealth()
        {
            yield return Load(SceneIds.VersusStage);
            var vs = Object.FindAnyObjectByType<VersusController>();
            Assert.IsNotNull(vs);
            vs.grantRewards = false;
            yield return WaitUntil(() => vs.IsLive, 6f);
            Assert.IsTrue(vs.IsLive, "round 1 starts after the intro");
            Assert.AreNotEqual(vs.Player.go, vs.Opponent.go);
            Assert.AreEqual(VersusRules.HealthFor(vs.Opponent.def.weight), vs.Opponent.health.MaxHealth);

            vs.Opponent.health.TakeDirectDamage(vs.Opponent.health.MaxHealth);
            yield return WaitUntil(() => vs.Match.Phase != VersusPhase.Fighting, 2f);
            Assert.AreEqual(1, vs.Match.Wins(0));
            Assert.AreEqual(VersusPhase.RoundOver, vs.Match.Phase);

            yield return WaitUntil(() => vs.IsLive, 8f);
            Assert.AreEqual(2, vs.Match.Round);
            Assert.AreEqual(vs.Opponent.health.MaxHealth, vs.Opponent.health.Health, "health refills for the new round");
            Assert.IsFalse(vs.Opponent.health.IsDead);
        }

        [Test]
        public void Moveset_DirectionalSpecials()
        {
            var ms = ScriptableObject.CreateInstance<Moveset>();
            var n = ScriptableObject.CreateInstance<MoveDefinition>();
            var side = ScriptableObject.CreateInstance<MoveDefinition>();
            var down = ScriptableObject.CreateInstance<MoveDefinition>();
            var air = ScriptableObject.CreateInstance<MoveDefinition>();
            ms.groundSpecial = n; ms.sideSpecial = side; ms.downSpecial = down; ms.airSpecial = air;
            Assert.AreSame(n, ms.ForSpecial(true, Vector2.zero));
            Assert.AreSame(side, ms.ForSpecial(true, new Vector2(-1f, 0f)));
            Assert.AreSame(down, ms.ForSpecial(true, new Vector2(0.9f, -0.9f)), "down beats side");
            Assert.AreSame(air, ms.ForSpecial(false, new Vector2(1f, 0f)));
            ms.sideSpecial = null;
            Assert.AreSame(n, ms.ForSpecial(true, new Vector2(1f, 0f)), "no side special: the neutral one");
            foreach (var o in new Object[] { ms, n, side, down, air }) Object.DestroyImmediate(o);
        }
    }
}
