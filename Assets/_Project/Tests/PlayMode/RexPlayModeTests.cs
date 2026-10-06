using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PKR.Tests
{
    /// <summary>
    /// Rex Rollo's wall ride through the real motor: flying into a wall while holding toward it rides up it,
    /// jumping kicks off the other way, and heroes without wall riding just slide down.
    /// </summary>
    public class RexPlayModeTests
    {
        const int GroundLayer = 8;
        readonly List<Object> _cleanup = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _cleanup) if (o != null) Object.Destroy(o);
            _cleanup.Clear();
        }

        T Track<T>(T o) where T : Object { _cleanup.Add(o); return o; }

        void Block(Vector2 center, Vector2 size)
        {
            var go = Track(new GameObject("Block") { layer = GroundLayer });
            go.transform.position = center;
            go.AddComponent<BoxCollider2D>().size = size;
        }

        PlatformerMotor2D Skater(float wallRideTime)
        {
            var def = Track(ScriptableObject.CreateInstance<CharacterDefinition>());
            def.id = "skater";
            def.movement.runSpeed = 9.5f;
            def.movement.wallRideTime = wallRideTime;
            def.movement.wallRideSpeed = 6.5f;
            def.movement.wallRideMinSpeed = 4f;
            def.movement.wallJumpSpeedX = 9f;

            var go = Track(new GameObject("Skater"));
            go.SetActive(false);
            go.transform.position = new Vector3(2.5f, 4f, 0f);
            go.AddComponent<Rigidbody2D>();
            go.AddComponent<CapsuleCollider2D>().size = new Vector2(0.7f, 1.4f);
            var motor = go.AddComponent<PlatformerMotor2D>();
            motor.SetDefinition(def);
            motor.SetGroundMask(1 << GroundLayer);
            go.SetActive(true);
            return motor;
        }

        [UnityTest]
        public IEnumerator WallRide_RidesUpThenWallJumpsAway()
        {
            Block(new Vector2(0f, -0.5f), new Vector2(40f, 1f));   // floor
            Block(new Vector2(5f, 5f), new Vector2(1f, 12f));      // wall face at x = 4.5
            var motor = Skater(0.45f);
            yield return new WaitForFixedUpdate();

            motor.Intent.Move = new Vector2(1f, 0f);
            motor.Body.linearVelocity = new Vector2(9f, 0f); // flying at the wall
            bool rode = false;
            for (int i = 0; i < 30 && !rode; i++)
            {
                yield return new WaitForFixedUpdate();
                rode = motor.IsWallRiding;
            }
            Assert.IsTrue(rode, "hitting the wall at speed while holding toward it starts a ride");
            yield return new WaitForFixedUpdate();
            Assert.Greater(motor.Velocity.y, 0f, "riding up the wall");

            motor.Intent.Jump.Press(Time.time);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(motor.IsWallRiding);
            Assert.Less(motor.Velocity.x, 0f, "wall jump kicks away from the wall");
            Assert.Greater(motor.Velocity.y, 0f);
        }

        [UnityTest]
        public IEnumerator HeroWithoutWallRide_NeverRides()
        {
            Block(new Vector2(0f, -0.5f), new Vector2(40f, 1f));
            Block(new Vector2(5f, 5f), new Vector2(1f, 12f));
            var motor = Skater(0f);
            yield return new WaitForFixedUpdate();

            motor.Intent.Move = new Vector2(1f, 0f);
            motor.Body.linearVelocity = new Vector2(9f, 0f);
            for (int i = 0; i < 30; i++)
            {
                yield return new WaitForFixedUpdate();
                Assert.IsFalse(motor.IsWallRiding);
            }
        }
    }
}
