using System.Collections.Generic;
using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    /// <summary>Kingdom Plaza party quests: items, race standings, the crown zone, coins, live counts and records.</summary>
    [TestFixture]
    public class PartyTests
    {
        [Test]
        public void Items_CatchUp_LastPlaceGetsTheBell_LeaderNever()
        {
            var lead = PartyItems.Weights(1, 4);
            var last = PartyItems.Weights(4, 4);
            Assert.AreEqual(0, lead[(int)PartyItem.ThunderBell], "the leader never gets the bell");
            Assert.Greater(last[(int)PartyItem.ThunderBell], 0);
            Assert.Greater(last[(int)PartyItem.DashFizz], lead[(int)PartyItem.DashFizz], "more speed at the back");
            Assert.Greater(lead[(int)PartyItem.SlickPuddle], last[(int)PartyItem.SlickPuddle], "more puddles at the front");
            var rng = new System.Random(3);
            var seen = new HashSet<PartyItem>();
            for (int i = 0; i < 400; i++) seen.Add(PartyItems.Roll(rng, 4, 4));
            Assert.AreEqual(5, seen.Count, "last place can roll everything");
            for (int i = 0; i < 400; i++) Assert.AreNotEqual(PartyItem.ThunderBell, PartyItems.Roll(rng, 1, 4));
            foreach (PartyItem it in System.Enum.GetValues(typeof(PartyItem))) Assert.IsTrue(PartyItems.Name(it).Length > 0);
        }

        [Test]
        public void Race_PlacesByDistance_ThenFinishOrder()
        {
            var r = new RaceStandings(4, 100f);
            r.Report(0, 40f); r.Report(1, 60f); r.Report(2, 20f); r.Report(3, 50f);
            Assert.AreEqual(1, r.Place(1));
            Assert.AreEqual(2, r.Place(3));
            Assert.AreEqual(3, r.Place(0));
            Assert.AreEqual(4, r.Place(2));
            r.Report(1, 30f);
            Assert.AreEqual(1, r.Place(1), "going backwards doesn't lose your best distance");
            Assert.IsTrue(r.Report(2, 101f), "crossing the line");
            Assert.IsFalse(r.Report(2, 120f), "only once");
            Assert.AreEqual(1, r.Place(2), "first across the line is first");
            Assert.AreEqual(2, r.Place(1));
            Assert.IsTrue(r.Report(0, 100f));
            Assert.AreEqual(2, r.Place(0));
            Assert.AreEqual(3, r.Place(1));
            Assert.IsFalse(r.AllFinished);
            Assert.AreEqual(0.5f, new RaceStandings(1, 10f).Progress01(0, 10f), 1f); // degenerate course doesn't throw
        }

        [Test]
        public void CrownZone_OnlyALoneFighterScores_AndItMoves()
        {
            Assert.AreEqual(2, CrownZone.Scorer(new[] { false, false, true, false }));
            Assert.AreEqual(-1, CrownZone.Scorer(new[] { true, false, true, false }), "contested");
            Assert.AreEqual(-1, CrownZone.Scorer(new[] { false, false, false, false }));
            int prev = CrownZone.SpotAt(0f, 4, 7);
            for (int k = 1; k < 8; k++)
            {
                int now = CrownZone.SpotAt(k * CrownZone.MoveEvery + 0.1f, 4, 7);
                Assert.AreNotEqual(prev, now, "never the same spot twice in a row");
                Assert.IsTrue(now >= 0 && now < 4);
                prev = now;
            }
            Assert.AreEqual(CrownZone.SpotAt(3f, 4, 7), CrownZone.SpotAt(14f, 4, 7), "stays put for 15 seconds");
        }

        [Test]
        public void Coins_DropNoMoreThanYouHave()
        {
            Assert.AreEqual(2, CoinRules.Drop(10, false));
            Assert.AreEqual(5, CoinRules.Drop(10, true));
            Assert.AreEqual(1, CoinRules.Drop(1, true));
            Assert.AreEqual(0, CoinRules.Drop(0, false));
            var m = ArenaMatchConfig.ForParty(PartyMode.CoinRush).CreateMatch();
            Assert.AreEqual(MatchMode.Timed, m.Mode);
            Assert.AreEqual(90f, m.DurationSeconds, 1e-4);
            m.AddScore(1, 5);
            m.ReportFall(1, 0);
            Assert.AreEqual(5, m.Get(1).score, "falls don't change the coin score");
            Assert.AreEqual(0, m.Get(0).score);
            m.AddScore(1, -9);
            Assert.AreEqual(0, m.Get(1).score, "never below zero");
            m.AddScore(2, 3);
            Assert.AreEqual(1, m.Placements()[2]);
        }

        [Test]
        public void PartyConfigs_HaveTheRightRules()
        {
            var balloon = ArenaMatchConfig.ForParty(PartyMode.BalloonBattle);
            Assert.AreEqual(MatchMode.Stock, balloon.mode);
            Assert.AreEqual(3, balloon.stocks);
            Assert.AreEqual(4, balloon.FighterCount);
            Assert.IsFalse(balloon.Rated, "party quests don't change Rumble Rating");
            var king = ArenaMatchConfig.ForParty(PartyMode.KingOfTheHill).CreateMatch();
            Assert.IsFalse(king.ScoresFalls);
            var normal = new ArenaMatchConfig { mode = MatchMode.Timed }.CreateMatch();
            Assert.IsTrue(normal.ScoresFalls, "Arena Clash keeps its knock-out scoring");
            Assert.AreEqual(5, PartyModes.All.Length);
            Assert.AreEqual("COIN RUSH", PartyModes.Info(PartyMode.CoinRush).name);
        }

        [Test]
        public void LiveCount_IsHonest()
        {
            Assert.AreEqual("-- LIVE", LiveCount.Label(null), "unknown is never shown as a number");
            Assert.AreEqual("-- LIVE", LiveCount.Label(-1));
            Assert.AreEqual("0 LIVE", LiveCount.Label(0));
            Assert.AreEqual("37 LIVE", LiveCount.Label(37));
            Assert.AreEqual("1.2K LIVE", LiveCount.Label(1234));
            Assert.AreEqual("12K LIVE", LiveCount.Label(12345));
            Assert.AreEqual(null, LiveCount.Total(new int?[] { null, null }));
            Assert.AreEqual(7, LiveCount.Total(new int?[] { 3, null, 4 }));
            var groups = new HashSet<int> { KingdomPlaza.PlazaGroup };
            foreach (var b in KingdomPlaza.Buildings) Assert.IsTrue(groups.Add(b.playerGroup), "each place has its own group");
            Assert.AreEqual(9, KingdomPlaza.Buildings.Length);
        }

        [Test]
        public void Records_CountAndPay()
        {
            var save = SaveData.CreateNew();
            int start = save.starShards;
            Assert.AreEqual(40, PartyResults.Record(save, PartyMode.ItemRace, 1, 95f));
            Assert.AreEqual(8, PartyResults.Record(save, PartyMode.ItemRace, 4, 80f));
            Assert.AreEqual(start + 48, save.starShards);
            var r = save.party.Find(PartyMode.ItemRace);
            Assert.AreEqual(2, r.played);
            Assert.AreEqual(1, r.wins);
            Assert.AreEqual(1, r.podiums);
            Assert.AreEqual(80f, r.bestRaceSeconds, 1e-4, "fastest finish");
            Assert.AreEqual(0, PartyResults.Record(save, PartyMode.None, 1));
            save.party = null;
            save.SanitizeAndMigrate();
            Assert.IsNotNull(save.party);
        }
    }
}

