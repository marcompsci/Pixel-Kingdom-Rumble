using System.Collections.Generic;
using UnityEngine;

namespace PKR
{
    /// <summary>Slowly cycles a set of bulbs' brightness. Still (but lit) with Reduced motion.</summary>
    public class Twinkle : MonoBehaviour
    {
        readonly List<SpriteRenderer> _bulbs = new List<SpriteRenderer>();
        readonly List<Color> _base = new List<Color>();
        float _t;

        public void Add(SpriteRenderer sr) { _bulbs.Add(sr); _base.Add(sr.color); }

        void Update()
        {
            if (SettingsService.ReducedMotion) return;
            _t += Time.deltaTime;
            for (int i = 0; i < _bulbs.Count; i++)
            {
                if (_bulbs[i] == null) continue;
                float k = 0.65f + 0.35f * Mathf.Sin(_t * 2.2f + i * 1.7f);
                var c = _base[i];
                _bulbs[i].color = new Color(c.r * k + (1f - k) * 0.2f, c.g * k + (1f - k) * 0.2f, c.b * k + (1f - k) * 0.25f, 1f);
            }
        }
    }
}
