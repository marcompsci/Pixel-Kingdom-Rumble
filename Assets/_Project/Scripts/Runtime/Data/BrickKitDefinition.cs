using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Brick's stone-guardian kit:
    ///  - Granite Guard (Dodge in the air): he stalls in a shell of stone, invulnerable, then drops.
    ///    It is a defensive option, not a recovery; Brick recovers with his Stone Step (a mid-air jump, set by
    ///    MovementStats.airJumps on his CharacterDefinition).
    ///  - Landslide Slam (Special in the air): a heavy armored dive with a wide landing shockwave.
    /// His armored ground special (Bulwark Charge) is a normal MoveDefinition with super armor.
    /// </summary>
    [CreateAssetMenu(menuName = "PKR/Kits/Brick Kit", fileName = "BrickKit")]
    public class BrickKitDefinition : HeroKitDefinition
    {
        [Header("Granite Guard")]
        [Tooltip("Small upward drift while guarding, so the stall reads clearly.")]
        public float guardRiseSpeed = 0.6f;

        // Defaults for new assets and CreateInstance (plain field writes only; safe in a ScriptableObject constructor).
        public BrickKitDefinition()
        {
            airDodgeName = "Granite Guard";
            diveName = "Landslide Slam";
            airDodgesPerAirtime = 1;
            airDodgeDuration = 0.32f;
            airDodgeInvulnerability = 0.32f;
            airDodgeCarryOver = 0f;
            hasDive = true;
            diveStartup = 0.16f; diveFallSpeed = 22f; diveLandingLag = 0.32f; diveShockwaveRadius = 2.1f;
            diveArmored = true;
        }

        public override Vector2 AirDodgeVelocity(Vector2 stick, int facing) => new Vector2(0f, guardRiseSpeed);

        public override bool AirDodgeRecovers => false;

        public override Vector2 AirDodgePose => new Vector2(1.25f, 0.85f);
    }
}
