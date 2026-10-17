using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    /// <summary>Splitters, burrowers and the Kindler: their numbers, when they appear, and how they travel online.</summary>
    [TestFixture]
    public class ZombieNewFoesTests
    {
        [Test]
        public void NewKinds_HaveNumbers_AndHalvesAreWeakerAndFaster()
        {
            foreach (var f in new[] { ZLFoe.Splitter, ZLFoe.SplitterHalf, ZLFoe.Burrower })
            {
                Assert.Greater(ZLRules.Health(f, 1), 0);
                Assert.Greater(ZLRules.Speed(f, 1), 0f);
                Assert.Greater(ZLRules.Bite(f), 0);
                Assert.Greater(LastStandRules.KillPoints(f), 0);
                Assert.Greater(ZLShop.ScrapForKill(f), 0);
                Assert.Less((int)f, 0x40, "the kind fits in the snapshot's low six bits");
            }
            Assert.Less(ZLRules.Health(ZLFoe.SplitterHalf, 1) * ZLRules.SplitterHalves, ZLRules.Health(ZLFoe.Splitter, 1) + 1);
            Assert.Greater(ZLRules.Speed(ZLFoe.SplitterHalf, 1), ZLRules.Speed(ZLFoe.Splitter, 1));
            Assert.Less(LastStandRules.KillPoints(ZLFoe.SplitterHalf), LastStandRules.KillPoints(ZLFoe.Splitter));
        }

        static bool Appears(System.Func<double, ZLFoe> pick, ZLFoe kind)
        {
            for (int i = 0; i < 2000; i++) if (pick(i / 2000.0) == kind) return true;
            return false;
        }

        [Test]
        public void LastStand_SplittersFromRoundFive_BurrowersFromEleven()
        {
            Assert.IsFalse(Appears(r => LastStandRules.Pick(4, r), ZLFoe.Splitter));
            Assert.IsTrue(Appears(r => LastStandRules.Pick(5, r), ZLFoe.Splitter));
            Assert.IsFalse(Appears(r => LastStandRules.Pick(10, r), ZLFoe.Burrower));
            Assert.IsTrue(Appears(r => LastStandRules.Pick(11, r), ZLFoe.Burrower));
            Assert.IsFalse(Appears(r => LastStandRules.Pick(30, r), ZLFoe.SplitterHalf), "halves only come from a splitter");
            Assert.AreEqual(ZLFoe.Shambler, LastStandRules.Pick(2, 0.0), "the first rounds stay shamblers");
        }

        [Test]
        public void ZombieLand_NewKindsOnlyInDeeperDistricts()
        {
            Assert.IsFalse(Appears(r => ZLRules.Pick(2, r), ZLFoe.Splitter));
            Assert.IsTrue(Appears(r => ZLRules.Pick(3, r), ZLFoe.Splitter));
            Assert.IsFalse(Appears(r => ZLRules.Pick(3, r), ZLFoe.Burrower));
            Assert.IsTrue(Appears(r => ZLRules.Pick(4, r), ZLFoe.Burrower));
        }

        [Test]
        public void Burrower_DivesInRange_SurfacesCloseOrOutOfBreath()
        {
            Assert.IsTrue(ZLRules.BurrowerDives(true, 8f, 0f));
            Assert.IsFalse(ZLRules.BurrowerDives(false, 8f, 0f), "only when hunting");
            Assert.IsFalse(ZLRules.BurrowerDives(true, 2f, 0f), "already in bite range");
            Assert.IsFalse(ZLRules.BurrowerDives(true, 30f, 0f), "too far");
            Assert.IsFalse(ZLRules.BurrowerDives(true, 8f, 1f), "resting");
            Assert.IsTrue(ZLRules.BurrowerSurfaces(1f, 0.5f));
            Assert.IsTrue(ZLRules.BurrowerSurfaces(9f, ZLRules.BurrowMaxTime));
            Assert.IsFalse(ZLRules.BurrowerSurfaces(9f, 1f));
            Assert.Greater(ZLRules.BurrowTell, 0.3f, "a fair warning before it bursts up");
        }

        [Test]
        public void Kindler_IsABoxWonder_AppendedForOnlinePlay_AndCantKindleBosses()
        {
            var k = ZLCatalog.Gun("kindler");
            Assert.IsNotNull(k);
            Assert.IsTrue(k.wonder);
            Assert.Greater(k.kindle, 0f);
            Assert.IsFalse(k.bossDrop);
            Assert.IsTrue(ZLShop.BoxPool().Exists(x => x.gun == ZLCatalog.Kindler));
            Assert.AreEqual(ZLCatalog.Guns.Length - 1, ZLNet.GunIndex("kindler"), "new guns go at the end");
            Assert.AreEqual(ZLNet.GunIndex("singularity") + 1, ZLNet.GunIndex("kindler"));
            Assert.AreEqual("kindler", ZLNet.GunId(ZLNet.GunIndex("kindler")));
            Assert.IsFalse(ZLCatalog.Kindleable(ZLFoe.Herald));
            Assert.IsFalse(ZLCatalog.Kindleable(ZLFoe.FirstTurned));
            Assert.IsTrue(ZLCatalog.Kindleable(ZLFoe.Brute));
            Assert.AreEqual("storm_coil", ZLShop.RollBox(0.1, "", ZLShop.BoxPity).id, "the pity still gives the first two wonders");
            Assert.IsTrue(ZombieTips.Texts.ContainsKey(ZombieTips.Kindler));
            Assert.IsTrue(ZombieTips.Texts.ContainsKey(ZombieTips.Splitter));
            Assert.IsTrue(ZombieTips.Texts.ContainsKey(ZombieTips.Burrower));
        }

        [Test]
        public void Snapshot_CarriesBurrowedAndKindledFlags()
        {
            var s = new NetSnapshot { tick = 1 };
            s.zombies.Add(new NetZombie { id = 1, kind = (int)ZLFoe.Burrower | 0x40, x = 1f, y = 1f, hp01 = 255 });
            s.zombies.Add(new NetZombie { id = 2, kind = (int)ZLFoe.Splitter | 0x80, x = 2f, y = 1f, hp01 = 100 });
            var back = ZLNet.ReadSnapshot(ZLNet.Snapshot(s));
            Assert.AreEqual(ZLFoe.Burrower, (ZLFoe)(back.zombies[0].kind & 0x3f));
            Assert.AreNotEqual(0, back.zombies[0].kind & 0x40);
            Assert.AreEqual(ZLFoe.Splitter, (ZLFoe)(back.zombies[1].kind & 0x3f));
            Assert.AreNotEqual(0, back.zombies[1].kind & 0x80);
        }
    }
}
