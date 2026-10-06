using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Nova's sky-courier kit: an 8-way Air Dash (stick aims; neutral = forward) and the Meteor Drop dive.
    /// </summary>
    [CreateAssetMenu(menuName = "PKR/Kits/Nova Kit", fileName = "NovaKit")]
    public class NovaKitDefinition : HeroKitDefinition
    {
        [Header("Air Dash")]
        public float airDashSpeed = 15f;
        [Tooltip("Stick deflection needed to aim the dash; below this Nova dashes forward.")]
        [Range(0f, 1f)] public float airDashAimDeadzone = 0.35f;

        // Defaults for new assets and CreateInstance (plain field writes only; safe in a ScriptableObject constructor).
        public NovaKitDefinition()
        {
            airDodgeName = "Air Dash";
            diveName = "Meteor Drop";
            airDodgesPerAirtime = 1;
            airDodgeDuration = 0.14f;
            airDodgeInvulnerability = 0.08f;
            airDodgeCarryOver = 0.35f;
            hasDive = true;
            diveStartup = 0.1f; diveFallSpeed = 26f; diveLandingLag = 0.2f; diveShockwaveRadius = 1.6f;
        }

        public override Vector2 AirDodgeVelocity(Vector2 stick, int facing)
        {
            Vector2 dir;
            if (JumpPhysics.SnapToEightWay(stick.x, stick.y, airDashAimDeadzone, out var snapped))
                dir = new Vector2(snapped.x, snapped.y);
            else
                dir = new Vector2(facing >= 0 ? 1f : -1f, 0f);
            return dir * airDashSpeed;
        }
    }
}
