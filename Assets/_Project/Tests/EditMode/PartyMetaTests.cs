using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    /// <summary>Party quest meta-progression: difficulty, the daily party challenge, the Kingdom Cup, ranks and the ghost.</summary>
    [TestFixture]
    public class PartyMetaTests
    {
        static PartyOutcome Out(PartyMode m, int place, int score = 0, float secs = 0f, PartyDifficulty d = PartyDifficulty.Normal, int[] all = null) =>
            new PartyOutcome { mode = m, placement = place, score = score, raceSeconds = secs, difficulty = d, placements = all };

        [Test]
        public void Difficulty_ScalesRewardsBots_AndRaceSpeeds()
        {
            Assert.AreEqual(30, PartyDifficulties.ScaleReward(40, PartyDifficulty.Easy));
            Assert.AreEqual(40, PartyDifficulties.ScaleReward(40, PartyDifficulty.Normal));
            Assert.AreEqual(50, PartyDifficulties.ScaleReward(40, PartyDifficulty.Hard));
            Assert.AreEqual(BotLevel.Hard, PartyDifficulties.Bots(PartyDifficulty.Hard));
            foreach (var d in new[] { PartyDifficulty.Easy, PartyDifficulty.Normal, PartyDifficulty.Hard })
            {
                Assert.Less(PartyDifficulties.RaceSpeed(d, 20f), PartyDifficulties.RaceSpeed(d, 0f), "slower when far ahead");
                Assert.Greater(PartyDifficulties.RaceSpeed(d, -20f), PartyDifficulties.RaceSpeed(d, 0f), "faster when far behind");
            }
            Assert.Greater(PartyDifficulties.RaceSpeed(PartyDifficulty.Hard, 0f), PartyDifficulties.RaceSpeed(PartyDifficulty.Easy, 0f));
            Assert.AreEqual(PartyDifficulty.Easy, PartyDifficulties.Next(PartyDifficulty.Hard));
        }

        [Test]
        public void Apply_PaysScaledShards_AndRanksUp()
        {
            var save = SaveData.CreateNew();
            var r = PartyProgress.Apply(save, Out(PartyMode.CoinRush, 1, 9, d: PartyDifficulty.Hard), 20261113);
            Assert.AreEqual(50, r.shards, "1st on Hard = 40 x 1.25");
            Assert.GreaterOrEqual(save.starShards, 50);
            Assert.AreEqual(3, save.party.Find(PartyMode.CoinRush).rankPoints);
            Assert.AreEqual(PartyRank.Rookie, r.rank);
            var r2 = PartyProgress.Apply(save, Out(PartyMode.CoinRush, 1), 20261113);
            Assert.IsTrue(r2.rankUp, "6 points: Contender");
            Assert.AreEqual(PartyRank.Contender, r2.rank);
            var r3 = PartyProgress.Apply(save, Out(PartyMode.CoinRush, 2), 20261113);
            Assert.AreEqual(7, save.party.Find(PartyMode.CoinRush).rankPoints);
            Assert.IsFalse(r3.rankUp);
            Assert.IsFalse(PartyProgress.Apply(save, Out(PartyMode.None, 1), 20261113).rankUp, "no mode, no record");
        }

        [Test]
        public void Ranks_Thresholds()
        {
            Assert.AreEqual(PartyRank.Rookie, PartyRanks.For(0));
            Assert.AreEqual(PartyRank.Contender, PartyRanks.For(6));
            Assert.AreEqual(PartyRank.Legend, PartyRanks.For(500));
            Assert.AreEqual(6, PartyRanks.ToNext(0));
            Assert.AreEqual(0, PartyRanks.ToNext(90));
            Assert.AreEqual(0, PartyRanks.PointsFor(4));
            Assert.AreEqual("LEGEND", PartyRanks.Name(PartyRank.Legend));
        }

        [Test]
        public void Daily_IsStablePerDay_VariesAcrossDays_AndDescribes()
        {
            var a = DailyParty.For(20261113);
            var b = DailyParty.For(20261113);
            Assert.AreEqual(a.mode, b.mode);
            Assert.AreEqual(a.goal, b.goal);
            var seen = new System.Collections.Generic.HashSet<PartyMode>();
            for (int d = 1; d <= 28; d++) seen.Add(DailyParty.For(20261100 + d).mode);
            Assert.GreaterOrEqual(seen.Count, 4, "a month of days covers most quests");
            for (int d = 1; d <= 28; d++) Assert.IsTrue(DailyParty.For(20261200 + d).text.Length > 0);
            Assert.AreEqual(20261231, DailyParty.PreviousDay(20270101));
            Assert.AreEqual(20260228, DailyParty.PreviousDay(20260301));
        }

        [Test]
        public void Daily_Goals_Match()
        {
            var c = new DailyPartyChallenge { mode = PartyMode.CoinRush, goal = DailyGoal.CoinsAtLeast, target = 12 };
            Assert.IsTrue(c.Met(Out(PartyMode.CoinRush, 3, 12)));
            Assert.IsFalse(c.Met(Out(PartyMode.CoinRush, 1, 11)));
            Assert.IsFalse(c.Met(Out(PartyMode.KingOfTheHill, 1, 30)), "other quest");
            var race = new DailyPartyChallenge { mode = PartyMode.ItemRace, goal = DailyGoal.RaceUnder, target = 75 };
            Assert.IsTrue(race.Met(Out(PartyMode.ItemRace, 4, 0, 74.5f)));
            Assert.IsFalse(race.Met(Out(PartyMode.ItemRace, 1, 0, 0f)), "didn't finish");
        }

        [Test]
        public void Daily_PaysOnce_AndCountsAStreak()
        {
            var save = SaveData.CreateNew();
            int day = 20261113;
            var c = DailyParty.For(day);
            var win = Out(c.mode, 1, 99, c.goal == DailyGoal.RaceUnder ? 10f : 0f, all: new[] { 1, 2, 3, 4 });
            var r1 = PartyProgress.Apply(save, win, day);
            Assert.IsTrue(r1.dailyDone);
            Assert.AreEqual(DailyParty.Reward, r1.dailyBonus);
            Assert.AreEqual(1, r1.dailyStreak);
            Assert.IsFalse(PartyProgress.Apply(save, win, day).dailyDone, "once a day");
            int next = 20261114;
            var c2 = DailyParty.For(next);
            var r2 = PartyProgress.Apply(save, Out(c2.mode, 1, 99, c2.goal == DailyGoal.RaceUnder ? 10f : 0f), next);
            Assert.IsTrue(r2.dailyDone);
            Assert.AreEqual(2, r2.dailyStreak, "two days in a row");
            int gap = 20261120;
            var c3 = DailyParty.For(gap);
            var r3 = PartyProgress.Apply(save, Out(c3.mode, 1, 99, c3.goal == DailyGoal.RaceUnder ? 10f : 0f), gap);
            Assert.AreEqual(1, r3.dailyStreak, "a missed day restarts the streak");
            Assert.AreEqual(3, save.party.dailyCompleted);
        }

        [Test]
        public void Cup_FourEvents_PointsTrophyAndBonus()
        {
            var save = SaveData.CreateNew();
            KingdomCup.Start(save.party, PartyDifficulty.Hard);
            Assert.AreEqual(PartyMode.ItemRace, save.party.cup.CurrentEvent);
            Assert.AreEqual(PartyMode.BalloonBattle, save.party.cup.NextEvent);
            // Off-cup quests don't move the cup.
            Assert.IsFalse(PartyProgress.Apply(save, Out(PartyMode.RingOut, 1), 1).cupEvent);
            int[][] results = { new[] { 2, 1, 3, 4 }, new[] { 1, 2, 3, 4 }, new[] { 1, 3, 2, 4 }, new[] { 3, 1, 2, 4 } };
            PartyReport last = null;
            for (int i = 0; i < 4; i++)
            {
                var m = KingdomCup.Events[i];
                Assert.AreEqual(m, save.party.cup.CurrentEvent);
                last = PartyProgress.Apply(save, Out(m, results[i][0], d: PartyDifficulty.Hard, all: results[i]), 1);
                Assert.IsTrue(last.cupEvent);
                Assert.AreEqual(i == 3, last.cupFinished);
            }
            // You: 6+10+10+3 = 29; CPU1: 10+6+3+10 = 29 (tie goes to you); CPU2: 3+3+6+6 = 18; CPU3: 4.
            Assert.AreEqual(string.Join(",", new[] { 29, 29, 18, 4 }), string.Join(",", save.party.cup.points));
            Assert.AreEqual(1, last.cupPlace);
            Assert.AreEqual(CupTrophy.Gold, last.cupTrophy);
            Assert.AreEqual(KingdomCup.Bonus(1, PartyDifficulty.Hard), last.cupBonus);
            Assert.AreEqual(CupTrophy.Gold, save.party.BestTrophy(PartyDifficulty.Hard));
            Assert.AreEqual(CupTrophy.None, save.party.BestTrophy(PartyDifficulty.Easy));
            Assert.IsFalse(save.party.cup.active);
            Assert.AreEqual(1, save.party.cupsPlayed);
            save.party.AwardTrophy(PartyDifficulty.Hard, CupTrophy.Bronze);
            Assert.AreEqual(CupTrophy.Gold, save.party.BestTrophy(PartyDifficulty.Hard), "keeps the best");
        }

        [Test]
        public void Cup_Standings_TiesFavourYou()
        {
            Assert.AreEqual(string.Join(",", new[] { 1, 2, 3, 4 }), string.Join(",", KingdomCup.Standings(new[] { 20, 20, 5, 1 })));
            Assert.AreEqual(string.Join(",", new[] { 4, 1, 2, 3 }), string.Join(",", KingdomCup.Standings(new[] { 0, 40, 30, 10 })));
            Assert.AreEqual(CupTrophy.None, KingdomCup.TrophyFor(4));
            Assert.Greater(KingdomCup.Bonus(1, PartyDifficulty.Normal), KingdomCup.Bonus(2, PartyDifficulty.Normal));
        }

        [Test]
        public void Ghost_RecordsEncodesAndPlaysBack()
        {
            var g = new GhostTrack();
            for (int i = 0; i < 60; i++) g.Record(1f / 60f * 10f, i * 0.5f, 2f + (i % 3) * 0.25f); // 10 s of samples
            Assert.Greater(g.Count, 50);
            var back = GhostTrack.Decode(g.Encode());
            Assert.AreEqual(g.Count, back.Count);
            for (float t = 0f; t < g.Duration; t += 0.37f)
            {
                var a = g.At(t);
                var b = back.At(t);
                Assert.AreEqual(a.x, b.x, 0.06f);
                Assert.AreEqual(a.y, b.y, 0.06f);
            }
            Assert.AreEqual(g.At(999f).x, back.At(999f).x, 0.06f, "clamps at the end");
            Assert.AreEqual(0, GhostTrack.Decode("not base64!").Count);
            Assert.AreEqual(0, GhostTrack.Decode(null).Count);
        }

        [Test]
        public void Ghost_OnlyKeptWhenFaster_AndSanitized()
        {
            var party = new PartySave();
            var g = new GhostTrack();
            g.Add(0f, 0f); g.Add(1f, 0f); g.Add(2f, 0f);
            Assert.IsTrue(party.OfferGhost(g, 80f));
            Assert.IsFalse(party.OfferGhost(g, 85f), "slower runs don't replace it");
            Assert.IsTrue(party.OfferGhost(g, 70f));
            Assert.AreEqual(70f, party.ghostSeconds);
            party.ghostSeconds = float.NaN;
            party.difficulty = 9;
            party.trophies = null;
            party.cup = null;
            party.Sanitize();
            Assert.AreEqual(0f, party.ghostSeconds);
            Assert.AreEqual("", party.ghost);
            Assert.AreEqual(2, party.difficulty);
            Assert.AreEqual(3, party.trophies.Length);
            Assert.IsNotNull(party.cup);
            Assert.IsFalse(party.cup.active);
        }
    }
}
