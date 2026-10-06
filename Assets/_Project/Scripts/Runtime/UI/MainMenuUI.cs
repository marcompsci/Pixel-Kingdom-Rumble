using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Portrait main menu: title, Star Shard total, best result for the test level, and
    /// Story Quest / Arena Clash / Settings / Codex. Modes lead to Character Select.
    /// Buttons for scenes that aren't in the build yet are shown disabled instead of failing.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] LevelDefinition featuredLevel;

        RectTransform _menuRoot, _settingsRoot;
        SettingsPanel _settings;

        void Awake()
        {
            var safe = UIFactory.CreateCanvas(transform, "MenuCanvas", 10, landscape: false, out _);
            _menuRoot = UIFactory.Rect("Menu", safe);
            UIFactory.Stretch(_menuRoot);
            _settingsRoot = UIFactory.Rect("Settings", safe);
            UIFactory.Stretch(_settingsRoot);
            _settings = gameObject.AddComponent<SettingsPanel>();
        }

        void OnEnable() => EventBus<SettingsChanged>.Subscribe(OnSettingsChanged);
        void OnDisable() => EventBus<SettingsChanged>.Unsubscribe(OnSettingsChanged);

        void Start() => ShowMenu();

        bool _lastHighContrast;

        void OnSettingsChanged(SettingsChanged e)
        {
            if (e.settings.highContrastUI == _lastHighContrast) return;
            _lastHighContrast = e.settings.highContrastUI;
            if (_menuRoot.gameObject.activeSelf) BuildMenu();
        }

        void ShowMenu()
        {
            _settingsRoot.gameObject.SetActive(false);
            _menuRoot.gameObject.SetActive(true);
            BuildMenu();
        }

        void BuildMenu()
        {
            _lastHighContrast = Services.Settings != null && Services.Settings.Data.highContrastUI;
            UIFactory.Clear(_menuRoot);
            var panel = UIFactory.Panel(_menuRoot, new Vector2(960f, 1560f));

            UIFactory.Label(panel, "PIXEL KINGDOM\nRUMBLE", 96, TextAnchor.MiddleCenter, 260f, title: true);
            UIFactory.Label(panel, "Adventures of the Sunspire Isles", 40, TextAnchor.MiddleCenter, 60f).color = UITheme.Current.subtle;

            var save = Services.Save != null ? Services.Save.Data : null;
            int shards = save != null ? save.starShards : 0;
            UIFactory.Label(panel, $"STAR SHARDS  {shards}", 48, TextAnchor.MiddleCenter, 80f).color = UITheme.Current.title;

            string best = "Not cleared yet";
            if (featuredLevel != null && save != null)
            {
                var rec = save.GetLevel(featuredLevel.id);
                if (rec != null && rec.completed)
                    best = $"Best {ResultsMath.FormatTime(rec.bestTimeSeconds)}  ·  {rec.mostShardsCollected} shards  ·  {rec.mostSecretsFound} secret(s)";
            }
            string levelName = featuredLevel != null ? featuredLevel.displayName : "Story Quest";
            UIFactory.Label(panel, $"{levelName}: {best}", 36, TextAnchor.MiddleCenter, 70f).color = UITheme.Current.subtle;

            var story = UIFactory.Button(panel, "STORY QUEST", () => Choose(SessionMode.StoryQuest),
                                         interactable: Application.CanStreamedLevelBeLoaded(SceneIds.StoryTest));
            bool hasArena = Application.CanStreamedLevelBeLoaded(SceneIds.ArenaTest);
            UIFactory.Button(panel, hasArena ? "ARENA CLASH" : "ARENA CLASH (SOON)", () => Choose(SessionMode.ArenaClash),
                             interactable: hasArena);
            UIFactory.Button(panel, "SETTINGS", ShowSettings);
            UIFactory.Button(panel, "CODEX (SOON)", null, interactable: false);
            UIFactory.Label(panel, "Phase 1 prototype · placeholder art", 30, TextAnchor.MiddleCenter, 50f).color = UITheme.Current.subtle;
            UIFactory.Select(story);
        }

        void Choose(SessionMode mode)
        {
            GameSession.Mode = mode;
            if (Services.Scenes != null) Services.Scenes.Load(SceneIds.CharacterSelect);
        }

        void ShowSettings()
        {
            _menuRoot.gameObject.SetActive(false);
            _settingsRoot.gameObject.SetActive(true);
            _settings.Open(_settingsRoot, landscape: false, onBack: ShowMenu, onEditLayout: null);
        }
    }
}
