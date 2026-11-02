using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Timed item effects on a racer: Dash Fizz (faster) and Bubble Shield (the next knock is blocked). Also the shared
    /// "knocked by an item" reaction, which a shield cancels.
    /// </summary>
    public class ItemBuffs : MonoBehaviour
    {
        public const float BoostScale = 1.6f;
        /// <summary>The racer's normal speed (CPU racers use it for catch-up).</summary>
        public float baseSpeed = 1f;
        float _boostUntil, _shieldUntil;
        SpriteRenderer _bubble;
        PlatformerMotor2D _motor;

        public bool Shielded => Time.time < _shieldUntil;
        public bool Boosted => Time.time < _boostUntil;

        public static ItemBuffs On(GameObject go)
        {
            var b = go.GetComponent<ItemBuffs>();
            return b != null ? b : go.AddComponent<ItemBuffs>();
        }

        void Awake() => _motor = GetComponent<PlatformerMotor2D>();

        public void Boost(float seconds) => _boostUntil = Time.time + seconds;

        public void Shield(float seconds, Material mat)
        {
            _shieldUntil = Time.time + seconds;
            if (_bubble == null)
            {
                var go = new GameObject("ShieldBubble");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                _bubble = go.AddComponent<SpriteRenderer>();
                _bubble.sprite = PlazaArt.Bubble();
                _bubble.sortingOrder = 30;
                if (mat != null) _bubble.sharedMaterial = mat;
            }
            _bubble.enabled = true;
        }

        /// <summary>An item hit this racer: knock them about unless their shield takes it (the shield pops).</summary>
        public static bool Knock(GameObject victim, Vector2 velocity, float lockSeconds)
        {
            if (victim == null || !victim.activeInHierarchy) return false;
            var buffs = victim.GetComponent<ItemBuffs>();
            if (buffs != null && buffs.Shielded)
            {
                buffs._shieldUntil = 0f;
                PuffEffect.Play(victim.transform.position, new Color32(150, 230, 255, 255), null, 0.25f, 4f, 0.4f);
                return false;
            }
            if (victim.TryGetComponent(out Invulnerability inv) && inv.IsActive) return false;
            if (victim.TryGetComponent(out PlatformerMotor2D motor)) motor.ApplyKnockback(velocity, lockSeconds);
            return true;
        }

        void Update()
        {
            if (_motor != null) _motor.SpeedScale = baseSpeed * (Boosted ? BoostScale : 1f);
            if (_bubble != null)
            {
                bool on = Shielded;
                _bubble.enabled = on && (Time.time + 1.2f < _shieldUntil || Mathf.Repeat(Time.time * 8f, 1f) < 0.6f);
            }
        }
    }
}
