using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// The rescue jetpack (Story and Contracts). Hold JET to fly up on a small tank of fuel that refills on the ground,
    /// so a missed jump over a pit can still be saved. If the hero ever gets stuck (pushed inside a wall or hung
    /// motionless on an edge by a physics glitch), JET warps them back to the last safe ground they stood on.
    /// Added to the player by LevelFlowController.
    /// </summary>
    [RequireComponent(typeof(PlatformerMotor2D))]
    public class JetpackRescue : MonoBehaviour
    {
        [Tooltip("Seconds of thrust in a full tank.")]
        public float tankSeconds = 1.4f;
        [Tooltip("Tank refilled per second while standing on the ground.")]
        public float refillPerSecond = 1.2f;
        public float liftSpeed = 7.5f;
        public float liftAccel = 70f;

        public static JetpackRescue Player { get; private set; }

        /// <summary>0..1 fuel left (the button's ring shows it).</summary>
        public float Fuel01 => tankSeconds > 0f ? _fuel / tankSeconds : 0f;
        public bool IsStuck => _stuckFor >= StuckSeconds;
        public bool IsFlying => _flying;

        const float StuckSeconds = 0.6f;
        const float SafeSampleSeconds = 0.25f;

        PlatformerMotor2D _motor;
        Collider2D _body;
        float _fuel, _stuckFor, _stillFor, _safeTimer, _puffTimer, _sfxTimer;
        Vector2 _lastSafe, _lastPos;
        bool _hasSafe, _flying, _hinted;
        Material _material;
        static readonly Collider2D[] Overlaps = new Collider2D[8];

        void Awake()
        {
            _motor = GetComponent<PlatformerMotor2D>();
            _body = GetComponent<Collider2D>();
            _fuel = tankSeconds;
            var sr = GetComponentInChildren<SpriteRenderer>();
            _material = sr != null ? sr.sharedMaterial : null;
        }

        void OnEnable()
        {
            if (GetComponent<PlayerMarker>() != null) Player = this;
            EventBus<PlayerRespawned>.Subscribe(OnRespawned);
        }

        void OnDisable()
        {
            if (Player == this) Player = null;
            EventBus<PlayerRespawned>.Unsubscribe(OnRespawned);
        }

        void OnRespawned(PlayerRespawned e)
        {
            _fuel = tankSeconds;
            _stuckFor = _stillFor = 0f;
            _flying = false;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            Vector2 pos = _motor.Body != null ? _motor.Body.position : (Vector2)transform.position;
            var intent = _motor.Intent;

            // --- stuck detection -----------------------------------------------------------------
            bool embedded = IsEmbedded();
            bool still = (pos - _lastPos).sqrMagnitude < 0.0004f;
            bool wantsToMove = Mathf.Abs(intent.Move.x) > 0.3f || intent.Jump.Held;
            _stillFor = !_motor.IsGrounded && still && wantsToMove ? _stillFor + dt : 0f;
            _stuckFor = embedded || _stillFor > 1.2f ? _stuckFor + dt : 0f;
            _lastPos = pos;
            if (IsStuck && !_hinted)
            {
                _hinted = true;
                EventBus<GameHint>.Raise(new GameHint { id = "jetpack_stuck", text = "STUCK? TAP JET TO WARP TO SAFE GROUND" });
            }

            // --- safe spot: solid ground, not a moving platform, not inside anything -------------------
            _safeTimer -= dt;
            if (_motor.IsGrounded && !embedded && _motor.Platform == null && _safeTimer <= 0f)
            {
                _safeTimer = SafeSampleSeconds;
                _lastSafe = pos;
                _hasSafe = true;
            }

            if (_motor.IsGrounded && !_flying) _fuel = Mathf.Min(tankSeconds, _fuel + refillPerSecond * dt);

            // --- JET ----------------------------------------------------------------------------
            bool pressed = intent.Jetpack.IsBuffered(Time.time);
            bool held = intent.Jetpack.Held;
            if (pressed && IsStuck && _hasSafe)
            {
                intent.Jetpack.Consume(Time.time);
                Warp();
                return;
            }
            if (held && _fuel > 0f && !_motor.IsControlLocked)
            {
                if (!_flying) { _flying = true; Sfx.Play(Sfx.Jetpack, 0.8f, 0.02f); }
                _fuel = Mathf.Max(0f, _fuel - dt);
                _motor.Thrust(liftSpeed, liftAccel);
                Exhaust(dt);
            }
            else _flying = false;
        }

        void Exhaust(float dt)
        {
            _puffTimer -= dt;
            _sfxTimer -= dt;
            Vector2 nozzle = (Vector2)transform.position + new Vector2(-0.15f * _motor.Facing, -0.55f);
            if (_puffTimer <= 0f)
            {
                _puffTimer = 0.05f;
                PuffEffect.Play(nozzle, Random.value < 0.5f ? new Color(1f, 0.75f, 0.25f) : new Color(1f, 0.45f, 0.15f),
                                _material, 0.12f, 2.5f, 0.25f);
            }
            if (_sfxTimer <= 0f)
            {
                _sfxTimer = 0.18f;
                Sfx.Play(Sfx.Jetpack, 0.45f, 0.1f);
            }
        }

        /// <summary>Back to the last safe ground, with a puff at both ends.</summary>
        public void Warp()
        {
            Vector2 from = transform.position;
            PuffEffect.Play(from, new Color(0.6f, 0.9f, 1f), _material, 0.3f, 5f, 0.4f);
            _motor.Teleport(_lastSafe + Vector2.up * 0.1f);
            PuffEffect.Play(_lastSafe, new Color(0.6f, 0.9f, 1f), _material, 0.3f, 5f, 0.4f);
            Sfx.Play(Sfx.Heal, 0.9f, 0f);
            if (Services.Haptics != null) Services.Haptics.Play(HapticStrength.Medium);
            _stuckFor = _stillFor = 0f;
            _fuel = tankSeconds;
        }

        bool IsEmbedded()
        {
            if (_body == null) return false;
            var b = _body.bounds;
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = PKRLayers.GroundMask, useTriggers = false };
            // A box well inside the hero: touching floors and walls doesn't count, being inside solid ground does.
            int n = Physics2D.OverlapBox(b.center, b.size * 0.45f, 0f, filter, Overlaps);
            for (int i = 0; i < n; i++)
            {
                var c = Overlaps[i];
                if (c == null || c == _body || c.usedByEffector) continue; // one-way planks are fine to stand in
                if (c.transform.IsChildOf(transform)) continue;
                return true;
            }
            return false;
        }
    }
}
