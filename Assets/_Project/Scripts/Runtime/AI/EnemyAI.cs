using System;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Drives a common enemy through the same FighterIntent a player uses, so enemies share the motor,
    /// knockback and hitstun rules. Walker: patrols, turning at walls and ledges. Hopper: waits, then hops
    /// toward the player when in range.
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
        int _dir = -1;
        bool _hopping;
        Vector2 _halfSize = new Vector2(0.45f, 0.4f);

        void Awake()
        {
            _motor = GetComponent<PlatformerMotor2D>();
            _health = GetComponent<Damageable>();
        }

        void OnEnable()
        {
            _health.Died += OnDied;
            _motor.Landed += OnLanded;
        }

        void OnDisable()
        {
            _health.Died -= OnDied;
            _motor.Landed -= OnLanded;
        }

        public void Init(EnemyDefinition def, int startDir)
        {
            Definition = def;
            _dir = startDir >= 0 ? 1 : -1;
            _halfSize = def.bodySize * 0.5f;
            _hopper = def.behavior == EnemyBehavior.Hopper ? new HopperLogic(def.hopCooldown, def.hopRange, 0.6f) : null;
            _hopping = false;
            _motor.Intent.ClearAll();
            _motor.SetFacing(_dir);
        }

        void FixedUpdate()
        {
            var intent = _motor.Intent;
            if (Definition == null || _health.IsDead) { intent.Move = Vector2.zero; return; }

            if (Definition.behavior == EnemyBehavior.Walker) Walk(intent);
            else Hop(intent);
            _motor.SetFacing(_dir);
        }

        void Walk(FighterIntent intent)
        {
            Vector2 pos = _motor.Body.position;
            LayerMask ground = PKRLayers.GroundMask;
            bool wall = Physics2D.Raycast(pos, new Vector2(_dir, 0f), _halfSize.x + 0.12f, ground);
            Vector2 foot = pos + new Vector2(_dir * (_halfSize.x + 0.08f), -_halfSize.y + 0.05f);
            bool groundAhead = Physics2D.Raycast(foot, Vector2.down, 0.45f, ground);
            _dir = PatrolLogic.NextDirection(_dir, wall, groundAhead, _motor.IsGrounded);
            intent.Move = new Vector2(_dir * Definition.moveSpeed, 0f);
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
