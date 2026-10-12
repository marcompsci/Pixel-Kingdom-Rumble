using System;
using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    [TestFixture]
    public class RumbleRatingTests
    {
        static RumbleMatchReport Report(int fighters, int placement, int ko = 0, int falls = 0, float difficulty = 0.5f) =>
            new RumbleMatchReport { fighters = fighters, placement = placement, knockouts = ko, falls = falls, difficulty = difficulty };

        [Test]
        public void BetterPlacement_AlwaysEarnsMore_WhateverTheStats()
        {
            for (int n = 2; n <= 4; n++)
            {
                Assert.Greater(RumblePoints.MinPlacementGap(n), RumblePoints.PerformanceCap, "gap > cap for " + n);
                for (int place = 2; place <= n; place++)
                {
                    // Best possible worse placement vs worst possible better placement.
                    var worse = new RumbleMatchReport { fighters = n, placement = place, knockouts = 9, firstBlood = true, revengeKOs = 3, difficulty = 1f };
                    var better = new RumbleMatchReport { fighters = n, placement = place - 1, falls = 9, selfDestructs = 9, difficulty = 1f };
                    Assert.Greater(RumblePoints.Score(better).total, RumblePoints.Score(worse).total, $"{n}p: {place - 1} vs {place}");
                }
            }
        }

        [Test]
        public void Breakdown_IsItemised_AndAddsUp()
        {
            var b = RumblePoints.Score(new RumbleMatchReport
            {
                fighters = 4, placement = 1, knockouts = 2, falls = 1, firstBlood = true, difficulty = 0.5f
            });
            // 130 + (16 - 4 + 5) = 147 at x1.025 -> 151
            Assert.AreEqual(130, b.placementPoints);
            Assert.AreEqual(17, b.performance);
            int sum = 0;
            foreach (var l in b.lines) sum += l.points;
            Assert.AreEqual(b.total, sum, "lines add up to the total");
            Assert.AreEqual(151, b.total);
        }

        [Test]
        public void Style_IsCapped_AndNeverNegative()
        {
            var hot = RumblePoints.Score(Report(4, 1, ko: 10));
            Assert.AreEqual(RumblePoints.PerformanceCap, hot.performance);
            var cold = RumblePoints.Score(new RumbleMatchReport { fighters = 4, placement = 4, falls = 5, selfDestructs = 2, difficulty = 0.5f });
            Assert.AreEqual(0, cold.performance);
            Assert.AreEqual(0, cold.total);
        }

        [Test]
        public void SelfDestruct_CostsMoreThanAnOrdinaryFall()
        {
            var normal = RumblePoints.Score(Report(2, 1, ko: 3, falls: 1)).performanceRaw;
            var sd = RumblePoints.Score(new RumbleMatchReport { fighters = 2, placement = 1, knockouts = 3, falls = 1, selfDestructs = 1, difficulty = 0.5f }).performanceRaw;
            Assert.AreEqual(normal + RumblePoints.SelfDestructExtra, sd);
        }

        [Test]
        public void HarderCpu_PaysMore()
        {
            Assert.Less(RumblePoints.Score(Report(4, 1, difficulty: 0f)).total, RumblePoints.Score(Report(4, 1, difficulty: 0.5f)).total);
            Assert.Less(RumblePoints.Score(Report(4, 1, difficulty: 0.5f)).total, RumblePoints.Score(Report(4, 1, difficulty: 1f)).total);
        }

        [Test]
        public void DailyBonus_GrowsWithStreak_UpToCap()
        {
            Assert.AreEqual(20, RumblePoints.DailyBonusFor(1));
            Assert.AreEqual(25, RumblePoints.DailyBonusFor(2));
            Assert.AreEqual(40, RumblePoints.DailyBonusFor(5));
            Assert.AreEqual(40, RumblePoints.DailyBonusFor(30));
        }

        [Test]
        public void Tiers_FollowThresholds()
        {
            Assert.AreEqual(RumbleTier.Bronze, RumbleTiers.TierFor(0));
            Assert.AreEqual(RumbleTier.Silver, RumbleTiers.TierFor(250));
            Assert.AreEqual(RumbleTier.Gold, RumbleTiers.TierFor(699));
            Assert.AreEqual(RumbleTier.Legend, RumbleTiers.TierFor(99999));
            Assert.AreEqual(1f, RumbleTiers.Progress(5000), 0.0001);
            Assert.AreEqual(0.5f, RumbleTiers.Progress(125), 0.0001);
        }

        [Test]
        public void Bronze_NeverLosesPoints()
        {
            var p = new RumbleProfile { rating = 100 };
            var c = RumbleRating.Apply(p, RumblePoints.Score(Report(4, 4)), 4, 4);
            Assert.AreEqual(0, c.delta);
            Assert.IsTrue(c.shielded);
            Assert.AreEqual(100, p.rating);
        }

        [Test]
        public void Promotion_GivesAShield_ThenLossesCount()
        {
            var p = new RumbleProfile { rating = 240 };
            var win = RumbleRating.Apply(p, RumblePoints.Score(Report(4, 1, ko: 3)), 1, 4);
            Assert.IsTrue(win.promoted);
            Assert.AreEqual(RumbleTier.Silver, p.Tier);
            Assert.AreEqual(RumbleTiers.PromotionShieldMatches, p.shieldMatches);
            int before = p.rating;
            for (int i = 0; i < RumbleTiers.PromotionShieldMatches; i++)
            {
                var c = RumbleRating.Apply(p, RumblePoints.Score(Report(4, 4)), 4, 4);
                Assert.IsTrue(c.shielded, "shielded match " + i);
            }
            Assert.AreEqual(before, p.rating);
            var hurt = RumbleRating.Apply(p, RumblePoints.Score(Report(4, 4)), 4, 4);
            Assert.IsFalse(hurt.shielded);
            Assert.Less(hurt.delta, 0);
        }

        [Test]
        public void LosingStreak_HalvesLosses()
        {
            var p = new RumbleProfile { rating = 1000, lossStreak = 0 };
            var first = RumbleRating.Apply(p, RumblePoints.Score(Report(4, 4, falls: 2)), 4, 4);   // -70 at Gold
            Assert.AreEqual(-70, first.delta);
            RumbleRating.Apply(p, RumblePoints.Score(Report(4, 4, falls: 2)), 4, 4);             // streak 1 -> full loss
            var third = RumbleRating.Apply(p, RumblePoints.Score(Report(4, 4, falls: 2)), 4, 4);  // streak 2 -> halved
            Assert.IsTrue(third.streakProtected);
            Assert.AreEqual(-35, third.delta);
            RumbleRating.Apply(p, RumblePoints.Score(Report(4, 1)), 1, 4);
            Assert.AreEqual(0, p.lossStreak, "a win resets the streak");
        }

        [Test]
        public void Rating_NeverBelowZero_AndTracksBest()
        {
            var p = new RumbleProfile { rating = 300, shieldMatches = 0, lossStreak = 0 };
            RumbleRating.Apply(p, RumblePoints.Score(Report(4, 1, ko: 5, difficulty: 1f)), 1, 4);
            int best = p.rating;
            Assert.AreEqual(best, p.bestRating);
            p.shieldMatches = 0;
            for (int i = 0; i < 20; i++) RumbleRating.Apply(p, RumblePoints.Score(Report(4, 4)), 4, 4);
            Assert.IsTrue(p.rating >= 0);
            Assert.AreEqual(best, p.bestRating);
            Assert.AreEqual(RumbleRating.HistoryLength, p.recentDeltas.Count);
        }

        [Test]
        public void AutoDifficulty_RisesWhenWinning_FallsWhenLosing_StaysInRange()
        {
            float s = RumbleProfile.StartingSkill;
            for (int i = 0; i < 30; i++) s = AdaptiveDifficulty.Update(s, 1, 4);
            Assert.AreEqual(1f, s, 0.0001);
            for (int i = 0; i < 30; i++) s = AdaptiveDifficulty.Update(s, 4, 4);
            Assert.AreEqual(0f, s, 0.0001);
            Assert.AreEqual(0f, AdaptiveDifficulty.PlacementScore(2, 3), 0.0001, "middle of 3 is neutral");
            var mid = AdaptiveDifficulty.Blend(0.5f);
            var normal = BotDifficulty.For(BotLevel.Normal);
            Assert.AreEqual(normal.reactionTime, mid.reactionTime, 0.0001);
            Assert.Less(AdaptiveDifficulty.Blend(1f).reactionTime, AdaptiveDifficulty.Blend(0f).reactionTime);
        }

        [Test]
        public void Tracker_FirstBlood_Revenge_SelfDestruct_Comeback()
        {
            var t = new RumbleTracker(0);
            t.OnFall(0, 2, new[] { 2, 1, 1, 1 });  // CPU 2 KOs you; you're last
            Assert.IsFalse(t.FirstBlood);
            Assert.IsTrue(t.WasLast);
            t.OnFall(2, 0, new[] { 1, 1, 2, 1 });  // you take revenge on 2
            Assert.AreEqual(1, t.RevengeKOs);
            t.OnFall(0, -1, new[] { 2, 1, 2, 1 }); // self-destruct
            Assert.AreEqual(1, t.SelfDestructs);

            var t2 = new RumbleTracker(0);
            t2.OnFall(1, 0, new[] { 1, 2 });
            Assert.IsTrue(t2.FirstBlood);
            Assert.IsFalse(t2.WasLast);
        }

        [Test]
        public void DailyRumble_IsDeterministic_AndStreaksCountConsecutiveDays()
        {
            var a = DailyRumble.For(20261012);
            var b = DailyRumble.For(20261012);
            Assert.AreEqual(a.title, b.title);
            Assert.AreEqual((int)a.mutators, (int)b.mutators);
            Assert.IsTrue(a.mutators != Mutator.None);
            if ((a.mutators & Mutator.SuddenDeath) != 0) Assert.AreEqual(1, a.stocks);

            Assert.AreEqual(20261231, DailyRumble.PreviousDay(20270101));
            Assert.AreEqual(20240229, DailyRumble.PreviousDay(20240301));

            var p = new RumbleProfile();
            Assert.AreEqual(1, DailyRumble.RecordPlayed(p, 20261010));
            Assert.AreEqual(2, DailyRumble.RecordPlayed(p, 20261011));
            Assert.IsTrue(DailyRumble.PlayedToday(p, 20261011));
            Assert.AreEqual(1, DailyRumble.RecordPlayed(p, 20261014), "missed days reset the streak");
            Assert.AreEqual(2, p.bestDailyStreak);
        }

        [Test]
        public void DailyRumble_UsesEveryTwistOverAMonth()
        {
            var seen = new System.Collections.Generic.HashSet<string>();
            var day = new DateTime(2026, 10, 1);
            for (int i = 0; i < 60; i++) seen.Add(DailyRumble.For(DailyRumble.DayKey(day.AddDays(i))).title);
            Assert.IsTrue(seen.Count >= 5, "variety: " + seen.Count);
        }

        [Test]
        public void Config_CouchPlay_AllowsNoBots_AndIsUnrated()
        {
            var cfg = new ArenaMatchConfig { humans = 2, botCount = 0 };
            cfg.Clamp();
            Assert.AreEqual(0, cfg.botCount);
            Assert.AreEqual(2, cfg.FighterCount);
            Assert.IsFalse(cfg.Rated);
            cfg.botCount = 9; cfg.Clamp();
            Assert.AreEqual(2, cfg.botCount);
            var solo = new ArenaMatchConfig { humans = 1, botCount = 0 };
            solo.Clamp();
            Assert.AreEqual(1, solo.botCount);
            Assert.IsTrue(solo.Rated);
            var sd = new ArenaMatchConfig { stocks = 4, mutators = Mutator.SuddenDeath };
            sd.Clamp();
            Assert.AreEqual(1, sd.stocks);
        }

        [Test]
        public void Config_LevelCycle_GoesThroughAuto()
        {
            var cfg = new ArenaMatchConfig();
            Assert.IsTrue(cfg.autoLevel);
            cfg.NextLevel(); Assert.IsFalse(cfg.autoLevel); Assert.AreEqual(BotLevel.Easy, cfg.botLevel);
            cfg.NextLevel(); Assert.AreEqual(BotLevel.Normal, cfg.botLevel);
            cfg.NextLevel(); Assert.AreEqual(BotLevel.Hard, cfg.botLevel);
            cfg.NextLevel(); Assert.IsTrue(cfg.autoLevel);
        }

        [Test]
        public void Save_SanitizesRumbleProfile()
        {
            var s = new SaveData { rumble = null };
            s.SanitizeAndMigrate();
            Assert.IsNotNull(s.rumble);
            s.rumble.rating = -5; s.rumble.skill = float.NaN; s.rumble.recentDeltas = null;
            s.SanitizeAndMigrate();
            Assert.AreEqual(0, s.rumble.rating);
            Assert.AreEqual(RumbleProfile.StartingSkill, s.rumble.skill, 0.0001);
            Assert.IsNotNull(s.rumble.recentDeltas);
        }
    }
}
