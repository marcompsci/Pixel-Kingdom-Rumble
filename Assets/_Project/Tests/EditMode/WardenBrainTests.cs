using System.Collections.Generic;
using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    [TestFixture]
    public class WardenBrainTests
    {
        static WardenBrain Make() => new WardenBrain(-10f, 10f);

        /// <summary>Tick until the action changes; returns the new action.</summary>
        static WardenAction Advance(WardenBrain b, float heroX = 0f)
        {
            for (int i = 0; i < 10000; i++)
                if (b.Tick(0.01f, heroX)) return b.Current;
            Assert.IsTrue(false, "action never changed");
            return b.Current;
        }

        [Test]
        public void PhaseOne_Pattern_SlamSweepSlam_WithStaggerAfterEachSlam()
        {
            var b = Make();
            Assert.AreEqual(WardenAction.Idle, b.Current);
            var seen = new List<WardenAction>();
            for (int i = 0; i < 11; i++) seen.Add(Advance(b));
            var expected = new[]
            {
                WardenAction.SlamWindup, WardenAction.Slam, WardenAction.Stagger, WardenAction.Idle,
                WardenAction.SweepWindup, WardenAction.Sweep, WardenAction.Idle,
                WardenAction.SlamWindup, WardenAction.Slam, WardenAction.Stagger, WardenAction.Idle
            };
            for (int i = 0; i < expected.Length; i++) Assert.AreEqual(expected[i], seen[i], $"step {i}");
        }

        [Test]
        public void OnlyVulnerableWhileStaggered()
        {
            var b = Make();
            int vulnerableStates = 0;
            for (int i = 0; i < 12; i++)
            {
                Advance(b);
                if (b.IsVulnerable)
                {
                    Assert.AreEqual(WardenAction.Stagger, b.Current);
                    vulnerableStates++;
                }
            }
            Assert.Greater(vulnerableStates, 0);
        }

        [Test]
        public void Slam_TracksHeroThenLocksOn()
        {
            var b = Make();
            Assert.AreEqual(WardenAction.SlamWindup, Advance(b, 3f));
            b.Tick(0.05f, 50f);
            Assert.AreEqual(10f, b.SlamX, 0.001f, "clamped to the arena while tracking");
            b.Tick(0.05f, 4f);
            Assert.AreEqual(4f, b.SlamX, 0.001f, "tracks early in the windup");
            b.Tick(0.55f, -8f); // now past the tracking window (0.6 of 0.9 s)
            b.Tick(0.05f, -8f);
            Assert.AreNotEqual(-8f, b.SlamX, "locked on: last-second dodges work");
            Assert.AreEqual(4f, b.SlamX, 0.001f);
        }

        [Test]
        public void Phases_FollowHealth_OnlyForward_AndSpeedUp()
        {
            var b = Make();
            Assert.IsFalse(b.SetHealthFraction(0.9f));
            Assert.IsTrue(b.SetHealthFraction(0.6f));
            Assert.AreEqual(WardenPhase.Two, b.Phase);
            Assert.AreEqual(0.85f, b.SpeedScale, 0.001f);
            Assert.IsFalse(b.SetHealthFraction(0.9f), "never goes back");
            Assert.IsTrue(b.SetHealthFraction(0.1f));
            Assert.AreEqual(WardenPhase.Three, b.Phase);
        }

        [Test]
        public void PhaseTwo_AddsVolley()
        {
            var b = Make();
            b.SetHealthFraction(0.5f);
            bool volley = false;
            for (int i = 0; i < 20 && !volley; i++) volley = Advance(b) == WardenAction.VolleyWindup;
            Assert.IsTrue(volley);
        }

        [Test]
        public void Sweeps_AlternateSides()
        {
            var b = Make();
            var dirs = new List<int>();
            for (int i = 0; i < 40 && dirs.Count < 2; i++)
                if (Advance(b) == WardenAction.SweepWindup) dirs.Add(b.SweepDir);
            Assert.AreEqual(2, dirs.Count);
            Assert.AreEqual(-dirs[0], dirs[1]);
        }

        [Test]
        public void Defeat_StopsEverything_ResetRestarts()
        {
            var b = Make();
            b.SetHealthFraction(0.2f);
            b.Defeat();
            Assert.IsTrue(b.IsDefeated);
            Assert.IsFalse(b.Tick(100f, 0f));
            Assert.IsFalse(b.SetHealthFraction(0f));
            b.Reset();
            Assert.AreEqual(WardenPhase.One, b.Phase);
            Assert.AreEqual(WardenAction.Idle, b.Current);
        }

        [Test]
        public void VolleyPositions_SpreadAroundHero_Clamped()
        {
            var b = Make();
            var xs = b.VolleyPositions(9f, 3, 3f);
            Assert.AreEqual(3, xs.Length);
            Assert.AreEqual(6f, xs[0], 0.001f);
            Assert.AreEqual(9f, xs[1], 0.001f);
            Assert.AreEqual(10f, xs[2], 0.001f, "clamped to the arena");
            Assert.AreEqual(0, b.VolleyPositions(0f, 0, 3f).Length);
        }
    }
}
