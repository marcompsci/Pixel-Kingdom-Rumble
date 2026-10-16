using System.Collections;
using NUnit.Framework;
using PKR.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PKR.Tests
{
    /// <summary>
    /// The new dead (riot shields, leapers), the Rime Cannon, and Zombie Land chapter 2: the causeway opens after the
    /// First Turned, four sigils open the tide gate, Mother Brine can be hurt by anything and her fall ends the chapter.
    /// </summary>
    public class ZombieContentPlayModeTests
    {
        string _zlBackup, _careerBackup;
        int _shardsBackup;

        void Backup()
        {
            var save = Services.Save != null ? Services.Save.Data : null;
            if (save == null) return;
            if (save.zombieCareer == null) save.zombieCareer = new ZombieCareer();
            _zlBackup = JsonUtility.ToJson(save.zombieLand);
            _careerBackup = JsonUtility.ToJson(save.zombieCareer);
            _shardsBackup = save.starShards;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            GameSession.Mode = SessionMode.StoryQuest;
            GameSession.ZombieSquad = SquadKind.Solo;
            GameSession.ZombieMap = LastStandRules.MapId;
            var save = Services.Save != null ? Services.Save.Data : null;
            if (save != null)
            {
                if (_zlBackup != null) JsonUtility.FromJsonOverwrite(_zlBackup, save.zombieLand);
                if (_careerBackup != null) JsonUtility.FromJsonOverwrite(_careerBackup, save.zombieCareer);
                if (_careerBackup != null) save.starShards = _shardsBackup;
            }
            _zlBackup = _careerBackup = null;
        }

        IEnumerator LoadLastStand()
        {
            if (!Application.CanStreamedLevelBeLoaded(SceneIds.LastStand)) Assert.Ignore("Last Stand isn't in the build.");
            Backup();
            GameSession.Mode = SessionMode.Zombies;
            GameSession.ZombieMode = SurvivalMode.Endless;
            GameSession.ZombieSquad = SquadKind.Solo;
            GameSession.ZombieMap = LastStandRules.MapId;
            GameSession.ZombieWeekly = false;
            SceneManager.LoadScene(SceneIds.LastStand, LoadSceneMode.Single);
            yield return null;
            float t = 0f;
            while ((ZLGame.Current == null || !ZLGame.Current.Ready || !ZLGame.Current.IsLastStand) && t < 10f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsNotNull(ZLGame.Current);
            ZLGame.Current.Player.DebugInvulnerable = true;
        }

        IEnumerator LoadZombieLand(System.Action<ZombieLandProgress> setup)
        {
            if (!Application.CanStreamedLevelBeLoaded(SceneIds.ZombieLand)) Assert.Ignore("Zombie Land isn't in the build.");
            Backup();
            var save = Services.Save.Data;
            ZLRules.NewGame(save.zombieLand, 2222);
            save.zombieLand.introSeen = true;
            setup?.Invoke(save.zombieLand);
            GameSession.Mode = SessionMode.Zombies;
            SceneManager.LoadScene(SceneIds.ZombieLand, LoadSceneMode.Single);
            yield return null;
            float t = 0f;
            while ((ZLGame.Current == null || !ZLGame.Current.Ready || ZLGame.Current.IsLastStand) && t < 10f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsNotNull(ZLGame.Current);
            Assert.IsTrue(ZLGame.Current.Ready);
            ZLGame.Current.Player.DebugInvulnerable = true;
        }

        IEnumerator Calm()
        {
            if (Application.CanStreamedLevelBeLoaded(SceneIds.StoryTest))
            {
                SceneManager.LoadScene(SceneIds.StoryTest, LoadSceneMode.Single);
                yield return null;
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator RiotShield_StopsBulletsFromTheFront_NotFromBehind()
        {
            yield return LoadLastStand();
            var g = ZLGame.Current;
            Vector2 me = g.Player.transform.position;
            var z = g.SpawnZombie(ZLFoe.Riot, me + Vector2.right * 5f, 1, true);
            z.maxHp = z.hp = 1000;
            Assert.IsNotNull(z.transform.Find("Shield"), "riot zombies carry a shield");
            for (int i = 0; i < 20; i++) yield return new WaitForFixedUpdate();
            Assert.Less(z.Facing.x, -0.5f, "it faces the hero (to its left)");
            Vector2 at = z.transform.position;
            // from the front: a bullet from the hero's side travelling right
            ZLBullet.Fire(at + Vector2.left * 1.2f, Vector2.right, ZLCatalog.Pistol, 20, false, null, Color.white, null, null, 30f, 3f, g.Player);
            for (int i = 0; i < 6; i++) yield return null;
            int front = 1000 - z.hp;
            int before = z.hp;
            // from behind: travelling left into its back
            at = z.transform.position;
            ZLBullet.Fire(at + Vector2.right * 1.2f, Vector2.left, ZLCatalog.Pistol, 20, false, null, Color.white, null, null, 30f, 3f, g.Player);
            for (int i = 0; i < 6; i++) yield return null;
            int back = before - z.hp;
            Assert.Greater(front, 0, "the shot hit");
            Assert.Less(front, back, "the shield takes most of a front hit");
            Assert.AreEqual(20, back, "full damage from behind");
            yield return Calm();
        }

        [UnityTest]
        public IEnumerator Leaper_CrouchesThenSprings()
        {
            yield return LoadLastStand();
            var g = ZLGame.Current;
            Vector2 me = g.Player.transform.position;
            var z = g.SpawnZombie(ZLFoe.Leaper, me + Vector2.right * 5f, 1, true);
            z.maxHp = z.hp = 1000;
            bool crouched = false, leapt = false;
            float t = 0f;
            while (!(crouched && leapt) && t < 6f)
            {
                crouched |= z.Crouching;
                leapt |= z.Leaping;
                t += Time.deltaTime;
                yield return null;
            }
            Assert.IsTrue(crouched, "a tell first");
            Assert.IsTrue(leapt, "then the leap");
            yield return Calm();
        }

        [UnityTest]
        public IEnumerator RimeCannon_FreezesAGroup_AndFrozenZombiesTakeMore()
        {
            yield return LoadLastStand();
            var g = ZLGame.Current;
            Vector2 me = g.Player.transform.position;
            var a = g.SpawnZombie(ZLFoe.Shambler, me + new Vector2(5f, 0f), 1, false);
            var b = g.SpawnZombie(ZLFoe.Shambler, me + new Vector2(5.6f, 0.8f), 1, false);
            a.maxHp = a.hp = b.maxHp = b.hp = 1000;
            a.Hit(10, Vector2.right, ZLCatalog.RimeCannon);
            Assert.IsTrue(a.Frozen);
            Assert.IsTrue(b.Frozen, "the frost bursts over its neighbour");
            int hp = b.hp;
            b.Hit(10, Vector2.right, ZLCatalog.Pistol);
            Assert.AreEqual(hp - ZLRules.FrozenDamage(10), b.hp);
            yield return null;
            yield return Calm();
        }

        [UnityTest]
        public IEnumerator Chapter2_CausewayOpens_SigilsOpenTheTideGate_MotherBrineEndsIt()
        {
            // a save that has just beaten the First Turned
            yield return LoadZombieLand(p => { p.bossDefeated = true; });
            var g = ZLGame.Current;
            var p = g.Progress;
            Assert.IsTrue(p.chapter2Begun, "chapter 2 starts");
            Assert.IsTrue(p.InChapter2);
            Assert.IsTrue(p.leads.Contains("c2_ferry"));
            var bridge = g.City.doors.Find(d => d.id == ZLCatalog.IsleBridge);
            Assert.IsNotNull(bridge, "city.json has the causeway");
            Assert.IsFalse(g.City.Blocked(bridge.x, bridge.y), "the quarantine line is open");
            Assert.AreEqual("isle", g.City.DistrictAt(new Vector2(220f, 120f)));
            Assert.AreEqual(5, g.ThreatAt(new Vector2(220f, 120f)));

            // the tide gate is sealed without the sigils
            var gate = g.City.lairs.Find(l => l.id == ZLCatalog.TideGate);
            Assert.IsNotNull(gate);
            Vector2 gp = new Vector2(gate.x, gate.y);
            g.TryLair(ZLCatalog.TideGate, gp);
            Assert.IsNull(g.Boss);

            // a Blackwater mission runs on the isle
            var run = g.DebugStartMission("c2_cellblock");
            Assert.IsNotNull(run, "the cell block mission has a marker");
            g.AbandonMission();

            foreach (var m in ZLCatalog.Chapter2Missions) if (m.required) ZLRules.Complete(p, m.id);
            Assert.AreEqual(ZLCatalog.SigilsNeeded, p.Sigils);
            g.Player.transform.position = gp;
            g.Player.Body.position = gp;
            g.TryLair(ZLCatalog.TideGate, gp);
            Assert.IsNotNull(g.Boss, "Mother Brine rises");
            Assert.IsTrue(g.Boss.Brine);
            Assert.AreEqual(ZLCatalog.Chapter2BossName, g.Boss.BossName);
            int hp = g.Boss.hp;
            g.Boss.Hit(30, Vector2.up, ZLCatalog.Pistol);
            Assert.AreEqual(hp - 30, g.Boss.hp, "any gun hurts her");
            int shards = Services.Save.Data.starShards;
            g.Boss.Hit(999999, Vector2.up, ZLCatalog.Pistol);
            yield return null;
            Assert.IsTrue(p.chapter2Done);
            Assert.IsFalse(p.InChapter2);
            Assert.GreaterOrEqual(Services.Save.Data.starShards, shards + ZLRules.Chapter2Reward, "the reward (plus any Survivor Rank shards)");
            yield return Calm();
        }

        [UnityTest]
        public IEnumerator Chapter1_TheCausewayIsSealed()
        {
            yield return LoadZombieLand(null);
            var g = ZLGame.Current;
            Assert.IsFalse(g.Progress.chapter2Begun);
            var bridge = g.City.doors.Find(d => d.id == ZLCatalog.IsleBridge);
            Assert.IsNotNull(bridge);
            Assert.IsTrue(g.City.Blocked(bridge.x, bridge.y), "sealed until the First Turned falls");
            Assert.IsNotNull(GameObject.Find("QuarantineLine"));
            yield return Calm();
        }
    }
}
