using System;
using System.Collections.Generic;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Plays a fighter's attacks from its Moveset: picks the move for the current context, steps frame data,
    /// checks hitboxes on active frames (each target once per swing), handles follow-up chains, projectiles,
    /// ability impacts (dive shockwaves such as Nova's Meteor Drop) and hit feedback (hit stop, haptics, screen shake).
    /// Runs before HeroAbilities each physics step.
    /// </summary>
    [RequireComponent(typeof(PlatformerMotor2D))]
    [DefaultExecutionOrder(-20)]
    public class AttackRunner : MonoBehaviour
    {
        [SerializeField] Moveset moveset;
        [Tooltip("Hits on Damageables with the same team are ignored.")]
        [SerializeField] int team = TeamIds.Player;
        [Tooltip("Draw hitboxes in the Scene/Game view (Gizmos).")]
        public bool debugDrawHitboxes = true;

        public Moveset Moveset { get => moveset; set => moveset = value; }
        public int Team { get => team; set => team = value; }
        public MoveDefinition Current { get; private set; }
        public int Frame { get; private set; }
        public bool IsAttacking => Current != null;
        public bool CanCancel => Current != null && AttackTimeline.InCancelWindow(Current.frames, Frame);
        /// <summary>The current move has super armor on this frame (Damageable then ignores knockback that isn't a launch).</summary>
        public bool HasSuperArmor => Current != null && Current.superArmor &&
                                     AttackTimeline.InArmorWindow(Current.frames, Mathf.Max(0, Frame - 1), Current.armorStartFrame, Current.armorEndFrame);

        public event Action<MoveDefinition> AttackStarted;
        public event Action<Damageable, HitResult> HitLanded;

        PlatformerMotor2D _motor;
        HeroAbilities _abilities;
        Damageable _self;
        Material _spriteMaterial;
        bool _startedInAir;
        bool _usingOverride;
        readonly SwingHitLog _log = new SwingHitLog();
        readonly List<Damageable> _targets = new List<Damageable>();

        void Awake()
        {
            _motor = GetComponent<PlatformerMotor2D>();
            _abilities = GetComponent<HeroAbilities>();
            _self = GetComponent<Damageable>();
            if (moveset == null && _motor.Definition != null) moveset = _motor.Definition.moveset;
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null) _spriteMaterial = sr.sharedMaterial;
        }

        void OnEnable()
        {
            _motor.KnockedBack += Cancel;
            _motor.Landed += OnLanded;
            if (_abilities != null) _abilities.DiveImpact += OnDiveImpact;
        }

        void OnDisable()
        {
            _motor.KnockedBack -= Cancel;
            _motor.Landed -= OnLanded;
            if (_abilities != null) _abilities.DiveImpact -= OnDiveImpact;
        }

        void FixedUpdate()
        {
            if (moveset == null) return;
            float now = Time.time;
            var intent = _motor.Intent;

            if (!IsAttacking)
            {
                if (_motor.IsControlLocked || (_abilities != null && _abilities.IsBusy)) return;
                bool grounded = _motor.IsGrounded;
                if (intent.Attack.Consume(now))
                {
                    var m = moveset.ForAttack(grounded, intent.Move);
                    if (m != null) Begin(m, intent.Move);
                    return;
                }
                var special = moveset.ForSpecial(grounded);
                if (special != null && intent.Special.Consume(now)) Begin(special, intent.Move);
                return;
            }

            // Chain into the follow-up during the cancel window.
            if (Current.followUp != null && CanCancel && intent.Attack.Consume(now))
            {
                Begin(Current.followUp, intent.Move);
                return;
            }

            Step();
        }

        void Begin(MoveDefinition move, Vector2 stick)
        {
            if (_usingOverride) { _motor.ClearOverride(); _usingOverride = false; }
            Current = move;
            Frame = 0;
            _log.Reset();
            _startedInAir = !_motor.IsGrounded;
            if (Mathf.Abs(stick.x) > 0.3f) _motor.SetFacing(stick.x > 0f ? 1 : -1);
            _motor.FacingLocked = true;
            AttackStarted?.Invoke(move);
            // Process frame 0 now, before the motor runs this step, so a rooted move can't be
            // overridden by a jump buffered on the same frame.
            Step();
        }

        void Step()
        {
            var move = Current;
            var f = move.frames;
            var phase = AttackTimeline.PhaseAt(f, Frame);
            int facing = _motor.Facing;

            // Ground moves plant the fighter, with an optional forward lunge before recovery.
            if (!_startedInAir && move.rootedOnGround && _motor.IsGrounded)
            {
                float vx = phase == AttackPhase.Recovery ? 0f : move.lungeSpeed * facing;
                _motor.SetOverride(new Vector2(vx, 0f));
                _usingOverride = true;
            }
            else if (_usingOverride)
            {
                // Lunged off a ledge: let gravity take over for the rest of the move.
                _motor.ClearOverride();
                _usingOverride = false;
            }

            if (phase == AttackPhase.Active)
            {
                if (move.spawnsProjectile)
                {
                    if (AttackTimeline.IsFirstActiveFrame(f, Frame))
                        Projectile.Spawn(move, _motor.Body.position, facing, team, _self, this, _spriteMaterial);
                }
                else
                {
                    ApplyHits(move, move.WorldHitboxCenter(_motor.Body.position, facing), facing,
                              move.shape == HitShape.Circle ? move.hitboxRadius : -1f);
                }
            }

            Frame++;
            if (AttackTimeline.PhaseAt(f, Frame) == AttackPhase.Done) End();
        }

        /// <summary>
        /// Hit everything in the move's area once. radius &gt; 0 uses a circle centered at center and pushes
        /// targets away from it; otherwise the move's box with knockback in the facing direction.
        /// </summary>
        void ApplyHits(MoveDefinition move, Vector2 center, int facing, float radius)
        {
            if (radius > 0f) CombatQuery.OverlapCircle(center, radius, _targets);
            else CombatQuery.OverlapBox(center, move.hitboxSize, _targets);

            foreach (var target in _targets)
            {
                if (target == _self) continue;
                if (!_log.TryRegister(target.GetInstanceID())) continue;
                int dir = radius > 0f ? (target.transform.position.x >= center.x ? 1 : -1) : facing;
                if (target.TakeHit(move.hit, dir, team, out var result)) OnHitLanded(target, result, move.hit, move);
            }
        }

        void OnHitLanded(Damageable target, HitResult result, in HitData hit, MoveDefinition move)
        {
            HitStop.Request(result.hitstopFrames);
            if (Services.Haptics != null)
                Services.Haptics.Play(result.isLaunch ? HapticStrength.Heavy : (move != null ? move.haptic : HapticStrength.Light));
            float shake = move != null ? move.shakeAmplitude : 0f;
            if (result.isLaunch) shake = Mathf.Max(shake, 0.35f);
            if (shake > 0f && CameraFollow2D.Main != null)
                CameraFollow2D.Main.Shake(shake, move != null ? move.shakeDuration : 0.15f);
            HitLanded?.Invoke(target, result);
        }

        /// <summary>Called by projectiles this runner spawned.</summary>
        public void ReportProjectileHit(Damageable target, HitResult result, HitData hit) =>
            OnHitLanded(target, result, hit, null);

        void OnDiveImpact(Vector2 position, float radius)
        {
            var impact = moveset != null ? moveset.abilityImpact : null;
            if (impact == null) return;
            _log.Reset();
            ApplyHits(impact, position + new Vector2(0f, impact.hitboxOffset.y), _motor.Facing, radius > 0f ? radius : impact.hitboxRadius);
            if (CameraFollow2D.Main != null) CameraFollow2D.Main.Shake(Mathf.Max(0.15f, impact.shakeAmplitude), impact.shakeDuration);
        }

        void OnLanded(float impactSpeed)
        {
            if (Current != null && _startedInAir && Current.endsOnLanding) End();
        }

        void End()
        {
            if (_usingOverride) { _motor.ClearOverride(Vector2.zero); _usingOverride = false; }
            _motor.FacingLocked = false;
            Current = null;
            Frame = 0;
        }

        /// <summary>Stop the current attack immediately (got hit, dodged out, respawned).</summary>
        public void Cancel()
        {
            if (Current == null) return;
            if (_usingOverride) { _motor.ClearOverride(); _usingOverride = false; }
            _motor.FacingLocked = false;
            Current = null;
            Frame = 0;
        }

        void OnDrawGizmos()
        {
            if (!debugDrawHitboxes || Current == null || Current.spawnsProjectile) return;
            var phase = AttackTimeline.PhaseAt(Current.frames, Frame);
            Gizmos.color = phase == AttackPhase.Active ? new Color(1f, 0.2f, 0.2f, 0.9f) : new Color(1f, 0.8f, 0.2f, 0.5f);
            int facing = _motor != null ? _motor.Facing : 1;
            Vector2 c = Current.WorldHitboxCenter(transform.position, facing);
            if (Current.shape == HitShape.Circle) Gizmos.DrawWireSphere(c, Current.hitboxRadius);
            else Gizmos.DrawWireCube(c, Current.hitboxSize);
        }
    }
}
