using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    [TestFixture]
    public class LevelSelectTests
    {
        static readonly string[] Route = { "sq_sunspire_test", "sq_clockwork_warden", "sq_future" };

        [Test]
        public void NewSave_OnlyFirstLevelOpen()
        {
            var slots = LevelSelect.Build(Route, SaveData.CreateNew());
            Assert.AreEqual(3, slots.Length);
            Assert.IsTrue(slots[0].unlocked);
            Assert.IsFalse(slots[1].unlocked);
            Assert.IsFalse(slots[2].unlocked);
            Assert.IsFalse(slots[0].rank.HasValue);
            Assert.AreEqual(0, LevelSelect.Suggested(slots));
            Assert.AreEqual("-", LevelSelect.RankLabel(slots[0].rank));
        }

        [Test]
        public void ClearingALevel_OpensTheNext_AndShowsBests()
        {
            var save = SaveData.CreateNew();
            save.RecordLevelResult("sq_sunspire_test", 101.5f, 20, 1);
            Assert.IsTrue(save.RecordRank("sq_sunspire_test", ClearRank.A));
            var slots = LevelSelect.Build(Route, save);
            Assert.IsTrue(slots[0].completed);
            Assert.IsTrue(slots[1].unlocked, "boss opens after Sunspire");
            Assert.IsFalse(slots[2].unlocked);
            Assert.AreEqual(101.5f, slots[0].bestTimeSeconds, 0.001f);
            Assert.AreEqual(20, slots[0].shards);
            Assert.AreEqual(ClearRank.A, slots[0].rank.Value);
            Assert.AreEqual(1, LevelSelect.Suggested(slots), "suggests the next uncleared level");
        }

        [Test]
        public void Rank_KeepsTheBest_NeedsARecord()
        {
            var save = SaveData.CreateNew();
            Assert.IsFalse(save.RecordRank("sq_sunspire_test", ClearRank.S), "no record yet");
            save.RecordLevelResult("sq_sunspire_test", 90f, 1, 0);
            Assert.IsTrue(save.RecordRank("sq_sunspire_test", ClearRank.B));
            Assert.IsFalse(save.RecordRank("sq_sunspire_test", ClearRank.C), "worse rank ignored");
            Assert.IsTrue(save.RecordRank("sq_sunspire_test", ClearRank.S));
            Assert.AreEqual(ClearRank.S, save.GetLevel("sq_sunspire_test").BestRank.Value);
        }

        [Test]
        public void AllCleared_SuggestsLast_BadRankRepaired()
        {
            var save = SaveData.CreateNew();
            foreach (var id in Route) save.RecordLevelResult(id, 60f, 0, 0);
            Assert.AreEqual(2, LevelSelect.Suggested(LevelSelect.Build(Route, save)));
            save.GetLevel("sq_future").bestRankPlusOne = 9;
            save.SanitizeAndMigrate();
            Assert.AreEqual(0, save.GetLevel("sq_future").bestRankPlusOne);
            Assert.AreEqual(0, LevelSelect.Build(null, save).Length);
        }

        [Test]
        public void ClearedLevel_StaysOpen_EvenIfAnEarlierOneIsnt()
        {
            var save = SaveData.CreateNew();
            save.RecordLevelResult("sq_clockwork_warden", 80f, 0, 0); // e.g. played the boss scene directly
            var slots = LevelSelect.Build(Route, save);
            Assert.IsTrue(slots[1].unlocked, "cleared levels are never locked");
            Assert.IsTrue(slots[2].unlocked, "and they open the next one");
            Assert.AreEqual(0, LevelSelect.Suggested(slots));
        }
    }
}
