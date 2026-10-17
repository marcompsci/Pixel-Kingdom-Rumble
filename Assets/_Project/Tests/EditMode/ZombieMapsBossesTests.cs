using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    /// <summary>The Rail Yard, Last Stand boss phases, the Singularity Cannon and Zombie Land chapter 3.</summary>
    [TestFixture]
    public class ZombieMapsBossesTests
    {
        [Test]
        public void RailYard_IsTheFourthMap_ItsHiddenBossDropsTheSingularity()
        {
            var m = LastStandMap.Get("rail_yard");
            Assert.IsTrue(ReferenceEquals(LastStandMap.RailYard, m));
            Assert.AreEqual("rail", m.File);
            Assert.AreEqual("pkr.lb.rail", m.Leaderboard);
            Assert.AreEqual("singularity", m.HiddenDrop);
            Assert.AreEqual("lever", m.EggSprite);
            Assert.AreEqual("", LastStandMap.HollowBlock.HiddenDrop);
        }

        [Test]
        public void BossPhases_HalfAndQuarter_FasterAndSummoning()
        {
            Assert.AreEqual(1, LastStandRules.BossPhase(1f));
            Assert.AreEqual(1, LastStandRules.BossPhase(0.51f));
            Assert.AreEqual(2, LastStandRules.BossPhase(0.5f));
            Assert.AreEqual(3, LastStandRules.BossPhase(0.25f));
            Assert.Greater(LastStandRules.BossPhaseSpeed(2), LastStandRules.BossPhaseSpeed(1));
            Assert.Greater(LastStandRules.BossPhaseSpeed(3), LastStandRules.BossPhaseSpeed(2));
            Assert.AreEqual(0, LastStandRules.BossSummons(1, false).count);
            Assert.AreEqual(ZLFoe.Runner, LastStandRules.BossSummons(2, false).kind);
            Assert.AreEqual(ZLFoe.Leaper, LastStandRules.BossSummons(2, true).kind);
            Assert.Greater(LastStandRules.BossSummons(3, false).count, 0);
        }

        [Test]
        public void Singularity_IsABossDrop_NeverInTheBox()
        {
            var s = ZLCatalog.Gun("singularity");
            Assert.IsNotNull(s);
            Assert.IsTrue(s.wonder && s.bossDrop);
            Assert.Greater(s.pull, 0f);
            foreach (var (g, w) in ZLShop.BoxPool()) Assert.AreNotEqual("singularity", g.id);
            for (int i = 0; i < 50; i++) Assert.AreNotEqual("singularity", ZLShop.RollBox(i / 50.0, "", i % 7).id);
            Assert.AreEqual("singularity", ZLNet.GunId(ZLNet.GunIndex("singularity")));
        }

        [Test]
        public void Chapter3_OpensAfterBlackwater_KeycardsOpenTheVault()
        {
            var p = new ZombieLandProgress();
            ZLRules.NewGame(p, 9);
            p.bossDefeated = true;
            ZLRules.BeginChapter2(p);
            Assert.IsFalse(ZLRules.BeginChapter3(p), "not before Mother Brine");
            Assert.IsFalse(ZLRules.LiftOpen(p));
            p.chapter2Done = true;
            Assert.IsTrue(ZLRules.BeginChapter3(p));
            Assert.IsTrue(ZLRules.LiftOpen(p));
            Assert.IsTrue(p.InChapter3);
            Assert.IsTrue(p.leads.Contains("c3_power"));
            Assert.IsFalse(ZLRules.VaultOpen(p));
            int shards = p.Shards, sigils = p.Sigils;
            foreach (var m in ZLCatalog.Chapter3Missions) ZLRules.Complete(p, m.id);
            Assert.AreEqual(shards, p.Shards);
            Assert.AreEqual(sigils, p.Sigils);
            Assert.AreEqual(ZLCatalog.KeycardsNeeded, p.Keycards);
            Assert.IsTrue(ZLRules.VaultOpen(p));
            Assert.IsTrue(p.rewards.Contains("thumper"));
            p.chapter3Done = true;
            Assert.IsFalse(ZLRules.VaultOpen(p));
            ZLRules.NewGame(p, 10);
            Assert.IsFalse(p.chapter3Begun);
            Assert.AreEqual("", p.zone);
        }

        [Test]
        public void Chapter3Missions_InTheLabs_WithThreeKeycards_AndOldZonesSanitize()
        {
            int req = 0;
            foreach (var m in ZLCatalog.Chapter3Missions)
            {
                Assert.AreEqual("labs", m.district);
                Assert.AreEqual(3, m.chapter);
                Assert.IsTrue(ReferenceEquals(m, ZLCatalog.Mission(m.id)));
                foreach (var r in m.reveals) Assert.IsNotNull(ZLCatalog.Mission(r), r);
                if (m.required) req++;
            }
            Assert.AreEqual(ZLCatalog.KeycardsNeeded, req);
            Assert.AreEqual(5, ZLCatalog.District("labs").threat);
            var p = new ZombieLandProgress { zone = "moon", arriveAt = null };
            p.Sanitize();
            Assert.AreEqual("", p.zone);
            Assert.AreEqual("", p.arriveAt);
        }
    }
}
