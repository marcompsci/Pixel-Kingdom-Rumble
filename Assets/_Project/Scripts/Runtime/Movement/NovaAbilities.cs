using System;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Nova's movement kit:
    ///  - Dodge on ground  -> quick roll with brief invulnerability (shared by all heroes)
    ///  - Dodge in air     -> 8-way air dash (stick aims; neutral = forward), limited per airtime
    ///  - Special in air   -> Meteor Drop: short hang, straight dive, landing shockwave (damage wired in increment 3)
    /// Runs before the motor each physics step so overrides apply in the same step.
    /// </summary>
    [RequireComponent(typeof(PlatformerMotor2D))]
    [DefaultExecutionOrder(-10)]
    public class NovaAbilities : MonoBehaviour
    {
        public enum State { None, Roll, AirDash, MeteorStartup, MeteorFall, MeteorLanding }

        [SerializeField] NovaKitDefinition kit;

        public State Current { get; private set; } = State.None;
        public bool IsBusy => Current != State.None;
        public NovaKitDefinition Kit => kit;

        /// <summary>Fired on meteor impact with the landing position and shockwave radius.</summary>
        public event Action<Vector2, float> MeteorImpact;
        public event Action AirDashStarted;
        public event Action RollStarted;

        PlatformerMotor2D _motor;
        AttackRunner _attacks;
        Invulnerability _invuln;
        float _stateTimer;
        float _dodgeCooldown;
        int _airDashesLeft;
        Vector2 _dashVelocity;

        void Awake()
        {
            _motor = GetComponent<PlatformerMotor2D>();
            _attacks = GetComponent<AttackRunner>();
            _invuln = GetComponent<Invulnerability>();
            if (_invuln == null) _invuln = gameObject.AddComponent<Invulnerability>();
            if (kit == null && _motor.Definition != null) kit = _motor.Definition.kit as NovaKitDefinition;
            if (kit == null)
            {
                Debug.LogWarning("[NovaAbilities] No NovaKitDefinition assigned; using defaults.", this);
                kit = ScriptableObject.CreateInstance<NovaKitDefinition>();
            }
        }

        void OnEnable()
        {
            _motor.Landed += OnLanded;
            _motor.LeftGround += OnLeftGround;
            _motor.KnockedBack += Cancel;
        }

        void OnDisable()
        {
            _motor.Landed -= OnLanded;
            _motor.LeftGround -= OnLeftGround;
            _motor.KnockedBack -= Cancel;
        }

        // Rolling off a ledge ends the roll so Nova falls instead of gliding.
        void OnLeftGround()
        {
            if (Current == State.Roll) End(new Vector2(_dashVelocity.x * 0.5f, 0f));
        }

        void OnLanded(float impactSpeed)
        {
            _airDashesLeft = kit.airDashesPerAirtime;
            if (Current == State.MeteorFall) BeginMeteorLanding();
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            float now = Time.time;
            var intent = _motor.Intent;
            if (_dodgeCooldown > 0f) _dodgeCooldown -= dt;
            if (_motor.IsGrounded && Current == State.None) _airDashesLeft = kit.airDashesPerAirtime;

            switch (Current)
            {
                case State.None:
                    if (_motor.IsControlLocked) break;
                    // Attacks own the fighter until their cancel window, where a dodge may interrupt them.
                    bool attacking = _attacks != null && _attacks.IsAttacking;
                    if (attacking && !_attacks.CanCancel) break;
                    if (!attacking && !_motor.IsGrounded && intent.Special.Consume(now)) { BeginMeteor(); break; }
                    if (intent.Dodge.IsBuffered(now) && _dodgeCooldown <= 0f)
                    {
                        bool canRoll = _motor.IsGrounded;
                        bool canDash = !_motor.IsGrounded && _airDashesLeft > 0;
                        if (canRoll || canDash)
                        {
                            intent.Dodge.Consume(now);
                            if (attacking) _attacks.Cancel();
                            if (canRoll) BeginRoll(intent.Move); else BeginAirDash(intent.Move);
                        }
                    }
                    break;

                case State.Roll:
                    _stateTimer -= dt;
                    if (_stateTimer <= 0f) End(new Vector2(_dashVelocity.x * 0.3f, 0f));
                    break;

                case State.AirDash:
                    _stateTimer -= dt;
                    if (_stateTimer <= 0f) End(_dashVelocity * kit.airDashCarryOver);
                    break;

                case State.MeteorStartup:
                    _stateTimer -= dt;
                    if (_stateTimer <= 0f)
                    {
                        Current = State.MeteorFall;
                        _stateTimer = kit.meteorMaxDuration;
                        _motor.SetOverride(new Vector2(0f, -kit.meteorFallSpeed));
                    }
                    break;

                case State.MeteorFall:
                    _stateTimer -= dt;
                    // Landed event normally ends the fall; this catches landing on the very first step or a timeout.
                    if (_motor.IsGrounded) BeginMeteorLanding();
                    else if (_stateTimer <= 0f) End(new Vector2(0f, -kit.meteorFallSpeed * 0.5f));
                    break;

                case State.MeteorLanding:
                    _stateTimer -= dt;
                    if (_stateTimer <= 0f) Current = State.None;
                    break;
            }
        }

        void BeginRoll(Vector2 move)
        {
            var def = _motor.Definition;
            float speed = def != null ? def.dodgeSpeed : 11f;
            float duration = def != null ? def.dodgeDuration : 0.22f;
            float invuln = def != null ? def.dodgeInvulnerability : 0.18f;
            int dir = Mathf.Abs(move.x) > 0.2f ? (move.x > 0f ? 1 : -1) : _motor.Facing;

            _motor.SetFacing(dir);
            _dashVelocity = new Vector2(dir * speed, 0f);
            _motor.SetOverride(_dashVelocity);
            _motor.FacingLocked = true;
            _invuln.Grant(invuln);
            _stateTimer = duration;
            _dodgeCooldown = duration + (def != null ? def.dodgeCooldown : 0.35f);
            Current = State.Roll;
            RollStarted?.Invoke();
            Services.Haptics?.Play(HapticStrength.Light);
        }

        void BeginAirDash(Vector2 move)
        {
            Vector2 dir;
            if (JumpPhysics.SnapToEightWay(move.x, move.y, kit.airDashAimDeadzone, out var snapped))
                dir = new Vector2(snapped.x, snapped.y);
            else
                dir = new Vector2(_motor.Facing, 0f);

            if (Mathf.Abs(dir.x) > 0.01f) _motor.SetFacing(dir.x > 0f ? 1 : -1);
            _dashVelocity = dir * kit.airDashSpeed;
            _motor.SetOverride(_dashVelocity);
            _motor.FacingLocked = true;
            _invuln.Grant(kit.airDashInvulnerability);
            _airDashesLeft--;
            _stateTimer = kit.airDashDuration;
            _dodgeCooldown = kit.airDashDuration;
            Current = State.AirDash;
            AirDashStarted?.Invoke();
            Services.Haptics?.Play(HapticStrength.Light);
        }

        void BeginMeteor()
        {
            Current = State.MeteorStartup;
            _stateTimer = kit.meteorStartup;
            _motor.SetOverride(Vector2.zero); // brief hang so the move is readable
            _motor.FacingLocked = true;
        }

        void BeginMeteorLanding()
        {
            _motor.ClearOverride(Vector2.zero);
            _motor.FacingLocked = false;
            _motor.LockControl(kit.meteorLandingLag);
            Current = State.MeteorLanding;
            _stateTimer = kit.meteorLandingLag;
            MeteorImpact?.Invoke(transform.position, kit.meteorShockwaveRadius);
            Services.Haptics?.Play(HapticStrength.Heavy);
        }

        void End(Vector2 exitVelocity)
        {
            _motor.ClearOverride(exitVelocity);
            _motor.FacingLocked = false;
            Current = State.None;
        }

        /// <summary>Hard cancel (used when hit or respawned).</summary>
        public void Cancel()
        {
            if (Current == State.None) return;
            _motor.ClearOverride();
            _motor.FacingLocked = false;
            Current = State.None;
        }
    }
}
