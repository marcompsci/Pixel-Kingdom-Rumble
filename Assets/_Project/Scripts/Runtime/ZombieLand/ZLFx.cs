using System.Collections.Generic;
using PKR.Core;
using UnityEngine;

namespace PKR
{
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
}