namespace PKR.Tests
{
    [TestFixture]
    public class PartyLayoutTests
    {
        [Test]
        public void PartyMaps_PassValidation_AreRectangular_AndWalled()
        {
            foreach (var kv in PartyLayouts.All())
            {
                int w = kv.Value[0].Length;
                foreach (var row in kv.Value) Assert.AreEqual(w, row.Length, kv.Key + " row width");
                var lvl = new AsciiLevel(kv.Value);
                var errors = lvl.Validate();
                Assert.AreEqual(0, errors.Count, $"{kv.Key}: {string.Join("; ", errors)}");
                for (int y = 0; y < lvl.Height; y++)
                {
                    Assert.IsTrue(lvl.IsSolid(0, y), $"{kv.Key} left wall y{y}");
                    Assert.IsTrue(lvl.IsSolid(lvl.Width - 1, y), $"{kv.Key} right wall y{y}");
                }
            }
        }

        [Test]
        public void Plaza_FitsEveryBuilding_AndTheRaceHasNoFoesOrSpikes()
        {
            var plaza = new AsciiLevel(PartyLayouts.KingdomPlaza);
            foreach (var b in KingdomPlaza.Buildings) Assert.Less(b.x, plaza.Width - 6, b.name + " fits on the street");
            var race = new AsciiLevel(PartyLayouts.ItemRace);
            foreach (char c in "BTMKFYQDW^") Assert.AreEqual(0, race.Count(c), "race course has no " + c);
            Assert.Greater(race.Count('C'), 2, "checkpoints to respawn racers");
        }
    }
}
