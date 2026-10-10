using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using PKR.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace PKR.Tests
{
    /// <summary>Shadow Contracts: the board's states, target gating, and relic pickups, in empty scenes.</summary>
    public class ContractPlayModeTests
    {
        readonly List<Object> _cleanup = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            PoolService.ClearAll();
            foreach (var o in _cleanup) if (o != null) Object.Destroy(o);
            _cleanup.Clear();
        }

        T Track<T>(T o) where T : Object { _cleanup.Add(o); return o; }

        MissionDefinition Contract(string id, ContractKind kind, bool free, string clue = "")
        {
            var lvl = Track(ScriptableObject.CreateInstance<LevelDefinition>());
            lvl.id = id; lvl.displayName = id; lvl.sceneName = "MS_NotBuilt_" + id;
            var m = Track(ScriptableObject.CreateInstance<MissionDefinition>());
            m.id = id; m.displayName = id.ToUpperInvariant(); m.kind = kind; m.free = free; m.revealedByClue = clue;
            m.clueHint = "look harder"; m.level = lvl; m.totalRelics = 3;
            return m;
        }

        [UnityTest]
        public IEnumerator Board_ShowsFreeTeaser_PaidLock_AndHiddenSecret()
        {
            var board = Track(ScriptableObject.CreateInstance<MissionBoardDefinition>());
            board.contracts.Add(Contract("pkr_test_c1", ContractKind.Main, true));
            board.contracts.Add(Contract("pkr_test_c2", ContractKind.Main, false));
            board.contracts.Add(Contract("pkr_test_s1", ContractKind.Secret, false, "pkr_test_clue_never_found"));
            var host = Track(new GameObject("Board"));
            host.SetActive(false);
            var ui = host.AddComponent<MissionBoardUI>();
            ui.SetBoard(board);
            host.SetActive(true);
            yield return null;

            Assert.AreEqual(3, ui.Cards.Count);
            Assert.AreEqual(ContractState.Open, ui.Slots[0].state);
            bool owned = Services.Save != null && Services.Save.Data.contractsUnlocked;
            Assert.AreEqual(owned ? ContractState.Open : ContractState.NeedsUnlock, ui.Slots[1].state);
            Assert.AreEqual(ContractState.Hidden, ui.Slots[2].state);
            StringAssert.Contains("???", ui.Cards[2].GetComponentInChildren<UnityEngine.UI.Text>().text);
            Assert.IsFalse(ui.Cards[2].interactable, "hidden secrets can't be tapped");
        }

        [UnityTest]
        public IEnumerator Gate_WaitsForTheTarget()
        {
            var def = Track(ScriptableObject.CreateInstance<EnemyDefinition>());
            def.id = "test_captain"; def.isMissionTarget = true;
            var sp = Track(new GameObject("Spawn")).AddComponent<EnemySpawnPoint>();
            sp.enemy = def;
            var tracker = Track(new GameObject("Tracker")).AddComponent<StealthTracker>();
            yield return null;
            Assert.AreEqual(1, tracker.TargetsTotal);
            Assert.IsFalse(tracker.CanFinish());
            tracker.ReportTargetDown("Captain");
            Assert.IsTrue(tracker.CanFinish());
            Assert.AreEqual(1, tracker.ObjectivesDone);
            EventBus<PlayerRespawned>.Raise(new PlayerRespawned { afterDeath = true });
            Assert.AreEqual(0, tracker.TargetsDown, "a full death brings the target back");
        }

        [UnityTest]
        public IEnumerator Relic_IsCountedOnce_WhenTouched()
        {
            var relic = Track(new GameObject("Relic"));
            relic.transform.position = new Vector2(30f, 30f);
            relic.AddComponent<CircleCollider2D>().radius = 0.5f;
            relic.AddComponent<RelicPickup>();
            var mission = Track(new GameObject("Mission")).AddComponent<MissionTracker>();
            yield return null;
            Assert.AreEqual(1, mission.TotalRelics);

            var hero = Track(new GameObject("Hero"));
            hero.transform.position = new Vector2(30f, 30f);
            var rb = hero.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            hero.AddComponent<CapsuleCollider2D>().size = new Vector2(0.7f, 1.4f);
            hero.AddComponent<PlayerMarker>();
            for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(1, mission.Relics);
            Assert.IsFalse(relic.activeSelf, "taken");
        }
    }
}
