using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PKR.Tests
{
    /// <summary>Moving platform carry and contact damage, built in an empty scene.</summary>
    public class LevelPlayModeTests
    {
        const int GroundLayer = 8;
        GameObject _platform, _hero, _spikes;
        PlatformerMotor2D _motor;

        [SetUp]
        public void SetUp()
        {
            _platform = new GameObject("Platform") { layer = GroundLayer };
            _platform.transform.position = new Vector3(0f, 0f, 0f);
            var rb = _platform.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            _platform.AddComponent<BoxCollider2D>().size = new Vector2(4f, 0.5f);
            var mp = _platform.AddComponent<MovingPlatform>();
            mp.travel = new Vector2(6f, 0f);
            mp.legDuration = 3f;
            mp.pauseAtEnds = 0f;

            _hero = new GameObject("Hero");
            _hero.transform.position = new Vector3(0f, 1.2f, 0f);
            _hero.AddComponent<Rigidbody2D>();
            _hero.AddComponent<CapsuleCollider2D>().size = new Vector2(0.7f, 1.4f);
            _motor = _hero.AddComponent<PlatformerMotor2D>();
            _motor.SetGroundMask(1 << GroundLayer);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_hero);
            Object.Destroy(_platform);
            if (_spikes != null) Object.Destroy(_spikes);
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator Rider_IsCarriedByMovingPlatform()
        {
            for (int i = 0; i < 20; i++) yield return new WaitForFixedUpdate();
            Assert.IsTrue(_motor.IsGrounded, "should land on the platform");
            float heroStart = _motor.Body.position.x;
            float platStart = _platform.GetComponent<Rigidbody2D>().position.x;
            for (int i = 0; i < 60; i++) yield return new WaitForFixedUpdate();
            float heroMoved = _motor.Body.position.x - heroStart;
            float platMoved = _platform.GetComponent<Rigidbody2D>().position.x - platStart;
            Assert.Greater(platMoved, 0.5f, "platform should have moved");
            Assert.AreEqual(platMoved, heroMoved, 0.25f, "rider should move with the platform");
        }

        [UnityTest]
        public IEnumerator Spikes_DamagePlayerTeamOnce_ThenInvulnerabilityProtects()
        {
            var d = _hero.AddComponent<Damageable>();
            d.Configure(DamageModel.StoryHealth, TeamIds.Player, 5, 1f, 0f, 1f);
            var hb = new GameObject("Hurtbox");
            hb.transform.SetParent(_hero.transform, false);
            hb.AddComponent<BoxCollider2D>().size = new Vector2(0.7f, 1.4f);
            hb.AddComponent<Hurtbox>();

            _spikes = new GameObject("Spikes");
            _spikes.transform.position = _hero.transform.position;
            var cd = _spikes.AddComponent<ContactDamage>();
            cd.team = TeamIds.Enemy;
            cd.size = new Vector2(2f, 2f);
            cd.inactiveWhileStunned = false;

            for (int i = 0; i < 10; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(4, d.Health, "one hit, then post-hit invulnerability");
        }
    }
}
