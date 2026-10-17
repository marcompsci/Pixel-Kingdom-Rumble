using System.Collections;
using NUnit.Framework;
using PKR.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PKR.Tests
{
    /// <summary>Splitters fall into halves, burrowers can't be hurt under the street and come up when frozen, and the
    /// Kindler turns one of the dead on the others (aim assist skips it).</summary>
    public class ZombieNewFoesPlayModeTests
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

        Vector2 OpenSpot(ZLGame g, Vector2 me, float min, float max)
        {
            for (float r = min; r <= max; r += 0.5f)
                for (int a = 0; a < 16; a++)
                {
                    float ang = a * Mathf.PI / 8f;
                    var p = me + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r;
                    if (g.City.Walkable(p) && g.City.Sees(me, p)) return p;
                }
            Assert.Inconclusive("no open spot near the start");
            return me;
        }

        [UnityTest]
        public IEnumerator Splitter_FallsIntoTwoHalves()
        {
            yield return LoadLastStand();
            var g = ZLGame.Current;
            Vector2 me = g.Player.transform.position;
            var s = g.SpawnZombie(ZLFoe.Splitter, OpenSpot(g, me, 4f, 7f), 1, false);
            int halves = 0;
            foreach (var z in g.Zombies) if (z != null && z.kind == ZLFoe.SplitterHalf) halves++;
            s.Hit(99999, Vector2.right, ZLCatalog.Pistol);
            yield return null;
            int after = 0;
            foreach (var z in g.Zombies) if (z != null && z.Alive && z.kind == ZLFoe.SplitterHalf) after++;
            Assert.AreEqual(halves + ZLRules.SplitterHalves, after, "two halves get up");
            yield return Calm();
        }

        [UnityTest]
        public IEnumerator Burrower_IsUntouchableUnderground_AndFrostDrivesItUp()
        {
            yield return LoadLastStand();
            var g = ZLGame.Current;
            Vector2 me = g.Player.transform.position;
            var b = g.SpawnZombie(ZLFoe.Burrower, OpenSpot(g, me, 7f, 10f), 1, true);
            b.maxHp = b.hp = 500;
            float t = 0f;
            while (!b.Burrowed && t < 4f) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(b.Burrowed, "it dives");
            Assert.IsFalse(b.Hostile, "bullets pass over the mound");
            b.Hit(50, Vector2.right, ZLCatalog.Pistol);
            Assert.AreEqual(500, b.hp, "nothing hurts it under the street");
            var aimed = g.AutoAim(me, ((Vector2)b.transform.position - me).normalized, 20f);
            Assert.IsTrue(aimed == null || Vector2.Distance(aimed.Value, b.transform.position) > 0.3f, "aim assist ignores the mound");
            b.Freeze(2f);
            Assert.IsFalse(b.Burrowed, "the frost drives it up");
            b.Hit(50, Vector2.right, ZLCatalog.Pistol);
            Assert.Less(b.hp, 500);
            yield return Calm();
        }

        [UnityTest]
        public IEnumerator Kindler_TurnsOneOnTheOthers()
        {
            yield return LoadLastStand();
            var g = ZLGame.Current;
            Vector2 me = g.Player.transform.position;
            Vector2 spot = OpenSpot(g, me, 3f, 5f);
            var ally = g.SpawnZombie(ZLFoe.Shambler, spot, 1, false);
            var prey = g.SpawnZombie(ZLFoe.Shambler, spot + ((spot - me).normalized * 1.2f), 1, false);
            ally.maxHp = ally.hp = 1000;
            prey.maxHp = prey.hp = 1000;
            ally.Hit(ZLCatalog.Kindler.damage, Vector2.right, ZLCatalog.Kindler);
            Assert.IsTrue(ally.Kindled);
            Assert.AreEqual(1000, ally.hp, "kindling doesn't hurt it");
            Assert.IsFalse(ally.Hostile);
            var aim = g.AutoAim(me, (spot - me).normalized, 12f);
            Assert.IsTrue(aim.HasValue);
            Assert.Greater(Vector2.Distance(aim.Value, ally.transform.position), 0.3f, "aim assist skips the kindled one");
            float t = 0f;
            while (prey.hp >= 1000 && t < 4f) { t += Time.deltaTime; yield return null; }
            Assert.Less(prey.hp, 1000, "it bites the other dead");
            Assert.AreEqual(g.Player, prey.LastHitBy, "the shooter gets the credit");
            yield return Calm();
        }
    }
}
