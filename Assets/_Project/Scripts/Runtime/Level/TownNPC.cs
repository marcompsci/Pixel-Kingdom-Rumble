using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// A friendly local standing in a level (Mayor Opal, Rosie Crumb, Fitz Postwell, Lila Sprout, Benji Bolts). They
    /// breathe, wave, turn to face the hero, and say their line as a hint the first time the hero walks up in a level.
    /// Not solid and can't be hurt.
    /// </summary>
    public class TownNPC : MonoBehaviour
    {
        public string displayName = "Local";
        [TextArea(1, 3)] public string line = "Hello!";
        public Sprite[] frames;
        public SpriteRenderer art;
        public float talkRange = 2.2f;

        bool _spoke;
        float _clock;

        void Start() => _clock = Random.Range(0f, 3f);

        void Update()
        {
            _clock += Time.deltaTime;
            var hero = PlayerMarker.Current;
            float dx = hero != null ? hero.transform.position.x - transform.position.x : 99f;
            float dy = hero != null ? hero.transform.position.y - transform.position.y : 99f;
            bool near = Mathf.Abs(dx) < talkRange && Mathf.Abs(dy) < 2f;

            if (art != null && frames != null && frames.Length > 0)
            {
                // Idle: breathe on frames 0/1 and blink now and then; wave (frame 2) while the hero is close.
                int f = near ? (Mathf.Repeat(_clock, 0.8f) < 0.4f ? 2 : 1)
                             : (Mathf.Repeat(_clock, 3.2f) > 3.05f ? Mathf.Min(3, frames.Length - 1) : (int)(_clock * 1.6f) % 2);
                art.sprite = frames[Mathf.Clamp(f, 0, frames.Length - 1)];
                if (hero != null && Mathf.Abs(dx) < 6f) art.flipX = dx < 0f;
            }

            if (near && !_spoke)
            {
                _spoke = true;
                EventBus<GameHint>.Raise(new GameHint { id = "npc_" + displayName, text = $"{displayName.ToUpperInvariant()}: {line}" });
            }
        }
    }
}
