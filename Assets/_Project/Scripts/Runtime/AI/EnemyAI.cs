using System;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Drives a common enemy through the same FighterIntent a player uses, so enemies share the motor,
    /// knockback and hitstun rules. Walker: patrols, turning at walls and ledges. Hopper: waits, then hops
    /// toward the player when in range. Flyer: hovers and swoops (Core FlyerLogic) using the motor's velocity
    /// override, so no gravity. ShieldWalker: patrols slowly, and stops to face a nearby hero shield-first.
    /// Guard: patrols and stops now and then to look back; freezes while suspicious; chases once its
    /// GuardVision is alerted (never off a ledge).
    /// </summary>
    [RequireComponent(typeof(PlatformerMotor2D), typeof(Damageable))]
    [DefaultExecutionOrder(-30)]
    public class EnemyAI : MonoBehaviour
    {
        public EnemyDefinition Definition { get; private set; }
        public event Action<EnemyAI> Defeated;

        PlatformerMotor2D _motor;
        Damageable _health;
        HopperLogic _hopper;
        FlyerLogic _flyer;
        Vector2 _home;
        float _turnTimer;
        /// <summary>Seconds the hero must stay behind a Bolt Knight before it turns: the window to hit its back.</summary>
        const float ShieldTurnDelay = 0.5f;
        int _dir = -1;
        bool _hopping;
        GuardVision _vision;
        float _patrolTimer, _pauseTimer;
        Vector2 _halfSize = new Vector2(0.45f, 0.4f);

        void Awake()
        {
            _motor = GetComponent<PlatformerMotor2D>();
            _health = GetComponent<Damageable>();
        }

        void OnEnable()
        {
            _health.Died += OnDied;
            _health.Hit += OnHit;
            _motor.Landed += OnLanded;
        }

        void OnDisable()
        {
            _health.Died -= OnDied;
            _health.Hit -= OnHit;
            _motor.Landed -= OnLanded;
        }

        public void Init(EnemyDefinition def, int startDir)
        {
            Definition = def;
            _dir = startDir >= 0 ? 1 : -1;
            _halfSize = def.bodySize * 0.5f;
            _hopper = def.behavior == EnemyBehavior.Hopper ? new HopperLogic(def.hopCooldown, def.hopRange, 0.6f) : null;
            _flyer = def.behavior == EnemyBehavior.Flyer
                ? new FlyerLogic(new FlyerTuning
                  {
                      bobHeight = 0.4f, bobSpeed = 2.5f, aggroRange = def.aggroRange, windup = def.swoopWindup,
                      swoopSpeed = def.swoopSpeed, swoopTime = def.swoopTime, returnSpeed = def.flyerReturnSpeed,
                      cooldown = def.swoopCooldown
                  }, 0.8f)
                : null;
            _home = _motor.Body != null ? _motor.Body.position : (Vector2)transform.position;
            if (TryGetComponent(out ShieldGuard guard)) guard.Shield.Reset();
            _hopping = false;
            _turnTimer = 0f;
            _vision = def.behavior == EnemyBehavior.Guard ? GetComponent<GuardVision>() : null;
            _patrolTimer = 0f;
            _pauseTimer = 0f;
            _motor.Intent.ClearAll();
            _motor.SetFacing(_dir);
        }

        void FixedUpdate()
        {
            var intent = _motor.Intent;
            if (Definition == null || _health.IsDead)
            {
                intent.Move = Vector2.zero;
                if (_motor.HasOverride) _motor.ClearOverride(Vector2.zero); // a dead flyer drops
                return;
            }

            switch (Definition.behavior)
            {
                case EnemyBehavior.Walker: Walk(intent, Definition.moveSpeed); break;
                case EnemyBehavior.Hopper: Hop(intent); break;
                case EnemyBehavior.Flyer: Fly(); break;
                case EnemyBehavior.ShieldWalker: ShieldWalk(intent); break;
                case EnemyBehavior.Guard: Guard(intent); break;
            }
            _motor.SetFacing(_dir);
        }

        void Walk(FighterIntent intent, float speed)
        {
            Vector2 pos = _motor.Body.position;
            LayerMask ground = PKRLayers.GroundMask;
            bool wall = Physics2D.Raycast(pos, new Vector2(_dir, 0f), _halfSize.x + 0.12f, ground);
            Vector2 foot = pos + new Vector2(_dir * (_halfSize.x + 0.08f), -_halfSize.y + 0.05f);
            bool groundAhead = Physics2D.Raycast(foot, Vector2.down, 0.45f, ground);
            _dir = PatrolLogic.NextDirection(_dir, wall, groundAhead, _motor.IsGrounded);
            intent.Move = new Vector2(_dir * speed, 0f);
        }

        void Fly()
        {
            // Knocked back: the motor's knockback (with gravity) plays out; resume flying when control returns.
            if (_motor.IsControlLocked) return;
            var player = PlayerMarker.Current;
            Vector2 pos = _motor.Body.position;
            Vector2 target = player != null ? (Vector2)player.transform.position : pos;
            var v = _flyer.Step(Time.fixedDeltaTime, new Vec2(pos.x, pos.y), new Vec2(_home.x, _home.y), player != null,
                                new Vec2(target.x, target.y));
            // Bumped into a wall/floor mid-swoop: give up and go home.
            // (Only for level or downward swoops: rising through a one-way plank must not abort it.)
            if (_flyer.State == FlyerState.Swoop && v.y <= 0.1f &&
                Physics2D.Raycast(pos, new Vector2(v.x, v.y).normalized, _halfSize.x + 0.15f, PKRLayers.GroundMask))
                _flyer.Interrupt();
            _motor.SetOverride(new Vector2(v.x, v.y));
            if (Mathf.Abs(v.x) > 0.1f) _dir = v.x > 0f ? 1 : -1;
            else if (player != null) _dir = target.x >= pos.x ? 1 : -1;
        }

        void ShieldWalk(FighterIntent intent)
        {
            if (_motor.IsControlLocked) { intent.Move = Vector2.zero; return; } // staggered after a shield break
            var player = PlayerMarker.Current;
            if (player != null && _motor.IsGrounded)
            {
                Vector2 d = (Vector2)player.transform.position - _motor.Body.position;
                if (Mathf.Abs(d.x) <= Definition.guardRange && Mathf.Abs(d.y) <= 2f)
                {
                    // Hold ground and turn the shield toward the hero, but slowly: a hero who gets behind it
                    // (and is back on the ground) has a short window to hit its unarmored back.
                    int want = d.x >= 0f ? 1 : -1;
                    bool heroGrounded = !player.TryGetComponent(out PlatformerMotor2D heroMotor) || heroMotor.IsGrounded;
                    if (want != _dir && heroGrounded)
                    {
                        _turnTimer += Time.fixedDeltaTime;
                        if (_turnTimer >= ShieldTurnDelay) { _dir = want; _turnTimer = 0f; }
                    }
                    else if (want == _dir) _turnTimer = 0f;
                    intent.Move = Vector2.zero;
                    return;
                }
            }
            _turnTimer = 0f;
            Walk(intent, Definition.moveSpeed);
        }

        void Guard(FighterIntent intent)
        {
            if (_motor.IsControlLocked || _vision == null) { intent.Move = Vector2.zero; return; }
            float dt = Time.fixedDeltaTime;
            var player = PlayerMarker.Current;
            if (_vision.Alerted && player != null)
            {
                _pauseTimer = 0f;
                float dx = player.transform.position.x - _motor.Body.position.x;
                if (Mathf.Abs(dx) > 0.3f) _dir = dx > 0f ? 1 : -1;
                Vector2 foot = _motor.Body.position + new Vector2(_dir * (_halfSize.x + 0.08f), -_halfSize.y + 0.05f);
                bool groundAhead = Physics2D.Raycast(foot, Vector2.down, 0.45f, PKRLayers.GroundMask);
                bool wall = Physics2D.Raycast(_motor.Body.position, new Vector2(_dir, 0f), _halfSize.x + 0.12f, PKRLayers.GroundMask);
                intent.Move = groundAhead && !wall && Mathf.Abs(dx) > 0.3f ? new Vector2(_dir * Definition.chaseSpeed, 0f) : Vector2.zero;
                return;
            }
            if (_vision.Awareness == GuardAwareness.Suspicious)
            {
                // Stop and stare: the meter drains if the hero breaks line of sight.
                intent.Move = Vector2.zero;
                return;
            }
            if (_pauseTimer > 0f)
            {
                intent.Move = Vector2.zero;
                _pauseTimer -= dt;
                if (_pauseTimer <= 0f) _dir = -_dir; // look back the other way, then walk that way
                return;
            }
            _patrolTimer += dt;
            if (_patrolTimer >= Definition.lookBackInterval)
            {
                _patrolTimer = 0f;
                _pauseTimer = Definition.lookBackPause;
                intent.Move = Vector2.zero;
                return;
            }
            Walk(intent, Definition.moveSpeed);
        }

        void OnHit(HitResult r)
        {
            if (_flyer != null) _flyer.Interrupt();
        }

        void Hop(FighterIntent intent)
        {
            // In hitstun the motor ignores jumps; don't start (or get stuck in) a hop.
            if (_motor.IsControlLocked)
            {
                intent.Move = Vector2.zero;
                if (_hopping && _motor.IsGrounded) { _hopping = false; intent.Jump.SetHeld(false); }
                return;
            }
            var player = PlayerMarker.Current;
            float dx = 0f, dy = 0f;
            if (player != null)
            {
                Vector2 d = (Vector2)player.transform.position - _motor.Body.position;
                dx = d.x; dy = d.y;
            }
            else dx = 999f; // out of range: idle

            if (_motor.IsGrounded && !_hopping)
            {
                intent.Move = Vector2.zero;
                if (_hopper.Tick(Time.fixedDeltaTime, true, dx, dy, out int dir))
                {
                    _dir = dir;
                    _hopping = true;
                    intent.Jump.Press(Time.time);
                    intent.Jump.SetHeld(true);
                }
            }
            else if (_hopping)
            {
                intent.Move = new Vector2(_dir * Definition.hopDrift, 0f);
            }
        }

        void OnLanded(float speed)
        {
            if (!_hopping) return;
            _hopping = false;
            _motor.Intent.Jump.SetHeld(false);
            _motor.Intent.Move = Vector2.zero;
        }

        void OnDied() => Defeated?.Invoke(this);
    }
}
