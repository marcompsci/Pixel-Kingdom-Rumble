using System.Collections.Generic;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Finds Damageables touched by a hitbox. Attacks don't use hitbox colliders; they query the
    /// Hurtbox layer on their active frames, which keeps hits deterministic and easy to debug.
    /// Each Damageable is returned once even if it has several hurtboxes.
    /// </summary>
    public static class CombatQuery
    {
        static readonly Collider2D[] Buffer = new Collider2D[32];
        static readonly HashSet<Damageable> Seen = new HashSet<Damageable>();

        static ContactFilter2D Filter => new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = PKRLayers.HurtboxMask,
            useTriggers = true
        };

        public static void OverlapBox(Vector2 center, Vector2 size, List<Damageable> results) =>
            Collect(Physics2D.OverlapBox(center, size, 0f, Filter, Buffer), results);

        public static void OverlapCircle(Vector2 center, float radius, List<Damageable> results) =>
            Collect(Physics2D.OverlapCircle(center, radius, Filter, Buffer), results);

        /// <summary>Overlap for a move's hitbox with the fighter at origin facing (+1/-1).</summary>
        public static void OverlapMove(MoveDefinition move, Vector2 origin, int facing, List<Damageable> results)
        {
            Vector2 c = move.WorldHitboxCenter(origin, facing);
            if (move.shape == HitShape.Circle) OverlapCircle(c, move.hitboxRadius, results);
            else OverlapBox(c, move.hitboxSize, results);
        }

        static void Collect(int count, List<Damageable> results)
        {
            results.Clear();
            Seen.Clear();
            for (int i = 0; i < count; i++)
            {
                var col = Buffer[i];
                if (col == null || !col.TryGetComponent(out Hurtbox hb)) continue;
                var owner = hb.Owner;
                if (owner != null && Seen.Add(owner)) results.Add(owner);
            }
        }
    }
}
