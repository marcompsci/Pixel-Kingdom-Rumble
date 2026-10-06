using System.Collections;
using NUnit.Framework;
using PKR.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace PKR.Tests
{
    /// <summary>
    /// End-to-end hit pipeline: AttackRunner -> CombatQuery (Hurtbox layer) -> Damageable -> knockback.
    /// Builds an attacker and a target on a floor with a runtime-created move (no assets needed).
    /// </summary>
    public class CombatPlayModeTests
    {
        const int GroundLayer = 8;
        GameObject _floor, _attacker, _target;
        AttackRunner _runner;
        PlatformerMotor2D _attackerMotor;
        Damageable _targetHealth;
        MoveDefinition _jab;
        Moveset _set;

        [SetUp]
        public void SetUp()
        {
            _floor = new GameObject("Floor") { layer = GroundLayer };
            _floor.transform.position = new Vector3(0f, -0.5f, 0f);
            _floor.AddComponent<BoxCollider2D>().size = new Vector2(60f, 1f);

            _jab = ScriptableObject.CreateInstance<MoveDefinition>();
            _jab.frames = new FrameData(2, 3, 6);
            _jab.hitboxOffset = new Vector2(0.8f, 0f);
            _jab.hitboxSize = new Vector2(1.2f, 1f);
            _jab.hit = HitData.Light(2);
            _jab.lungeSpeed = 0f;
            _set = ScriptableObject.CreateInstance<Moveset>();
            _set.groundAttack = _jab;

            _attacker = MakeFighter("Attacker", new Vector2(0f, 0.7f), TeamIds.Player, out _attackerMotor);
            _runner = _attacker.AddComponent<AttackRunner>();
            _runner.Moveset = _set;
            _runner.Team = TeamIds.Player;

            _target = MakeFighter("Target", new Vector2(1.0f, 0.7f), TeamIds.Enemy, out _);
            _targetHealth = _target.GetComponent<Damageable>();
        }

        static GameObject MakeFighter(string name, Vector2 pos, int team, out PlatformerMotor2D motor)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            go.AddComponent<Rigidbody2D>();
            go.AddComponent<CapsuleCollider2D>().size = new Vector2(0.7f, 1.4f);
            motor = go.AddComponent<PlatformerMotor2D>();
            motor.SetGroundMask(1 << GroundLayer);
            var d = go.AddComponent<Damageable>();
            d.Team = team;
            var hb = new GameObject("Hurtbox");
            hb.transform.SetParent(go.transform, false);
            var c = hb.AddComponent<BoxCollider2D>();
            c.size = new Vector2(0.7f, 1.4f);
            hb.AddComponent<Hurtbox>();
            return go;
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_attacker);
            Object.Destroy(_target);
            Object.Destroy(_floor);
            Object.Destroy(_jab);
            Object.Destroy(_set);
            Time.timeScale = 1f;
        }

        IEnumerator Steps(int n)
        {
            // Hit stop pauses physics in real time, so wait on frames with a generous cap.
            for (int i = 0; i < n; i++) yield return new WaitForFixedUpdate();
        }

        [UnityTest]
        public IEnumerator Jab_DamagesAndPushesTarget()
        {
            yield return Steps(20); // settle on the floor
            int before = _targetHealth.Health;
            float x0 = _target.transform.position.x;
            _attackerMotor.Intent.Attack.Press(Time.time);
            yield return Steps(15);
            Assert.AreEqual(before - 2, _targetHealth.Health, "jab should deal 2");
            yield return Steps(5);
            Assert.Greater(_target.transform.position.x, x0, "target should be pushed away (right)");
        }

        [UnityTest]
        public IEnumerator SameTeam_IsNotHit()
        {
            _targetHealth.Team = TeamIds.Player;
            yield return Steps(20);
            int before = _targetHealth.Health;
            _attackerMotor.Intent.Attack.Press(Time.time);
            yield return Steps(15);
            Assert.AreEqual(before, _targetHealth.Health);
        }

        [UnityTest]
        public IEnumerator MultiFrameHitbox_HitsOncePerSwing()
        {
            yield return Steps(20);
            int before = _targetHealth.Health;
            _attackerMotor.Intent.Attack.Press(Time.time);
            yield return Steps(15); // 3 active frames
            Assert.AreEqual(before - 2, _targetHealth.Health);
        }
    }
}
