using System;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    public enum DamageModel
    {
        /// <summary>Story Quest: HP with armor; dies at 0 HP.</summary>
        StoryHealth,
        /// <summary>Arena Clash: Guard Pips; never "dies" from hits, only from blast zones.</summary>
        ArenaPips
    }

    /// <summary>
    /// Optional component next to a Damageable that can stop hits outright (a shield). Return true to block:
    /// the hit then deals nothing and the attacker gets a short "clank".
    /// </summary>
    public interface IHitFilter
    {
        /// <param name="sourceX">World x the hit came from (attacker, projectile, hit center); NaN if unknown.</param>
        bool TryBlock(in HitData hit, int attackerFacing, float sourceX);
    }

    public static class TeamIds
    {
        public const int Player = 0;
        public const int Enemy = 1;
        public const int Neutral = -1; // hit by everyone (training dummies)
    }

    /// <summary>
    /// Anything that can be hit: heroes, enemies, dummies, breakables.
    /// Resolves hits through Core CombatMath, applies knockback via the motor (or Rigidbody2D),
    /// grants post-hit invulnerability and raises events for feedback (flash, pips, sounds).
    /// </summary>
    public class Damageable : MonoBehaviour
    {
        [SerializeField] DamageModel model = DamageModel.StoryHealth;
        [Tooltip("Attacks from the same team are ignored. -1 = neutral (anyone can hit it).")]
        [SerializeField] int team = TeamIds.Enemy;
        [Tooltip("0 = use the CharacterDefinition's maxHealth (or 3 if none).")]
        [SerializeField] int maxHealthOverride;
        [Tooltip("Used when there is no CharacterDefinition.")]
        [SerializeField] float fallbackWeight = 1f;
        [SerializeField, Range(0f, 0.9f)] float fallbackArmor;
        [Tooltip("Blinking invulnerability after taking a hit (players ~1 s, enemies 0).")]
        [SerializeField] float postHitInvulnerability;

        public DamageModel Model { get => model; set { model = value; ResetState(); } }
        public int Team { get => team; set => team = value; }
        public int MaxHealth { get; private set; }
        public int Health { get; private set; }
        public GuardPipState Pips { get; private set; }
        public bool IsDead { get; private set; }
        public bool IsExposed => model == DamageModel.ArenaPips && Pips != null && Pips.IsExposed;
        /// <summary>Team of whoever hit this last (for arena KO credit). -2 = nobody yet.</summary>
        public int LastAttackerTeam { get; private set; } = -2;
        public float LastHitTime { get; private set; } = float.NegativeInfinity;

        public event Action<HitResult> Hit;
        /// <summary>A shield (IHitFilter) stopped a hit.</summary>
        public event Action<HitResult> Blocked;
        public event Action BecameExposed;
        public event Action Died;
        public event Action Restored;

        PlatformerMotor2D _motor;
        AttackRunner _attacks;
        HeroAbilities _abilities;
        IHitFilter _filter;
        Rigidbody2D _body;
        Invulnerability _invuln;

        float Weight => _motor != null && _motor.Definition != null ? _motor.Definition.weight : fallbackWeight;
        /// <summary>Super armor from the current attack or ability (e.g. Brick's Bulwark Charge).</summary>
        public bool HasSuperArmor
        {
            get
            {
                // Looked up on demand so components added after Awake (tests, runtime builds) are seen.
                if (_attacks == null) _attacks = GetComponent<AttackRunner>();
                if (_abilities == null) _abilities = GetComponent<HeroAbilities>();
                return (_attacks != null && _attacks.HasSuperArmor) || (_abilities != null && _abilities.HasSuperArmor);
            }
        }

        float Armor => _motor != null && _motor.Definition != null ? _motor.Definition.armorPercent : fallbackArmor;

        void Awake()
        {
            _motor = GetComponent<PlatformerMotor2D>();
            _body = GetComponent<Rigidbody2D>();
            _invuln = GetComponent<Invulnerability>();
            if (_invuln == null) _invuln = gameObject.AddComponent<Invulnerability>();
            ResetState();
        }

        void Update()
        {
            if (model == DamageModel.ArenaPips && Pips != null) Pips.Tick(Time.deltaTime);
        }

        public void ResetState()
        {
            var def = _motor != null ? _motor.Definition : null;
            MaxHealth = maxHealthOverride > 0 ? maxHealthOverride : (def != null ? def.maxHealth : 3);
            Health = MaxHealth;
            Pips = new GuardPipState(def != null ? Mathf.Max(1, def.guardPips) : 3);
            IsDead = false;
            LastAttackerTeam = -2;
            Restored?.Invoke();
        }

        public bool CanBeHitBy(int attackerTeam)
        {
            if (IsDead || !isActiveAndEnabled) return false;
            if (_invuln != null && _invuln.IsActive) return false;
            return team == TeamIds.Neutral || attackerTeam != team;
        }

        /// <summary>
        /// Apply a hit. attackerFacing (+1/-1) decides knockback direction. Returns false if ignored
        /// (same team, invulnerable, already dead).
        /// </summary>
        public bool TakeHit(in HitData hit, int attackerFacing, int attackerTeam, out HitResult result, float sourceX = float.NaN)
        {
            result = default;
            if (!CanBeHitBy(attackerTeam)) return false;

            // Shields: looked up on demand so a ShieldGuard added at runtime is seen.
            if (_filter == null) _filter = GetComponent<IHitFilter>();
            if (_filter != null && _filter.TryBlock(hit, attackerFacing, sourceX))
            {
                result = CombatMath.BlockedResult(hit);
                Blocked?.Invoke(result);
                return true;
            }

            bool wasExposed = IsExposed;
            result = model == DamageModel.ArenaPips
                ? CombatMath.ResolveArenaHit(hit, Pips, Weight, attackerFacing)
                : CombatMath.ResolveStoryHit(hit, Armor, Weight, attackerFacing);

            LastAttackerTeam = attackerTeam;
            LastHitTime = Time.time;

            // Super armor: take the damage, keep going (unless this hit is a launch).
            bool armored = HasSuperArmor && CombatMath.ApplySuperArmor(ref result);
            if (!armored)
            {
                var kb = new Vector2(result.knockbackVelocity.x, result.knockbackVelocity.y);
                float stunSeconds = result.hitstunFrames / 60f;
                if (_motor != null) _motor.ApplyKnockback(kb, stunSeconds);
                else if (_body != null && _body.bodyType == RigidbodyType2D.Dynamic) _body.linearVelocity = kb;
            }

            if (model == DamageModel.StoryHealth)
            {
                Health = Mathf.Max(0, Health - result.hpDamage);
            }

            if (postHitInvulnerability > 0f && _invuln != null) _invuln.Grant(postHitInvulnerability, blink: true);

            Hit?.Invoke(result);
            if (model == DamageModel.ArenaPips && !wasExposed && IsExposed) BecameExposed?.Invoke();
            if (model == DamageModel.StoryHealth && Health <= 0) Die();
            return true;
        }

        public void Heal(int amount)
        {
            if (IsDead || amount <= 0) return;
            Health = Mathf.Min(MaxHealth, Health + amount);
            Restored?.Invoke();
        }

        /// <summary>
        /// Lose HP without knockback or invulnerability checks (falling into a pit). Story mode only.
        /// Returns true if this caused death.
        /// </summary>
        public bool TakeDirectDamage(int amount)
        {
            if (IsDead || amount <= 0 || model != DamageModel.StoryHealth) return false;
            Health = Mathf.Max(0, Health - amount);
            if (Health <= 0) { Die(); return true; }
            return false;
        }

        /// <summary>Runtime setup for fighters built in code (enemies from EnemyFactory).</summary>
        public void Configure(DamageModel newModel, int newTeam, int maxHealth, float weight, float armor, float postHitInvuln)
        {
            model = newModel;
            team = newTeam;
            maxHealthOverride = maxHealth;
            fallbackWeight = weight;
            fallbackArmor = armor;
            postHitInvulnerability = postHitInvuln;
            ResetState();
        }

        /// <summary>Instant defeat (pits, crushers). Ignores invulnerability.</summary>
        public void Kill()
        {
            if (IsDead) return;
            Health = 0;
            Die();
        }

        void Die()
        {
            if (IsDead) return;
            IsDead = true;
            Died?.Invoke();
        }
    }
}
