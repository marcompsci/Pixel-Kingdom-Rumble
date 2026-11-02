using UnityEngine;

namespace PKR
{
    /// <summary>Coin Rush: a coin floating over the arena. Touch it to score one. Dropped coins can't be grabbed for a moment.</summary>
    public class PartyCoin : MonoBehaviour
    {
        PartyArena _arena;
        float _age, _delay;
        SpriteRenderer _sr;

        public bool Collectable => _age >= _delay;

        public static PartyCoin Spawn(PartyArena arena, Vector2 at, float delay)
        {
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
            foreach (var col in Physics2D.OverlapCircleAll(transform.position, 0.45f))
            {
                var rb = col.attachedRigidbody;
                if (rb == null || !_arena.IsRacer(rb.gameObject)) continue;
                if (_arena.Collect(rb.gameObject))
                {
                    Destroy(gameObject);
                    return;
                }
            }
        }
    }
}
