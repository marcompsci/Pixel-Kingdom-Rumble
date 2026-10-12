using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Ember Wastes fire geyser: a vent that rumbles (warning), then blasts a column of fire that hurts and launches,
    /// on a fixed cycle. Timing is Core BridgeCycle (solid = dormant, warning, open = erupting).
    /// </summary>
    public class FireGeyser : MonoBehaviour
    {
        public float dormant = 2.2f, warning = 0.8f, erupting = 1.3f;
        [Tooltip("Seconds added to the clock so neighbouring geysers don't fire together.")]
        public float offset;
        public float height = 4f;
        public SpriteRenderer column;
        public SpriteRenderer vent;
        public ContactDamage hurt;

        BridgeCycle _cycle;
        float _t;

        public BridgePhase Phase => _cycle.PhaseAt(_t + offset);

        void Awake() => _cycle = new BridgeCycle(dormant, warning, erupting);

        void Update()
        {
            _t += Time.deltaTime;
            var phase = _cycle.PhaseAt(_t + offset);
            bool on = phase == BridgePhase.Open;
            if (hurt != null && hurt.enabled != on) hurt.enabled = on;
            if (column != null)
            {
                float h = on ? height : (phase == BridgePhase.Warning ? 0.5f : 0f);
                column.enabled = h > 0f;
                column.size = new Vector2(column.size.x, Mathf.Max(0.01f, h));
                column.transform.localPosition = new Vector3(0f, h * 0.5f, 0f);
                float flicker = 0.85f + 0.15f * Mathf.Sin(Time.time * 30f);
                column.color = new Color(1f, 0.55f * flicker, 0.15f, on ? 0.9f : 0.5f);
            }
            if (vent != null) vent.color = phase == BridgePhase.Warning && Mathf.Repeat(Time.time * 8f, 1f) > 0.5f
                ? new Color(1f, 0.6f, 0.3f) : new Color(0.45f, 0.3f, 0.25f);
        }
    }
}
