using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    [TestFixture]
    public class JumpPhysicsTests
    {
        [Test]
        public void GravityAndVelocity_MatchClosedForm()
        {
            Assert.AreEqual(2f * 3f / (0.4f * 0.4f), JumpPhysics.Gravity(3f, 0.4f), 0.001f);
            Assert.AreEqual(15f, JumpPhysics.JumpVelocity(3f, 0.4f), 0.001f);
        }

        [Test]
        public void Simulated60Hz_JumpPeaksNearAuthoredHeight()
        {
            var s = new MovementStats { jumpHeight = 3.2f, timeToApex = 0.38f };
            float g = JumpPhysics.Gravity(s.jumpHeight, s.timeToApex);
            float vy = JumpPhysics.JumpVelocity(s.jumpHeight, s.timeToApex);
            float y = 0f, peak = 0f, t = 0f, peakTime = 0f;
            const float dt = 1f / 60f;
            // Semi-implicit Euler, gravity multiplier 1 (jump not held -> no apex hang) like the motor's rising phase.
            while (vy > 0f)
            {
                vy -= g * dt;
                y += vy * dt;
                t += dt;
                if (y > peak) { peak = y; peakTime = t; }
            }
            Assert.AreEqual(s.jumpHeight, peak, s.jumpHeight * 0.06f);
            Assert.AreEqual(s.timeToApex, peakTime, 0.034f);
        }

        [Test]
        public void GravityMultiplier_SelectsApexFallOrNormal()
        {
            var s = new MovementStats();
            Assert.AreEqual(s.apexGravityMultiplier, JumpPhysics.GravityMultiplier(0.5f, true, s), 0.0001f);
            Assert.AreEqual(s.fallGravityMultiplier, JumpPhysics.GravityMultiplier(-0.5f, false, s), 0.0001f);
            Assert.AreEqual(s.fallGravityMultiplier, JumpPhysics.GravityMultiplier(-5f, true, s), 0.0001f);
            Assert.AreEqual(1f, JumpPhysics.GravityMultiplier(5f, true, s), 0.0001f);
        }

        [Test]
        public void Horizontal_AcceleratesToRunSpeed_AndStops()
        {
            var s = new MovementStats { runSpeed = 8f, groundAcceleration = 80f, groundDeceleration = 80f };
            float v = 0f;
            v = JumpPhysics.NextHorizontalVelocity(v, 1f, s, true, 0.05f);
            Assert.AreEqual(4f, v, 0.0001f);
            v = JumpPhysics.NextHorizontalVelocity(v, 1f, s, true, 1f);
            Assert.AreEqual(8f, v, 0.0001f);
            v = JumpPhysics.NextHorizontalVelocity(v, 0f, s, true, 1f);
            Assert.AreEqual(0f, v, 0.0001f);
        }

        [Test]
        public void Horizontal_TurnaroundUsesStrongerRate_InAir()
        {
            var s = new MovementStats { runSpeed = 8f, airAcceleration = 40f, airDeceleration = 10f };
            float v = JumpPhysics.NextHorizontalVelocity(8f, -1f, s, false, 0.1f);
            Assert.AreEqual(4f, v, 0.0001f); // 40 * 0.1
            float coast = JumpPhysics.NextHorizontalVelocity(8f, 0f, s, false, 0.1f);
            Assert.AreEqual(7f, coast, 0.0001f); // air decel 10 * 0.1
        }

        [Test]
        public void Horizontal_InputIsClamped()
        {
            var s = new MovementStats { runSpeed = 5f, groundAcceleration = 1000f };
            Assert.AreEqual(5f, JumpPhysics.NextHorizontalVelocity(0f, 3f, s, true, 1f), 0.0001f);
        }

        [Test]
        public void SnapToEightWay_Directions()
        {
            Assert.IsFalse(JumpPhysics.SnapToEightWay(0.05f, 0.05f, 0.3f, out _));
            JumpPhysics.SnapToEightWay(0.9f, 0.1f, 0.3f, out var right);
            Assert.AreEqual(1f, right.x, 0.0001f); Assert.AreEqual(0f, right.y, 0.0001f);
            JumpPhysics.SnapToEightWay(-0.7f, 0.7f, 0.3f, out var upLeft);
            Assert.AreEqual(-0.7071f, upLeft.x, 0.001f); Assert.AreEqual(0.7071f, upLeft.y, 0.001f);
            JumpPhysics.SnapToEightWay(0.1f, -1f, 0.3f, out var down);
            Assert.AreEqual(0f, down.x, 0.0001f); Assert.AreEqual(-1f, down.y, 0.0001f);
        }

        [Test]
        public void MovementStats_DefaultsAreValid_AndBadValuesReported()
        {
            Assert.AreEqual(0, new MovementStats().Validate().Count);
            var bad = new MovementStats { runSpeed = 0f, timeToApex = 0f, jumpCutMultiplier = 2f, airJumps = -1 };
            Assert.AreEqual(4, bad.Validate().Count);
        }
    }

    [TestFixture]
    public class ActionBufferTests
    {
        [Test]
        public void PressIsConsumableOnce()
        {
            var b = new ActionBuffer(0.1f);
            b.Press(1f);
            Assert.IsTrue(b.Consume(1.05f));
            Assert.IsFalse(b.Consume(1.06f));
        }

        [Test]
        public void PressExpiresAfterWindow()
        {
            var b = new ActionBuffer(0.1f);
            b.Press(1f);
            Assert.IsFalse(b.Consume(1.2f));
        }

        [Test]
        public void PressCountAndHeld()
        {
            var b = new ActionBuffer();
            Assert.IsFalse(b.Held);
            b.Press(0f);
            b.Press(0.5f);
            Assert.AreEqual(2, b.PressCount);
            Assert.IsTrue(b.Held);
            b.SetHeld(false);
            Assert.IsFalse(b.Held);
            b.Clear();
            Assert.IsFalse(b.Consume(0.5f));
        }
    }
}
