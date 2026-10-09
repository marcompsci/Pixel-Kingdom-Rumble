using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using PKR.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace PKR.Tests
{
    /// <summary>Phase 3.2/3.3 props in empty scenes: guard sight, takedowns, hiding, springs, vines, crates.</summary>
    public class StealthPlayModeTests
    {
        const int GroundLayer = 8;
        readonly List<Object> _cleanup = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            PoolService.ClearAll();
            foreach (var o in _cleanup) if (o != null) Object.Destroy(o);
            _cleanup.Clear();
            Time.timeScale = 1f;
        }

        T Track<T>(T o) where T : Object { _cleanup.Add(o); return o; }

        void Floor()
        {
            var f = Track(new GameObject("Floor") { layer = GroundLayer });
            f.transform.position = new Vector3(0f, -0.5f, 0f);
            f.AddComponent<BoxCollider2D>().size = new Vector2(60f, 1f);
        }

        StealthTracker Tracker() => Track(new GameObject("Tracker")).AddComponent<StealthTracker>();

        GameObject Marker(Vector2 pos)
        {
            var go = Track(new GameObject("Hero"));
            go.transform.position = pos;
            go.AddComponent<PlayerMarker>();
            return go;
        }

        PlatformerMotor2D Hero(Vector2 pos)
        {
            var go = Track(new GameObject("Hero"));
            go.transform.position = pos;
            go.AddComponent<Rigidbody2D>();
            go.AddComponent<CapsuleCollider2D>().size = new Vector2(0.7f, 1.4f);
            var motor = go.AddComponent<PlatformerMotor2D>();
            motor.SetGroundMask(1 << GroundLayer);
            go.AddComponent<PlayerMarker>();
            return motor;
        }

        EnemyAI Guard(Vector2 feet)
        {
            var def = Track(ScriptableObject.CreateInstance<EnemyDefinition>());
            def.id = "test_guard"; def.displayName = "Test Guard"; def.behavior = EnemyBehavior.Guard;
            def.bodySize = new Vector2(0.8f, 1.3f); def.maxHealth = 4; def.moveSpeed = 0f;
            def.sightRange = 6f; def.sightHalfAngle = 30f; def.lookBackInterval = 100f;
            return EnemyFactory.Spawn(def, feet + new Vector2(0f, 0.7f), -1, null); // faces left
        }

        [UnityTest]
        public IEnumerator Guard_SpotsHeroInFront_AndReportsIt()
        {
            Floor();
            var tracker = Tracker();
            var guard = Guard(new Vector2(0f, 0f));
            Marker(new Vector2(-3f, 0.7f));
            var vision = guard.GetComponent<GuardVision>();
            for (int i = 0; i < 90 && !vision.Alerted; i++) yield return new WaitForFixedUpdate();
            Assert.IsTrue(vision.Alerted, "a hero standing in the light gets spotted");
            Assert.AreEqual(1, tracker.TimesSpotted);
            Assert.IsTrue(guard.GetComponent<ContactDamage>().enabled, "alerted guards hurt on contact");
        }

        [UnityTest]
        public IEnumerator Guard_DoesNotSeeBehind_AndBackHitIsATakedown()
        {
            Floor();
            var tracker = Tracker();
            var guard = Guard(new Vector2(0f, 0f));
            Marker(new Vector2(2.5f, 0.7f)); // behind a left-facing guard
            var vision = guard.GetComponent<GuardVision>();
            for (int i = 0; i < 60; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(GuardAwareness.Calm, vision.Awareness);
            Assert.IsFalse(guard.GetComponent<ContactDamage>().enabled, "calm guards can be slipped past");

            var health = guard.GetComponent<Damageable>();
            Assert.IsTrue(health.TakeHit(HitData.Light(1), -1, TeamIds.Player, out _));
            Assert.IsTrue(health.IsDead, "one hit from behind ends it");
            Assert.AreEqual(1, tracker.Takedowns);
            Assert.AreEqual(0, tracker.TimesSpotted);
        }

        [UnityTest]
        public IEnumerator Guard_FrontHit_AlertsInsteadOfTakedown()
        {
            Floor();
            var tracker = Tracker();
            var guard = Guard(new Vector2(0f, 0f));
            Marker(new Vector2(-1.2f, 0.7f));
            yield return new WaitForFixedUpdate();
            var health = guard.GetComponent<Damageable>();
            health.TakeHit(HitData.Light(1), 1, TeamIds.Player, out _);
            Assert.IsFalse(health.IsDead);
            Assert.IsTrue(guard.GetComponent<GuardVision>().Alerted);
            Assert.AreEqual(1, tracker.TimesSpotted);
        }

        [UnityTest]
        public IEnumerator HiddenHero_IsNotSeen()
        {
            Floor();
            var tracker = Tracker();
            var hay = Track(new GameObject("Hay"));
            hay.transform.position = new Vector2(-3f, 0.65f);
            hay.AddComponent<HideSpot>().size = new Vector2(2f, 1.6f);
            var guard = Guard(new Vector2(0f, 0f));
            Marker(new Vector2(-3f, 0.7f));
            for (int i = 0; i < 120; i++) yield return new WaitForFixedUpdate();
            Assert.IsTrue(tracker.HeroHidden);
            Assert.AreEqual(GuardAwareness.Calm, guard.GetComponent<GuardVision>().Awareness);
        }

        [UnityTest]
        public IEnumerator Objective_GatesTheFinish()
        {
            var obj = Track(new GameObject("Ledger"));
            obj.AddComponent<CircleCollider2D>();
            obj.AddComponent<StealthObjective>();
            var tracker = Tracker();
            yield return null;
            Assert.IsTrue(tracker.HasObjective);
            Assert.IsFalse(tracker.CanFinish());
            tracker.TakeObjective("Ledger");
            Assert.IsTrue(tracker.CanFinish());
        }

        [UnityTest]
        public IEnumerator SpringPad_LaunchesFarHigherThanAJump()
        {
            Floor();
            var pad = Track(new GameObject("Spring"));
            pad.transform.position = new Vector2(0f, 0.05f);
            var spring = pad.AddComponent<SpringPad>();
            spring.launchHeight = 6f;
            var motor = Hero(new Vector2(0f, 2.5f));
            float peak = float.MinValue;
            for (int i = 0; i < 150; i++)
            {
                yield return new WaitForFixedUpdate();
                peak = Mathf.Max(peak, motor.Body.position.y);
            }
            Assert.Greater(peak, 4.5f, "bounced well above where it fell from");
        }

        [UnityTest]
        public IEnumerator Vine_ClimbsUpWhileHoldingUp()
        {
            Floor();
            var vine = Track(new GameObject("Vine"));
            vine.transform.position = new Vector2(0.5f, 4f);
            var zone = vine.AddComponent<ClimbZone>();
            zone.size = new Vector2(1f, 8f);
            var motor = Hero(new Vector2(0.5f, 0.75f));
            for (int i = 0; i < 20; i++) yield return new WaitForFixedUpdate();
            float y0 = motor.Body.position.y;
            motor.Intent.Move = new Vector2(0f, 1f);
            for (int i = 0; i < 40; i++) yield return new WaitForFixedUpdate();
            Assert.Greater(motor.Body.position.y, y0 + 1.5f, "climbed upward");
            motor.Intent.Move = Vector2.zero;
            float hold = motor.Body.position.y;
            for (int i = 0; i < 30; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(hold, motor.Body.position.y, 0.1f, "hangs on when the stick is released");
        }

        [UnityTest]
        public IEnumerator ShardCrate_PopsWhenBumpedFromBelow()
        {
            Floor();
            var crate = Track(new GameObject("Crate") { layer = GroundLayer });
            crate.transform.position = new Vector2(0f, 2.6f);
            crate.AddComponent<BoxCollider2D>().size = Vector2.one;
            var c = crate.AddComponent<ShardCrate>();
            var motor = Hero(new Vector2(0f, 0.75f));
            for (int i = 0; i < 10; i++) yield return new WaitForFixedUpdate();
            Assert.IsFalse(c.Used);
            motor.Intent.Jump.Press(Time.time);
            motor.Intent.Jump.SetHeld(true);
            for (int i = 0; i < 40 && !c.Used; i++) yield return new WaitForFixedUpdate();
            Assert.IsTrue(c.Used, "head-bump opens it");
        }
    }
}
