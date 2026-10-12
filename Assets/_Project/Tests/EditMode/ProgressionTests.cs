using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    /// <summary>Phase 7: shard upgrades, world-completion bonus and the story scenes.</summary>
    [TestFixture]
    public class ProgressionTests
    {
        static SaveData Save(int shards)
        {
            var s = SaveData.CreateNew();
            s.starShards = shards;
            return s;
        }

        [Test]
        public void Upgrades_CostShards_LevelUp_AndMaxOut()
        {
            var s = Save(1000);
            Assert.AreEqual(150, Upgrades.NextCost(s, Upgrades.ExtraHeart));
            Assert.AreEqual(PurchaseResult.Success, Upgrades.Buy(s, Upgrades.ExtraHeart));
            Assert.AreEqual(850, s.starShards);
            Assert.AreEqual(1, Upgrades.BonusHealth(s));
            Assert.AreEqual(PurchaseResult.Success, Upgrades.Buy(s, Upgrades.ExtraHeart));
            Assert.AreEqual(2, Upgrades.BonusHealth(s));
            Assert.AreEqual(-1, Upgrades.NextCost(s, Upgrades.ExtraHeart));
            Assert.AreEqual(PurchaseResult.AlreadyOwned, Upgrades.Buy(s, Upgrades.ExtraHeart));
            Assert.AreEqual(550, s.starShards);
        }

        [Test]
        public void Upgrades_RefuseWhenPoor_OrUnknown()
        {
            var s = Save(50);
            Assert.AreEqual(PurchaseResult.NotEnoughShards, Upgrades.Buy(s, Upgrades.ShardMagnet));
            Assert.AreEqual(50, s.starShards);
            Assert.AreEqual(PurchaseResult.InvalidItem, Upgrades.Buy(s, "nope"));
            Assert.AreEqual(0f, Upgrades.MagnetRadius(s), 0.0001);
        }

        [Test]
        public void Magnet_And_Lucky_ScaleWithLevel()
        {
            var s = Save(10000);
            Upgrades.Buy(s, Upgrades.ShardMagnet);
            float r1 = Upgrades.MagnetRadius(s);
            Upgrades.Buy(s, Upgrades.ShardMagnet);
            Assert.Greater(Upgrades.MagnetRadius(s), r1);
            Assert.AreEqual(0, Upgrades.LuckyBonus(s, 100));
            Upgrades.Buy(s, Upgrades.LuckyShards);
            Assert.AreEqual(10, Upgrades.LuckyBonus(s, 100));
            Assert.AreEqual(1, Upgrades.LuckyBonus(s, 3), "rounds up to at least 1");
            Upgrades.Buy(s, Upgrades.LuckyShards);
            Assert.AreEqual(20, Upgrades.LuckyBonus(s, 100));
            Assert.AreEqual(0, Upgrades.LuckyBonus(s, 0));
        }

        [Test]
        public void Save_SanitizesUpgrades()
        {
            var s = Save(0);
            s.upgrades.Add(new UpgradeRecord { id = Upgrades.ExtraHeart, level = 9 });
            s.upgrades.Add(new UpgradeRecord { id = "gone", level = 1 });
            s.upgrades.Add(null);
            s.seenCutscenes = null; s.claimedWorldRewards = null;
            s.SanitizeAndMigrate();
            Assert.AreEqual(1, s.upgrades.Count);
            Assert.AreEqual(2, Upgrades.Level(s, Upgrades.ExtraHeart));
            Assert.IsNotNull(s.seenCutscenes);
            Assert.IsNotNull(s.claimedWorldRewards);
        }

        [Test]
        public void WorldBonus_PaysOnce_WhenEveryLevelIsCleared()
        {
            var s = Save(0);
            var frost = StoryWorlds.All[1].levels;
            s.RecordLevelResult(frost[0], 100f, 5, 0);
            Assert.AreEqual(-1, StoryWorlds.ClaimIfComplete(s, frost[0]));
            s.RecordLevelResult(frost[1], 100f, 5, 0);
            Assert.AreEqual(1, StoryWorlds.ClaimIfComplete(s, frost[1]));
            Assert.AreEqual(StoryWorlds.CompletionBonus, s.starShards);
            Assert.AreEqual(-1, StoryWorlds.ClaimIfComplete(s, frost[1]), "only once");
            Assert.AreEqual(-1, StoryWorlds.ClaimIfComplete(s, "not_a_level"));
        }

        [Test]
        public void StoryWorlds_CoverEveryStoryLevelOnce()
        {
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (var w in StoryWorlds.All)
                foreach (var id in w.levels) Assert.IsTrue(seen.Add(id), "duplicate " + id);
            Assert.AreEqual(14, seen.Count);
            Assert.AreEqual(2, StoryWorlds.WorldOf("sq_fourfold_sovereign"));
        }

        [Test]
        public void StoryBeats_PlayOnce_AndBossesHaveIntroAndDefeat()
        {
            var s = Save(0);
            foreach (var boss in new[] { "sq_clockwork_warden", "sq_fourfold_sovereign" })
            {
                Assert.IsTrue(StoryBeats.For(boss, BeatTiming.BeforeLevel).HasValue, boss + " intro");
                Assert.IsTrue(StoryBeats.For(boss, BeatTiming.AfterClear).HasValue, boss + " defeat");
            }
            var b = StoryBeats.Pending(s, "sq_ember_wastes", BeatTiming.BeforeLevel);
            Assert.IsTrue(b.HasValue);
            Assert.IsTrue(StoryBeats.MarkSeen(s, b.Value.id));
            Assert.IsFalse(StoryBeats.Pending(s, "sq_ember_wastes", BeatTiming.BeforeLevel).HasValue);
            Assert.IsFalse(StoryBeats.MarkSeen(s, b.Value.id));
            Assert.IsFalse(StoryBeats.Pending(s, "sq_sky_gardens", BeatTiming.BeforeLevel).HasValue);
            var ids = new System.Collections.Generic.HashSet<string>();
            foreach (var beat in StoryBeats.All)
            {
                Assert.IsTrue(ids.Add(beat.id), "unique id " + beat.id);
                Assert.Greater(beat.panels.Length, 0);
                Assert.IsTrue(StoryWorlds.WorldOf(beat.levelId) >= 0, "beat on a real story level: " + beat.levelId);
                foreach (var p in beat.panels) Assert.IsFalse(string.IsNullOrEmpty(p.text));
            }
        }
    }
}
