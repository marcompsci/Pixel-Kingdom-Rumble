using System.Collections.Generic;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    public enum HitShape { Box, Circle }

    /// <summary>
    /// One attack: frame data, hitbox, hit properties, movement and feedback.
    /// Hitbox offsets are authored for a fighter facing RIGHT and mirrored automatically.
    /// </summary>
    [CreateAssetMenu(menuName = "PKR/Move Definition", fileName = "NewMove")]
    public class MoveDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id = "new_move";
        public string displayName = "New Move";
        [TextArea(2, 4)] public string description = "";

        [Header("Timing (60 Hz frames)")]
        public FrameData frames = new FrameData(4, 3, 10, 4);

        [Header("Hitbox (facing right, relative to fighter center)")]
        public HitShape shape = HitShape.Box;
        public Vector2 hitboxOffset = new Vector2(0.8f, 0f);
        public Vector2 hitboxSize = new Vector2(1.0f, 0.8f);
        [Tooltip("Circle radius when shape = Circle.")]
        public float hitboxRadius = 0.8f;

        [Header("Hit")]
        public HitData hit = HitData.Light();

        [Header("Movement")]
        [Tooltip("Ground moves: hold the fighter in place except for this forward lunge speed during startup + active.")]
        public bool rootedOnGround = true;
        public float lungeSpeed = 2f;
        [Tooltip("Air moves end immediately on landing.")]
        public bool endsOnLanding = true;

        [Header("Momentum (optional)")]
        [Tooltip("Hits harder the faster the fighter was moving when the move started: knockback x (1 + speed/runSpeed * this). " +
                 "At 80%+ of run speed it also deals +1 damage and +1 Guard Pip. 0 = off.")]
        [Min(0f)] public float speedBonus;

        [Header("Super armor (optional)")]
        [Tooltip("Hits don't knock the fighter out of this move (damage and Guard Pips still apply). A launch breaks it.")]
        public bool superArmor;
        [Tooltip("First armored frame (0 = first startup frame).")]
        [Min(0)] public int armorStartFrame;
        [Tooltip("Last armored frame. -1 = through the last active frame.")]
        public int armorEndFrame = -1;

        [Header("Chaining")]
        [Tooltip("Pressing the same button inside the cancel window performs this move next.")]
        public MoveDefinition followUp;

        [Header("Projectile (optional)")]
        public bool spawnsProjectile;
        public Vector2 projectileSpawnOffset = new Vector2(0.7f, 0.1f);
        public float projectileSpeed = 12f;
        public float projectileLifetime = 0.6f;
        public float projectileRadius = 0.3f;
        public Color projectileColor = new Color32(255, 200, 80, 255);

        [Header("Trap (optional, e.g. Luma's Spark Coil)")]
        [Tooltip("Places a trap at projectileSpawnOffset on the first active frame instead of hitting directly. Uses `hit` and projectileColor.")]
        public bool spawnsTrap;
        public float trapArmDelay = 0.35f;
        public float trapLifetime = 6f;
        public float trapRadius = 0.55f;
        public float trapFallSpeed = 14f;

        [Header("Feedback")]
        public HapticStrength haptic = HapticStrength.Light;
        public float shakeAmplitude = 0f;
        public float shakeDuration = 0.12f;

        /// <summary>Hitbox center in world space for a fighter at origin facing dir (+1/-1).</summary>
        public Vector2 WorldHitboxCenter(Vector2 origin, int facing) =>
            origin + new Vector2(hitboxOffset.x * facing, hitboxOffset.y);

        public List<string> Validate()
        {
            var e = frames.Validate();
            if (string.IsNullOrWhiteSpace(id)) e.Add("id is empty");
            if (shape == HitShape.Box && (hitboxSize.x <= 0f || hitboxSize.y <= 0f)) e.Add("hitboxSize must be positive");
            if (shape == HitShape.Circle && hitboxRadius <= 0f) e.Add("hitboxRadius must be positive");
            if (followUp == this) e.Add("followUp cannot be the move itself");
            if (spawnsProjectile && (projectileSpeed <= 0f || projectileLifetime <= 0f)) e.Add("projectile speed/lifetime must be positive");
            if (spawnsProjectile && spawnsTrap) e.Add("a move can spawn a projectile or a trap, not both");
            if ((spawnsProjectile || spawnsTrap) && speedBonus > 0f) e.Add("speedBonus has no effect on projectile or trap moves");
            if (spawnsTrap && (trapLifetime <= 0f || trapRadius <= 0f || trapArmDelay < 0f)) e.Add("trap lifetime/radius must be positive, arm delay >= 0");
            return e;
        }

        void OnValidate()
        {
            foreach (var err in Validate()) Debug.LogWarning($"[MoveDefinition:{name}] {err}", this);
        }
    }
}
