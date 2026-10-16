using System.Collections.Generic;
using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    /// <summary>The "zombies lead the game" phase: Survivor Rank, Survival modes, the Zombie Land loop and Last Stand.</summary>
    [TestFixture]
    public class ZombiesLeadTests
    {
        // ---- Survivor Rank --------------------------------------------------------------------------------

        [Test]
        public void Rank_ClimbsQuicklyAtFirst_AndEachRankPaysOnce()
        {
            Assert.AreEqual(1, SurvivorRank.RankFor(0));
            Assert.AreEqual(2, SurvivorRank.RankFor(SurvivorRank.XpAt(2)));
            Assert.Less(SurvivorRank.XpAt(2), 200, "rank 2 should come within a run or two");
            for (int r = 3; r <= SurvivorRank.MaxRank; r++)
                Assert.Greater(SurvivorRank.XpAt(r) - SurvivorRank.XpAt(r - 1), SurvivorRank.XpAt(r - 1) - SurvivorRank.XpAt(r - 2));
            Assert.AreEqual(SurvivorRank.MaxRank, SurvivorRank.RankFor(int.MaxValue / 2));

            var c = new ZombieCareer();
            var reached = SurvivorRank.Grant(c, SurvivorRank.XpAt(4) + 5);
            Assert.AreEqual(3, reached.Count);
            Assert.AreEqual(2, reached[0]);
            Assert.AreEqual(4, reached[2]);
            Assert.AreEqual(0, SurvivorRank.Grant(c, 1).Count, "no rank twice");
            Assert.IsTrue(SurvivorRank.Has(c.xp, "starter_1"));
            Assert.IsFalse(SurvivorRank.Has(c.xp, "starter_2"));
            Assert.AreEqual(1, SurvivorRank.StarterTier(c.xp));
            Assert.AreEqual(250, SurvivorRank.BonusPoints(c.xp));
        }

        [Test]
        public void Rank_UnlocksAreInOrder_AndTitlesExist()
        {
            int last = 0;
            var ids = new HashSet<string>();
            foreach (var u in SurvivorRank.Unlocks)
            {
                Assert.Greater(u.rank, last);
                Assert.IsTrue(ids.Add(u.id));
                Assert.IsFalse(string.IsNullOrEmpty(u.info));
                last = u.rank;
            }
            for (int r = 1; r <= SurvivorRank.MaxRank; r++) Assert.IsFalse(string.IsNullOrEmpty(SurvivorRank.Title(r)));
            Assert.AreEqual(0, SurvivorRank.Next(SurvivorRank.MaxRank).rank);
        }

        [Test]
        public void RunXp_RewardsRoundsWinsAndHardcore()
        {
            int endless = SurvivorRank.RunXp(SurvivalMode.Endless, 11, 100, false);
            Assert.AreEqual(10 * 20 + 50, endless);
            Assert.Greater(SurvivorRank.RunXp(SurvivalMode.Hardcore, 11, 100, false), endless);
            Assert.Greater(SurvivorRank.RunXp(SurvivalMode.Raid, 10, 80, true), SurvivorRank.RunXp(SurvivalMode.Raid, 10, 80, false) + 200);
            Assert.AreEqual(0, SurvivorRank.RunXp(SurvivalMode.Endless, 1, 0, false));
        }

        [Test]
        public void Career_KeepsBestRoundsPerMapAndMode()
        {
            var c = new ZombieCareer();
            Assert.IsTrue(c.RecordRound("manor", SurvivalMode.Raid, 6));
            Assert.IsFalse(c.RecordRound("manor", SurvivalMode.Raid, 4));
            Assert.IsTrue(c.RecordRound(LastStandRules.MapId, SurvivalMode.Raid, 3));
            Assert.AreEqual(6, c.Best("manor", SurvivalMode.Raid));
            Assert.AreEqual(3, c.Best(LastStandRules.MapId, SurvivalMode.Raid));
            Assert.AreEqual(0, c.Best("manor", SurvivalMode.Endless));
            var s = new SaveData();
            s.zombieCareer = null;
            s.SanitizeAndMigrate();
            Assert.IsNotNull(s.zombieCareer);
        }

        // ---- Survival modes ------------------------------------------------------------------------------

        [Test]
        public void Raid_IsTenRoundsWithBosses_HardcoreIsTougher()
        {
            Assert.IsTrue(SurvivalModes.IsBossRound(SurvivalMode.Raid, 5));
            Assert.IsTrue(SurvivalModes.IsBossRound(SurvivalMode.Raid, 10));
            Assert.IsTrue(SurvivalModes.IsFinalRound(SurvivalMode.Raid, 10));
            Assert.IsFalse(SurvivalModes.IsFinalRound(SurvivalMode.Endless, 10));
            Assert.Greater(SurvivalModes.BossHealth(SurvivalMode.Raid, 10, 100), SurvivalModes.BossHealth(SurvivalMode.Endless, 10, 100));
            Assert.Greater(SurvivalModes.HealthScale(SurvivalMode.Hardcore), 1f);
            Assert.Less(SurvivalModes.SpawnScale(SurvivalMode.Hardcore), 1f);
            var hc = new SurvivalRun(SurvivalMode.Hardcore, 0);
            var en = new SurvivalRun(SurvivalMode.Endless, 0);
            Assert.Greater(hc.Earn(100), en.Earn(100));
        }

        [Test]
        public void SupplyCaches_EveryThirdRound_CappedAtTen()
        {
            Assert.AreEqual(0, SurvivalModes.Caches(2));
            Assert.AreEqual(1, SurvivalModes.Caches(3));
            Assert.AreEqual(3, SurvivalModes.Caches(10));
            Assert.AreEqual(SurvivalModes.MaxCaches, SurvivalModes.Caches(200));
            Assert.IsTrue(SurvivalModes.CacheAfter(6));
            Assert.IsFalse(SurvivalModes.CacheAfter(7));
            int noWin = SurvivalModes.Shards(SurvivalMode.Raid, 10, 40, false);
            int win = SurvivalModes.Shards(SurvivalMode.Raid, 10, 40, true);
            Assert.Greater(win, noWin + SurvivalModes.RaidWinShards - 1);
            Assert.AreEqual(9 * 5 + 10 + 3 * SurvivalModes.ShardsPerCache, SurvivalModes.Shards(SurvivalMode.Endless, 10, 40, false));
        }

        [Test]
        public void Drops_PityTimerEndsDrySpells_ScavengerHelps()
        {
            var run = new SurvivalRun();
            for (int i = 0; i < ZombieRules.DropPity; i++) run.Kill(ZombieKind.Shambler, false);
            Assert.IsTrue(ZombieRules.Drops(ZombieKind.Shambler, run.DropsThisRound, run.KillsSinceDrop, 0.99, SurvivalMode.Endless, false));
            run.CountDrop();
            Assert.AreEqual(0, run.KillsSinceDrop);
            Assert.IsFalse(ZombieRules.Drops(ZombieKind.Shambler, 0, 0, 0.05, SurvivalMode.Endless, false));
            Assert.IsTrue(ZombieRules.Drops(ZombieKind.Shambler, 0, 0, 0.05, SurvivalMode.Endless, true));
            Assert.IsFalse(ZombieRules.Drops(ZombieKind.Shambler, ZombieRules.MaxDropsPerRound, 99, 0.0, SurvivalMode.Endless, false));
            Assert.IsTrue(ZombieRules.Drops(ZombieKind.Shambler, ZombieRules.MaxDropsPerRound, 99, 0.0, SurvivalMode.Endless, true));
        }

        [Test]
        public void Crate_BadLuckProtection_GivesAWonder()
        {
            for (int i = 0; i < 20; i++)
            {
                var w = ZombieArmory.RollCrate(i / 20.0, "candy_repeater", ZombieArmory.CratePity);
                Assert.IsTrue(ZombieArmory.IsWonder(w.id), w.id);
                Assert.AreNotEqual(ZombieArmory.MoonbeamRay.id, ZombieArmory.RollCrate(i / 20.0, ZombieArmory.MoonbeamRay.id, ZombieArmory.CratePity).id);
            }
            var run = new SurvivalRun();
            run.CountSpin(false); run.CountSpin(false);
            Assert.AreEqual(2, run.SpinsSinceWonder);
            run.CountSpin(true);
            Assert.AreEqual(0, run.SpinsSinceWonder);
        }

        [Test]
        public void Perks_FourAtATime_LanternsOpenAFifth()
        {
            var run = new SurvivalRun(SurvivalMode.Endless, 50000);
            Assert.IsTrue(run.BuyPerk(PerkKind.IronHeart));
            Assert.IsTrue(run.BuyPerk(PerkKind.QuickDraw));
            Assert.IsTrue(run.BuyPerk(PerkKind.Deadeye));
            Assert.IsTrue(run.BuyPerk(PerkKind.Scavenger));
            Assert.IsFalse(run.BuyPerk(PerkKind.FleetFeet), "only four");
            run.ExtraPerkSlot = true;
            Assert.IsTrue(run.BuyPerk(PerkKind.FleetFeet));
            Assert.IsFalse(run.BuyPerk(PerkKind.SecondWind));
            Assert.AreEqual(5, run.PerkCount);
            foreach (PerkKind p in System.Enum.GetValues(typeof(PerkKind)))
            {
                Assert.Greater(ZombieRules.PerkCost(p), 0);
                Assert.IsFalse(string.IsNullOrEmpty(ZombieRules.PerkInfo(p)));
            }
            Assert.AreEqual(5500, new SurvivalRun(SurvivalMode.Endless, 5000).Points);
        }

        // ---- Zombie Land: the classic loop ---------------------------------------------------------------

        static ZombieLandProgress NewZL()
        {
            var p = new ZombieLandProgress();
            ZLRules.NewGame(p, 77);
            return p;
        }

        [Test]
        public void Scrap_FromKills_DoubleScrapDoubles_SpendIsChecked()
        {
            var p = NewZL();
            Assert.AreEqual(0, p.scrap);
            Assert.AreEqual(30, ZLShop.Earn(p, ZLShop.ScrapForKill(ZLFoe.Shambler), false));
            Assert.AreEqual(300, ZLShop.Earn(p, ZLShop.ScrapForKill(ZLFoe.Brute), true));
            Assert.AreEqual(330, p.scrap);
            Assert.IsFalse(ZLShop.Spend(p, 331));
            Assert.IsTrue(ZLShop.Spend(p, 330));
            Assert.AreEqual(330, p.scrapEarned);
            Assert.Greater(ZLShop.ScrapForKill(ZLFoe.Herald), ZLShop.ScrapForKill(ZLFoe.Brute));
            Assert.Less(ZLShop.Price(1000, true), 1000);
        }

        [Test]
        public void ZLPerks_FourMax_ChangeTheHero_SecondChanceIsUsedUp()
        {
            var p = NewZL();
            p.scrap = 100000;
            Assert.AreEqual(ZLRules.HeroHealth, ZLShop.MaxHealth(p));
            Assert.IsTrue(ZLShop.BuyPerk(p, ZLPerk.ThickSkin, false));
            Assert.IsFalse(ZLShop.BuyPerk(p, ZLPerk.ThickSkin, false), "once");
            Assert.AreEqual(ZLRules.HeroHealth + 50, ZLShop.MaxHealth(p));
            Assert.IsTrue(ZLShop.BuyPerk(p, ZLPerk.QuickHands, false));
            Assert.IsTrue(ZLShop.BuyPerk(p, ZLPerk.SteadyAim, false));
            Assert.IsTrue(ZLShop.BuyPerk(p, ZLPerk.SecondChance, false));
            Assert.IsFalse(ZLShop.BuyPerk(p, ZLPerk.FleetFoot, false), "four at a time");
            Assert.Less(ZLShop.ReloadScale(p), 1f);
            Assert.Greater(ZLShop.ShotDamage(p, ZLCatalog.Pistol), ZLCatalog.Pistol.damage);
            Assert.IsTrue(ZLShop.UseSecondChance(p));
            Assert.IsFalse(ZLShop.UseSecondChance(p));
            Assert.IsTrue(ZLShop.BuyPerk(p, ZLPerk.FleetFoot, false), "the slot frees up");
            // Perks survive death; dying refills the pistol as before.
            ZLRules.Respawn(p);
            Assert.IsTrue(ZLShop.HasPerk(p, ZLPerk.ThickSkin));
        }

        [Test]
        public void Forge_DoublesDamageAndAmmo_NotRelics_NotTwice()
        {
            var p = NewZL();
            p.scrap = 20000;
            var smg = ZLCatalog.Gun("street_smg");
            ZLRules.GiveGun(p, smg, 30);
            Assert.IsTrue(ZLShop.Forge(p, smg.id, ZLShop.ForgeCost));
            Assert.IsFalse(ZLShop.Forge(p, smg.id, ZLShop.ForgeCost), "once");
            var e = ZLShop.Effective(p, smg);
            Assert.AreEqual(smg.damage * 2, e.damage);
            Assert.Greater(e.magazine, smg.magazine);
            Assert.AreEqual(e.magazine, p.Gun(smg.id).mag, "forged guns come back full");
            Assert.AreEqual(smg.damage, ZLCatalog.Gun("street_smg").damage, "the catalog is untouched");
            var relic = ZLCatalog.RelicGun(ZLRelic.Arc);
            ZLRules.GiveGun(p, relic, 10);
            Assert.IsFalse(ZLShop.Forge(p, relic.id, 0));
            Assert.IsFalse(ZLShop.Forge(p, "riot_shotgun", 0), "only guns you own");
        }

        [Test]
        public void Box_NeverGivesRelicsOrThePistol_PityGivesTheWonder()
        {
            foreach (var (g, w) in ZLShop.BoxPool())
            {
                Assert.IsFalse(g.isRelic);
                Assert.AreNotEqual(ZLCatalog.Pistol.id, g.id);
                Assert.Greater(w, 0f);
            }
            for (int i = 0; i < 50; i++)
                Assert.AreNotEqual("street_smg", ZLShop.RollBox(i / 50.0, "street_smg", 0).id);
            Assert.AreEqual(ZLCatalog.StormCoil.id, ZLShop.RollBox(0.0, "street_smg", ZLShop.BoxPity).id);
            var p = NewZL();
            p.scrap = ZLShop.BoxCost;
            var prize = ZLShop.SpinBox(p, 0.5, ZLShop.BoxCost);
            Assert.IsNotNull(prize);
            Assert.AreEqual(prize.id, p.equipped);
            Assert.IsNotNull(p.Gun(prize.id));
            Assert.IsNull(ZLShop.SpinBox(p, 0.5, ZLShop.BoxCost), "no scrap left");
            Assert.IsTrue(ZLCatalog.StormCoil.wonder);
            Assert.IsNotNull(ZLCatalog.Gun(ZLCatalog.StormCoil.id));
        }

        [Test]
        public void PowerUps_PityAndMaxAmmo()
        {
            Assert.IsTrue(ZLShop.DropsPowerUp(ZLShop.PowerUpPity, 0.99));
            Assert.IsFalse(ZLShop.DropsPowerUp(0, 0.5));
            var seen = new HashSet<ZLPowerUp>();
            for (int i = 0; i < 100; i++) seen.Add(ZLShop.PickPowerUp(i / 100.0));
            Assert.AreEqual(4, seen.Count);
            var p = NewZL();
            p.Gun(ZLCatalog.Pistol.id).reserve = 0;
            ZLShop.MaxAmmo(p);
            Assert.AreEqual(ZLCatalog.Pistol.maxReserve, p.Gun(ZLCatalog.Pistol.id).reserve);
        }

        [Test]
        public void NewGame_StillStartsWith25Rounds_AndClearsTheShop()
        {
            var p = NewZL();
            p.scrap = 999; p.perks.Add("ThickSkin"); p.forged.Add("street_smg");
            ZLRules.NewGame(p, 5);
            Assert.AreEqual(0, p.scrap);
            Assert.AreEqual(0, p.perks.Count);
            Assert.AreEqual(0, p.forged.Count);
            var pistol = p.Gun(ZLCatalog.Pistol.id);
            Assert.AreEqual(ZLCatalog.StartingRounds, pistol.mag + pistol.reserve);
        }

        [Test]
        public void Respawn_FieldKits_GiveMedkitsAndArmor()
        {
            var p = NewZL();
            p.medkits = 0; p.armor = 0;
            ZLRules.Respawn(p, 2, 25);
            Assert.AreEqual(2, p.medkits);
            Assert.AreEqual(25, p.armor);
            var old = new ZombieLandProgress { perks = null, forged = null };
            old.Sanitize();
            Assert.IsNotNull(old.perks);
            Assert.IsNotNull(old.forged);
        }

        // ---- Last Stand ----------------------------------------------------------------------------------

        [Test]
        public void LastStand_RoundsHoundsAndBoss()
        {
            Assert.IsTrue(LastStandRules.IsHoundRound(SurvivalMode.Endless, 5));
            Assert.IsTrue(LastStandRules.IsHoundRound(SurvivalMode.Endless, 15));
            Assert.IsFalse(LastStandRules.IsHoundRound(SurvivalMode.Endless, 10));
            Assert.IsTrue(LastStandRules.IsBossRound(SurvivalMode.Raid, 10));
            Assert.Greater(LastStandRules.ZombiesInRound(8), LastStandRules.ZombiesInRound(2));
            Assert.Greater(LastStandRules.Health(ZLFoe.Shambler, 10, SurvivalMode.Endless), LastStandRules.Health(ZLFoe.Shambler, 1, SurvivalMode.Endless));
            Assert.Greater(LastStandRules.Speed(ZLFoe.Shambler, 10, SurvivalMode.Endless), LastStandRules.Speed(ZLFoe.Shambler, 1, SurvivalMode.Endless));
            Assert.Greater(LastStandRules.BossHealth(10, SurvivalMode.Raid, false), LastStandRules.BossHealth(10, SurvivalMode.Endless, false));
            for (int i = 0; i < 20; i++) Assert.AreEqual(ZLFoe.Shambler, LastStandRules.Pick(1, i / 20.0));
            var seen = new HashSet<ZLFoe>();
            for (int i = 0; i < 200; i++) seen.Add(LastStandRules.Pick(12, i / 200.0));
            Assert.IsTrue(seen.Contains(ZLFoe.Brute) && seen.Contains(ZLFoe.Runner) && seen.Contains(ZLFoe.Spitter));
            Assert.AreEqual(ZLCatalog.Pistol.id, LastStandRules.StarterGun(0).id);
            for (int t = 1; t <= 3; t++) Assert.IsNotNull(LastStandRules.StarterGun(t));
        }

        [Test]
        public void LastStand_DoorsPerksBoxAndTheEgg()
        {
            var ls = new LastStandRun(SurvivalMode.Endless, 0);
            Assert.AreEqual(ZombieRules.StartingPoints, ls.Run.Points);
            Assert.IsFalse(ls.OpenDoor("lot", 750));
            ls.Run.Earn(20000);
            Assert.IsTrue(ls.OpenDoor("lot", 750));
            Assert.IsFalse(ls.OpenDoor("lot", 750), "already open");
            Assert.IsTrue(ls.BuyPerk(ZLPerk.ThickSkin));
            Assert.IsTrue(ls.BuyPerk(ZLPerk.QuickHands));
            Assert.IsTrue(ls.BuyPerk(ZLPerk.SteadyAim));
            Assert.IsTrue(ls.BuyPerk(ZLPerk.FleetFoot));
            Assert.IsFalse(ls.BuyPerk(ZLPerk.SecondChance), "four at a time");
            ls.HiddenBossBeaten = true;
            Assert.IsTrue(ls.BuyPerk(ZLPerk.SecondChance), "the Hollow King opens a fifth");
            ls.FreeSpinLeft = true;
            int before = ls.Run.Points;
            Assert.IsTrue(ls.PayForSpin());
            Assert.AreEqual(before, ls.Run.Points, "the first spin was free");
            Assert.IsTrue(ls.PayForSpin());
            Assert.AreEqual(before - LastStandRules.BoxCost, ls.Run.Points);
            for (int i = 2; i < LastStandRules.BoxMovesAfter; i++) ls.PayForSpin();
            Assert.IsTrue(ls.BoxShouldMove);
            Assert.IsFalse(ls.FindFragment());
            Assert.IsFalse(ls.FindFragment());
            Assert.IsTrue(ls.FindFragment());
            Assert.IsFalse(ls.FindFragment());
            Assert.AreEqual(3000, LastStandRules.ForgePrice(true));
        }
    }
}
