using System.Collections;
using NUnit.Framework;
using PKR.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace PKR.Tests
{
    /// <summary>Arena match flow: spawn, blast-zone KO, elimination, results with the player in first place.</summary>
    public class ArenaPlayModeTests
    {
        GameObject _floor, _host, _s1, _s2;
        CharacterDefinition _hero;
        bool _ended;
        ArenaMatchEnded _result;

        void OnEnded(ArenaMatchEnded e) { _ended = true; _result = e; }

        [SetUp]
        public void SetUp()
        {
            _floor = new GameObject("Floor") { layer = PKRLayers.Ground };
            _floor.transform.position = new Vector3(0f, -0.5f, 0f);
            _floor.AddComponent<BoxCollider2D>().size = new Vector2(18f, 1f);
            _hero = ScriptableObject.CreateInstance<CharacterDefinition>();
            _hero.id = "test_hero"; _hero.displayName = "Tester"; _hero.playableInThisBuild = true;
            _s1 = new GameObject("S1"); _s1.transform.position = new Vector3(-3f, 1f, 0f);
            _s2 = new GameObject("S2"); _s2.transform.position = new Vector3(3f, 1f, 0f);
            EventBus<ArenaMatchEnded>.Subscribe(OnEnded);
            _ended = false;
        }

        [TearDown]
        public void TearDown()
        {
            EventBus<ArenaMatchEnded>.Unsubscribe(OnEnded);
            if (_host != null) Object.Destroy(_host);
            foreach (var go in new[] { _floor, _s1, _s2 }) if (go != null) Object.Destroy(go);
            Object.Destroy(_hero);
            if (Services.State != null) Services.State.SetState(GameState.Menu);
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator BotFallingOut_EndsStockMatch_PlayerWins()
        {
            _host = new GameObject("Arena");
            var match = _host.AddComponent<ArenaMatchController>();
            match.grantRewards = false;
            match.Configure(_hero, new[] { _s1.transform, _s2.transform },
                            new Rect(-15f, -8f, 30f, 20f), new Rect(-9f, -1f, 18f, 1f));
            yield return null;

            match.StartMatch(new ArenaMatchConfig { mode = MatchMode.Stock, botCount = 1, stocks = 1 });
            Assert.IsTrue(match.IsLive);
            Assert.AreEqual(2, match.Fighters.Count);
            Assert.IsNotNull(match.Fighters[0].go.GetComponent<PlayerInputRouter>());
            Assert.IsNotNull(match.Fighters[1].go.GetComponent<BotController>());

            // Throw the CPU fighter out of the blast zone.
            match.Fighters[1].motor.Teleport(new Vector2(0f, -20f));
            for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();
            yield return null;

            Assert.IsTrue(_ended, "match should end when the only rival is out of stocks");
            Assert.AreEqual(1, _result.playerPlacement);
            Assert.AreEqual(1, _result.falls[1]);
            Assert.IsFalse(match.IsLive);
        }
    }
}
