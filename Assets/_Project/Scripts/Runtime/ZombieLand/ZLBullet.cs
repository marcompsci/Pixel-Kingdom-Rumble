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

        public static void Fire(Vector2 from, Vector2 dir, ZLGun gun, int damage, bool hostile, Sprite sprite, Color tint,
                                Material mat, Collider2D ignore, float speedOverride = 0f, float rangeOverride = 0f)
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
                var target = col.GetComponentInParent<IZLHittable>();
                if (target != null)
                {
                    if (!target.Alive || target.Hostile == _hostile) continue; // friendly: pass through
                    target.Hit(_damage, _vel.normalized, _gun);
                    if (_gun != null && _gun.shot == "comet") ZLGame.Current?.Explode(Hits[i].point, 2.2f, _damage / 2, !_hostile);
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
                if (_gun != null && _gun.shot == "comet") ZLGame.Current?.Explode(Hits[i].point, 2.2f, _damage / 2, !_hostile);
                Despawn();
                return;
            }
            transform.position = p + _vel * dt;
            _life -= dt;
            if (_life <= 0f) Despawn();
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
