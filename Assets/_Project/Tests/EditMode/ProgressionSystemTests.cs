using System.Collections.Generic;
using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    [TestFixture]
    public class PlayerLevelTests
    {
        [Test]
        public void Levels_GrowWithXp_AndPayShards()
        {
            Assert.AreEqual(1, PlayerLevel.LevelFor(0));
            Assert.AreEqual(1, PlayerLevel.LevelFor(119));
            Assert.AreEqual(2, PlayerLevel.LevelFor(120));
            Assert.AreEqual(3, PlayerLevel.LevelFor(120 + 170));
            Assert.AreEqual(0f, PlayerLevel.Progress01(0), 0.001f);
            Assert.AreEqual(0.5f, PlayerLevel.Progress01(60), 0.001f);
            var s = SaveData.CreateNew();
            var gained = PlayerLevel.AddXp(s, 300);
            Assert.AreEqual(2, gained.Count);
            Assert.AreEqual(2, gained[0]);
            Assert.AreEqual(3, gained[1]);
            Assert.AreEqual(PlayerLevel.ShardReward(2) + PlayerLevel.ShardReward(3), s.starShards);
            Assert.AreEqual(0, PlayerLevel.AddXp(s, 0).Count);
            Assert.IsTrue(PlayerLevel.IsMilestone(5));
            Assert.AreEqual(20 + 25 + PlayerLevel.MilestoneBonus, PlayerLevel.ShardReward(5));
            Assert.IsFalse(PlayerLevel.IsMilestone(6));
            Assert.LessOrEqual(PlayerLevel.LevelFor(int.MaxValue / 2), PlayerLevel.MaxLevel);
        }
    }

    [TestFixture]
    public class DailyChallengeTests
    {
        [Test]
        public void SameDay_SamePicks_ThreeDifferentKinds()
        {
            var a = DailyChallenges.PickFor(20261015);
            var b = DailyChallenges.PickFor(20261015);
            Assert.AreEqual(3, a.Count);
            var kinds = new HashSet<ChallengeKind>();
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(a[i].id, b[i].id);
                Assert.IsTrue(kinds.Add(a[i].kind), "different kinds");
            }
            bool anyDifferent = false;
            for (int d = 1; d < 10; d++)
                if (DailyChallenges.PickFor(20261015 + d)[0].id != a[0].id) anyDifferent = true;
            Assert.IsTrue(anyDifferent, "the picks change from day to day");
        }

        [Test]
        public void Report_CountsUp_PaysOnce_AndRollsOverNextDay()
        {
            var s = SaveData.CreateNew();
            Assert.IsTrue(DailyChallenges.EnsureToday(s, 20261015));
            Assert.IsFalse(DailyChallenges.EnsureToday(s, 20261015), "same day keeps progress");
            var first = DailyChallenges.Find(s.dailyChallenges[0].id).Value;
            int shards = s.starShards;
            int amount = first.best ? first.target : 1;
            int xp = 0, total = 0;
            for (int i = 0; i < first.target && !s.dailyChallenges[0].done; i++)
            {
                DailyChallenges.Report(s, first.kind, amount, out xp);
                total += xp;
            }
            Assert.IsTrue(s.dailyChallenges[0].done);
            Assert.AreEqual(first.xp, total, "XP paid once");
            Assert.GreaterOrEqual(s.starShards - shards, first.shards);
            int after = s.starShards;
            DailyChallenges.Report(s, first.kind, 100, out xp);
            Assert.AreEqual(after, s.starShards, "a finished challenge never pays twice");
            Assert.IsTrue(DailyChallenges.EnsureToday(s, 20261016));
            foreach (var c in s.dailyChallenges) Assert.IsFalse(c.done);
        }

        [Test]
        public void BestKind_KeepsTheBestNotTheSum()
        {
            var s = SaveData.CreateNew();
            s.dailyDayKey = 1;
            s.dailyChallenges = new List<ChallengeRecord> { new ChallengeRecord { id = "round_10" }, new ChallengeRecord { id = "clear_2" }, new ChallengeRecord { id = "win_2" } };
            DailyChallenges.Report(s, ChallengeKind.SurvivalRound, 6, out _);
            DailyChallenges.Report(s, ChallengeKind.SurvivalRound, 4, out _);
            Assert.AreEqual(6, s.dailyChallenges[0].progress);
            Assert.IsFalse(s.dailyChallenges[0].done);
            DailyChallenges.Report(s, ChallengeKind.SurvivalRound, 11, out int xp);
            Assert.IsTrue(s.dailyChallenges[0].done);
            Assert.AreEqual(160, xp);
        }

        [Test]
        public void Sanitize_DropsUnknownChallenges()
        {
            var s = SaveData.CreateNew();
            s.dailyChallenges.Add(new ChallengeRecord { id = "not_real" });
            s.dailyChallenges.Add(null);
            s.playerXp = -5;
            s.SanitizeAndMigrate();
            Assert.AreEqual(0, s.dailyChallenges.Count);
            Assert.AreEqual(0, s.playerXp);
        }
    }
}
