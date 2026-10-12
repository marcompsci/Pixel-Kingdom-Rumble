using System.Collections.Generic;
using PKR.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// Match rules screen shown when the arena opens (and from "Change Rules" after a match).
    /// Left: mode, players (1P / couch 2P), P2's fighter, CPU fighters, CPU level (Auto adapts to you), twist,
    /// stocks or minutes. Right: your Rumble Rating and today's Daily Rumble.
    /// Each option is one big tap-to-cycle button, which is quick to use with a thumb.
    /// </summary>
    public class ArenaSetupUI : MonoBehaviour
    {
        [SerializeField] ArenaMatchController match;
        public int sortingOrder = 220;

        const float CycleHeight = 76f;
        static readonly Mutator[] Twists =
            { Mutator.None, Mutator.LowGravity, Mutator.HeavyHitters, Mutator.Giants, Mutator.SuddenDeath };

        RectTransform _root;
        static ArenaMatchConfig _last; // remembered for the session so rematches keep your rules

        void Awake()
        {
            if (match == null) match = FindAnyObjectByType<ArenaMatchController>();
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
            cfg.daily = false;
            cfg.Clamp();
            UIFactory.Clear(_root);
            UIFactory.Dim(_root);
            var panel = UIFactory.Panel(_root, new Vector2(1640f, 1000f));
            UIFactory.Label(panel, "ARENA CLASH · SKYFORGE ARENA", 50, TextAnchor.MiddleCenter, 60f, title: true);

            var columns = UIFactory.Rect("Columns", panel);
            columns.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            var ch = columns.gameObject.AddComponent<HorizontalLayoutGroup>();
            ch.spacing = 40f;
            ch.childControlWidth = true; ch.childControlHeight = true;
            ch.childForceExpandWidth = true; ch.childForceExpandHeight = true;
            var left = Column(columns, "Rules");
            var right = Column(columns, "Rumble");

            BuildRules(left, cfg);
            BuildRumble(right, cfg);

            var row = UIFactory.Rect("Buttons", panel);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = UIFactory.RowHeight;
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 24f;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = true; h.childForceExpandHeight = true;
            UIFactory.Button(row, "MAIN MENU", () => { if (Services.Scenes != null) Services.Scenes.Load(SceneIds.MainMenu); },
                             interactable: Application.CanStreamedLevelBeLoaded(SceneIds.MainMenu));
            var start = UIFactory.Button(row, "START!", () => Launch(cfg));
            UIFactory.Select(start);
        }

        static RectTransform Column(RectTransform parent, string name)
        {
            var col = UIFactory.Rect(name, parent);
            var v = col.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 14f;
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true; v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            return col;
        }

        void BuildRules(RectTransform col, ArenaMatchConfig cfg)
        {
            Cycle(col, () => $"MODE: {ModeName(cfg.mode)}", () => cfg.mode = ArenaMatchConfig.Next(cfg.mode), rebuild: true);
            if (cfg.mode != MatchMode.Training)
            {
                Cycle(col, () => cfg.humans >= 2 ? "PLAYERS: 2 (COUCH)" : "PLAYERS: 1", () =>
                {
                    cfg.humans = cfg.humans >= 2 ? 1 : 2;
                    cfg.Clamp();
                }, rebuild: true);
                if (cfg.humans >= 2)
                    Cycle(col, () => $"P2 FIGHTER: {P2Name(cfg)}", () => cfg.p2CharacterId = NextHero(cfg.p2CharacterId));
            }
            Cycle(col, () => $"CPU FIGHTERS: {cfg.botCount}",
                  () => cfg.botCount = ArenaMatchConfig.Cycle(cfg.botCount, cfg.MinBotsNow, cfg.MaxBotsNow));
            if (cfg.mode != MatchMode.Training)
            {
                Cycle(col, () => $"CPU LEVEL: {RumbleRecorder.LevelLabel(cfg.autoLevel, cfg.botLevel)}", cfg.NextLevel);
                Cycle(col, () => $"TWIST: {DailyRumble.Describe(cfg.mutators).ToUpperInvariant()}", () =>
                {
                    int i = System.Array.IndexOf(Twists, cfg.mutators);
                    cfg.mutators = Twists[(i + 1) % Twists.Length];
                }, rebuild: true);
            }
            if (cfg.mode == MatchMode.Stock && (cfg.mutators & Mutator.SuddenDeath) == 0)
                Cycle(col, () => $"STOCKS: {cfg.stocks}",
                      () => cfg.stocks = ArenaMatchConfig.Cycle(cfg.stocks, ArenaMatchConfig.MinStocks, ArenaMatchConfig.MaxStocks));
            if (cfg.mode == MatchMode.Timed)
                Cycle(col, () => $"TIME: {cfg.minutes} MIN",
                      () => cfg.minutes = ArenaMatchConfig.Cycle(cfg.minutes, ArenaMatchConfig.MinMinutes, ArenaMatchConfig.MaxMinutes));

            string hint = cfg.humans >= 2 ? CouchInput.Make().description + "\nCouch matches are just for fun (unrated)." : Hint(cfg.mode);
            UIFactory.Label(col, hint, 28, TextAnchor.MiddleCenter, 80f).color = UITheme.Current.subtle;
        }

        void BuildRumble(RectTransform col, ArenaMatchConfig cfg)
        {
            var theme = UITheme.Current;
            var p = RumbleRecorder.Profile ?? new RumbleProfile();
            UIFactory.Label(col, "RUMBLE RATING", 40, TextAnchor.MiddleCenter, 52f, title: true);
            UIFactory.Label(col, RumbleUI.RankLine(p), 40, TextAnchor.MiddleCenter, 56f).color = RumbleUI.TierColor(p.Tier);
            RumbleUI.AddBar(col, RumbleTiers.Progress(p.rating), RumbleUI.TierColor(p.Tier));
            string record = p.matches > 0 ? $"{p.wins} wins in {p.matches} rated matches  ·  best {p.bestRating}" : "Play a solo match vs the CPU to start ranking up";
            UIFactory.Label(col, record, 28, TextAnchor.MiddleCenter, 44f).color = theme.subtle;
            UIFactory.Label(col, "Placement first, then KOs and style. Harder CPUs pay more. Bronze never loses points.",
                            26, TextAnchor.MiddleCenter, 70f).color = theme.subtle;

            int today = RumbleRecorder.Today;
            var spec = DailyRumble.For(today);
            bool done = DailyRumble.PlayedToday(p, today);
            int streak = DailyRumble.StreakIfPlayed(p, today);
            UIFactory.Label(col, $"DAILY RUMBLE · {spec.title}", 40, TextAnchor.MiddleCenter, 56f, title: true);
            UIFactory.Label(col, spec.description, 30, TextAnchor.MiddleCenter, 46f);
            string streakText = done
                ? $"Done today! Streak: {p.dailyStreak} day{(p.dailyStreak == 1 ? "" : "s")}. Come back tomorrow."
                : $"+{RumblePoints.DailyBonusFor(streak)} bonus points today" + (streak > 1 ? $" ({streak}-day streak)" : "");
            UIFactory.Label(col, streakText, 28, TextAnchor.MiddleCenter, 44f).color = done ? theme.subtle : theme.accent;
            UIFactory.Button(col, done ? "PRACTICE TODAY'S TWIST" : "PLAY DAILY RUMBLE", () => Launch(DailyConfig(spec, !done)), CycleHeight);
        }

        static ArenaMatchConfig DailyConfig(DailyRumbleSpec spec, bool rated)
        {
            var c = new ArenaMatchConfig
            {
                mode = spec.mode, botCount = spec.bots, stocks = spec.stocks, minutes = spec.minutes,
                mutators = spec.mutators, autoLevel = true, humans = 1, daily = rated,
            };
            c.Clamp();
            return c;
        }

        void Launch(ArenaMatchConfig cfg)
        {
            _root.gameObject.SetActive(false);
            if (match != null) match.StartMatch(cfg);
        }

        void Cycle(RectTransform parent, System.Func<string> label, System.Action next, bool rebuild = false)
        {
            Button b = null;
            b = UIFactory.Button(parent, label(), () =>
            {
                next();
                if (rebuild) Build();
                else b.GetComponentInChildren<Text>().text = label();
            }, CycleHeight);
        }

        List<CharacterDefinition> Heroes()
        {
            var list = new List<CharacterDefinition>();
            var roster = match != null ? match.Roster : null;
            if (roster == null) return list;
            foreach (var h in roster.heroes) if (h != null && roster.IsSelectable(h)) list.Add(h);
            return list;
        }

        string P2Name(ArenaMatchConfig cfg)
        {
            foreach (var h in Heroes())
                if (h.id == cfg.p2CharacterId) return h.displayName.ToUpperInvariant();
            return "RANDOM";
        }

        string NextHero(string current)
        {
            var heroes = Heroes();
            if (heroes.Count == 0) return null;
            int i = heroes.FindIndex(h => h.id == current);
            if (i < 0) return heroes[0].id;            // random -> first
            return i + 1 < heroes.Count ? heroes[i + 1].id : null; // last -> random
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
