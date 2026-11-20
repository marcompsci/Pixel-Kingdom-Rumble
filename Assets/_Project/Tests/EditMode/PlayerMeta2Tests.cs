using System;
using System.Collections.Generic;
using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    /// <summary>Round 4: splits, BRAVE, Last Stand history and accuracy, tips, badges, the weekly recap, the break
    /// reminder and the saved arena setup.</summary>
    [TestFixture]
    public class PlayerMeta2Tests
    {
        [Test]
        public void Splits_KeepTheFastestClear_AndCompareLive()
        {
            var s = SaveData.CreateNew();
            Assert.IsNull(Splits.Delta(s, "sq_a", 0, 10f));
            Assert.IsTrue(Splits.Offer(s, "sq_a", new List<float> { 10f, 20f }, 30f));
            Assert.AreEqual(-1f, Splits.Delta(s, "sq_a", 0, 9f).Value, 0.001f);
            Assert.AreEqual("-1.0s", Splits.Format(-1f));
            Assert.AreEqual("+0.5s", Splits.Format(0.5f));
            Assert.IsFalse(Splits.Offer(s, "sq_a", new List<float> { 5f }, 31f), "slower clears don't replace it");
            Assert.IsTrue(Splits.Offer(s, "sq_a", new List<float> { 9f, 18f }, 28f));
            Assert.IsNull(Splits.Delta(s, "sq_a", 5, 1f), "no split for that checkpoint");
        }

        [Test]
        public void Brave_PaysOnce_NotWhenAssisted()
        {
            var rec = new LevelRecord { levelId = "x" };
            Assert.AreEqual(0, StoryBrave.Record(rec, braveRun: true, assisted: true));
            Assert.AreEqual(0, StoryBrave.Record(rec, braveRun: false, assisted: false));
            Assert.AreEqual(StoryBrave.Shards, StoryBrave.Record(rec, true, false));
            Assert.AreEqual(0, StoryBrave.Record(rec, true, false));
        }

        [Test]
        public void StandHistory_KeepsTheLastFivePerMap_AndAccuracy()
        {
            var s = SaveData.CreateNew();
            for (int i = 1; i <= 7; i++) StandHistory.Add(s, new StandRunRecord { map = "pier", round = i });
            StandHistory.Add(s, new StandRunRecord { map = "rail", round = 9 });
            Assert.AreEqual(StandHistory.PerMap, StandHistory.For(s, "pier").Count);
            Assert.AreEqual(5f, StandHistory.AverageRound(s, "pier"), 0.001f, "rounds 3..7");
            Assert.AreEqual(1, StandHistory.For(s, "rail").Count);
            Assert.AreEqual(25, StandHistory.Accuracy(200, 50));
            Assert.AreEqual(0, StandHistory.Accuracy(0, 3));
            Assert.AreEqual(100, StandHistory.Accuracy(4, 9));
        }

        [Test]
        public void Tips_ChangeByDay()
        {
            Assert.IsFalse(string.IsNullOrEmpty(TipOfTheDay.For(20261120)));
            var seen = new HashSet<string>();
            for (int d = 1; d <= 28; d++) seen.Add(TipOfTheDay.For(20261100 + d));
            Assert.Greater(seen.Count, 5);
        }

        [Test]
        public void Badges_EarnOnce_AndScoreTheProfile()
        {
            var s = SaveData.CreateNew();
            Assert.AreEqual(0, Badges.Update(s).Count);
            s.levels.Add(new LevelRecord { levelId = "a", completed = true });
            var fresh = Badges.Update(s);
            Assert.AreEqual(1, fresh.Count);
            Assert.AreEqual("first_clear", fresh[0].id);
            Assert.AreEqual(0, Badges.Update(s).Count, "only once");
            Assert.IsTrue(Badges.Has(s, "first_clear"));
            HeroMastery.Add(s, "nova", 460);
            Assert.AreEqual(1 + 5, Badges.ProfileScore(s));
        }

        [Test]
        public void WeeklyRecap_OnlyAfterANewWeekWithActivity()
        {
            var s = SaveData.CreateNew();
            var mon = new DateTime(2026, 11, 16);
            Assert.AreEqual(WeeklyRecap.WeekKey(mon), WeeklyRecap.WeekKey(new DateTime(2026, 11, 22)));
            Assert.AreNotEqual(WeeklyRecap.WeekKey(mon), WeeklyRecap.WeekKey(new DateTime(2026, 11, 23)));
            Assert.IsNull(WeeklyRecap.Roll(s, WeeklyRecap.WeekKey(mon)), "first open just takes a snapshot");
            s.levels.Add(new LevelRecord { levelId = "a", completed = true });
            Assert.IsNull(WeeklyRecap.Roll(s, WeeklyRecap.WeekKey(mon)), "same week: nothing yet");
            var d = WeeklyRecap.Roll(s, WeeklyRecap.WeekKey(new DateTime(2026, 11, 24)));
            Assert.IsNotNull(d);
            Assert.AreEqual(1, d[0]);
            Assert.IsNull(WeeklyRecap.Roll(s, WeeklyRecap.WeekKey(new DateTime(2026, 12, 2))), "a quiet week shows nothing");
        }

        [Test]
        public void BreakReminder_HourlyWhenOn()
        {
            Assert.IsFalse(BreakReminder.Due(false, 99999f, 0));
            Assert.IsFalse(BreakReminder.Due(true, 3000f, 0));
            Assert.IsTrue(BreakReminder.Due(true, 3700f, 0));
            Assert.IsFalse(BreakReminder.Due(true, 3700f, 1));
        }

        [Test]
        public void ArenaSetup_IsKept_AndCleaned()
        {
            var s = SaveData.CreateNew();
            s.arenaSetup = new ArenaMatchConfig { items = true, botCount = 9, daily = true, party = PartyMode.CoinRush };
            s.SanitizeAndMigrate();
            Assert.IsTrue(s.arenaSetup.items);
            Assert.AreEqual(ArenaMatchConfig.MaxBots, s.arenaSetup.botCount);
            Assert.IsFalse(s.arenaSetup.daily);
            Assert.AreEqual(PartyMode.None, s.arenaSetup.party);
        }
    }
}
