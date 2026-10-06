using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    [TestFixture]
    public class CombatMathTests
    {
        const float Eps = 0.001f;

        static HitData Flat(float kb, bool heavy = false, float exposedMult = 3f) => new HitData
        {
            damage = 2, pipDamage = 1, baseKnockback = kb, exposedMultiplier = exposedMult,
            angleDegrees = 0f, baseHitstunFrames = 10, hitstopFrames = 4, isHeavy = heavy
        };

        [Test]
        public void Knockback_FacingRight_PushesRight()
        {
            var kb = CombatMath.Knockback(Flat(5f), false, 1f, +1);
            Assert.AreEqual(5f, kb.x, Eps);
            Assert.AreEqual(0f, kb.y, Eps);
        }

        [Test]
        public void Knockback_FacingLeft_MirrorsX()
        {
            var kb = CombatMath.Knockback(Flat(5f), false, 1f, -1);
            Assert.AreEqual(-5f, kb.x, Eps);
        }

        [Test]
        public void Knockback_StraightUpAngle_HasNoHorizontal()
        {
            var hit = Flat(6f); hit.angleDegrees = 90f;
            var kb = CombatMath.Knockback(hit, false, 1f, -1);
            Assert.AreEqual(0f, kb.x, Eps);
            Assert.AreEqual(6f, kb.y, Eps);
        }

        [Test]
        public void Knockback_HeavierTarget_FliesLess()
        {
            float light = CombatMath.Knockback(Flat(6f), false, 1f, 1).Magnitude;
            float heavy = CombatMath.Knockback(Flat(6f), false, 1.5f, 1).Magnitude;
            Assert.Less(heavy, light);
            Assert.AreEqual(4f, heavy, Eps);
        }

        [Test]
        public void Knockback_WeightIsClamped()
        {
            float tiny = CombatMath.Knockback(Flat(4f), false, 0.01f, 1).Magnitude;
            Assert.AreEqual(4f / CombatMath.MinWeight, tiny, Eps);
        }

        [Test]
        public void Knockback_ExposedHeavy_GetsFullMultiplier()
        {
            float kb = CombatMath.Knockback(Flat(5f, heavy: true, exposedMult: 3f), true, 1f, 1).Magnitude;
            Assert.AreEqual(15f, kb, Eps);
        }

        [Test]
        public void Knockback_ExposedLight_GetsHalfBonus()
        {
            // bonus = 3 - 1 = 2, light gets half -> multiplier 2
            float kb = CombatMath.Knockback(Flat(5f, heavy: false, exposedMult: 3f), true, 1f, 1).Magnitude;
            Assert.AreEqual(10f, kb, Eps);
        }

        [Test]
        public void Knockback_IsCappedAtMax()
        {
            float kb = CombatMath.Knockback(Flat(100f, heavy: true, exposedMult: 5f), true, 0.5f, 1).Magnitude;
            Assert.AreEqual(CombatMath.MaxKnockback, kb, Eps);
        }

        [Test]
        public void Knockback_NegativeBase_TreatedAsZero()
        {
            Assert.AreEqual(0f, CombatMath.Knockback(Flat(-3f), false, 1f, 1).Magnitude, Eps);
        }

        [Test]
        public void Hitstun_GrowsWithKnockback_AndIsCapped()
        {
            var hit = Flat(0f);
            Assert.AreEqual(10, CombatMath.HitstunFrames(hit, 0f));
            Assert.AreEqual(16, CombatMath.HitstunFrames(hit, 10f)); // 10 + round(10*0.6)
            Assert.AreEqual(CombatMath.MaxHitstunFrames, CombatMath.HitstunFrames(hit, 1000f));
        }

        [Test]
        public void Hitstop_IsClamped()
        {
            var hit = Flat(1f); hit.hitstopFrames = 99;
            Assert.AreEqual(CombatMath.MaxHitstopFrames, CombatMath.HitstopFrames(hit));
            hit.hitstopFrames = -2;
            Assert.AreEqual(0, CombatMath.HitstopFrames(hit));
        }

        [Test]
        public void DamageAfterArmor_ReducesButNeverBelowOne()
        {
            Assert.AreEqual(4, CombatMath.DamageAfterArmor(4, 0f));
            Assert.AreEqual(2, CombatMath.DamageAfterArmor(4, 0.5f));
            Assert.AreEqual(1, CombatMath.DamageAfterArmor(1, 0.9f));
            Assert.AreEqual(1, CombatMath.DamageAfterArmor(3, 5f)); // armor clamped to 0.9
            Assert.AreEqual(0, CombatMath.DamageAfterArmor(0, 0.2f));
        }

        [Test]
        public void ResolveArenaHit_BreakingLastPip_DoesNotLaunchYet()
        {
            var pips = new GuardPipState(1);
            var hit = Flat(5f, heavy: true);
            var r = CombatMath.ResolveArenaHit(hit, pips, 1f, 1);
            Assert.AreEqual(0, r.pipsRemaining);
            Assert.IsFalse(r.targetWasExposed);
            Assert.IsFalse(r.isLaunch);
            Assert.AreEqual(5f, r.knockbackVelocity.Magnitude, Eps);
        }

        [Test]
        public void ResolveArenaHit_HeavyOnExposed_Launches()
        {
            var pips = new GuardPipState(1);
            pips.ApplyHit(1);
            var r = CombatMath.ResolveArenaHit(Flat(5f, heavy: true), pips, 1f, 1);
            Assert.IsTrue(r.targetWasExposed);
            Assert.IsTrue(r.isLaunch);
            Assert.AreEqual(15f, r.knockbackVelocity.Magnitude, Eps);
        }

        [Test]
        public void ResolveStoryHit_AppliesArmorAndNoExposedBonus()
        {
            var r = CombatMath.ResolveStoryHit(Flat(5f, heavy: true), 0.5f, 1f, 1);
            Assert.AreEqual(1, r.hpDamage);
            Assert.AreEqual(5f, r.knockbackVelocity.Magnitude, Eps);
            Assert.AreEqual(-1, r.pipsRemaining);
        }
    
        [Test]
        public void SuperArmor_KeepsDamageButRemovesKnockbackAndStun()
        {
            var pips = new GuardPipState(3);
            var r = CombatMath.ResolveArenaHit(Flat(6f, heavy: true), pips, 1f, 1);
            Assert.IsTrue(CombatMath.ApplySuperArmor(ref r));
            Assert.IsTrue(r.armored);
            Assert.AreEqual(0f, r.knockbackVelocity.Magnitude, Eps);
            Assert.AreEqual(0, r.hitstunFrames);
            Assert.AreEqual(2, pips.Current, "pips still chipped");
            var story = CombatMath.ResolveStoryHit(Flat(6f), 0f, 1f, 1);
            int hp = story.hpDamage;
            CombatMath.ApplySuperArmor(ref story);
            Assert.AreEqual(hp, story.hpDamage, "damage still applies");
            Assert.Greater(story.hitstopFrames, 0, "hit stop is kept for feel");
        }

        [Test]
        public void SuperArmor_BrokenByLaunch()
        {
            var pips = new GuardPipState(1);
            pips.ApplyHit(1);
            var r = CombatMath.ResolveArenaHit(Flat(5f, heavy: true), pips, 1f, 1);
            Assert.IsFalse(CombatMath.ApplySuperArmor(ref r));
            Assert.IsFalse(r.armored);
            Assert.Greater(r.knockbackVelocity.Magnitude, 0f);
        }
    }
}
