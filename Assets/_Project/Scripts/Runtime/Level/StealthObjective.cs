using System.Collections.Generic;
using PKR.Core;
using UnityEngine;

namespace PKR
{
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
