using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>A cipher scroll: picking it up reveals a secret contract on the board.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class ClueScroll : MonoBehaviour
    {
        bool _taken;

        void Awake() => GetComponent<Collider2D>().isTrigger = true;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (_taken || !TriggerUtil.IsPlayer(other, out _)) return;
            _taken = true;
            if (MissionTracker.Current != null) MissionTracker.Current.FindClue();
            var sr = GetComponentInChildren<SpriteRenderer>();
            PuffEffect.Play(transform.position, new Color32(200, 160, 255, 255), sr != null ? sr.sharedMaterial : null, 0.22f, 4f, 0.5f);
            gameObject.SetActive(false);
        }
    }
}
