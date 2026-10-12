using UnityEngine;

namespace PKR
{
    /// <summary>
    /// A solid block that shakes when the hero lands on it, falls away a moment later, and comes back after a few
    /// seconds (basalt bridges in the Ember Wastes, trapdoors in the Toy Fortress). It never reappears inside the hero.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class CrumblePlatform : MonoBehaviour
    {
        public float delay = 0.5f;
        public float respawn = 3f;

        BoxCollider2D _box;
        SpriteRenderer _sr;
        Vector3 _home;
        Color _color;
        float _timer;
        bool _shaking, _gone;

        public bool IsGone => _gone;

        void Awake()
        {
            _box = GetComponent<BoxCollider2D>();
            _sr = GetComponent<SpriteRenderer>();
            _home = transform.position;
            if (_sr != null) _color = _sr.color;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            if (_gone)
            {
                _timer -= dt;
                if (_timer <= 0f && !HeroInside()) Restore();
                return;
            }
            if (_shaking)
            {
                _timer -= dt;
                transform.position = _home + (Vector3)(Random.insideUnitCircle * 0.06f);
                if (_timer <= 0f) Fall();
                return;
            }
            if (HeroStandingOn()) { _shaking = true; _timer = delay; }
        }

        bool HeroStandingOn()
        {
            if (!PropUtil.TryPlayer(out var motor, out var b) || !motor.IsGrounded) return false;
            var mine = _box.bounds;
            return b.max.x > mine.min.x + 0.05f && b.min.x < mine.max.x - 0.05f && Mathf.Abs(b.min.y - mine.max.y) < 0.2f;
        }

        bool HeroInside()
        {
            if (!PropUtil.TryPlayer(out _, out var b)) return false;
            var size = (Vector2)_box.size;
            var rect = new Rect((Vector2)_home - size * 0.5f, size);
            return b.max.x > rect.xMin && b.min.x < rect.xMax && b.max.y > rect.yMin && b.min.y < rect.yMax;
        }

        void Fall()
        {
            _shaking = false;
            _gone = true;
            _timer = respawn;
            _box.enabled = false;
            transform.position = _home;
            if (_sr != null) _sr.color = new Color(_color.r, _color.g, _color.b, 0.15f);
            Sfx.Play(Sfx.Land, 0.5f);
        }

        void Restore()
        {
            _gone = false;
            _box.enabled = true;
            if (_sr != null) _sr.color = _color;
        }

        /// <summary>Back to solid immediately (tests, level restart).</summary>
        public void ResetNow()
        {
            _shaking = false;
            transform.position = _home;
            Restore();
        }
    }
}
