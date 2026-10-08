using System.Collections.Generic;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Pooled placed trap (Luma's Spark Coil). Falls straight down until it lands on ground, arms after a short delay,
    /// then zaps the first opponent that touches it (once) or fizzles when its lifetime ends.
    /// Each attacker can have one trap out; a new one replaces the old.
    /// </summary>
    public class SparkTrap : MonoBehaviour
    {
        public const string PoolKey = "pkr.trap.spark";

        static readonly Dictionary<int, SparkTrap> ActiveByOwner = new Dictionary<int, SparkTrap>();
        static readonly RaycastHit2D[] GroundHits = new RaycastHit2D[4];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => ActiveByOwner.Clear();

        TrapLogic _logic;
        HitData _hit;
        int _team;
        int _ownerKey;
        float _radius;
        float _fallSpeed;
        Damageable _owner;
        AttackRunner _source;
        SpriteRenderer _renderer;
        Color _color;
        float _baseScale;
        MovingPlatform _platform;
        readonly List<Damageable> _targets = new List<Damageable>();

        public bool IsArmed => _logic != null && _logic.IsArmed;
        public TrapPhase Phase => _logic != null ? _logic.Phase : TrapPhase.Spent;

        public static GameObject CreateTemplate()
        {
            var go = new GameObject("SparkTrap");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = UISprites.Ring;
            sr.sortingOrder = 15;
            go.AddComponent<SparkTrap>();
            return go;
        }

        /// <summary>Remove every live trap (arena rematch: fighters are rebuilt without a scene load).</summary>
        public static void DespawnAll()
        {
            var live = new List<SparkTrap>(ActiveByOwner.Values);
            foreach (var t in live) if (t != null && t.isActiveAndEnabled) t.Despawn();
            ActiveByOwner.Clear();
        }

        public static SparkTrap Spawn(MoveDefinition move, Vector2 origin, int facing, int team,
                                      Damageable owner, AttackRunner source, Material material)
        {
            int key = source != null ? source.RuntimeId : 0;
            if (ActiveByOwner.TryGetValue(key, out var old) && old != null && old.isActiveAndEnabled) old.Despawn();

            var go = PoolService.Get(PoolKey, CreateTemplate);
            var trap = go.GetComponent<SparkTrap>();
            trap.Init(move, origin, facing, team, owner, source, material, key);
            ActiveByOwner[key] = trap;
            return trap;
        }

        void Init(MoveDefinition move, Vector2 origin, int facing, int team, Damageable owner, AttackRunner source,
                  Material material, int key)
        {
            int dir = facing >= 0 ? 1 : -1;
            _logic = new TrapLogic(move.trapArmDelay, move.trapLifetime);
            _hit = move.hit;
            _team = team;
            _ownerKey = key;
            _radius = Mathf.Max(0.1f, move.trapRadius);
            _fallSpeed = Mathf.Max(1f, move.trapFallSpeed);
            _owner = owner;
            _source = source;
            _color = move.projectileColor;
            _platform = null;

            transform.position = origin + new Vector2(move.projectileSpawnOffset.x * dir, move.projectileSpawnOffset.y);
            _renderer = GetComponent<SpriteRenderer>();
            _renderer.color = _color;
            _renderer.enabled = true;
            if (material != null) _renderer.sharedMaterial = material;
            // UISprites.Ring is 1.28 units across at 100 PPU; scale to the trigger radius.
            _baseScale = _radius * 2f / 1.28f;
            transform.localScale = new Vector3(_baseScale, _baseScale * 0.5f, 1f); // flat coil
        }

        void FixedUpdate()
        {
            if (_logic == null) return;
            float dt = Time.fixedDeltaTime;
            _logic.Tick(dt);

            if (_logic.Phase == TrapPhase.Falling)
            {
                float step = _fallSpeed * dt;
                var filter = new ContactFilter2D { useLayerMask = true, layerMask = PKRLayers.GroundMask, useTriggers = false };
                Vector2 pos = transform.position;
                int n = Physics2D.Raycast(pos, Vector2.down, filter, GroundHits, step + _radius * 0.5f);
                bool landed = false;
                for (int i = 0; i < n && !landed; i++)
                {
                    var h = GroundHits[i];
                    // Started inside a one-way plank (thrown while jumping up through it): ignore it, like the motor does.
                    if (h.distance <= 0f && h.collider != null && h.collider.usedByEffector) continue;
                    if (h.normal.y <= 0.5f) continue;
                    transform.position = h.point + new Vector2(0f, _radius * 0.5f);
                    if (h.collider != null) h.collider.TryGetComponent(out _platform);
                    _logic.Land();
                    landed = true;
                }
                if (!landed) transform.position = pos + Vector2.down * step;
            }
            else if (_platform != null)
            {
                // Ride a moving platform it landed on.
                transform.position = (Vector2)transform.position + _platform.Velocity * dt;
            }

            if (_logic.IsArmed)
            {
                CombatQuery.OverlapCircle(transform.position, _radius, _targets);
                foreach (var target in _targets)
                {
                    if (target == _owner || !target.CanBeHitBy(_team)) continue;
                    if (!_logic.TryTrigger()) break;
                    int dir = target.transform.position.x >= transform.position.x ? 1 : -1;
                    if (target.TakeHit(_hit, dir, _team, out var result, transform.position.x) && _source != null)
                        _source.ReportProjectileHit(target, result, _hit);
                    PuffEffect.Play(transform.position, _color, _renderer.sharedMaterial, 0.3f, 6f, 0.35f);
                    break;
                }
            }

            if (_logic.IsSpent) { Despawn(); return; }
            UpdateVisual();
        }

        void UpdateVisual()
        {
            float t = Time.time;
            switch (_logic.Phase)
            {
                case TrapPhase.Arming:
                    _renderer.enabled = Mathf.FloorToInt(t * 16f) % 2 == 0; // fast blink: not live yet
                    break;
                case TrapPhase.Armed:
                    _renderer.enabled = _logic.TimeLeft > 1f || Mathf.FloorToInt(t * 8f) % 2 == 0; // blink before fizzling
                    float pulse = 1f + 0.12f * Mathf.Sin(t * 14f);
                    transform.localScale = new Vector3(_baseScale * pulse, _baseScale * 0.5f * pulse, 1f);
                    break;
                default:
                    _renderer.enabled = true;
                    break;
            }
        }

        void Despawn()
        {
            if (ActiveByOwner.TryGetValue(_ownerKey, out var current) && current == this) ActiveByOwner.Remove(_ownerKey);
            _logic = null;
            _platform = null;
            _owner = null;
            _source = null;
            PoolService.Release(PoolKey, gameObject);
        }
    }
}
