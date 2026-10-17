using System.Collections.Generic;
using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    /// <summary>Social and retention: daily orders and streaks, the LOCKER's earned cosmetics, zombie achievements.</summary>
    [TestFixture]
    public class ZombieSocialTests
    {
        [Test]
        public void DailyOrders_AreTheSameForEveryone_ThreeDifferentKinds_HarderAsTheyGo()
        {
            for (int day = 0; day < 400; day++)
            {
                var a = ZombieDaily.For(day);
                var b = ZombieDaily.For(day);
                Assert.AreEqual(ZombieDaily.Orders, a.Length);
                var kinds = new HashSet<DailyKind>();
                for (int i = 0; i < a.Length; i++)
                {
                    Assert.AreEqual(a[i].Kind, b[i].Kind, "deterministic");
                    Assert.AreEqual(a[i].Target, b[i].Target);
                    Assert.AreEqual(a[i].MapId, b[i].MapId);
                    Assert.Greater(a[i].Target, 0);
                    Assert.IsTrue(kinds.Add(a[i].Kind), "no repeated kind in a day");
                    Assert.IsFalse(string.IsNullOrEmpty(a[i].Text));
                    if (a[i].MapId != "") Assert.AreEqual(a[i].MapId, LastStandMap.Get(a[i].MapId).Id, "a real map");
                }
                Assert.Less(a[0].RewardShards, a[2].RewardShards, "the hard one pays more");
            }
            Assert.AreEqual(0, ZombieDaily.DayOf(new System.DateTime(2026, 1, 1, 23, 59, 0, System.DateTimeKind.Utc)));
            Assert.AreEqual(1, ZombieDaily.DayOf(new System.DateTime(2026, 1, 2, 0, 0, 0, System.DateTimeKind.Utc)));
        }

        static int FindDay(DailyKind kind, out int index)
        {
            for (int d = 0; d < 1000; d++)
            {
                var o = ZombieDaily.For(d);
                for (int i = 0; i < o.Length; i++) if (o[i].Kind == kind) { index = i; return d; }
            }
            index = -1;
            return -1;
        }

        [Test]
        public void Report_AddsUp_PaysOnce_RoundsTakeTheBest_MapOrdersNeedTheirMap()
        {
            int day = FindDay(DailyKind.BoxSpins, out int i);
            Assert.GreaterOrEqual(day, 0);
            var c = new ZombieCareer();
            var o = ZombieDaily.For(day)[i];
            for (int k = 0; k < o.Target - 1; k++) Assert.AreEqual(0, ZombieDaily.Report(c, day, DailyKind.BoxSpins, 1, "", out _).Count);
            var done = ZombieDaily.Report(c, day, DailyKind.BoxSpins, 1, "", out _);
            Assert.AreEqual(1, done.Count);
            Assert.AreEqual(i, done[0]);
            Assert.AreEqual(0, ZombieDaily.Report(c, day, DailyKind.BoxSpins, 5, "", out _).Count, "paid once");
            Assert.AreEqual(o.Target, ZombieDaily.ProgressOf(c, day, i));
            Assert.AreEqual(1, ZombieDaily.DoneCount(c, day));

            int rday = FindDay(DailyKind.ReachRound, out int ri);
            var ro = ZombieDaily.For(rday)[ri];
            var c2 = new ZombieCareer();
            string wrong = ro.MapId == LastStandMap.HollowBlock.Id ? LastStandMap.SaltPier.Id : LastStandMap.HollowBlock.Id;
            ZombieDaily.Report(c2, rday, DailyKind.ReachRound, 99, wrong, out _);
            Assert.AreEqual(0, ZombieDaily.ProgressOf(c2, rday, ri), "a map order only counts on its map");
            ZombieDaily.Report(c2, rday, DailyKind.ReachRound, 3, ro.MapId, out _);
            ZombieDaily.Report(c2, rday, DailyKind.ReachRound, 2, ro.MapId, out _);
            Assert.AreEqual(3, ZombieDaily.ProgressOf(c2, rday, ri), "rounds keep the best, they don't add");

            // a new day starts fresh
            ZombieDaily.Roll(c, day + 1);
            Assert.AreEqual(0, ZombieDaily.DoneCount(c, day + 1));
        }

        static void FinishDay(ZombieCareer c, int day, out bool all)
        {
            all = false;
            foreach (var o in ZombieDaily.For(day))
            {
                ZombieDaily.Report(c, day, o.Kind, o.Target, o.MapId, out bool a);
                all |= a;
            }
        }

        [Test]
        public void Streak_GrowsOnBackToBackDays_RestartsAfterAMiss_BonusCapsAtAWeek()
        {
            var c = new ZombieCareer();
            FinishDay(c, 100, out bool all);
            Assert.IsTrue(all);
            Assert.AreEqual(1, c.streak);
            FinishDay(c, 101, out _);
            FinishDay(c, 102, out _);
            Assert.AreEqual(3, c.streak);
            Assert.AreEqual(3, ZombieDaily.StreakNow(c, 103), "still alive the next day");
            Assert.AreEqual(0, ZombieDaily.StreakNow(c, 104), "a missed day");
            FinishDay(c, 104, out _);
            Assert.AreEqual(1, c.streak, "starts again");
            Assert.AreEqual(3, c.bestStreak);
            Assert.AreEqual(4, c.dailyDays);
            Assert.AreEqual(ZombieDaily.StreakBonus(7), ZombieDaily.StreakBonus(30), "capped");
            Assert.Greater(ZombieDaily.StreakBonus(3), ZombieDaily.StreakBonus(1));
        }

        [Test]
        public void Locker_ItemsUnlockByPlaying_LockedOnesCantBeWorn_TogglingTakesOff()
        {
            var c = new ZombieCareer();
            var zl = new ZombieLandProgress();
            var none = CosmeticStats.From(c, zl);
            foreach (var item in ZombieCosmetics.All) Assert.IsFalse(item.Unlocked(none), item.Id + " starts locked");
            Assert.IsFalse(ZombieCosmetics.Toggle(c, "tr_toxic", none));
            Assert.AreEqual("", c.tracer);

            c.totalKills = 600;
            c.secrets.Add(LastStandMap.RailYard.HiddenSecret);
            var some = CosmeticStats.From(c, zl);
            var news = ZombieCosmetics.NewlyUnlocked(none, some);
            Assert.IsTrue(news.Exists(x => x.Id == "tr_toxic"));
            Assert.IsTrue(news.Exists(x => x.Id == "ti_widowmaker"));
            Assert.IsFalse(news.Exists(x => x.Id == "tr_gold"), "gold needs every hidden boss");
            Assert.IsTrue(ZombieCosmetics.Toggle(c, "tr_toxic", some));
            Assert.AreEqual("tr_toxic", c.tracer);
            Assert.AreEqual("tr_toxic", ZombieCosmetics.Worn(c, CosmeticSlot.Tracer, some).Id);
            Assert.IsTrue(ZombieCosmetics.Toggle(c, "tr_toxic", some));
            Assert.AreEqual("", c.tracer, "tapping again takes it off");
            Assert.IsTrue(ZombieCosmetics.Toggle(c, "ti_widowmaker", some));
            Assert.AreEqual("ti_widowmaker", c.title);
            Assert.IsNull(ZombieCosmetics.Worn(c, CosmeticSlot.Title, none), "a reset save can't keep wearing it");

            foreach (var m in LastStandMap.All) if (!c.Has(m.HiddenSecret)) c.secrets.Add(m.HiddenSecret);
            Assert.IsTrue(ZombieCosmetics.Get("tr_gold").Unlocked(CosmeticStats.From(c, zl)));
            var ids = new HashSet<string>();
            foreach (var item in ZombieCosmetics.All) Assert.IsTrue(ids.Add(item.Id), "unique ids");
        }

        [Test]
        public void ZombieAchievements_FollowTheSave_IdsAreUnique()
        {
            var c = new ZombieCareer();
            var zl = new ZombieLandProgress();
            Assert.AreEqual(0, Achievements.ForZombies(c, zl).Count);
            c.RecordRound("manor", SurvivalMode.Endless, 30);
            Assert.AreEqual(0, Achievements.ForZombies(c, zl).Count, "the Manor isn't Last Stand");
            c.RecordRound(LastStandMap.SaltPier.Id, SurvivalMode.Endless, 12);
            var got = Achievements.ForZombies(c, zl);
            Assert.IsTrue(got.Contains(Achievements.ZRound10));
            Assert.IsFalse(got.Contains(Achievements.ZRound20));
            zl.bossDefeated = zl.chapter2Done = zl.chapter3Done = true;
            c.bestStreak = 7; c.revives = 25; c.weeklyWins = 1;
            foreach (var m in LastStandMap.All) c.secrets.Add(m.HiddenSecret);
            c.RecordRound(LastStandMap.RailYard.Id, SurvivalMode.Hardcore, 21);
            got = Achievements.ForZombies(c, zl);
            foreach (var id in new[] { Achievements.ZRound20, Achievements.ZHollowKing, Achievements.ZTideMother, Achievements.ZIronWidow,
                                       Achievements.ZChapter1, Achievements.ZChapter2, Achievements.ZChapter3, Achievements.ZWeekly,
                                       Achievements.ZStreak, Achievements.ZMedic })
                Assert.IsTrue(got.Contains(id), id);
            var ids = new HashSet<string>();
            int points = 0;
            foreach (var a in Achievements.All)
            {
                Assert.IsTrue(ids.Add(a.id), "unique " + a.id);
                Assert.IsTrue(a.id.StartsWith("pkr.ach."));
                points += a.points;
            }
            foreach (var id in got) Assert.IsTrue(ids.Contains(id), id + " is listed in All (App Store Connect setup)");
            Assert.LessOrEqual(points, 1000, "Game Center allows 1000 points per app");
        }
    }
}
