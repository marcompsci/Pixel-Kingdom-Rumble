using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Red (1) or blue (2) switch block: solid while its colour is active, a see-through outline otherwise. Hitting a
    /// switch flips every block in the level. A block never turns solid inside the hero (it waits until they move off).
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class SwitchBlock : MonoBehaviour
    {
        public bool red = true;
        BoxCollider2D _box;
        SpriteRenderer _sr;
        Color _color;
        bool _pending;

        bool ShouldBeSolid => red == LevelPuzzleState.RedActive;

        void Awake()
        {
            _box = GetComponent<BoxCollider2D>();
            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _color = _sr.color;
        }

        void OnEnable() { LevelPuzzleState.Changed += Apply; Apply(); }
        void OnDisable() => LevelPuzzleState.Changed -= Apply;

        void Apply()
        {
            bool solid = ShouldBeSolid;
            if (solid && HeroInside()) { _pending = true; SetSolid(false, true); return; }
            _pending = false;
            SetSolid(solid, false);
        }

        void FixedUpdate()
        {
            if (_pending && !HeroInside()) Apply();
        }

        void SetSolid(bool solid, bool waiting)
        {
            _box.enabled = solid;
            if (_sr == null) return;
            var c = _color;
            c.a = solid ? 1f : (waiting ? 0.55f : 0.22f);
            _sr.color = c;
        }

        bool HeroInside()
        {
            if (!PropUtil.TryPlayer(out _, out var b)) return false;
            var mine = _box.bounds;
            if (!_box.enabled) mine = new Bounds(transform.position, (Vector3)(Vector2)_box.size);
            return mine.Intersects(b);
        }
    }
}
