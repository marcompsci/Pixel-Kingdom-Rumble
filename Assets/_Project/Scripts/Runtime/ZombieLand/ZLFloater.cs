using System.Collections.Generic;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>A floating world label ("IMMUNE", "+AMMO", damage).</summary>
    public class ZLFloater : MonoBehaviour
    {
        float _t;
        TextMesh _tm;
        Color _c;

        static int _alive;

        /// <summary>Most labels floating at once (each is a text mesh; a busy round on a slow phone keeps fewer).</summary>
        public static int MaxAlive(PerfTier t) => t == PerfTier.High ? 30 : t == PerfTier.Medium ? 16 : 8;

        public static void Show(string text, Vector2 pos, Color c, float size = 1f)
        {
            if (_alive >= MaxAlive(PerfMonitor.Tier)) return;
            _alive++;
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

        void OnDestroy() => _alive = Mathf.Max(0, _alive - 1);

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
