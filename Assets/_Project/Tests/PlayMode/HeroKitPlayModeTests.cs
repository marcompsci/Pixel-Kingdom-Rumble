using System.Collections;
using NUnit.Framework;
using PKR.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace PKR.Tests
{
    /// <summary>
    /// Phase 2 hero framework: super armor (Brick's Bulwark Charge), swapping heroes at runtime (HeroLoadout),
    /// and the mid-air jump (Brick's Stone Step). Everything is built in code; no assets needed.
    /// </summary>
    public class HeroKitPlayModeTests
    {
        const int GroundLayer = 8;
        GameObject _floor;
        readonly System.Collections.Generic.List<Object> _cleanup = new System.Collections.Generic.List<Object>();

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
            Time.timeScale = 1f;
        }

        T Track<T>(T o) where T : Object { _cleanup.Add(o); return o; }

        GameObject MakeFighter(string name, Vector2 pos, int team, CharacterDefinition def, out PlatformerMotor2D motor)
        {
            var go = Track(new GameObject(name));
            go.SetActive(false);
            go.transform.position = pos;
            go.AddComponent<Rigidbody2D>();
            go.AddComponent<CapsuleCollider2D>().size = new Vector2(0.7f, 1.4f);
            motor = go.AddComponent<PlatformerMotor2D>();
            motor.SetGroundMask(1 << GroundLayer);
            if (def != null) motor.SetDefinition(def);
            var d = go.AddComponent<Damageable>();
            d.Team = team;
            var hb = new GameObject("Hurtbox");
            hb.transform.SetParent(go.transform, false);
            hb.AddComponent<BoxCollider2D>().size = new Vector2(0.7f, 1.4f);
            hb.AddComponent<Hurtbox>();
            go.SetActive(true);
            return go;
        }

        MoveDefinition Move(FrameData frames, Vector2 offset, bool armor)
        {
            var m = Track(ScriptableObject.CreateInstance<MoveDefinition>());
            m.frames = frames;
            m.hitboxOffset = offset;
            m.hitboxSize = new Vector2(1.2f, 1f);
            m.hit = HitData.Light(2);
            m.lungeSpeed = 0f;
            m.superArmor = armor;
            m.armorEndFrame = -1;
            return m;
        }

        Moveset Set(MoveDefinition ground)
        {
            var s = Track(ScriptableObject.CreateInstance<Moveset>());
            s.groundAttack = ground;
            return s;
        }

        static IEnumerator Steps(int n)
        {
            for (int i = 0; i < n; i++) yield return new WaitForFixedUpdate();
        }

        IEnumerator HitWhileAttacking(bool armor, System.Action<AttackRunner, Damageable, int> check)
        {
            var attacker = MakeFighter("Attacker", new Vector2(0f, 0.7f), TeamIds.Player, null, out var attackerMotor);
            var jab = attacker.AddComponent<AttackRunner>();
            jab.Moveset = Set(Move(new FrameData(2, 3, 6), new Vector2(0.8f, 0f), false));
            jab.Team = TeamIds.Player;

            // The defender's move is long and reaches nowhere near the attacker; only its armor matters.
            var defender = MakeFighter("Defender", new Vector2(1.0f, 0.7f), TeamIds.Enemy, null, out var defenderMotor);
            var charge = defender.AddComponent<AttackRunner>();
            charge.Moveset = Set(Move(new FrameData(2, 40, 5), new Vector2(20f, 0f), armor));
            charge.Team = TeamIds.Enemy;
            var health = defender.GetComponent<Damageable>();

            yield return Steps(20); // settle
            int before = health.Health;
            defenderMotor.Intent.Attack.Press(Time.time);
            yield return Steps(4);
            Assert.IsTrue(charge.IsAttacking, "defender's move started");
            attackerMotor.Intent.Attack.Press(Time.time);
            yield return Steps(12);
            check(charge, health, before);
        }

        [UnityTest]
        public IEnumerator SuperArmor_TakesDamageButKeepsAttacking()
        {
            yield return HitWhileAttacking(true, (charge, health, before) =>
            {
                Assert.AreEqual(before - 2, health.Health, "armor doesn't block damage");
                Assert.IsTrue(charge.IsAttacking, "armor keeps the move going");
            });
        }

        [UnityTest]
        public IEnumerator WithoutArmor_HitInterruptsTheMove()
        {
            yield return HitWhileAttacking(false, (charge, health, before) =>
            {
                Assert.AreEqual(before - 2, health.Health);
                Assert.IsFalse(charge.IsAttacking, "a normal hit cancels the move");
            });
        }

        [UnityTest]
        public IEnumerator HeroLoadout_SwapsKitMovesetAndGuardPips()
        {
            var novaKit = Track(ScriptableObject.CreateInstance<NovaKitDefinition>());
            var brickKit = Track(ScriptableObject.CreateInstance<BrickKitDefinition>());
            var nova = Track(ScriptableObject.CreateInstance<CharacterDefinition>());
            nova.id = "nova"; nova.guardPips = 3; nova.kit = novaKit; nova.moveset = Set(Move(new FrameData(2, 2, 2), Vector2.right, false));
            var brick = Track(ScriptableObject.CreateInstance<CharacterDefinition>());
            brick.id = "brick"; brick.guardPips = 4; brick.kit = brickKit; brick.moveset = Set(Move(new FrameData(3, 3, 3), Vector2.right, true));
            brick.movement.airJumps = 1;

            var go = Track(new GameObject("Hero"));
            go.SetActive(false);
            go.AddComponent<Rigidbody2D>();
            go.AddComponent<CapsuleCollider2D>().size = new Vector2(0.7f, 1.4f);
            var motor = go.AddComponent<PlatformerMotor2D>();
            motor.SetGroundMask(1 << GroundLayer);
            motor.SetDefinition(nova);
            var health = go.AddComponent<Damageable>();
            health.Model = DamageModel.ArenaPips;
            var abilities = go.AddComponent<HeroAbilities>();
            var runner = go.AddComponent<AttackRunner>();
            go.transform.position = new Vector3(0f, 0.7f, 0f);
            go.SetActive(true);
            yield return Steps(2);

            Assert.IsTrue(abilities.Kit is NovaKitDefinition);
            Assert.AreEqual(3, health.Pips.MaxPips);

            HeroLoadout.Apply(go, brick);
            yield return Steps(2);

            Assert.AreSame(brick, motor.Definition);
            Assert.IsTrue(abilities.Kit is BrickKitDefinition, "kit swapped");
            Assert.AreSame(brick.moveset, runner.Moveset, "moveset swapped");
            Assert.AreEqual(4, health.Pips.MaxPips, "Guard Pips re-read from the new hero");
        }

        [UnityTest]
        public IEnumerator StoneStep_MidAirJumpRisesAgain()
        {
            var def = Track(ScriptableObject.CreateInstance<CharacterDefinition>());
            def.id = "jumper";
            def.movement.airJumps = 1;
            MakeFighter("Jumper", new Vector2(0f, 0.7f), TeamIds.Player, def, out var motor);
            yield return Steps(20);
            Assert.IsTrue(motor.IsGrounded);

            motor.Intent.Jump.Press(Time.time);
            motor.Intent.Jump.SetHeld(true);
            // Wait until falling.
            for (int i = 0; i < 120 && !(motor.Velocity.y < -0.5f); i++) yield return new WaitForFixedUpdate();
            Assert.Less(motor.Velocity.y, 0f, "should be falling after the first jump");
            Assert.AreEqual(1, motor.AirJumpsLeft);

            motor.Intent.Jump.Press(Time.time);
            yield return Steps(3);
            Assert.Greater(motor.Velocity.y, 0f, "the mid-air jump goes up again");
            Assert.AreEqual(0, motor.AirJumpsLeft);
        }
    }
}
