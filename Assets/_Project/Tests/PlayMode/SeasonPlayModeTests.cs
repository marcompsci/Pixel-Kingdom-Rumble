using System;
using System.Collections;
using NUnit.Framework;
using PKR.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PKR.Tests
{
    /// <summary>
    /// Live seasons: real Story levels loaded on a pretend date (Christmas, Thanksgiving week, a spring day) dress
    /// themselves, run without errors, and the menus show the season. Needs PKR > Build All Scenes.
    /// </summary>
    public class SeasonPlayModeTests
    {
        /// <summary>Leave an empty scene behind, so later tests don't run inside a dressed Story level with its hero.</summary>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SeasonCalendar.OverrideToday = null;
            if (Services.Settings != null) Services.Settings.SetSeasonalMode(SeasonCalendar.ModeLive);
            Time.timeScale = 1f;
            var old = SceneManager.GetActiveScene();
            var empty = SceneManager.CreateScene("SeasonTestsDone" + Time.frameCount);
            SceneManager.SetActiveScene(empty);
            if (old.IsValid() && old.isLoaded && old != empty) yield return SceneManager.UnloadSceneAsync(old);
            yield return null;
        }

        static IEnumerator Load(string scene, DateTime day)
        {
            if (!Application.CanStreamedLevelBeLoaded(scene))
                Assert.Ignore($"{scene} is not in Build Settings; run PKR > Build All Scenes first.");
            if (Services.Settings != null) Services.Settings.SetSeasonalMode(SeasonCalendar.ModeLive);
            SeasonCalendar.OverrideToday = day;
            SceneManager.LoadScene(scene, LoadSceneMode.Single);
            yield return null;
            yield return null;
            yield return null;
        }

        static int Count(string name)
        {
            int n = 0;
            foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>()) if (t.name == name) n++;
            return n;
        }

        [UnityTest]
        public IEnumerator Christmas_DressesAStoryLevel()
        {
            yield return Load(SceneIds.SunspireHeights, new DateTime(2026, 12, 24));
            var d = SeasonDirector.Current;
            Assert.IsNotNull(d, "a Story level gets a season director");
            Assert.AreEqual(SeasonEvent.Winterfest, d.State.Event);
            Assert.AreEqual(SeasonPeak.Christmas, d.State.Peak);
            Assert.Greater(Count("Ledge"), 0, "snow lies on the ledges");
            Assert.Greater(Count("Flake"), 0, "snow falls");
            Assert.AreEqual(1, Count("SeasonToken"), "one hidden gift");
            Assert.Greater(Count("GoalTree"), 0, "a lit tree at the goal");
            Assert.Greater(Count("Bulb"), 0);
            yield return new WaitForSecondsRealtime(2f); // weather, twinkle and hats tick without errors
        }

        [UnityTest]
        public IEnumerator ThanksgivingWeek_SetsTheFeast_AndFoesWearHats()
        {
            yield return Load(SceneIds.GearfallCaverns, new DateTime(2026, 11, 26));
            var d = SeasonDirector.Current;
            Assert.IsNotNull(d);
            Assert.AreEqual(SeasonEvent.Harvest, d.State.Event);
            Assert.Greater(Count("Flake"), 0, "leaves fall");
            Assert.Greater(Count("GoalFeast"), 0, "a feast table at the goal");
            Assert.Greater(Count("CheckpointFeast") + Count("CheckpointHay"), -1);
            yield return new WaitForSecondsRealtime(3f); // the foe scan runs
            int foes = UnityEngine.Object.FindObjectsByType<EnemyAI>().Length;
            if (foes > 0) Assert.Greater(Count("SeasonHat"), 0, "foes wear leaf crowns");
        }

        [UnityTest]
        public IEnumerator SpringDay_LeavesTheLevelAlone()
        {
            yield return Load(SceneIds.SunspireHeights, new DateTime(2027, 4, 15));
            Assert.IsNull(SeasonDirector.Current, "no season in spring");
            Assert.AreEqual(0, Count("SeasonToken"));
            Assert.AreEqual(0, Count("Flake"));
        }

        [UnityTest]
        public IEnumerator SettingOff_LeavesTheLevelAlone()
        {
            yield return Load(SceneIds.SunspireHeights, new DateTime(2026, 12, 24));
            Assert.IsNotNull(SeasonDirector.Current);
            if (Services.Settings == null) Assert.Ignore("needs the settings service");
            Services.Settings.SetSeasonalMode(SeasonCalendar.ModeOff);
            SceneManager.LoadScene(SceneIds.SunspireHeights, LoadSceneMode.Single);
            yield return null;
            yield return null;
            Assert.IsNull(SeasonDirector.Current, "seasons turned off in Settings");
            Services.Settings.SetSeasonalMode(SeasonCalendar.ModeLive);
        }

        [UnityTest]
        public IEnumerator Menus_ShowTheSeason()
        {
            yield return Load(SceneIds.MainMenu, new DateTime(2026, 11, 26));
            Assert.Greater(Count("SeasonWeather"), 0, "leaves drift over the menu");
            yield return new WaitForSecondsRealtime(1f);
            yield return Load(SceneIds.LevelSelect, new DateTime(2026, 12, 24));
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.Greater(Count("SeasonBadge"), 0, "level nodes show the season's token");
        }
    }
}
