using System.Collections;
using System.Collections.Generic;
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
    /// The balance lab: the CPU plays every hero against four sparring partners (light, heavy, fast, puncher) on Hard,
    /// sped up, and writes win rates to Logs/balance/. Runs only when Logs/balance_request exists (Tools/balance_lab.command
    /// creates it), so normal checks skip it.
    /// </summary>
    public class BalanceLab
    {
        static readonly string[] Heroes =
        {
            "nova", "brick", "luma", "rex_rollo", "kai_tempest", "mara_vex", "volt_ramirez", "sola_brightwing", "grizz",
            "pixel_pip", "nyx_frost", "blaze_torres", "juno_strike", "ollie_kickflip", "juni_bloom", "marco_brio",
            "tamsin_reed", "dex_ortega", "pepper_quinn", "bo_tidewell", "zuri_starling", "otto_brass"
        };
        static readonly string[] Sparring = { "nova", "grizz", "mara_vex", "volt_ramirez" };

        static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        [UnityTest, Timeout(7200000)]
        public IEnumerator RunLab()
        {
            if (!File.Exists(Path.Combine(Root, "Logs", "balance_request")))
                Assert.Ignore("The balance lab runs only on request (Logs/balance_request).");
            if (!Application.CanStreamedLevelBeLoaded(SceneIds.VersusStage)) Assert.Ignore("No Versus stage in the build.");
            SceneManager.LoadScene(SceneIds.VersusStage, LoadSceneMode.Single);
            yield return null;
            yield return null;
            var vc = Object.FindAnyObjectByType<VersusController>();
            Assert.IsNotNull(vc);
            vc.grantRewards = false;
            vc.botVsBot = true;

            var outDir = Path.Combine(Root, "Logs", "balance");
            Directory.CreateDirectory(outDir);
            var csv = new StringBuilder("hero,partner,side,winner,heroRounds,partnerRounds,seconds\n");
            var wins = new Dictionary<string, float>();
            var games = new Dictionary<string, int>();
            Time.timeScale = 10f;
            try
            {
                for (int h = 0; h < Heroes.Length; h++)
                {
                    for (int s = 0; s < Sparring.Length; s++)
                    {
                        string hero = Heroes[h], partner = Sparring[s];
                        if (hero == partner) continue;
                        int side = (h + s) % 2; // alternate sides
                        vc.StartMatch(side == 0 ? hero : partner, side == 0 ? partner : hero, BotLevel.Hard);
                        float started = Time.realtimeSinceStartup;
                        while (vc.Match != null && vc.Match.Phase != VersusPhase.MatchOver && Time.realtimeSinceStartup - started < 40f)
                            yield return null;
                        var m = vc.Match;
                        int heroSlot = side == 0 ? 0 : 1;
                        int winner = m.Phase == VersusPhase.MatchOver ? m.Winner : -1;
                        float score = winner < 0 ? 0.5f : winner == heroSlot ? 1f : 0f;
                        wins[hero] = (wins.TryGetValue(hero, out var w) ? w : 0f) + score;
                        games[hero] = (games.TryGetValue(hero, out var g) ? g : 0) + 1;
                        csv.Append($"{hero},{partner},{side},{(winner < 0 ? "draw" : winner == heroSlot ? "hero" : "partner")}," +
                                   $"{m.Wins(heroSlot)},{m.Wins(1 - heroSlot)},{Time.realtimeSinceStartup - started:0.0}\n");
                        File.WriteAllText(Path.Combine(outDir, "results.csv"), csv.ToString());
                        yield return null;
                    }
                }
            }
            finally { Time.timeScale = 1f; }

            var summary = new StringBuilder("hero,winRate,games\n");
            foreach (var hero in Heroes)
                if (games.TryGetValue(hero, out var n) && n > 0)
                    summary.Append($"{hero},{wins[hero] / n:0.00},{n}\n");
            File.WriteAllText(Path.Combine(outDir, "summary.csv"), summary.ToString());
            Debug.Log("[PKR BalanceLab] done\n" + summary);
        }
    }
}
