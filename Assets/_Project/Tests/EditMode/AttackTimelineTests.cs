using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    [TestFixture]
    public class AttackTimelineTests
    {
        static readonly FrameData Jab = new FrameData(startup: 4, active: 3, recovery: 10, cancelWindow: 6);

        [Test]
        public void Total_SumsPhases()
        {
            Assert.AreEqual(17, Jab.Total);
            Assert.AreEqual(1, new FrameData(0, 0, 0).Total); // active is at least 1 frame
        }

        [Test]
        public void PhaseAt_WalksThroughPhases()
        {
            Assert.AreEqual(AttackPhase.Startup, AttackTimeline.PhaseAt(Jab, 0));
            Assert.AreEqual(AttackPhase.Startup, AttackTimeline.PhaseAt(Jab, 3));
            Assert.AreEqual(AttackPhase.Active, AttackTimeline.PhaseAt(Jab, 4));
            Assert.AreEqual(AttackPhase.Active, AttackTimeline.PhaseAt(Jab, 6));
            Assert.AreEqual(AttackPhase.Recovery, AttackTimeline.PhaseAt(Jab, 7));
            Assert.AreEqual(AttackPhase.Recovery, AttackTimeline.PhaseAt(Jab, 16));
            Assert.AreEqual(AttackPhase.Done, AttackTimeline.PhaseAt(Jab, 17));
        }

        [Test]
        public void ZeroStartup_IsActiveImmediately()
        {
            var f = new FrameData(0, 2, 5);
            Assert.AreEqual(AttackPhase.Active, AttackTimeline.PhaseAt(f, 0));
            Assert.IsTrue(AttackTimeline.IsFirstActiveFrame(f, 0));
        }

        [Test]
        public void CancelWindow_IsTheTailOfRecovery()
        {
            Assert.IsFalse(AttackTimeline.InCancelWindow(Jab, 10));
            Assert.IsTrue(AttackTimeline.InCancelWindow(Jab, 11));
            Assert.IsTrue(AttackTimeline.InCancelWindow(Jab, 16));
            Assert.IsFalse(AttackTimeline.InCancelWindow(Jab, 17));
            Assert.IsFalse(AttackTimeline.InCancelWindow(new FrameData(4, 3, 10, 0), 15));
        }

        [Test]
        public void FirstActiveFrame_OnlyOnce()
        {
            Assert.IsFalse(AttackTimeline.IsFirstActiveFrame(Jab, 3));
            Assert.IsTrue(AttackTimeline.IsFirstActiveFrame(Jab, 4));
            Assert.IsFalse(AttackTimeline.IsFirstActiveFrame(Jab, 5));
        }

        [Test]
        public void Validate_FlagsBadFrameData()
        {
            Assert.AreEqual(0, Jab.Validate().Count);
            Assert.AreEqual(3, new FrameData(-1, 0, 2, 5).Validate().Count);
        }

        [Test]
        public void SwingHitLog_HitsEachTargetOnce()
        {
            var log = new SwingHitLog();
            Assert.IsTrue(log.TryRegister(7));
            Assert.IsFalse(log.TryRegister(7));
            Assert.IsTrue(log.TryRegister(9));
            Assert.AreEqual(2, log.Count);
            log.Reset();
            Assert.IsTrue(log.TryRegister(7));
        }
    }
}
