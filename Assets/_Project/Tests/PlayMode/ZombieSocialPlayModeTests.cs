using System.Collections;
using NUnit.Framework;
using PKR.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PKR.Tests
{
    /// <summary>Daily orders count real Last Stand play, the LOCKER's tracer colours your bullets, and the new ZOMBIES menu pages open.</summary>
    public class ZombieSocialPlayModeTests
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
        }

        [TearDown]
        public void TearDown()
        {
            ZombieDailyTracker.NowOverride = null;
            Time.timeScale = 1f;
            GameSession.Mode = SessionMode.StoryQuest;
            GameSession.ZombieSquad = SquadKind.Solo;
            GameSession.ZombieMap = LastStandRules.MapId;
            var save = Services.Save != null ? Services.Save.Data : null;
            if (save != null && _careerBackup != null)
            {
                JsonUtility.FromJsonOverwrite(_careerBackup, save.zombieCareer);
                save.starShards = _shardsBackup;
            }
            _careerBackup = null;
        }

        IEnumerator LoadLastStand()
        {
            if (!Application.CanStreamedLevelBeLoaded(SceneIds.LastStand)) Assert.Ignore("Last Stand isn't in the build.");
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

        static IEnumerator Calm()
        {
            if (Application.CanStreamedLevelBeLoaded(SceneIds.StoryTest))
            {
                SceneManager.LoadScene(SceneIds.StoryTest, LoadSceneMode.Single);
                yield return null;
                yield return null;
            }
        }

        /// <summary>A day whose first (easy) order is "kill N zombies in Last Stand" on any map.</summary>
        static int KillDay()
        {
            for (int d = ZombieDaily.DayOf(System.DateTime.UtcNow); d < ZombieDaily.DayOf(System.DateTime.UtcNow) + 400; d++)
            {
                var o = ZombieDaily.For(d)[0];
                if (o.Kind == DailyKind.Kills && o.MapId == "") return d;
            }
            return -1;
        }

        [UnityTest]
        public IEnumerator DailyOrders_CountKillsInLastStand_AndPayWhenDone()
        {
            Backup();
            int day = KillDay();
            Assert.GreaterOrEqual(day, 0);
            ZombieDailyTracker.NowOverride = ZombieDaily.Epoch.AddDays(day).AddHours(12);
            Assert.AreEqual(day, ZombieDailyTracker.Today);
            var c = Services.Save.Data.zombieCareer;
            ZombieDaily.Roll(c, day);
            c.dailyPaid = 0;
            for (int i = 0; i < c.dailyProgress.Count; i++) c.dailyProgress[i] = 0;
            var order = ZombieDaily.For(day)[0];
            c.dailyProgress[0] = order.Target - 1;   // one kill to go

            yield return LoadLastStand();
            var g = ZLGame.Current;
            var st = g.Stand;
            float t = 0f;
            while (st.Alive.Count == 0 && t < 15f) { t += Time.deltaTime; yield return null; }
            Assert.Greater(st.Alive.Count, 0);
            int shards = Services.Save.Data.starShards;
            st.Alive[0].Hit(999999, Vector2.up, null);
            yield return null;
            Assert.AreNotEqual(0, c.dailyPaid & 1, "the order is done");
            Assert.AreEqual(order.Target, ZombieDaily.ProgressOf(c, day, 0));
            Assert.GreaterOrEqual(Services.Save.Data.starShards, shards + order.RewardShards, "and paid");
            StringAssert.Contains("DAILY ORDER DONE", g.Toast);
            yield return Calm();
        }

        [UnityTest]
        public IEnumerator Locker_TracerColoursYourBullets()
        {
            Backup();
            var c = Services.Save.Data.zombieCareer;
            c.totalKills = Mathf.Max(c.totalKills, 600);
            c.tracer = "tr_toxic";
            yield return LoadLastStand();
            var g = ZLGame.Current;
            Assert.IsTrue(g.Player.Tracer.HasValue);
            var tox = ZombieCosmetics.Get("tr_toxic");
            Assert.AreEqual(tox.G, g.Player.Tracer.Value.g);
            yield return Calm();
        }

        static Button FindButton(Transform root, string startsWith)
        {
            foreach (var b in root.GetComponentsInChildren<Button>(true))
            {
                var t = b.GetComponentInChildren<Text>();
                if (t != null && t.text.StartsWith(startsWith)) return b;
            }
            return null;
        }

        static bool HasText(Transform root, string startsWith)
        {
            foreach (var t in root.GetComponentsInChildren<Text>(true)) if (t.text.StartsWith(startsWith)) return true;
            return false;
        }

        [UnityTest]
        public IEnumerator ZombiesMenu_DailyLockerAndBoardsPagesOpen()
        {
            Backup();
            var host = new GameObject("MenuHost");
            var root = UIFactory.CreateCanvas(host.transform, "ZombiesTest", 50, landscape: false, out _);
            var panel = host.AddComponent<ZombiesMenuPanel>();
            panel.Open(root, null);
            yield return null;
            Assert.IsTrue(HasText(root, "DAILY ORDERS "), "today's orders on the main page");
            foreach (var (button, page) in new[] { ("DAILY ORDERS", "DAILY ORDERS"), ("LOCKER", "LOCKER"), ("BOARDS", "LEADERBOARDS") })
            {
                var b = FindButton(root, button);
                Assert.IsNotNull(b, button);
                b.onClick.Invoke();
                yield return null;
                Assert.IsTrue(HasText(root, page), page + " opens");
                var back = FindButton(root, "BACK");
                Assert.IsNotNull(back);
                back.onClick.Invoke();
                yield return null;
                Assert.IsTrue(HasText(root, "ZOMBIES"), "back to the menu");
            }
            Object.Destroy(host);
            yield return null;
        }
    }
}
