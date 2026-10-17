using System.Collections.Generic;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>A pooled bullet / spit glob. Sweeps a circle each frame against everything solid.</summary>
    public class ZLBullet : MonoBehaviour
    {
        static readonly Stack<ZLBullet> Pool = new Stack<ZLBullet>();
        static readonly RaycastHit2D[] Hits = new RaycastHit2D[8];
        static ContactFilter2D _filter;
        static Transform _parent;

        SpriteRenderer _sr;
        Vector2 _vel;
        float _life, _radius;
        int _damage;
        bool _hostile;
        ZLGun _gun;
        Collider2D _ignore;
        ZLPlayer _owner;
        bool _cosmetic;

        /// <summary>Who fired the last bullet that hit (points go to them in Last Stand co-op).</summary>
        public static ZLPlayer LastOwner { get; private set; }
        /// <summary>True while a bullet is landing its hit (riot shields only stop bullets, not blasts or melee).</summary>
        public static bool Striking { get; private set; }

        public static void Fire(Vector2 from, Vector2 dir, ZLGun gun, int damage, bool hostile, Sprite sprite, Color tint,
                                Material mat, Collider2D ignore, float speedOverride = 0f, float rangeOverride = 0f,
                                ZLPlayer owner = null, bool cosmetic = false)
        {
            if (_parent == null)
            {
                _parent = new GameObject("ZLBullets").transform;
                _filter = new ContactFilter2D { useTriggers = false };
            }
            var b = Pool.Count > 0 ? Pool.Pop() : null;
            if (b == null)
            {
                var go = new GameObject("Bullet");
                go.transform.SetParent(_parent, false);
                b = go.AddComponent<ZLBullet>();
                b._sr = go.AddComponent<SpriteRenderer>();
                b._sr.sortingOrder = ZLSort.Canopy - 1;
            }
            b.gameObject.SetActive(true);
            b.transform.position = from;
            float speed = speedOverride > 0f ? speedOverride : gun != null ? gun.speed : 10f;
            float range = rangeOverride > 0f ? rangeOverride : gun != null ? gun.range : 8f;
            b._vel = dir.normalized * speed;
            b._life = range / Mathf.Max(1f, speed);
            b._radius = gun != null && gun.shot == "comet" ? 0.3f : 0.14f;
            b._damage = damage;
            b._hostile = hostile;
            b._gun = gun;
            b._ignore = ignore;
            b._owner = owner;
            b._cosmetic = cosmetic;
            b._sr.sprite = sprite;
            b._sr.color = tint;
            if (mat != null) b._sr.sharedMaterial = mat;
            b.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            b.transform.localScale = Vector3.one * (gun != null && gun.shot == "comet" ? 1.3f : 0.8f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            Vector2 p = transform.position;
            float dist = _vel.magnitude * dt;
            int n = Physics2D.CircleCast(p, _radius, _vel.normalized, _filter, Hits, dist);
            for (int i = 0; i < n; i++)
            {
                var col = Hits[i].collider;
                if (col == null || col == _ignore) continue;
                if (_cosmetic)
                {
                    // An online guest's own shots (and other players' shots it is shown): look only, the host does the damage.
                    if (col.GetComponentInParent<ZLPlayer>() != null || col.isTrigger) continue;
                    Despawn();
                    return;
                }
                var target = col.GetComponentInParent<IZLHittable>();
                if (target != null)
                {
                    if (!target.Alive || target.Hostile == _hostile) continue; // friendly: pass through
                    LastOwner = _owner;
                    Striking = true;
                    target.Hit(_damage, _vel.normalized, _gun);
                    Striking = false;
                    LastOwner = null;
                    if (_gun != null && _gun.shot == "comet") Burst(Hits[i].point);
                    Despawn();
                    return;
                }
                if (col.GetComponentInParent<ZLPlayer>() != null || col.GetComponentInParent<ZLVehicle>() != null)
                {
                    if (!_hostile) continue;
                    var pl = col.GetComponentInParent<ZLPlayer>();
                    if (pl != null) pl.Hurt(_damage, p);
                    var v = col.GetComponentInParent<ZLVehicle>();
                    if (v != null) v.Damage(_damage);
                    Despawn();
                    return;
                }
                // a wall
                if (_gun != null && _gun.shot == "comet") Burst(Hits[i].point);
                Despawn();
                return;
            }
            transform.position = p + _vel * dt;
            _life -= dt;
            if (_life <= 0f) Despawn();
        }

        /// <summary>Comet shots burst where they land; the Singularity first drags the dead in.</summary>
        void Burst(Vector2 at)
        {
            var g = ZLGame.Current;
            if (g == null) return;
            if (_gun != null && _gun.pull > 0f && !_hostile) g.Singularity(at, _damage, _owner);
            else g.Explode(at, 2.2f, _damage / 2, !_hostile);
        }

        void Despawn()
        {
            gameObject.SetActive(false);
            Pool.Push(this);
        }

        public static void ClearPool()
        {
            Pool.Clear();
            _parent = null;
        }
    }
}
