using System.Collections.Generic;
using PKR.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// Portrait Story Quest route (03_LevelSelect): one big card per level in play order, showing best rank, time,
    /// Star Shards and secrets. Levels unlock in order; locked cards say what opens them. Opened after Character Select.
    /// </summary>
    public class LevelSelectUI : MonoBehaviour
    {
        [SerializeField] WorldDefinition world;
        public int sortingOrder = 10;

        RectTransform _root;
        LevelSlot[] _slots = new LevelSlot[0];
        readonly List<Button> _cards = new List<Button>();

        public IReadOnlyList<Button> Cards => _cards;
        public LevelSlot[] Slots => _slots;
        public int SuggestedIndex { get; private set; }

        /// <summary>Runtime setup (tests). The menu builder assigns the field instead.</summary>
        public void SetWorld(WorldDefinition w) => world = w;

        void Awake()
        {
            var safe = UIFactory.CreateCanvas(transform, "LevelSelectCanvas", sortingOrder, landscape: false, out _);
            _root = UIFactory.Rect("LevelSelect", safe);
            UIFactory.Stretch(_root);
        }

        void OnEnable() => EventBus<SettingsChanged>.Subscribe(OnSettings);
        void OnDisable() => EventBus<SettingsChanged>.Unsubscribe(OnSettings);
        void OnSettings(SettingsChanged e) => Build();

        void Start()
        {
            if (world == null || world.levels.Count == 0)
            {
                Debug.LogError("[LevelSelectUI] No world assigned. Run PKR > Build Menu Scenes.");
                return;
            }
            Build();
        }

        void Build()
        {
            if (_root == null || world == null) return;
            _slots = LevelSelect.Build(world.LevelIds(), Services.Save != null ? Services.Save.Data : null);
            SuggestedIndex = LevelSelect.Suggested(_slots);

            UIFactory.Clear(_root);
            _cards.Clear();
            var p = UITheme.Current;
            var panel = UIFactory.Panel(_root, new Vector2(1000f, 1760f));
            // Six or more levels: shorter cards so the whole route fits the screen.
            bool compact = world.levels.Count > 4;
            float cardHeight = compact ? 158f : 230f;
            UIFactory.Label(panel, $"STORY QUEST\n{world.displayName.ToUpperInvariant()}", 56, TextAnchor.MiddleCenter, 150f, title: true);
            if (!string.IsNullOrEmpty(world.description))
                UIFactory.Label(panel, world.description, 32, TextAnchor.MiddleCenter, compact ? 80f : 90f).color = p.subtle;

            Button suggested = null;
            for (int i = 0; i < world.levels.Count; i++)
            {
                var level = world.levels[i];
                var slot = _slots[i];
                int idx = i;
                bool playable = slot.unlocked && level != null && Application.CanStreamedLevelBeLoaded(level.sceneName);
                var card = UIFactory.Button(panel, CardText(i, level, slot), () => Play(idx), cardHeight, interactable: playable);
                var text = card.GetComponentInChildren<Text>();
                text.fontSize = compact ? 32 : 38;
                text.alignment = TextAnchor.MiddleCenter;
                _cards.Add(card);
                if (i == SuggestedIndex && playable) suggested = card;
            }

            UIFactory.Button(panel, "BACK", () => { if (Services.Scenes != null) Services.Scenes.Load(SceneIds.CharacterSelect); });
            if (suggested != null) UIFactory.Select(suggested);
        }

        static string CardText(int index, LevelDefinition level, LevelSlot slot)
        {
            string name = level != null ? level.displayName.ToUpperInvariant() : "???";
            if (level != null && !string.IsNullOrEmpty(level.modeTag)) name += $"  [{level.modeTag.ToUpperInvariant()}]";
            if (!slot.unlocked) return $"{index + 1}. {name}\nLOCKED: CLEAR THE LEVEL BEFORE IT";
            if (!slot.completed) return $"{index + 1}. {name}\nNEW!";
            // Enemy drops can push the collected count past the placed total; cap it for display.
            string shards = level != null && level.totalShards > 0 ? $"   SHARDS {Mathf.Min(slot.shards, level.totalShards)}/{level.totalShards}" : "";
            string secrets = level != null && level.totalSecrets > 0 ? $"   SECRETS {slot.secrets}/{level.totalSecrets}" : "";
            return $"{index + 1}. {name}   RANK {LevelSelect.RankLabel(slot.rank)}\n" +
                   $"BEST {ResultsMath.FormatTime(slot.bestTimeSeconds)}{shards}{secrets}";
        }

        void Play(int index)
        {
            if (index < 0 || index >= world.levels.Count || !_slots[index].unlocked) return;
            var level = world.levels[index];
            if (level == null || Services.Scenes == null) return;
            Services.Scenes.Load(level.sceneName);
        }
    }
}
