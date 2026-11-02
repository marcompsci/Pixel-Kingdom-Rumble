using System.Collections;
using NUnit.Framework;
using PKR.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PKR.Tests
{
    /// <summary>
    /// The Kingdom Plaza (buildings, honest live counts, the enter card), the Item Race (countdown, CPU racers move,
    /// item boxes) and the four arena party quests (each starts with its own rules and props). Needs Build All Scenes.
    /// </summary>
    public class PartyPlayModeTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            GameSession.Party = PartyMode.None;
            GameSession.PlazaReturnX = -1f;
            Time.timeScale = 1f;
            var old = SceneManager.GetActiveScene();
            var empty = SceneManager.CreateScene("PartyDone" + Time.frameCount);
            SceneManager.SetActiveScene(empty);
            if (old.IsValid() && old.isLoaded && old != empty) yield return SceneManager.UnloadSceneAsync(old);
        }

        static IEnumerator Load(string scene)
        {
            if (!Application.CanStreamedLevelBeLoaded(scene)) Assert.Ignore($"{scene} not built; run PKR > Build All Scenes.");
            SceneManager.LoadScene(scene, LoadSceneMode.Single);
            yield return null;
            yield return null;
            yield return null;
        }

        static int Count(string prefix)
        {
            int n = 0;
            foreach (var t in Object.FindObjectsByType<Transform>()) if (t.name.StartsWith(prefix)) n++;
            return n;
        }

        static bool HasText(string contains)
        {
            foreach (var t in Object.FindObjectsByType<Text>())
                if (t.isActiveAndEnabled && t.text != null && t.text.Contains(contains)) return true;
            return false;
        }

        [UnityTest]
        public IEnumerator Plaza_HasEveryQuestBuilding_AndHonestCounts()
        {
            yield return Load(SceneIds.KingdomPlaza);
            Assert.IsNotNull(Object.FindAnyObjectByType<HubDirector>(), "the plaza runs the hub");
            Assert.AreEqual(KingdomPlaza.Buildings.Length, Count("Building_"));
            Assert.AreEqual(KingdomPlaza.Buildings.Length, Count("Sign_"));
            yield return new WaitForSecondsRealtime(1.2f);
            // No Game Center in the editor: counts must read "--", never a number we made up.
            Assert.IsTrue(HasText("-- LIVE"), "unknown counts show --");
            Assert.IsTrue(HasText("KINGDOM PLAZA"));
            // Walk the hero to the first door: the enter card appears.
            var hero = Object.FindAnyObjectByType<PlayerMarker>();
            Assert.IsNotNull(hero);
            var motor = hero.GetComponent<PlatformerMotor2D>();
            motor.Teleport(new Vector2(KingdomPlaza.Buildings[0].x + 0.5f, 3.2f));
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.IsTrue(HasText("ENTER"), "the enter card at a door");
            Assert.IsTrue(HasText(KingdomPlaza.Buildings[0].name.ToUpperInvariant()));
        }

        [UnityTest]
        public IEnumerator ItemRace_CountsDown_ThenCpuRacersRun()
        {
            yield return Load(SceneIds.ItemRace);
            var race = Object.FindAnyObjectByType<RaceDirector>();
            Assert.IsNotNull(race);
            yield return new WaitForSecondsRealtime(0.5f);
            var bots = Object.FindObjectsByType<RacerBot>();
            Assert.AreEqual(3, bots.Length, "three CPU racers");
            Assert.Greater(Count("ItemBox"), 3, "item boxes along the course");
            float[] start = new float[bots.Length];
            for (int i = 0; i < bots.Length; i++) start[i] = bots[i].transform.position.x;
            yield return new WaitForSecondsRealtime(9f);
            int moved = 0;
            for (int i = 0; i < bots.Length; i++) if (bots[i].transform.position.x > start[i] + 6f) moved++;
            Assert.GreaterOrEqual(moved, 2, "CPU racers run the course after GO");
        }

        static IEnumerator Party(PartyMode mode)
        {
            GameSession.Party = mode;
            yield return Load(SceneIds.ArenaTest);
            yield return new WaitForSecondsRealtime(0.5f);
            var arena = Object.FindAnyObjectByType<ArenaMatchController>();
            Assert.IsNotNull(arena);
            arena.grantRewards = false;
            Assert.IsTrue(arena.IsLive, "a party quest starts at once");
            Assert.AreEqual(mode, arena.Config.party);
            Assert.AreEqual(4, arena.Fighters.Count);
            Assert.AreEqual(3, Count("ItemBox"), "item boxes in every party arena");
        }

        [UnityTest]
        public IEnumerator BalloonBattle_GivesEveryoneBalloons()
        {
            yield return Party(PartyMode.BalloonBattle);
            Assert.AreEqual(4, Count("Balloons"));
            yield return new WaitForSecondsRealtime(3f);
        }

        [UnityTest]
        public IEnumerator CoinRush_SpawnsCoins()
        {
            yield return Party(PartyMode.CoinRush);
            yield return new WaitForSecondsRealtime(3f);
            Assert.Greater(Count("Coin"), 1);
            Assert.AreEqual(MatchMode.Timed, Object.FindAnyObjectByType<ArenaMatchController>().Match.Mode);
        }

        [UnityTest]
        public IEnumerator KingOfTheHill_HasTheCrownZone()
        {
            yield return Party(PartyMode.KingOfTheHill);
            yield return new WaitForSecondsRealtime(2f);
            Assert.AreEqual(1, Count("CrownZone"));
        }

        [UnityTest]
        public IEnumerator RingOut_RunsWithItems()
        {
            yield return Party(PartyMode.RingOut);
            var arena = Object.FindAnyObjectByType<ArenaMatchController>();
            var zone = arena.BlastZone;
            yield return new WaitForSecondsRealtime(3f);
            Assert.AreEqual(zone.width, arena.BlastZone.width, 0.01f, "the ring only closes after a minute");
        }
    }
}
