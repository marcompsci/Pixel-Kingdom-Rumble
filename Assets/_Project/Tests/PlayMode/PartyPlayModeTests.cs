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

        static Button ButtonWith(string text)
        {
            foreach (var b in Object.FindObjectsByType<Button>())
            {
                var t = b.GetComponentInChildren<Text>();
                if (b.isActiveAndEnabled && t != null && t.text != null && t.text.StartsWith(text)) return b;
            }
            return null;
        }

        [UnityTest]
        public IEnumerator Plaza_NoticeBoard_TownsfolkAndMapTravel()
        {
            yield return Load(SceneIds.KingdomPlaza);
            Assert.AreEqual(1, Count("NoticeBoard"));
            Assert.GreaterOrEqual(Count("Townsfolk_"), 3, "townsfolk stroll the street");
            var hero = Object.FindAnyObjectByType<PlayerMarker>();
            var motor = hero.GetComponent<PlatformerMotor2D>();
            motor.Teleport(new Vector2(HubDirector.BoardX + 0.5f, 3.2f));
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.IsTrue(HasText("NOTICE BOARD"));
            Assert.IsNotNull(ButtonWith("CPUs:"), "difficulty button");
            Assert.IsNotNull(ButtonWith("KINGDOM CUP") ?? ButtonWith("CONTINUE CUP"), "cup button");
            Assert.IsNotNull(ButtonWith("AUTO-RUN"), "auto-run button");
            // Fast travel: MAP > COIN VAULT puts the hero at that door.
            var map = ButtonWith("MAP");
            Assert.IsNotNull(map);
            map.onClick.Invoke();
            yield return null;
            var vault = ButtonWith("COIN VAULT");
            Assert.IsNotNull(vault, "every place is on the map");
            vault.onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.3f);
            float vaultX = 0f;
            foreach (var b in KingdomPlaza.Buildings) if (b.party == PartyMode.CoinRush) vaultX = b.x;
            Assert.AreEqual(vaultX + 0.5f, hero.transform.position.x, 1.5f, "travelled to the Coin Vault");
            Assert.IsTrue(HasText("COIN VAULT"), "its enter card is up");
        }

        [UnityTest]
        public IEnumerator Plaza_StopsAnnouncing_WhenThePlayerLeaves()
        {
            yield return Load(SceneIds.KingdomPlaza);
            yield return null;
            Assert.AreEqual(KingdomPlaza.PlazaGroup, LivePresence.Here, "the plaza announces itself");
            var old = SceneManager.GetActiveScene();
            var empty = SceneManager.CreateScene("LeftPlaza" + Time.frameCount);
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync(old);
            yield return null;
            Assert.AreEqual(-1, LivePresence.Here, "no live-count pings after leaving (they could cancel an Online Co-op search)");
        }

        [UnityTest]
        public IEnumerator ItemRace_YourBestRunRacesAsAGhost()
        {
            if (Services.Save == null) Assert.Ignore("needs the save service");
            var party = Services.Save.Data.party;
            string oldGhost = party.ghost; float oldSecs = party.ghostSeconds;
            var g = new GhostTrack();
            for (int i = 0; i < 200; i++) g.Add(4f + i * 0.5f, 3f);
            party.ghost = g.Encode(); party.ghostSeconds = 20f;
            try
            {
                yield return Load(SceneIds.ItemRace);
                yield return new WaitForSecondsRealtime(4.5f); // past 3-2-1-GO
                Assert.AreEqual(1, Count("RaceGhost"), "the ghost is on the course");
                var ghost = GameObject.Find("RaceGhost");
                float x0 = ghost.transform.position.x;
                yield return new WaitForSecondsRealtime(1f);
                Assert.Greater(ghost.transform.position.x, x0 + 2f, "and it runs your saved line");
                Assert.IsNull(ghost.GetComponent<Collider2D>(), "it can't touch anyone");
            }
            finally { party.ghost = oldGhost; party.ghostSeconds = oldSecs; }
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
            if (Services.Save != null) Assert.IsTrue(HasText("RUN RIGHT TO THE FLAG") || Services.Save.Data.seenHints.Contains(HowToPlay.HintId(PartyMode.ItemRace)),
                                                     "the first race shows how to play");
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
            yield return new WaitForSecondsRealtime(4f);
            var arena = Object.FindAnyObjectByType<ArenaMatchController>();
            Assert.AreEqual(MatchMode.Timed, arena.Match.Mode);
            int collected = 0;
            foreach (var f in arena.Match.Fighters) collected += f.score;
            // CPUs head straight for coins, so count the ones on the field and the ones already grabbed.
            Assert.GreaterOrEqual(Count("Coin") + collected, 3, $"coins keep appearing (field {Count("Coin")}, collected {collected})");
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
