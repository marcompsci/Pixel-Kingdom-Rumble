using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    [TestFixture]
    public class BotBrainTests
    {
        static BotView Base() => new BotView
        {
            position = new Vec2(0f, 0.7f), grounded = true, canAirDash = true, hasTarget = true,
            targetPosition = new Vec2(5f, 0.7f), stageLeft = -9f, stageRight = 9f, stageTop = 0f
        };

        static readonly BotDifficulty Perfect = new BotDifficulty
        {
            reactionTime = 0.1f, accuracy = 1f, aggression = 1f, recoverySkill = 1f, dodgeChance = 1f
        };

        [Test]
        public void Offstage_RecoversTowardCenterWithAirDash()
        {
            var v = Base();
            v.position = new Vec2(-12f, -2f); v.grounded = false; v.velocity = new Vec2(0f, -3f);
            var c = BotBrain.Decide(v, Perfect, new System.Random(1));
            Assert.AreEqual(1f, c.moveX, 0.001f);
            Assert.IsTrue(c.dodge);
            Assert.AreEqual(1f, c.moveY, 0.001f);
            Assert.IsFalse(c.attack);
        }

        [Test]
        public void Offstage_WithoutDash_StillDriftsHome()
        {
            var v = Base();
            v.position = new Vec2(12f, -1f); v.grounded = false; v.canAirDash = false;
            var c = BotBrain.Decide(v, Perfect, new System.Random(1));
            Assert.AreEqual(-1f, c.moveX, 0.001f);
            Assert.IsFalse(c.dodge);
        }

        [Test]
        public void Far_Approaches()
        {
            var v = Base(); v.targetPosition = new Vec2(8f, 0.7f); // beyond projectile range
            var c = BotBrain.Decide(v, Perfect, new System.Random(1));
            Assert.Greater(c.moveX, 0.5f);
        }

        [Test]
        public void MidRange_SometimesThrowsProjectile()
        {
            var v = Base(); v.targetPosition = new Vec2(5f, 0.7f); v.hasProjectile = true;
            int specials = 0;
            var rng = new System.Random(7);
            for (int i = 0; i < 200; i++) if (BotBrain.Decide(v, Perfect, rng).special) specials++;
            Assert.Greater(specials, 20);   // ~25% with perfect accuracy
            Assert.Less(specials, 90);
        }

        [Test]
        public void MidRange_NoProjectile_NeverUsesSpecialFromRange()
        {
            var v = Base(); v.targetPosition = new Vec2(5f, 0.7f); v.hasProjectile = false;
            var rng = new System.Random(7);
            for (int i = 0; i < 200; i++) Assert.IsFalse(BotBrain.Decide(v, Perfect, rng).special);
        }

        [Test]
        public void InRange_Attacks()
        {
            var v = Base(); v.targetPosition = new Vec2(1f, 0.7f);
            var c = BotBrain.Decide(v, Perfect, new System.Random(1));
            Assert.IsTrue(c.attack);
        }

        [Test]
        public void TargetAbove_UsesUpAttack()
        {
            var v = Base(); v.targetPosition = new Vec2(0.5f, 2.5f);
            var c = BotBrain.Decide(v, Perfect, new System.Random(1));
            Assert.IsTrue(c.attack);
            Assert.AreEqual(1f, c.moveY, 0.001f);
        }

        [Test]
        public void NeverWalksOffEdgeWhileGrounded()
        {
            var v = Base();
            v.position = new Vec2(8.6f, 0.7f);
            v.targetPosition = new Vec2(14f, 0.7f); // target is off to the right
            var c = BotBrain.Decide(v, Perfect, new System.Random(1));
            Assert.AreEqual(0f, c.moveX, 0.001f);
        }

        [Test]
        public void OpenGap_IsTreatedLikeAnEdge()
        {
            var v = Base();
            v.hasGap = true; v.gapLeft = -2f; v.gapRight = 2f;
            v.position = new Vec2(-2.3f, 0.7f);
            v.targetPosition = new Vec2(8f, 0.7f); // rival across the gap
            var c = BotBrain.Decide(v, Perfect, new System.Random(1));
            Assert.AreEqual(0f, c.moveX, 0.001f);
        }

        [Test]
        public void FallingIntoGap_RecoversToNearestPlatform()
        {
            var v = Base();
            v.hasGap = true; v.gapLeft = -2f; v.gapRight = 2f;
            v.position = new Vec2(0.5f, -2f); v.grounded = false;
            var c = BotBrain.Decide(v, Perfect, new System.Random(1));
            Assert.AreEqual(1f, c.moveX, 0.001f); // right platform center (5.5) is nearer than left (-5.5)
        }

        [Test]
        public void Exposed_BacksOffOrDodges()
        {
            var v = Base(); v.exposed = true; v.targetPosition = new Vec2(1.5f, 0.7f);
            var c = BotBrain.Decide(v, Perfect, new System.Random(1));
            Assert.Less(c.moveX, 0f);
            Assert.IsTrue(c.dodge);
        }

        [Test]
        public void NoTarget_Idles()
        {
            var v = Base(); v.hasTarget = false;
            var c = BotBrain.Decide(v, Perfect, new System.Random(1));
            Assert.AreEqual(0f, c.moveX, 0.001f);
            Assert.IsFalse(c.attack || c.jump || c.special || c.dodge);
        }

        [Test]
        public void Difficulty_PresetsAreOrdered()
        {
            var e = BotDifficulty.For(BotLevel.Easy);
            var n = BotDifficulty.For(BotLevel.Normal);
            var h = BotDifficulty.For(BotLevel.Hard);
            Assert.Greater(e.reactionTime, n.reactionTime);
            Assert.Greater(n.reactionTime, h.reactionTime);
            Assert.Less(e.accuracy, h.accuracy);
        }

        [Test]
        public void Offstage_UsesAirJumpBeforeAirDash()
        {
            var v = Base();
            v.position = new Vec2(-12f, -2f); v.grounded = false; v.velocity = new Vec2(0f, -3f);
            v.canAirJump = true;
            var c = BotBrain.Decide(v, Perfect, new System.Random(1));
            Assert.IsTrue(c.jump);
            Assert.IsTrue(c.jumpHeld);
            Assert.IsFalse(c.dodge);
            Assert.AreEqual(1f, c.moveX, 0.001f);
        }

        [Test]
        public void Offstage_Rising_SavesRecovery()
        {
            var v = Base();
            v.position = new Vec2(-12f, -2f); v.grounded = false; v.velocity = new Vec2(0f, 4f);
            v.canAirJump = true;
            var c = BotBrain.Decide(v, Perfect, new System.Random(1));
            Assert.IsFalse(c.jump);
            Assert.IsFalse(c.dodge);
            Assert.IsTrue(c.jumpHeld, "keeps holding so the rising jump isn't cut");
        }
    }

    [TestFixture]
    public class ArenaConfigTests
    {
        [Test]
        public void Clamp_KeepsValuesInRange()
        {
            var c = new ArenaMatchConfig { botCount = 9, stocks = 0, minutes = 99 };
            c.Clamp();
            Assert.AreEqual(3, c.botCount);
            Assert.AreEqual(1, c.stocks);
            Assert.AreEqual(5, c.minutes);
            Assert.AreEqual(4, c.FighterCount);
        }

        [Test]
        public void CreateMatch_UsesSettings()
        {
            var c = new ArenaMatchConfig { mode = MatchMode.Timed, botCount = 1, minutes = 2 };
            var m = c.CreateMatch();
            Assert.AreEqual(2, m.Fighters.Count);
            Assert.AreEqual(120f, m.DurationSeconds, 0.001f);
        }

        [Test]
        public void Cycles_Wrap()
        {
            Assert.AreEqual(1, ArenaMatchConfig.Cycle(3, 1, 3));
            Assert.AreEqual(3, ArenaMatchConfig.Cycle(2, 1, 3));
            Assert.AreEqual(MatchMode.Timed, ArenaMatchConfig.Next(MatchMode.Stock));
            Assert.AreEqual(MatchMode.Stock, ArenaMatchConfig.Next(MatchMode.Training));
            Assert.AreEqual(BotLevel.Easy, ArenaMatchConfig.Next(BotLevel.Hard));
        }

        [Test]
        public void BridgeCycle_Phases()
        {
            var b = new BridgeCycle(10f, 2f, 5f);
            Assert.AreEqual(17f, b.Period, 0.001f);
            Assert.AreEqual(BridgePhase.Solid, b.PhaseAt(0f));
            Assert.AreEqual(BridgePhase.Warning, b.PhaseAt(10.5f));
            Assert.AreEqual(BridgePhase.Open, b.PhaseAt(13f));
            Assert.AreEqual(BridgePhase.Solid, b.PhaseAt(17.5f)); // wraps
            Assert.AreEqual(BridgePhase.Open, b.PhaseAt(-2f));    // negative time wraps too
        }
    
    }
}
