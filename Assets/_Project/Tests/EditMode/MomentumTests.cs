using System.Collections.Generic;
using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    [TestFixture]
    public class MomentumTests
    {
        static MovementStats Rex() => new MovementStats
        {
            runSpeed = 9.5f, groundAcceleration = 28f, groundDeceleration = 60f,
            coastDeceleration = 7f, skidDeceleration = 45f, airAcceleration = 30f, airDeceleration = 10f
        };

        [Test]
        public void Momentum_CoastsWhenReleased()
        {
            var s = Rex();
            float v = JumpPhysics.NextHorizontalVelocity(9f, 0f, s, true, 0.5f);
            Assert.AreEqual(5.5f, v, 0.001f); // 9 - 7*0.5
            var normal = new MovementStats { runSpeed = 9.5f, groundDeceleration = 60f };
            Assert.AreEqual(0f, JumpPhysics.NextHorizontalVelocity(9f, 0f, normal, true, 0.5f), 0.001f, "default heroes stop");
        }

        [Test]
        public void Momentum_SkidsWhenReversing()
        {
            var s = Rex();
            float v = JumpPhysics.NextHorizontalVelocity(9f, -1f, s, true, 0.1f);
            Assert.AreEqual(4.5f, v, 0.001f); // 9 - 45*0.1
            Assert.IsTrue(JumpPhysics.IsSkidding(9f, -1f, true));
            Assert.IsFalse(JumpPhysics.IsSkidding(9f, 1f, true));
            Assert.IsFalse(JumpPhysics.IsSkidding(9f, -1f, false), "no skid in the air");
        }

        [Test]
        public void Momentum_OverspeedBleedsSlowly_DefaultsSnapBack()
        {
            var s = Rex();
            float v = JumpPhysics.NextHorizontalVelocity(14f, 1f, s, true, 0.5f);
            Assert.AreEqual(10.5f, v, 0.001f); // 14 - 7*0.5, still above runSpeed
            var normal = new MovementStats { runSpeed = 9.5f, groundAcceleration = 70f };
            Assert.AreEqual(9.5f, JumpPhysics.NextHorizontalVelocity(14f, 1f, normal, true, 0.5f), 0.001f);
        }

        [Test]
        public void WallRide_StartsOnlyWithSpeedTowardWall_OncePerAirtime()
        {
            var w = new WallRide();
            Assert.IsFalse(w.TryStart(false, true, 1, 1f, 9f, 0.45f, 4f), "not on the ground");
            Assert.IsFalse(w.TryStart(true, true, 1, -1f, 9f, 0.45f, 4f), "must hold toward the wall");
            Assert.IsFalse(w.TryStart(true, true, 1, 1f, 2f, 0.45f, 4f), "too slow");
            Assert.IsFalse(w.TryStart(true, false, 1, 1f, 9f, 0.45f, 4f), "no wall");
            Assert.IsFalse(w.TryStart(true, true, 1, 1f, 9f, 0f, 4f), "hero can't wall ride");
            Assert.IsTrue(w.TryStart(true, true, 1, 1f, 9f, 0.45f, 4f));
            Assert.AreEqual(1, w.WallDir);
            w.Tick(0.5f);
            Assert.IsFalse(w.IsActive, "timed out");
            Assert.IsFalse(w.TryStart(true, true, -1, -1f, 9f, 0.45f, 4f), "once per airtime");
            w.Land();
            Assert.IsTrue(w.TryStart(true, true, -1, -1f, 9f, 0.45f, 4f), "landing restores it");
        }

        [Test]
        public void WallRide_JumpKicksAwayFromWall()
        {
            var w = new WallRide();
            Assert.AreEqual(0, w.TryWallJump(), "not riding");
            w.TryStart(true, true, 1, 1f, 9f, 0.45f, 4f);
            Assert.AreEqual(-1, w.TryWallJump());
            Assert.IsFalse(w.IsActive);
        }

        [Test]
        public void ScaleBySpeed_HitsHarderWhenFast()
        {
            var hit = new HitData { damage = 1, pipDamage = 1, baseKnockback = 5f };
            var still = CombatMath.ScaleBySpeed(hit, 0f, 9.5f, 1.5f);
            Assert.AreEqual(5f, still.baseKnockback, 0.001f);
            Assert.AreEqual(1, still.damage);
            var half = CombatMath.ScaleBySpeed(hit, 4.75f, 9.5f, 1.5f);
            Assert.AreEqual(8.75f, half.baseKnockback, 0.001f);
            Assert.AreEqual(1, half.pipDamage);
            var full = CombatMath.ScaleBySpeed(hit, -12f, 9.5f, 1.5f); // direction doesn't matter; clamped
            Assert.AreEqual(12.5f, full.baseKnockback, 0.001f);
            Assert.AreEqual(2, full.damage);
            Assert.AreEqual(2, full.pipDamage);
            Assert.AreEqual(5f, hit.baseKnockback, 0.001f, "original untouched");
        }

        [Test]
        public void UnlockRule_CanRequireSecrets()
        {
            var rex = new UnlockRule { characterId = "rex_rollo", clearLevelId = "sq_sunspire_test", minSecrets = 1 };
            var save = SaveData.CreateNew();
            save.RecordLevelResult("sq_sunspire_test", 90f, 10, 0);
            Assert.AreEqual(0, UnlockRules.Apply(save, new List<UnlockRule> { rex }).Count, "cleared without the secret");
            save.RecordLevelResult("sq_sunspire_test", 95f, 10, 1);
            Assert.AreEqual(1, UnlockRules.Apply(save, new List<UnlockRule> { rex }).Count);
        }
    }
}
