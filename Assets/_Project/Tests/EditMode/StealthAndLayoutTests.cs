using System.Collections.Generic;
using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    [TestFixture]
    public class StealthLogicTests
    {
        [Test]
        public void Cone_SeesAhead_NotBehind_NotOutsideTheAngle()
        {
            var guard = new Vec2(0f, 0f);
            Assert.IsTrue(StealthRules.InCone(guard, 1, new Vec2(4f, 0.5f), 6f, 35f));
            Assert.IsFalse(StealthRules.InCone(guard, 1, new Vec2(-2f, 0f), 6f, 35f), "behind");
            Assert.IsFalse(StealthRules.InCone(guard, -1, new Vec2(2f, 0f), 6f, 35f), "facing away");
            Assert.IsFalse(StealthRules.InCone(guard, 1, new Vec2(7f, 0f), 6f, 35f), "too far");
            Assert.IsFalse(StealthRules.InCone(guard, 1, new Vec2(1f, 3f), 6f, 35f), "too steep");
        }

        [Test]
        public void Takedown_OnlyFromBehind_AndNeverWhenAlerted()
        {
            Assert.IsTrue(StealthRules.IsTakedown(GuardAwareness.Calm, 1, 5f, 4f));
            Assert.IsTrue(StealthRules.IsTakedown(GuardAwareness.Suspicious, -1, 5f, 6f));
            Assert.IsFalse(StealthRules.IsTakedown(GuardAwareness.Calm, 1, 5f, 6f), "in front");
            Assert.IsFalse(StealthRules.IsTakedown(GuardAwareness.Alerted, 1, 5f, 4f), "alerted guards can't be ambushed");
        }

        [Test]
        public void Meter_FillsWhileSeen_AlertsOnce_ThenCalmsDown()
        {
            var m = new DetectionMeter(1f, 0.5f, 2f);
            Assert.IsFalse(m.Tick(0.5f, true));
            Assert.AreEqual(GuardAwareness.Suspicious, m.Awareness);
            Assert.IsTrue(m.Tick(0.6f, true), "becomes alerted");
            Assert.IsFalse(m.Tick(0.1f, true), "only reports the change once");
            Assert.AreEqual(GuardAwareness.Alerted, m.Awareness);
            m.Tick(1.5f, false);
            Assert.AreEqual(GuardAwareness.Alerted, m.Awareness);
            m.Tick(0.6f, false);
            Assert.AreEqual(GuardAwareness.Suspicious, m.Awareness);
            m.Tick(2f, false);
            Assert.AreEqual(GuardAwareness.Calm, m.Awareness);
        }

        [Test]
        public void Meter_FillsFasterUpClose()
        {
            var far = new DetectionMeter();
            var near = new DetectionMeter();
            far.Tick(0.2f, true, 0f);
            near.Tick(0.2f, true, 1f);
            Assert.Greater(near.Value, far.Value);
        }

        [Test]
        public void Alert_SnapsToAlerted_AndReportsOnlyTheChange()
        {
            var m = new DetectionMeter();
            Assert.IsTrue(m.Alert());
            Assert.IsFalse(m.Alert());
            m.Reset();
            Assert.AreEqual(GuardAwareness.Calm, m.Awareness);
            Assert.AreEqual(0.0, m.Value, 0.0001);
        }

        [Test]
        public void Rank_RewardsStayingUnseen()
        {
            Assert.AreEqual("GHOST", StealthRules.Rank(0, 0));
            Assert.AreEqual("SHADOW", StealthRules.Rank(0, 3));
            Assert.AreEqual("AGENT", StealthRules.Rank(2, 1));
            Assert.AreEqual("BRAWLER", StealthRules.Rank(5, 0));
        }
    }

    [TestFixture]
    public class AsciiLevelTests
    {
        static readonly string[] Tiny =
        {
            "..........",
            "...===....",
            ".P.*..C.G.",
            "####^^####",
        };

        [Test]
        public void Parses_BottomRowAsYZero_AndFindsPoints()
        {
            var lvl = new AsciiLevel(Tiny);
            Assert.AreEqual(10, lvl.Width);
            Assert.AreEqual(4, lvl.Height);
            Assert.AreEqual('#', lvl.At(0, 0));
            Assert.AreEqual('P', lvl.At(1, 1));
            Assert.AreEqual(1, lvl.Count('P'));
            Assert.AreEqual(1, lvl.Count('*'));
            Assert.AreEqual(8, lvl.First('G').Value.x);
            Assert.AreEqual(0, lvl.Validate().Count, string.Join("; ", lvl.Validate()));
        }

        [Test]
        public void Runs_SplitByTile()
        {
            var lvl = new AsciiLevel(Tiny);
            int ground = 0, spikes = 0, planks = 0;
            foreach (var r in lvl.Runs)
            {
                if (r.tile == '#') ground += r.length;
                if (r.tile == '^') spikes += r.length;
                if (r.tile == '=') planks += r.length;
            }
            Assert.AreEqual(8, ground);
            Assert.AreEqual(2, spikes);
            Assert.AreEqual(3, planks);
        }

        [Test]
        public void Validate_CatchesMissingStartAndFloatingGoal()
        {
            var lvl = new AsciiLevel(new[] { "......G...", "..........", "##########" });
            var errors = lvl.Validate();
            Assert.IsTrue(errors.Exists(e => e.Contains("start")), "no start");
            Assert.IsTrue(errors.Exists(e => e.Contains("goal")), "goal floats over air");
        }

        [Test]
        public void MergedRects_StackIdenticalRuns()
        {
            var lvl = new AsciiLevel(new[] { "XX..", "XX..", "####" });
            var walls = lvl.MergedRects('X');
            Assert.AreEqual(1, walls.Count);
            Assert.AreEqual(2, walls[0].w);
            Assert.AreEqual(2, walls[0].h);
            Assert.AreEqual(1, walls[0].y);
            Assert.AreEqual(1, lvl.MergedRects('#').Count);
        }

        [Test]
        public void MergedRects_CoverEveryCellExactlyOnce()
        {
            foreach (var kv in StoryLayouts.All())
            {
                var lvl = new AsciiLevel(kv.Value);
                foreach (char t in new[] { '#', 'X', 'L' })
                {
                    int cells = 0, covered = 0;
                    for (int x = 0; x < lvl.Width; x++)
                    for (int y = 0; y < lvl.Height; y++)
                        if (lvl.At(x, y) == t) cells++;
                    foreach (var r in lvl.MergedRects(t)) covered += r.w * r.h;
                    Assert.AreEqual(cells, covered, $"{kv.Key} '{t}'");
                }
            }
        }

        [Test]
        public void Secret_IsTheBoundingBoxOfItsCells()
        {
            var lvl = new AsciiLevel(new[] { "X.sss", "X.s*s", "P####" });
            Assert.IsTrue(lvl.HasSecret);
            Assert.AreEqual(2, lvl.SecretMinX);
            Assert.AreEqual(4, lvl.SecretMaxX);
            Assert.AreEqual(1, lvl.SecretMinY);
            Assert.AreEqual(2, lvl.SecretMaxY);
        }

        [Test]
        public void MoverTravel_StopsOneCellShortOfTheNextLedge()
        {
            var lvl = new AsciiLevel(new[] { "m.........", "........##", "##......##" });
            // Deck is 3 wide centered on x=0: right edge at 2 + travel; next solid at x=8, so it parks at 7.
            Assert.AreEqual(5, lvl.MoverTravel(0, 2));
        }
    }

    [TestFixture]
    public class StoryLayoutTests
    {
        [Test]
        public void EveryLayout_PassesValidation()
        {
            foreach (var kv in StoryLayouts.All())
            {
                var errors = new AsciiLevel(kv.Value).Validate();
                Assert.AreEqual(0, errors.Count, $"{kv.Key}: {string.Join("; ", errors)}");
            }
        }

        [Test]
        public void EveryLayout_HasShards_ACheckpoint_AndASecret()
        {
            foreach (var kv in StoryLayouts.All())
            {
                var lvl = new AsciiLevel(kv.Value);
                Assert.Greater(lvl.Count('*'), 9, kv.Key + " shards");
                Assert.Greater(lvl.Count('C'), 0, kv.Key + " checkpoints");
                Assert.IsTrue(lvl.HasSecret, kv.Key + " secret");
            }
        }

        [Test]
        public void StealthLevels_HaveGuards_HidingSpots_AndOneObjective()
        {
            foreach (var layout in new[] { StoryLayouts.RooftopRun, StoryLayouts.NightMarketHeist })
            {
                var lvl = new AsciiLevel(layout);
                Assert.Greater(lvl.Count('g'), 4, "guards");
                Assert.AreEqual(1, lvl.Count('O'), "objective");
                bool hay = false;
                foreach (var r in lvl.Runs) if (r.tile == 'H') hay = true;
                Assert.IsTrue(hay, "hiding spots");
            }
        }

        [Test]
        public void PlatformLevels_HaveCratesSpringsAndMovers()
        {
            foreach (var layout in new[] { StoryLayouts.SunspireHeights, StoryLayouts.GearfallCaverns })
            {
                var lvl = new AsciiLevel(layout);
                Assert.Greater(lvl.Count('?'), 0, "crates");
                Assert.Greater(lvl.Count('J'), 0, "springs");
                Assert.Greater(lvl.Count('m') + lvl.Count('u'), 0, "moving platforms");
            }
        }

        [Test]
        public void Layouts_AreRectangular_AndWalledOnBothSides()
        {
            foreach (var kv in StoryLayouts.All())
            {
                int w = kv.Value[0].Length;
                foreach (var row in kv.Value) Assert.AreEqual(w, row.Length, kv.Key + " row width");
                var lvl = new AsciiLevel(kv.Value);
                for (int y = 1; y < lvl.Height; y++)
                {
                    Assert.IsTrue(lvl.IsSolid(0, y) || lvl.At(0, y) == '~', $"{kv.Key} left wall y{y}");
                    Assert.IsTrue(lvl.IsSolid(lvl.Width - 1, y) || lvl.At(lvl.Width - 1, y) == '~', $"{kv.Key} right wall y{y}");
                }
            }
        }

        static IEnumerable<string> Names() { foreach (var kv in StoryLayouts.All()) yield return kv.Key; }
    }
}
