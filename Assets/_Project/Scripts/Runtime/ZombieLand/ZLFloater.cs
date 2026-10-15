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
