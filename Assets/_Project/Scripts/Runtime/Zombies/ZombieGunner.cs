using System.Collections.Generic;
using PKR.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PKR
{
    /// <summary>
    /// Zombie Survival: the hero's guns. Two slots (start with the Pea Popper), FIRE shoots (hold for automatics),
    /// magazines reload by themselves, push the stick up to aim high (bats). USE buys from stations and repairs
    /// barricades; with nothing nearby it swaps guns. Melee (ATTACK / SPECIAL) still works and pays a bonus for kills.
    /// Keyboard: H fire, E use. Gamepad: right trigger fire, left trigger use.
    /// </summary>
    [RequireComponent(typeof(PlatformerMotor2D))]
    public class ZombieGunner : MonoBehaviour
    {
        public static ZombieGunner Player { get; private set; }

        public class Slot
        {
            public ZombieWeapon spec;
            public int mag, reserve;
            public MoveDefinition move;
            public Color tint;
        }

        public Material material;
        readonly List<Slot> _slots = new List<Slot>();
        int _current;
        float _cooldown, _reloadLeft;
        PlatformerMotor2D _motor;
        Damageable _health;
        TouchControlsUI _touch;
        InputAction _fire, _use;
        bool _usePressed;
        static readonly Dictionary<string, Sprite> ShotArt = new Dictionary<string, Sprite>();

        public const int MaxSlots = 2;
        public float FireRateScale { get; set; } = 1f;
        public Slot Current => _current < _slots.Count ? _slots[_current] : null;
        public IReadOnlyList<Slot> Slots => _slots;
        public bool Reloading => _reloadLeft > 0f;
        public float Reload01 => Current != null && _reloadLeft > 0f ? 1f - _reloadLeft / (Current.spec.reloadSeconds / FireRateScale) : 1f;
        /// <summary>USE was pressed this frame (stations and barricades consume it).</summary>
        public bool UsePressed => _usePressed;
        public bool UseHeld => (_use != null && _use.IsPressed()) || (_touch != null && _touch.IsHeld(ControlIds.Use));

        void Awake()
        {
            _motor = GetComponent<PlatformerMotor2D>();
            _health = GetComponent<Damageable>();
            _fire = new InputAction("ZombieFire", InputActionType.Button);
            _fire.AddBinding("<Keyboard>/h");
            _fire.AddBinding("<Gamepad>/rightTrigger");
            _use = new InputAction("ZombieUse", InputActionType.Button);
            _use.AddBinding("<Keyboard>/e");
            _use.AddBinding("<Gamepad>/leftTrigger");
        }

        void OnEnable()
        {
            Player = this;
            _fire.Enable();
            _use.Enable();
        }

        void OnDisable()
        {
            if (Player == this) Player = null;
            _fire.Disable();
            _use.Disable();
        }

        void OnDestroy()
        {
            _fire.Dispose();
            _use.Dispose();
        }

        void Start()
        {
            _touch = FindAnyObjectByType<TouchControlsUI>();
            if (_slots.Count == 0) Give(ZombieArmory.PeaPopper);
        }

        // ---- inventory --------------------------------------------------------------------------------------------

        public bool Holds(string weaponId)
        {
            foreach (var s in _slots) if (s.spec.id == weaponId) return true;
            return false;
        }

        Slot Find(string weaponId)
        {
            foreach (var s in _slots) if (s.spec.id == weaponId) return s;
            return null;
        }

        /// <summary>Adds a gun (full ammo). With both slots full it replaces the one in hand.</summary>
        public void Give(ZombieWeapon w)
        {
            var existing = Find(w.id);
            if (existing != null)
            {
                Fill(existing, w);
                _current = _slots.IndexOf(existing);
                return;
            }
            var slot = new Slot();
            Fill(slot, w);
            if (_slots.Count < MaxSlots) { _slots.Add(slot); _current = _slots.Count - 1; }
            else _slots[_current] = slot;
            _reloadLeft = 0f;
        }

        void Fill(Slot slot, ZombieWeapon w)
        {
            slot.spec = w;
            slot.mag = w.magazine;
            slot.reserve = w.reserve;
            slot.move = BuildMove(w);
            slot.tint = new Color32(w.r, w.g, w.b, 255);
        }

        /// <summary>Wall ammo: refills the reserve of a gun you hold. False if you don't hold it.</summary>
        public bool RefillAmmo(string weaponId)
        {
            var s = Find(weaponId);
            if (s == null) return false;
            s.reserve = s.spec.reserve;
            return true;
        }

        /// <summary>Ammo Pinata: every gun full.</summary>
        public void RefillAll()
        {
            foreach (var s in _slots) { s.mag = s.spec.magazine; s.reserve = s.spec.reserve; }
            _reloadLeft = 0f;
        }

        /// <summary>The forge: upgrades the gun in hand (false if there is none or it's already cursed).</summary>
        public bool UpgradeCurrent()
        {
            var s = Current;
            if (s == null || s.spec.upgraded) return false;
            Fill(s, ZombieArmory.Upgrade(s.spec));
            return true;
        }

        public void Swap()
        {
            if (_slots.Count < 2) return;
            _current = (_current + 1) % _slots.Count;
            _reloadLeft = 0f;
            _cooldown = 0.15f;
            Sfx.Play(Sfx.Reload, 0.5f, 0.1f);
        }

        // ---- firing -----------------------------------------------------------------------------------------------

        void Update()
        {
            _usePressed = (_use != null && _use.WasPressedThisFrame()) || (_touch != null && _touch.ConsumePressed(ControlIds.Use));
            if (Services.State != null && Services.State.State != GameState.Playing) return;
            if (_health != null && _health.IsDead) return;
            float dt = Time.deltaTime;
            _cooldown -= dt;
            var s = Current;
            if (s == null) return;

            if (_reloadLeft > 0f)
            {
                _reloadLeft -= dt;
                if (_reloadLeft <= 0f)
                {
                    int take = Mathf.Min(s.spec.magazine - s.mag, s.reserve);
                    s.mag += take;
                    s.reserve -= take;
                    Sfx.Play(Sfx.Reload, 0.45f, 0.05f);
                }
                return;
            }
            if (s.mag <= 0)
            {
                if (s.reserve > 0) StartReload();
                return;
            }

            bool pressed = (_fire != null && _fire.WasPressedThisFrame()) || (_touch != null && _touch.ConsumePressed(ControlIds.Fire));
            bool held = (_fire != null && _fire.IsPressed()) || (_touch != null && _touch.IsHeld(ControlIds.Fire));
            if (_cooldown > 0f || !(pressed || (s.spec.automatic && held))) return;
            Shoot(s);
        }

        void StartReload()
        {
            var s = Current;
            if (s == null || s.mag >= s.spec.magazine || s.reserve <= 0) return;
            _reloadLeft = s.spec.reloadSeconds / Mathf.Max(0.1f, FireRateScale);
        }

        void Shoot(Slot s)
        {
            s.mag--;
            _cooldown = 1f / Mathf.Max(0.1f, s.spec.shotsPerSecond * FireRateScale);
            int facing = _motor.Facing >= 0 ? 1 : -1;
            // Stick up aims high (bats, a zombie on a ledge); down aims low.
            float aim = _motor.Intent.Move.y > 0.5f ? 38f : _motor.Intent.Move.y < -0.5f && !_motor.IsGrounded ? -38f : 0f;
            Vector2 origin = (Vector2)transform.position + new Vector2(0.15f * facing, 0.25f);
            int pellets = Mathf.Max(1, s.spec.pellets);
            for (int i = 0; i < pellets; i++)
            {
                float spread = s.spec.spreadDegrees;
                float a = pellets > 1 ? Mathf.Lerp(-spread, spread, i / (float)(pellets - 1)) : Random.Range(-spread, spread);
                Projectile.SpawnAimed(s.move, origin, facing, TeamIds.Player, _health, material, aim + a, s.tint);
            }
            PuffEffect.Play(origin + new Vector2(0.6f * facing, 0f), s.tint, material, 0.1f, 2f, 0.12f);
            Sfx.Play(Sfx.Shoot, s.spec.pellets > 1 || s.spec.damage >= 6 ? 0.75f : 0.5f, 0.08f);
            if (s.spec.damage >= 6 && CameraFollow2D.Main != null) CameraFollow2D.Main.Shake(0.08f, 0.1f);
            if (s.mag <= 0 && s.reserve > 0) StartReload();
        }

        /// <summary>A runtime projectile move for a gun (hits like the game's other shots, team Player).</summary>
        static MoveDefinition BuildMove(ZombieWeapon w)
        {
            var m = ScriptableObject.CreateInstance<MoveDefinition>();
            m.name = "Gun_" + w.id;
            m.id = "gun_" + w.id;
            m.displayName = w.name;
            m.spawnsProjectile = true;
            m.projectileSpeed = w.speed;
            m.projectileLifetime = w.lifetime;
            m.projectileRadius = w.radius;
            m.projectileSpawnOffset = new Vector2(0.55f, 0f);
            m.projectileColor = new Color32(w.r, w.g, w.b, 255);
            m.projectileSprite = Shot(w.shot);
            m.hit = new HitData
            {
                damage = w.damage, pipDamage = 1, baseKnockback = w.damage >= 6 ? 7f : 3.5f, exposedMultiplier = 1f,
                angleDegrees = 30f, baseHitstunFrames = w.damage >= 6 ? 14 : 6, hitstopFrames = w.damage >= 6 ? 3 : 1,
                isHeavy = w.damage >= 6
            };
            return m;
        }

        /// <summary>Gun shot art (Resources/PKR/Shots/shot_{kind}); null = a coloured dot.</summary>
        static Sprite Shot(string kind)
        {
            if (string.IsNullOrEmpty(kind)) return null;
            if (ShotArt.TryGetValue(kind, out var s)) return s;
            s = Resources.Load<Sprite>("PKR/Shots/shot_" + kind);
            ShotArt[kind] = s;
            return s;
        }
    }
}
