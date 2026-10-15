using PKR.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PKR
{
    /// <summary>
    /// The hero on foot in Zombie Land (top-down): moves with the stick, auto-aims at the nearest of the dead in front,
    /// FIRE shoots (hold for automatics), ATTACK is a knife slash, DODGE rolls, SPECIAL swaps guns, USE enters
    /// vehicles and works things. Health 100, armor soaks most of a hit until it's gone.
    /// </summary>
    public class ZLPlayer : MonoBehaviour
    {
        public static ZLPlayer Current { get; private set; }

        public ZLGame game;
        public CharacterDefinition hero;
        public Material material;

        public int Health { get; private set; } = ZLRules.HeroHealth;
        public bool Dead => Health <= 0;
        public ZLVehicle Driving { get; private set; }
        public Vector2 Aim { get; private set; } = Vector2.right;
        public ZLGun Gun => game != null ? ZLCatalog.Gun(game.Progress.equipped) : ZLCatalog.Pistol;
        public ZLOwnedGun Ammo => game != null ? game.Progress.Gun(game.Progress.equipped) : null;
        public bool Reloading => _reload > 0f;
        public float Reload01 => Gun != null && _reload > 0f ? 1f - _reload / Gun.reloadSeconds : 1f;
        public IZLUse Nearby { get; private set; }
        public Rigidbody2D Body { get; private set; }
        public float HurtFlash { get; private set; }

        PlayerInputRouter _router;
        FighterIntent Intent => _router != null ? _router.Intent : null;
        InputAction _fire, _use;
        TouchControlsUI _touch;
        SpriteRenderer _sr;
        CircleCollider2D _col;
        readonly ZLAnim _anim = new ZLAnim();
        float _cool, _reload, _roll, _rollCool, _melee, _invuln, _animT, _flashT;
        Vector2 _rollDir;
        bool _usePrev, _firePrev;
        SpriteRenderer _flash;

        public void Init(ZLGame g, CharacterDefinition def, Material mat, Color tint)
        {
            game = g; hero = def; material = mat;
            Current = this;
            Body = gameObject.AddComponent<Rigidbody2D>();
            Body.gravityScale = 0f;
            Body.freezeRotation = true;
            Body.interpolation = RigidbodyInterpolation2D.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _col = gameObject.AddComponent<CircleCollider2D>();
            _col.radius = 0.32f;
            var art = new GameObject("Art");
            art.transform.SetParent(transform, false);
            art.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            art.transform.localScale = Vector3.one * 0.75f;
            _sr = art.AddComponent<SpriteRenderer>();
            _sr.color = tint;
            if (mat != null) _sr.sharedMaterial = mat;
            if (def != null && def.frames != null)
            {
                _anim.idle = def.frames.idle; _anim.move = def.frames.run; _anim.hurt = def.frames.hurt;
            }
            if (_anim.idle == null || _anim.idle.Length == 0) _anim.idle = new[] { def != null ? def.bodySprite : null };
            var fl = new GameObject("Flash");
            fl.transform.SetParent(transform, false);
            _flash = fl.AddComponent<SpriteRenderer>();
            _flash.sprite = GlowHalo.Sprite;
            _flash.color = new Color(1f, 0.85f, 0.4f, 0f);
            _flash.sortingOrder = ZLSort.Canopy;
            fl.transform.localScale = Vector3.one * 0.6f;
            _router = gameObject.AddComponent<PlayerInputRouter>();
            _fire = new InputAction("ZLFire", InputActionType.Button);
            _fire.AddBinding("<Keyboard>/h"); _fire.AddBinding("<Mouse>/leftButton"); _fire.AddBinding("<Gamepad>/rightTrigger");
            _use = new InputAction("ZLUse", InputActionType.Button);
            _use.AddBinding("<Keyboard>/e"); _use.AddBinding("<Gamepad>/leftTrigger");
            _fire.Enable(); _use.Enable();
            _touch = Object.FindAnyObjectByType<TouchControlsUI>();
        }

        void OnDestroy()
        {
            _fire?.Dispose(); _use?.Dispose();
            if (Current == this) Current = null;
        }

        bool FireHeld => (_fire != null && _fire.IsPressed()) || (_touch != null && _touch.Visible && _touch.IsHeld(ControlIds.Fire));
        bool UsePressed
        {
            get
            {
                bool p = (_use != null && _use.WasPressedThisFrame()) || (_touch != null && _touch.Visible && _touch.ConsumePressed(ControlIds.Use));
                return p;
            }
        }

        public void SetHealth(int hp) => Health = Mathf.Clamp(hp, 0, ZLRules.HeroHealth);

        public void Heal(int amount)
        {
            if (Dead) return;
            Health = Mathf.Min(ZLRules.HeroHealth, Health + amount);
            ZLFloater.Show("+" + amount, transform.position, new Color(0.5f, 1f, 0.5f));
            Sfx.Play(Sfx.Heal, 0.7f);
        }

        public void Hurt(int damage, Vector2 from)
        {
            if (Dead || _invuln > 0f || game == null || game.Paused) return;
            if (Driving != null) { Driving.Damage(damage); return; }
            var p = game.Progress;
            if (p.armor > 0)
            {
                int soak = Mathf.Min(p.armor, Mathf.CeilToInt(damage * 0.6f));
                p.armor -= soak;
                damage -= soak;
            }
            Health = Mathf.Max(0, Health - damage);
            _invuln = 0.35f;
            HurtFlash = 0.25f;
            Sfx.Play(Sfx.Hurt, 0.8f);
            if (Services.Haptics != null) Services.Haptics.Play(HapticStrength.Medium);
            Vector2 push = ((Vector2)transform.position - from).normalized;
            Body.linearVelocity += push * 4f;
            if (Health <= 0) game.PlayerDied();
        }

        public void EnterVehicle(ZLVehicle v)
        {
            Driving = v;
            _col.enabled = false;
            _sr.enabled = false;
            Body.simulated = false;
            Sfx.Play(Sfx.Click, 0.8f, 0f);
        }

        public void ExitVehicle(Vector2 at)
        {
            Driving = null;
            transform.position = at;
            Body.simulated = true;
            Body.linearVelocity = Vector2.zero;
            _col.enabled = true;
            _sr.enabled = true;
            _invuln = 0.6f;
        }

        public void Revive(Vector2 at)
        {
            if (Driving != null) Driving.Eject(false);
            Driving = null;
            transform.position = at;
            Body.simulated = true;
            Body.linearVelocity = Vector2.zero;
            _col.enabled = true;
            _sr.enabled = true;
            Health = ZLRules.HeroHealth;
            _invuln = 2f;
            _reload = 0f;
        }

        void Update()
        {
            if (game == null || game.Paused || Dead) { if (Body != null && Driving == null) Body.linearVelocity = Vector2.zero; return; }
            float dt = Time.deltaTime;
            _cool -= dt; _rollCool -= dt; _melee -= dt; _invuln -= dt; HurtFlash -= dt; _flashT -= dt;
            if (_flash != null) _flash.color = new Color(1f, 0.85f, 0.4f, Mathf.Clamp01(_flashT * 10f));
            var intent = Intent;
            Vector2 move = intent != null ? intent.Move : Vector2.zero;

            // Interactables (also while driving: USE gets out).
            Nearby = Driving != null ? null : game.NearestUse(transform.position);
            if (UsePressed)
            {
                if (Driving != null) Driving.Eject(true);
                else if (Nearby != null) Nearby.Use(this);
            }
            if (Driving != null)
            {
                Driving.Drive(move);
                transform.position = Driving.transform.position;
                return;
            }

            // Roll
            if (intent != null && intent.Dodge.Consume(Time.time) && _rollCool <= 0f)
            {
                _rollDir = move.sqrMagnitude > 0.04f ? move.normalized : Aim;
                _roll = ZLRules.RollTime;
                _rollCool = ZLRules.RollCooldown;
                _invuln = Mathf.Max(_invuln, ZLRules.RollTime);
                Sfx.Play(Sfx.Jump, 0.5f);
            }
            if (_roll > 0f)
            {
                _roll -= dt;
                Body.linearVelocity = _rollDir * ZLRules.RollSpeed;
            }
            else
            {
                float speed = move.magnitude > 0.92f && !FireHeld ? ZLRules.SprintSpeed : ZLRules.WalkSpeed;
                if (game.Progress.rewards.Contains("fleet")) speed *= 1.1f;
                Body.linearVelocity = Vector2.ClampMagnitude(move, 1f) * speed;
            }

            // Aim: the nearest visible enemy in a wide cone ahead (or anywhere close), else where we're walking.
            var gun = Gun;
            var target = game.AutoAim(transform.position, move.sqrMagnitude > 0.04f ? move.normalized : Aim, gun != null ? gun.range : 8f);
            if (target.HasValue) Aim = (target.Value - (Vector2)transform.position).normalized;
            else if (move.sqrMagnitude > 0.04f) Aim = move.normalized;

            // Swap guns
            if (intent != null && intent.Special.Consume(Time.time)) game.SwapGun();

            // Knife
            if (intent != null && intent.Attack.Consume(Time.time) && _melee <= 0f)
            {
                _melee = 0.45f;
                game.Melee(transform.position, Aim);
            }

            // Guns
            if (_reload > 0f)
            {
                _reload -= dt;
                if (_reload <= 0f) FinishReload();
            }
            bool held = FireHeld;
            bool pressed = held && !_firePrev;
            _firePrev = held;
            var ammo = Ammo;
            if (gun != null && ammo != null && _reload <= 0f && _cool <= 0f && (pressed || (held && gun.automatic)))
            {
                if (ammo.mag > 0) Shoot(gun, ammo);
                else if (ammo.reserve > 0) StartReload();
                else if (pressed) { ZLFloater.Show("NO AMMO", transform.position, new Color(1f, 0.5f, 0.4f)); Sfx.Play(Sfx.Click, 0.6f, 0f); }
            }
            if (gun != null && ammo != null && ammo.mag == 0 && ammo.reserve > 0 && _reload <= 0f && !held) StartReload();
        }

        void Shoot(ZLGun gun, ZLOwnedGun ammo)
        {
            ammo.mag--;
            _cool = 1f / gun.shotsPerSecond;
            Vector2 from = (Vector2)transform.position + Aim * 0.5f + new Vector2(0f, 0.2f);
            int pellets = Mathf.Max(1, gun.pellets);
            for (int i = 0; i < pellets; i++)
            {
                float spread = gun.spreadDegrees * (pellets > 1 ? (i / (float)(pellets - 1) - 0.5f) : (Random.value - 0.5f));
                var dir = Quaternion.Euler(0f, 0f, spread) * Aim;
                ZLBullet.Fire(from, dir, gun, gun.damage, false, game.ShotSprite(gun.shot), new Color32(gun.r, gun.g, gun.b, 255), material, _col);
            }
            _flashT = 0.08f;
            if (_flash != null) _flash.transform.localPosition = Aim * 0.6f + new Vector2(0f, 0.25f);
            Sfx.Play(Sfx.Shoot, 0.55f);
            game.MakeNoise(transform.position, gun.noise);
            if (ammo.mag == 0 && ammo.reserve > 0) StartReload();
        }

        void StartReload()
        {
            var gun = Gun;
            if (gun == null) return;
            _reload = gun.reloadSeconds;
            Sfx.Play(Sfx.Reload, 0.6f, 0f);
        }

        void FinishReload()
        {
            var gun = Gun;
            var ammo = Ammo;
            if (gun == null || ammo == null) return;
            int need = gun.magazine - ammo.mag;
            int take = Mathf.Min(need, ammo.reserve);
            ammo.mag += take;
            ammo.reserve -= take;
        }

        public void CancelReload() => _reload = 0f;

        void LateUpdate()
        {
            if (_sr == null) return;
            _animT += Time.deltaTime;
            bool moving = Body != null && Body.linearVelocity.sqrMagnitude > 0.3f;
            var s = _anim.Pick(moving, HurtFlash > 0f, _animT);
            if (s != null) _sr.sprite = s;
            if (Mathf.Abs(Aim.x) > 0.05f) _sr.flipX = Aim.x < 0f;
            _sr.sortingOrder = ZLSort.ByY(transform.position.y);
            float blink = _invuln > 0f && HurtFlash <= 0f && Mathf.Repeat(_animT * 12f, 1f) < 0.3f ? 0.5f : 1f;
            var c = _sr.color; c.a = blink; _sr.color = c;
            if (_roll > 0f) _sr.transform.localRotation = Quaternion.Euler(0f, 0f, (_rollDir.x >= 0 ? -1 : 1) * (1f - _roll / ZLRules.RollTime) * 360f);
            else _sr.transform.localRotation = Quaternion.identity;
        }
    }
}
