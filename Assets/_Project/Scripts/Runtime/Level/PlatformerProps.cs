using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>Shared helpers for props that react to the player without physics callbacks.</summary>
    static class PropUtil
    {
        /// <summary>The player's motor and body bounds, or false if there is no live player.</summary>
        public static bool TryPlayer(out PlatformerMotor2D motor, out Bounds bounds)
        {
            motor = null;
            bounds = default;
            var p = PlayerMarker.Current;
            if (p == null || !p.TryGetComponent(out motor) || motor.Body == null) return false;
            if (p.TryGetComponent(out Damageable d) && d.IsDead) return false;
            var col = p.GetComponent<Collider2D>();
            bounds = col != null ? col.bounds : new Bounds(p.transform.position, new Vector3(0.7f, 1.4f, 0f));
            return true;
        }
    }

    /// <summary>
    /// A climbable vine. Push up (or down in the air) while overlapping it to grab on; climb with the stick,
    /// jump to let go, and climbing off the top hops you onto the ledge beside it.
    /// </summary>
    public class ClimbZone : MonoBehaviour
    {
        public Vector2 size = new Vector2(1f, 6f);
        [Tooltip("+1: the ledge at the top is to the right; -1: to the left.")]
        public int hopDirection = 1;
        public float climbSpeed = 4.2f;

        bool _climbing;
        float _cooldown;

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            if (_cooldown > 0f) _cooldown -= dt;
            if (!PropUtil.TryPlayer(out var motor, out var b)) { _climbing = false; return; }

            Vector2 c = transform.position;
            float top = c.y + size.y * 0.5f, bottom = c.y - size.y * 0.5f;
            Vector2 p = motor.Body.position;
            bool inside = Mathf.Abs(p.x - c.x) <= size.x * 0.5f + 0.35f && b.min.y < top && b.max.y > bottom;
            var intent = motor.Intent;

            if (_climbing)
            {
                if (!inside || motor.IsControlLocked) { Release(motor, null); return; }
                if (intent.Jump.Consume(Time.time))
                {
                    Release(motor, new Vector2(intent.Move.x * 6f, 9f));
                    return;
                }
                float vy = Mathf.Abs(intent.Move.y) > 0.25f ? Mathf.Sign(intent.Move.y) * climbSpeed : 0f;
                if (b.min.y >= top - 0.35f && vy > 0f)
                {
                    // Off the top: hop sideways onto the ledge.
                    motor.SetFacing(hopDirection);
                    Release(motor, new Vector2(hopDirection * 4.5f, 7.5f));
                    return;
                }
                if (vy < 0f && motor.IsGrounded) { Release(motor, Vector2.zero); return; }
                float pullX = (c.x - p.x) * 10f; // keep centered on the vine
                motor.SetOverride(new Vector2(pullX, vy));
                return;
            }

            if (inside && _cooldown <= 0f && !motor.IsControlLocked && !motor.HasOverride &&
                (intent.Move.y > 0.5f || (intent.Move.y < -0.5f && !motor.IsGrounded)))
            {
                _climbing = true;
                motor.SetOverride(Vector2.zero);
            }
        }

        void Release(PlatformerMotor2D motor, Vector2? exit)
        {
            _climbing = false;
            _cooldown = 0.3f;
            motor.ClearOverride(exit);
        }

        void OnDisable() => _climbing = false;
    }

    /// <summary>A bounce pad: landing on it launches the hero high (keeps their sideways speed).</summary>
    public class SpringPad : MonoBehaviour
    {
        [Tooltip("Launch height in world units.")]
        public float launchHeight = 9.5f;
        public Transform art;
        float _squash;

        void FixedUpdate()
        {
            if (_squash > 0f) _squash -= Time.fixedDeltaTime;
            if (art != null) art.localScale = new Vector3(1f, _squash > 0f ? 0.6f : 1f, 1f);
            if (!PropUtil.TryPlayer(out var motor, out var b)) return;
            Vector2 c = transform.position;     // pad top
            bool over = Mathf.Abs(b.center.x - c.x) <= 0.75f && b.min.y >= c.y - 0.15f && b.min.y <= c.y + 0.3f;
            if (!over || motor.Velocity.y > 0.5f || motor.IsControlLocked) return;
            var st = motor.Stats;
            float g = JumpPhysics.Gravity(st.jumpHeight, st.timeToApex);
            float vy = Mathf.Sqrt(2f * g * launchHeight);
            motor.ApplyKnockback(new Vector2(motor.Velocity.x, vy), 0f);
            _squash = 0.15f;
            if (Services.Haptics != null) Services.Haptics.Play(HapticStrength.Light);
        }
    }

    /// <summary>
    /// A solid crate with a Star Shard inside: bump it from below (head-first) to pop the shard into your
    /// pocket. Each crate counts toward the level's shard total.
    /// </summary>
    public class ShardCrate : MonoBehaviour
    {
        public SpriteRenderer art;
        public Sprite emptySprite;
        public Sprite shardSprite;
        public bool Used { get; private set; }

        void FixedUpdate()
        {
            if (Used || !PropUtil.TryPlayer(out var motor, out var b)) return;
            Vector2 c = transform.position;
            float bottom = c.y - 0.5f;
            bool underneath = Mathf.Abs(b.center.x - c.x) <= 0.7f && b.max.y >= bottom - 0.12f && b.center.y < bottom;
            if (underneath) Pop();
        }

        public void Pop()
        {
            if (Used) return;
            Used = true;
            if (art != null && emptySprite != null) art.sprite = emptySprite;
            var flow = LevelFlowController.Current;
            if (flow != null) flow.OnPickupCollected(PickupKind.StarShard, 1);
            Vector2 top = (Vector2)transform.position + Vector2.up * 0.9f;
            PuffEffect.Play(top, new Color32(255, 220, 90, 255), art != null ? art.sharedMaterial : null, 0.18f, 3f, 0.4f);
            if (Services.Haptics != null) Services.Haptics.Play(HapticStrength.Light);
            if (shardSprite != null) StartCoroutine(RiseShard(top));
        }

        System.Collections.IEnumerator RiseShard(Vector2 from)
        {
            var go = new GameObject("CrateShard");
            go.transform.position = from;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = shardSprite;
            if (art != null) sr.sharedMaterial = art.sharedMaterial;
            sr.sortingOrder = 16;
            for (float t = 0f; t < 0.45f; t += Time.deltaTime)
            {
                go.transform.position = from + Vector2.up * (t * 3f);
                var col = sr.color; col.a = 1f - t / 0.45f; sr.color = col;
                yield return null;
            }
            Destroy(go);
        }
    }
}
