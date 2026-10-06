using PKR.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// Match rules screen shown when the arena opens (and from "Change Rules" after a match):
    /// mode (Stock / Timed / Training), number of CPU fighters, CPU level, stocks or minutes, Start, Main Menu.
    /// Each option is one big tap-to-cycle button, which is quick to use with a thumb.
    /// </summary>
    public class ArenaSetupUI : MonoBehaviour
    {
        [SerializeField] ArenaMatchController match;
        public int sortingOrder = 220;

        RectTransform _root;
        static ArenaMatchConfig _last; // remembered for the session so rematches keep your rules

        void Awake()
        {
            if (match == null) match = FindFirstObjectByType<ArenaMatchController>();
            var safe = UIFactory.CreateCanvas(transform, "SetupCanvas", sortingOrder, landscape: true, out _);
            _root = UIFactory.Rect("Setup", safe);
            UIFactory.Stretch(_root);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _last = null;

        void OnEnable() => EventBus<SettingsChanged>.Subscribe(OnSettings);
        void OnDisable() => EventBus<SettingsChanged>.Unsubscribe(OnSettings);
        void OnSettings(SettingsChanged e) { if (_root.gameObject.activeSelf) Build(); }

        void Start() => Show();

        public void Show()
        {
            _root.gameObject.SetActive(true);
            Build();
        }

        void Build()
        {
            var cfg = _last ?? (_last = new ArenaMatchConfig());
            UIFactory.Clear(_root);
            UIFactory.Dim(_root);
            var panel = UIFactory.Panel(_root, new Vector2(1300f, 960f));
            UIFactory.Label(panel, "ARENA CLASH · SKYFORGE ARENA", 56, TextAnchor.MiddleCenter, 80f, title: true);

            Cycle(panel, () => $"MODE: {ModeName(cfg.mode)}", () => cfg.mode = ArenaMatchConfig.Next(cfg.mode), rebuild: true);
            Cycle(panel, () => $"CPU FIGHTERS: {cfg.botCount}",
                  () => cfg.botCount = ArenaMatchConfig.Cycle(cfg.botCount, ArenaMatchConfig.MinBots, ArenaMatchConfig.MaxBots));
            if (cfg.mode != MatchMode.Training)
                Cycle(panel, () => $"CPU LEVEL: {cfg.botLevel.ToString().ToUpperInvariant()}",
                      () => cfg.botLevel = ArenaMatchConfig.Next(cfg.botLevel));
            if (cfg.mode == MatchMode.Stock)
                Cycle(panel, () => $"STOCKS: {cfg.stocks}",
                      () => cfg.stocks = ArenaMatchConfig.Cycle(cfg.stocks, ArenaMatchConfig.MinStocks, ArenaMatchConfig.MaxStocks));
            if (cfg.mode == MatchMode.Timed)
                Cycle(panel, () => $"TIME: {cfg.minutes} MIN",
                      () => cfg.minutes = ArenaMatchConfig.Cycle(cfg.minutes, ArenaMatchConfig.MinMinutes, ArenaMatchConfig.MaxMinutes));

            UIFactory.Label(panel, Hint(cfg.mode), 34, TextAnchor.MiddleCenter, 90f).color = UITheme.Current.subtle;

            var row = UIFactory.Rect("Buttons", panel);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = UIFactory.RowHeight;
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 24f;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = true; h.childForceExpandHeight = true;
            UIFactory.Button(row, "MAIN MENU", () => { if (Services.Scenes != null) Services.Scenes.Load(SceneIds.MainMenu); },
                             interactable: Application.CanStreamedLevelBeLoaded(SceneIds.MainMenu));
            var start = UIFactory.Button(row, "START!", () =>
            {
                _root.gameObject.SetActive(false);
                if (match != null) match.StartMatch(cfg);
            });
            UIFactory.Select(start);
        }

        void Cycle(RectTransform parent, System.Func<string> label, System.Action next, bool rebuild = false)
        {
            Button b = null;
            b = UIFactory.Button(parent, label(), () =>
            {
                next();
                if (rebuild) Build();
                else b.GetComponentInChildren<Text>().text = label();
            });
        }

        static string ModeName(MatchMode m) => m == MatchMode.Stock ? "STOCK" : (m == MatchMode.Timed ? "TIMED" : "TRAINING");

        static string Hint(MatchMode m)
        {
            switch (m)
            {
                case MatchMode.Stock: return "Knock rivals past the edges. Last fighter with lives left wins.\nBreak all 3 Guard Pips to EXPOSE a rival, then land a heavy hit.";
                case MatchMode.Timed: return "+1 for every knockout, -1 for every fall. Highest score when time runs out wins.";
                default: return "CPU fighters stand still. Practice combos and Exposed launches.\nPause > Main Menu to leave.";
            }
        }
    }
}
