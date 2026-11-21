using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    [TestFixture]
    public class PlayerMeta6Tests
    {
        [Test]
        public void ArenaCareer_RecordsBestsAndMilestones()
        {
            var s = SaveData.CreateNew();
            var r = ArenaCareer.Record(s, true, 3, 4);
            Assert.IsFalse(r.newBestKOs, "the first match sets the bar quietly");
            r = ArenaCareer.Record(s, false, 5, 2);
            Assert.IsTrue(r.newBestKOs);
            Assert.IsFalse(r.newBestCombo);
            Assert.AreEqual(2, s.lifetime.arenaMatches);
            Assert.AreEqual(1, s.lifetime.arenaWins);
            Assert.AreEqual(8, s.lifetime.arenaKOs);
            s.lifetime.arenaKOs = 98;
            Assert.AreEqual(100, ArenaCareer.Record(s, false, 3, 0).koMilestone);
            StringAssert.Contains("Arena Clash: 1 wins in 3 matches", ArenaCareer.Line(s));
            Assert.AreEqual("", ArenaCareer.Line(SaveData.CreateNew()));
        }

        [Test]
        public void Mvp_MostKOsNoTies()
        {
            Assert.AreEqual(2, ArenaCareer.Mvp(new[] { 1, 0, 4, 2 }));
            Assert.AreEqual(-1, ArenaCareer.Mvp(new[] { 3, 3, 0 }));
            Assert.AreEqual(-1, ArenaCareer.Mvp(new[] { 0, 0 }));
            Assert.AreEqual(-1, ArenaCareer.Mvp(null));
        }

        [Test]
        public void Party_ModesFavoriteAndMilestones()
        {
            var s = SaveData.CreateNew();
            Assert.AreEqual(0, PartyMeta.ModesPlayed(s));
            PartyResults.Record(s, PartyMode.CoinRush, 1);
            PartyResults.Record(s, PartyMode.CoinRush, 2);
            PartyResults.Record(s, PartyMode.RingOut, 3);
            Assert.AreEqual(2, PartyMeta.ModesPlayed(s));
            Assert.AreEqual((int)PartyMode.CoinRush, PartyMeta.Favorite(s).mode);
            Assert.IsTrue(PartyMeta.WinMilestone(1));
            Assert.IsFalse(PartyMeta.WinMilestone(2));
            Assert.IsTrue(PartyMeta.WinMilestone(100));
        }

        [Test]
        public void NewBadges_RecentOrder_DailyCareer()
        {
            var s = SaveData.CreateNew();
            s.lifetime.arenaKOs = 250; s.lifetime.arenaBestCombo = 8; s.lifetime.arenaMatches = 50; s.lifetime.arenaWins = 50;
            DailyCareer.Add(s, 30);
            s.zombieCareer.weeklyDone.AddRange(new[] { "w1", "w2", "w3" });
            foreach (var m in PartyModes.All) PartyResults.Record(s, m.mode, 4);
            Badges.Update(s);
            foreach (var id in new[] { "arena_ko250", "combo8", "party_all", "weekly3", "daily30", "arena_win50" }) Assert.IsTrue(Badges.Has(s, id), id);
            var recent = RecentBadges.Names(s, 2);
            Assert.AreEqual(2, recent.Count);
            Assert.GreaterOrEqual(Badges.All.Length, 32);
            Assert.GreaterOrEqual(TipOfTheDay.Tips.Length, 24);
            s.lifetime.arenaWins = 99; s.lifetime.arenaMatches = 10; s.lifetime.dailyDone = -3;
            s.SanitizeAndMigrate();
            Assert.AreEqual(10, s.lifetime.arenaWins, "wins never exceed matches");
            Assert.AreEqual(0, s.lifetime.dailyDone);
        }

        [Test]
        public void NextGoal_MentionsPayoutWhenOneAway()
        {
            var s = SaveData.CreateNew();
            for (int i = 0; i < 4; i++) s.lifetime.badges.Add("pkr_x" + i);
            StringAssert.Contains("+25 shards", Badges.NextGoal(s));
        }
    }
}
