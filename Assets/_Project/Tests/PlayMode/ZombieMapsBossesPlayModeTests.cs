using System.Collections;
using NUnit.Framework;
using PKR.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PKR.Tests
{
    /// <summary>
    /// More maps and bosses: the Rail Yard (Last Stand map 3, its signal levers and the Iron Widow's Singularity drop),
    /// boss phases (enraged at half, the horde at a quarter), the Singularity's pull, and Zombie Land chapter 3: the
    /// freight lift to the sublevel, three keycards, the Curator's vault and the Curator's fall.
    /// </summary>
    public class ZombieMapsBossesPlayModeTests
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
            GameSession.ZombieWeekly = false;
            var save = Services.Save != null ? Services.Save.Data : null;
            if (save != null)
            {
                if (_zlBackup != null) JsonUtility.FromJsonOverwrite(_zlBackup, save.zombieLand);
                if (_careerBackup != null) JsonUtility.FromJsonOverwrite(_careerBackup, save.zombieCareer);
                if (_careerBackup != null) save.starShards = _shardsBackup;
            }
            _zlBackup = _careerBackup = null;
        }

        IEnumerator LoadLastStand(string map)
        {
            if (!Application.CanStreamedLevelBeLoaded(SceneIds.LastStand)) Assert.Ignore("Last Stand isn't in the build.");
            Backup();
            GameSession.Mode = SessionMode.Zombies;
            GameSession.ZombieMode = SurvivalMode.Endless;
            GameSession.ZombieSquad = SquadKind.Solo;
            GameSession.ZombieMap = map;
            GameSession.ZombieWeekly = false;
            SceneManager.LoadScene(SceneIds.LastStand, LoadSceneMode.Single);
            yield return null;
            float t = 0f;
            while ((ZLGame.Current == null || !ZLGame.Current.Ready || !ZLGame.Current.IsLastStand) && t < 10f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsNotNull(ZLGame.Current);
            Assert.IsNotNull(ZLGame.Current.Stand);
            ZLGame.Current.Player.DebugInvulnerable = true;
        }

        IEnumerator WaitForZombieLand()
        {
            yield return null;
            float t = 0f;
            while ((ZLGame.Current == null || !ZLGame.Current.Ready || ZLGame.Current.IsLastStand) && t < 10f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsNotNull(ZLGame.Current);
            Assert.IsTrue(ZLGame.Current.Ready);
            ZLGame.Current.Player.DebugInvulnerable = true;
        }

        IEnumerator LoadZombieLand(System.Action<ZombieLandProgress> setup)
        {
            if (!Application.CanStreamedLevelBeLoaded(SceneIds.ZombieLand)) Assert.Ignore("Zombie Land isn't in the build.");
            Backup();
            var save = Services.Save.Data;
            ZLRules.NewGame(save.zombieLand, 3333);
            save.zombieLand.introSeen = true;
            setup?.Invoke(save.zombieLand);
            GameSession.Mode = SessionMode.Zombies;
            SceneManager.LoadScene(SceneIds.ZombieLand, LoadSceneMode.Single);
            yield return WaitForZombieLand();
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
        public IEnumerator RailYard_Loads_LeversWakeTheIronWidow_SheDropsTheSingularity()
        {
            yield return LoadLastStand(LastStandMap.RailYard.Id);
            var g = ZLGame.Current;
            var st = g.Stand;
            Assert.AreEqual(LastStandMap.RailYard.Id, st.Map.Id);
            Assert.AreEqual(64, g.City.w);
            Assert.AreEqual(48, g.City.h);
            Assert.IsNotNull(g.Minimap, "the rail minimap");
            Assert.IsTrue(g.City.Walkable(g.Player.transform.position), "the hero starts on open ground");
            var levers = Object.FindObjectsByType<ZLFragment>(FindObjectsSortMode.None);
            Assert.AreEqual(LastStandRules.Fragments, levers.Length, "three signal levers");
            foreach (var l in levers) Assert.AreEqual("THROW THE SIGNAL LEVER", l.Prompt);

            st.DebugFindAllFragments();
            Assert.IsTrue(st.Ls.HiddenBossSummoned, "the levers wake the Iron Widow");
            st.DebugSkipToRound(10);
            float t = 0f;
            while (st.HollowKing == null && t < 20f) { t += Time.deltaTime; g.Player.DebugInvulnerable = true; yield return null; }
            Assert.IsNotNull(st.HollowKing, "the Iron Widow comes");
            st.HollowKing.Hit(999999, Vector2.up, null);
            yield return null;
            Assert.IsTrue(st.Ls.HiddenBossBeaten);
            Assert.IsTrue(Services.Save.Data.zombieCareer.Has(LastStandMap.RailYard.HiddenSecret));
            Assert.IsNotNull(g.Player.Loadout.Gun(ZLCatalog.Singularity.id), "she drops the Singularity for the squad");
            yield return Calm();
        }

        [UnityTest]
        public IEnumerator Gravemoor_Loads_LanternsWakeTheBriarQueen_SheDropsTheKindler()
        {
            yield return LoadLastStand(LastStandMap.Gravemoor.Id);
            var g = ZLGame.Current;
            var st = g.Stand;
            Assert.AreEqual(LastStandMap.Gravemoor.Id, st.Map.Id);
            Assert.AreEqual(64, g.City.w);
            Assert.AreEqual(52, g.City.h);
            Assert.IsNotNull(g.Minimap, "the Gravemoor minimap");
            Assert.IsTrue(g.City.Walkable(g.Player.transform.position), "the hero starts on open ground");
            var lanterns = Object.FindObjectsByType<ZLFragment>(FindObjectsSortMode.None);
            Assert.AreEqual(LastStandRules.Fragments, lanterns.Length, "three mourning lanterns");
            foreach (var l in lanterns) Assert.AreEqual("LIGHT THE MOURNING LANTERN", l.Prompt);
            float mt = 0f;
            while (g.MusicNow == null && mt < 2f) { mt += Time.unscaledDeltaTime; yield return null; }
            Assert.AreEqual(Sfx.MusicGardens, g.MusicNow, "Gravemoor has its own music");
            Assert.IsNotNull(Sfx.Clip(Sfx.MusicGardens), "the music file is in Resources/Audio");

            st.DebugFindAllFragments();
            Assert.IsTrue(st.Ls.HiddenBossSummoned, "the lanterns wake the Briar Queen");
            st.DebugSkipToRound(10);
            float t = 0f;
            while (st.HollowKing == null && t < 20f) { t += Time.deltaTime; g.Player.DebugInvulnerable = true; yield return null; }
            Assert.IsNotNull(st.HollowKing, "the Briar Queen comes");
            mt = 0f;
            while (g.MusicNow != Sfx.MusicZombieBoss && mt < 2f) { mt += Time.unscaledDeltaTime; yield return null; }
            Assert.AreEqual(Sfx.MusicZombieBoss, g.MusicNow, "boss music while she's up");
            st.HollowKing.Hit(999999, Vector2.up, null);
            yield return null;
            Assert.IsTrue(st.Ls.HiddenBossBeaten);
            Assert.IsTrue(Services.Save.Data.zombieCareer.Has(LastStandMap.Gravemoor.HiddenSecret));
            Assert.IsNotNull(g.Player.Loadout.Gun(ZLCatalog.Kindler.id), "she drops the Kindler for the squad");
            yield return Calm();
        }

        [UnityTest]
        public IEnumerator Bosses_EnrageAtHalf_AndCallTheHordeAtAQuarter()
        {
            yield return LoadLastStand(LastStandRules.MapId);
            var g = ZLGame.Current;
            var st = g.Stand;
            st.DebugSkipToRound(10);
            float t = 0f;
            while (st.Butcher == null && t < 20f) { t += Time.deltaTime; g.Player.DebugInvulnerable = true; yield return null; }
            var b = st.Butcher;
            Assert.IsNotNull(b, "the Butcher leads round 10");
            yield return null;
            float speed = b.speed;
            Assert.AreEqual(1, st.BossPhaseOf(b));
            b.hp = Mathf.FloorToInt(b.maxHp * 0.45f);
            yield return null;
            yield return null;
            Assert.AreEqual(2, st.BossPhaseOf(b), "enraged at half health");
            Assert.Greater(b.speed, speed, "and faster");
            int before = st.Alive.Count;
            b.hp = Mathf.FloorToInt(b.maxHp * 0.2f);
            yield return null;
            yield return null;
            Assert.AreEqual(3, st.BossPhaseOf(b), "the horde at a quarter");
            Assert.GreaterOrEqual(st.Alive.Count, before, "it calls more of the dead");
            yield return Calm();
        }

        [UnityTest]
        public IEnumerator Singularity_PullsTheDeadIn_ThenBursts()
        {
            yield return LoadLastStand(LastStandRules.MapId);
            var g = ZLGame.Current;
            Vector2 me = g.Player.transform.position;
            Vector2 orb = me + new Vector2(0f, 1f);
            var z = g.SpawnZombie(ZLFoe.Shambler, orb + new Vector2(2.5f, 0f), 1, false);
            z.maxHp = z.hp = 100000;
            z.speed = 0f;
            float d0 = Vector2.Distance(z.transform.position, orb);
            g.Singularity(orb, 40, g.Player);
            yield return new WaitForSeconds(ZLCatalog.PullSeconds * 0.6f);
            Assert.Less(Vector2.Distance(z.transform.position, orb), d0, "dragged toward the orb");
            yield return new WaitForSeconds(ZLCatalog.PullSeconds);
            Assert.Less(z.hp, 100000, "then the burst hurts it");
            Assert.IsTrue(ZLCatalog.Singularity.bossDrop);
            Assert.IsFalse(ZLShop.BoxPool().Exists(x => x.gun == ZLCatalog.Singularity), "only bosses give it");
            yield return Calm();
        }

        [UnityTest]
        public IEnumerator Chapter3_LiftToTheSublevel_KeycardsOpenTheVault_TheCuratorEndsIt()
        {
            // a save that has just cleared Blackwater
            yield return LoadZombieLand(p => { p.bossDefeated = true; p.chapter2Begun = true; p.chapter2Done = true; });
            var g = ZLGame.Current;
            var p = g.Progress;
            Assert.IsTrue(p.chapter3Begun, "chapter 3 starts");
            Assert.IsTrue(p.InChapter3);
            Assert.IsFalse(p.InLabs);
            Assert.IsTrue(p.leads.Contains("c3_power"));
            ZLLift lift = null;
            foreach (var l in Object.FindObjectsByType<ZLLift>(FindObjectsSortMode.None)) if (l.Which == ZLLift.Kind.LiftDown) lift = l;
            Assert.IsNotNull(lift, "the freight lift under the lab");
            Assert.IsTrue(lift.CanUse);

            // ride it down: Zombie Land reloads on the sublevel map
            var old = g;
            lift.Use(g.Player);
            Assert.IsTrue(p.InLabs);
            float w = 0f;
            while ((ZLGame.Current == old || ZLGame.Current == null) && w < 10f) { w += Time.unscaledDeltaTime; yield return null; }
            Assert.AreNotSame(old, ZLGame.Current, "the scene reloaded");
            yield return WaitForZombieLand();
            g = ZLGame.Current;
            p = g.Progress;
            Assert.IsTrue(p.InLabs, "still in the sublevel after the reload");
            Assert.AreEqual(72, g.City.w);
            Assert.AreEqual(52, g.City.h);
            Assert.AreEqual("", p.arriveAt, "arrived");
            Assert.Less(Vector2.Distance(g.Player.transform.position, g.City.SiteOf("lift")), 4f, "you step out of the lift");
            Assert.AreEqual("safe_labs", p.checkpoint);
            Assert.IsNotNull(g.Minimap, "the sublevel minimap");
            Assert.IsNotNull(g.DebugStartMission("c3_power"), "a sublevel mission has a marker");
            g.AbandonMission();

            // the vault is sealed without the keycards
            var vault = g.City.lairs.Find(l => l.id == ZLCatalog.Vault);
            Assert.IsNotNull(vault);
            Vector2 vp = new Vector2(vault.x, vault.y);
            g.TryLair(ZLCatalog.Vault, vp);
            Assert.IsNull(g.Boss);

            foreach (var m in ZLCatalog.Chapter3Missions) if (m.required) ZLRules.Complete(p, m.id);
            Assert.AreEqual(ZLCatalog.KeycardsNeeded, p.Keycards);
            g.Player.transform.position = vp;
            g.Player.Body.position = vp;
            g.TryLair(ZLCatalog.Vault, vp);
            Assert.IsNotNull(g.Boss, "the Curator wakes");
            Assert.IsTrue(g.Boss.Curator);
            Assert.AreEqual(ZLCatalog.Chapter3BossName, g.Boss.BossName);
            Assert.AreEqual(ZLRules.Chapter3BossHealth, g.Boss.maxHp);
            int hp = g.Boss.hp;
            g.Boss.Hit(30, Vector2.up, ZLCatalog.Pistol);
            Assert.AreEqual(hp - 30, g.Boss.hp, "any gun hurts it");
            int shards = Services.Save.Data.starShards;
            g.Boss.Hit(999999, Vector2.up, ZLCatalog.Pistol);
            yield return null;
            Assert.IsTrue(p.chapter3Done);
            Assert.IsFalse(p.InChapter3);
            Assert.GreaterOrEqual(Services.Save.Data.starShards, shards + ZLRules.Chapter3Reward);
            Assert.IsNotNull(p.Gun(ZLCatalog.Singularity.id), "the Curator's Singularity is yours");
            yield return Calm();
        }

        [UnityTest]
        public IEnumerator Chapter4_RoadToColdHarbor_ClearancesOpenTheHangar_TheMarshalEndsIt()
        {
            // a save that has just sealed the sublevel
            yield return LoadZombieLand(p => { p.bossDefeated = p.chapter2Begun = p.chapter2Done = p.chapter3Begun = p.chapter3Done = true; });
            var g = ZLGame.Current;
            var p = g.Progress;
            Assert.IsTrue(p.chapter4Begun, "chapter 4 starts");
            Assert.IsTrue(p.InChapter4);
            Assert.IsTrue(p.leads.Contains("c4_lights"));
            ZLLift road = null;
            foreach (var l in Object.FindObjectsByType<ZLLift>(FindObjectsSortMode.None)) if (l.Which == ZLLift.Kind.RoadOut) road = l;
            Assert.IsNotNull(road, "the service road by the rail depot");
            Assert.IsTrue(road.CanUse);

            var old = g;
            road.Use(g.Player);
            Assert.IsTrue(p.InAirfield);
            float w = 0f;
            while ((ZLGame.Current == old || ZLGame.Current == null) && w < 10f) { w += Time.unscaledDeltaTime; yield return null; }
            Assert.AreNotSame(old, ZLGame.Current, "the scene reloaded");
            yield return WaitForZombieLand();
            g = ZLGame.Current;
            p = g.Progress;
            Assert.IsTrue(p.InAirfield, "still at the airfield after the reload");
            Assert.AreEqual(76, g.City.w);
            Assert.AreEqual(56, g.City.h);
            Assert.Less(Vector2.Distance(g.Player.transform.position, g.City.SiteOf("road")), 4f, "you arrive at the end of the road");
            Assert.AreEqual("safe_air", p.checkpoint);
            Assert.IsNotNull(g.Minimap, "the airfield minimap");
            Assert.IsNotNull(g.DebugStartMission("c4_lights"), "an airfield mission has a marker");
            g.AbandonMission();

            var hangar = g.City.lairs.Find(l => l.id == ZLCatalog.Hangar);
            Assert.IsNotNull(hangar);
            Vector2 hp0 = new Vector2(hangar.x, hangar.y);
            g.TryLair(ZLCatalog.Hangar, hp0);
            Assert.IsNull(g.Boss, "locked down without the clearances");

            foreach (var m in ZLCatalog.Chapter4Missions) if (m.required) ZLRules.Complete(p, m.id);
            Assert.AreEqual(ZLCatalog.ClearancesNeeded, p.Clearances);
            g.Player.transform.position = hp0;
            g.Player.Body.position = hp0;
            g.TryLair(ZLCatalog.Hangar, hp0);
            Assert.IsNotNull(g.Boss, "the Marshal comes out");
            Assert.IsTrue(g.Boss.Marshal);
            Assert.AreEqual(ZLCatalog.Chapter4BossName, g.Boss.BossName);
            Assert.AreEqual(ZLRules.Chapter4BossHealth, g.Boss.maxHp);
            int hp = g.Boss.hp;
            g.Boss.Hit(30, Vector2.up, ZLCatalog.Pistol);
            Assert.AreEqual(hp - 30, g.Boss.hp, "any gun hurts him");
            int shards = Services.Save.Data.starShards;
            g.Boss.Hit(999999, Vector2.up, ZLCatalog.Pistol);
            yield return null;
            Assert.IsTrue(p.chapter4Done);
            Assert.IsFalse(p.InChapter4);
            Assert.GreaterOrEqual(Services.Save.Data.starShards, shards + ZLRules.Chapter4Reward);
            Assert.IsNotNull(p.Gun(ZLCatalog.Kindler.id), "the Marshal's Kindler is yours");
            yield return Calm();
        }
    }
}
