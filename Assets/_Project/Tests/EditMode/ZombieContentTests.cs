using System.Collections.Generic;
using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    /// <summary>Chapter 2 (Blackwater Isle), the riot and leaper zombies and the Rime Cannon.</summary>
    [TestFixture]
    public class ZombieContentTests
    {
        [Test]
        public void RiotShield_BlocksFromTheFront_NotFromBehindOrTheSide()
        {
            // facing right (+x); a shot travelling left (-x) hits the front
            Assert.AreEqual(2, ZLRules.RiotDamage(10, 1f, 0f, -1f, 0f));
            Assert.AreEqual(10, ZLRules.RiotDamage(10, 1f, 0f, 1f, 0f), "from behind");
            Assert.AreEqual(10, ZLRules.RiotDamage(10, 1f, 0f, 0f, -1f), "from the side");
            Assert.AreEqual(2, ZLRules.RiotDamage(10, 1f, 0f, -1f, 0.5f), "a little off-centre still hits the shield");
            Assert.AreEqual(10, ZLRules.RiotDamage(10, 1f, 0f, 0f, 0f), "explosions and melee");
            Assert.AreEqual(1, ZLRules.RiotDamage(2, 1f, 0f, -1f, 0f), "never zero");
            Assert.IsTrue(ZLRules.ShieldBlocks(0f, 1f, 0f, -1f));
        }

        [Test]
        public void NewDead_OnlyDeeperInTheCity_AndLaterInLastStand()
        {
            var seen1 = Kinds(r => ZLRules.Pick(1, r));
            Assert.IsFalse(seen1.Contains(ZLFoe.Riot));
            Assert.IsFalse(seen1.Contains(ZLFoe.Leaper));
            var seen5 = Kinds(r => ZLRules.Pick(5, r));
            Assert.IsTrue(seen5.Contains(ZLFoe.Riot));
            Assert.IsTrue(seen5.Contains(ZLFoe.Leaper));
            var early = Kinds(r => LastStandRules.Pick(6, r));
            Assert.IsFalse(early.Contains(ZLFoe.Riot));
            Assert.IsFalse(early.Contains(ZLFoe.Leaper));
            var late = Kinds(r => LastStandRules.Pick(12, r));
            Assert.IsTrue(late.Contains(ZLFoe.Riot));
            Assert.IsTrue(late.Contains(ZLFoe.Leaper));
            Assert.Greater(ZLRules.Health(ZLFoe.Riot, 1), ZLRules.Health(ZLFoe.Shambler, 1));
            Assert.Greater(ZLRules.Speed(ZLFoe.Leaper, 1), ZLRules.Speed(ZLFoe.Shambler, 1));
            Assert.Greater(LastStandRules.KillPoints(ZLFoe.Riot), LastStandRules.KillPoints(ZLFoe.Shambler));
        }

        static HashSet<ZLFoe> Kinds(System.Func<double, ZLFoe> pick)
        {
            var s = new HashSet<ZLFoe>();
            for (int i = 0; i < 1000; i++) s.Add(pick(i / 1000.0));
            return s;
        }

        [Test]
        public void RimeCannon_IsABoxWonder_AndThePityGivesEitherWonder()
        {
            var rime = ZLCatalog.Gun("rime_cannon");
            Assert.IsNotNull(rime);
            Assert.IsTrue(rime.wonder);
            Assert.Greater(rime.freeze, 0f);
            bool inBox = false;
            foreach (var (g, w) in ZLShop.BoxPool()) if (g.id == rime.id) inBox = true;
            Assert.IsTrue(inBox);
            Assert.AreEqual("storm_coil", ZLShop.RollBox(0.1, "", ZLShop.BoxPity).id);
            Assert.AreEqual("rime_cannon", ZLShop.RollBox(0.9, "", ZLShop.BoxPity).id);
            Assert.AreEqual("rime_cannon", ZLShop.RollBox(0.1, "storm_coil", ZLShop.BoxPity).id, "never the one in hand");
            Assert.AreEqual("storm_coil", ZLShop.RollBox(0.9, "rime_cannon", ZLShop.BoxPity).id);
            Assert.AreEqual(21, ZLRules.FrozenDamage(14));
            Assert.AreNotEqual(ZLNet.GunIndex("storm_coil"), ZLNet.GunIndex("rime_cannon"));
            Assert.AreEqual("rime_cannon", ZLNet.GunId(ZLNet.GunIndex("rime_cannon")));
        }

        [Test]
        public void Chapter2_OpensAfterTheFirstTurned_SigilsAreSeparateFromShards()
        {
            var p = new ZombieLandProgress();
            ZLRules.NewGame(p, 7);
            Assert.IsFalse(ZLRules.BeginChapter2(p), "not before the First Turned falls");
            p.bossDefeated = true;
            Assert.IsTrue(ZLRules.BeginChapter2(p));
            Assert.IsFalse(ZLRules.BeginChapter2(p), "once");
            Assert.IsTrue(p.InChapter2);
            Assert.IsTrue(p.leads.Contains("c2_ferry"));
            int shards = p.Shards;
            foreach (var m in ZLCatalog.Chapter2Missions) ZLRules.Complete(p, m.id);
            Assert.AreEqual(shards, p.Shards, "chapter 2 missions don't count as Graystone shards");
            Assert.AreEqual(ZLCatalog.SigilsNeeded, p.Sigils);
            Assert.IsTrue(ZLRules.TideGateOpen(p));
            Assert.IsTrue(p.rewards.Contains("patrol_lmg"));
            p.chapter2Done = true;
            Assert.IsFalse(p.InChapter2);
            Assert.IsFalse(ZLRules.TideGateOpen(p));
            ZLRules.NewGame(p, 8);
            Assert.IsFalse(p.chapter2Begun);
            Assert.IsFalse(p.chapter2Done);
        }

        [Test]
        public void Chapter2Missions_AreOnTheIsle_WithUniqueIds_AndFourSigils()
        {
            var ids = new HashSet<string>();
            foreach (var m in ZLCatalog.Missions) Assert.IsTrue(ids.Add(m.id));
            int req = 0;
            foreach (var m in ZLCatalog.Chapter2Missions)
            {
                Assert.IsTrue(ids.Add(m.id), m.id);
                Assert.AreEqual("isle", m.district);
                Assert.AreEqual(2, m.chapter);
                Assert.IsTrue(ReferenceEquals(m, ZLCatalog.Mission(m.id)));
                if (m.required) req++;
                foreach (var r in m.reveals) Assert.IsNotNull(ZLCatalog.Mission(r), r);
            }
            Assert.AreEqual(ZLCatalog.SigilsNeeded, req);
            Assert.AreEqual(14, ZLCatalog.Missions.Length, "chapter 1 is unchanged");
            Assert.AreEqual(5, ZLCatalog.District("isle").threat);
        }
    }
}
