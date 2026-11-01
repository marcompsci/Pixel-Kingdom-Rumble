using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    /// <summary>The Quick Game widget as the game sees it (rewards, stats), time medals and the level checklist.</summary>
    [TestFixture]
    public class QuickGameTests
    {
        static QuickGameStats Stats(int clears) => new QuickGameStats { clears = clears, runs = clears + 1, shards = clears * 2 };

        [Test]
        public void Sync_PaysEachClearOnce()
        {
            var save = SaveData.CreateNew();
            int start = save.starShards;
            Assert.AreEqual(3 * QuickGame.ShardsPerClear, QuickGame.Sync(save, Stats(3)));
            Assert.AreEqual(start + 60, save.starShards);
            Assert.AreEqual(0, QuickGame.Sync(save, Stats(3)), "already paid");
            Assert.AreEqual(QuickGame.ShardsPerClear, QuickGame.Sync(save, Stats(4)));
            Assert.AreEqual(4, save.quickGame.last.clears, "keeps the last stats for the stats page");
        }

        [Test]
        public void Sync_IsCapped_AndSurvivesAReset()
        {
            var save = SaveData.CreateNew();
            Assert.AreEqual(QuickGame.MaxPerSync, QuickGame.Sync(save, Stats(500)));
            Assert.AreEqual(0, QuickGame.Sync(save, Stats(2)), "widget data reset: new baseline, nothing paid");
            Assert.AreEqual(QuickGame.ShardsPerClear, QuickGame.Sync(save, Stats(3)));
            Assert.AreEqual(0, QuickGame.Sync(save, null));
        }

        [Test]
        public void Stats_SanitizeAndBest()
        {
            var s = new QuickGameStats { clears = -3, best = null, lastDay = null };
            s.Sanitize();
            Assert.AreEqual(0, s.clears);
            Assert.IsNotNull(s.best);
            s.best.Add(new QGBestRecord { id = "meadow_hop", steps = 33 });
            Assert.AreEqual(33, s.BestSteps("meadow_hop"));
            Assert.AreEqual(0, s.BestSteps("nope"));
            var c = s.Copy();
            c.best[0].steps = 99;
            Assert.AreEqual(33, s.BestSteps("meadow_hop"), "copy is deep");
            Assert.AreEqual("Double Gap", QuickGame.StageName("double_gap"));
            Assert.AreEqual(QuickGame.StageIds.Length, QuickGame.StageNames.Length);
            var save = SaveData.CreateNew();
            save.quickGame = null;
            save.SanitizeAndMigrate();
            Assert.IsNotNull(save.quickGame);
        }

        [Test]
        public void Stats_ReadTheWidgetsJson()
        {
            // what QuickGameEngine.swift's QGStats encodes
            string json = "{\"runs\":5,\"clears\":3,\"losses\":1,\"shards\":9,\"stomps\":4,\"taps\":120,\"streak\":2,\"bestStreak\":3," +
                          "\"lastDay\":\"2026-11-01\",\"lastStage\":\"high_road\",\"best\":[{\"id\":\"meadow_hop\",\"steps\":33}]}";
            var s = TinyJson.QuickGameStatsFrom(json);
            Assert.AreEqual(3, s.clears);
            Assert.AreEqual(120, s.taps);
            Assert.AreEqual("high_road", s.lastStage);
            Assert.AreEqual(33, s.BestSteps("meadow_hop"));
            Assert.IsNull(TinyJson.QuickGameStatsFrom("not json"));
            Assert.IsNull(TinyJson.QuickGameStatsFrom(""));
        }

        [Test]
        public void Medals_FromPar()
        {
            Assert.AreEqual(TimeMedal.None, StoryMedals.For(0f, 60f));
            Assert.AreEqual(TimeMedal.Gold, StoryMedals.For(60f, 60f));
            Assert.AreEqual(TimeMedal.Silver, StoryMedals.For(70f, 60f));
            Assert.AreEqual(TimeMedal.Silver, StoryMedals.For(75f, 60f));
            Assert.AreEqual(TimeMedal.Bronze, StoryMedals.For(75.1f, 60f));
            Assert.AreEqual(TimeMedal.Bronze, StoryMedals.For(30f, 0f), "no par: a clear is bronze");
            Assert.AreEqual(TimeMedal.Silver, StoryMedals.Next(TimeMedal.Bronze));
            Assert.AreEqual(TimeMedal.Gold, StoryMedals.Next(TimeMedal.Gold));
            Assert.AreEqual(75f, StoryMedals.Target(TimeMedal.Silver, 60f), 1e-4);
        }

        [Test]
        public void Checklist_ListsWhatsLeft()
        {
            var all = StoryChecklist.Missing(30, 30, 1, 1, 50f, 60f, "Golden Acorn", true);
            Assert.AreEqual(0, all.Count, "everything found, gold medal");
            var some = StoryChecklist.Missing(27, 30, 0, 1, 70f, 60f, "Golden Acorn", false);
            Assert.AreEqual(4, some.Count);
            Assert.AreEqual("3 Star Shards", some[0]);
            Assert.AreEqual("1 secret", some[1]);
            Assert.AreEqual("Golden Acorn", some[2]);
            Assert.AreEqual("GOLD: under 1:00.0", some[3]);
            var bronze = StoryChecklist.Missing(30, 30, 0, 0, 200f, 60f);
            Assert.AreEqual("SILVER: under 1:15.0", bronze[0]);
        }
    }
}
