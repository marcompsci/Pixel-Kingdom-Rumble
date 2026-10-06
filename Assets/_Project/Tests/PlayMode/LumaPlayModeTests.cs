using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using PKR.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace PKR.Tests
{
    /// <summary>
    /// Luma's kit end to end: Magnet Tether pulls the target toward her; Spark Coil falls, arms, zaps once.
    /// Built in code with runtime-created moves (no assets needed).
    /// </summary>
    public class LumaPlayModeTests
    {
        const int GroundLayer = 8;
        GameObject _floor;
        readonly List<Object> _cleanup = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            _floor = new GameObject("Floor") { layer = GroundLayer };
            _floor.transform.position = new Vector3(0f, -0.5f, 0f);
            _floor.AddComponent<BoxCollider2D>().size = new Vector2(60f, 1f);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _cleanup) if (o != null) Object.Destroy(o);
            _cleanup.Clear();
            Object.Destroy(_floor);
            PoolService.ClearAll();
            Time.timeScale = 1f;
        }

        T Track<T>(T o) where T : Object { _cleanup.Add(o); return o; }

        GameObject MakeFighter(string name, Vector2 pos, int team, out PlatformerMotor2D motor)
        {
            var go = Track(new GameObject(name));
            go.transform.position = pos;
            go.AddComponent<Rigidbody2D>();
            go.AddComponent<CapsuleCollider2D>().size = new Vector2(0.7f, 1.4f);
            motor = go.AddComponent<PlatformerMotor2D>();
            motor.SetGroundMask(1 << GroundLayer);
            var d = go.AddComponent<Damageable>();
            d.Team = team;
            var hb = new GameObject("Hurtbox");
            hb.transform.SetParent(go.transform, false);
            hb.AddComponent<BoxCollider2D>().size = new Vector2(0.7f, 1.4f);
            hb.AddComponent<Hurtbox>();
            return go;
        }

        static IEnumerator Steps(int n)
        {
            for (int i = 0; i < n; i++) yield return new WaitForFixedUpdate();
        }

        [UnityTest]
        public IEnumerator MagnetTether_PullsTargetTowardLuma()
        {
            var tether = Track(ScriptableObject.CreateInstance<MoveDefinition>());
            tether.frames = new FrameData(2, 1, 6);
            tether.hit = new HitData { damage = 1, pipDamage = 1, baseKnockback = 8f, exposedMultiplier = 1f,
                                       angleDegrees = 170f, baseHitstunFrames = 18, hitstopFrames = 0 };
            tether.spawnsProjectile = true; tether.projectileSpeed = 15f; tether.projectileLifetime = 0.6f;
            tether.projectileRadius = 0.3f; tether.projectileSpawnOffset = new Vector2(0.7f, 0f); tether.lungeSpeed = 0f;
            var set = Track(ScriptableObject.CreateInstance<Moveset>());
            set.groundSpecial = tether;

            var luma = MakeFighter("Luma", new Vector2(0f, 0.7f), TeamIds.Player, out var lumaMotor);
            var runner = luma.AddComponent<AttackRunner>();
            runner.Moveset = set;
            runner.Team = TeamIds.Player;
            var target = MakeFighter("Target", new Vector2(4f, 0.7f), TeamIds.Enemy, out var targetMotor);
            var health = target.GetComponent<Damageable>();

            yield return Steps(20);
            int before = health.Health;
            lumaMotor.Intent.Special.Press(Time.time);
            bool pulled = false;
            for (int i = 0; i < 40 && !pulled; i++)
            {
                yield return new WaitForFixedUpdate();
                if (health.Health < before) pulled = targetMotor.Velocity.x < 0f;
            }
            Assert.Less(health.Health, before, "tether hit");
            Assert.IsTrue(pulled, "target should move toward Luma (left)");
        }

        [UnityTest]
        public IEnumerator SparkCoil_FallsArmsAndZapsOnce()
        {
            var coil = Track(ScriptableObject.CreateInstance<MoveDefinition>());
            coil.hit = HitData.Light(1);
            coil.spawnsTrap = true; coil.trapArmDelay = 0.2f; coil.trapLifetime = 5f; coil.trapRadius = 0.6f;
            coil.trapFallSpeed = 14f; coil.projectileSpawnOffset = Vector2.zero;

            var owner = MakeFighter("Luma", new Vector2(-6f, 0.7f), TeamIds.Player, out _);
            var runner = owner.AddComponent<AttackRunner>();
            runner.Team = TeamIds.Player;
            var target = MakeFighter("Target", new Vector2(2f, 0.7f), TeamIds.Enemy, out _);
            var health = target.GetComponent<Damageable>();
            yield return Steps(20);

            // Drop the coil from above the target.
            var trap = SparkTrap.Spawn(coil, new Vector2(2f, 3f), 1, TeamIds.Player, owner.GetComponent<Damageable>(), runner, null);
            Assert.AreEqual(TrapPhase.Falling, trap.Phase);
            int before = health.Health;

            for (int i = 0; i < 90 && health.Health == before; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(before - 1, health.Health, "zapped once");
            yield return Steps(30);
            Assert.AreEqual(before - 1, health.Health, "a spent coil never zaps again");
            Assert.IsFalse(trap.isActiveAndEnabled, "returned to the pool");
        }
    }
}
