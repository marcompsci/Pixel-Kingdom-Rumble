using System.Collections;
using NUnit.Framework;
using PKR.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace PKR.Tests
{
    /// <summary>
    /// Physics-level checks for PlatformerMotor2D. Each test builds a floor + a capsule character in an empty scene.
    /// Uses fallback MovementStats (no definition asset needed).
    /// </summary>
    public class MotorPlayModeTests
    {
        const int GroundLayer = 8;
        GameObject _floor, _hero;
        PlatformerMotor2D _motor;
        readonly MovementStats _stats = new MovementStats();

        [SetUp]
        public void SetUp()
        {
            _floor = new GameObject("TestFloor") { layer = GroundLayer };
            _floor.transform.position = new Vector3(0f, -0.5f, 0f);
            _floor.AddComponent<BoxCollider2D>().size = new Vector2(60f, 1f);

            _hero = new GameObject("TestHero");
            _hero.transform.position = new Vector3(0f, 2f, 0f);
            _hero.AddComponent<Rigidbody2D>();
            var col = _hero.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(0.7f, 1.4f);
            _motor = _hero.AddComponent<PlatformerMotor2D>();
            _motor.SetGroundMask(1 << GroundLayer);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_hero);
            Object.Destroy(_floor);
        }

        IEnumerator WaitUntilGrounded(float timeout = 2f)
        {
            float t = 0f;
            while (!_motor.IsGrounded && t < timeout)
            {
                yield return new WaitForFixedUpdate();
                t += Time.fixedDeltaTime;
            }
        }

        [UnityTest]
        public IEnumerator FallsAndLandsOnFloor()
        {
            yield return WaitUntilGrounded();
            Assert.IsTrue(_motor.IsGrounded, "never landed");
            Assert.AreEqual(0.7f, _motor.Body.position.y, 0.08f, "should rest on the floor surface");
        }

        [UnityTest]
        public IEnumerator HeldJump_ReachesAuthoredHeight()
        {
            yield return WaitUntilGrounded();
            float startY = _motor.Body.position.y;
            _motor.Intent.Jump.Press(Time.time);
            _motor.Intent.Jump.SetHeld(true);

            float peak = startY;
            for (int i = 0; i < 90; i++)
            {
                yield return new WaitForFixedUpdate();
                peak = Mathf.Max(peak, _motor.Body.position.y);
            }
            float height = peak - startY;
            // Apex hang adds a little height on top of the authored value.
            Assert.GreaterOrEqual(height, _stats.jumpHeight * 0.9f);
            Assert.LessOrEqual(height, _stats.jumpHeight * 1.3f);
        }

        [UnityTest]
        public IEnumerator ReleasedJump_IsLowerThanHeldJump()
        {
            yield return WaitUntilGrounded();
            float startY = _motor.Body.position.y;
            _motor.Intent.Jump.Press(Time.time);
            _motor.Intent.Jump.SetHeld(true);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            _motor.Intent.Jump.SetHeld(false); // tap

            float peak = startY;
            for (int i = 0; i < 60; i++)
            {
                yield return new WaitForFixedUpdate();
                peak = Mathf.Max(peak, _motor.Body.position.y);
            }
            Assert.Less(peak - startY, _stats.jumpHeight * 0.6f);
        }

        [UnityTest]
        public IEnumerator HoldingRight_ReachesRunSpeed()
        {
            yield return WaitUntilGrounded();
            _motor.Intent.Move = Vector2.right;
            for (int i = 0; i < 40; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(_stats.runSpeed, _motor.Velocity.x, 0.25f);
            Assert.AreEqual(1, _motor.Facing);
        }

        [UnityTest]
        public IEnumerator Knockback_LocksControlThenReturnsIt()
        {
            yield return WaitUntilGrounded();
            _motor.ApplyKnockback(new Vector2(-6f, 6f), 0.2f);
            Assert.IsTrue(_motor.IsControlLocked);
            for (int i = 0; i < 20; i++) yield return new WaitForFixedUpdate();
            Assert.IsFalse(_motor.IsControlLocked);
        }
    }
}
