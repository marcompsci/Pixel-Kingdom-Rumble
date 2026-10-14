using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Shadow guards: the moment a guard spots the hero (GuardVision alerted), it copies them. It takes the hero's
    /// weapon and whole moveset (attacks, combos, specials, projectiles) and turns into a shadow double: the hero's
    /// own animation frames in a dark violet tint. Then it fights back. EnemyAI asks Fight() for the guard's
    /// inputs while alerted. Each copied hit does at most 1 damage (AttackRunner.DamageCap) so a copied finisher
    /// can't erase a whole health bar. The copy is undone when the guard respawns from the pool.
    /// </summary>
    [RequireComponent(typeof(AttackRunner), typeof(PlatformerMotor2D))]
    public class GuardMirror : MonoBehaviour
    {
        public static readonly Color ShadowTint = new Color(0.5f, 0.38f, 0.8f, 1f);

        /// <summary>True once this guard has copied the hero.</summary>
        public bool IsMirroring { get; private set; }
        /// <summary>The hero whose moves were copied (null until spotted).</summary>
        public CharacterDefinition Copied { get; private set; }
        public Moveset CopiedMoves => _runner != null ? _runner.Moveset : null;

        AttackRunner _runner;
        PlatformerMotor2D _motor;
        FighterVisual _visual;
        HitFlash _flash;
        SpriteRenderer _body;
        FighterFrames _ownFrames;
        Sprite _ownSprite;
        float _attackCd, _specialCd, _jumpCd;
        float _attackEvery = 0.9f;
        static int _hintedScene = -1;

        void Awake()
        {
            _runner = GetComponent<AttackRunner>();
            _motor = GetComponent<PlatformerMotor2D>();
            _visual = GetComponent<FighterVisual>();
            _flash = GetComponent<HitFlash>();
            _body = transform.childCount > 0 ? transform.GetChild(0).GetComponent<SpriteRenderer>() : null;
            _runner.Team = TeamIds.Enemy;
            _runner.DamageCap = 1;
            _runner.debugDrawHitboxes = false;
        }

        /// <summary>EnemyAI.Init: back to a plain guard with its own look (pooled enemies are reused).</summary>
        public void ResetCopy(EnemyDefinition def)
        {
            _ownFrames = def != null ? def.frames : null;
            _ownSprite = def != null ? def.sprite : null;
            // Elites and captains swing more often than sentries.
            _attackEvery = def != null && def.noticeRate >= 2f ? 0.65f : (def != null && def.maxHealth >= 6 ? 0.75f : 0.95f);
            if (IsMirroring)
            {
                if (_visual != null) _visual.SetFrames(_ownFrames);
                if (_body != null && _ownSprite != null && (_ownFrames == null || !_ownFrames.HasAny)) _body.sprite = _ownSprite;
                SetTint(Color.white);
            }
            IsMirroring = false;
            Copied = null;
            _runner.Moveset = null;
            _attackCd = _specialCd = _jumpCd = 0f;
        }

        /// <summary>Copies the current hero's weapon, moves and look. False if there is no hero to copy.</summary>
        public bool TryCopy()
        {
            if (IsMirroring) return true;
            var player = PlayerMarker.Current;
            if (player == null) return false;
            var heroRunner = player.GetComponent<AttackRunner>();
            var heroMotor = player.GetComponent<PlatformerMotor2D>();
            var hero = heroMotor != null ? heroMotor.Definition : null;
            var moves = heroRunner != null && heroRunner.Moveset != null ? heroRunner.Moveset : (hero != null ? hero.moveset : null);
            if (moves == null) return false;

            IsMirroring = true;
            Copied = hero;
            _runner.Moveset = moves;
            if (_visual != null && hero != null && hero.frames != null && hero.frames.HasAny) _visual.SetFrames(hero.frames);
            SetTint(ShadowTint);
            _attackCd = 0.5f; // a beat to read the transformation before the first swing
            _specialCd = 1.2f;

            var mat = _body != null ? _body.sharedMaterial : null;
            Vector2 at = (Vector2)transform.position + Vector2.up * 0.3f;
            PuffEffect.Play(at, new Color(0.55f, 0.35f, 0.9f), mat, 0.3f, 5f, 0.45f);
            PuffEffect.Play(at, new Color(0.15f, 0.1f, 0.25f), mat, 0.22f, 3f, 0.5f);
            Sfx.Play(Sfx.Special, 0.7f, 0.05f);
            int scene = gameObject.scene.handle;
            if (_hintedScene != scene)
            {
                _hintedScene = scene;
                string weapon = hero != null && hero.frames != null && !string.IsNullOrEmpty(hero.frames.weaponName)
                    ? hero.frames.weaponName.ToUpperInvariant() : "YOUR WEAPON";
                EventBus<GameHint>.Raise(new GameHint { id = "guard_mirror",
                    text = $"SPOTTED GUARDS COPY {weapon} AND YOUR MOVES. FIGHT BACK OR GET OUT OF SIGHT!" });
            }
            return true;
        }

        void SetTint(Color c)
        {
            if (_flash != null) _flash.SetBaseColor(c);
            else if (_body != null) _body.color = c;
        }

        /// <summary>
        /// The shadow double's combat inputs for one physics step (EnemyAI calls this while the guard is alerted).
        /// Sets the intent's move (already scaled by chaseSpeed) and presses Attack / Special / Jump.
        /// </summary>
        public void Fight(FighterIntent intent, Vector2 toHero, float chaseSpeed, bool safeToStep)
        {
            float dt = Time.fixedDeltaTime;
            _attackCd -= dt; _specialCd -= dt; _jumpCd -= dt;
            if (!TryCopy())
            {
                intent.Move = new Vector2(safeToStep && Mathf.Abs(toHero.x) > 0.3f ? Mathf.Sign(toHero.x) * chaseSpeed : 0f, 0f);
                return;
            }

            float ax = Mathf.Abs(toHero.x);
            int dir = toHero.x >= 0f ? 1 : -1;
            float now = Time.time;
            bool busy = _runner.IsAttacking || _motor.IsControlLocked;
            var moves = _runner.Moveset;

            if (!busy)
            {
                var special = moves != null ? moves.ForSpecial(_motor.IsGrounded, new Vector2(dir, 0f)) : null;
                // Close: swing (aim the stick at the hero so directional moves face the right way, up for a hero above).
                // Now and then a close-range special instead, just like a player mixing it up.
                if (ax < 1.35f && Mathf.Abs(toHero.y) < 1.2f && _attackCd <= 0f)
                {
                    if (special != null && !special.spawnsProjectile && _specialCd <= 0f && Random.value < 0.25f)
                    {
                        intent.Move = new Vector2(dir * 0.6f, 0f);
                        intent.Special.Press(now);
                        _specialCd = 2.6f;
                        _attackCd = _attackEvery * 1.3f;
                        return;
                    }
                    intent.Move = new Vector2(dir * 0.2f, toHero.y > 0.8f ? 0.8f : 0f);
                    intent.Attack.Press(now);
                    _attackCd = _attackEvery * Random.Range(0.85f, 1.25f);
                    return;
                }
                // Mid range: fire the hero's projectile special if the moveset has one.
                if (special != null && special.spawnsProjectile && ax > 2.2f && ax < 7f && Mathf.Abs(toHero.y) < 1.2f && _specialCd <= 0f)
                {
                    intent.Move = new Vector2(dir * 0.6f, 0f);
                    intent.Special.Press(now);
                    _specialCd = 2.6f * Random.Range(0.9f, 1.3f);
                    return;
                }
                // Hero jumped up onto something close by: hop after them.
                if (toHero.y > 1.3f && ax < 2.5f && _motor.IsGrounded && _jumpCd <= 0f)
                {
                    intent.Jump.Press(now);
                    intent.Jump.SetHeld(true);
                    _jumpCd = 1.6f;
                }
                else if (_jumpCd < 1.2f) intent.Jump.SetHeld(false);
            }

            // Footsies: close in to striking distance and hold there instead of walking into the hero.
            intent.Move = new Vector2(ax < 0.9f || !safeToStep ? 0f : dir * Mathf.Max(chaseSpeed, 0.55f), 0f);
        }
    }
}
