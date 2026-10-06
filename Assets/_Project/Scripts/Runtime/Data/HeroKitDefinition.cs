using UnityEngine;
using UnityEngine.Serialization;

namespace PKR
{
    /// <summary>
    /// Base for a hero's ability kit, read by HeroAbilities. Every hero has:
    ///  - Dodge on the ground: roll (tuned on the CharacterDefinition)
    ///  - Dodge in the air: an "air dodge" whose velocity the kit decides (Nova: 8-way dash, Brick: guard hover)
    ///  - Special in the air: an optional dive that ends in a landing shockwave (Nova: Meteor Drop, Brick: Landslide Slam)
    /// The shockwave's hit comes from the hero's Moveset.abilityImpact.
    /// </summary>
    public abstract class HeroKitDefinition : ScriptableObject
    {
        [Header("Names (shown in docs, codex and the debug panel)")]
        public string airDodgeName = "Air Dodge";
        public string diveName = "Dive";

        [Header("Air dodge (Dodge while airborne)")]
        [FormerlySerializedAs("airDashesPerAirtime")] [Min(0)] public int airDodgesPerAirtime = 1;
        [FormerlySerializedAs("airDashDuration")] public float airDodgeDuration = 0.14f;
        [FormerlySerializedAs("airDashInvulnerability")] public float airDodgeInvulnerability = 0.08f;
        [Tooltip("Fraction of the air-dodge velocity kept when it ends.")]
        [FormerlySerializedAs("airDashCarryOver")] [Range(0f, 1f)] public float airDodgeCarryOver = 0.35f;

        [Header("Dive (Special while airborne)")]
        public bool hasDive = true;
        [Tooltip("Hang in the air briefly before diving, so the move is readable.")]
        [FormerlySerializedAs("meteorStartup")] public float diveStartup = 0.1f;
        [FormerlySerializedAs("meteorFallSpeed")] public float diveFallSpeed = 26f;
        [FormerlySerializedAs("meteorLandingLag")] public float diveLandingLag = 0.2f;
        [FormerlySerializedAs("meteorShockwaveRadius")] public float diveShockwaveRadius = 1.6f;
        [Tooltip("Safety: cancel the dive if no ground is reached in this time.")]
        [FormerlySerializedAs("meteorMaxDuration")] public float diveMaxDuration = 2.5f;
        [Tooltip("Super armor while diving (hits don't knock the hero out of it unless they launch).")]
        public bool diveArmored;

        /// <summary>Velocity held during the air dodge. stick = raw move input, facing = +1/-1.</summary>
        public abstract Vector2 AirDodgeVelocity(Vector2 stick, int facing);

        /// <summary>True if the air dodge gains height or distance, so CPU fighters use it to get back to the stage.</summary>
        public virtual bool AirDodgeRecovers => true;

        /// <summary>Placeholder squash/stretch while air dodging (x, y scale).</summary>
        public virtual Vector2 AirDodgePose => new Vector2(1.3f, 0.8f);
    }
}
