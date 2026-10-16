using PKR.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PKR
{
    /// <summary>Who drives a hero: the person holding this phone, an AI teammate, a player online (on the host), or a
    /// puppet that only mirrors what the host says (on a guest's phone).</summary>
    public enum ZLControl { Local, AI, Remote, Puppet }

    /// <summary>One frame of controls for a hero, from touch, an AI brain or the network.</summary>
    public struct ZLInputFrame
    {
        public Vector2 move;
        /// <summary>Where to face when there's nothing to auto-aim at (AI); zero = the way you're walking.</summary>
        public Vector2 face;
        public bool fire, useHeld, usePressed, dodge, melee, swap;
    }

    /// <summary>Something that decides an AI or remote hero's controls each frame.</summary>
    public interface IZLBrain
    {
        ZLInputFrame Think(ZLPlayer self, float dt);
    }

    /// <summary>
    /// A hero on foot in Zombie Land / Last Stand (top-down): moves with the stick, auto-aims at the nearest of the dead
    /// in front, FIRE shoots (hold for automatics), ATTACK is a knife slash, DODGE rolls, SPECIAL swaps guns, USE enters
    /// vehicles and works things. Health 100, armor soaks most of a hit until it's gone. In Last Stand co-op a hero
    /// who falls is DOWNED: a teammate holding USE next to them for three seconds gets them back up.
    /// </summary>
    public class ZLPlayer : MonoBehaviour
    {
        /// <summary>The hero of the person holding this phone.</summary>
        public static ZLPlayer Current { get; private set; }

        public ZLGame game;
        public CharacterDefinition hero;
        public Material material;

        public ZLControl Control { get; private set; } = ZLControl.Local;
        public IZLBrain Brain { get; set; }
        public int Slot { get; set; }
        public string DisplayName { get; set; } = "";
        /// <summary>Guns, ammo, armor, medkits and perks (Zombie Land: the save; Last Stand: this hero's own).</summary>
        public ZombieLandProgress Loadout { get; set; }
        /// <summary>Last Stand: this hero's points.</summary>
        public SurvivalRun Wallet { get; set; }
        public int Kills { get; set; }

        public int Health { get; private set; } = ZLRules.HeroHealth;
        public bool Dead => Health <= 0;
        /// <summary>Co-op: down, waiting for a revive (bleeds out after a while).</summary>
        public bool Downed { get; private set; }
        public float BleedOut { get; private set; }
        public float ReviveProgress { get; set; }
        /// <summary>Bled out: sits the round out and comes back with the next one.</summary>
        public bool Out { get; private set; }
        public bool Standing => !Dead && !Downed && !Out;
        public ZLVehicle Driving { get; private set; }
        public Vector2 Aim { get; private set; } = Vector2.right;
        ZombieLandProgress L => Loadout ?? (game != null ? game.Progress : null);
        /// <summary>The gun in hand as it really is (hollow-forged guns are stronger).</summary>
        public ZLGun Gun => L != null ? ZLShop.Effective(L, ZLCatalog.Gun(L.equipped)) : ZLCatalog.Pistol;
        public int MaxHealth => L != null ? ZLShop.MaxHealth(L) : ZLRules.HeroHealth;
        public ZLOwnedGun Ammo => L != null ? L.Gun(L.equipped) : null;
        public bool Reloading => _reload > 0f;
        public float Reload01 => Gun != null && _reload > 0f ? 1f - _reload / ReloadTime : 1f;
        float ReloadTime => Gun != null ? Gun.reloadSeconds * (L != null ? ZLShop.ReloadScale(L) : 1f) : 1f;
        public IZLUse Nearby { get; private set; }
        /// <summary>A downed teammate close enough to revive (with USE held).</summary>
        public ZLPlayer ReviveTarget { get; private set; }
        public Rigidbody2D Body { get; private set; }
        public float HurtFlash { get; private set; }
        /// <summary>Tests and screenshots: nothing hurts the hero.</summary>
        public bool DebugInvulnerable { get; set; }
        /// <summary>The controls used last frame (an online guest sends these to the host).</summary>
        public ZLInputFrame LastFrame { get; private set; }
        public bool FiringNow { get; private set; }

        PlayerInputRouter _router;
        FighterIntent Intent => _router != null ? _router.Intent : null;
        InputAction _fire, _use;
        TouchControlsUI _touch;
        SpriteRenderer _sr, _ring;
        CircleCollider2D _col;
        TextMesh _nameTag;
        readonly ZLAnim _anim = new ZLAnim();
        float _cool, _reload, _roll, _rollCool, _melee, _invuln, _animT, _flashT;
        Vector2 _rollDir, _puppetTarget;
        bool _firePrev;
        SpriteRenderer _flash;

        public void Init(ZLGame g, CharacterDefinition def, Material mat, Color tint) => Init(g, def, mat, tint, ZLControl.Local);

        public void Init(ZLGame g, CharacterDefinition def, Material mat, Color tint, ZLControl control)
        {
            game = g; hero = def; material = mat;
            Control = control;
            if (control == ZLControl.Local) Current = this;
            Body = gameObject.AddComponent<Rigidbody2D>();
            Body.gravityScale = 0f;
            Body.freezeRotation = true;
            Body.interpolation = RigidbodyInterpolation2D.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            if (control == ZLControl.Puppet || control == ZLControl.Remote) Body.bodyType = RigidbodyType2D.Kinematic;
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
            if (control == ZLControl.Local)
            {
                _router = gameObject.AddComponent<PlayerInputRouter>();
                _fire = new InputAction("ZLFire", InputActionType.Button);
                _fire.AddBinding("<Keyboard>/h"); _fire.AddBinding("<Mouse>/leftButton"); _fire.AddBinding("<Gamepad>/rightTrigger");
                _use = new InputAction("ZLUse", InputActionType.Button);
                _use.AddBinding("<Keyboard>/e"); _use.AddBinding("<Gamepad>/leftTrigger");
                _fire.Enable(); _use.Enable();
                _touch = Object.FindAnyObjectByType<TouchControlsUI>();
            }
            Health = MaxHealth;
            _puppetTarget = transform.position;
        }

        /// <summary>Co-op: a coloured ring under the hero and a name over them, so the squad is easy to tell apart.</summary>
        public void ShowSquadMarker(Color color, string name)
        {
            DisplayName = name;
            var r = new GameObject("Ring");
            r.transform.SetParent(transform, false);
            _ring = r.AddComponent<SpriteRenderer>();
            _ring.sprite = GlowHalo.Sprite;
            _ring.color = new Color(color.r, color.g, color.b, 0.45f);
            _ring.sortingOrder = ZLSort.Decal + 3;
            r.transform.localScale = new Vector3(1.1f, 0.6f, 1f);
            if (Control == ZLControl.Local) return;
            var lg = new GameObject("Name");
            lg.transform.SetParent(transform, false);
            lg.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            _nameTag = lg.AddComponent<TextMesh>();
            _nameTag.font = UISprites.DisplayFont;
            _nameTag.fontSize = 40;
            _nameTag.characterSize = 0.055f;
            _nameTag.anchor = TextAnchor.MiddleCenter;
            _nameTag.color = color;
            _nameTag.text = name;
            var mr = lg.GetComponent<MeshRenderer>();
            mr.sharedMaterial = UISprites.DisplayFont.material;
            mr.sortingOrder = ZLSort.Overhead;
        }

        void OnDestroy()
        {
            _fire?.Dispose(); _use?.Dispose();
            if (Current == this) Current = null;
        }

        bool FireHeldLocal => (_fire != null && _fire.IsPressed()) || (_touch != null && _touch.Visible && _touch.IsHeld(ControlIds.Fire));
        bool UseHeldLocal => (_use != null && _use.IsPressed()) || (_touch != null && _touch.Visible && _touch.IsHeld(ControlIds.Use));
        bool UsePressedLocal => (_use != null && _use.WasPressedThisFrame()) || (_touch != null && _touch.Visible && _touch.ConsumePressed(ControlIds.Use));

        ZLInputFrame LocalFrame()
        {
            var intent = Intent;
            var f = new ZLInputFrame
            {
                move = intent != null ? intent.Move : Vector2.zero,
                fire = FireHeldLocal,
                useHeld = UseHeldLocal,
                usePressed = UsePressedLocal,
            };
            if (intent != null)
            {
                f.dodge = intent.Dodge.Consume(Time.time);
                f.swap = intent.Special.Consume(Time.time);
                f.melee = intent.Attack.Consume(Time.time);
            }
            return f;
        }

        public void SetHealth(int hp)
        {
            Health = Mathf.Clamp(hp, 0, MaxHealth);
        }

        public void Heal(int amount)
        {
            if (Dead) return;
            Health = Mathf.Min(MaxHealth, Health + amount);
            if (Control != ZLControl.Puppet) ZLFloater.Show("+" + amount, transform.position, new Color(0.5f, 1f, 0.5f));
            if (Control == ZLControl.Local) Sfx.Play(Sfx.Heal, 0.7f);
        }

        public void Hurt(int damage, Vector2 from)
        {
            if (Dead || Out || _invuln > 0f || game == null || game.Paused || DebugInvulnerable || Control == ZLControl.Puppet) return;
            if (game.IsGuest) return;   // on a guest's phone the host decides who gets hurt
            if (Driving != null) { Driving.Damage(damage); return; }
            var p = L;
            if (p != null && p.armor > 0)
            {
                int soak = Mathf.Min(p.armor, Mathf.CeilToInt(damage * 0.6f));
                p.armor -= soak;
                damage -= soak;
            }
            Health = Mathf.Max(0, Health - damage);
            _invuln = 0.35f;
            HurtFlash = 0.25f;
            if (Control == ZLControl.Local)
            {
                Sfx.Play(Sfx.Hurt, 0.8f);
                if (Services.Haptics != null) Services.Haptics.Play(HapticStrength.Medium);
            }
            Vector2 push = ((Vector2)transform.position - from).normalized;
            if (Body.bodyType == RigidbodyType2D.Dynamic) Body.linearVelocity += push * 4f;
            if (Health <= 0) game.HeroFell(this);
        }

        // ---- co-op: down, revive, bleed out --------------------------------------------------------------

        public void GoDown()
        {
            Health = 0;
            Downed = true;
            BleedOut = SquadRules.BleedOutSeconds;
            ReviveProgress = 0f;
            _reload = 0f;
            if (Body != null) Body.linearVelocity = Vector2.zero;
        }

        /// <summary>Back on their feet (a teammate's revive, Second Chance, or a new round after bleeding out).</summary>
        public void StandUp(float healthShare)
        {
            Downed = false;
            Out = false;
            ReviveProgress = 0f;
            Health = Mathf.Max(1, Mathf.RoundToInt(MaxHealth * healthShare));
            _invuln = 2f;
            if (_sr != null) _sr.enabled = true;
            if (_col != null) _col.enabled = true;
        }

        public void BleedOutNow()
        {
            Downed = false;
            Out = true;
            Health = 0;
            if (_sr != null) _sr.enabled = false;
            if (_col != null) _col.enabled = false;
        }

        /// <summary>Guest's phone: the host says how this hero is doing.</summary>
        public void MirrorState(int hp, bool downed, bool isOut)
        {
            Health = Mathf.Max(0, hp);
            Downed = downed;
            Out = isOut;
            if (_sr != null) _sr.enabled = !isOut;
            if (_col != null) _col.enabled = !isOut;
        }

        // ---- vehicles, revive at a checkpoint ------------------------------------------------------------

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
            Downed = false;
            Out = false;
            Health = MaxHealth;
            _invuln = 2f;
            _reload = 0f;
        }

        /// <summary>An online player left: an AI teammate takes over this hero.</summary>
        public void BecomeAI(IZLBrain brain)
        {
            Control = ZLControl.AI;
            Brain = brain;
            if (Body != null) Body.bodyType = RigidbodyType2D.Dynamic;
        }

        /// <summary>Puppets and remote heroes: where the network says this hero is.</summary>
        public void SetNetPosition(Vector2 pos, Vector2 aim, bool snap = false)
        {
            _puppetTarget = pos;
            if (aim.sqrMagnitude > 0.01f) Aim = aim.normalized;
            if (snap) transform.position = pos;
        }

        // ---- the frame -----------------------------------------------------------------------------------

        void Update()
        {
            if (game == null) return;
            float dt = Time.deltaTime;
            HurtFlash -= dt; _flashT -= dt;
            if (_flash != null) _flash.color = new Color(1f, 0.85f, 0.4f, Mathf.Clamp01(_flashT * 10f));
            if (Control == ZLControl.Puppet)
            {
                // smooth toward the host's position
                Vector2 p = transform.position;
                float k = 1f - Mathf.Exp(-12f * dt);
                transform.position = (_puppetTarget - p).sqrMagnitude > 25f ? _puppetTarget : Vector2.Lerp(p, _puppetTarget, k);
                return;
            }
            if (game.Paused || Dead || Out)
            {
                if (Body != null && Driving == null && Body.bodyType == RigidbodyType2D.Dynamic) Body.linearVelocity = Vector2.zero;
                if (Downed && !game.IsGuest) game.TickDowned(this, dt);
                return;
            }
            _cool -= dt; _rollCool -= dt; _melee -= dt; _invuln -= dt;

            var frame = Control == ZLControl.Local ? LocalFrame() : Brain != null ? Brain.Think(this, dt) : default;
            LastFrame = frame;
            Vector2 move = frame.move;

            if (Control == ZLControl.Remote)
            {
                // An online player moves on their own phone; here the hero follows where they say they are.
                Vector2 p = transform.position;
                float k = 1f - Mathf.Exp(-15f * dt);
                var next = (_puppetTarget - p).sqrMagnitude > 25f ? _puppetTarget : Vector2.Lerp(p, _puppetTarget, k);
                Body.MovePosition(next);
                move = Vector2.zero;
            }

            // Revive a downed teammate (USE held next to them)
            ReviveTarget = game.DownedTeammateNear(this);
            if (ReviveTarget != null)
            {
                if (frame.useHeld && !game.IsGuest)
                {
                    ReviveTarget.ReviveProgress += dt;
                    if (ReviveTarget.ReviveProgress >= SquadRules.ReviveSeconds) game.Revived(ReviveTarget, this);
                }
                else if (!frame.useHeld) ReviveTarget.ReviveProgress = Mathf.Max(0f, ReviveTarget.ReviveProgress - dt);
            }

            // Interactables (also while driving: USE gets out).
            Nearby = Driving != null ? null : game.NearestUse(transform.position);
            if (frame.usePressed && ReviveTarget == null)
            {
                if (Driving != null) Driving.Eject(true);
                else if (Nearby != null && !game.IsGuest) Nearby.Use(this);
            }
            if (Driving != null)
            {
                Driving.Drive(move);
                transform.position = Driving.transform.position;
                return;
            }

            if (Control != ZLControl.Remote)
            {
                // Roll
                if (frame.dodge && _rollCool <= 0f)
                {
                    _rollDir = move.sqrMagnitude > 0.04f ? move.normalized : Aim;
                    _roll = ZLRules.RollTime;
                    _rollCool = ZLRules.RollCooldown;
                    _invuln = Mathf.Max(_invuln, ZLRules.RollTime);
                    if (Control == ZLControl.Local) Sfx.Play(Sfx.Jump, 0.5f);
                }
                if (_roll > 0f)
                {
                    _roll -= dt;
                    Body.linearVelocity = _rollDir * ZLRules.RollSpeed;
                }
                else
                {
                    float speed = move.magnitude > 0.92f && !frame.fire ? ZLRules.SprintSpeed : ZLRules.WalkSpeed;
                    if (L != null && L.rewards.Contains("fleet")) speed *= 1.1f;
                    speed *= L != null ? ZLShop.SpeedScale(L) : 1f;
                    if (ReviveTarget != null && frame.useHeld) speed = 0f;
                    Body.linearVelocity = Vector2.ClampMagnitude(move, 1f) * speed;
                }
            }

            // Aim: the nearest visible enemy in a wide cone ahead (or anywhere close), else where we're walking.
            var gun = Gun;
            Vector2 facing = frame.face.sqrMagnitude > 0.01f ? frame.face.normalized : move.sqrMagnitude > 0.04f ? move.normalized : Aim;
            if (Control == ZLControl.Remote) facing = Aim;
            var target = game.AutoAim(transform.position, facing, gun != null ? gun.range : 8f);
            if (target.HasValue) Aim = (target.Value - (Vector2)transform.position).normalized;
            else if (Control != ZLControl.Remote && move.sqrMagnitude > 0.04f) Aim = move.normalized;

            if (frame.swap) game.SwapGun(this);

            if (frame.melee && _melee <= 0f)
            {
                _melee = 0.45f;
                game.Melee(transform.position, Aim, this);
            }

            // Guns
            if (_reload > 0f)
            {
                _reload -= dt;
                if (_reload <= 0f) FinishReload();
            }
            bool held = frame.fire && !(ReviveTarget != null && frame.useHeld);
            bool pressed = held && !_firePrev;
            _firePrev = held;
            FiringNow = false;
            var ammo = Ammo;
            if (gun != null && ammo != null && _reload <= 0f && _cool <= 0f && (pressed || (held && gun.automatic)))
            {
                if (ammo.mag > 0) Shoot(gun, ammo);
                else if (ammo.reserve > 0) StartReload();
                else if (pressed && Control == ZLControl.Local) { ZLFloater.Show("NO AMMO", transform.position, new Color(1f, 0.5f, 0.4f)); Sfx.Play(Sfx.Click, 0.6f, 0f); }
            }
            if (gun != null && ammo != null && ammo.mag == 0 && ammo.reserve > 0 && _reload <= 0f && !held) StartReload();
        }

        void Shoot(ZLGun gun, ZLOwnedGun ammo)
        {
            ammo.mag--;
            _cool = 1f / gun.shotsPerSecond;
            FiringNow = true;
            Vector2 from = (Vector2)transform.position + Aim * 0.5f + new Vector2(0f, 0.2f);
            int pellets = Mathf.Max(1, gun.pellets);
            bool cosmetic = game.IsGuest;
            for (int i = 0; i < pellets; i++)
            {
                float spread = gun.spreadDegrees * (pellets > 1 ? (i / (float)(pellets - 1) - 0.5f) : (Random.value - 0.5f));
                var dir = Quaternion.Euler(0f, 0f, spread) * Aim;
                ZLBullet.Fire(from, dir, gun, cosmetic ? 0 : ZLShop.ShotDamage(L, ZLCatalog.Gun(gun.id)), false, game.ShotSprite(gun.shot),
                              new Color32(gun.r, gun.g, gun.b, 255), material, _col, 0f, 0f, this, cosmetic);
            }
            _flashT = 0.08f;
            if (_flash != null) _flash.transform.localPosition = Aim * 0.6f + new Vector2(0f, 0.25f);
            if (Control == ZLControl.Local) Sfx.Play(Sfx.Shoot, 0.55f);
            else if (Random.value < 0.4f) Sfx.Play(Sfx.Shoot, 0.25f);
            if (!cosmetic) game.HeroShot(this, from, Aim, gun);
            if (ammo.mag == 0 && ammo.reserve > 0) StartReload();
        }

        /// <summary>A guest's phone draws the other players' bullets (looks only).</summary>
        public void ShowShot(Vector2 from, Vector2 dir, ZLGun gun)
        {
            if (gun == null) return;
            int pellets = Mathf.Max(1, gun.pellets);
            for (int i = 0; i < pellets; i++)
            {
                float spread = gun.spreadDegrees * (pellets > 1 ? (i / (float)(pellets - 1) - 0.5f) : (Random.value - 0.5f));
                ZLBullet.Fire(from, Quaternion.Euler(0f, 0f, spread) * dir, gun, 0, false, game.ShotSprite(gun.shot), new Color32(gun.r, gun.g, gun.b, 255),
                              material, _col, 0f, 0f, this, true);
            }
            _flashT = 0.08f;
            if (_flash != null) _flash.transform.localPosition = dir * 0.6f + new Vector2(0f, 0.25f);
            if (Random.value < 0.4f) Sfx.Play(Sfx.Shoot, 0.25f);
        }

        void StartReload()
        {
            var gun = Gun;
            if (gun == null) return;
            _reload = ReloadTime;
            if (Control == ZLControl.Local) Sfx.Play(Sfx.Reload, 0.6f, 0f);
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
            bool moving = Control == ZLControl.Puppet || Control == ZLControl.Remote
                ? ((Vector2)transform.position - _puppetTarget).sqrMagnitude > 0.002f
                : Body != null && Body.linearVelocity.sqrMagnitude > 0.3f;
            var s = _anim.Pick(moving && !Downed, HurtFlash > 0f, _animT);
            if (s != null) _sr.sprite = s;
            if (Mathf.Abs(Aim.x) > 0.05f) _sr.flipX = Aim.x < 0f;
            _sr.sortingOrder = ZLSort.ByY(transform.position.y);
            float blink = _invuln > 0f && HurtFlash <= 0f && Mathf.Repeat(_animT * 12f, 1f) < 0.3f ? 0.5f : 1f;
            var c = _sr.color; c.a = blink; _sr.color = c;
            if (Downed) _sr.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            else if (_roll > 0f) _sr.transform.localRotation = Quaternion.Euler(0f, 0f, (_rollDir.x >= 0 ? -1 : 1) * (1f - _roll / ZLRules.RollTime) * 360f);
            else _sr.transform.localRotation = Quaternion.identity;
            if (_nameTag != null)
                _nameTag.text = Downed ? $"{DisplayName}  DOWN {Mathf.CeilToInt(BleedOut)}" : DisplayName;
        }

        /// <summary>Called by the game while this hero is downed (authority only).</summary>
        public void TickBleed(float dt) => BleedOut -= dt;
    }
}
