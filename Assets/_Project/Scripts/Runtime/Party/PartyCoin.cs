using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Coin Rush: a coin floating over the arena. Touch it to score one. Dropped coins can't be grabbed for a moment.
    /// Coins are pooled (a round spawns and drops dozens) and the touch check reuses one buffer, so Coin Rush makes no
    /// garbage each frame.
    /// </summary>
    public class PartyCoin : MonoBehaviour
    {
        PartyArena _arena;
        float _age, _delay;
        SpriteRenderer _sr;
        static readonly System.Collections.Generic.Stack<PartyCoin> Pool = new System.Collections.Generic.Stack<PartyCoin>();
        static readonly System.Collections.Generic.List<Collider2D> Touching = new System.Collections.Generic.List<Collider2D>(8);
        static ContactFilter2D _all;

        public bool Collectable => _age >= _delay;
        public bool Live => gameObject.activeSelf;

        public static PartyCoin Spawn(PartyArena arena, Vector2 at, float delay)
        {
            while (Pool.Count > 0)
            {
                var pooled = Pool.Pop();
                if (pooled == null) continue; // its scene was unloaded
                pooled.transform.position = at;
                pooled._arena = arena; pooled._delay = delay; pooled._age = 0f;
                pooled.gameObject.SetActive(true);
                return pooled;
            }
            var go = new GameObject("Coin");
            go.transform.position = at;
            var c = go.AddComponent<PartyCoin>();
            c._arena = arena; c._delay = delay;
            c._sr = new GameObject("Art").AddComponent<SpriteRenderer>();
            c._sr.transform.SetParent(go.transform, false);
            c._sr.sprite = PlazaArt.Coin();
            c._sr.sortingOrder = 14;
            c._sr.transform.localScale = Vector3.one * 1.4f;
            if (arena.SpriteMaterial != null) c._sr.sharedMaterial = arena.SpriteMaterial;
            return c;
        }

        void Update()
        {
            _age += Time.deltaTime;
            if (_sr != null)
            {
                _sr.color = new Color(1f, 1f, 1f, Collectable ? 1f : 0.5f);
                // a coin "spins" by squashing sideways (still with Reduced motion)
                float sx = SettingsService.ReducedMotion ? 1f : Mathf.Abs(Mathf.Cos(_age * 3f)) * 0.8f + 0.2f;
                _sr.transform.localScale = new Vector3(1.4f * sx, 1.4f, 1f);
            }
            if (!Collectable || _arena == null) return;
            _all.NoFilter();
            Physics2D.OverlapCircle(transform.position, 0.45f, _all, Touching);
            foreach (var col in Touching)
            {
                var rb = col.attachedRigidbody;
                if (rb == null || !_arena.IsRacer(rb.gameObject)) continue;
                if (_arena.Collect(rb.gameObject))
                {
                    Release();
                    return;
                }
            }
        }

        /// <summary>Back to the pool (hidden, reused by the next Spawn).</summary>
        public void Release()
        {
            if (!gameObject.activeSelf) return;
            gameObject.SetActive(false);
            Pool.Push(this);
        }
    }
}
