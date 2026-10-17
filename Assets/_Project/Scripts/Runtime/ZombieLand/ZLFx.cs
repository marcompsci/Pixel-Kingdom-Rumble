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
            Active++;
        }

        /// <summary>Blood on the ground: stays for as long as the tier allows.</summary>
        public static void Decal(Sprite s, Vector2 pos, Material mat, float scale = 1.1f) =>
            Spawn(s, pos, Color.white, PerfGovernor.DecalLife(PerfMonitor.Tier), scale, 0f, mat, ZLSort.Decal + 1, false);

        void Update()
        {
            _life -= Time.deltaTime;
            transform.localScale += _grow * Time.deltaTime;
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
