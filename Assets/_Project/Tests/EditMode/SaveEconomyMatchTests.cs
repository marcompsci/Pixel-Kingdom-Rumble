using System.Collections.Generic;
using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    [TestFixture]
    public class SaveDataTests
    {
        [Test]
        public void NewSave_HasNovaUnlocked()
        {
            var s = SaveData.CreateNew();
            Assert.IsTrue(s.IsCharacterUnlocked("nova"));
            Assert.IsFalse(s.IsCharacterUnlocked("brick"));
            Assert.AreEqual(SaveData.CurrentVersion, s.version);
        }

        [Test]
        public void UnlockCharacter_IsIdempotent()
        {
            var s = SaveData.CreateNew();
            Assert.IsTrue(s.UnlockCharacter("luma"));
            Assert.IsFalse(s.UnlockCharacter("luma"));
            Assert.IsFalse(s.UnlockCharacter(""));
            Assert.AreEqual(2, s.unlockedCharacters.Count);
        }

        [Test]
        public void RecordLevelResult_KeepsBestOfEachStat()
        {
            var s = SaveData.CreateNew();
            Assert.IsTrue(s.RecordLevelResult("sq_test", 90f, 10, 0));
            Assert.IsTrue(s.RecordLevelResult("sq_test", 120f, 15, 1)); // slower but more shards/secrets
            var r = s.GetLevel("sq_test");
            Assert.AreEqual(90f, r.bestTimeSeconds, 0.0001f);
            Assert.AreEqual(15, r.mostShardsCollected);
            Assert.AreEqual(1, r.mostSecretsFound);
            Assert.IsFalse(s.RecordLevelResult("sq_test", 100f, 5, 0)); // no improvement
            Assert.AreEqual(1, s.levels.Count);
        }

        [Test]
        public void SanitizeAndMigrate_RepairsBrokenData()
        {
            var s = new SaveData
            {
                version = 0,
                starShards = -40,
                unlockedCharacters = null,
                ownedCosmetics = new List<string> { "hat_a" },
                equippedCosmetics = new List<string> { "hat_a", "hat_not_owned" },
                levels = new List<LevelRecord> { null, new LevelRecord { levelId = "" }, new LevelRecord { levelId = "x", bestTimeSeconds = -1f } },
                lastSelectedCharacter = "rex"
            };
            s.SanitizeAndMigrate();
            Assert.AreEqual(SaveData.CurrentVersion, s.version);
            Assert.AreEqual(0, s.starShards);
            Assert.IsTrue(s.IsCharacterUnlocked("nova"));
            Assert.AreEqual(1, s.equippedCosmetics.Count);
            Assert.AreEqual(1, s.levels.Count);
            Assert.AreEqual(0f, s.levels[0].bestTimeSeconds, 0.0001f);
            Assert.AreEqual("nova", s.lastSelectedCharacter);
            Assert.IsNotNull(s.achievements);
        }
    }

    [TestFixture]
    public class SettingsDataTests
    {
        [Test]
        public void Defaults_ContainEveryControl()
        {
            var s = new SettingsData();
            foreach (var id in ControlIds.All) Assert.IsNotNull(s.GetPlacement(id), id);
        }

        [Test]
        public void Sanitize_ClampsAndRestoresMissingControls()
        {
            var s = new SettingsData
            {
                musicVolume = 3f,
                sfxVolume = float.NaN,
                controlLayout = new List<ControlPlacement>
                {
                    new ControlPlacement(ControlIds.Jump, 2f, -1f, 9f),
                    new ControlPlacement(ControlIds.Jump, 0.5f, 0.5f),
                    new ControlPlacement("bogus", 0.5f, 0.5f),
                    null
                }
            };
            s.Sanitize();
            Assert.AreEqual(1f, s.musicVolume, 0.0001f);
            Assert.AreEqual(0f, s.sfxVolume, 0.0001f);
            Assert.AreEqual(ControlIds.All.Length, s.controlLayout.Count);
            var jump = s.GetPlacement(ControlIds.Jump);
            Assert.AreEqual(1f, jump.x, 0.0001f);
            Assert.AreEqual(0f, jump.y, 0.0001f);
            Assert.AreEqual(SettingsData.MaxControlScale, jump.scale, 0.0001f);
            Assert.IsNull(s.GetPlacement("bogus"));
        }
    }

    [TestFixture]
    public class EconomyTests
    {
        [Test]
        public void LevelReward_SumsComponents()
        {
            Assert.AreEqual(12 + 25 + 20 + 50, Economy.LevelReward(12, 2, true));
            Assert.AreEqual(25, Economy.LevelReward(-5, -1, false));
        }

        [Test]
        public void ArenaReward_ByPlacement()
        {
            Assert.AreEqual(20, Economy.ArenaReward(1));
            Assert.AreEqual(5, Economy.ArenaReward(4));
            Assert.AreEqual(0, Economy.ArenaReward(0));
            Assert.AreEqual(0, Economy.ArenaReward(5));
        }

        [Test]
        public void Grant_CapsAtMax()
        {
            var s = SaveData.CreateNew();
            s.starShards = Economy.MaxShards - 1;
            Economy.Grant(s, 1000);
            Assert.AreEqual(Economy.MaxShards, s.starShards);
            Economy.Grant(s, -50);
            Assert.AreEqual(Economy.MaxShards, s.starShards);
        }

        [Test]
        public void TryBuyCosmetic_AllResults()
        {
            var s = SaveData.CreateNew();
            s.starShards = 30;
            Assert.AreEqual(PurchaseResult.NotEnoughShards, Economy.TryBuyCosmetic(s, "scarf_red", 50));
            Assert.AreEqual(PurchaseResult.Success, Economy.TryBuyCosmetic(s, "scarf_red", 20));
            Assert.AreEqual(10, s.starShards);
            Assert.AreEqual(PurchaseResult.AlreadyOwned, Economy.TryBuyCosmetic(s, "scarf_red", 0));
            Assert.AreEqual(PurchaseResult.InvalidItem, Economy.TryBuyCosmetic(s, "", 1));
            Assert.AreEqual(PurchaseResult.InvalidItem, Economy.TryBuyCosmetic(s, "x", -1));
        }
    }

    [TestFixture]
    public class MatchStateTests
    {
        [Test]
        public void Stock_LastFighterStandingWins()
        {
            var m = new MatchState(MatchMode.Stock, 3, startingStocks: 1);
            Assert.IsFalse(m.ReportFall(1, 0));
            Assert.IsFalse(m.IsOver);
            Assert.IsFalse(m.ReportFall(2, 0));
            Assert.IsTrue(m.IsOver);
            var p = m.Placements();
            Assert.AreEqual(1, p[0]);
            Assert.AreEqual(2, p[2]); // eliminated last
            Assert.AreEqual(3, p[1]); // eliminated first
            Assert.AreEqual(2, m.Get(0).knockouts);
        }

        [Test]
        public void Stock_RespawnsWhileStocksRemain()
        {
            var m = new MatchState(MatchMode.Stock, 2, startingStocks: 2);
            Assert.IsTrue(m.ReportFall(0, 1));
            Assert.AreEqual(1, m.Get(0).stocks);
            Assert.IsFalse(m.ReportFall(0, -1));
            Assert.IsTrue(m.IsOver);
            Assert.IsFalse(m.ReportFall(1, -1)); // ignored after match end
        }

        [Test]
        public void Timed_ScoresKOsAndFalls_AndEndsOnClock()
        {
            var m = new MatchState(MatchMode.Timed, 2, durationSeconds: 10f);
            m.ReportFall(1, 0);
            m.ReportFall(1, -1); // self-destruct: no credit
            Assert.AreEqual(1, m.Get(0).score);
            Assert.AreEqual(-2, m.Get(1).score);
            m.Tick(9.5f);
            Assert.IsFalse(m.IsOver);
            m.Tick(1f);
            Assert.IsTrue(m.IsOver);
            Assert.AreEqual(0f, m.TimeRemaining, 0.0001f);
            var p = m.Placements();
            Assert.AreEqual(1, p[0]);
            Assert.AreEqual(2, p[1]);
        }

        [Test]
        public void Timed_TiesSharePlacement()
        {
            var m = new MatchState(MatchMode.Timed, 3, durationSeconds: 5f);
            var p = m.Placements();
            Assert.AreEqual(1, p[0]);
            Assert.AreEqual(1, p[1]);
            Assert.AreEqual(1, p[2]);
        }

        [Test]
        public void Training_NeverEndsOnItsOwn()
        {
            var m = new MatchState(MatchMode.Training, 2);
            for (int i = 0; i < 20; i++) Assert.IsTrue(m.ReportFall(1, 0));
            m.Tick(9999f);
            Assert.IsFalse(m.IsOver);
            m.End();
            Assert.IsTrue(m.IsOver);
        }

        [Test]
        public void Constructor_RejectsBadFighterCount()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new MatchState(MatchMode.Stock, 5));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new MatchState(MatchMode.Stock, 0));
        }
    }
}
