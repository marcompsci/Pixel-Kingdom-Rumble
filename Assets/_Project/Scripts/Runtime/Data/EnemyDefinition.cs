using System.Collections.Generic;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    public enum EnemyBehavior
    {
        Walker,
        Hopper,
        /// <summary>Hovers at its spawn point and swoops at the hero (Gyro Moth).</summary>
        Flyer,
        /// <summary>Walker with a front shield that turns to face a nearby hero (Bolt Knight).</summary>
        ShieldWalker,
        /// <summary>Patrols, pauses to look back, and watches a sight cone; chases once alerted (Patrol Guard).</summary>
        Guard
    }

    /// <summary>
    /// Data for a common enemy: identity, codex text, art, stats, behavior and drops.
    /// Enemies are built at runtime from this (EnemyFactory) and pooled, so no prefab is required.
    /// </summary>
    [CreateAssetMenu(menuName = "PKR/Enemy Definition", fileName = "NewEnemy")]
    public class EnemyDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id = "new_enemy";
        public string displayName = "New Enemy";
        [TextArea(2, 5)] public string codexEntry = "";

        [Header("Art")]
        public Sprite sprite;
        public Vector2 bodySize = new Vector2(0.9f, 0.8f);

        [Header("Stats")]
        [Min(1)] public int maxHealth = 2;
        [Range(0.5f, 2.5f)] public float weight = 1f;
        [Range(0f, 1f), Tooltip("Fraction of the default run speed used while patrolling.")]
        public float moveSpeed = 0.3f;

        [Header("Behavior")]
        public EnemyBehavior behavior = EnemyBehavior.Walker;
        public float hopCooldown = 1.4f;
        public float hopRange = 7f;
        [Tooltip("Horizontal stick strength while hopping toward the player.")]
        [Range(0f, 1f)] public float hopDrift = 0.55f;

        [Header("Flyer (Behavior = Flyer)")]
        public float aggroRange = 6f;
        public float swoopSpeed = 9f;
        public float swoopTime = 0.8f;
        public float swoopWindup = 0.55f;
        public float flyerReturnSpeed = 4f;
        public float swoopCooldown = 1.4f;

        [Header("Shield (Behavior = ShieldWalker)")]
        [Tooltip("Seconds the shield stays down after a heavy hit breaks it.")]
        public float shieldBreakDuration = 1.6f;
        [Tooltip("Within this distance the knight stops and turns its shield toward the hero.")]
        public float guardRange = 4f;
        public Sprite shieldSprite;

        [Header("Guard (Behavior = Guard)")]
        public float sightRange = 6f;
        [Range(5f, 80f)] public float sightHalfAngle = 32f;
        [Tooltip("Seconds of patrolling between stops to look back the other way.")]
        public float lookBackInterval = 4f;
        public float lookBackPause = 1.1f;
        [Tooltip("Chase speed (fraction of the default run speed) once alerted.")]
        [Range(0f, 1f)] public float chaseSpeed = 0.62f;

        [Header("Contact damage")]
        public HitData contactHit = new HitData
        {
            damage = 1, pipDamage = 1, baseKnockback = 7f, exposedMultiplier = 1f,
            angleDegrees = 45f, baseHitstunFrames = 18, hitstopFrames = 4, isHeavy = false
        };

        [Header("Drops")]
        [Min(0)] public int shardDrop = 2;

        public List<string> Validate()
        {
            var e = new List<string>();
            if (string.IsNullOrWhiteSpace(id)) e.Add("id is empty");
            if (bodySize.x <= 0f || bodySize.y <= 0f) e.Add("bodySize must be positive");
            if (behavior == EnemyBehavior.Hopper && (hopCooldown <= 0f || hopRange <= 0f)) e.Add("hop cooldown/range must be positive");
            if (behavior == EnemyBehavior.Flyer && (swoopSpeed <= 0f || swoopTime <= 0f || flyerReturnSpeed <= 0f)) e.Add("flyer speeds/times must be positive");
            if (behavior == EnemyBehavior.ShieldWalker && shieldBreakDuration <= 0f) e.Add("shieldBreakDuration must be positive");
            if (behavior == EnemyBehavior.Guard && (sightRange <= 0f || lookBackInterval <= 0f)) e.Add("guard sight range / look-back interval must be positive");
            return e;
        }

        void OnValidate()
        {
            foreach (var err in Validate()) Debug.LogWarning($"[EnemyDefinition:{name}] {err}", this);
        }
    }
}
