using System.Collections.Generic;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>Anything a bullet can hit in Zombie Land.</summary>
    public interface IZLHittable
    {
        /// <summary>True for the dead (zombies, nests, the boss); false for the hero's side.</summary>
        bool Hostile { get; }
        bool Alive { get; }
        void Hit(int damage, Vector2 dir, ZLGun gun);
    }

    /// <summary>Something the hero can USE (vehicles, cases, missions, lairs...).</summary>
    public interface IZLUse
    {
        Vector2 UsePos { get; }
        float UseRange { get; }
        bool CanUse { get; }
        string Prompt { get; }
        void Use(ZLPlayer p);
    }

    /// <summary>Simple frame animation for a SpriteRenderer.</summary>
    public class ZLAnim
    {
        public Sprite[] idle = new Sprite[0], move = new Sprite[0], hurt = new Sprite[0];
        public float fps = 8f;

        public Sprite Pick(bool moving, bool hurtNow, float t)
        {
            var clip = hurtNow && hurt.Length > 0 ? hurt : moving && move.Length > 0 ? move : idle.Length > 0 ? idle : move;
            if (clip == null || clip.Length == 0) return null;
            return clip[(int)(t * fps) % clip.Length];
        }
    }

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

    /// <summary>A short-lived world sprite (muzzle flashes, blood, gas puffs, explosions).</summary>
    public class ZLFx : MonoBehaviour
    {
        float _life, _total;
        Vector3 _grow;
        SpriteRenderer _sr;
        Color _c;

        public static void Spawn(Sprite s, Vector2 pos, Color c, float life, float scale, float grow, Material mat, int order = ZLSort.Canopy - 2)
        {
            if (s == null) return;
            var go = new GameObject("Fx");
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * scale;
            var fx = go.AddComponent<ZLFx>();
            fx._sr = go.AddComponent<SpriteRenderer>();
            fx._sr.sprite = s;
            fx._sr.color = c;
            fx._sr.sortingOrder = order;
            if (mat != null) fx._sr.sharedMaterial = mat;
            fx._life = fx._total = life;
            fx._grow = Vector3.one * grow;
            fx._c = c;
        }

        void Update()
        {
            _life -= Time.deltaTime;
            transform.localScale += _grow * Time.deltaTime;
            var c = _c;
            c.a *= Mathf.Clamp01(_life / _total);
            _sr.color = c;
            if (_life <= 0f) Destroy(gameObject);
        }
    }

    /// <summary>A floating world label ("IMMUNE", "+AMMO", damage).</summary>
    public class ZLFloater : MonoBehaviour
    {
        float _t;
        TextMesh _tm;
        Color _c;

        public static void Show(string text, Vector2 pos, Color c, float size = 1f)
        {
            var go = new GameObject("Floater");
            go.transform.position = pos + new Vector2(0f, 0.6f);
            var f = go.AddComponent<ZLFloater>();
            f._tm = go.AddComponent<TextMesh>();
            f._tm.text = text;
            f._tm.font = UISprites.DisplayFont;
            f._tm.fontSize = 48;
            f._tm.characterSize = 0.05f * size;
            f._tm.anchor = TextAnchor.MiddleCenter;
            f._tm.color = c;
            f._c = c;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = UISprites.DisplayFont.material;
            mr.sortingOrder = ZLSort.Overhead;
        }

        void Update()
        {
            _t += Time.deltaTime;
            transform.position += Vector3.up * Time.deltaTime * 0.8f;
            var c = _c;
            c.a = Mathf.Clamp01(1.4f - _t);
            _tm.color = c;
            if (_t > 1.4f) Destroy(gameObject);
        }
    }
}
