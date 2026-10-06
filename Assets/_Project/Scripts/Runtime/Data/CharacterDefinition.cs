using System.Collections.Generic;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Everything that defines a playable hero: identity, lore, art references and shared stats.
    /// Hero-specific ability tuning lives in a separate kit asset (e.g. NovaKitDefinition) referenced by `kit`.
    /// Rename or reskin a hero by editing this asset only.
    /// </summary>
    [CreateAssetMenu(menuName = "PKR/Character Definition", fileName = "NewCharacter")]
    public class CharacterDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable internal ID used in saves. Never change after release.")]
        public string id = "new_hero";
        public string displayName = "New Hero";
        public string tagline = "";
        [TextArea(3, 8)] public string lore = "";
        public bool unlockedByDefault;
        [Tooltip("If false, shown as a locked silhouette in Character Select (Phase 1: only Nova is playable).")]
        public bool playableInThisBuild = true;

        [Header("Placeholder art")]
        public Color placeholderColor = Color.white;
        public Sprite bodySprite;
        public Sprite portrait;

        [Header("Movement")]
        public MovementStats movement = new MovementStats();

        [Header("Defense")]
        [Min(1)] public int maxHealth = 5;
        [Tooltip("Knockback is divided by weight. 1 = average.")]
        [Range(0.5f, 2.5f)] public float weight = 1f;
        [Range(0f, 0.9f)] public float armorPercent;
        [Min(1)] public int guardPips = 3;

        [Header("Dodge (all heroes)")]
        public float dodgeSpeed = 11f;
        public float dodgeDuration = 0.22f;
        [Tooltip("Invulnerable for this long from the start of the dodge.")]
        public float dodgeInvulnerability = 0.18f;
        public float dodgeCooldown = 0.35f;

        [Header("Moves")]
        public Moveset moveset;

        [Header("Hero kit")]
        public ScriptableObject kit;

        public List<string> Validate()
        {
            var errors = movement != null ? movement.Validate() : new List<string> { "movement is null" };
            if (string.IsNullOrWhiteSpace(id)) errors.Add("id is empty");
            else if (id != id.ToLowerInvariant() || id.Contains(" ")) errors.Add("id must be lowercase with no spaces");
            if (string.IsNullOrWhiteSpace(displayName)) errors.Add("displayName is empty");
            if (dodgeDuration <= 0f) errors.Add("dodgeDuration must be > 0");
            if (dodgeInvulnerability > dodgeDuration) errors.Add("dodgeInvulnerability should not exceed dodgeDuration");
            return errors;
        }

        void OnValidate()
        {
            foreach (var e in Validate()) Debug.LogWarning($"[CharacterDefinition:{name}] {e}", this);
        }
    }
}
