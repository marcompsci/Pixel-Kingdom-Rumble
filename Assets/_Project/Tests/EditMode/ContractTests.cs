using System.Collections.Generic;
using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    [TestFixture]
    public class ContractTests
    {
        static readonly ContractInfo[] Board =
        {
            new ContractInfo { id = "c1", kind = ContractKind.Main, free = true },
            new ContractInfo { id = "c2", kind = ContractKind.Main },
            new ContractInfo { id = "s1", kind = ContractKind.Secret, revealedByClue = "clue_a" },
        };

        [Test]
        public void NewSave_TeaserOpen_RestNeedUnlock_SecretHidden()
        {
            var slots = MissionBoard.Build(Board, SaveData.CreateNew());
            Assert.AreEqual(ContractState.Open, slots[0].state);
            Assert.AreEqual(ContractState.NeedsUnlock, slots[1].state);
            Assert.AreEqual(ContractState.Hidden, slots[2].state);
            Assert.IsTrue(slots[0].Playable);
            Assert.IsFalse(slots[1].Playable);
        }

        [Test]
        public void Clue_RevealsSecret_UnlockMakesEverythingPlayable()
        {
            var save = SaveData.CreateNew();
            Assert.IsTrue(save.FindClue("clue_a"));
            Assert.IsFalse(save.FindClue("clue_a"), "only new once");
            var slots = MissionBoard.Build(Board, save);
            Assert.AreEqual(ContractState.NeedsUnlock, slots[2].state, "revealed but still paid");
            save.contractsUnlocked = true;
            slots = MissionBoard.Build(Board, save);
            foreach (var s in slots) Assert.AreEqual(ContractState.Open, s.state);
        }

        [Test]
        public void Unlock_WithoutClue_KeepsSecretHidden()
        {
            var save = SaveData.CreateNew();
            save.contractsUnlocked = true;
            Assert.AreEqual(ContractState.Hidden, MissionBoard.Build(Board, save)[2].state);
        }

        [Test]
        public void Records_KeepBestScoreAndRelics_AndTotal()
        {
            var save = SaveData.CreateNew();
            Assert.IsTrue(save.RecordMission("c1", 3000, 2));
            Assert.IsFalse(save.RecordMission("c1", 2000, 4), "lower score is not a new best");
            Assert.IsTrue(save.RecordMission("c1", 5000, 1));
            var rec = save.GetMission("c1");
            Assert.AreEqual(5000, rec.bestScore);
            Assert.AreEqual(4, rec.bestRelics, "relic best kept separately");
            save.RecordMission("c2", 1000, 0);
            Assert.AreEqual(6000, MissionBoard.TotalScore(save));
            Assert.AreEqual("NIGHT RUNNER", MissionBoard.ShadowTitle(6000));
            Assert.IsTrue(MissionBoard.Build(Board, save)[0].completed);
        }

        [Test]
        public void Score_RewardsObjectivesRelicsStealthAndSpeed()
        {
            int ghost = MissionScore.Compute(1, 3, 2, 0, 100f, 120f);
            Assert.AreEqual(1000 + 750 + 300 + 1500 + 200, ghost);
            int seen = MissionScore.Compute(1, 3, 2, 2, 100f, 120f);
            Assert.Less(seen, ghost);
            Assert.AreEqual(0, MissionScore.Compute(0, 0, 0, 9, 500f, 120f), "never negative");
        }

        [Test]
        public void Migrate_RepairsContractData()
        {
            var save = SaveData.CreateNew();
            save.foundClues = null;
            save.missions = new List<MissionRecord> { null, new MissionRecord { contractId = "" }, new MissionRecord { contractId = "c1", bestScore = -5 } };
            save.SanitizeAndMigrate();
            Assert.IsNotNull(save.foundClues);
            Assert.AreEqual(1, save.missions.Count);
            Assert.AreEqual(0, save.missions[0].bestScore);
        }

        [Test]
        public void LevelSelect_OpenAll_UnlocksEveryLevel()
        {
            var slots = LevelSelect.Build(new[] { "a", "b", "c" }, SaveData.CreateNew(), openAll: true);
            foreach (var s in slots) Assert.IsTrue(s.unlocked);
            Assert.AreEqual(0, LevelSelect.Suggested(slots));
        }

        [Test]
        public void ContractMaps_AreValid_AndHaveTheirPieces()
        {
            int scrolls = 0;
            foreach (var kv in ContractLayouts.All())
            {
                var lvl = new AsciiLevel(kv.Value);
                var errors = lvl.Validate();
                Assert.AreEqual(0, errors.Count, $"{kv.Key}: {string.Join("; ", errors)}");
                Assert.Greater(lvl.Count('R'), 3, kv.Key + " relics");
                Assert.Greater(lvl.Count('g'), 2, kv.Key + " guards");
                Assert.Greater(lvl.Count('O') + lvl.Count('V'), 0, kv.Key + " needs a steal or a target");
                foreach (var row in kv.Value) Assert.AreEqual(kv.Value[0].Length, row.Length, kv.Key + " row width");
                scrolls += lvl.Count('Z');
            }
            Assert.AreEqual(2, scrolls, "one cipher scroll per secret contract");
            Assert.AreEqual(1, new AsciiLevel(ContractLayouts.BellKeeper).Count('Z'));
            Assert.AreEqual(1, new AsciiLevel(ContractLayouts.LanternHarbor).Count('Z'));
        }
    }
}
