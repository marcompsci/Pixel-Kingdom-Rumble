using System.Collections;
using NUnit.Framework;
using PKR.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PKR.Tests
{
    /// <summary>Zombie Land in the real scene: the city builds, the dead spawn, vehicles drive, missions complete,
    /// the lair stays sealed and only the true relic hurts the First Turned.</summary>
    public class ZombieLandPlayModeTests
    {
        string _backup;

        IEnumerator Load()
        {
            if (!Application.CanStreamedLevelBeLoaded(SceneIds.ZombieLand)) Assert.Ignore("Zombie Land isn't in the build.");
            var save = Services.Save != null ? Services.Save.Data : null;
            if (save != null)
            {
                _backup = JsonUtility.ToJson(save.zombieLand);
                ZLRules.NewGame(save.zombieLand, 12345);
                save.zombieLand.introSeen = true;
            }
            SceneManager.LoadScene(SceneIds.ZombieLand, LoadSceneMode.Single);
            float t = 0f;
            while ((ZLGame.Current == null || !ZLGame.Current.Ready) && t < 10f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsNotNull(ZLGame.Current, "the game controller exists");
            Assert.IsTrue(ZLGame.Current.Ready, "the city finished building");
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            var save = Services.Save != null ? Services.Save.Data : null;
            if (save != null && _backup != null) JsonUtility.FromJsonOverwrite(_backup, save.zombieLand);
            _backup = null;
        }

        [UnityTest]
        public IEnumerator City_Builds_AndTheDeadSpawn()
        {
            yield return Load();
            var g = ZLGame.Current;
            Assert.Greater(g.City.buildings.Count, 50);
            Assert.AreEqual(14, g.Markers.Count, "every mission has a marker");
            Assert.AreEqual(7, g.Safehouses.Count);
            Assert.Greater(g.Vehicles.Count, 20);
            Assert.IsNotNull(g.Player);
            Assert.AreEqual(25, g.Progress.Gun(ZLCatalog.Pistol.id).mag + g.Progress.Gun(ZLCatalog.Pistol.id).reserve);
            // walk the hero out of the safehouse and give the city a few seconds
            g.Player.transform.position = g.Player.transform.position + new Vector3(0f, 12f, 0f);
            float t = 0f;
            while (g.Zombies.Count == 0 && t < 8f) { t += Time.deltaTime; yield return null; }
            Assert.Greater(g.Zombies.Count, 0, "the dead roam the streets");
        }

        [UnityTest]
        public IEnumerator Vehicles_EnterDriveExit()
        {
            yield return Load();
            var g = ZLGame.Current;
            ZLVehicle car = null;
            foreach (var v in g.Vehicles) if (v.spec.kind == ZLVehicleKind.Car) { car = v; break; }
            Assert.IsNotNull(car);
            car.Use(g.Player);
            Assert.AreEqual(car, g.Player.Driving);
            Vector2 start = car.transform.position;
            for (int i = 0; i < 60; i++) { car.Drive((Vector2)car.transform.right); yield return null; }
            Assert.Greater(Vector2.Distance(start, car.transform.position), 0.5f, "the car moves");
            car.Eject(true);
            Assert.IsNull(g.Player.Driving);
        }

        [UnityTest]
        public IEnumerator Retrieve_Mission_Completes_AndGivesAShardAndAClue()
        {
            yield return Load();
            var g = ZLGame.Current;
            var run = g.DebugStartMission("m_medicine");
            Assert.IsNotNull(run);
            for (int i = 0; i < 3 && g.Active != null; i++)
            {
                g.Player.transform.position = run.Waypoint;
                g.Player.Body.position = run.Waypoint;
                for (int k = 0; k < 5; k++) yield return null;
            }
            // the completion popup pauses the game; close it
            if (CutscenePlayer.IsPlaying) Object.FindAnyObjectByType<CutscenePlayer>()?.Finish();
            Assert.IsNull(g.Active, "mission finished");
            Assert.IsTrue(g.Progress.Done("m_medicine"));
            Assert.AreEqual(1, g.Progress.Shards);
            Assert.IsTrue(g.Puzzle.Clues.ContainsKey("m_medicine"));
        }

        [UnityTest]
        public IEnumerator Lair_Sealed_And_BossOnlyHurtByTheTrueRelic()
        {
            yield return Load();
            var g = ZLGame.Current;
            g.TryLair(g.Puzzle.TrueLair, g.Player.transform.position);
            Assert.IsNull(g.Boss, "sealed without ten shards");
            var boss = g.DebugSpawnBoss((Vector2)g.Player.transform.position + Vector2.up * 6f);
            int hp = boss.hp;
            boss.Hit(50, Vector2.up, ZLCatalog.Pistol);
            Assert.AreEqual(hp, boss.hp, "guns don't hurt him");
            foreach (ZLRelic r in System.Enum.GetValues(typeof(ZLRelic)))
                if (r != g.Puzzle.TrueRelic) boss.Hit(50, Vector2.up, ZLCatalog.RelicGun(r));
            Assert.AreEqual(hp, boss.hp, "the wrong relics don't either");
            boss.Hit(50, Vector2.up, ZLCatalog.RelicGun(g.Puzzle.TrueRelic));
            Assert.AreEqual(hp - 50, boss.hp, "the true relic does");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Hounds_ComeAfterFiveMinutesOutside()
        {
            yield return Load();
            var g = ZLGame.Current;
            g.Player.transform.position = g.Player.transform.position + new Vector3(0f, 14f, 0f);
            g.Player.Body.position = g.Player.transform.position;
            g.DebugSetRoamed(ZLRules.HoundsAfter + 1f);
            float t = 0f;
            bool hound = false;
            while (!hound && t < 3f)
            {
                foreach (var z in g.Zombies) if (z != null && z.kind == ZLFoe.Hound) hound = true;
                t += Time.deltaTime;
                yield return null;
            }
            Assert.IsTrue(hound, "blood hounds spawn");
        }
    }
}
