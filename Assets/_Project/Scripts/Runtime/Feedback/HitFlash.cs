using System.Collections;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Tints the fighter's sprite briefly when it takes a hit (real time, so it shows during hit stop).
    /// Placeholder for a proper white-flash shader once production art exists.
    /// </summary>
    [RequireComponent(typeof(Damageable))]
    public class HitFlash : MonoBehaviour
    {
        [SerializeField] SpriteRenderer target;
        [SerializeField] Color flashColor = new Color(1f, 0.35f, 0.35f, 1f);
        [SerializeField] Color launchColor = new Color(1f, 0.95f, 0.3f, 1f);
        [SerializeField] float duration = 0.12f;

        Damageable _damageable;
        Color _base = Color.white;
        Coroutine _running;

        void Awake()
        {
            _damageable = GetComponent<Damageable>();
            if (target == null) target = GetComponentInChildren<SpriteRenderer>();
            if (target != null) _base = target.color;
        }

        void OnEnable() => _damageable.Hit += OnHit;
        void OnDisable()
        {
            _damageable.Hit -= OnHit;
            if (target != null) target.color = _base;
        }

        /// <summary>The color the sprite returns to after a flash (cosmetic palettes change it).</summary>
        public void SetBaseColor(Color c)
        {
            _base = c;
            if (_running == null && target != null) target.color = c;
        }

        void OnHit(PKR.Core.HitResult r)
        {
            if (target == null) return;
            if (_running != null) StopCoroutine(_running);
            _running = StartCoroutine(Flash(r.isLaunch ? launchColor : flashColor));
        }

        IEnumerator Flash(Color c)
        {
            target.color = c;
            yield return new WaitForSecondsRealtime(duration);
            target.color = _base;
            _running = null;
        }
    }
}
