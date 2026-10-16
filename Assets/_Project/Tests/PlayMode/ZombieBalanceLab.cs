using System.Collections;
using System.IO;
using System.Text;
using NUnit.Framework;
using PKR.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PKR.Tests
{
    /// <summary>
    /// The zombie balance lab: a bot plays Last Stand (every map, solo and with the AI squad, Endless) sped up, and
    /// writes how long each round took, the points it had and when it fell to Logs/zombie_balance/. The bot only shoots,
    /// dodges and revives (it gets better guns at rounds 4/8/12 like the AI teammates) and never buys doors, perks or the
    /// box, so it is a "weak player" baseline. Runs only when Logs/zombie_balance_request exists.
    /// </summary>
    public class ZombieBalanceLab
    {
        static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        const int MaxRound = 25;
        const float MaxRealSeconds = 300f, Speed = 4f;

        [UnityTest, Timeout(10800000)]
        public IEnumerator RunLab()
        {
            if (!File.Exists(Path.Combine(Root, "Logs", "zombie_balance_request")))
                Assert.Ignore("The zombie balance lab runs only on request (Logs/zombie_balance_request).");
            if (!Application.CanStreamedLevelBeLoaded(SceneIds.LastStand)) Assert.Ignore("Last Stand isn't in the build.");
            var outDir = Path.Combine(Root, "Logs", "zombie_balance");
            Directory.CreateDirectory(outDir);
            var rounds = new StringBuilder("map,squad,round,gameSeconds,points,kills,heroHp\n");
            var summary = new StringBuilder("map,squad,reachedRound,gameSeconds,kills,points,ended\n");
            var save = Services.Save != null ? Services.Save.Data : null;
            string careerBackup = save != null && save.zombieCareer != null ? JsonUtility.ToJson(save.zombieCareer) : null;
            int shardsBackup = save != null ? save.starShards : 0;

            foreach (var map in LastStandMap.All)
                foreach (var squad in new[] { SquadKind.Solo, SquadKind.AISquad })
                {
                    GameSession.Mode = SessionMode.Zombies;
                    GameSession.ZombieMode = SurvivalMode.Endless;
                    GameSession.ZombieSquad = squad;
                    GameSession.ZombieMap = map.Id;
                    GameSession.ZombieWeekly = false;
                    Time.timeScale = 1f;
                    SceneManager.LoadScene(SceneIds.LastStand, LoadSceneMode.Single);
                    yield return null;
                    float w = 0f;
                    while ((ZLGame.Current == null || !ZLGame.Current.Ready || !ZLGame.Current.IsLastStand) && w < 15f) { w += Time.unscaledDeltaTime; yield return null; }
                    var g = ZLGame.Current;
                    Assert.IsNotNull(g, map.Id);
                    var st = g.Stand;
                    g.Player.Loadout = g.Progress;
                    g.Player.BecomeAI(new ZLAIBrain(g, 0));
                    Time.timeScale = Speed;
                    float real = 0f, game = 0f, roundStart = 0f;
                    int lastRound = 0;
                    while (!st.Over && real < MaxRealSeconds && st.Run.Round <= MaxRound)
                    {
                        real += Time.unscaledDeltaTime;
                        game += Time.deltaTime;
                        if (st.Run.Round != lastRound)
                        {
                            if (lastRound > 0)
                                rounds.Append($"{map.Id},{squad},{lastRound},{game - roundStart:F1},{st.Run.Points},{st.Run.Kills},{g.Player.Health}\n");
                            lastRound = st.Run.Round;
                            roundStart = game;
                        }
                        yield return null;
                    }
                    string ended = st.Over ? "fell" : st.Run.Round > MaxRound ? "cap" : "time";
                    summary.Append($"{map.Id},{squad},{st.Run.Round},{game:F0},{st.Run.Kills},{st.Run.TotalEarned},{ended}\n");
                    Time.timeScale = 1f;
                    File.WriteAllText(Path.Combine(outDir, "rounds.csv"), rounds.ToString());
                    File.WriteAllText(Path.Combine(outDir, "summary.csv"), summary.ToString());
                    yield return null;
                }

            if (save != null && careerBackup != null) { JsonUtility.FromJsonOverwrite(careerBackup, save.zombieCareer); save.starShards = shardsBackup; }
            GameSession.Mode = SessionMode.StoryQuest;
            GameSession.ZombieSquad = SquadKind.Solo;
            GameSession.ZombieMap = LastStandRules.MapId;
            Debug.Log("[PKR ZombieBalanceLab]\n" + summary);
        }
    }
}
