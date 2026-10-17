using System;
using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    [TestFixture]
    public class ZombieWeeklyTests
    {
        [Test]
        public void Maps_HaveUniqueIdsFilesAndBoards()
        {
            Assert.AreEqual(3, LastStandMap.All.Length);
            for (int i = 0; i < LastStandMap.All.Length; i++)
                for (int j = i + 1; j < LastStandMap.All.Length; j++)
                {
                    Assert.AreNotEqual(LastStandMap.All[i].Id, LastStandMap.All[j].Id);
                    Assert.AreNotEqual(LastStandMap.All[i].File, LastStandMap.All[j].File);
                    Assert.AreNotEqual(LastStandMap.All[i].Leaderboard, LastStandMap.All[j].Leaderboard);
                    Assert.AreNotEqual(LastStandMap.All[i].HiddenSecret, LastStandMap.All[j].HiddenSecret);
                }
            Assert.IsTrue(ReferenceEquals(LastStandMap.HollowBlock, LastStandMap.Get("nope")));
            Assert.IsTrue(ReferenceEquals(LastStandMap.SaltPier, LastStandMap.Get("salt_pier")));
            Assert.AreEqual("block", LastStandMap.Get(LastStandRules.MapId).File);
            Assert.AreEqual(1, LastStandMap.IndexOf("salt_pier"));
            Assert.IsTrue(ReferenceEquals(LastStandMap.HollowBlock, LastStandMap.At(9)));
        }

        [Test]
        public void Week_StartsOnMondayUtc()
        {
            Assert.AreEqual(0, ZombieWeekly.WeekOf(new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc)));
            Assert.AreEqual(0, ZombieWeekly.WeekOf(new DateTime(2026, 1, 11, 23, 59, 0, DateTimeKind.Utc)));
            Assert.AreEqual(1, ZombieWeekly.WeekOf(new DateTime(2026, 1, 12, 0, 0, 0, DateTimeKind.Utc)));
            int w = ZombieWeekly.WeekOf(new DateTime(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc));
            Assert.AreEqual(DayOfWeek.Monday, ZombieWeekly.StartOf(w).DayOfWeek);
            Assert.IsTrue(ZombieWeekly.StartOf(w) <= new DateTime(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc));
            Assert.AreEqual("2d 12h left", ZombieWeekly.LeftText(new DateTime(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc)));
        }

        [Test]
        public void Challenge_IsDeterministic_TwoDifferentMutators_ValidMap()
        {
            for (int w = 0; w < 200; w++)
            {
                var a = ZombieWeekly.For(w);
                var b = ZombieWeekly.For(w);
                Assert.AreEqual(a.MapId, b.MapId);
                Assert.AreEqual(a.Mutators[0], b.Mutators[0]);
                Assert.AreEqual(a.Mutators[1], b.Mutators[1]);
                Assert.AreNotEqual(a.Mutators[0], a.Mutators[1]);
                Assert.AreEqual(a.MapId, LastStandMap.Get(a.MapId).Id);
                Assert.AreNotEqual(SurvivalMode.Raid, a.Mode);
                Assert.GreaterOrEqual(a.GoalRound, 10);
                Assert.AreEqual("W" + w, a.Key);
            }
        }

        [Test]
        public void Challenge_VariesAcrossWeeks()
        {
            var seenMaps = new System.Collections.Generic.HashSet<string>();
            var seenMut = new System.Collections.Generic.HashSet<ZombieMutator>();
            for (int w = 0; w < 52; w++)
            {
                var c = ZombieWeekly.For(w);
                seenMaps.Add(c.MapId);
                seenMut.Add(c.Mutators[0]); seenMut.Add(c.Mutators[1]);
            }
            Assert.AreEqual(LastStandMap.All.Length, seenMaps.Count, "every map comes up in a year");
            Assert.AreEqual(Enum.GetValues(typeof(ZombieMutator)).Length, seenMut.Count, "every mutator comes up in a year");
        }

        [Test]
        public void Mutators_ChangeTheNumbers()
        {
            Assert.AreEqual(1f, MutatorSet.None.ZombieSpeed);
            Assert.AreEqual(950, MutatorSet.None.BoxCost(950));
            var s = new MutatorSet(new[] { ZombieMutator.Sprinters, ZombieMutator.BoxFever });
            Assert.AreEqual(1.25f, s.ZombieSpeed, 1e-4);
            Assert.AreEqual(480, s.BoxCost(950));
            Assert.IsTrue(s.NoWallGuns);
            var h = new MutatorSet(new[] { ZombieMutator.Headhunter, ZombieMutator.ThickHide });
            Assert.AreEqual(180, h.Kill(60));
            Assert.AreEqual(0, h.Hit(10));
            Assert.AreEqual(150, h.Health(100));
            var g = new MutatorSet(new[] { ZombieMutator.GlassCannon });
            Assert.AreEqual(50, g.HeroMaxHealth(100));
            Assert.AreEqual(50, g.Health(100));
            var d = new MutatorSet(new[] { ZombieMutator.OpenDoors, ZombieMutator.IronRations });
            Assert.AreEqual(380, d.DoorCost(750));
            Assert.IsTrue(d.NoDrops);
            Assert.AreEqual("IRON RATIONS + OPEN DOORS", d.Names());
        }

        [Test]
        public void HoundPack_EveryThirdRound_NeverOnABossRound()
        {
            var s = new MutatorSet(new[] { ZombieMutator.HoundPack });
            Assert.IsTrue(s.IsHoundRound(SurvivalMode.Endless, 3));
            Assert.IsTrue(s.IsHoundRound(SurvivalMode.Endless, 6));
            Assert.IsFalse(s.IsHoundRound(SurvivalMode.Endless, 5));
            Assert.IsFalse(s.IsHoundRound(SurvivalMode.Endless, 30), "boss round");
            Assert.IsTrue(MutatorSet.None.IsHoundRound(SurvivalMode.Endless, 5));
        }

        [Test]
        public void Record_PaysOncePerWeek_AndTracksBest()
        {
            var c = new ZombieCareer();
            var w = ZombieWeekly.For(40);
            Assert.IsFalse(ZombieWeekly.Record(c, w, w.GoalRound - 1));
            Assert.AreEqual(w.GoalRound - 1, ZombieWeekly.BestThisWeek(c, w));
            Assert.IsTrue(ZombieWeekly.Record(c, w, w.GoalRound + 2));
            Assert.IsTrue(ZombieWeekly.Done(c, w));
            Assert.IsFalse(ZombieWeekly.Record(c, w, w.GoalRound + 5), "paid once");
            Assert.AreEqual(w.GoalRound + 5, ZombieWeekly.BestThisWeek(c, w));
            Assert.AreEqual(1, c.weeklyWins);
            var next = ZombieWeekly.For(41);
            Assert.AreEqual(0, ZombieWeekly.BestThisWeek(c, next));
            Assert.IsFalse(ZombieWeekly.Done(c, next));
            ZombieWeekly.Record(c, next, 1);
            Assert.AreEqual(1, c.weeklyBest, "a new week starts a new best");
        }

        [Test]
        public void OldSave_WithoutWeeklyFields_Sanitizes()
        {
            var c = new ZombieCareer { weeklyDone = null, weeklyKey = null };
            c.Sanitize();
            Assert.IsNotNull(c.weeklyDone);
            Assert.AreEqual("", c.weeklyKey);
        }
    }
}
