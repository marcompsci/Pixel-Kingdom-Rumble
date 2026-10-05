using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Owns the player's SaveData. Gameplay mutates Data then calls MarkDirty();
    /// writes are batched to at most once per second, plus on app pause/quit.
    /// </summary>
    public class SaveService : MonoBehaviour
    {
        public const string FileName = "pkr_save.json";
        const float AutoSaveInterval = 1f;

        public SaveData Data { get; private set; } = SaveData.CreateNew();

        bool _dirty;
        float _lastSave;

        public void Load()
        {
            Data = JsonFileStore.TryRead<SaveData>(FileName) ?? SaveData.CreateNew();
            Data.SanitizeAndMigrate();
        }

        public void MarkDirty() => _dirty = true;

        public void SaveNow()
        {
            if (JsonFileStore.TryWrite(FileName, Data))
            {
                _dirty = false;
                _lastSave = Time.unscaledTime;
            }
        }

        public void AddShards(int amount)
        {
            if (amount <= 0) return;
            int before = Data.starShards;
            Economy.Grant(Data, amount);
            MarkDirty();
            EventBus<ShardsChanged>.Raise(new ShardsChanged { total = Data.starShards, delta = Data.starShards - before });
        }

        /// <summary>Debug/test helper: wipes progress (settings are kept).</summary>
        public void ResetProgress()
        {
            Data = SaveData.CreateNew();
            SaveNow();
        }

        void Update()
        {
            if (_dirty && Time.unscaledTime - _lastSave >= AutoSaveInterval) SaveNow();
        }

        void OnApplicationQuit()
        {
            if (_dirty) SaveNow();
        }
    }
}
