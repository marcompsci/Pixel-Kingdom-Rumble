using UnityEngine;

namespace PKR
{
    /// <summary>
    /// A floor switch: step on it (or land on it) to flip red and blue switch blocks. It re-arms once you step off,
    /// so mazes can ask you to flip, run, flip back.
    /// </summary>
    public class SwitchButton : MonoBehaviour
    {
        public SpriteRenderer cap;
        bool _armed = true;

        void FixedUpdate()
        {
            bool on = HeroOnTop();
            if (on && _armed)
            {
                _armed = false;
                LevelPuzzleState.Toggle();
                Sfx.Play(Sfx.Click, 1f, 0f);
                if (Services.Haptics != null) Services.Haptics.Play(HapticStrength.Light);
                var mat = cap != null ? cap.sharedMaterial : null;
                PuffEffect.Play(transform.position + Vector3.up * 0.2f,
                                LevelPuzzleState.RedActive ? new Color(1f, 0.35f, 0.35f) : new Color(0.35f, 0.6f, 1f), mat, 0.15f, 3f, 0.3f);
            }
            else if (!on) _armed = true;
            if (cap != null)
            {
                cap.color = LevelPuzzleState.RedActive ? new Color(1f, 0.4f, 0.4f) : new Color(0.4f, 0.65f, 1f);
                cap.transform.localScale = new Vector3(1f, on ? 0.5f : 1f, 1f);
            }
        }

        bool HeroOnTop()
        {
            if (!PropUtil.TryPlayer(out _, out var b)) return false;
            Vector2 p = transform.position;
            return b.max.x > p.x - 0.45f && b.min.x < p.x + 0.45f && b.min.y < p.y + 0.45f && b.min.y > p.y - 0.25f;
        }
    }
}
