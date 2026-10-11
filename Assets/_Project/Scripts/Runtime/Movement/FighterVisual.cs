using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Procedural animation for single-frame pixel sprites: facing flip, squash and stretch on jump/land,
    /// a run cycle (bob + lean), idle breathing, air tilt, an attack lunge, a hit flinch, a K.O. pose,
    /// and blink during respawn i-frames. Lives on the root; drives a child sprite (bottom pivot, so it
    /// rotates around the feet). Replace with an Animator when frame-by-frame art arrives.
    /// </summary>
    public class FighterVisual : MonoBehaviour
    {
        [SerializeField] Transform body;
        [SerializeField] SpriteRenderer bodyRenderer;
        [SerializeField] float squashRecover = 12f;

        PlatformerMotor2D _motor;
        HeroAbilities _abilities;
        Invulnerability _invuln;
        AttackRunner _attacks;
        Damageable _health;
        Vector3 _baseScale = Vector3.one;
        Vector3 _basePos;
        Vector2 _squash = Vector2.one;
        float _runPhase, _tilt, _flinch, _breath;
        bool _down;

        /// <summary>K.O. pose (Versus): the body tips over onto its back until cleared.</summary>
        public void SetDownPose(bool down) => _down = down;

        void Awake()
        {
            _motor = GetComponent<PlatformerMotor2D>();
            _abilities = GetComponent<HeroAbilities>();
            _invuln = GetComponent<Invulnerability>();
            if (body == null && transform.childCount > 0) body = transform.GetChild(0);
            if (bodyRenderer == null && body != null) bodyRenderer = body.GetComponent<SpriteRenderer>();
            if (body != null) { _baseScale = body.localScale; _basePos = body.localPosition; }
            _attacks = GetComponent<AttackRunner>();
            _health = GetComponent<Damageable>();
            _breath = Random.value * 6f;
        }

        void OnEnable()
        {
            if (_motor == null) return;
            _motor.Jumped += OnJumped;
            _motor.Landed += OnLanded;
            if (_health != null) _health.Hit += OnHit;
        }

        void OnDisable()
        {
            if (_motor == null) return;
            _motor.Jumped -= OnJumped;
            _motor.Landed -= OnLanded;
            if (_health != null) _health.Hit -= OnHit;
        }

        void OnHit(PKR.Core.HitResult r)
        {
            _flinch = 1f;
            _squash = new Vector2(1.2f, 0.85f);
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
            bool abilityIdle = _abilities == null || _abilities.Current == HeroAbilities.State.None;
            if (abilityIdle && _motor.IsWallRiding) target = new Vector2(0.8f, 1.2f);
            else if (abilityIdle && _motor.IsSkidding) target = new Vector2(1.15f, 0.85f);
            _squash = Vector2.Lerp(_squash, target, 1f - Mathf.Exp(-squashRecover * Time.deltaTime));

            float dt = Time.deltaTime;
            float vx = _motor.Body != null ? _motor.Velocity.x : 0f;
            float vy = _motor.Body != null ? _motor.Velocity.y : 0f;
            float speed01 = Mathf.Clamp01(Mathf.Abs(vx) / 7.5f);
            bool attacking = _attacks != null && _attacks.IsAttacking;

            // Run cycle: a quick bob and a forward lean that grow with speed; idle: slow breathing.
            float bob = 0f, scaleY = 1f, tiltTarget = 0f;
            if (_motor.IsGrounded && abilityIdle && speed01 > 0.08f)
            {
                _runPhase += dt * Mathf.Lerp(8f, 18f, speed01);
                bob = Mathf.Abs(Mathf.Sin(_runPhase)) * 0.09f * speed01;
                tiltTarget = -6f * speed01;
            }
            else if (_motor.IsGrounded && abilityIdle)
            {
                _breath += dt * 2.2f;
                scaleY = 1f + Mathf.Sin(_breath) * 0.025f;
            }
            else if (!_motor.IsGrounded && abilityIdle)
            {
                tiltTarget = Mathf.Clamp(-vy * 1.2f, -12f, 12f) * 0.5f - 4f; // nose up rising, down falling
            }
            if (attacking) { tiltTarget = -14f; target = new Vector2(1.12f, 0.94f); }
            if (_flinch > 0f) { tiltTarget = 16f * _flinch; _flinch = Mathf.Max(0f, _flinch - dt * 4f); }
            _tilt = Mathf.Lerp(_tilt, tiltTarget, 1f - Mathf.Exp(-14f * dt));

            if (_down)
            {
                body.localRotation = Quaternion.Euler(0f, 0f, _motor.Facing > 0 ? 90f : -90f);
                body.localPosition = _basePos + new Vector3(0f, 0.25f, 0f);
                body.localScale = new Vector3(_baseScale.x * _motor.Facing, _baseScale.y, _baseScale.z);
            }
            else
            {
                body.localRotation = Quaternion.Euler(0f, 0f, _tilt * _motor.Facing);
                body.localPosition = _basePos + new Vector3(0f, bob, 0f);
                body.localScale = new Vector3(
                    _baseScale.x * _squash.x * _motor.Facing,
                    _baseScale.y * _squash.y * scaleY,
                    _baseScale.z);
            }

            if (bodyRenderer != null)
            {
                bool hidden = _invuln != null && _invuln.IsBlinking && Mathf.FloorToInt(Time.time * 20f) % 2 == 0;
                bodyRenderer.enabled = !hidden;
            }
        }
    }
}
