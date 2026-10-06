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
            return e;
        }

        void OnValidate()
        {
            foreach (var err in Validate()) Debug.LogWarning($"[MoveDefinition:{name}] {err}", this);
        }
    }
}
