using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// A Patrol Guard's eyes. Each physics step it checks whether the hero is inside its sight cone with a clear
    /// line (and not hidden), feeds the DetectionMeter, and tints the cone: pale when calm, amber when suspicious,
    /// red when alerted. While calm or suspicious its body doesn't hurt on contact, so the hero can slip behind it;
    /// a hit from behind before it is alerted is a silent takedown (instant defeat). A hit from the front alerts it.
    /// </summary>
    [RequireComponent(typeof(PlatformerMotor2D), typeof(Damageable))]
    public class GuardVision : MonoBehaviour
    {
        public float range = 6f;
        public float halfAngle = 32f;
        public GuardAwareness Awareness => _meter.Awareness;
        public bool Alerted => _meter.Awareness == GuardAwareness.Alerted;
        public float Meter => _meter.Value;

        readonly DetectionMeter _meter = new DetectionMeter(1.5f, 0.45f, 4f);
        PlatformerMotor2D _motor;
        Damageable _health;
        ContactDamage _contact;
        SpriteRenderer _cone;
        GuardAwareness _awarenessAtHit;

        static Sprite _coneSprite;
        static readonly Color CalmColor = new Color(1f, 1f, 0.85f, 0.16f);
        static readonly Color SuspiciousColor = new Color(1f, 0.78f, 0.2f, 0.3f);
        static readonly Color AlertColor = new Color(1f, 0.25f, 0.2f, 0.38f);
        const float EyeHeight = 0.35f;

        void Awake()
        {
            _motor = GetComponent<PlatformerMotor2D>();
            _health = GetComponent<Damageable>();
            _contact = GetComponent<ContactDamage>();
            var go = new GameObject("SightCone");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, EyeHeight, 0f);
            _cone = go.AddComponent<SpriteRenderer>();
            _cone.sprite = ConeSprite();
            _cone.sortingOrder = 2;
        }

        void OnEnable() => _health.Hit += OnHit;
        void OnDisable() => _health.Hit -= OnHit;

        /// <summary>Called by EnemyAI.Init (spawn and respawn).</summary>
        public void Configure(float sightRange, float sightHalfAngle, Material material)
        {
            range = sightRange;
            halfAngle = sightHalfAngle;
            if (material != null) _cone.sharedMaterial = material;
            _meter.Reset();
            _awarenessAtHit = GuardAwareness.Calm;
            UpdateVisual();
        }

        void FixedUpdate()
        {
            if (_health.IsDead) { _cone.enabled = false; return; }
            _cone.enabled = true;
            bool sees = false;
            float closeness = 0f;
            var player = PlayerMarker.Current;
            var tracker = StealthTracker.Current;
            if (player != null && !(tracker != null && tracker.HeroHidden && !Alerted))
            {
                Vector2 eye = (Vector2)transform.position + Vector2.up * EyeHeight;
                Vector2 target = player.transform.position;
                if (StealthRules.InCone(new Vec2(eye.x, eye.y), _motor.Facing, new Vec2(target.x, target.y), range, halfAngle))
                {
                    Vector2 d = target - eye;
                    sees = !Physics2D.Raycast(eye, d.normalized, d.magnitude, PKRLayers.GroundMask);
                    closeness = 1f - Mathf.Clamp01(d.magnitude / range);
                }
                // Brushing right up against a guard gets you noticed whichever way it faces.
                if (!sees && Vector2.Distance(target, transform.position) < 0.9f && !(tracker != null && tracker.HeroHidden))
                {
                    sees = true;
                    closeness = 1f;
                }
            }
            if (_meter.Tick(Time.fixedDeltaTime, sees, closeness) && tracker != null) tracker.ReportSpotted();
            if (_contact != null) _contact.enabled = Alerted;
            _awarenessAtHit = _meter.Awareness;
            UpdateVisual();
        }

        void OnHit(HitResult r)
        {
            if (_health.IsDead) return;
            var player = PlayerMarker.Current;
            float attackerX = player != null ? player.transform.position.x : transform.position.x;
            if (StealthRules.IsTakedown(_awarenessAtHit, _motor.Facing, transform.position.x, attackerX))
            {
                _health.Kill();
                if (StealthTracker.Current != null) StealthTracker.Current.ReportTakedown();
                if (CameraFollow2D.Main != null) CameraFollow2D.Main.Shake(0.08f, 0.1f);
                return;
            }
            if (_meter.Alert() && StealthTracker.Current != null) StealthTracker.Current.ReportSpotted();
        }

        void UpdateVisual()
        {
            if (_cone == null) return;
            var t = _cone.transform;
            float height = 2f * range * Mathf.Tan(halfAngle * Mathf.Deg2Rad);
            t.localScale = new Vector3(range * (_motor != null ? _motor.Facing : 1), height, 1f);
            Color c = Alerted ? AlertColor : Color.Lerp(CalmColor, SuspiciousColor, Mathf.Clamp01(_meter.Value * 1.4f));
            _cone.color = c;
        }

        /// <summary>A soft white wedge 1 unit long and 1 unit tall, pivot at its tip (built once at runtime).</summary>
        static Sprite ConeSprite()
        {
            if (_coneSprite != null) return _coneSprite;
            const int w = 48, h = 48;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float fx = (x + 0.5f) / w;
                float fy = Mathf.Abs((y + 0.5f) / h - 0.5f) * 2f; // 0 at the middle, 1 at the edges
                bool inside = fy <= fx;
                byte a = inside ? (byte)(255 * (1f - 0.6f * fx)) : (byte)0;
                px[y * w + x] = new Color32(255, 255, 255, a);
            }
            tex.SetPixels32(px);
            tex.Apply();
            _coneSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0f, 0.5f), w);
            _coneSprite.name = "GuardCone";
            return _coneSprite;
        }
    }
}
