using UnityEngine;

namespace PKR
{
    public enum PickupKind { StarShard, Health }

    /// <summary>
    /// Pooled collectible. Placed ones hover in place; dropped ones pop out in an arc first.
    /// Collected when the player's body touches its trigger.
    /// </summary>
    public class Pickup : MonoBehaviour
    {
        public PickupKind Kind { get; private set; }
        public int Value { get; private set; }

        SpriteRenderer _sr;
        Vector2 _home;
        Vector2 _popVelocity;
        float _age;
        float _collectDelay;
        bool _popping;
        float _popFloorY;
        Material _material;

        static string PoolKey(PickupKind kind) => "pkr.pickup." + kind;

        public static Pickup Spawn(PickupKind kind, int value, Vector2 position, Sprite sprite, Material material,
                                   Vector2? popVelocity = null)
        {
            var go = PoolService.Get(PoolKey(kind), Create, maxSize: 128);
            var p = go.GetComponent<Pickup>();
            p.Begin(kind, value, position, sprite, material, popVelocity);
            return p;
        }

        static GameObject Create()
        {
            var go = new GameObject("Pickup") { layer = PKRLayers.Pickup };
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.4f;
            var visual = new GameObject("Sprite");
            visual.transform.SetParent(go.transform, false);
            visual.AddComponent<SpriteRenderer>().sortingOrder = 15;
            go.AddComponent<Pickup>();
            return go;
        }

        void Begin(PickupKind kind, int value, Vector2 position, Sprite sprite, Material material, Vector2? pop)
        {
            Kind = kind;
            Value = Mathf.Max(1, value);
            _sr = GetComponentInChildren<SpriteRenderer>();
            _sr.sprite = sprite;
            _material = material;
            if (material != null) _sr.sharedMaterial = material;
            _sr.transform.localPosition = Vector3.zero;
            transform.position = position;
            _home = position;
            _age = Random.value * 3f; // desync the bobbing
            _popping = pop.HasValue;
            _popVelocity = pop ?? Vector2.zero;
            _popFloorY = position.y - 0.3f;
            _collectDelay = _popping ? 0.3f : 0f;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            _age += dt;
            if (_collectDelay > 0f) _collectDelay -= dt;

            if (_popping)
            {
                // Simple arc, then settle where it lands (no physics needed).
                _popVelocity.y -= 22f * dt;
                Vector2 p = (Vector2)transform.position + _popVelocity * dt;
                transform.position = p;
                // Fall back to roughly where it started, then hover there.
                if (_popVelocity.y < 0f && p.y <= _popFloorY)
                {
                    _popping = false;
                    _home = p;
                }
                return;
            }
            _sr.transform.localPosition = new Vector3(0f, Mathf.Sin(_age * 3f) * 0.08f, 0f);
        }

        void OnTriggerEnter2D(Collider2D other) => TryCollect(other);
        void OnTriggerStay2D(Collider2D other) => TryCollect(other); // catches drops that spawn on the player

        void TryCollect(Collider2D other)
        {
            if (_collectDelay > 0f || !gameObject.activeInHierarchy) return;
            var rb = other.attachedRigidbody;
            if (rb == null || !rb.TryGetComponent(out PlayerMarker player)) return;

            if (Kind == PickupKind.Health && player.TryGetComponent(out Damageable d)) d.Heal(Value);
            var flow = LevelFlowController.Current;
            if (flow != null) flow.OnPickupCollected(Kind, Value);

            Color c = Kind == PickupKind.StarShard ? new Color32(255, 220, 90, 255) : new Color32(255, 110, 120, 255);
            PuffEffect.Play(transform.position, c, _material, 0.14f, 2.5f, 0.25f);
            if (Services.Haptics != null) Services.Haptics.Play(HapticStrength.Light);
            PoolService.Release(PoolKey(Kind), gameObject);
        }
    }
}
