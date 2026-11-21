using System;
using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    [TestFixture]
    public class PlayerMeta4Tests
    {
        [Test]
        public void VersusStreak_CountsAndResets()
        {
            var s = SaveData.CreateNew();
            Assert.AreEqual(1, VersusStreak.Record(s, true));
            Assert.AreEqual(2, VersusStreak.Record(s, true));
            Assert.AreEqual(3, VersusStreak.Record(s, true));
            Assert.AreEqual(0, VersusStreak.Record(s, false));
            Assert.AreEqual(3, s.lifetime.vsBestStreak);
            Assert.IsTrue(VersusStreak.Milestone(3));
            Assert.IsFalse(VersusStreak.Milestone(4));
            Assert.IsTrue(VersusStreak.Milestone(10));
            s.lifetime.vsStreak = -2; s.lifetime.vsBestStreak = -1;
            s.SanitizeAndMigrate();
            Assert.AreEqual(0, s.lifetime.vsStreak);
            Assert.AreEqual(0, s.lifetime.vsBestStreak);
        }

        [Test]
        public void CleanStreak_AndCloseCall()
        {
            Assert.AreEqual(3, CleanStreak.Next(2, 0));
            Assert.AreEqual(0, CleanStreak.Next(5, 1));
            Assert.IsTrue(CleanStreak.Milestone(5));
            Assert.IsTrue(StoryResults.CloseCall(1, 3));
            Assert.IsFalse(StoryResults.CloseCall(1, 1), "BRAVE's single heart isn't a close call");
            Assert.IsFalse(StoryResults.CloseCall(2, 3));
        }

        [Test]
        public void VsBest_Lines()
        {
            Assert.AreEqual("", StoryResults.VsBest(50f, 0f));
            Assert.AreEqual("1.5s faster than your old best!", StoryResults.VsBest(48.5f, 50f));
            Assert.AreEqual("0.8s off your best", StoryResults.VsBest(50.8f, 50f));
            Assert.AreEqual("matched your best!", StoryResults.VsBest(50.02f, 50f));
        }

        [Test]
        public void StoryRecords_TriesToughestFastest()
        {
            var s = SaveData.CreateNew();
            Assert.IsNull(StoryRecords.Toughest(s));
            Assert.IsNull(StoryRecords.Fastest(s));
            s.GetOrAddLevel("a").attempts = 2;
            s.GetOrAddLevel("b").attempts = 9;
            s.RecordLevelResult("c", 40f, 0, 0);
            s.GetLevel("c").attempts = 4;
            s.RecordLevelResult("d", 25f, 0, 0);
            Assert.AreEqual(15, StoryRecords.TotalTries(s));
            Assert.AreEqual("b", StoryRecords.Toughest(s).levelId);
            Assert.AreEqual("d", StoryRecords.Fastest(s).levelId);
        }

        [Test]
        public void Greeting_CoversTheDay()
        {
            StringAssert.Contains("morning", Greeting.For(8));
            StringAssert.Contains("afternoon", Greeting.For(13));
            StringAssert.Contains("evening", Greeting.For(19));
            StringAssert.Contains("late", Greeting.For(2));
            Assert.AreEqual(Greeting.For(1), Greeting.For(25));
        }

        [Test]
        public void SessionSummary_LinesAndChanged()
        {
            var a = new[] { 1, 10, 0, 0, 100 };
            var b = new[] { 3, 10, 1, 0, 900 };
            var lines = SessionSummary.Lines(a, b);
            Assert.AreEqual(2, lines.Count, "time played is left out");
            StringAssert.Contains("+2", lines[0]);
            Assert.IsTrue(SessionSummary.Changed(a, b));
            Assert.IsFalse(SessionSummary.Changed(new[] { 1, 10, 0, 0, 5 }, new[] { 1, 10, 0, 0, 99 }), "time alone isn't news");
            Assert.IsTrue(SessionSummary.Changed(null, b));
            Assert.AreEqual(0, SessionSummary.Lines(a, new[] { 1 }).Count);
        }

        [Test]
        public void StandLast_AndKillsPerRound()
        {
            var s = SaveData.CreateNew();
            Assert.IsNull(StandLast.Latest(s));
            StandHistory.Add(s, new StandRunRecord { map = "pier", round = 4 });
            StandHistory.Add(s, new StandRunRecord { map = "block", round = 9 });
            Assert.AreEqual("block", StandLast.Latest(s).map);
            Assert.AreEqual("4.5 kills per round", StandLast.KillsPerRound(45, 10));
            Assert.AreEqual("", StandLast.KillsPerRound(5, 0));
        }

        [Test]
        public void NewBadges_FromStreakDaysTriesRuns()
        {
            Assert.AreEqual(22, Badges.All.Length);
            var s = SaveData.CreateNew();
            s.lifetime.vsBestStreak = 5;
            s.lifetime.loginGifts = 30;
            s.GetOrAddLevel("x").attempts = 100;
            s.zombieCareer.lastStandRuns = 50;
            var fresh = Badges.Update(s);
            foreach (var id in new[] { "streak_vs5", "days30", "tries100", "runs50" }) Assert.IsTrue(Badges.Has(s, id), id);
            Assert.AreEqual(4, fresh.Count);
        }
    }
}
