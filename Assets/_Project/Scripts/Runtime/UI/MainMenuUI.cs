using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Portrait main menu: title, Star Shard total, best result for the test level, and
    /// Story Quest / Arena Clash / Versus / Shop / Settings / Codex. Modes lead to Character Select.
    /// The Shop (Star Shard palettes), Codex and Settings open in place as sub-panels.
    /// Buttons for scenes that aren't in the build yet are shown disabled instead of failing.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] LevelDefinition featuredLevel;
        [Tooltip("Heroes and their shop palettes (roster.cosmetics). The SHOP button hides without it.")]
        [SerializeField] CharacterRoster roster;
        [Tooltip("Heroes, foes and places for the CODEX button. The button shows as disabled without it.")]
        [SerializeField] CodexDefinition codex;

        RectTransform _menuRoot, _settingsRoot, _shopRoot, _codexRoot;
        SettingsPanel _settings;
        ShopPanel _shop;
        CodexPanel _codex;

        void Awake()
        {
            var safe = UIFactory.CreateCanvas(transform, "MenuCanvas", 10, landscape: false, out _);
            _menuRoot = UIFactory.Rect("Menu", safe);
            UIFactory.Stretch(_menuRoot);
            _settingsRoot = UIFactory.Rect("Settings", safe);
            UIFactory.Stretch(_settingsRoot);
            _settings = gameObject.AddComponent<SettingsPanel>();
            _shopRoot = UIFactory.Rect("Shop", safe);
            UIFactory.Stretch(_shopRoot);
            _shop = gameObject.AddComponent<ShopPanel>();
            _codexRoot = UIFactory.Rect("Codex", safe);
            UIFactory.Stretch(_codexRoot);
            _codex = gameObject.AddComponent<CodexPanel>();
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
            _shopRoot.gameObject.SetActive(false);
            _codexRoot.gameObject.SetActive(false);
            _menuRoot.gameObject.SetActive(true);
            BuildMenu();
        }

        void BuildMenu()
        {
            _lastHighContrast = Services.Settings != null && Services.Settings.Data.highContrastUI;
            UIFactory.Clear(_menuRoot);
            var panel = UIFactory.Panel(_menuRoot, new Vector2(960f, 1680f));

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
            if (Application.CanStreamedLevelBeLoaded(SceneIds.VersusSelect))
                UIFactory.Button(panel, "VERSUS  ·  1 ON 1", () =>
                {
                    GameSession.Mode = SessionMode.Versus;
                    if (Services.Scenes != null) Services.Scenes.Load(SceneIds.VersusSelect);
                });
            if (Application.CanStreamedLevelBeLoaded(SceneIds.MissionBoard))
            {
                var contracts = UIFactory.Button(panel, "SHADOW CONTRACTS", () =>
                {
                    GameSession.Mode = SessionMode.Contracts;
                    if (Services.Scenes != null) Services.Scenes.Load(SceneIds.MissionBoard);
                });
                UIFactory.Tint(contracts, new Color32(124, 104, 214, 255), Color.white);
            }
            if (roster != null && roster.cosmetics != null && roster.cosmetics.items.Count > 0)
                UIFactory.Button(panel, "SHOP", ShowShop);
            UIFactory.Button(panel, "SETTINGS", ShowSettings);
            if (codex != null)
            {
                int unseen = codex.Progress().unseen;
                UIFactory.Button(panel, unseen > 0 ? $"CODEX  ·  {unseen} NEW" : "CODEX", ShowCodex);
            }
            else UIFactory.Button(panel, "CODEX", null, interactable: false);
            UIFactory.Label(panel, "Prototype · placeholder art", 30, TextAnchor.MiddleCenter, 50f).color = UITheme.Current.subtle;
            UIFactory.Select(story);
        }

        void Choose(SessionMode mode)
        {
            GameSession.Mode = mode;
            if (Services.Scenes != null) Services.Scenes.Load(SceneIds.CharacterSelect);
        }

        void ShowShop()
        {
            _menuRoot.gameObject.SetActive(false);
            _shopRoot.gameObject.SetActive(true);
            _shop.Open(_shopRoot, roster, ShowMenu);
        }

        void ShowCodex()
        {
            _menuRoot.gameObject.SetActive(false);
            _codexRoot.gameObject.SetActive(true);
            _codex.Open(_codexRoot, codex, ShowMenu);
        }

        void ShowSettings()
        {
            _menuRoot.gameObject.SetActive(false);
            _settingsRoot.gameObject.SetActive(true);
            _settings.Open(_settingsRoot, landscape: false, onBack: ShowMenu, onEditLayout: null);
        }
    }
}
