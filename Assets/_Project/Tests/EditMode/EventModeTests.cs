using System.Collections.Generic;
using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    [TestFixture]
    public class ObstacleLegendTests
    {
        [Test]
        public void LockedDoors_NeedAKeyEach_AndAreSolid()
        {
            var map = new AsciiLevel(new[] { "X.....n..X", "XP....n.GX", "##########" });
            Assert.IsTrue(map.IsSolid(6, 1));
            Assert.IsTrue(map.Validate().Exists(e => e.Contains("locked door")), "door without a key");
            var ok = new AsciiLevel(new[] { "X.....n..X", "XPk...n.GX", "##########" });
            Assert.AreEqual(0, ok.Validate().Count, string.Join("; ", ok.Validate()));
            Assert.AreEqual(1, ok.MergedRects('n').Count, "a column of n is one door");
        }

        [Test]
        public void SwitchBlocks_NeedASwitch()
        {
            var map = new AsciiLevel(new[] { "X..1122..X", "XP......GX", "##########" });
            Assert.IsTrue(map.Validate().Exists(e => e.Contains("switch")));
            var ok = new AsciiLevel(new[] { "X..1122..X", "XP.!....GX", "##########" });
            Assert.AreEqual(0, ok.Validate().Count, string.Join("; ", ok.Validate()));
            Assert.AreEqual(2, ok.CellCount('1'));
            Assert.IsFalse(ok.IsSolid(3, 2), "switch blocks are built separately (they come and go)");
        }

        [Test]
        public void Crushers_Lasers_Saws_ParseAsPoints()
        {
            var map = new AsciiLevel(new[] { "X.v...l..X", "XP..o.l.GX", "##########" });
            Assert.AreEqual(1, map.Count('v'));
            Assert.AreEqual(1, map.Count('o'));
            Assert.AreEqual(1, map.MergedRects('l').Count);
            Assert.AreEqual(2, map.MergedRects('l')[0].h);
        }

        [Test]
        public void LaterStoryLevels_HaveMoreObstacles()
        {
            int Obstacles(string[] rows)
            {
                var m = new AsciiLevel(rows);
                return m.Count('v') + m.Count('o') + m.MergedRects('l').Count;
            }
            Assert.Greater(Obstacles(StoryLayouts.ClockworkGlacier), Obstacles(StoryLayouts.SunspireHeights));
            Assert.Greater(Obstacles(StoryLayouts.ShatteredCrossing), Obstacles(StoryLayouts.GearfallCaverns));
            Assert.Greater(Obstacles(StoryLayouts.ShatteredCrossing), 6);
        }
    }

    [TestFixture]
    public class MazeAndEventLayoutTests
    {
        static IEnumerable<KeyValuePair<string, string[]>> AllMaps()
        {
            foreach (var kv in MazeLayouts.All()) yield return kv;
            foreach (var kv in EventLayouts.All()) yield return kv;
        }

        [Test]
        public void EveryMap_PassesValidation_IsRectangular_AndWalled()
        {
            foreach (var kv in AllMaps())
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
        public void Mazes_UseKeysDoorsAndSwitches_AndHaveACheckpoint()
        {
            foreach (var kv in MazeLayouts.All())
            {
                var lvl = new AsciiLevel(kv.Value);
                Assert.Greater(lvl.MergedRects('n').Count, 0, kv.Key + " doors");
                Assert.GreaterOrEqual(lvl.Count('k'), lvl.MergedRects('n').Count, kv.Key + " keys");
                Assert.Greater(lvl.Count('!'), 0, kv.Key + " switches");
                Assert.Greater(lvl.Count('C'), 0, kv.Key + " checkpoint");
                Assert.Greater(lvl.Count('v') + lvl.Count('o') + lvl.MergedRects('l').Count, 3, kv.Key + " obstacles");
            }
        }

        [Test]
        public void EventRuns_HaveTreats_Checkpoints_AndFoes()
        {
            foreach (var kv in EventLayouts.All())
            {
                var lvl = new AsciiLevel(kv.Value);
                if (lvl.IsSurvival) continue;
                Assert.Greater(lvl.Count('*'), 15, kv.Key + " treats");
                Assert.Greater(lvl.Count('C'), 1, kv.Key + " checkpoints");
                int foes = 0;
                foreach (char c in "BTMKFYQDW") foes += lvl.Count(c);
                Assert.Greater(foes, 3, kv.Key + " foes");
            }
        }

        [Test]
        public void ZombieArena_HasSpawnsBarricadesAndEverySurvivalStation()
        {
            var lvl = new AsciiLevel(EventLayouts.ZombieArena);
            Assert.IsTrue(lvl.IsSurvival);
            Assert.AreEqual(0, lvl.Count('G'));
            Assert.AreEqual(1, lvl.Count('P'));
            Assert.GreaterOrEqual(lvl.Count('z'), 4);
            Assert.GreaterOrEqual(lvl.MergedRects('w').Count, 2, "a barricade per window");
            Assert.GreaterOrEqual(lvl.Count('x'), 4, "wall weapons");
            Assert.AreEqual(1, lvl.Count('b'), "mystery crate");
            Assert.GreaterOrEqual(lvl.Count('p'), 4, "a shrine per perk");
            Assert.AreEqual(1, lvl.Count('U'), "forge");
            Assert.AreEqual(3, lvl.Count('j'), "three lanterns");
            Assert.GreaterOrEqual(lvl.MergedRects('q').Count, 1, "paid door");
            Assert.IsTrue(lvl.IsSolid(5, 2) && lvl.IsSolid(26, 14), "barricades and doors are solid");
        }
    }

    [TestFixture]
    public class ZombieRulesTests
    {
        [Test]
        public void Rounds_GetBiggerFasterAndTougher()
        {
            Assert.AreEqual(6, ZombieRules.ZombiesInRound(1));
            int last = 0;
            for (int r = 1; r <= 30; r++)
            {
                int n = ZombieRules.ZombiesInRound(r);
                Assert.GreaterOrEqual(n, last, "round " + r);
                Assert.LessOrEqual(n, 90);
                last = n;
            }
            Assert.Greater(ZombieRules.SpawnInterval(1), ZombieRules.SpawnInterval(10));
            Assert.GreaterOrEqual(ZombieRules.SpawnInterval(99), 0.45f);
            Assert.Greater(ZombieRules.Health(ZombieKind.Shambler, 10), ZombieRules.Health(ZombieKind.Shambler, 1));
            Assert.LessOrEqual(ZombieRules.MaxAlive(50), 16);
            Assert.Greater(ZombieRules.Speed(ZombieKind.Runner, 1), ZombieRules.Speed(ZombieKind.Shambler, 1));
        }

        [Test]
        public void EarlyRounds_AreShamblersOnly_NewKindsJoinLater()
        {
            for (double roll = 0; roll < 1; roll += 0.01)
            {
                Assert.AreEqual(ZombieKind.Shambler, ZombieRules.Pick(1, roll));
                Assert.AreNotEqual(ZombieKind.Brute, ZombieRules.Pick(5, roll));
            }
            var seen = new HashSet<ZombieKind>();
            for (double roll = 0; roll < 1; roll += 0.005) seen.Add(ZombieRules.Pick(12, roll));
            foreach (var k in new[] { ZombieKind.Shambler, ZombieKind.Runner, ZombieKind.Bloater, ZombieKind.Spitter, ZombieKind.Brute, ZombieKind.Bat })
                Assert.IsTrue(seen.Contains(k), "round 12 has " + k);
            Assert.IsFalse(seen.Contains(ZombieKind.PumpkinKing), "the boss is never a random pick");
        }

        [Test]
        public void BossEveryFifthRound_AlwaysDrops()
        {
            Assert.IsTrue(ZombieRules.IsBossRound(5));
            Assert.IsTrue(ZombieRules.IsBossRound(10));
            Assert.IsFalse(ZombieRules.IsBossRound(4));
            Assert.IsTrue(ZombieRules.Drops(ZombieKind.PumpkinKing, 99, 0.99));
            Assert.IsFalse(ZombieRules.Drops(ZombieKind.Shambler, ZombieRules.MaxDropsPerRound, 0.0), "drop cap");
            Assert.IsTrue(ZombieRules.Drops(ZombieKind.Shambler, 0, 0.01));
        }

        [Test]
        public void Points_DoubleWithTwinTreats_AndSpendingIsChecked()
        {
            var run = new SurvivalRun();
            Assert.AreEqual(ZombieRules.StartingPoints, run.Points);
            Assert.AreEqual(1, run.NextRound());
            Assert.AreEqual(60, run.Kill(ZombieKind.Shambler, melee: false));
            Assert.AreEqual(130, run.Kill(ZombieKind.Shambler, melee: true), "melee kills pay extra");
            run.DoublePoints = true;
            Assert.AreEqual(20, run.Earn(ZombieRules.HitPoints));
            Assert.AreEqual(2, run.Kills);
            int before = run.Points;
            Assert.IsFalse(run.Spend(before + 1));
            Assert.AreEqual(before, run.Points);
            Assert.IsTrue(run.Spend(100));
            Assert.AreEqual(before - 100, run.Points);
            Assert.AreEqual(60 + 130 + 20, run.TotalEarned, "spending doesn't lower the score");
        }

        [Test]
        public void Perks_BuyOnce_SecondWindIsUsedUp()
        {
            var run = new SurvivalRun();
            run.Earn(5000);
            Assert.IsTrue(run.BuyPerk(PerkKind.SecondWind));
            Assert.IsFalse(run.BuyPerk(PerkKind.SecondWind), "once");
            Assert.IsTrue(run.HasPerk(PerkKind.SecondWind));
            Assert.IsTrue(run.UseSecondWind());
            Assert.IsFalse(run.UseSecondWind());
            Assert.IsFalse(run.HasPerk(PerkKind.SecondWind));
            var poor = new SurvivalRun();
            Assert.IsFalse(poor.BuyPerk(PerkKind.QuickDraw), "too expensive at the start");
        }

        [Test]
        public void Armory_CrateNeverRollsWhatYouHold_ForgeUpgradesOnce()
        {
            for (double roll = 0; roll < 1; roll += 0.01)
                Assert.AreNotEqual("bone_rifle", ZombieArmory.RollCrate(roll, "bone_rifle").id);
            var up = ZombieArmory.Upgrade(ZombieArmory.PeaPopper);
            Assert.IsTrue(up.upgraded);
            Assert.AreEqual(ZombieArmory.PeaPopper.damage * 2, up.damage);
            Assert.Greater(up.magazine, ZombieArmory.PeaPopper.magazine);
            StringAssert.StartsWith("CURSED", up.name);
            Assert.AreEqual(up.damage, ZombieArmory.Upgrade(up).damage, "no double upgrade");
            foreach (var w in ZombieArmory.All())
            {
                Assert.Greater(w.damage, 0, w.id);
                Assert.Greater(w.magazine, 0, w.id);
                Assert.Greater(w.shotsPerSecond, 0f, w.id);
                Assert.IsFalse(string.IsNullOrEmpty(w.shot), w.id);
            }
            foreach (var w in ZombieArmory.Wall) Assert.Greater(w.cost, 0, w.id);
        }

        [Test]
        public void Save_KeepsBestSurvivalRun()
        {
            var s = SaveData.CreateNew();
            Assert.IsTrue(s.RecordSurvival(4, 3000, 40));
            Assert.IsFalse(s.RecordSurvival(3, 9000, 10), "a lower round is not a best");
            Assert.AreEqual(4, s.zombieBestRound);
            Assert.AreEqual(3000, s.zombieBestPoints);
            Assert.AreEqual(40, s.zombieBestKills);
            Assert.IsTrue(s.RecordSurvival(4, 3500, 41), "same round, more points");
            Assert.AreEqual(41, s.zombieBestKills);
        }
    }
}
