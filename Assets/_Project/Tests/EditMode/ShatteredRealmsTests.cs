using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    /// <summary>Story world 3 (The Shattered Realms): each realm uses its rule, the new tiles parse, and the finale boss tuning.</summary>
    [TestFixture]
    public class ShatteredRealmsTests
    {
        static int Cells(AsciiLevel l, char c)
        {
            int n = 0;
            for (int x = 0; x < l.Width; x++)
            for (int y = 0; y < l.Height; y++)
                if (l.At(x, y) == c) n++;
            return n;
        }

        [Test]
        public void EachRealm_UsesItsRule()
        {
            var ember = new AsciiLevel(StoryLayouts.EmberWastes);
            Assert.Greater(Cells(ember, 'a'), 10, "lava");
            Assert.Greater(Cells(ember, 'c'), 5, "crumbling basalt");
            Assert.Greater(ember.Count('f'), 3, "geysers");
            Assert.Greater(ember.Count('Y'), 3, "cinder crawlers");

            var sky = new AsciiLevel(StoryLayouts.SkyGardens);
            Assert.Greater(Cells(sky, '>'), 20, "tailwind");
            Assert.Greater(Cells(sky, '<'), 5, "headwind");
            Assert.Greater(Cells(sky, 'L'), 3, "vines");
            Assert.Greater(sky.Count('Q'), 3, "puff gliders");

            var sunken = new AsciiLevel(StoryLayouts.SunkenClockworks);
            Assert.Greater(Cells(sunken, ')'), 20, "current");
            Assert.Greater(Cells(sunken, '('), 5, "undertow");
            Assert.Greater(sunken.Count('D'), 3, "rust crabs");

            var toy = new AsciiLevel(StoryLayouts.ToyFortress);
            Assert.Greater(Cells(toy, '}'), 10, "belts right");
            Assert.Greater(Cells(toy, '{'), 5, "belts left");
            Assert.Greater(Cells(toy, 'c'), 5, "trapdoors");
            Assert.Greater(toy.Count('W'), 3, "wind-up soldiers");
        }

        [Test]
        public void Crossing_HasAllFourRules_InOrder()
        {
            var l = new AsciiLevel(StoryLayouts.ShatteredCrossing);
            int First(char c)
            {
                for (int x = 0; x < l.Width; x++)
                for (int y = 0; y < l.Height; y++)
                    if (l.At(x, y) == c) return x;
                return -1;
            }
            int lava = First('a'), wind = First('>'), current = First(')'), belt = First('}');
            Assert.IsTrue(lava > 0 && wind > lava && current > wind && belt > current, $"{lava} {wind} {current} {belt}");
            Assert.AreEqual(3, l.Count('C'), "a checkpoint at every gate");
            Assert.Greater(l.Width, 200);
        }

        [Test]
        public void NewTiles_Parse_AndSolidityIsRight()
        {
            var l = new AsciiLevel(new[] { "X......X", "XP.f.YQX", "XcaD{}WX", "X######X" });
            Assert.IsTrue(l.IsSolid(1, 1), "crumble block");
            Assert.IsTrue(l.IsSolid(4, 1), "belt");
            Assert.IsTrue(l.IsSolid(5, 1), "belt");
            Assert.IsFalse(l.IsSolid(2, 1), "lava is a hazard, not a floor");
            Assert.AreEqual(1, l.Count('f'));
            Assert.AreEqual(1, l.Count('Y'));
            Assert.AreEqual(1, l.Count('Q'));
            Assert.AreEqual(1, l.Count('D'));
            Assert.AreEqual(1, l.Count('W'));
            bool lavaRun = false;
            foreach (var r in l.Runs) if (r.tile == 'a') lavaRun = true;
            Assert.IsTrue(lavaRun);
        }

        [Test]
        public void Sovereign_IsAFasterWarden_WithAFairStagger()
        {
            var d = WardenTuning.Default;
            var s = WardenTuning.Scaled(1.25f);
            Assert.AreEqual(d.slamWindup / 1.25f, s.slamWindup, 0.0001);
            Assert.AreEqual(d.sweep / 1.25f, s.sweep, 0.0001);
            Assert.Less(s.stagger, d.stagger);
            Assert.Greater(s.stagger, d.stagger / 1.25f, "the stagger window shrinks less than the attacks");
            Assert.AreEqual(d.phaseTwoAt, s.phaseTwoAt, 0.0001);
            var same = WardenTuning.Scaled(0f);
            Assert.AreEqual(d.idle, same.idle, 0.0001, "bad tempo falls back to normal speed");
        }
    }
}
