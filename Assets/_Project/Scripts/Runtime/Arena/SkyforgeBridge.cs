using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Skyforge Arena's stage event: the center bridge stays solid, flashes a warning, then retracts and opens a
    /// gap in the middle of the stage before sliding back. Runs only while a match is live.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class SkyforgeBridge : MonoBehaviour
    {
        public float solidSeconds = 18f;
        public float warningSeconds = 2.5f;
        public float openSeconds = 7f;
        public SpriteRenderer art;
        public Color warningColor = new Color(1f, 0.45f, 0.3f, 1f);

        public BridgePhase Phase { get; private set; } = BridgePhase.Solid;
        public bool Running { get; set; }

        Collider2D _col;
        float _t;
        BridgeCycle _cycle;
        Color _baseColor = Color.white;

        void Awake()
        {
            _col = GetComponent<Collider2D>();
            if (art == null) art = GetComponent<SpriteRenderer>();
            if (art != null) _baseColor = art.color;
            _cycle = new BridgeCycle(solidSeconds, warningSeconds, openSeconds);
            Apply(BridgePhase.Solid);
        }

        public void ResetCycle()
        {
            _t = 0f;
            Apply(BridgePhase.Solid);
        }

        void Update()
        {
            if (!Running) return;
            _t += Time.deltaTime;
            var phase = _cycle.PhaseAt(_t);
            if (phase != Phase) Apply(phase);
            if (Phase == BridgePhase.Warning && art != null)
                art.color = Mathf.FloorToInt(Time.time * 8f) % 2 == 0 ? warningColor : _baseColor;
        }

        void Apply(BridgePhase phase)
        {
            Phase = phase;
            bool solid = phase != BridgePhase.Open;
            if (_col != null) _col.enabled = solid;
            if (art != null)
            {
                var c = _baseColor;
                c.a = solid ? 1f : 0.15f;
                art.color = c;
            }
        }
    }
}
