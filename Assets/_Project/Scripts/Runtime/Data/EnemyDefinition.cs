using System.Collections.Generic;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    public enum EnemyBehavior { Walker, Hopper }

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
            return e;
        }

        void OnValidate()
        {
            foreach (var err in Validate()) Debug.LogWarning($"[EnemyDefinition:{name}] {err}", this);
        }
    }
}
