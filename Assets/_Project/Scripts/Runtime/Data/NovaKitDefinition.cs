using UnityEngine;

namespace PKR
{
    /// <summary>Tuning for Nova's sky-courier kit: air dash and meteor drop.</summary>
    [CreateAssetMenu(menuName = "PKR/Kits/Nova Kit", fileName = "NovaKit")]
    public class NovaKitDefinition : ScriptableObject
    {
        [Header("Air Dash (Dodge while airborne)")]
        public float airDashSpeed = 15f;
        public float airDashDuration = 0.14f;
        [Tooltip("Fraction of dash speed kept when the dash ends.")]
        [Range(0f, 1f)] public float airDashCarryOver = 0.35f;
        [Min(0)] public int airDashesPerAirtime = 1;
        [Tooltip("Stick deflection needed to aim the dash; below this Nova dashes forward.")]
        [Range(0f, 1f)] public float airDashAimDeadzone = 0.35f;
        public float airDashInvulnerability = 0.08f;

        [Header("Meteor Drop (Special while airborne)")]
        [Tooltip("Hang in the air briefly before diving, so the move is readable.")]
        public float meteorStartup = 0.1f;
        public float meteorFallSpeed = 26f;
        public float meteorLandingLag = 0.2f;
        public float meteorShockwaveRadius = 1.6f;
        [Tooltip("Safety: cancel the dive if no ground is reached in this time.")]
        public float meteorMaxDuration = 2.5f;
    }
}
