using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Placeholder "animation" for geometric sprites: facing flip, squash and stretch on jump/land,
    /// stretch while dashing, and blink during respawn i-frames. Lives on the root; drives a child sprite.
    /// Replace with an Animator when production art arrives; the motor/ability events stay the same.
    /// </summary>
    public class FighterVisual : MonoBehaviour
    {
        [SerializeField] Transform body;
        [SerializeField] SpriteRenderer bodyRenderer;
        [SerializeField] float squashRecover = 12f;

        PlatformerMotor2D _motor;
        HeroAbilities _abilities;
        Invulnerability _invuln;
        Vector3 _baseScale = Vector3.one;
        Vector2 _squash = Vector2.one;

        void Awake()
        {
            _motor = GetComponent<PlatformerMotor2D>();
            _abilities = GetComponent<HeroAbilities>();
            _invuln = GetComponent<Invulnerability>();
            if (body == null && transform.childCount > 0) body = transform.GetChild(0);
            if (bodyRenderer == null && body != null) bodyRenderer = body.GetComponent<SpriteRenderer>();
            if (body != null) _baseScale = body.localScale;
        }

        void OnEnable()
        {
            if (_motor == null) return;
            _motor.Jumped += OnJumped;
            _motor.Landed += OnLanded;
        }

        void OnDisable()
        {
            if (_motor == null) return;
            _motor.Jumped -= OnJumped;
            _motor.Landed -= OnLanded;
        }

        void OnJumped() => _squash = new Vector2(0.75f, 1.3f);

        void OnLanded(float impactSpeed)
        {
            float t = Mathf.Clamp01(impactSpeed / 20f);
            _squash = new Vector2(Mathf.Lerp(1.1f, 1.35f, t), Mathf.Lerp(0.9f, 0.65f, t));
        }

        void LateUpdate()
        {
            if (body == null || _motor == null) return;

            Vector2 target = Vector2.one;
            if (_abilities != null)
            {
                switch (_abilities.Current)
                {
                    case HeroAbilities.State.AirDodge: target = _abilities.Kit.AirDodgePose; break;
                    case HeroAbilities.State.Roll: target = new Vector2(1.2f, 0.7f); break;
                    case HeroAbilities.State.DiveFall: target = new Vector2(0.7f, 1.35f); break;
                }
            }
            _squash = Vector2.Lerp(_squash, target, 1f - Mathf.Exp(-squashRecover * Time.deltaTime));

            body.localScale = new Vector3(
                _baseScale.x * _squash.x * _motor.Facing,
                _baseScale.y * _squash.y,
                _baseScale.z);

            if (bodyRenderer != null)
            {
                bool hidden = _invuln != null && _invuln.IsBlinking && Mathf.FloorToInt(Time.time * 20f) % 2 == 0;
                bodyRenderer.enabled = !hidden;
            }
        }
    }
}
