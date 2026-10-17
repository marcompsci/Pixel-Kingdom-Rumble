using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    /// <summary>Overcharge (the forge's second tier), weapon mastery finishes and prestige.</summary>
    [TestFixture]
    public class ZombieProgressionTests
    {
        static ZombieLandProgress WithGun(string id)
        {
            var p = new ZombieLandProgress();
            ZLRules.NewGame(p, 3);
            var g = ZLCatalog.Gun(id);
            ZLRules.GiveGun(p, g, g.magazine + g.maxReserve);
            p.equipped = id;
            return p;
        }

        [Test]
        public void Overcharge_OnlyAfterTheForge_StacksOnIt_AndPierces()
        {
            var p = WithGun("street_smg");
            var smg = ZLCatalog.Gun("street_smg");
            Assert.AreEqual(1, ZLShop.NextForgeTier(p, smg));
            Assert.IsFalse(ZLShop.CanOvercharge(p, smg), "forge it first");
            Assert.IsFalse(ZLShop.Overcharge(p, smg.id, 0));
            Assert.IsTrue(ZLShop.Forge(p, smg.id, 0));
            var forged = ZLShop.Effective(p, smg);
            Assert.AreEqual(2, ZLShop.NextForgeTier(p, smg));
            p.scrap = ZLShop.OverchargeCost - 1;
            Assert.IsFalse(ZLShop.Overcharge(p, smg.id, ZLShop.OverchargeCost), "too poor");
            p.scrap = ZLShop.OverchargeCost;
            Assert.IsTrue(ZLShop.Overcharge(p, smg.id, ZLShop.OverchargeCost));
            Assert.AreEqual(0, p.scrap);
            var oc = ZLShop.Effective(p, smg);
            Assert.IsTrue(oc.overcharged);
            Assert.IsTrue(oc.forged);
            Assert.Greater(oc.damage, forged.damage);
            Assert.Greater(oc.magazine, forged.magazine);
            Assert.Greater(oc.shotsPerSecond, forged.shotsPerSecond);
            Assert.AreEqual(1, oc.pierce);
            Assert.AreEqual(0, forged.pierce);
            StringAssert.StartsWith("OVERCHARGED", oc.name);
            Assert.AreEqual(oc.magazine, p.Gun(smg.id).mag, "refilled");
            Assert.AreEqual(0, ZLShop.NextForgeTier(p, smg), "fully upgraded");
            Assert.IsTrue(ReferenceEquals(oc, ZLShop.Effective(p, smg)), "cached, no garbage per frame");
            Assert.AreEqual(0, ZLShop.NextForgeTier(p, ZLCatalog.RelicGun(ZLRelic.Arc)), "never relics");
        }

        [Test]
        public void Overcharge_ClearsOnANewGame_AndSurvivesOldSaves()
        {
            var p = WithGun("street_smg");
            ZLShop.Forge(p, "street_smg", 0);
            ZLShop.Overcharge(p, "street_smg", 0);
            ZLRules.NewGame(p, 9);
            Assert.AreEqual(0, p.overcharged.Count);
            var old = new ZombieLandProgress { overcharged = null };
            old.Sanitize();
            Assert.IsNotNull(old.overcharged);
        }

        [Test]
        public void Snapshot_CarriesOvercharged()
        {
            var s = new NetSnapshot { tick = 3 };
            s.members.Add(new NetMember { slot = 1, forged = true, overcharged = true });
            var back = ZLNet.ReadSnapshot(ZLNet.Snapshot(s));
            Assert.IsTrue(back.members[0].forged);
            Assert.IsTrue(back.members[0].overcharged);
        }

        [Test]
        public void Mastery_BronzeSilverGold_ThenObsidianOnSixGolds()
        {
            var c = new ZombieCareer();
            Assert.AreEqual(WeaponFinish.None, WeaponMastery.AddKills(c, "street_smg", WeaponMastery.Steps[0] - 1));
            Assert.AreEqual(WeaponFinish.Bronze, WeaponMastery.AddKills(c, "street_smg", 1), "the 50th kill");
            Assert.AreEqual(WeaponFinish.None, WeaponMastery.AddKills(c, "street_smg", 1), "only reports going up");
            WeaponMastery.AddKills(c, "street_smg", WeaponMastery.Steps[2]);
            Assert.AreEqual(WeaponFinish.Gold, WeaponMastery.FinishFor(c, "street_smg"));
            Assert.AreEqual(0, WeaponMastery.NextGoal(WeaponMastery.Kills(c, "street_smg")));
            string[] five = { "riot_shotgun", "ranger_rifle", "assault_rifle", "patrol_lmg", "thumper" };
            for (int i = 0; i < 4; i++) WeaponMastery.AddKills(c, five[i], WeaponMastery.Steps[2]);
            Assert.AreEqual(5, WeaponMastery.Golds(c));
            Assert.AreEqual(WeaponFinish.Gold, WeaponMastery.FinishFor(c, "street_smg"));
            WeaponMastery.AddKills(c, five[4], WeaponMastery.Steps[2]);
            Assert.AreEqual(WeaponFinish.Obsidian, WeaponMastery.FinishFor(c, "street_smg"), "six golds: obsidian");
            Assert.IsTrue(WeaponMastery.TintsBullets(WeaponFinish.Gold));
            Assert.IsFalse(WeaponMastery.TintsBullets(WeaponFinish.Silver));
            Assert.AreEqual("*** ", WeaponMastery.Stars(WeaponFinish.Gold));
        }

        [Test]
        public void Mastery_IgnoresRelicsAndUnknownGuns_AndOldSavesSanitize()
        {
            var c = new ZombieCareer();
            Assert.AreEqual(WeaponFinish.None, WeaponMastery.AddKills(c, ZLCatalog.RelicGun(ZLRelic.Sunflare).id, 999));
            Assert.AreEqual(WeaponFinish.None, WeaponMastery.AddKills(c, "no_such_gun", 999));
            Assert.AreEqual(0, c.gunKillIds.Count);
            foreach (var g in WeaponMastery.Guns()) Assert.IsFalse(g.isRelic);
            var old = new ZombieCareer { gunKillIds = null, gunKills = null };
            old.Sanitize();
            Assert.AreEqual(0, WeaponMastery.Kills(old, "street_smg"));
            var broken = new ZombieCareer();
            broken.gunKillIds.Add("street_smg");
            broken.Sanitize();
            Assert.AreEqual(1, broken.gunKills.Count, "parallel lists line up");
        }

        [Test]
        public void Prestige_NeedsRankThirty_KeepsUnlocks_AndTheClimbPaysAgain()
        {
            var c = new ZombieCareer();
            Assert.IsFalse(ZombiePrestige.CanPrestige(c));
            SurvivorRank.Grant(c, SurvivorRank.XpAt(SurvivorRank.MaxRank));
            Assert.AreEqual(SurvivorRank.MaxRank, ZombiePrestige.Rank(c));
            Assert.IsTrue(ZombiePrestige.CanPrestige(c));
            int xpBefore = c.xp;
            int shards = ZombiePrestige.Prestige(c);
            Assert.AreEqual(ZombiePrestige.Reward(1), shards);
            Assert.AreEqual(1, c.prestige);
            Assert.AreEqual(1, ZombiePrestige.Rank(c), "the ranks start again");
            Assert.AreEqual(xpBefore, c.xp, "lifetime XP (and its leaderboard) keeps counting");
            Assert.IsTrue(SurvivorRank.Has(c.xp, "kit_forge"), "unlocks stay");
            Assert.IsFalse(ZombiePrestige.CanPrestige(c));
            StringAssert.StartsWith("PRESTIGE 1", ZombiePrestige.Label(c));
            var reached = SurvivorRank.Grant(c, SurvivorRank.XpAt(3));
            Assert.AreEqual(2, reached.Count, "climbing again pays again");
            Assert.AreEqual(2, reached[0]);
            Assert.AreEqual(3, reached[1]);
            Assert.AreEqual(3, ZombiePrestige.Rank(c));
            Assert.AreEqual(0, SurvivorRank.Grant(c, 0).Count);
        }

        [Test]
        public void Prestige_StopsAtTen_AndUnlocksLooks()
        {
            var c = new ZombieCareer();
            for (int i = 0; i < ZombiePrestige.Max + 2; i++)
            {
                SurvivorRank.Grant(c, SurvivorRank.XpAt(SurvivorRank.MaxRank));
                ZombiePrestige.Prestige(c);
            }
            Assert.AreEqual(ZombiePrestige.Max, c.prestige);
            Assert.IsFalse(ZombiePrestige.CanPrestige(c));
            var s = CosmeticStats.From(c, null);
            Assert.IsTrue(ZombieCosmetics.Get("ti_eternal").Unlocked(s));
            Assert.IsTrue(ZombieCosmetics.Get("tr_prism").Unlocked(s));
            Assert.IsTrue(ZombieCosmetics.Get("ti_survivor").Unlocked(s), "rank looks follow lifetime XP");
            var fresh = CosmeticStats.From(new ZombieCareer(), null);
            Assert.IsFalse(ZombieCosmetics.Get("ti_reborn").Unlocked(fresh));
            var bad = new ZombieCareer { prestige = 50, prestigeBase = -5 };
            bad.Sanitize();
            Assert.AreEqual(ZombiePrestige.Max, bad.prestige);
            Assert.AreEqual(0, bad.prestigeBase);
        }
    }
}
