using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using PKR.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace PKR.Tests
{
    /// <summary>Gyro Moth hovers without falling; Bolt Knight's shield blocks light front hits only.</summary>
    public class NewEnemyPlayModeTests
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
            f.AddComponent<BoxCollider2D>().size = new Vector2(40f, 1f);
        }

        [UnityTest]
        public IEnumerator GyroMoth_HoversNearHome_WithoutFalling()
        {
            Floor();
            var def = Track(ScriptableObject.CreateInstance<EnemyDefinition>());
            def.id = "test_moth"; def.displayName = "Test Moth"; def.behavior = EnemyBehavior.Flyer;
            def.bodySize = new Vector2(0.8f, 0.6f); def.maxHealth = 1;
            var ai = EnemyFactory.Spawn(def, new Vector2(0f, 4f), -1, null);
            for (int i = 0; i < 90; i++) yield return new WaitForFixedUpdate(); // 1.5 s, no hero in the scene
            Vector2 p = ai.transform.position;
            Assert.Less(Mathf.Abs(p.y - 4f), 0.8f, "bobs around its home height instead of falling");
            Assert.Less(Mathf.Abs(p.x), 0.5f);
        }

        [UnityTest]
        public IEnumerator BoltKnight_ShieldBlocksLightFrontHits_NotBackOrHeavy()
        {
            Floor();
            var def = Track(ScriptableObject.CreateInstance<EnemyDefinition>());
            def.id = "test_knight"; def.displayName = "Test Knight"; def.behavior = EnemyBehavior.ShieldWalker;
            def.bodySize = new Vector2(0.9f, 1.25f); def.maxHealth = 10; def.moveSpeed = 0f; def.shieldBreakDuration = 1.5f;
            var ai = EnemyFactory.Spawn(def, new Vector2(0f, 0.7f), -1, null); // faces left
            var health = ai.GetComponent<Damageable>();
            yield return new WaitForFixedUpdate();
            int hp = health.Health;

            var light = HitData.Light(1);
            Assert.IsTrue(health.TakeHit(light, +1, TeamIds.Player, out var front), "front = pushes against its facing");
            Assert.IsTrue(front.blocked);
            Assert.AreEqual(hp, health.Health, "blocked: no damage");

            Assert.IsTrue(health.TakeHit(light, -1, TeamIds.Player, out var back));
            Assert.IsFalse(back.blocked);
            Assert.AreEqual(hp - 1, health.Health, "from behind it lands");

            yield return new WaitForSeconds(0.1f);
            health.GetComponent<Invulnerability>().Clear();
            var heavy = HitData.Heavy(2);
            ai.GetComponent<PlatformerMotor2D>().SetFacing(-1);
            Assert.IsTrue(health.TakeHit(heavy, +1, TeamIds.Player, out var broke));
            Assert.IsFalse(broke.blocked, "heavy hit breaks the shield and lands");
            Assert.IsTrue(ai.GetComponent<ShieldGuard>().Shield.IsBroken);
        }
    }
}
