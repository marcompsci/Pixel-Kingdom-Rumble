using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Fighter animation: frame-by-frame clips from the CharacterDefinition (idle, run, jump, fall, attack, special,
    /// hurt) layered with procedural juice (facing flip, squash and stretch on jump/land, run bob, air tilt, a hit
    /// flinch, a K.O. pose, blink during i-frames) and a soft ground shadow. Fighters without frames (enemies) keep
    /// the procedural animation on their single sprite. Lives on the root; drives a child sprite (bottom pivot).
    /// </summary>
    public class FighterVisual : MonoBehaviour
    {
        [SerializeField] Transform body;
        [SerializeField] SpriteRenderer bodyRenderer;
        [SerializeField] float squashRecover = 12f;
        [Tooltip("Frame-by-frame art comes from this hero (empty: single sprite, procedural only).")]
        [SerializeField] CharacterDefinition character;
        [SerializeField] bool groundShadow = true;
        [SerializeField] float idleFps = 6f;

        PlatformerMotor2D _motor;
        HeroAbilities _abilities;
        Invulnerability _invuln;
        AttackRunner _attacks;
        Damageable _health;
        Vector3 _baseScale = Vector3.one;
        Vector3 _basePos;
        Vector2 _squash = Vector2.one;
        float _runPhase, _tilt, _flinch, _breath, _clock;
        bool _down;
        SpriteRenderer _shadow;
        static Sprite _shadowSprite;
        static readonly RaycastHit2D[] ShadowHits = new RaycastHit2D[4];

        /// <summary>Use this hero's animation frames (called when a scene's hero is swapped).</summary>
        public void SetCharacter(CharacterDefinition def)
        {
            character = def;
            _clock = 0f;
            if (bodyRenderer != null && def != null && def.bodySprite != null) bodyRenderer.sprite = def.bodySprite;
        }

        /// <summary>Use these frames directly (enemies, which have no CharacterDefinition).</summary>
        public void SetFrames(FighterFrames frames)
        {
            _frames = frames;
            _clock = Random.value * 2f; // so a group of the same enemy doesn't animate in lockstep
            var first = frames != null ? (FighterFrames.Pick(frames.idle, 0) ?? FighterFrames.Pick(frames.run, 0)) : null;
            if (bodyRenderer != null && first != null) bodyRenderer.sprite = first;
        }

        public CharacterDefinition Character => character;
        FighterFrames _frames;
        FighterFrames Frames => _frames != null ? _frames : (character != null ? character.frames : null);
        bool HasFrames => Frames != null && Frames.HasAny;

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
            if (groundShadow) CreateShadow();
        }

        void CreateShadow()
        {
            if (_shadowSprite == null) _shadowSprite = Resources.Load<Sprite>("PKR/fx_shadow");
            if (_shadowSprite == null || bodyRenderer == null) return;
            var go = new GameObject("Shadow");
            go.transform.SetParent(transform, false); // after Body, so "first child sprite" lookups still find the body
            _shadow = go.AddComponent<SpriteRenderer>();
            _shadow.sprite = _shadowSprite;
            _shadow.sharedMaterial = bodyRenderer.sharedMaterial;
            _shadow.sortingLayerID = bodyRenderer.sortingLayerID;
            _shadow.sortingOrder = bodyRenderer.sortingOrder - 1;
        }

        void UpdateShadow()
        {
            if (_shadow == null || body == null) return;
            Vector2 feet = (Vector2)transform.position + (Vector2)_basePos;
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = PKRLayers.GroundMask, useTriggers = false };
            int n = Physics2D.Raycast(feet + Vector2.up * 0.1f, Vector2.down, filter, ShadowHits, 6f);
            float best = float.MaxValue;
            Vector2 at = feet;
            for (int i = 0; i < n; i++)
            {
                if (ShadowHits[i].collider == null || ShadowHits[i].collider.isTrigger) continue;
                if (ShadowHits[i].distance < best) { best = ShadowHits[i].distance; at = ShadowHits[i].point; }
            }
            bool show = best < 6f && !_down && bodyRenderer != null && bodyRenderer.enabled;
            _shadow.enabled = show;
            if (!show) return;
            float t = Mathf.Clamp01((best - 0.1f) / 4f);
            _shadow.transform.position = new Vector3(feet.x, at.y + 0.02f, transform.position.z);
            float w = Mathf.Lerp(1f, 0.45f, t) * Mathf.Abs(_baseScale.x);
            _shadow.transform.localScale = new Vector3(w, w, 1f);
            _shadow.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.85f, 0.2f, t));
        }

        static bool IsSpecial(MoveDefinition m, Moveset ms)
        {
            if (m == null) return false;
            if (m.spawnsProjectile || m.spawnsTrap) return true;
            return ms != null && (m == ms.groundSpecial || m == ms.airSpecial || m == ms.sideSpecial || m == ms.downSpecial);
        }

        /// <summary>Chooses the animation frame for this tick (null = keep the current sprite).</summary>
        Sprite PickFrame(bool grounded, bool abilityIdle, float speed01, float vy, bool attacking)
        {
            var f = Frames;
            if (_down || _flinch > 0.3f) return FighterFrames.Pick(f.hurt, 0);
            if (attacking)
            {
                var m = _attacks.Current;
                var fd = m.frames;
                int fr = _attacks.Frame;
                int phase = fr < fd.startup ? 0 : (fr < fd.startup + Mathf.Max(1, fd.active) + 2 ? 1 : 2);
                if (IsSpecial(m, _attacks.Moveset) && f.special != null && f.special.Length > 0)
                    return f.special[Mathf.Min(phase == 0 ? 0 : 1, f.special.Length - 1)];
                return FighterFrames.Pick(f.attack, Mathf.Min(phase, f.attack.Length - 1));
            }
            if (!abilityIdle && _abilities != null && _abilities.Current == HeroAbilities.State.Roll)
                return FighterFrames.Pick(f.jump, 0);
            if (!grounded)
            {
                // Multi-frame air clips loop (wing beats, propellers); hero clips are single poses.
                var air = vy > 0.5f || f.fall == null || f.fall.Length == 0 ? f.jump : f.fall;
                if (air == null || air.Length == 0) air = f.idle;
                return FighterFrames.Pick(air, air != null && air.Length > 1 ? Mathf.FloorToInt(_clock * 12f) : 0);
            }
            if (speed01 > 0.08f && f.run != null && f.run.Length > 0)
            {
                int i = Mathf.FloorToInt(_runPhase / (Mathf.PI * 2f) * f.run.Length);
                return FighterFrames.Pick(f.run, i);
            }
            return FighterFrames.Pick(f.idle, Mathf.FloorToInt(_clock * idleFps));
        }

        void OnEnable()
        {
            if (_motor == null) return;
            _motor.Jumped += OnJumped;
            _motor.AirJumped += OnAirJumped;
            _motor.Landed += OnLanded;
            if (_health != null) _health.Hit += OnHit;
        }

        void OnDisable()
        {
            if (_motor == null) return;
            _motor.Jumped -= OnJumped;
            _motor.AirJumped -= OnAirJumped;
            _motor.Landed -= OnLanded;
            if (_health != null) _health.Hit -= OnHit;
        }

        void OnHit(PKR.Core.HitResult r)
        {
            _flinch = 1f;
            _squash = new Vector2(1.2f, 0.85f);
        }

        void OnJumped() => _squash = new Vector2(0.75f, 1.3f);

        /// <summary>Double jump: a front flip, a ring of sparkles under the feet and a brighter jump sound.</summary>
        void OnAirJumped()
        {
            _flip = 1f;
            Sfx.Play(Sfx.AirJump, 0.8f, 0.04f);
            var mat = bodyRenderer != null ? bodyRenderer.sharedMaterial : null;
            Vector2 feet = (Vector2)transform.position + (Vector2)_basePos;
            PuffEffect.Play(feet, new Color(1f, 1f, 1f, 0.9f), mat, 0.14f, 4f, 0.3f);
            PuffEffect.Play(feet, new Color(0.6f, 0.9f, 1f, 0.9f), mat, 0.1f, 2.5f, 0.3f);
        }

        float _flip; // 1 -> 0 over a double jump's front flip

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
            bool frames = HasFrames;
            _clock += dt;
            if (_motor.IsGrounded && abilityIdle && speed01 > 0.08f)
            {
                _runPhase += dt * Mathf.Lerp(8f, 18f, speed01) * (frames ? 0.85f : 1f);
                bob = frames ? 0f : Mathf.Abs(Mathf.Sin(_runPhase)) * 0.09f * speed01;
                tiltTarget = (frames ? -2f : -6f) * speed01;
            }
            else if (_motor.IsGrounded && abilityIdle)
            {
                _breath += dt * 2.2f;
                scaleY = 1f + Mathf.Sin(_breath) * (frames ? 0.01f : 0.025f);
            }
            else if (!_motor.IsGrounded && abilityIdle)
            {
                tiltTarget = Mathf.Clamp(-vy * 1.2f, -12f, 12f) * 0.5f - 4f; // nose up rising, down falling
            }
            if (attacking) { tiltTarget = frames ? -3f : -14f; target = frames ? new Vector2(1.04f, 0.98f) : new Vector2(1.12f, 0.94f); }
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
                float flipDeg = 0f;
                if (_flip > 0f)
                {
                    _flip = Mathf.Max(0f, _flip - dt / 0.32f);
                    flipDeg = -360f * (1f - _flip) * (1f - _flip * 0.15f);   // eased front flip
                    if (_flip <= 0f) flipDeg = 0f;
                }
                body.localRotation = Quaternion.Euler(0f, 0f, (_tilt + flipDeg) * _motor.Facing);
                body.localPosition = _basePos + new Vector3(0f, bob, 0f);
                if (flipDeg != 0f)
                {
                    // Spin around the body's middle, not the feet pivot.
                    var mid = new Vector3(0f, 0.75f * Mathf.Abs(_baseScale.y), 0f);
                    body.localPosition += mid - Quaternion.Euler(0f, 0f, flipDeg * _motor.Facing) * mid;
                }
                body.localScale = new Vector3(
                    _baseScale.x * _squash.x * _motor.Facing,
                    _baseScale.y * _squash.y * scaleY,
                    _baseScale.z);
            }

            if (frames && bodyRenderer != null)
            {
                var frame = PickFrame(_motor.IsGrounded, abilityIdle, speed01, vy, attacking);
                if (frame != null) bodyRenderer.sprite = frame;
            }

            if (bodyRenderer != null)
            {
                bool hidden = _invuln != null && _invuln.IsBlinking && Mathf.FloorToInt(Time.time * 20f) % 2 == 0;
                bodyRenderer.enabled = !hidden;
            }
            UpdateShadow();
        }
    }
}
