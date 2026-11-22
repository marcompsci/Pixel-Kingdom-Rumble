using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    [TestFixture]
    public class PlayerMeta8Tests
    {
        [Test]
        public void RankTitle_ClimbsWithBadges()
        {
            Assert.AreEqual("SQUIRE", RankTitle.For(0));
            Assert.AreEqual("ADVENTURER", RankTitle.For(5));
            Assert.AreEqual("KNIGHT", RankTitle.For(17));
            Assert.AreEqual("MYTH", RankTitle.For(40));
            Assert.AreEqual("ADVENTURER in 2 badges", RankTitle.Next(3));
            Assert.AreEqual("", RankTitle.Next(40));
        }

        [Test]
        public void TodayPlay_ResetsAtMidnight()
        {
            var s = SaveData.CreateNew();
            Assert.AreEqual("", TodayPlay.Line(s, 20261121));
            TodayPlay.Add(s, 20261121, 900f);
            TodayPlay.Add(s, 20261121, 600f);
            Assert.AreEqual(1500f, TodayPlay.Seconds(s, 20261121), 0.01f);
            StringAssert.Contains("25m today", TodayPlay.Line(s, 20261121));
            Assert.AreEqual(0f, TodayPlay.Seconds(s, 20261122), "a new day");
            TodayPlay.Add(s, 20261122, 30f);
            Assert.AreEqual(30f, TodayPlay.Seconds(s, 20261122), 0.01f);
        }

        [Test]
        public void TimeBadges_NightOwlEarlyBird()
        {
            var s = SaveData.CreateNew();
            TimeBadges.Mark(s, 14);
            Badges.Update(s);
            Assert.IsFalse(Badges.Has(s, "night_owl"));
            TimeBadges.Mark(s, 2);
            TimeBadges.Mark(s, 6);
            Badges.Update(s);
            Assert.IsTrue(Badges.Has(s, "night_owl"));
            Assert.IsTrue(Badges.Has(s, "early_bird"));
        }

        [Test]
        public void Contracts_VsBestAndBadge()
        {
            Assert.AreEqual("", ContractMeta.VsBest(500, 0));
            Assert.AreEqual("+150 over your best", ContractMeta.VsBest(650, 500));
            Assert.AreEqual("120 short of your best", ContractMeta.VsBest(380, 500));
            var s = SaveData.CreateNew();
            for (int i = 0; i < 10; i++) s.RecordMission("ms_" + i, 100 + i, 0);
            Assert.AreEqual(10, ContractMeta.Cleared(s));
            Badges.Update(s);
            Assert.IsTrue(Badges.Has(s, "contracts10"));
        }

        [Test]
        public void StandTime_LabRecord_PerfectRounds()
        {
            var s = SaveData.CreateNew();
            Assert.IsFalse(StandTime.Record(s, 120f), "the first run sets the bar quietly");
            Assert.IsTrue(StandTime.Record(s, 300f));
            Assert.IsFalse(StandTime.Record(s, 200f));
            Assert.AreEqual(300f, s.lifetime.longestStand, 0.01f);
            Assert.IsTrue(LabRecord.Offer(s, 4));
            Assert.IsFalse(LabRecord.Offer(s, 3));
            s.lifetime.vsPerfectRounds = 5;
            Badges.Update(s);
            Assert.IsTrue(Badges.Has(s, "vs_perfect5"));
            Assert.GreaterOrEqual(Badges.All.Length, 40);
            s.lifetime.todaySeconds = float.NaN; s.lifetime.longestStand = -5f;
            s.SanitizeAndMigrate();
            Assert.AreEqual(0f, s.lifetime.todaySeconds);
            Assert.AreEqual(0f, s.lifetime.longestStand);
        }
    }
}
