using System;
using System.Collections;
using NUnit.Framework;
using PKR.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace PKR.Tests
{
    /// <summary>The third round-based map (Salt Pier), the weekly challenge with its mutators, and feel fixes.</summary>
    public class ZombiesNextPlayModeTests
    {
        string _careerBackup;
        int _shardsBackup;

        void Backup()
        {
            var save = Services.Save != null ? Services.Save.Data : null;
            if (save == null) return;
            if (save.zombieCareer == null) save.zombieCareer = new ZombieCareer();
            _careerBackup = JsonUtility.ToJson(save.zombieCareer);
            _shardsBackup = save.starShards;
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new ZombieCareer()), save.zombieCareer);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            GameSession.Mode = SessionMode.StoryQuest;
            GameSession.ZombieMode = SurvivalMode.Endless;
            GameSession.ZombieSquad = SquadKind.Solo;
            GameSession.ZombieMap = LastStandRules.MapId;
            GameSession.ZombieWeekly = false;
            var save = Services.Save != null ? Services.Save.Data : null;
            if (save != null && _careerBackup != null)
            {
                JsonUtility.FromJsonOverwrite(_careerBackup, save.zombieCareer);
                save.starShards = _shardsBackup;
            }
            _careerBackup = null;
        }

        IEnumerator Load(string map, bool weekly)
        {
            if (!Application.CanStreamedLevelBeLoaded(SceneIds.LastStand)) Assert.Ignore("Last Stand isn't in the build.");
            Backup();
            GameSession.Mode = SessionMode.Zombies;
            GameSession.ZombieMode = SurvivalMode.Endless;
            GameSession.ZombieSquad = SquadKind.Solo;
            GameSession.ZombieMap = map;
            GameSession.ZombieWeekly = weekly;
            SceneManager.LoadScene(SceneIds.LastStand, LoadSceneMode.Single);
            yield return null;
            float t = 0f;
            while ((ZLGame.Current == null || !ZLGame.Current.Ready || !ZLGame.Current.IsLastStand) && t < 10f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsNotNull(ZLGame.Current);
            Assert.IsNotNull(ZLGame.Current.Stand);
            foreach (var h in ZLGame.Current.Squad) h.DebugInvulnerable = true;
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
        public IEnumerator SaltPier_Loads_RoundsDoorsAndTheBells()
        {
            yield return Load("salt_pier", false);
            var g = ZLGame.Current;
            var st = g.Stand;
            Assert.IsTrue(ReferenceEquals(LastStandMap.SaltPier, st.Map));
            Assert.AreEqual(60, g.City.w);
            Assert.AreEqual(52, g.City.h);
            Assert.AreEqual(4, g.City.doors.Count);
            Assert.IsNotNull(g.Minimap, "minimap_pier");
            Assert.IsTrue(st.Mut.Count == 0 && st.Weekly == null);

            // a round goes by
            float t = 0f;
            while (st.Run.Round < 1 && t < 10f) { t += Time.deltaTime; yield return null; }
            Assert.AreEqual(1, st.Run.Round);
            t = 0f;
            while (st.Alive.Count == 0 && t < 10f) { t += Time.deltaTime; yield return null; }
            Assert.Greater(st.Alive.Count, 0, "the dead climb out of the water");
            st.DebugClearRound();
            t = 0f;
            while (!st.Intermission && t < 5f) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(st.Intermission);

            // the container yard gate
            st.DebugGivePoints(5000);
            var door = Object.FindObjectsByType<ZLDebrisDoor>(FindObjectsSortMode.None);
            ZLDebrisDoor yard = null;
            foreach (var d in door) if (d.Data.id == "yard_gate") yard = d;
            Assert.IsNotNull(yard);
            Assert.AreEqual(750, yard.Cost);
            var at = yard.UsePos;
            Assert.IsTrue(g.City.Blocked(Mathf.FloorToInt(at.x), Mathf.FloorToInt(at.y)));
            st.OpenDoor(yard);
            yield return null;
            Assert.IsTrue(st.HasZone("yard"));
            Assert.IsFalse(g.City.Blocked(Mathf.FloorToInt(at.x), Mathf.FloorToInt(at.y)));

            // three brass bells wake the Tide Mother
            st.DebugFindAllFragments();
            Assert.IsTrue(st.Ls.HiddenBossSummoned);
            yield return Calm();
        }

        [UnityTest]
        public IEnumerator Weekly_UsesThisWeeksMapAndMutators_AndPaysOnceAtTheGoal()
        {
            var w = ZombieWeekly.Current(DateTime.UtcNow);
            GameSession.ZombieMode = w.Mode;
            yield return Load(w.MapId, true);
            var g = ZLGame.Current;
            var st = g.Stand;
            Assert.IsNotNull(st.Weekly);
            Assert.AreEqual(w.Key, st.Weekly.Key);
            Assert.AreEqual(w.Mode, st.Mode);
            Assert.AreEqual(w.MapId, st.Map.Id);
            Assert.AreEqual(2, st.Mut.Count);
            Assert.AreEqual(st.Mut.BoxCost(LastStandRules.BoxCost), st.BoxCost);
            Assert.AreEqual(st.Mut.HeroMaxHealth(ZLShop.MaxHealth(g.Progress)), g.Player.MaxHealth, "Glass Cannon halves your health (or nothing does)");
            float t = 0f;
            while (st.Alive.Count == 0 && t < 15f) { t += Time.deltaTime; yield return null; }
            Assert.Greater(st.Alive.Count, 0);
            var z = st.Alive[0];
            int normal = LastStandRules.Health(z.kind, st.Run.Round, st.Mode);
            Assert.AreEqual(st.Mut.Health(normal), z.maxHp, "mutators change zombie health");

            int shards = Services.Save.Data.starShards;
            st.DebugSkipToRound(w.GoalRound);
            t = 0f;
            while (st.Run.Round < w.GoalRound && t < 5f) { t += Time.deltaTime; yield return null; }
            Assert.AreEqual(w.GoalRound, st.Run.Round);
            st.DebugClearRound();
            t = 0f;
            while (!st.Intermission && t < 5f) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(st.WeeklyPaid, "reaching the goal pays");
            Assert.IsTrue(ZombieWeekly.Done(Services.Save.Data.zombieCareer, w));
            Assert.GreaterOrEqual(Services.Save.Data.starShards, shards + w.RewardShards, "the reward (plus any Survivor Rank shards the XP brings)");
            Assert.AreEqual(0, Services.Save.Data.zombieCareer.Best(w.MapId, w.Mode), "weekly runs don't touch the map's records");
            yield return Calm();
        }

        [UnityTest]
        public IEnumerator HurtFlash_ReturnsToTheZombiesOwnColour()
        {
            yield return Load(LastStandRules.MapId, false);
            var g = ZLGame.Current;
            var z = g.SpawnZombie(ZLFoe.Brute, g.TargetPos + Vector2.right * 6f, 1, true);
            z.maxHp = z.hp = 9999;
            var tint = new Color(0.3f, 0.9f, 0.4f);
            z.SetTint(tint);
            z.Hit(5, Vector2.right, null);
            yield return null;
            var sr = z.GetComponentInChildren<SpriteRenderer>();
            Assert.AreNotEqual(tint, sr.color, "white pop on the hit");
            float t = 0f;
            while (t < 0.4f) { t += Time.deltaTime; yield return null; }
            Assert.AreEqual(tint.r, sr.color.r, 0.01f);
            Assert.AreEqual(tint.g, sr.color.g, 0.01f);
            Assert.AreEqual(tint.b, sr.color.b, 0.01f);
            yield return Calm();
        }
    }
}
