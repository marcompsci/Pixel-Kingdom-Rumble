using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Rex Rollo's roller-skate kit:
    ///  - Rail Boost (Dodge in the air): a fast horizontal burst (stick picks the side) that keeps most of its speed.
    ///  - Grind Drop (Special in the air): a quick dive with a small landing shockwave.
    /// His momentum (coasting, skids) and wall ride come from his MovementStats; Momentum Ram uses MoveDefinition.speedBonus.
    /// </summary>
    [CreateAssetMenu(menuName = "PKR/Kits/Rex Rollo Kit", fileName = "RexKit")]
    public class RexKitDefinition : HeroKitDefinition
    {
        [Header("Rail Boost")]
        public float boostSpeed = 13f;
        [Tooltip("Small lift so the boost doesn't sink.")]
        public float boostLift = 2f;
        [Range(0f, 1f)] public float boostAimDeadzone = 0.3f;

        // Defaults for new assets and CreateInstance (plain field writes only; safe in a ScriptableObject constructor).
        public RexKitDefinition()
        {
            airDodgeName = "Rail Boost";
            diveName = "Grind Drop";
            airDodgesPerAirtime = 1;
            airDodgeDuration = 0.18f;
            airDodgeInvulnerability = 0.06f;
            airDodgeCarryOver = 0.7f;
            hasDive = true;
            diveStartup = 0.08f; diveFallSpeed = 24f; diveLandingLag = 0.18f; diveShockwaveRadius = 1.3f;
        }

        public override Vector2 AirDodgeVelocity(Vector2 stick, int facing)
        {
            float dir = Mathf.Abs(stick.x) > boostAimDeadzone ? Mathf.Sign(stick.x) : (facing >= 0 ? 1f : -1f);
            return new Vector2(dir * boostSpeed, boostLift);
        }

        public override Vector2 AirDodgePose => new Vector2(1.35f, 0.8f);
    }
}
