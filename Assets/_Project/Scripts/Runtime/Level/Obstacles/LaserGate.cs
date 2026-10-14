using UnityEngine;

namespace PKR
{
    /// <summary>
    /// A vertical energy beam that switches on and off on a timer (flickering as a warning before it turns on).
    /// Dash through in the gap. Neighbouring gates get offsets from the builder so they can be read as a rhythm.
    /// </summary>
    public class LaserGate : MonoBehaviour
    {
        public float on = 1.4f, off = 1.4f, warn = 0.45f, offset;
        public SpriteRenderer beam;
        public SpriteRenderer[] emitters;
        public ContactDamage hurt;
        float _t;

        public bool IsOn { get; private set; }

        void Awake() => _t = offset;

        void Update()
        {
            _t += Time.deltaTime;
            float cycle = on + off;
            float t = Mathf.Repeat(_t, cycle);
            IsOn = t < on;
            bool warning = !IsOn && t > cycle - warn;
            if (hurt != null && hurt.enabled != IsOn) hurt.enabled = IsOn;
            if (beam != null)
            {
                float flicker = 0.85f + 0.15f * Mathf.Sin(Time.time * 40f);
                beam.enabled = IsOn || (warning && Mathf.Repeat(Time.time * 12f, 1f) > 0.5f);
                beam.color = IsOn ? new Color(1f, 0.25f * flicker, 0.35f, 0.9f) : new Color(1f, 0.5f, 0.5f, 0.3f);
                beam.transform.localScale = new Vector3(IsOn ? 1f : 0.35f, 1f, 1f);
            }
            if (emitters != null)
                foreach (var e in emitters) if (e != null) e.color = IsOn || warning ? new Color(1f, 0.45f, 0.45f) : new Color(0.55f, 0.55f, 0.65f);
        }
    }
}
