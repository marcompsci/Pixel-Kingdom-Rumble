using System.Collections;
using NUnit.Framework;
using PKR.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PKR.Tests
{
    /// <summary>
    /// The "zombies lead the game" phase in real scenes: Last Stand rounds, doors, perks, guns, hound and boss rounds,
    /// the Easter egg and a Raid win; Zombie Land scrap, power-ups and the safehouse shop; the Manor's Raid win.
    /// The save's zombie career and Zombie Land progress are backed up and restored.
    /// </summary>
    public class ZombiesLeadPlayModeTests
    {
        string _careerBackup, _zlBackup;
        int _shardsBackup;

        void Backup()
        {
            var save = Services.Save != null ? Services.Save.Data : null;
            if (save == null) return;
            if (save.zombieCareer == null) save.zombieCareer = new ZombieCareer();
            _careerBackup = JsonUtility.ToJson(save.zombieCareer);
            _zlBackup = JsonUtility.ToJson(save.zombieLand);
            _shardsBackup = save.starShards;
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new ZombieCareer()), save.zombieCareer);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            GameSession.Mode = SessionMode.StoryQuest;
            GameSession.ZombieMode = SurvivalMode.Endless;
            var save = Services.Save != null ? Services.Save.Data : null;
            if (save != null && _careerBackup != null)
            {
                JsonUtility.FromJsonOverwrite(_careerBackup, save.zombieCareer);
                JsonUtility.FromJsonOverwrite(_zlBackup, save.zombieLand);
                save.starShards = _shardsBackup;
            }
            _careerBackup = _zlBackup = null;
        }

        IEnumerator LoadLastStand(SurvivalMode mode)
        {
            if (!Application.CanStreamedLevelBeLoaded(SceneIds.LastStand)) Assert.Ignore("Last Stand isn't in the build.");
            Backup();
            GameSession.Mode = SessionMode.Zombies;
            GameSession.ZombieMode = mode;
            SceneManager.LoadScene(SceneIds.LastStand, LoadSceneMode.Single);
            yield return null;   // the old scene (maybe Zombie Land) goes away first
            float t = 0f;
            while ((ZLGame.Current == null || !ZLGame.Current.Ready || !ZLGame.Current.IsLastStand) && t < 10f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsNotNull(ZLGame.Current);
            Assert.IsTrue(ZLGame.Current.IsLastStand);
            Assert.IsNotNull(ZLGame.Current.Stand, "the Last Stand controller exists");
        }

        IEnumerator Calm()
        {
            // Leave a quiet scene for the next fixture.
            if (Application.CanStreamedLevelBeLoaded(SceneIds.StoryTest))
            {
                SceneManager.LoadScene(SceneIds.StoryTest, LoadSceneMode.Single);
                yield return null;
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator LastStand_RoundsStart_TheDeadCome_ClearingARoundStartsTheNext()
        {
            yield return LoadLastStand(SurvivalMode.Endless);
            var g = ZLGame.Current;
            var st = g.Stand;
            Assert.AreEqual(60, g.City.w);
            Assert.Greater(g.City.doors.Count, 2);
            Assert.AreEqual(ZombieRules.StartingPoints, st.Run.Points);
            var pistol = g.Progress.Gun(ZLCatalog.Pistol.id);
            Assert.AreEqual(LastStandRules.PistolRounds, pistol.mag + pistol.reserve);
            g.Player.DebugInvulnerable = true;
            float t = 0f;
            while (st.Run.Round < 1 && t < 8f) { t += Time.deltaTime; yield return null; }
            Assert.AreEqual(1, st.Run.Round);
            t = 0f;
            while (st.Alive.Count == 0 && t < 8f) { t += Time.deltaTime; yield return null; }
            Assert.Greater(st.Alive.Count, 0, "the dead climb over the fences");
            int before = st.Run.Points;
            st.DebugClearRound();
            yield return null;
            Assert.Greater(st.Run.Points, before, "kills pay points");
            t = 0f;
            while (st.Run.Round < 2 && t < 12f) { t += Time.deltaTime; yield return null; }
            Assert.AreEqual(2, st.Run.Round, "the next round begins");
            Assert.AreEqual(0, Services.Save.Data.zombieLand.scrap, "Last Stand never touches Zombie Land's save");
            yield return Calm();
        }

        [UnityTest]
        public IEnumerator LastStand_DoorsPerksGunsAndTheForge()
        {
            yield return LoadLastStand(SurvivalMode.Endless);
            var g = ZLGame.Current;
            var st = g.Stand;
            st.DebugGivePoints(30000);
            var doors = Object.FindObjectsByType<ZLDebrisDoor>(FindObjectsSortMode.None);
            Assert.Greater(doors.Length, 0);
            var door = doors[0];
            var d = door.Data;
            Assert.IsTrue(g.City.Blocked(d.x, d.y));
            Assert.IsFalse(st.HasZone(d.opens));
            door.Use(g.Player);
            yield return null;
            Assert.IsFalse(g.City.Blocked(d.x, d.y), "the debris is cleared");
            Assert.IsTrue(st.HasZone(d.opens));

            Assert.IsTrue(st.BuyPerk(ZLPerk.ThickSkin));
            Assert.AreEqual(ZLRules.HeroHealth + 50, g.Player.MaxHealth);
            Assert.IsFalse(st.BuyPerk(ZLPerk.ThickSkin), "once");

            var smg = ZLCatalog.Gun("street_smg");
            st.UseWallGun(smg, 1000);
            Assert.AreEqual("street_smg", g.Progress.equipped);
            st.UseForge();
            Assert.IsTrue(ZLShop.IsForged(g.Progress, "street_smg"));
            Assert.AreEqual(smg.damage * 2, g.Player.Gun.damage);
            yield return Calm();
        }

        [UnityTest]
        public IEnumerator LastStand_HoundRound_TheButcher_AndTheHollowKing()
        {
            yield return LoadLastStand(SurvivalMode.Endless);
            var g = ZLGame.Current;
            var st = g.Stand;
            g.Player.DebugInvulnerable = true;
            st.DebugSkipToRound(5);
            yield return null;
            yield return null;
            Assert.AreEqual(5, st.Run.Round);
            Assert.IsTrue(st.HoundRound, "round 5 is a hound round");
            st.DebugClearRound();
            st.DebugFindAllFragments();
            Assert.IsTrue(st.Ls.HiddenBossSummoned, "three fragments wake the Hollow King");
            st.DebugSkipToRound(10);
            float t = 0f;
            while ((st.Butcher == null || st.HollowKing == null) && t < 20f) { t += Time.deltaTime; g.Player.DebugInvulnerable = true; yield return null; }
            Assert.IsNotNull(st.HollowKing, "the Hollow King comes");
            Assert.IsNotNull(st.Butcher, "the Butcher leads round 10");
            st.HollowKing.Hit(999999, Vector2.up, null);
            yield return null;
            Assert.IsTrue(st.Ls.HiddenBossBeaten);
            Assert.IsTrue(Services.Save.Data.zombieCareer.Has(LastStandRules.HiddenBossSecret));
            yield return Calm();
        }

        [UnityTest]
        public IEnumerator LastStand_RaidWin_PaysAndRecords()
        {
            yield return LoadLastStand(SurvivalMode.Raid);
            var g = ZLGame.Current;
            var st = g.Stand;
            g.Player.DebugInvulnerable = true;
            int shards = Services.Save.Data.starShards;
            st.DebugSkipToRound(10);
            float t = 0f;
            while (st.Run.Round < 10 && t < 5f) { t += Time.deltaTime; yield return null; }
            st.DebugClearRound();
            t = 0f;
            while (!st.Over && t < 10f) { t += Time.deltaTime; st.DebugClearRound(); g.Player.DebugInvulnerable = true; yield return null; }
            Assert.IsTrue(st.Over);
            Assert.IsTrue(st.Run.Won, "ten rounds win a Raid");
            Assert.AreEqual(1, Services.Save.Data.zombieCareer.raidWins);
            Assert.AreEqual(10, Services.Save.Data.zombieCareer.Best(LastStandRules.MapId, SurvivalMode.Raid));
            Assert.Greater(Services.Save.Data.starShards, shards + SurvivalModes.RaidWinShards - 1);
            Assert.Greater(Services.Save.Data.zombieCareer.xp, 0, "Survivor XP");
            yield return Calm();
        }

        [UnityTest]
        public IEnumerator ZombieLand_ScrapPowerUpsAndTheSafehouseShop()
        {
            if (!Application.CanStreamedLevelBeLoaded(SceneIds.ZombieLand)) Assert.Ignore("Zombie Land isn't in the build.");
            Backup();
            var save = Services.Save.Data;
            ZLRules.NewGame(save.zombieLand, 4321);
            save.zombieLand.introSeen = true;
            SceneManager.LoadScene(SceneIds.ZombieLand, LoadSceneMode.Single);
            yield return null;
            float t = 0f;
            while ((ZLGame.Current == null || !ZLGame.Current.Ready || ZLGame.Current.IsLastStand) && t < 10f) { t += Time.unscaledDeltaTime; yield return null; }
            var g = ZLGame.Current;
            Assert.IsFalse(g.IsLastStand);
            var p = g.Player;
            p.SetHealth(100);
            var at = (Vector2)p.transform.position + Vector2.right * 3f;
            var z = g.SpawnZombie(ZLFoe.Shambler, at, 1, false);
            z.Hit(9999, Vector2.right, null);
            yield return null;
            Assert.AreEqual(ZLShop.ScrapForKill(ZLFoe.Shambler), g.Progress.scrap, "a kill pays scrap");

            g.ApplyPowerUp(ZLPowerUp.InstaKill, p.transform.position);
            var brute = g.SpawnZombie(ZLFoe.Brute, at, 3, false);
            brute.Hit(1, Vector2.right, null);
            Assert.IsFalse(brute.Alive, "insta-kill drops a brute in one hit");

            var a = g.SpawnZombie(ZLFoe.Shambler, at, 1, false);
            var b = g.SpawnZombie(ZLFoe.Runner, at + Vector2.up, 1, false);
            g.ApplyPowerUp(ZLPowerUp.Nuke, p.transform.position);
            yield return null;
            Assert.IsFalse(a.Alive || b.Alive, "the nuke clears the street");

            g.Progress.scrap = 10000;
            Assert.Greater(g.Safehouses.Count, 0);
            var house = g.Safehouses[0];
            var panel = g.gameObject.AddComponent<ZLShopPanel>();
            panel.Open(house);
            Assert.IsTrue(ZLShopPanel.IsOpen);
            Assert.IsTrue(g.Paused, "the shop pauses the city");
            Assert.IsTrue(ZLShop.BuyPerk(g.Progress, ZLPerk.QuickHands, false));
            panel.Close();
            Assert.IsFalse(ZLShopPanel.IsOpen);
            Assert.AreEqual(1f, Time.timeScale);
            yield return Calm();
        }

        [UnityTest]
        public IEnumerator Manor_Raid_TenRoundsWins()
        {
            if (!Application.CanStreamedLevelBeLoaded(SceneIds.ZombieSurvival)) Assert.Ignore("Zombie Survival isn't in the build.");
            Backup();
            GameSession.Mode = SessionMode.Zombies;
            GameSession.ZombieMode = SurvivalMode.Raid;
            SceneManager.LoadScene(SceneIds.ZombieSurvival, LoadSceneMode.Single);
            yield return null;
            yield return null;
            var game = ZombieSurvival.Current;
            Assert.IsNotNull(game);
            Assert.AreEqual(SurvivalMode.Raid, game.Mode);
            if (game.player.TryGetComponent(out Invulnerability inv)) inv.Grant(120f);
            game.DebugSkipToRound(10);
            float t = 0f;
            while (game.Run.Round < 10 && t < 5f) { t += Time.deltaTime; yield return null; }
            Assert.AreEqual(10, game.Run.Round);
            t = 0f;
            while (!game.GameOver && t < 10f) { t += Time.deltaTime; game.DebugClearRound(); yield return null; }
            Assert.IsTrue(game.GameOver);
            Assert.IsTrue(game.Run.Won, "the tenth round wins the Raid");
            Assert.AreEqual(1, Services.Save.Data.zombieCareer.raidWins);
            yield return Calm();
        }
    }
}
