using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    [TestFixture]
    public class VersusMatchTests
    {
        [Test]
        public void FirstToTwoRounds_WinsTheMatch()
        {
            var m = new VersusMatch();
            m.StartRound();
            Assert.AreEqual(1, m.Round);
            Assert.AreEqual(VersusPhase.Fighting, m.Phase);
            m.ReportKO(1);
            Assert.AreEqual(VersusPhase.RoundOver, m.Phase);
            Assert.AreEqual(0, m.LastRoundWinner);
            m.StartRound();
            m.ReportKO(0);
            Assert.AreEqual(1, m.Wins(0)); Assert.AreEqual(1, m.Wins(1));
            m.StartRound();
            Assert.AreEqual(3, m.Round);
            m.ReportKO(0);
            Assert.AreEqual(VersusPhase.MatchOver, m.Phase);
            Assert.AreEqual(1, m.Winner);
        }

        [Test]
        public void Clock_RunsOut_AndHealthDecides()
        {
            var m = new VersusMatch(2, 10f);
            m.StartRound();
            Assert.IsFalse(m.Tick(4f));
            Assert.AreEqual(6, m.ClockSeconds);
            Assert.IsTrue(m.Tick(7f));
            Assert.AreEqual(0f, m.TimeLeft, 0.0001);
            m.EndRoundOnTime(0.4f, 0.7f);
            Assert.AreEqual(1, m.LastRoundWinner);
            Assert.IsTrue(m.LastRoundWasTimeUp);
            Assert.AreEqual(0, VersusMatch.TimeUpWinner(0.5f, 0.2f));
            Assert.AreEqual(-1, VersusMatch.TimeUpWinner(0.5f, 0.5f));
        }

        [Test]
        public void Draws_ScoreNothing_AndMaxRoundsEndsTheMatch()
        {
            var m = new VersusMatch();
            for (int i = 0; i < VersusMatch.MaxRounds; i++)
            {
                m.StartRound();
                m.ReportDoubleKO();
            }
            Assert.AreEqual(VersusPhase.MatchOver, m.Phase);
            Assert.AreEqual(-1, m.Winner);
            Assert.AreEqual(0, m.Wins(0) + m.Wins(1));

            var lead = new VersusMatch();
            lead.StartRound(); lead.ReportKO(1);
            for (int i = 1; i < VersusMatch.MaxRounds; i++) { lead.StartRound(); lead.EndRoundOnTime(0.5f, 0.5f); }
            Assert.AreEqual(0, lead.Winner, "the leader wins after the last round");
        }

        [Test]
        public void Reports_OutsideAFight_AreIgnored()
        {
            var m = new VersusMatch();
            m.ReportKO(0);
            Assert.AreEqual(VersusPhase.Waiting, m.Phase);
            Assert.IsFalse(m.Tick(1f));
            m.StartRound();
            m.StartRound(); // already fighting: no double start
            Assert.AreEqual(1, m.Round);
            m.ReportKO(5);
            Assert.AreEqual(VersusPhase.Fighting, m.Phase);
            m.ReportKO(1);
            m.ReportKO(0); // round already over
            Assert.AreEqual(1, m.Wins(0));
            Assert.AreEqual(0, m.Wins(1));
        }

        [Test]
        public void Health_ScalesWithWeight_WithinBounds()
        {
            Assert.AreEqual(VersusRules.BaseHealth, VersusRules.HealthFor(1f));
            Assert.Greater(VersusRules.HealthFor(1.6f), VersusRules.HealthFor(1f));
            Assert.Less(VersusRules.HealthFor(0.7f), VersusRules.HealthFor(1f));
            Assert.AreEqual(34, VersusRules.HealthFor(10f));
            Assert.AreEqual(16, VersusRules.HealthFor(-10f));
            Assert.Greater(VersusRules.Reward(true, true), VersusRules.Reward(true, false));
            Assert.Greater(VersusRules.Reward(true, false), VersusRules.Reward(false, false));
        }
    }
}
