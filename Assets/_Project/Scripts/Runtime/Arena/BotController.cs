using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// CPU fighter: every reaction interval asks Core BotBrain for a decision and feeds it into the same
    /// FighterIntent a human uses. Passive bots (Training mode) stand still.
    /// Runs before AttackRunner/HeroAbilities so presses are consumed in the same physics step.
    /// </summary>
    [RequireComponent(typeof(PlatformerMotor2D))]
    [DefaultExecutionOrder(-60)]
    public class BotController : MonoBehaviour
    {
        public BotDifficulty difficulty = BotDifficulty.For(BotLevel.Normal);
        public bool passive;

        ArenaMatchController _match;
        PlatformerMotor2D _motor;
        Damageable _self;
        HeroAbilities _abilities;
        AttackRunner _attacks;
        System.Random _rng;
        float _nextDecision;
        BotCommand _last;
        bool _holdAttackForChain;

        public void Init(ArenaMatchController match, BotDifficulty d, int seed, bool isPassive)
        {
            _match = match;
            difficulty = d;
            passive = isPassive;
            _rng = new System.Random(seed);
            _nextDecision = 0f;
        }

        void Awake()
        {
            _motor = GetComponent<PlatformerMotor2D>();
            _self = GetComponent<Damageable>();
            _abilities = GetComponent<HeroAbilities>();
            _attacks = GetComponent<AttackRunner>();
            if (_rng == null) _rng = new System.Random(GetInstanceID());
        }

        void FixedUpdate()
        {
            var intent = _motor.Intent;
            if (passive || _match == null || !_match.IsLive)
            {
                intent.Move = Vector2.zero;
                intent.Jump.SetHeld(false);
                return;
            }

            float now = Time.time;
            if (now < _nextDecision)
            {
                intent.Move = new Vector2(_last.moveX, _last.moveY);
                return;
            }
            // Small jitter so several bots don't act in lockstep.
            _nextDecision = now + difficulty.reactionTime * (0.8f + 0.4f * (float)_rng.NextDouble());

            var view = BuildView();
            _last = BotBrain.Decide(view, difficulty, _rng);
            intent.Move = new Vector2(_last.moveX, _last.moveY);
            if (_last.jump) intent.Jump.Press(now);
            intent.Jump.SetHeld(_last.jumpHeld);

            // Follow a jab with the sweep on the next decision sometimes, but only if still in range on stage.
            bool chainOk = _holdAttackForChain && view.grounded && view.hasTarget
                           && Mathf.Abs(view.targetPosition.x - view.position.x) <= BotBrain.AttackRangeX
                           && Mathf.Abs(view.targetPosition.y - view.position.y) <= BotBrain.AttackRangeY;
            if (_last.attack || chainOk)
            {
                intent.Attack.Press(now);
                _holdAttackForChain = !chainOk && _last.attack && _rng.NextDouble() < difficulty.accuracy * 0.6;
            }
            else _holdAttackForChain = false;
            if (_last.special) intent.Special.Press(now);
            if (_last.dodge) intent.Dodge.Press(now);
        }

        BotView BuildView()
        {
            var pos = _motor.Body.position;
            var vel = _motor.Velocity;
            var stage = _match.StageRect;
            var v = new BotView
            {
                position = new Vec2(pos.x, pos.y),
                velocity = new Vec2(vel.x, vel.y),
                grounded = _motor.IsGrounded,
                exposed = _self != null && _self.IsExposed,
                canAirDash = _abilities != null && !_motor.IsGrounded && _abilities.CanRecoverWithAirDodge,
                canAirJump = !_motor.IsGrounded && _motor.AirJumpsLeft > 0 && (_abilities == null || !_abilities.IsBusy) && !_motor.IsControlLocked,
                hasProjectile = _attacks != null && _attacks.Moveset != null && _attacks.Moveset.groundSpecial != null
                                && _attacks.Moveset.groundSpecial.spawnsProjectile,
                stageLeft = stage.xMin,
                stageRight = stage.xMax,
                stageTop = stage.yMax
            };
            if (_match.GapOpen(out float gapL, out float gapR))
            {
                v.hasGap = true;
                v.gapLeft = gapL;
                v.gapRight = gapR;
            }
            var target = _match.NearestOpponent(gameObject, pos);
            if (target != null)
            {
                v.hasTarget = true;
                Vector2 tp = target.transform.position;
                v.targetPosition = new Vec2(tp.x, tp.y);
                var td = target.GetComponent<Damageable>();
                v.targetExposed = td != null && td.IsExposed;
            }
            return v;
        }
    }
}
