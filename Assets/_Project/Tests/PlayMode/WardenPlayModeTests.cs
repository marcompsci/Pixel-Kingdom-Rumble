using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using PKR.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace PKR.Tests
{
    /// <summary>
    /// The Clockwork Warden in a bare arena (no hero, no level flow): its core can only be hit while staggered
    /// after a slam, it can be defeated, and a hero death resets the fight.
    /// </summary>
    public class WardenPlayModeTests
    {
        const int GroundLayer = 8;
        readonly List<Object> _cleanup = new List<Object>();
        ClockworkWarden _warden;

        [SetUp]
        public void SetUp()
        {
            var floor = new GameObject("Floor") { layer = GroundLayer };
            floor.transform.position = new Vector3(0f, -1f, 0f);
            floor.AddComponent<BoxCollider2D>().size = new Vector2(30f, 2f);
            _cleanup.Add(floor);

            var go = new GameObject("Warden");
            go.AddComponent<Damageable>();
            _warden = go.AddComponent<ClockworkWarden>();
            _cleanup.Add(go);
            Time.timeScale = 3f; // the fight has a 1.5 s intro and ~2 s before the first slam lands
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            foreach (var o in _cleanup) if (o != null) Object.Destroy(o);
            _cleanup.Clear();
        }

        Vector2 CorePosition => _warden.transform.Find("Piston/Core").position;

        bool CoreIsHittable()
        {
            var found = new List<Damageable>();
            CombatQuery.OverlapCircle(CorePosition, 0.3f, found);
            return found.Contains(_warden.Health);
        }

        IEnumerator WaitFor(System.Func<bool> condition, int maxSteps = 900)
        {
            for (int i = 0; i < maxSteps && !condition(); i++) yield return new WaitForFixedUpdate();
        }

        [UnityTest]
        public IEnumerator Core_OnlyHittableWhileStaggered_ThenDefeatable()
        {
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(_warden.Brain.IsVulnerable);
            Assert.IsFalse(CoreIsHittable(), "armored before the first slam");

            yield return WaitFor(() => _warden.Brain.IsVulnerable);
            Assert.IsTrue(_warden.Brain.IsVulnerable, "a slam happens and staggers the Warden");
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(CoreIsHittable(), "core exposed during the stagger");

            var hit = new HitData { damage = 100, pipDamage = 1, baseKnockback = 1f };
            Assert.IsTrue(_warden.Health.TakeHit(hit, 1, TeamIds.Player, out _));
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(_warden.Brain.IsDefeated);
            Assert.IsFalse(CoreIsHittable(), "no more hits after defeat");
        }

        [UnityTest]
        public IEnumerator HeroDeath_ResetsTheFight()
        {
            yield return WaitFor(() => _warden.Brain.IsVulnerable);
            Assert.IsTrue(_warden.Brain.IsVulnerable);
            _warden.Health.TakeHit(new HitData { damage = 16, pipDamage = 1 }, 1, TeamIds.Player, out _);
            Assert.AreEqual(WardenPhase.Two, _warden.Brain.Phase, "below 60% health = phase 2");

            EventBus<PlayerRespawned>.Raise(new PlayerRespawned { afterDeath = true });
            Assert.AreEqual(_warden.Health.MaxHealth, _warden.Health.Health);
            Assert.AreEqual(WardenPhase.One, _warden.Brain.Phase);
            Assert.IsFalse(_warden.Brain.IsVulnerable);
        }
    }
}
