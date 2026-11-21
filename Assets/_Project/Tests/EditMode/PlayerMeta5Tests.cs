using System;
using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    [TestFixture]
    public class PlayerMeta5Tests
    {
        [Test]
        public void Perfect_RulesAndOneOffShards()
        {
            Assert.IsTrue(StoryPerfect.Is(ClearRank.S, true, 3, 3, 2, 2));
            Assert.IsFalse(StoryPerfect.Is(ClearRank.A, true, 3, 3, 2, 2));
            Assert.IsFalse(StoryPerfect.Is(ClearRank.S, false, 3, 3, 2, 2));
            Assert.IsFalse(StoryPerfect.Is(ClearRank.S, true, 2, 3, 2, 2));
            var s = SaveData.CreateNew();
            s.RecordLevelResult("a", 30f, 3, 2);
            Assert.AreEqual(StoryPerfect.Shards, StoryPerfect.Record(s.GetLevel("a"), true));
            Assert.AreEqual(0, StoryPerfect.Record(s.GetLevel("a"), true), "once");
            Assert.AreEqual(1, StoryPerfect.Count(s));
        }

        [Test]
        public void MedalUp_NextGap_RankUp()
        {
            Assert.AreEqual("MEDAL UP: GOLD!", StoryPerfect.MedalUp(70f, 55f, 60f));
            Assert.AreEqual("", StoryPerfect.MedalUp(0f, 55f, 60f), "first clear isn't an upgrade");
            Assert.AreEqual("", StoryPerfect.MedalUp(58f, 55f, 60f), "gold already");
            Assert.AreEqual("GOLD: 5.0s faster", StoryPerfect.NextMedalGap(65f, 60f));
            Assert.AreEqual("SILVER: 5.0s faster", StoryPerfect.NextMedalGap(80f, 60f));
            Assert.AreEqual("", StoryPerfect.NextMedalGap(59f, 60f));
            Assert.AreEqual("UP FROM A!", StoryPerfect.RankUp(ClearRank.A, ClearRank.S));
            Assert.AreEqual("", StoryPerfect.RankUp(null, ClearRank.S));
            Assert.AreEqual("", StoryPerfect.RankUp(ClearRank.S, ClearRank.B));
        }

        [Test]
        public void WorldFull_ClaimsOnce()
        {
            var s = SaveData.CreateNew();
            Assert.IsTrue(WorldFull.Claim(s, "Sunspire"));
            Assert.IsFalse(WorldFull.Claim(s, "Sunspire"));
            Assert.IsFalse(WorldFull.Claim(s, ""));
            Badges.Update(s);
            Assert.IsTrue(Badges.Has(s, "world100"));
        }

        [Test]
        public void BadgeRewards_PayEveryFive()
        {
            var s = SaveData.CreateNew();
            for (int i = 0; i < 4; i++) s.lifetime.badges.Add("x" + i);
            Assert.AreEqual(0, BadgeRewards.Pay(s));
            Assert.AreEqual(1, BadgeRewards.ToNext(s));
            s.lifetime.badges.Add("x4");
            Assert.AreEqual(BadgeRewards.Shards, BadgeRewards.Pay(s));
            Assert.AreEqual(0, BadgeRewards.Pay(s), "paid once");
            for (int i = 5; i < 15; i++) s.lifetime.badges.Add("x" + i);
            Assert.AreEqual(2 * BadgeRewards.Shards, BadgeRewards.Pay(s));
            Assert.AreEqual(BadgeRewards.Every, BadgeRewards.ToNext(s));
        }

        [Test]
        public void Accuracy_KillMilestones_Dates()
        {
            var s = SaveData.CreateNew();
            Assert.IsFalse(StandAccuracy.Record(s, 20, 20), "too few shots");
            Assert.IsTrue(StandAccuracy.Record(s, 100, 62));
            Assert.IsFalse(StandAccuracy.Record(s, 100, 50));
            Assert.AreEqual(62, s.lifetime.bestAccuracy);
            Badges.Update(s);
            Assert.IsTrue(Badges.Has(s, "sharpshooter"));
            Assert.AreEqual(2000, KillMilestone.Crossed(1990, 2010));
            Assert.AreEqual(0, KillMilestone.Crossed(1010, 1990));
            var today = new DateTime(2026, 11, 21);
            Assert.AreEqual("today", PlayDates.Ago(20261121, today));
            Assert.AreEqual("yesterday", PlayDates.Ago(20261120, today));
            Assert.AreEqual("5 days ago", PlayDates.Ago(20261116, today));
            Assert.AreEqual("", PlayDates.Ago(0, today));
            Assert.AreEqual("Playing since Nov 2026", PlayDates.Since(20261103));
        }

        [Test]
        public void WeeklySnapshot_IncludesBadgesAndVersus()
        {
            var s = SaveData.CreateNew();
            HeroRecords.Record(s, "luma", true);
            s.lifetime.badges.Add("first_clear");
            var snap = WeeklyRecap.Snapshot(s);
            Assert.AreEqual(WeeklyRecap.Labels.Length, snap.Length);
            Assert.AreEqual(1, snap[5]);
            Assert.AreEqual(1, snap[6]);
            Assert.GreaterOrEqual(TipOfTheDay.Tips.Length, 20);
            s.lifetime.bestAccuracy = 140; s.lifetime.closeCalls = -1;
            s.SanitizeAndMigrate();
            Assert.AreEqual(100, s.lifetime.bestAccuracy);
            Assert.AreEqual(0, s.lifetime.closeCalls);
        }
    }
}
