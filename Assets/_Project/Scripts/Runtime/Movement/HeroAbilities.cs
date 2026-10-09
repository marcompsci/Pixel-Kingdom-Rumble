using System;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Every hero's movement abilities, driven by their HeroKitDefinition:
    ///  - Dodge on ground  -> roll with brief invulnerability (speed/duration from the CharacterDefinition)
    ///  - Dodge in air     -> the kit's air dodge (Nova: 8-way Air Dash; Brick: Granite Guard), limited per airtime
    ///  - Special in air   -> the kit's dive (Nova: Meteor Drop; Brick: Landslide Slam): hang, dive, landing shockwave
    /// Runs before the motor each physics step so overrides apply in the same step.
    /// Swap heroes at runtime with SetKit (see HeroLoadout).
    /// </summary>
    [RequireComponent(typeof(PlatformerMotor2D))]
    [DefaultExecutionOrder(-10)]
    public class HeroAbilities : MonoBehaviour
    {
        public enum State { None, Roll, AirDodge, DiveStartup, DiveFall, DiveLanding }

        [SerializeField] HeroKitDefinition kit;

        public State Current { get; private set; } = State.None;
        public bool IsBusy => Current != State.None;
        public HeroKitDefinition Kit => kit;
        public int AirDodgesLeft => _airDodgesLeft;
        /// <summary>An air dodge is available now and it helps get back to the stage (for CPU fighters).</summary>
        public bool CanRecoverWithAirDodge => !IsBusy && _airDodgesLeft > 0 && kit.AirDodgeRecovers;
        /// <summary>Super armor from the kit (e.g. Brick's armored dive).</summary>
        public bool HasSuperArmor => kit.diveArmored && (Current == State.DiveStartup || Current == State.DiveFall);

        /// <summary>Fired on dive impact with the landing position and shockwave radius.</summary>
        public event Action<Vector2, float> DiveImpact;
        public event Action AirDodgeStarted;
        public event Action RollStarted;

        PlatformerMotor2D _motor;
        AttackRunner _attacks;
        Damageable _health;
        Invulnerability _invuln;
        float _stateTimer;
        float _dodgeCooldown;
        int _airDodgesLeft;
        Vector2 _dashVelocity;

        void Awake()
        {
            _motor = GetComponent<PlatformerMotor2D>();
            _attacks = GetComponent<AttackRunner>();
            _health = GetComponent<Damageable>();
            _invuln = GetComponent<Invulnerability>();
            if (_invuln == null) _invuln = gameObject.AddComponent<Invulnerability>();
            if (kit == null && _motor.Definition != null) kit = _motor.Definition.kit as HeroKitDefinition;
            if (kit == null)
            {
                Debug.LogWarning("[HeroAbilities] No HeroKitDefinition assigned; using Nova's defaults.", this);
                kit = ScriptableObject.CreateInstance<NovaKitDefinition>();
            }
            _airDodgesLeft = kit.airDodgesPerAirtime;
        }

        /// <summary>Change hero kit at runtime (hero swap). Cancels whatever ability is running.</summary>
        public void SetKit(HeroKitDefinition newKit)
        {
            if (newKit == null) return;
            Cancel();
            kit = newKit;
            _airDodgesLeft = kit.airDodgesPerAirtime;
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

        // Rolling off a ledge ends the roll so the hero falls instead of gliding.
        void OnLeftGround()
        {
            if (Current == State.Roll) End(new Vector2(_dashVelocity.x * 0.5f, 0f));
        }

        void OnLanded(float impactSpeed)
        {
            _airDodgesLeft = kit.airDodgesPerAirtime;
            if (Current == State.DiveFall) BeginDiveLanding();
        }

        void FixedUpdate()
        {
            if (_health != null && _health.IsDead) { if (Current != State.None) Cancel(); return; }
            float dt = Time.fixedDeltaTime;
            float now = Time.time;
            var intent = _motor.Intent;
            if (_dodgeCooldown > 0f) _dodgeCooldown -= dt;
            if (_motor.IsGrounded && Current == State.None) _airDodgesLeft = kit.airDodgesPerAirtime;

            switch (Current)
            {
                case State.None:
                    if (_motor.IsControlLocked) break;
                    // Attacks own the fighter until their cancel window, where a dodge may interrupt them.
                    bool attacking = _attacks != null && _attacks.IsAttacking;
                    if (attacking && !_attacks.CanCancel) break;
                    if (!attacking && kit.hasDive && !_motor.IsGrounded && intent.Special.Consume(now)) { BeginDive(); break; }
                    if (intent.Dodge.IsBuffered(now) && _dodgeCooldown <= 0f)
                    {
                        bool canRoll = _motor.IsGrounded;
                        bool canAirDodge = !_motor.IsGrounded && _airDodgesLeft > 0;
                        if (canRoll || canAirDodge)
                        {
                            intent.Dodge.Consume(now);
                            if (attacking) _attacks.Cancel();
                            if (canRoll) BeginRoll(intent.Move); else BeginAirDodge(intent.Move);
                        }
                    }
                    break;

                case State.Roll:
                    _stateTimer -= dt;
                    if (_stateTimer <= 0f) End(new Vector2(_dashVelocity.x * 0.3f, 0f));
                    break;

                case State.AirDodge:
                    _stateTimer -= dt;
                    if (_stateTimer <= 0f) End(_dashVelocity * kit.airDodgeCarryOver);
                    break;

                case State.DiveStartup:
                    _stateTimer -= dt;
                    if (_stateTimer <= 0f)
                    {
                        Current = State.DiveFall;
                        _stateTimer = kit.diveMaxDuration;
                        _motor.SetOverride(new Vector2(0f, -kit.diveFallSpeed));
                    }
                    break;

                case State.DiveFall:
                    _stateTimer -= dt;
                    // Landed event normally ends the fall; this catches landing on the very first step or a timeout.
                    if (_motor.IsGrounded) BeginDiveLanding();
                    else if (_stateTimer <= 0f) End(new Vector2(0f, -kit.diveFallSpeed * 0.5f));
                    break;

                case State.DiveLanding:
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

        void BeginAirDodge(Vector2 move)
        {
            _dashVelocity = kit.AirDodgeVelocity(move, _motor.Facing);
            if (Mathf.Abs(_dashVelocity.x) > 0.01f) _motor.SetFacing(_dashVelocity.x > 0f ? 1 : -1);
            _motor.SetOverride(_dashVelocity);
            _motor.FacingLocked = true;
            _invuln.Grant(kit.airDodgeInvulnerability);
            _airDodgesLeft--;
            _stateTimer = kit.airDodgeDuration;
            _dodgeCooldown = kit.airDodgeDuration;
            Current = State.AirDodge;
            AirDodgeStarted?.Invoke();
            Services.Haptics?.Play(HapticStrength.Light);
        }

        void BeginDive()
        {
            Current = State.DiveStartup;
            _stateTimer = kit.diveStartup;
            _motor.SetOverride(Vector2.zero); // brief hang so the move is readable
            _motor.FacingLocked = true;
        }

        void BeginDiveLanding()
        {
            _motor.ClearOverride(Vector2.zero);
            _motor.FacingLocked = false;
            _motor.LockControl(kit.diveLandingLag);
            Current = State.DiveLanding;
            _stateTimer = kit.diveLandingLag;
            DiveImpact?.Invoke(transform.position, kit.diveShockwaveRadius);
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
