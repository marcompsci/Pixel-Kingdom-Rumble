using System.Collections.Generic;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>A short stealth message for the HUD ("SPOTTED!", "SILENT TAKEDOWN").</summary>
    public struct StealthNotice { public string text; public bool alarm; }

    /// <summary>
    /// One per stealth level (the builder adds it when a map has Patrol Guards). Counts how often the hero was
    /// spotted and how many silent takedowns they pulled off, tracks the objective, and grades the run
    /// (StealthRules.Rank) for the results screen.
    /// </summary>
    public class StealthTracker : MonoBehaviour
    {
        public static StealthTracker Current { get; private set; }

        [Tooltip("Shown when the hero reaches the gate without the objective.")]
        public string objectiveName = "the ledger";

        public int TimesSpotted { get; private set; }
        public int Takedowns { get; private set; }
        public bool HasObjective { get; private set; }
        public bool ObjectiveTaken { get; private set; }
        public string Rank => StealthRules.Rank(TimesSpotted, Takedowns);
        /// <summary>True while the hero is tucked inside a hiding spot (guards can't see them).</summary>
        public bool HeroHidden { get; private set; }

        float _gateNagUntil;

        void Awake()
        {
            Current = this;
            HasObjective = FindObjectsByType<StealthObjective>().Length > 0;
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        void FixedUpdate()
        {
            var player = PlayerMarker.Current;
            HeroHidden = player != null && HideSpot.Contains(player.transform.position) && !IsAttacking(player);
        }

        static bool IsAttacking(PlayerMarker p) => p.TryGetComponent(out AttackRunner a) && a.IsAttacking;

        public void ReportSpotted()
        {
            TimesSpotted++;
            EventBus<StealthNotice>.Raise(new StealthNotice { text = "SPOTTED!", alarm = true });
            if (Services.Haptics != null) Services.Haptics.Play(HapticStrength.Medium);
        }

        public void ReportTakedown()
        {
            Takedowns++;
            EventBus<StealthNotice>.Raise(new StealthNotice { text = "SILENT TAKEDOWN" });
        }

        public void TakeObjective(string displayName)
        {
            if (ObjectiveTaken) return;
            ObjectiveTaken = true;
            EventBus<StealthNotice>.Raise(new StealthNotice { text = $"{displayName.ToUpperInvariant()} STOLEN!\nESCAPE TO THE GATE" });
            if (Services.Haptics != null) Services.Haptics.Play(HapticStrength.Heavy);
        }

        /// <summary>The gate only opens once the objective is taken. Returns false (and nags) if it isn't.</summary>
        public bool CanFinish()
        {
            if (!HasObjective || ObjectiveTaken) return true;
            if (Time.time >= _gateNagUntil)
            {
                _gateNagUntil = Time.time + 2.5f;
                EventBus<StealthNotice>.Raise(new StealthNotice { text = $"FIND {objectiveName.ToUpperInvariant()} FIRST!", alarm = true });
            }
            return false;
        }
    }

    /// <summary>A hay bale / awning the hero can hide in. Guards don't see a hidden hero (unless it attacks).</summary>
    public class HideSpot : MonoBehaviour
    {
        public Vector2 size = new Vector2(2f, 1.6f);
        static readonly List<HideSpot> Active = new List<HideSpot>();

        void OnEnable() => Active.Add(this);
        void OnDisable() => Active.Remove(this);

        /// <summary>Is this point (the hero's center) inside any hiding spot?</summary>
        public static bool Contains(Vector2 point)
        {
            foreach (var h in Active)
            {
                Vector2 c = h.transform.position;
                if (Mathf.Abs(point.x - c.x) <= h.size.x * 0.5f && Mathf.Abs(point.y - c.y) <= h.size.y * 0.5f) return true;
            }
            return false;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.9f, 0.8f, 0.3f, 0.6f);
            Gizmos.DrawWireCube(transform.position, size);
        }
    }

    /// <summary>The thing to steal on a stealth level. Touch it to take it; the goal gate opens afterwards.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class StealthObjective : MonoBehaviour
    {
        public string displayName = "Gearwright's Ledger";
        bool _taken;

        void Awake() => GetComponent<Collider2D>().isTrigger = true;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (_taken || !TriggerUtil.IsPlayer(other, out _)) return;
            _taken = true;
            if (StealthTracker.Current != null) StealthTracker.Current.TakeObjective(displayName);
            var sr = GetComponentInChildren<SpriteRenderer>();
            PuffEffect.Play(transform.position, new Color32(255, 220, 90, 255), sr != null ? sr.sharedMaterial : null, 0.2f, 4f, 0.5f);
            gameObject.SetActive(false);
        }
    }
}
