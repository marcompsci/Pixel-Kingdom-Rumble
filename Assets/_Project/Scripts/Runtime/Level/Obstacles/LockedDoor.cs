using UnityEngine;

namespace PKR
{
    /// <summary>A solid locked door. Walk into it carrying a key and it swings open (one key per door).</summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class LockedDoor : MonoBehaviour
    {
        BoxCollider2D _box;
        SpriteRenderer _sr;
        bool _open;
        float _fade = 1f;
        float _hinted;

        void Awake()
        {
            _box = GetComponent<BoxCollider2D>();
            _sr = GetComponent<SpriteRenderer>();
        }

        void FixedUpdate()
        {
            if (_open)
            {
                if (_sr != null && _fade > 0f)
                {
                    _fade = Mathf.Max(0f, _fade - Time.fixedDeltaTime * 3f);
                    var c = _sr.color; c.a = _fade; _sr.color = c;
                    if (_fade <= 0f) gameObject.SetActive(false);
                }
                return;
            }
            if (!PropUtil.TryPlayer(out _, out var b)) return;
            var mine = _box.bounds;
            mine.Expand(new Vector3(0.25f, 0.1f, 0f));
            if (!mine.Intersects(b)) return;
            if (LevelPuzzleState.UseKey())
            {
                _open = true;
                _box.enabled = false;
                PuffEffect.Play(transform.position, new Color32(255, 214, 90, 255), _sr != null ? _sr.sharedMaterial : null, 0.3f, 4f, 0.5f);
                Sfx.Play(Sfx.Secret, 0.8f, 0f);
            }
            else if (Time.time > _hinted)
            {
                _hinted = Time.time + 6f;
                EventBus<GameHint>.Raise(new GameHint { id = "door", text = "LOCKED. FIND A KEY" });
            }
        }
    }
}
