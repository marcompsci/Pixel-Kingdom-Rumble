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

}
