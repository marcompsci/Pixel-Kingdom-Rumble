using System.Collections.Generic;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Pooled straight-line projectile (Nova's comet bolt). Hits the first valid Damageable it overlaps,
    /// then returns to its pool; also stops on ground or when its lifetime ends.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        public const string PoolKey = "pkr.projectile.basic";

        HitData _hit;
        int _team;
        int _direction;
        float _radius;
        float _lifeLeft;
        Vector2 _velocity;
        Damageable _owner;
        AttackRunner _source;
        SpriteRenderer _renderer;
        readonly List<Damageable> _targets = new List<Damageable>();
        static readonly Collider2D[] GroundBuffer = new Collider2D[1];

        /// <summary>Create the pooled template (no prefab needed).</summary>
        public static GameObject CreateTemplate()
        {
            var go = new GameObject("Projectile");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = UISprites.Circle;
            sr.sortingOrder = 20;
            go.AddComponent<Projectile>();
            return go;
        }

        public static Projectile Spawn(MoveDefinition move, Vector2 origin, int facing, int team,
                                       Damageable owner, AttackRunner source, Material material)
        {
            var go = PoolService.Get(PoolKey, CreateTemplate);
            var p = go.GetComponent<Projectile>();
            p.Init(move, origin, facing, team, owner, source, material);
            return p;
        }

        void Init(MoveDefinition move, Vector2 origin, int facing, int team, Damageable owner, AttackRunner source, Material material)
        {
            _hit = move.hit;
            _team = team;
            _direction = facing >= 0 ? 1 : -1;
            _radius = Mathf.Max(0.05f, move.projectileRadius);
            _lifeLeft = move.projectileLifetime;
            _velocity = new Vector2(move.projectileSpeed * _direction, 0f);
            _owner = owner;
            _source = source;

            transform.position = origin + new Vector2(move.projectileSpawnOffset.x * _direction, move.projectileSpawnOffset.y);
            _renderer = GetComponent<SpriteRenderer>();
            _renderer.color = move.projectileColor;
            if (material != null) _renderer.sharedMaterial = material;
            // UISprites.Circle is 1.28 units across at 100 PPU; scale to the hit radius.
            float d = _radius * 2f / 1.28f;
            transform.localScale = new Vector3(d * 1.4f, d, 1f); // slightly stretched: reads as moving
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            _lifeLeft -= dt;
            Vector2 pos = (Vector2)transform.position + _velocity * dt;
            transform.position = pos;

            CombatQuery.OverlapCircle(pos, _radius, _targets);
            foreach (var target in _targets)
            {
                if (target == _owner) continue;
                if (target.TakeHit(_hit, _direction, _team, out var result, pos.x))
                {
                    if (_source != null) _source.ReportProjectileHit(target, result, _hit);
                    Despawn();
                    return;
                }
            }

            var groundFilter = new ContactFilter2D { useLayerMask = true, layerMask = PKRLayers.GroundMask, useTriggers = false };
            if (_lifeLeft <= 0f || Physics2D.OverlapCircle(pos, _radius * 0.6f, groundFilter, GroundBuffer) > 0)
                Despawn();
        }

        /// <summary>Removes every live projectile (round resets in Versus).</summary>
        public static void DespawnAll()
        {
            foreach (var p in FindObjectsByType<Projectile>())
                if (p != null && p.isActiveAndEnabled) p.Despawn();
        }

        void Despawn()
        {
            _owner = null;
            _source = null;
            PoolService.Release(PoolKey, gameObject);
        }
    }
}
