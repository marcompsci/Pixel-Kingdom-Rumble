using UnityEngine;

namespace PKR
{
    public enum ForceKind { Wind, Current, Conveyor }

    /// <summary>
    /// Shattered Realms push zones. Wind (Sky Gardens) and water currents (Sunken Clockworks) accelerate the hero
    /// while they overlap the box, mostly in the air; a conveyor (Toy Fortress) is a floor that carries the hero
    /// standing on it. No physics callbacks: the hero's bounds are checked each step like the other props.
    /// Streak sprites (children) scroll so the push is readable.
    /// </summary>
    public class ForceZone : MonoBehaviour
    {
        public ForceKind kind = ForceKind.Wind;
        public Vector2 size = new Vector2(4f, 4f);
        [Tooltip("+1 pushes right, -1 pushes left.")]
        public int direction = 1;
        [Tooltip("Wind/current acceleration (units/s²); conveyor floor speed (units/s).")]
        public float strength = 34f;
        [Tooltip("Upward lift for currents (units/s²), so you float a little while inside.")]
        public float lift;
        public SpriteRenderer[] streaks = new SpriteRenderer[0];
        public float streakSpeed = 4f;

        float _scroll;

        public bool Contains(Bounds b)
        {
            Vector2 c = transform.position;
            if (kind == ForceKind.Conveyor)
            {
                // Standing on the belt: feet within a hair of its top, overlapping it sideways.
                float top = c.y + size.y * 0.5f;
                return b.max.x > c.x - size.x * 0.5f && b.min.x < c.x + size.x * 0.5f && Mathf.Abs(b.min.y - top) < 0.2f;
            }
            return b.max.x > c.x - size.x * 0.5f && b.min.x < c.x + size.x * 0.5f &&
                   b.max.y > c.y - size.y * 0.5f && b.min.y < c.y + size.y * 0.5f;
        }

        void FixedUpdate()
        {
            if (!PropUtil.TryPlayer(out var motor, out var b) || !Contains(b) || motor.HasOverride) return;
            if (kind == ForceKind.Conveyor)
            {
                if (motor.IsGrounded) motor.AddPush(Vector2.zero, direction * strength);
            }
            else motor.AddPush(new Vector2(direction * strength, lift));
        }

        void Update()
        {
            if (streaks == null || streaks.Length == 0) return;
            _scroll += Time.deltaTime * streakSpeed * direction;
            float half = size.x * 0.5f;
            for (int i = 0; i < streaks.Length; i++)
            {
                var s = streaks[i];
                if (s == null) continue;
                float baseX = -half + (i + 0.5f) * size.x / streaks.Length;
                float x = Mathf.Repeat(baseX + _scroll + half, size.x) - half;
                var p = s.transform.localPosition;
                s.transform.localPosition = new Vector3(x, p.y, p.z);
            }
        }

        void OnDrawGizmos()
        {
            Gizmos.color = kind == ForceKind.Conveyor ? new Color(1f, 0.8f, 0.2f, 0.5f) : new Color(0.6f, 0.9f, 1f, 0.4f);
            Gizmos.DrawWireCube(transform.position, size);
        }
    }
}
