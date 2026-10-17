using System.Collections.Generic;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// A short-lived world sprite (muzzle flashes, blood, gas puffs, explosions). Pooled, and capped by the graphics
    /// tier (PerfMonitor): past the cap, or for decorative effects on a lower tier, nothing is spawned.
    /// </summary>
    public class ZLFx : MonoBehaviour
    {
        float _life, _total;
        Vector3 _grow;
        SpriteRenderer _sr;
        Color _c;
        Vector2 _vel;
        float _drag;

        static readonly Stack<ZLFx> Pool = new Stack<ZLFx>();
        static int _decorTick;

        /// <summary>Effects alive right now (tests and the perf log).</summary>
        public static int Active { get; private set; }
        public static int Skipped { get; private set; }

        public static void Spawn(Sprite s, Vector2 pos, Color c, float life, float scale, float grow, Material mat, int order = ZLSort.Canopy - 2) =>
            Spawn(s, pos, c, life, scale, grow, mat, order, false);

        /// <param name="decor">Purely decorative (muzzle flash, hit puff): lower tiers keep only some of these.</param>
        public static void Spawn(Sprite s, Vector2 pos, Color c, float life, float scale, float grow, Material mat, int order, bool decor)
        {
            if (s == null) return;
            var tier = PerfMonitor.Tier;
            if (Active >= PerfGovernor.MaxFx(tier)) { Skipped++; return; }
            if (decor)
            {
                float keep = PerfGovernor.DecorShare(tier);
                _decorTick++;
                if (keep < 1f && (_decorTick % 10) >= Mathf.RoundToInt(keep * 10f)) { Skipped++; return; }
            }
            ZLFx fx = null;
            while (Pool.Count > 0 && fx == null) fx = Pool.Pop();   // pooled ones die with their scene
            if (fx == null)
            {
                var go = new GameObject("Fx");
                fx = go.AddComponent<ZLFx>();
                fx._sr = go.AddComponent<SpriteRenderer>();
            }
            else fx.gameObject.SetActive(true);
            var t = fx.transform;
            t.position = pos;
            t.localScale = Vector3.one * scale;
            fx._sr.sprite = s;
            fx._sr.color = c;
            fx._sr.sortingOrder = order;
            fx._sr.sharedMaterial = mat != null ? mat : fx._sr.sharedMaterial;
            fx._life = fx._total = Mathf.Max(0.01f, life);
            fx._grow = Vector3.one * grow;
            fx._c = c;
            fx._vel = Vector2.zero;
            fx._drag = 0f;
            t.rotation = Quaternion.identity;
            fx._sr.flipX = false;
            Active++;
            _last = fx;
        }

        static ZLFx _last;

        /// <summary>
        /// A hit: a white impact spark where the shot landed and a few drops of blood flung the way the shot was going
        /// (decorative: lower graphics tiers keep fewer).
        /// </summary>
        public static void HitSpray(Sprite blood, Vector2 at, Vector2 dir, Material mat, int drops = 3)
        {
            Spawn(GlowHalo.Sprite, at, new Color(1f, 1f, 0.9f, 0.85f), 0.08f, 0.35f, 2.5f, mat, ZLSort.Canopy - 2, true);
            if (blood == null) return;
            Vector2 d = dir.sqrMagnitude > 0.01f ? dir.normalized : Random.insideUnitCircle.normalized;
            for (int i = 0; i < drops; i++)
            {
                _last = null;
                Spawn(blood, at + Random.insideUnitCircle * 0.1f, Color.white, 0.35f + Random.value * 0.2f, 0.22f + Random.value * 0.15f, -0.2f, mat, ZLSort.Canopy - 2, true);
                if (_last == null) continue;
                var spread = Quaternion.Euler(0f, 0f, Random.Range(-35f, 35f)) * d;
                _last._vel = (Vector2)spread * Random.Range(3f, 6f);
                _last._drag = 9f;
            }
        }

        /// <summary>
        /// A body that falls over where a zombie died and fades (its last frame turned on its side and darkened), so a kill
        /// reads as a fall instead of a vanish. Stays as long as blood does on this graphics tier.
        /// </summary>
        public static void Corpse(Sprite s, Vector2 pos, bool flipX, Color tint, float scale, Material mat)
        {
            if (s == null) return;
            _last = null;
            float life = PerfGovernor.DecalLife(PerfMonitor.Tier) * 0.7f;
            var dark = new Color(tint.r * 0.55f, tint.g * 0.55f, tint.b * 0.55f, 0.9f);
            Spawn(s, pos + new Vector2(0f, 0.15f), dark, life, scale, 0f, mat, ZLSort.Decal + 2, false);
            if (_last == null) return;
            _last._sr.flipX = flipX;
            _last.transform.rotation = Quaternion.Euler(0f, 0f, flipX ? 90f : -90f);
        }

        /// <summary>Blood on the ground: stays for as long as the tier allows.</summary>
        public static void Decal(Sprite s, Vector2 pos, Material mat, float scale = 1.1f) =>
            Spawn(s, pos, Color.white, PerfGovernor.DecalLife(PerfMonitor.Tier), scale, 0f, mat, ZLSort.Decal + 1, false);

        void Update()
        {
            float dt = Time.deltaTime;
            _life -= dt;
            transform.localScale += _grow * dt;
            if (transform.localScale.x < 0.02f) transform.localScale = Vector3.one * 0.02f;
            if (_vel.sqrMagnitude > 0.0001f)
            {
                transform.position += (Vector3)(_vel * dt);
                _vel *= Mathf.Max(0f, 1f - _drag * dt);
            }
            var c = _c;
            c.a *= Mathf.Clamp01(_life / _total);
            _sr.color = c;
            if (_life <= 0f) Release();
        }

        void Release()
        {
            Active = Mathf.Max(0, Active - 1);
            gameObject.SetActive(false);
            if (Pool.Count < 200) Pool.Push(this); else Destroy(gameObject);
        }

        void OnDestroy()
        {
            // destroyed while still showing (the scene unloaded)
            if (gameObject.activeSelf) Active = Mathf.Max(0, Active - 1);
        }
    }
}
