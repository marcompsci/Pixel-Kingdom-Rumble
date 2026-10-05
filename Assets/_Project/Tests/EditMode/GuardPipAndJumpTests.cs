using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    [TestFixture]
    public class GuardPipStateTests
    {
        [Test]
        public void StartsFull()
        {
            var p = new GuardPipState(3);
            Assert.AreEqual(3, p.Current);
            Assert.IsFalse(p.IsExposed);
        }

        [Test]
        public void ApplyHit_ReportsExposureOnlyOnTransition()
        {
            var p = new GuardPipState(3);
            Assert.IsFalse(p.ApplyHit(2));
            Assert.IsTrue(p.ApplyHit(2));
            Assert.AreEqual(0, p.Current);
            Assert.IsFalse(p.ApplyHit(1)); // already exposed
        }

        [Test]
        public void ApplyHit_NegativeDamage_IsIgnored()
        {
            var p = new GuardPipState(3);
            p.ApplyHit(-5);
            Assert.AreEqual(3, p.Current);
        }

        [Test]
        public void Regen_WaitsForDelay_ThenRestoresOnePerInterval()
        {
            var p = new GuardPipState(3, regenDelay: 3f, regenInterval: 1f);
            p.ApplyHit(3);
            Assert.AreEqual(0, p.Tick(2.9f));
            Assert.AreEqual(0, p.Current);
            Assert.AreEqual(0, p.Tick(0.6f)); // 3.5s total -> 0.5 into regen
            Assert.AreEqual(1, p.Tick(0.5f)); // 1.0 into regen
            Assert.AreEqual(1, p.Current);
            Assert.AreEqual(2, p.Tick(5f));
            Assert.AreEqual(3, p.Current);
            Assert.AreEqual(0, p.Tick(5f)); // capped
        }

        [Test]
        public void Regen_IsInterruptedByNewHit()
        {
            var p = new GuardPipState(3, 3f, 1f);
            p.ApplyHit(3);
            p.Tick(3.9f);
            p.ApplyHit(0); // even a 0-pip hit resets the quiet timer
            Assert.AreEqual(0, p.Tick(2f));
            Assert.AreEqual(0, p.Current);
        }

        [Test]
        public void Constructor_RejectsBadValues()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new GuardPipState(0));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new GuardPipState(3, -1f));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new GuardPipState(3, 1f, 0f));
        }
    }

    [TestFixture]
    public class JumpAssistTests
    {
        const float Dt = 1f / 60f;

        [Test]
        public void GroundedPress_Jumps()
        {
            var j = new JumpAssist(0.1f, 0.12f);
            j.Tick(Dt, true);
            j.PressJump();
            Assert.IsTrue(j.TryConsumeJump());
        }

        [Test]
        public void CoyoteTime_AllowsJumpShortlyAfterLeavingLedge()
        {
            var j = new JumpAssist(0.1f, 0.12f);
            j.Tick(Dt, true);
            for (int i = 0; i < 5; i++) j.Tick(Dt, false); // ~0.083s airborne
            j.PressJump();
            Assert.IsTrue(j.TryConsumeJump());
        }

        [Test]
        public void CoyoteTime_Expires()
        {
            var j = new JumpAssist(0.1f, 0.12f);
            j.Tick(Dt, true);
            for (int i = 0; i < 8; i++) j.Tick(Dt, false); // ~0.133s
            j.PressJump();
            Assert.IsFalse(j.TryConsumeJump());
        }

        [Test]
        public void Buffer_JumpPressedBeforeLanding_FiresOnLanding()
        {
            var j = new JumpAssist(0.1f, 0.12f);
            j.Tick(Dt, false);
            j.PressJump();
            Assert.IsFalse(j.TryConsumeJump());
            for (int i = 0; i < 5; i++) j.Tick(Dt, false);
            j.Tick(Dt, true); // lands ~0.1s after press
            Assert.IsTrue(j.TryConsumeJump());
        }

        [Test]
        public void Buffer_Expires()
        {
            var j = new JumpAssist(0.1f, 0.12f);
            j.PressJump();
            for (int i = 0; i < 10; i++) j.Tick(Dt, false);
            j.Tick(Dt, true);
            Assert.IsFalse(j.TryConsumeJump());
        }

        [Test]
        public void CoyoteJump_CannotBeUsedTwice()
        {
            var j = new JumpAssist(0.1f, 0.12f);
            j.Tick(Dt, true);
            j.Tick(Dt, false);
            j.PressJump();
            Assert.IsTrue(j.TryConsumeJump());
            j.Tick(Dt, false);
            j.PressJump();
            Assert.IsFalse(j.TryConsumeJump());
            Assert.IsTrue(j.TryConsumeBufferedPress()); // still available for an air action
        }
    }
}
