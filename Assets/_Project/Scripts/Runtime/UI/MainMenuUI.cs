using PKR.Core;
using UnityEngine;
using UnityEngine.UI;

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

        RectTransform _menuRoot, _settingsRoot, _shopRoot, _codexRoot, _eventRoot, _zombiesRoot;
        SettingsPanel _settings;
        ShopPanel _shop;
        CodexPanel _codex;
        EventMenuPanel _events;
        ZombiesMenuPanel _zombies;
        SeasonPanel _season;

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
            _eventRoot = UIFactory.Rect("Events", safe);
            UIFactory.Stretch(_eventRoot);
            _events = gameObject.AddComponent<EventMenuPanel>();
            _zombiesRoot = UIFactory.Rect("Zombies", safe);
            UIFactory.Stretch(_zombiesRoot);
            _zombies = gameObject.AddComponent<ZombiesMenuPanel>();
            _season = gameObject.AddComponent<SeasonPanel>();
            // Live seasons: leaves or snow drift over the menus (never over taps).
            SeasonWeatherUI.Attach(safe, 0.7f);
        }

        void OnEnable() => EventBus<SettingsChanged>.Subscribe(OnSettingsChanged);
        void OnDisable() => EventBus<SettingsChanged>.Unsubscribe(OnSettingsChanged);

        void Start()
        {
            ShowMenu();
            // Back from a Holiday / Halloween level: straight to that panel.
            if (GameSession.ReopenEventMenu)
            {
                GameSession.ReopenEventMenu = false;
                if (GameSession.Mode == SessionMode.Zombies) ShowZombies();
                else if (GameSession.Mode == SessionMode.Halloween) ShowHalloween();
                else if (GameSession.Mode == SessionMode.Holiday) ShowHoliday();
            }
            // Screenshot runs: open one panel straight away.
            if (BootLoader.TakeShotPanel("shop") != null) ShowShop();
            else if (BootLoader.TakeShotPanel("daily") != null) ShowDaily();
            else if (BootLoader.TakeShotPanel("settings") != null) ShowSettings();
            else if (BootLoader.TakeShotPanel("codex") != null && codex != null) ShowCodex();
            else if (BootLoader.TakeShotPanel("holiday") != null) ShowHoliday();
            else if (BootLoader.TakeShotPanel("halloween") != null) ShowHalloween();
            else if (BootLoader.TakeShotPanel("zombies") != null) ShowZombies();
            else if (BootLoader.TakeShotPanel("season") != null) ShowSeason();
            else if (BootLoader.TakeShotPanel("profile") != null) ShowProfile();
            else if (BootLoader.TakeShotPanel("welcome") != null || (!GameSession.ReopenEventMenu && WelcomeGuide.ShouldShow(Services.Save != null ? Services.Save.Data : null)))
                WelcomeGuide.Show(transform, () => Choose(SessionMode.StoryQuest), null);
            else if (BootLoader.TakeShotPanel("whatsnew") != null || (!GameSession.ReopenEventMenu && !Application.isBatchMode && Services.Save != null &&
                     WhatsNew.ShouldShow(Services.Save.Data, WelcomeGuide.IsNewPlayer(Services.Save.Data))))
                WhatsNewCard.Show(transform);
            ClaimLoginGift();
            MenuMeta();
        }

        /// <summary>Badges earned since last time (toasts) and, on the first open of a new week, last week's recap.</summary>
        void MenuMeta()
        {
            var save = Services.Save;
            if (save == null || Application.isBatchMode) return;
            foreach (var b in Badges.Update(save.Data))
                EventBus<ProgressToast>.Raise(new ProgressToast { title = "BADGE: " + b.name, detail = b.how, color = new Color32(120, 220, 255, 255) });
            var week = WeeklyRecap.Roll(save.Data, WeeklyRecap.WeekKey(System.DateTime.Now));
            save.SaveNow();
            if (week == null || FindAnyObjectByType<WelcomeGuide>() != null) return;
            var lines = new System.Collections.Generic.List<string>();
            for (int i = 0; i < week.Length; i++)
                if (week[i] > 0) lines.Add($"{WeeklyRecap.Labels[i]}:  {(i == 4 ? PlayTime.Format(week[i]) : week[i].ToString("N0"))}");
            InfoCard.Show(transform, "LAST WEEK", "Your week in Pixel Kingdom Rumble", lines);
        }

        /// <summary>The daily login gift: Star Shards the first time the game opens each day (more for a streak).</summary>
        static void ClaimLoginGift()
        {
            var save = Services.Save;
            if (save == null || Application.isBatchMode) return;
            var now = System.DateTime.Now;
            int gift = LoginStreak.Claim(save.Data, DailyChallenges.DayKey(now), DailyChallenges.DayKey(now.AddDays(-1)));
            if (gift <= 0) return;
            Economy.Grant(save.Data, gift);
            save.SaveNow();
            int streak = save.Data.lifetime.loginStreak;
            EventBus<ProgressToast>.Raise(new ProgressToast
            {
                title = $"DAILY GIFT  +{gift} STAR SHARDS",
                detail = streak > 1 ? $"Day {streak} in a row  ·  tomorrow +{LoginStreak.GiftFor(streak + 1)}" : $"Come back tomorrow for +{LoginStreak.GiftFor(2)}",
                color = new Color32(255, 214, 90, 255)
            });
        }

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
            _eventRoot.gameObject.SetActive(false);
            _zombiesRoot.gameObject.SetActive(false);
            _menuRoot.gameObject.SetActive(true);
            BuildMenu();
        }

        void BuildMenu()
        {
            _lastHighContrast = Services.Settings != null && Services.Settings.Data.highContrastUI;
            UIFactory.Clear(_menuRoot);
            var panel = UIFactory.Panel(_menuRoot, new Vector2(960f, 1800f));

            UIFactory.Label(panel, "PIXEL KINGDOM\nRUMBLE", 92, TextAnchor.MiddleCenter, 210f, title: true);
            if (roster != null)
            {
                var castRow = UIFactory.Rect("Cast", panel);
                var le = castRow.gameObject.AddComponent<LayoutElement>();
                le.preferredHeight = le.minHeight = 190f;
                TitleCast.Build(castRow, roster, 6);
            }

            var save = Services.Save != null ? Services.Save.Data : null;
            int shards = save != null ? save.starShards : 0;
            int xp = save != null ? save.playerXp : 0;
            int lv = PlayerLevel.LevelFor(xp);
            int pct = Mathf.RoundToInt(PlayerLevel.Progress01(xp) * 100f);
            UIFactory.Label(panel, $"LEVEL {lv}  ({pct}%)   ·   STAR SHARDS  {shards}", 44, TextAnchor.MiddleCenter, 80f).color = UITheme.Current.title;

            string best = "Not cleared yet";
            if (featuredLevel != null && save != null)
            {
                var rec = save.GetLevel(featuredLevel.id);
                if (rec != null && rec.completed)
                    best = $"Best {ResultsMath.FormatTime(rec.bestTimeSeconds)}  ·  {rec.mostShardsCollected} shards  ·  {rec.mostSecretsFound} secret(s)";
            }
            string levelName = featuredLevel != null ? featuredLevel.displayName : "Story Quest";
            var season = SettingsService.Season;

            // STORY QUEST leads the menu (the main game), dressed for the live season when one is on.
            ContinueButton(panel, save);
            var story = UIFactory.Button(panel, "STORY QUEST", () => Choose(SessionMode.StoryQuest), 128f,
                                         interactable: Application.CanStreamedLevelBeLoaded(SceneIds.StoryTest));
            UIFactory.Tint(story, season.Active ? SeasonPanel.ColorFor(season) : (Color)new Color32(214, 116, 40, 255), Color.white);
            UIFactory.Highlight(story, new Color32(255, 200, 80, 255));
            string storySub = season.Active ? $"{SeasonCalendar.Name(season.Event)} is live in every world" : $"{levelName}: {best}";
            UIFactory.TwoLine(story, "STORY QUEST", storySub, 50, 24);
            if (season.Active) SeasonBanner(panel, season, save);

            var zombies = UIFactory.Button(panel, "ZOMBIES", ShowZombies, 116f);
            UIFactory.Tint(zombies, new Color32(190, 40, 40, 255), Color.white);
            var career = save != null ? save.zombieCareer : null;
            int zr = ZombiePrestige.Rank(career);
            string pz = career != null && career.prestige > 0 ? $"P{career.prestige} " : "";
            UIFactory.TwoLine(zombies, "ZOMBIES", $"Zombie Land · Last Stand · Survival  ·  {pz}Rank {zr}", 44, 22);
            // Holiday Mode and Halloween (zombies) side by side.
            var events = UIFactory.Rect("EventModes", panel);
            events.gameObject.AddComponent<LayoutElement>().preferredHeight = UIFactory.RowHeight;
            var row = events.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 24f;
            row.childControlWidth = true; row.childControlHeight = true;
            row.childForceExpandWidth = true; row.childForceExpandHeight = true;
            var holiday = UIFactory.Button(events, "HOLIDAY MODE", ShowHoliday);
            UIFactory.Tint(holiday, new Color32(60, 150, 100, 255), Color.white);
            var halloween = UIFactory.Button(events, "HALLOWEEN", ShowHalloween);
            UIFactory.Tint(halloween, new Color32(220, 110, 30, 255), Color.white);
            if (EventMenuPanel.IsHalloweenSeason) UIFactory.Highlight(halloween, new Color32(255, 200, 80, 255));

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
            // Shop and Settings share a row (keeps the menu on one phone screen).
            var misc = UIFactory.Rect("ShopSettings", panel);
            misc.gameObject.AddComponent<LayoutElement>().preferredHeight = UIFactory.RowHeight;
            var miscRow = misc.gameObject.AddComponent<HorizontalLayoutGroup>();
            miscRow.spacing = 24f;
            miscRow.childControlWidth = true; miscRow.childControlHeight = true;
            miscRow.childForceExpandWidth = true; miscRow.childForceExpandHeight = true;
            if (roster != null && roster.cosmetics != null && roster.cosmetics.items.Count > 0)
                UIFactory.Button(misc, "SHOP", ShowShop);
            int dailyDone = 0;
            foreach (var c in ProgressTracker.Today()) if (c != null && c.done) dailyDone++;
            var daily = UIFactory.Button(misc, $"DAILY {dailyDone}/{DailyChallenges.PerDay}", ShowDaily);
            UIFactory.Tint(daily, new Color32(70, 150, 200, 255), Color.white);
            UIFactory.Button(misc, "SETTINGS", ShowSettings);
            var cRow = UIFactory.Rect("ProfileCodex", panel);
            cRow.gameObject.AddComponent<LayoutElement>().preferredHeight = UIFactory.RowHeight;
            var ch = cRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            ch.spacing = 24f;
            ch.childControlWidth = true; ch.childControlHeight = true;
            ch.childForceExpandWidth = true; ch.childForceExpandHeight = true;
            bool profileNew = save != null && Badges.ProfileScore(save) > save.lifetime.profileSeen;
            var prof = UIFactory.Button(cRow, profileNew ? "PROFILE · NEW" : "PROFILE", ShowProfile);
            if (profileNew) UIFactory.Highlight(prof, new Color32(255, 214, 90, 255));
            if (codex != null)
            {
                int unseen = codex.Progress().unseen;
                UIFactory.Button(cRow, unseen > 0 ? $"CODEX · {unseen} NEW" : "CODEX", ShowCodex);
            }
            else UIFactory.Button(cRow, "CODEX", null, interactable: false);
            if (GameCenterService.Available)
            {
                var gc = UIFactory.Button(panel, "GAME CENTER  ·  ACHIEVEMENTS & RANKS", GameCenterService.ShowDashboard, 80f);
                gc.GetComponentInChildren<Text>().fontSize = 30;
            }
            else UIFactory.Label(panel, "Original pixel art · made for touch", 30, TextAnchor.MiddleCenter, 50f).color = UITheme.Current.subtle;
            // Tip of the day.
            var tip = UIFactory.Label(panel, "TIP: " + TipOfTheDay.For(DailyChallenges.DayKey(System.DateTime.Now)), 26, TextAnchor.MiddleCenter, 64f);
            tip.color = UITheme.Current.subtle;
            UIFactory.Select(story.interactable ? story : zombies);
            FitPanel(panel);
        }

        /// <summary>
        /// The live season, under the title: what's on, the season's tokens and what's next. Tap it for the season
        /// panel (challenges, keepsakes). Replaces the featured-level line while a season runs.
        /// </summary>
        void SeasonBanner(RectTransform panel, SeasonState s, SaveData save)
        {
            var today = SeasonCalendar.Today;
            int have = save != null ? Seasons.Tokens(save, s) : 0;
            int unclaimed = save != null ? Seasons.Unclaimed(save, s) : 0;
            string headline = s.AtPeak ? $"{SeasonCalendar.PeakName(s.Peak).ToUpperInvariant()}  ·  LIVE"
                                       : $"{SeasonCalendar.Name(s.Event).ToUpperInvariant()}  ·  LIVE";
            string sub;
            if (unclaimed > 0) sub = $"{Plural.Of(unclaimed, "reward")} to claim!";
            else
            {
                var peak = SeasonCalendar.NextPeak(today);
                var next = SeasonCalendar.Next(today);
                bool peakSoon = !s.AtPeak && peak.peak != SeasonPeak.None && peak.start <= s.End;
                sub = peakSoon ? $"{SeasonCalendar.PeakName(peak.peak)} {SeasonCalendar.InDays((peak.start - today).Days)}"
                               : $"{SeasonCalendar.Name(next.ev)} {SeasonCalendar.InDays((next.start - today).Days)}";
            }
            var b = UIFactory.Button(panel, "", ShowSeason, 96f);
            UIFactory.Tint(b, Color.Lerp(SeasonPanel.ColorFor(s), Color.black, 0.35f), Color.white);
            if (unclaimed > 0 || s.AtPeak) UIFactory.Highlight(b, new Color32(255, 214, 90, 255));
            UIFactory.TwoLine(b, headline, $"{Seasons.TokenName(s.Event)}s {have}/{SeasonDirector.StoryLevelCount}  ·  {sub}", 34, 20);
        }

        void ShowSeason()
        {
            _menuRoot.gameObject.SetActive(false);
            _eventRoot.gameObject.SetActive(true);
            _season.Open(_eventRoot, ShowMenu);
        }

        /// <summary>
        /// The menu panel takes exactly the height of its contents, and shrinks to fit when a phone's screen is shorter
        /// than that, so every button (down to GAME CENTER) is always inside the frame and on screen.
        /// </summary>
        void FitPanel(RectTransform panel)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
            float h = LayoutUtility.GetPreferredHeight(panel);
            if (h < 200f) return;
            UIFactory.ResizePanel(panel, h);
            if (panel.parent is RectTransform edge)
            {
                float room = _menuRoot.rect.height - 24f;
                float need = edge.sizeDelta.y;
                edge.localScale = room > 400f && need > room ? Vector3.one * (room / need) : Vector3.one;
            }
        }

        /// <summary>CONTINUE: straight back to the last thing played (a Story level, the plaza, Arena Clash, Versus or Zombies).</summary>
        void ContinueButton(RectTransform panel, SaveData save)
        {
            if (save == null || save.lifetime == null) return;
            var l = save.lifetime;
            var kind = (ContinueKind)l.lastKind;
            string scene = l.lastScene;
            string sub = null;
            System.Action go = null;
            switch (kind)
            {
                case ContinueKind.StoryLevel:
                    if (string.IsNullOrEmpty(scene) || !Application.CanStreamedLevelBeLoaded(scene)) return;
                    sub = "Story Quest  ·  " + LevelName(l.lastLevelId);
                    go = () => { GameSession.Mode = SessionMode.StoryQuest; if (Services.Scenes != null) Services.Scenes.Load(scene); };
                    break;
                case ContinueKind.Plaza:
                    if (!Application.CanStreamedLevelBeLoaded(SceneIds.KingdomPlaza)) return;
                    sub = "Story Quest  ·  Kingdom Plaza";
                    go = () => { GameSession.Mode = SessionMode.StoryQuest; if (Services.Scenes != null) Services.Scenes.Load(SceneIds.KingdomPlaza); };
                    break;
                case ContinueKind.ArenaClash:
                    if (!Application.CanStreamedLevelBeLoaded(SceneIds.ArenaTest)) return;
                    sub = "Arena Clash";
                    go = () => { GameSession.Mode = SessionMode.ArenaClash; if (Services.Scenes != null) Services.Scenes.Load(SceneIds.ArenaTest); };
                    break;
                case ContinueKind.Versus:
                    if (!Application.CanStreamedLevelBeLoaded(SceneIds.VersusSelect)) return;
                    sub = "Versus";
                    go = () => { GameSession.Mode = SessionMode.Versus; if (Services.Scenes != null) Services.Scenes.Load(SceneIds.VersusSelect); };
                    break;
                case ContinueKind.Zombies:
                    sub = "Zombies";
                    go = ShowZombies;
                    break;
                default: return;
            }
            var b = UIFactory.Button(panel, "CONTINUE", () => go(), 96f);
            UIFactory.Tint(b, new Color32(60, 150, 100, 255), Color.white);
            UIFactory.TwoLine(b, "CONTINUE", sub, 40, 22);
        }

        string LevelName(string id)
        {
            if (featuredLevel != null && featuredLevel.id == id) return featuredLevel.displayName;
            var all = Resources.FindObjectsOfTypeAll<LevelDefinition>();
            foreach (var d in all) if (d != null && d.id == id) return d.displayName;
            return string.IsNullOrEmpty(id) ? "your last level" : id.Replace("sq_", "").Replace('_', ' ').ToUpperInvariant();
        }

        void ShowProfile()
        {
            _menuRoot.gameObject.SetActive(false);
            _eventRoot.gameObject.SetActive(true);
            var p = GetComponent<ProfilePanel>() ?? gameObject.AddComponent<ProfilePanel>();
            p.Open(_eventRoot, roster, ShowMenu);
        }

        void Choose(SessionMode mode)
        {
            GameSession.Mode = mode;
            if (Services.Scenes != null) Services.Scenes.Load(SceneIds.CharacterSelect);
        }

        void ShowDaily()
        {
            _menuRoot.gameObject.SetActive(false);
            _eventRoot.gameObject.SetActive(true);
            _events.OpenDaily(_eventRoot, ShowMenu);
        }

        void ShowHoliday()
        {
            _menuRoot.gameObject.SetActive(false);
            _eventRoot.gameObject.SetActive(true);
            _events.OpenHoliday(_eventRoot, ShowMenu);
        }

        void ShowZombies()
        {
            _menuRoot.gameObject.SetActive(false);
            _zombiesRoot.gameObject.SetActive(true);
            _zombies.Open(_zombiesRoot, ShowMenu);
        }

        void ShowHalloween()
        {
            _menuRoot.gameObject.SetActive(false);
            _eventRoot.gameObject.SetActive(true);
            _events.OpenHalloween(_eventRoot, ShowMenu);
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
