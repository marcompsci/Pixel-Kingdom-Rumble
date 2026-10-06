using System;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Responsive 2D platformer movement on a dynamic Rigidbody2D (zero built-in gravity; we integrate our own).
    /// Features: acceleration curves, coyote time, jump buffer, variable jump height, apex hang, fast fall,
    /// knockback/control lock, and a velocity override used by abilities (dash, roll, dive).
    ///
    /// Each FixedUpdate reads the body's velocity AFTER the physics solver, so walls, ceilings and floors
    /// naturally stop us; we only add intent and gravity on top.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [DefaultExecutionOrder(0)]
    public class PlatformerMotor2D : MonoBehaviour
    {
        const float GroundProbeDistance = 0.06f;
        const float GroundNormalMinY = 0.65f;
        const float GroundStickSpeed = -0.5f;
        const float WallProbeDistance = 0.08f;
        const float WallJumpControlLock = 0.12f;

        [SerializeField] CharacterDefinition definition;
        [Tooltip("Layers that count as standing surfaces.")]
        [SerializeField] LayerMask groundMask = 1 << 0;
        [Tooltip("Main body collider. Found automatically if empty.")]
        [SerializeField] Collider2D bodyCollider;

        public CharacterDefinition Definition => definition;
        public MovementStats Stats => definition != null ? definition.movement : _fallbackStats;
        public FighterIntent Intent { get; set; } = new FighterIntent();
        public Rigidbody2D Body { get; private set; }

        public bool IsGrounded { get; private set; }
        public int Facing { get; private set; } = 1;
        public Vector2 Velocity => Body.linearVelocity;
        public bool IsControlLocked => _lockTimer > 0f;
        public bool HasOverride => _override.HasValue;
        /// <summary>Riding up a wall (heroes with MovementStats.wallRideTime &gt; 0, e.g. Rex Rollo).</summary>
        public bool IsWallRiding => _wallRide.IsActive;
        /// <summary>Pushing against the direction of travel on the ground (momentum heroes show a skid).</summary>
        public bool IsSkidding => Stats.skidDeceleration >= 0f && !HasOverride && IsGrounded &&
                                  JumpPhysics.IsSkidding(Body.linearVelocity.x, Intent.Move.x, true);
        /// <summary>Mid-air jumps left this airtime (MovementStats.airJumps; Brick's Stone Step).</summary>
        public int AirJumpsLeft => _airJumpsLeft;
        /// <summary>The moving platform we're standing on, if any.</summary>
        public MovingPlatform Platform => _platform;
        /// <summary>Set by abilities to freeze facing (e.g. during a dash).</summary>
        public bool FacingLocked { get; set; }

        /// <summary>Fired the step a jump starts.</summary>
        public event Action Jumped;
        /// <summary>Fired on touching down. Argument = downward speed at impact (positive).</summary>
        public event Action<float> Landed;
        /// <summary>Fired when leaving the ground for any reason.</summary>
        public event Action LeftGround;
        /// <summary>Fired when hit with knockback, so abilities can cancel themselves.</summary>
        public event Action KnockedBack;
        /// <summary>Fired when a wall ride starts and on a wall jump (for feedback).</summary>
        public event Action WallRideStarted;
        public event Action WallJumped;

        readonly MovementStats _fallbackStats = new MovementStats();
        readonly RaycastHit2D[] _hits = new RaycastHit2D[8];
        JumpAssist _jump;
        ContactFilter2D _groundFilter;
        float _gravity;
        float _jumpVelocity;
        float _lockTimer;
        Vector2? _override;
        int _seenJumpPresses;
        int _airJumpsLeft;
        bool _rising;      // rising part of a player-initiated jump (jump-cut window)
        bool _jumpArc;     // anywhere in a player-initiated jump until landing (apex hang)
        bool _cutApplied;
        float _lastAirVy;
        MovingPlatform _platform;
        float _carryX; // horizontal platform velocity added last step
        float _prevVx; // horizontal velocity we asked for last step (the solver zeroes it on hitting a wall)
        bool _ridingLastStep;
        readonly WallRide _wallRide = new WallRide();

        void Awake()
        {
            Body = GetComponent<Rigidbody2D>();
            if (bodyCollider == null) bodyCollider = GetComponent<Collider2D>();

            Body.bodyType = RigidbodyType2D.Dynamic;
            Body.gravityScale = 0f;
            Body.freezeRotation = true;
            Body.interpolation = RigidbodyInterpolation2D.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            if (bodyCollider != null && bodyCollider.sharedMaterial == null)
                bodyCollider.sharedMaterial = new PhysicsMaterial2D("PKR_NoFriction") { friction = 0f, bounciness = 0f };

            RecalculatePhysics();
        }

        void OnValidate()
        {
            if (Application.isPlaying) RecalculatePhysics();
        }

        /// <summary>Re-derive gravity/jump speed from stats. Call after changing the definition at runtime.</summary>
        public void RecalculatePhysics()
        {
            var s = Stats;
            _gravity = JumpPhysics.Gravity(s.jumpHeight, s.timeToApex);
            _jumpVelocity = JumpPhysics.JumpVelocity(s.jumpHeight, s.timeToApex);
            if (_jump == null) _jump = new JumpAssist(s.coyoteTime, s.jumpBufferTime);
            _jump.CoyoteTime = s.coyoteTime;
            _jump.BufferTime = s.jumpBufferTime;
            _groundFilter = new ContactFilter2D { useLayerMask = true, layerMask = groundMask, useTriggers = false };
        }

        public void SetDefinition(CharacterDefinition def)
        {
            definition = def;
            RecalculatePhysics();
        }

        public void SetGroundMask(LayerMask mask)
        {
            groundMask = mask;
            _groundFilter.layerMask = mask;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            var s = Stats;
            Vector2 v = Body.linearVelocity;

            // --- Ground state ---------------------------------------------------------------
            bool wasGrounded = IsGrounded;
            // Grounded = touching a floor and not moving up relative to it (so rising platforms still count).
            bool touching = ProbeGround(out var platform);
            float platformVy = platform != null ? platform.Velocity.y : 0f;
            IsGrounded = touching && v.y - platformVy <= 0.01f;
            _platform = IsGrounded ? platform : null;
            if (IsGrounded && !wasGrounded)
            {
                _airJumpsLeft = s.airJumps;
                _rising = false;
                _jumpArc = false;
                // Velocity so far is in world space; treat the platform's share as carry so we switch to its frame.
                _carryX = _platform != null ? _platform.Velocity.x : 0f;
                Landed?.Invoke(Mathf.Max(0f, -_lastAirVy));
            }
            else if (!IsGrounded && wasGrounded)
            {
                LeftGround?.Invoke();
            }
            if (!IsGrounded) _lastAirVy = v.y;
            else _wallRide.Land();

            // --- Jump input edges -> JumpAssist ---------------------------------------------
            if (Intent.Jump.PressCount != _seenJumpPresses)
            {
                _seenJumpPresses = Intent.Jump.PressCount;
                _jump.PressJump();
            }
            _jump.Tick(dt, IsGrounded);

            if (_lockTimer > 0f) _lockTimer -= dt;

            // --- Ability override: constant velocity, no gravity, no input ------------------
            if (_override.HasValue)
            {
                Body.linearVelocity = _override.Value;
                _carryX = 0f;
                _prevVx = _override.Value.x;
                _ridingLastStep = false;
                return;
            }

            bool canAct = !IsControlLocked;

            // --- Horizontal -----------------------------------------------------------------
            // Work in the platform's frame while grounded; leaving the ground keeps the carried momentum.
            if (IsGrounded) v.x -= _carryX;
            float carry = _platform != null ? _platform.Velocity.x : 0f;
            if (canAct)
                v.x = JumpPhysics.NextHorizontalVelocity(v.x, Intent.Move.x, s, IsGrounded, dt);
            else
                v.x = JumpPhysics.MoveTowards(v.x, 0f, s.airDeceleration * 0.5f * dt); // knockback drift
            v.x += carry;
            _carryX = carry;

            // --- Wall ride (only heroes whose stats enable it) -------------------------------
            // Runs before the jump section so a wall jump owns the jump press for this step.
            bool riding = false, wallJumped = false;
            // A ceiling stopped the ride (velocity after physics no longer rising): let go.
            if (_wallRide.IsActive && _ridingLastStep && v.y <= 0.01f) _wallRide.End();
            if (s.wallRideTime > 0f && !IsGrounded && canAct)
            {
                // Don't cut a fresh jump short: only start once rising slower than the ride itself.
                if (!_wallRide.IsActive && Mathf.Abs(Intent.Move.x) > 0.3f && v.y <= s.wallRideSpeed)
                {
                    int dir = Intent.Move.x > 0f ? 1 : -1;
                    float speedIn = Mathf.Max(Mathf.Max(0f, v.x * dir), Mathf.Max(0f, _prevVx * dir));
                    if (_wallRide.TryStart(true, ProbeWall(dir), dir, Intent.Move.x, speedIn, s.wallRideTime, s.wallRideMinSpeed))
                        WallRideStarted?.Invoke();
                }
                if (_wallRide.IsActive)
                {
                    if (_jump.HasBufferedJump && _jump.TryConsumeBufferedPress())
                    {
                        int away = _wallRide.TryWallJump();
                        v.x = away * s.wallJumpSpeedX;
                        StartJump(ref v);
                        _jump.MarkJumped(); // no coyote jump right after
                        _lockTimer = Mathf.Max(_lockTimer, WallJumpControlLock); // a moment to kick clear of the wall
                        if (!FacingLocked) Facing = away;
                        wallJumped = true;
                        WallJumped?.Invoke();
                    }
                    else if (!ProbeWall(_wallRide.WallDir))
                    {
                        _wallRide.End(); // rode over the top (or the wall ended)
                    }
                    else
                    {
                        v.y = s.wallRideSpeed;
                        v.x = _wallRide.WallDir * 0.5f; // keep contact
                        _rising = false;
                        riding = true;
                    }
                }
            }
            else if (_wallRide.IsActive)
            {
                _wallRide.End();
            }
            _wallRide.Tick(dt);
            _ridingLastStep = riding;

            // --- Jump -----------------------------------------------------------------------
            if (!wallJumped && !riding && canAct && _jump.TryConsumeJump())
            {
                StartJump(ref v);
            }
            else if (!wallJumped && !riding && canAct && !IsGrounded && _airJumpsLeft > 0 && _jump.HasBufferedJump && !_jump.CanUseGroundJump)
            {
                _jump.TryConsumeBufferedPress();
                _airJumpsLeft--;
                StartJump(ref v);
            }

            // Variable jump height: releasing early cuts the rise once.
            if (_rising && !_cutApplied && v.y > 0f && !Intent.Jump.Held)
            {
                v.y *= s.jumpCutMultiplier;
                _cutApplied = true;
            }
            if (v.y <= 0f) _rising = false;

            // --- Gravity --------------------------------------------------------------------
            if (IsGrounded)
            {
                // Stick to the floor (and ride vertical platforms).
                v.y = (_platform != null ? _platform.Velocity.y : 0f) + GroundStickSpeed;
            }
            else if (!riding)
            {
                float mult = JumpPhysics.GravityMultiplier(v.y, Intent.Jump.Held && _jumpArc, s);
                v.y -= _gravity * mult * dt;
                v.y = Mathf.Max(v.y, -s.maxFallSpeed);
            }

            Body.linearVelocity = v;
            _prevVx = v.x;

            // --- Facing ---------------------------------------------------------------------
            if (!IsControlLocked && !FacingLocked && !riding && Mathf.Abs(Intent.Move.x) > 0.2f)
                Facing = Intent.Move.x > 0f ? 1 : -1;
        }

        void StartJump(ref Vector2 v)
        {
            v.y = _jumpVelocity;
            IsGrounded = false;
            _rising = true;
            _jumpArc = true;
            _cutApplied = false;
            Jumped?.Invoke();
        }

        /// <summary>A solid wall within WallProbeDistance on side dir (+1 right, -1 left). One-way planks don't count.</summary>
        bool ProbeWall(int dir)
        {
            if (bodyCollider == null) return false;
            int n = bodyCollider.Cast(new Vector2(dir, 0f), _groundFilter, _hits, WallProbeDistance);
            for (int i = 0; i < n; i++)
            {
                var c = _hits[i].collider;
                if (c == null || c.usedByEffector) continue;
                if (Mathf.Abs(_hits[i].normal.x) >= 0.7f && Mathf.Sign(_hits[i].normal.x) == -dir) return true;
            }
            return false;
        }

        bool ProbeGround(out MovingPlatform platform)
        {
            platform = null;
            if (bodyCollider == null) return false;
            int n = bodyCollider.Cast(Vector2.down, _groundFilter, _hits, GroundProbeDistance);
            for (int i = 0; i < n; i++)
            {
                // Cast reports colliders we already overlap with distance 0 and an upward normal, and it ignores
                // PlatformEffector2D. Skip those for one-way platforms so jumping up through one doesn't count as landing.
                if (_hits[i].distance <= 0f && _hits[i].collider != null && _hits[i].collider.usedByEffector) continue;
                if (_hits[i].normal.y >= GroundNormalMinY)
                {
                    if (_hits[i].collider != null) _hits[i].collider.TryGetComponent(out platform);
                    return true;
                }
            }
            return false;
        }

        // ---- API for combat & abilities -----------------------------------------------------

        /// <summary>Launch with a velocity and remove control for lockSeconds (hitstun).</summary>
        public void ApplyKnockback(Vector2 velocity, float lockSeconds)
        {
            ClearOverride();
            _wallRide.End();
            _rising = false;
            _jumpArc = false;
            _lockTimer = Mathf.Max(_lockTimer, lockSeconds);
            Body.linearVelocity = velocity;
            if (velocity.y > 0f) IsGrounded = false;
            KnockedBack?.Invoke();
        }

        /// <summary>Hold a constant velocity until ClearOverride (dashes, rolls, dives).</summary>
        public void SetOverride(Vector2 velocity)
        {
            _wallRide.End();
            _override = velocity;
            _rising = false;
            _jumpArc = false;
        }

        public void ClearOverride(Vector2? exitVelocity = null)
        {
            if (!_override.HasValue) return;
            _override = null;
            if (exitVelocity.HasValue) Body.linearVelocity = exitVelocity.Value;
        }

        public void LockControl(float seconds) => _lockTimer = Mathf.Max(_lockTimer, seconds);

        public void SetFacing(int dir)
        {
            if (dir != 0) Facing = dir > 0 ? 1 : -1;
        }

        /// <summary>Instantly move (respawn/checkpoint). Clears velocity and all transient state.</summary>
        public void Teleport(Vector2 position)
        {
            ClearOverride();
            _lockTimer = 0f;
            _rising = false;
            _jumpArc = false;
            _lastAirVy = 0f;
            _carryX = 0f;
            _platform = null;
            _airJumpsLeft = Stats.airJumps;
            _wallRide.Land();
            _prevVx = 0f;
            _jump.Reset();
            _seenJumpPresses = Intent.Jump.PressCount;
            Body.position = position;
            transform.position = position;
            Body.linearVelocity = Vector2.zero;
            IsGrounded = false;
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            var col = bodyCollider != null ? bodyCollider : GetComponent<Collider2D>();
            if (col == null) return;
            var b = col.bounds;
            Gizmos.color = IsGrounded ? Color.green : Color.yellow;
            Gizmos.DrawWireCube(new Vector3(b.center.x, b.min.y - GroundProbeDistance * 0.5f), new Vector3(b.size.x, GroundProbeDistance));
        }
#endif
    }
}
