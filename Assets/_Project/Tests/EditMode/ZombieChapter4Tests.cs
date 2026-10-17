using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    /// <summary>Zombie Land chapter 4 (Cold Harbor airfield) and the achievements and looks that came with this update.</summary>
    [TestFixture]
    public class ZombieChapter4Tests
    {
        static ZombieLandProgress AfterChapter3()
        {
            var p = new ZombieLandProgress();
            ZLRules.NewGame(p, 44);
            p.bossDefeated = p.chapter2Begun = p.chapter2Done = p.chapter3Begun = p.chapter3Done = true;
            return p;
        }

        [Test]
        public void Chapter4_BeginsAfterTheCurator_RoadOpens_FirstLead()
        {
            var early = new ZombieLandProgress();
            ZLRules.NewGame(early, 1);
            Assert.IsFalse(ZLRules.BeginChapter4(early), "not before chapter 3 is done");
            Assert.IsFalse(ZLRules.RoadOpen(early));
            var p = AfterChapter3();
            Assert.IsTrue(ZLRules.BeginChapter4(p));
            Assert.IsFalse(ZLRules.BeginChapter4(p), "only once");
            Assert.IsTrue(p.InChapter4);
            Assert.IsTrue(ZLRules.RoadOpen(p));
            foreach (var id in ZLCatalog.Chapter4StartingLeads) Assert.IsTrue(p.leads.Contains(id));
        }

        [Test]
        public void Hangar_OpensWithEveryClearance_OptionalMissionDoesntCount()
        {
            var p = AfterChapter3();
            ZLRules.BeginChapter4(p);
            Assert.IsFalse(ZLRules.HangarOpen(p));
            ZLRules.Complete(p, "c4_pilot");
            Assert.AreEqual(0, p.Clearances, "the pilot is a bonus");
            int required = 0;
            foreach (var m in ZLCatalog.Chapter4Missions) if (m.required) { required++; ZLRules.Complete(p, m.id); }
            Assert.AreEqual(ZLCatalog.ClearancesNeeded, required);
            Assert.AreEqual(ZLCatalog.ClearancesNeeded, p.Clearances);
            Assert.IsTrue(ZLRules.HangarOpen(p));
            p.chapter4Done = true;
            Assert.IsFalse(p.InChapter4);
            Assert.IsFalse(ZLRules.HangarOpen(p), "the Marshal only once");
        }

        [Test]
        public void Missions_AreChapter4_UniqueIds_FoundByLookup()
        {
            var ids = new System.Collections.Generic.HashSet<string>();
            foreach (var m in ZLCatalog.Missions) ids.Add(m.id);
            foreach (var m in ZLCatalog.Chapter2Missions) ids.Add(m.id);
            foreach (var m in ZLCatalog.Chapter3Missions) ids.Add(m.id);
            foreach (var m in ZLCatalog.Chapter4Missions)
            {
                Assert.AreEqual(4, m.chapter);
                Assert.AreEqual("airfield", m.district);
                Assert.IsTrue(ids.Add(m.id), "unique: " + m.id);
                Assert.IsTrue(ReferenceEquals(m, ZLCatalog.Mission(m.id)));
                foreach (var r in m.reveals) Assert.IsNotNull(ZLCatalog.Mission(r), r);
            }
            Assert.AreEqual(27, ids.Count, "the menu says 27 missions in 4 chapters");
            Assert.IsNotNull(ZLCatalog.Gun(ZLCatalog.Mission("c4_pilot").reward));
            Assert.AreEqual(5, ZLCatalog.District("airfield").threat);
        }

        [Test]
        public void Zone_Sanitizes_AndNewGameClearsChapter4()
        {
            var p = AfterChapter3();
            p.zone = ZLCatalog.AirfieldMap;
            p.Sanitize();
            Assert.IsTrue(p.InAirfield);
            Assert.IsTrue(p.OffCity);
            p.zone = "moon";
            p.Sanitize();
            Assert.AreEqual("", p.zone);
            ZLRules.BeginChapter4(p);
            p.chapter4Done = true;
            ZLRules.NewGame(p, 2);
            Assert.IsFalse(p.chapter4Begun || p.chapter4Done);
        }

        [Test]
        public void Achievements_NewOnesEarned_AndPointsFit()
        {
            var c = new ZombieCareer { prestige = 1 };
            c.Sanitize();
            WeaponMastery.AddKills(c, "street_smg", WeaponMastery.Steps[2]);
            c.secrets.Add(LastStandMap.Gravemoor.HiddenSecret);
            var zl = AfterChapter3();
            zl.chapter4Begun = zl.chapter4Done = true;
            var got = Achievements.ForZombies(c, zl);
            foreach (var id in new[] { Achievements.ZPrestige, Achievements.ZGoldFinish, Achievements.ZBriarQueen, Achievements.ZChapter4 })
                Assert.IsTrue(got.Contains(id), id);
            int points = 0;
            foreach (var a in Achievements.All) points += a.points;
            Assert.LessOrEqual(points, 1000);
            Assert.IsTrue(ZombieCosmetics.Get("ti_wheelsup").Unlocked(CosmeticStats.From(c, zl)));
            Assert.IsTrue(ZombieCosmetics.Get("ti_thornless").Unlocked(CosmeticStats.From(c, zl)));
        }
    }
}
