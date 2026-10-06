using System.Collections;
using System.Collections.Generic;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>Raised when the Warden's fight state changes (for the boss HUD).</summary>
    public struct BossStateChanged { public WardenAction action; public WardenPhase phase; public bool vulnerable; }

    /// <summary>
    /// The Clockwork Warden boss. Core WardenBrain decides the attack pattern; this component moves the parts:
    ///  - Piston Slam: the piston tracks the hero (shadow marker on the floor), locks, slams, and stays stuck.
    ///    While stuck its glowing core is the ONLY place the Warden can be hurt.
    ///  - Gear Sweep: a big gear rolls across the floor from one side; jump over it.
    ///  - Cog Volley (phase 2+): cogs drop from the ceiling onto marked spots.
    /// Health lives on the Damageable on this object; the hurtbox on the piston head is enabled only while staggered.
    /// The hero dying resets the fight; defeat completes the level through LevelFlowController.
    /// </summary>
    [RequireComponent(typeof(Damageable))]
    public class ClockworkWarden : MonoBehaviour
    {
        public const int MaxCogs = 5;

        [Header("Arena (world units)")]
        [SerializeField] float arenaLeft = -10f;
        [SerializeField] float arenaRight = 10f;
        [SerializeField] float floorY = 0f;
        [SerializeField] float ceilingY = 9f;

        [Header("Fight")]
        [SerializeField] int maxHealth = 24;
        [Tooltip("Seconds before the first attack (the name banner shows).")]
        [SerializeField] float introDelay = 1.5f;
        [SerializeField] float slamShockwaveRadius = 2.2f;
        [SerializeField] float cogSpacing = 3f;
        [SerializeField] float gearDiameter = 2.2f;
        [SerializeField] float cogDiameter = 0.9f;

        [Header("Placeholder art")]
        [SerializeField] Material spriteMaterial;
        [SerializeField] Sprite bodySprite;
        [SerializeField] Sprite gearSprite;
        [SerializeField] Sprite pistonSprite;

        public WardenBrain Brain { get; private set; }
        public Damageable Health { get; private set; }
        public string DisplayName => "CLOCKWORK WARDEN";

        static readonly Color CoreIdle = new Color32(120, 90, 60, 255);
        static readonly Color CoreExposed = new Color32(255, 220, 90, 255);
        static readonly Color Telegraph = new Color32(255, 80, 60, 200);

        Transform _piston, _gear;
        SpriteRenderer _coreSprite, _marker;
        Collider2D _coreCollider;
        ContactDamage _pistonHurt, _gearHurt;
        readonly Transform[] _cogs = new Transform[MaxCogs];
        readonly SpriteRenderer[] _cogMarkers = new SpriteRenderer[MaxCogs];
        readonly float[] _cogX = new float[MaxCogs];
        int _cogCount;
        float _introLeft;
        bool _completing;
        readonly List<Damageable> _targets = new List<Damageable>();

        float PistonUpY => ceilingY - 2.5f;
        float PistonDownY => floorY + 1.2f; // piston center when its head rests on the floor

        void Awake()
        {
            Brain = new WardenBrain(arenaLeft + 1.5f, arenaRight - 1.5f);
            Health = GetComponent<Damageable>();
            Health.Configure(DamageModel.StoryHealth, TeamIds.Enemy, maxHealth, 3f, 0f, 0.12f);
            BuildParts();
            ResetFight();
        }

        void OnEnable()
        {
            Health.Hit += OnHit;
            Health.Died += OnDied;
            EventBus<PlayerRespawned>.Subscribe(OnPlayerRespawned);
        }

        void OnDisable()
        {
            Health.Hit -= OnHit;
            Health.Died -= OnDied;
            EventBus<PlayerRespawned>.Unsubscribe(OnPlayerRespawned);
        }

        void OnPlayerRespawned(PlayerRespawned e)
        {
            if (e.afterDeath && !Brain.IsDefeated) ResetFight();
        }

        void OnHit(HitResult r)
        {
            Brain.SetHealthFraction(Health.MaxHealth > 0 ? (float)Health.Health / Health.MaxHealth : 0f);
            Raise();
        }

        void OnDied()
        {
            Brain.Defeat();
            SetVulnerable(false);
            _pistonHurt.enabled = _gearHurt.enabled = false;
            HideCogs();
            _gear.gameObject.SetActive(false);
            _marker.enabled = false;
            if (CameraFollow2D.Main != null) CameraFollow2D.Main.Shake(0.5f, 0.6f);
            if (Services.Haptics != null) Services.Haptics.Play(HapticStrength.Heavy);
            Raise();
            if (!_completing) StartCoroutine(FinishFight());
        }

        IEnumerator FinishFight()
        {
            _completing = true;
            for (int i = 0; i < 6; i++)
            {
                Vector2 p = (Vector2)_piston.position + Random.insideUnitCircle * 1.5f;
                PuffEffect.Play(p, CoreExposed, spriteMaterial, 0.35f, 6f, 0.5f);
                yield return new WaitForSeconds(0.2f);
            }
            if (LevelFlowController.Current != null) LevelFlowController.Current.CompleteLevel();
        }

        public void ResetFight()
        {
            StopAllCoroutines();
            _completing = false;
            Brain.Reset();
            Health.ResetState();
            _introLeft = introDelay;
            _piston.position = new Vector2((arenaLeft + arenaRight) * 0.5f, PistonUpY);
            _gear.gameObject.SetActive(false);
            _marker.enabled = false;
            HideCogs();
            _pistonHurt.enabled = _gearHurt.enabled = false;
            SetVulnerable(false);
            Raise();
        }

        void FixedUpdate()
        {
            if (Brain.IsDefeated) return;
            float dt = Time.fixedDeltaTime;
            if (_introLeft > 0f) { _introLeft -= dt; return; }

            var hero = PlayerMarker.Current;
            float heroX = hero != null ? hero.transform.position.x : (arenaLeft + arenaRight) * 0.5f;
            var before = Brain.Current;
            if (Brain.Tick(dt, heroX)) OnActionChanged(before, Brain.Current, heroX);
            Animate(dt);
        }

        void OnActionChanged(WardenAction from, WardenAction to, float heroX)
        {
            switch (to)
            {
                case WardenAction.SweepWindup:
                    _gear.gameObject.SetActive(true);
                    _gear.position = new Vector2(GearStartX(), GearY);
                    break;
                case WardenAction.Sweep:
                    _gearHurt.enabled = true;
                    break;
                case WardenAction.Slam:
                    _pistonHurt.enabled = true;
                    break;
                case WardenAction.Stagger:
                    _pistonHurt.enabled = false;
                    _piston.position = new Vector2(Brain.SlamX, PistonDownY);
                    SlamShockwave();
                    SetVulnerable(true);
                    break;
                case WardenAction.VolleyWindup:
                    var xs = Brain.VolleyPositions(heroX, Brain.Phase == WardenPhase.Three ? 5 : 3, cogSpacing);
                    _cogCount = xs.Length;
                    for (int i = 0; i < MaxCogs; i++)
                    {
                        bool used = i < _cogCount;
                        if (used) _cogX[i] = xs[i];
                        _cogMarkers[i].enabled = used;
                        if (used) _cogMarkers[i].transform.position = new Vector2(_cogX[i], floorY + 0.05f);
                    }
                    break;
                case WardenAction.Volley:
                    for (int i = 0; i < _cogCount; i++)
                    {
                        _cogMarkers[i].enabled = true; // steady warning while they fall
                        _cogs[i].gameObject.SetActive(true);
                        _cogs[i].position = new Vector2(_cogX[i], ceilingY);
                    }
                    break;
                case WardenAction.Idle:
                    if (from == WardenAction.Stagger) SetVulnerable(false);
                    if (from == WardenAction.Sweep) { _gearHurt.enabled = false; _gear.gameObject.SetActive(false); }
                    if (from == WardenAction.Volley) HideCogs();
                    break;
            }
            Raise();
        }

        void Animate(float dt)
        {
            float p = Brain.Progress;
            switch (Brain.Current)
            {
                case WardenAction.Idle:
                    RetractPiston(dt);
                    _marker.enabled = false;
                    break;
                case WardenAction.SlamWindup:
                    _piston.position = new Vector2(Mathf.MoveTowards(_piston.position.x, Brain.SlamX, 14f * dt), PistonUpY);
                    _marker.enabled = Mathf.FloorToInt(Time.time * (p > 0.6f ? 16f : 8f)) % 2 == 0;
                    _marker.transform.position = new Vector2(Brain.SlamX, floorY + 0.05f);
                    break;
                case WardenAction.Slam:
                    _marker.enabled = true;
                    _piston.position = new Vector2(Brain.SlamX, Mathf.Lerp(PistonUpY, PistonDownY, p * p));
                    break;
                case WardenAction.SweepWindup:
                    // Peek in from the edge, shaking, so the hero sees which side it comes from.
                    float shake = Mathf.Sin(Time.time * 50f) * 0.08f;
                    _gear.position = new Vector2(GearStartX() + shake, GearY);
                    RetractPiston(dt);
                    break;
                case WardenAction.Sweep:
                    float x = Mathf.Lerp(GearStartX(), GearEndX(), p);
                    _gear.position = new Vector2(x, GearY);
                    RetractPiston(dt);
                    _gear.Rotate(0f, 0f, -Brain.SweepDir * 540f * dt);
                    break;
                case WardenAction.VolleyWindup:
                    bool blink = Mathf.FloorToInt(Time.time * 10f) % 2 == 0;
                    for (int i = 0; i < _cogCount; i++) _cogMarkers[i].enabled = blink;
                    RetractPiston(dt);
                    break;
                case WardenAction.Volley:
                    float y = Mathf.Lerp(ceilingY, floorY + 0.4f, Mathf.Clamp01(p * 1.4f));
                    for (int i = 0; i < _cogCount; i++)
                    {
                        _cogs[i].position = new Vector2(_cogX[i], y);
                        _cogs[i].Rotate(0f, 0f, 400f * dt);
                    }
                    if (p * 1.4f >= 1f) HideCogs(); // landed
                    RetractPiston(dt);
                    break;
            }
        }

        float GearY => floorY + gearDiameter * 0.5f;
        // Rolls from wall to wall and stops inside the arena (it hides when the sweep ends).
        float GearStartX() => Brain.SweepDir > 0 ? arenaLeft + gearDiameter * 0.5f : arenaRight - gearDiameter * 0.5f;
        float GearEndX() => Brain.SweepDir > 0 ? arenaRight - gearDiameter * 0.5f : arenaLeft + gearDiameter * 0.5f;

        void RetractPiston(float dt)
        {
            var target = new Vector2(_piston.position.x, PistonUpY);
            _piston.position = Vector2.MoveTowards(_piston.position, target, 12f * dt);
        }

        void SlamShockwave()
        {
            if (CameraFollow2D.Main != null) CameraFollow2D.Main.Shake(0.35f, 0.25f);
            if (Services.Haptics != null) Services.Haptics.Play(HapticStrength.Heavy);
            Vector2 at = new Vector2(Brain.SlamX, floorY + 0.3f);
            PuffEffect.Play(at, Telegraph, spriteMaterial, 0.3f, 7f, 0.4f);
            var hit = new HitData { damage = 1, pipDamage = 1, baseKnockback = 8f, exposedMultiplier = 1f,
                                    angleDegrees = 60f, baseHitstunFrames = 16, hitstopFrames = 5 };
            CombatQuery.OverlapCircle(at, slamShockwaveRadius, _targets);
            foreach (var t in _targets)
            {
                if (t == Health) continue;
                int dir = t.transform.position.x >= at.x ? 1 : -1;
                t.TakeHit(hit, dir, TeamIds.Enemy, out _);
            }
        }

        void SetVulnerable(bool on)
        {
            _coreCollider.enabled = on;
            _coreSprite.color = on ? CoreExposed : CoreIdle;
        }

        void HideCogs()
        {
            for (int i = 0; i < MaxCogs; i++)
            {
                _cogs[i].gameObject.SetActive(false);
                _cogMarkers[i].enabled = false;
            }
        }

        void Raise() => EventBus<BossStateChanged>.Raise(new BossStateChanged
        {
            action = Brain.Current, phase = Brain.Phase, vulnerable = Brain.IsVulnerable
        });

        // ---- Construction (placeholder parts built in code) --------------------------------------

        void BuildParts()
        {
            // Decorative body looming at the back of the arena.
            var body = Sprite("Body", transform, bodySprite != null ? bodySprite : gearSprite, new Color32(150, 120, 80, 255), -20);
            body.transform.position = new Vector2((arenaLeft + arenaRight) * 0.5f, ceilingY - 1f);
            body.transform.localScale = Vector3.one * 6f;

            // Piston: shaft + head (core) + slam hazard.
            _piston = new GameObject("Piston").transform;
            _piston.SetParent(transform, false);
            var shaft = Sprite("Shaft", _piston, pistonSprite, new Color32(170, 150, 110, 255), 5);
            shaft.drawMode = SpriteDrawMode.Tiled;
            shaft.size = new Vector2(1.4f, 2.4f);
            _coreSprite = Sprite("Core", _piston, UISprites.Circle, CoreIdle, 6);
            _coreSprite.transform.localPosition = new Vector3(0f, -0.9f, 0f);
            _coreSprite.transform.localScale = Vector3.one * 0.9f;
            var coreCol = _coreSprite.gameObject.AddComponent<CircleCollider2D>();
            coreCol.radius = 0.6f;
            _coreCollider = coreCol;
            _coreSprite.gameObject.AddComponent<Hurtbox>();
            gameObject.AddComponent<HitFlash>(); // flashes the body (first child sprite) when the core is hit
            _pistonHurt = _piston.gameObject.AddComponent<ContactDamage>();
            _pistonHurt.size = new Vector2(1.5f, 2.4f);
            _pistonHurt.hit = new HitData { damage = 2, pipDamage = 2, baseKnockback = 9f, exposedMultiplier = 1f,
                                            angleDegrees = 45f, baseHitstunFrames = 18, hitstopFrames = 5 };
            _pistonHurt.enabled = false;

            // Sweep gear: an opaque disc with gear teeth on top, sized to match its hitbox.
            _gear = Wheel("Gear", gearDiameter, new Color32(200, 160, 70, 255));
            _gearHurt = _gear.gameObject.AddComponent<ContactDamage>();
            _gearHurt.size = Vector2.one * (gearDiameter * 0.9f);
            _gearHurt.hit = new HitData { damage = 1, pipDamage = 1, baseKnockback = 8f, exposedMultiplier = 1f,
                                          angleDegrees = 65f, baseHitstunFrames = 16, hitstopFrames = 4 };
            _gearHurt.enabled = false;

            // Slam marker + cogs and their markers.
            _marker = Sprite("SlamMarker", transform, UISprites.Ring, Telegraph, 3);
            // UISprites.Ring is 1.28 units across: scale it to the shockwave's full width.
            _marker.transform.localScale = new Vector3(slamShockwaveRadius * 2f / 1.28f, 0.5f, 1f);
            for (int i = 0; i < MaxCogs; i++)
            {
                var cog = Wheel($"Cog{i}", cogDiameter, new Color32(180, 180, 190, 255));
                var hurt = cog.gameObject.AddComponent<ContactDamage>();
                hurt.size = Vector2.one * (cogDiameter * 0.9f);
                hurt.hit = new HitData { damage = 1, pipDamage = 1, baseKnockback = 6f, exposedMultiplier = 1f,
                                         angleDegrees = 50f, baseHitstunFrames = 14, hitstopFrames = 3 };
                _cogs[i] = cog;
                _cogMarkers[i] = Sprite($"CogMarker{i}", transform, UISprites.Ring, Telegraph, 3);
                _cogMarkers[i].transform.localScale = new Vector3(0.9f, 0.25f, 1f);
            }
            HideCogs();
        }

        /// <summary>A gear-shaped hazard: opaque disc (UISprites.Circle, 1.28 units) + gear teeth (DecorGear, 2 units), diameter in world units.</summary>
        Transform Wheel(string name, float diameter, Color color)
        {
            var root = new GameObject(name).transform;
            root.SetParent(transform, false);
            var disc = Sprite("Disc", root, UISprites.Circle, color, 4);
            disc.transform.localScale = Vector3.one * (diameter * 0.8f / 1.28f);
            var teeth = Sprite("Teeth", root, gearSprite, Color.white, 5);
            teeth.transform.localScale = Vector3.one * (diameter / 2f);
            return root;
        }

        SpriteRenderer Sprite(string name, Transform parent, Sprite sprite, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            if (spriteMaterial != null) sr.sharedMaterial = spriteMaterial;
            return sr;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.6f);
            Gizmos.DrawWireCube(new Vector3((arenaLeft + arenaRight) * 0.5f, (floorY + ceilingY) * 0.5f),
                                new Vector3(arenaRight - arenaLeft, ceilingY - floorY));
        }
    }
}
