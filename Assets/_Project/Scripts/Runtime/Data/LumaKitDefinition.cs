using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Luma's inventor kit:
    ///  - Magnet Hop (Dodge in the air): her gloves repel off the air, popping her up and forward. A good recovery.
    ///  - No dive: her air Special is Spark Coil, a trap she throws down (a MoveDefinition with spawnsTrap).
    /// Her ground Special, Magnet Tether, is a projectile whose hit pulls the target toward her.
    /// </summary>
    [CreateAssetMenu(menuName = "PKR/Kits/Luma Kit", fileName = "LumaKit")]
    public class LumaKitDefinition : HeroKitDefinition
    {
        [Header("Magnet Hop")]
        public float hopUpSpeed = 12f;
        public float hopForwardSpeed = 6f;
        [Tooltip("Stick deflection needed to pick the hop direction; below this she hops the way she faces.")]
        [Range(0f, 1f)] public float hopAimDeadzone = 0.3f;

        // Defaults for new assets and CreateInstance (plain field writes only; safe in a ScriptableObject constructor).
        public LumaKitDefinition()
        {
            airDodgeName = "Magnet Hop";
            diveName = "";
            airDodgesPerAirtime = 1;
            airDodgeDuration = 0.16f;
            airDodgeInvulnerability = 0.06f;
            airDodgeCarryOver = 0.5f;
            hasDive = false;
        }

        public override Vector2 AirDodgeVelocity(Vector2 stick, int facing)
        {
            float dir = Mathf.Abs(stick.x) > hopAimDeadzone ? Mathf.Sign(stick.x) : (facing >= 0 ? 1f : -1f);
            return new Vector2(dir * hopForwardSpeed, hopUpSpeed);
        }

        public override Vector2 AirDodgePose => new Vector2(0.85f, 1.25f);
    }
}
