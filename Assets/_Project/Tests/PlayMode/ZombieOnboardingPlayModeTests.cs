using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using PKR.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PKR.Tests
{
    /// <summary>First-time coaching tips and the rookie warm-up in Last Stand.</summary>
    public class ZombieOnboardingPlayModeTests
    {
        string _careerBackup;
        List<string> _hintsBackup;
        int _shards;

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            GameSession.Mode = SessionMode.StoryQuest;
            GameSession.ZombieSquad = SquadKind.Solo;
            var save = Services.Save != null ? Services.Save.Data : null;
            if (save == null) return;
            if (_careerBackup != null) JsonUtility.FromJsonOverwrite(_careerBackup, save.zombieCareer);
            if (_hintsBackup != null) { save.seenHints.Clear(); save.seenHints.AddRange(_hintsBackup); }
            if (_careerBackup != null) save.starShards = _shards;
            _careerBackup = null; _hintsBackup = null;
        }

        IEnumerator Load()
        {
            if (!Application.CanStreamedLevelBeLoaded(SceneIds.LastStand)) Assert.Ignore("Last Stand isn't in the build.");
            var save = Services.Save.Data;
            if (save.zombieCareer == null) save.zombieCareer = new ZombieCareer();
            if (save.seenHints == null) save.seenHints = new List<string>();
            _careerBackup = JsonUtility.ToJson(save.zombieCareer);
            _hintsBackup = new List<string>(save.seenHints);
            _shards = save.starShards;
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new ZombieCareer()), save.zombieCareer);   // a brand-new player
            save.seenHints.RemoveAll(h => h.StartsWith("z_"));
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
        public IEnumerator NewPlayer_GetsThePointsTip_Once_AndARookieWarmUp()
        {
            yield return Load();
            var g = ZLGame.Current;
            var st = g.Stand;
            Assert.IsNotNull(g.Coach, "the coach runs in Last Stand");
            float t = 0f;
            while (g.CoachLeft <= 0f && t < 15f) { t += Time.deltaTime; yield return null; }
            Assert.Greater(g.CoachLeft, 0f, "a tip appears");
            StringAssert.Contains("POINTS", g.CoachTip);
            Assert.IsTrue(Services.Save.Data.seenHints.Contains(ZombieTips.Points));
            Assert.IsFalse(g.Coach.Show(ZombieTips.Points), "each tip shows once");

            Assert.IsTrue(st.RookieRound, "a first run opens with the rookie warm-up");
            t = 0f;
            while (st.Alive.Count == 0 && t < 15f) { t += Time.deltaTime; yield return null; }
            Assert.Greater(st.Alive.Count, 0);
            var z = st.Alive[0];
            float normal = LastStandRules.Speed(z.kind, st.Run.Round, st.Mode);
            Assert.Less(z.speed, normal * 1.1f * ZombieAssist.SpeedScale(st.Assist, true) + 0.001f);
            yield return Calm();
        }

        [UnityTest]
        public IEnumerator Veteran_HasNoRookieWarmUp()
        {
            yield return Load();
            Services.Save.Data.zombieCareer.lastStandRuns = 5;
            var st = ZLGame.Current.Stand;
            // the run count is read when the run starts: reload
            SceneManager.LoadScene(SceneIds.LastStand, LoadSceneMode.Single);
            yield return null;
            float t = 0f;
            while ((ZLGame.Current == null || !ZLGame.Current.Ready || !ZLGame.Current.IsLastStand || ZLGame.Current.Stand == st) && t < 10f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsFalse(ZLGame.Current.Stand.RookieRound);
            yield return Calm();
        }
    }
}
