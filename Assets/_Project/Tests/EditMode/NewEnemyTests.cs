using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    [TestFixture]
    public class NewEnemyTests
    {
        static readonly Vec2 Home = new Vec2(0f, 5f);

        [Test]
        public void Flyer_HoversUntilHeroInRange_ThenWindsUpAndSwoopsAtHero()
        {
            var f = new FlyerLogic(FlyerTuning.Default, initialCooldown: 0f);
            var far = new Vec2(20f, 0f);
            f.Step(0.1f, Home, Home, true, far);
            Assert.AreEqual(FlyerState.Hover, f.State, "hero too far");

            var hero = new Vec2(3f, 1f);
            var v = f.Step(0.1f, Home, Home, true, hero);
            Assert.AreEqual(FlyerState.Windup, f.State);
            Assert.AreEqual(0f, v.Magnitude, 0.001f, "holds still while winding up");

            for (int i = 0; i < 5; i++) f.Step(0.1f, Home, Home, true, hero);
            v = f.Step(0.1f, Home, Home, true, hero);
            Assert.AreEqual(FlyerState.Swoop, f.State);
            Assert.AreEqual(FlyerTuning.Default.swoopSpeed, v.Magnitude, 0.01f);
            Assert.Greater(v.x, 0f, "toward the hero");
            Assert.Less(v.y, 0f, "downward");
        }

        [Test]
        public void Flyer_ReturnsHome_ThenCoolsDown()
        {
            var f = new FlyerLogic(FlyerTuning.Default, initialCooldown: 0f);
            var hero = new Vec2(2f, 3f);
            for (int i = 0; i < 20 && f.State != FlyerState.Return; i++) f.Step(0.1f, Home, Home, true, hero);
            Assert.AreEqual(FlyerState.Return, f.State, "swoop ends with a return");
            var away = new Vec2(4f, 2f);
            var v = f.Step(0.1f, away, Home, true, hero);
            Assert.LessOrEqual(v.Magnitude, FlyerTuning.Default.returnSpeed + 0.001f);
            Assert.Less(v.x, 0f);
            f.Step(0.1f, Home, Home, true, hero); // arrived
            Assert.AreEqual(FlyerState.Hover, f.State);
            f.Step(0.1f, Home, Home, true, hero);
            Assert.AreEqual(FlyerState.Hover, f.State, "cooldown before the next swoop");
        }

        [Test]
        public void Flyer_InterruptSendsItHome()
        {
            var f = new FlyerLogic(FlyerTuning.Default, initialCooldown: 0f);
            f.Step(0.1f, Home, Home, true, new Vec2(1f, 4f));
            Assert.AreEqual(FlyerState.Windup, f.State);
            f.Interrupt();
            Assert.AreEqual(FlyerState.Return, f.State);
        }

        [Test]
        public void Shield_BlocksLightFrontHits_BreaksOnHeavy_IgnoresBack()
        {
            var s = new ShieldState(1.5f);
            // Knight faces left (-1). A hero on its left hits rightward (+1): from the front.
            Assert.AreEqual(ShieldOutcome.Blocked, s.Resolve(-1, +1, heavy: false));
            Assert.AreEqual(ShieldOutcome.Unshielded, s.Resolve(-1, -1, heavy: false), "from behind");
            Assert.AreEqual(ShieldOutcome.Broken, s.Resolve(-1, +1, heavy: true));
            Assert.IsTrue(s.IsBroken);
            Assert.AreEqual(ShieldOutcome.Unshielded, s.Resolve(-1, +1, heavy: false), "shield down");
            s.Tick(1.6f);
            Assert.IsFalse(s.IsBroken);
            Assert.AreEqual(ShieldOutcome.Blocked, s.Resolve(-1, +1, heavy: false), "back up");
        }

        [Test]
        public void Shield_JudgesFrontBySourcePosition()
        {
            var s = new ShieldState(1.5f);
            // Knight at x=0 facing right. A hero at x=2 is in front whichever way the hit pushes.
            Assert.AreEqual(ShieldOutcome.Blocked, s.ResolveFromSource(+1, 0f, 2f, heavy: false));
            Assert.AreEqual(ShieldOutcome.Unshielded, s.ResolveFromSource(+1, 0f, -2f, heavy: false), "behind");
            Assert.AreEqual(ShieldOutcome.Unshielded, s.ResolveFromSource(+1, 0f, 0.1f, heavy: false), "straight above");
        }

        [Test]
        public void Flyer_GivesUpReturningAfterAWhile()
        {
            var f = new FlyerLogic(FlyerTuning.Default, initialCooldown: 0f);
            var hero = new Vec2(2f, 3f);
            for (int i = 0; i < 20 && f.State != FlyerState.Return; i++) f.Step(0.1f, Home, Home, true, hero);
            var stuck = new Vec2(-5f, 0f);
            for (int i = 0; i < 31; i++) f.Step(0.1f, stuck, Home, false, hero);
            Assert.AreEqual(FlyerState.Hover, f.State, "stuck behind geometry: hover where it is");
        }

        [Test]
        public void BlockedResult_DoesNothingButClank()
        {
            var r = CombatMath.BlockedResult(HitData.Heavy(3));
            Assert.IsTrue(r.blocked);
            Assert.AreEqual(0, r.hpDamage);
            Assert.AreEqual(0f, r.knockbackVelocity.Magnitude, 0.001f);
            Assert.LessOrEqual(r.hitstopFrames, 3);
        }
    }
}
