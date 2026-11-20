using System;
using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    [TestFixture]
    public class PlayerMeta3Tests
    {
        [Test]
        public void Attempts_CountAndLines()
        {
            var s = SaveData.CreateNew();
            var r = s.GetOrAddLevel("lv1");
            Assert.IsFalse(r.completed, "a started level isn't cleared");
            StoryAttempts.Begin(r); StoryAttempts.Begin(r); StoryAttempts.Begin(r);
            Assert.AreEqual(3, s.GetLevel("lv1").attempts);
            Assert.IsTrue(ReferenceEquals(r, s.GetOrAddLevel("lv1")));
            Assert.AreEqual("Cleared on attempt 3!", StoryAttempts.ClearLine(3, true));
            Assert.AreEqual("Cleared on your first try!", StoryAttempts.ClearLine(1, true));
            Assert.AreEqual("Attempt 7", StoryAttempts.ClearLine(7, false));
            Assert.AreEqual("", StoryAttempts.TriesLine(0));
            Assert.AreEqual("3 tries so far", StoryAttempts.TriesLine(3));
            r.attempts = -4; s.SanitizeAndMigrate();
            Assert.AreEqual(0, r.attempts);
            Assert.AreEqual(0, Badges.Update(s).Count, "an attempted level earns no clear badge");
        }

        [Test]
        public void Completion_AndSpeedrunTotal()
        {
            Assert.AreEqual(0, StoryCompletion.Percent(0, 4, 0, 10, 0, 0));
            Assert.AreEqual(50, StoryCompletion.Percent(2, 4, 5, 10, 0, 0));
            Assert.AreEqual(100, StoryCompletion.Percent(4, 4, 12, 10, 3, 3), "capped");
            Assert.AreEqual(0, StoryCompletion.Percent(0, 0, 0, 0, 0, 0));
            var s = SaveData.CreateNew();
            s.RecordLevelResult("a", 60f, 0, 0);
            s.RecordLevelResult("b", 30.5f, 0, 0);
            s.GetOrAddLevel("c");
            var t = StorySpeedrun.Total(s, new[] { "a", "b", "c", "missing" });
            Assert.AreEqual(2, t.levels);
            Assert.AreEqual(90.5f, t.seconds, 0.001f);
        }

        [Test]
        public void HeroRecords_RecordBestAndSanitize()
        {
            var s = SaveData.CreateNew();
            Assert.IsNull(HeroRecords.Best(s));
            HeroRecords.Record(s, "luma", true);
            HeroRecords.Record(s, "luma", false);
            HeroRecords.Record(s, "rex", true);
            HeroRecords.Record(s, "rex", true);
            HeroRecords.Record(s, null, true);
            Assert.AreEqual("rex", HeroRecords.Best(s).id);
            Assert.AreEqual("YOUR RECORD 1W - 1L", HeroRecords.Line(HeroRecords.Get(s, "luma")));
            s.lifetime.heroRecords.Add(null);
            s.lifetime.heroRecords[0].losses = -3;
            s.SanitizeAndMigrate();
            Assert.AreEqual(2, s.lifetime.heroRecords.Count);
            Assert.AreEqual(0, s.lifetime.heroRecords[0].losses);
        }

        [Test]
        public void StandPace_Lines()
        {
            Assert.AreEqual("", StandPace.VsAverage(10, 0f, 0));
            Assert.AreEqual("3 rounds above your average!", StandPace.VsAverage(12, 9f, 4));
            Assert.AreEqual("2.5 rounds below your average", StandPace.VsAverage(6, 8.5f, 2));
            Assert.AreEqual("right on your average", StandPace.VsAverage(9, 9.2f, 2));
            Assert.IsTrue(StandPace.IsNewBestRound(15, 14));
            Assert.IsFalse(StandPace.IsNewBestRound(16, 14), "only once");
            Assert.IsFalse(StandPace.IsNewBestRound(1, 0), "no best yet");
        }

        [Test]
        public void Comeback_AfterAWeek()
        {
            var today = new DateTime(2026, 11, 20);
            Assert.AreEqual(0, ComebackGift.For(0, today), "first launch isn't a comeback");
            Assert.AreEqual(0, ComebackGift.For(20261114, today));
            Assert.AreEqual(ComebackGift.Shards, ComebackGift.For(20261113, today));
            Assert.AreEqual(31, ComebackGift.DaysAway(20261020, today));
            Assert.AreEqual(0, ComebackGift.DaysAway(20261399, today), "a bad key is ignored");
        }

        [Test]
        public void Badges_ProgressNextGoalAndNewOnes()
        {
            Assert.AreEqual(18, Badges.All.Length);
            var ids = new System.Collections.Generic.HashSet<string>();
            foreach (var b in Badges.All) Assert.IsTrue(ids.Add(b.id), "unique id " + b.id);
            var s = SaveData.CreateNew();
            for (int i = 0; i < 7; i++) s.RecordLevelResult("lv" + i, 50f, 0, 2);
            Badges.Update(s);
            Assert.IsTrue(Badges.Has(s, "first_clear"));
            Assert.IsTrue(Badges.Has(s, "secrets10"), "14 secrets found");
            var ten = Array.Find(Badges.All, b => b.id == "ten_clears");
            Assert.AreEqual(7, ten.Have(s));
            var next = Badges.Closest(s);
            Assert.IsNotNull(next);
            Assert.AreEqual("ten_clears", next.Value.id, "70% of the way is the closest");
            StringAssert.Contains("TRAILBLAZER", Badges.NextGoal(s));
            StringAssert.Contains("(7/10)", Badges.NextGoal(s));
        }

        [Test]
        public void Tips_Cycle()
        {
            int i = TipOfTheDay.Tips.Length - 1;
            Assert.AreEqual(TipOfTheDay.Tips[0], TipOfTheDay.Next(ref i));
            Assert.AreEqual(0, i);
            Assert.AreEqual(TipOfTheDay.For(20261120), TipOfTheDay.Tips[TipOfTheDay.Index(20261120)]);
        }
    }
}
