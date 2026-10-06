using System.Collections.Generic;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Hidden room. A "fake wall" of cover sprites (no colliders) hides it; when the player steps inside the
    /// trigger the covers fade out and the secret is counted once.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class SecretArea : MonoBehaviour
    {
        public string secretId = "secret_1";
        public List<SpriteRenderer> covers = new List<SpriteRenderer>();
        [Range(0f, 1f)] public float revealedAlpha = 0.2f;
        public float fadeSpeed = 4f;

        bool _inside;

        void Awake() => GetComponent<Collider2D>().isTrigger = true;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!TriggerUtil.IsPlayer(other, out _)) return;
            _inside = true;
            var flow = LevelFlowController.Current;
            if (flow != null) flow.FindSecret(secretId);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (TriggerUtil.IsPlayer(other, out _)) _inside = false;
        }

        void Update()
        {
            float target = _inside ? revealedAlpha : 1f;
            foreach (var c in covers)
            {
                if (c == null) continue;
                var col = c.color;
                col.a = Mathf.MoveTowards(col.a, target, fadeSpeed * Time.deltaTime);
                c.color = col;
            }
        }
    }
}
