using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// One per Shadow Contract scene. Counts ancient relics (points) and the cipher scroll; LevelFlowController asks it
    /// for the contract score when the gate is reached. The scroll is saved the moment it is picked up.
    /// </summary>
    public class MissionTracker : MonoBehaviour
    {
        public static MissionTracker Current { get; private set; }

        public string contractId = "";
        [Tooltip("Cipher scroll id hidden in this map (empty if none).")]
        public string clueId = "";

        public int Relics { get; private set; }
        public int TotalRelics { get; private set; }
        public bool ClueFoundThisRun { get; private set; }
        public bool ClueAlreadyKnown { get; private set; }

        void Awake()
        {
            Current = this;
            TotalRelics = FindObjectsByType<RelicPickup>().Length;
            var save = Services.Save;
            ClueAlreadyKnown = save != null && save.Data.HasClue(clueId);
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        public void CollectRelic(string relicName)
        {
            Relics++;
            EventBus<StealthNotice>.Raise(new StealthNotice
            {
                text = $"{relicName.ToUpperInvariant()}  +{MissionScore.RelicPoints}\nRELICS {Relics}/{TotalRelics}"
            });
            if (Services.Haptics != null) Services.Haptics.Play(HapticStrength.Light);
        }

        public void FindClue()
        {
            if (ClueFoundThisRun || string.IsNullOrEmpty(clueId)) return;
            ClueFoundThisRun = true;
            var save = Services.Save;
            bool isNew = save != null && save.Data.FindClue(clueId);
            if (save != null) save.SaveNow();
            EventBus<StealthNotice>.Raise(new StealthNotice
            {
                text = isNew || !ClueAlreadyKnown ? "CIPHER SCROLL FOUND!\nA SECRET CONTRACT IS REVEALED" : "CIPHER SCROLL (ALREADY DECODED)"
            });
            if (Services.Haptics != null) Services.Haptics.Play(HapticStrength.Heavy);
        }
    }

    /// <summary>An ancient relic: worth points in a Shadow Contract. Touch to take it.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class RelicPickup : MonoBehaviour
    {
        public string relicName = "Ancient Relic";
        bool _taken;
        float _age;
        Transform _art;

        void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            _art = transform.childCount > 0 ? transform.GetChild(0) : null;
            _age = Random.value * 3f;
        }

        void Update()
        {
            _age += Time.deltaTime;
            if (_art != null) _art.localPosition = new Vector3(0f, Mathf.Sin(_age * 2.5f) * 0.1f, 0f);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (_taken || !TriggerUtil.IsPlayer(other, out _)) return;
            _taken = true;
            if (MissionTracker.Current != null) MissionTracker.Current.CollectRelic(relicName);
            var sr = GetComponentInChildren<SpriteRenderer>();
            PuffEffect.Play(transform.position, new Color32(120, 230, 210, 255), sr != null ? sr.sharedMaterial : null, 0.2f, 3.5f, 0.45f);
            gameObject.SetActive(false);
        }
    }

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
