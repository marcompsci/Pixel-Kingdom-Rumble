using System.Collections.Generic;
using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    [TestFixture]
    public class ZombieLandTests
    {
        static List<string> Required()
        {
            var l = new List<string>();
            foreach (var m in ZLCatalog.Missions) if (m.required) l.Add(m.id);
            return l;
        }

        [Test]
        public void Catalog_HasFourteenMissions_TenRequired()
        {
            Assert.AreEqual(14, ZLCatalog.Missions.Length);
            Assert.AreEqual(10, ZLCatalog.RequiredCount);
            Assert.AreEqual(10, ZLCatalog.ShardsNeeded);
            var ids = new HashSet<string>();
            foreach (var m in ZLCatalog.Missions)
            {
                Assert.IsTrue(ids.Add(m.id), "unique id " + m.id);
                Assert.IsFalse(string.IsNullOrEmpty(m.briefing));
                Assert.IsNotNull(ZLCatalog.District(m.district));
                foreach (var r in m.reveals) Assert.IsNotNull(ZLCatalog.Mission(r), m.id + " reveals " + r);
                if (!m.required) Assert.IsFalse(string.IsNullOrEmpty(m.reward), m.id + " bonus needs a reward");
            }
        }

        [Test]
        public void EveryMission_CanBeLearnedFromTheTrail()
        {
            var known = new HashSet<string>(ZLCatalog.StartingLeads);
            bool grew = true;
            while (grew)
            {
                grew = false;
                foreach (var id in new List<string>(known))
                    foreach (var r in ZLCatalog.Mission(id).reveals)
                        if (known.Add(r)) grew = true;
            }
            foreach (var m in ZLCatalog.Missions) Assert.IsTrue(known.Contains(m.id), m.id + " has no lead");
        }

        [Test]
        public void Puzzle_AllTenClues_LeaveExactlyOneAnswer()
        {
            for (int seed = 1; seed < 60; seed++)
            {
                var p = ZLPuzzle.FromSeed(seed);
                Assert.AreEqual(10, p.Clues.Count);
                var all = Required();
                var relics = p.RelicsLeft(all);
                var lairs = p.LairsLeft(all);
                Assert.AreEqual(1, relics.Count, "seed " + seed);
                Assert.AreEqual(p.TrueRelic, relics[0]);
                Assert.AreEqual(1, lairs.Count, "seed " + seed);
                Assert.AreEqual(p.TrueLair, lairs[0]);
            }
        }

        [Test]
        public void Puzzle_NoClueEverRulesOutTheAnswer()
        {
            for (int seed = 1; seed < 60; seed++)
            {
                var p = ZLPuzzle.FromSeed(seed);
                foreach (var c in p.Clues.Values)
                {
                    if (!c.rulesOut) continue;
                    if (c.about == "lair") Assert.AreNotEqual(p.TrueLair, c.subject);
                    else Assert.AreNotEqual(((int)p.TrueRelic).ToString(), c.subject);
                }
            }
        }

        [Test]
        public void Puzzle_ChangesBetweenGames()
        {
            var seen = new HashSet<string>();
            for (int seed = 1; seed < 40; seed++)
            {
                var p = ZLPuzzle.FromSeed(seed);
                seen.Add(p.TrueRelic + "/" + p.TrueLair);
            }
            Assert.Greater(seen.Count, 8);
        }

        [Test]
        public void Lair_StaysSealed_UntilTenShards()
        {
            var p = new ZombieLandProgress();
            ZLRules.NewGame(p, 7);
            var puzzle = ZLPuzzle.FromSeed(p.seed);
            var req = Required();
            for (int i = 0; i < 9; i++) ZLRules.Complete(p, req[i]);
            Assert.AreEqual(9, p.Shards);
            Assert.IsFalse(ZLRules.LairOpens(p, puzzle.TrueLair, puzzle), "nine shards are not enough");
            // Bonus missions never count as shards.
            foreach (var m in ZLCatalog.Missions) if (!m.required) ZLRules.Complete(p, m.id);
            Assert.AreEqual(9, p.Shards);
            Assert.IsFalse(ZLRules.LairOpens(p, puzzle.TrueLair, puzzle));
            ZLRules.Complete(p, req[9]);
            Assert.IsTrue(ZLRules.LairOpens(p, puzzle.TrueLair, puzzle));
            foreach (var l in ZLCatalog.Lairs)
                if (l.id != puzzle.TrueLair) Assert.IsFalse(ZLRules.LairOpens(p, l.id, puzzle), "wrong lair stays empty");
        }

        [Test]
        public void Boss_OnlyTheTrueRelicHurts()
        {
            foreach (ZLRelic truth in System.Enum.GetValues(typeof(ZLRelic)))
            {
                foreach (ZLRelic r in System.Enum.GetValues(typeof(ZLRelic)))
                {
                    int dmg = ZLRules.BossDamage(ZLCatalog.RelicGun(r), 10, truth);
                    Assert.AreEqual(r == truth ? 10 : 0, dmg);
                }
                foreach (var g in ZLCatalog.Guns) Assert.AreEqual(0, ZLRules.BossDamage(g, 10, truth));
            }
        }

        [Test]
        public void NewGame_StartsWithAPistolAnd25Rounds()
        {
            var p = new ZombieLandProgress();
            ZLRules.NewGame(p, 3);
            Assert.AreEqual(1, p.guns.Count);
            Assert.AreEqual(ZLCatalog.Pistol.id, p.guns[0].id);
            Assert.AreEqual(25, p.guns[0].mag + p.guns[0].reserve);
            Assert.AreEqual(3, p.leads.Count);
        }

        [Test]
        public void Death_KeepsProgress_AndRefillsThePistol()
        {
            var p = new ZombieLandProgress();
            ZLRules.NewGame(p, 3);
            ZLRules.Complete(p, "m_medicine");
            ZLRules.Complete(p, "b_gunshop");
            p.guns[0].mag = 0; p.guns[0].reserve = 2;
            p.checkpoint = "safe_maple";
            ZLRules.Respawn(p);
            Assert.AreEqual(1, p.Shards);
            Assert.AreEqual("safe_maple", p.checkpoint);
            Assert.IsNotNull(p.Gun("riot_shotgun"), "found guns are kept");
            Assert.AreEqual(25, p.Gun(ZLCatalog.Pistol.id).mag + p.Gun(ZLCatalog.Pistol.id).reserve);
            Assert.AreEqual(1, p.deaths);
        }

        [Test]
        public void Hounds_ComeAfterFiveMinutes_AndGetWorse()
        {
            Assert.IsFalse(ZLRules.HoundsActive(299f));
            Assert.IsTrue(ZLRules.HoundsActive(300f));
            Assert.Greater(ZLRules.HoundInterval(300f), ZLRules.HoundInterval(900f));
            Assert.GreaterOrEqual(ZLRules.HoundInterval(5000f), 18f);
            Assert.AreEqual(3, ZLRules.HoundPack(0));
            Assert.AreEqual(7, ZLRules.HoundPack(40));
        }

        [Test]
        public void Relics_OneAtATime()
        {
            var p = new ZombieLandProgress();
            ZLRules.NewGame(p, 3);
            ZLRules.GiveGun(p, ZLCatalog.RelicGun(ZLRelic.Arc), 12);
            ZLRules.GiveGun(p, ZLCatalog.RelicGun(ZLRelic.Cinder), 12);
            int relics = 0;
            foreach (var g in p.guns) if (ZLCatalog.Gun(g.id).isRelic) relics++;
            Assert.AreEqual(1, relics);
            Assert.IsNotNull(p.Gun("relic_cinder"));
        }

        [Test]
        public void DeeperDistricts_AreWorse()
        {
            Assert.Greater(ZLRules.Population(4, 0), ZLRules.Population(1, 0));
            Assert.Less(ZLRules.Population(4, 2), ZLRules.Population(4, 0), "clearing nests thins the dead");
            Assert.Greater(ZLRules.Health(ZLFoe.Shambler, 4), ZLRules.Health(ZLFoe.Shambler, 1));
            int brutes1 = 0, brutes4 = 0;
            for (int i = 0; i < 1000; i++)
            {
                if (ZLRules.Pick(1, i / 1000.0) == ZLFoe.Brute) brutes1++;
                if (ZLRules.Pick(4, i / 1000.0) == ZLFoe.Brute) brutes4++;
            }
            Assert.AreEqual(0, brutes1);
            Assert.Greater(brutes4, 0);
        }
    }
}
