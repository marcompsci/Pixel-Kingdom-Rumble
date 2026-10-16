using System;
using PKR.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// The ZOMBIES menu (main menu, all year): Survivor Rank, the mode picker (Endless / Raid / Hardcore) and the zombie
    /// games — Zombie Land (the open-city campaign), Last Stand (Hollow Block or Salt Pier; solo, AI squad or online) and
    /// Survival: The Manor — plus the weekly challenge and the rank unlocks list. Players of other mobile zombie modes were angriest when zombies were taken away or only came
    /// back for a limited time, so this one never leaves.
    /// </summary>
    public class ZombiesMenuPanel : MonoBehaviour
    {
        public static readonly Color32 Blood = new Color32(200, 50, 50, 255);
        static readonly Color32 Gold = new Color32(255, 214, 90, 255);

        RectTransform _root;
        Action _onBack;
        SurvivalMode _mode;

        static ZombieCareer Career
        {
            get
            {
                var save = Services.Save != null ? Services.Save.Data : null;
                if (save == null) return new ZombieCareer();
                if (save.zombieCareer == null) save.zombieCareer = new ZombieCareer();
                return save.zombieCareer;
            }
        }

        public void Open(RectTransform root, Action onBack)
        {
            _root = root; _onBack = onBack;
            _mode = (SurvivalMode)Mathf.Clamp(Career.lastMode, 0, 2);
            Build();
        }

        void Build()
        {
            UIFactory.Clear(_root);
            var c = Career;
            var save = Services.Save != null ? Services.Save.Data : null;
            var panel = UIFactory.Panel(_root, new Vector2(980f, 1640f));
            var title = UIFactory.Label(panel, "ZOMBIES", 110, TextAnchor.MiddleCenter, 140f, title: true);
            title.color = new Color32(255, 90, 70, 255);

            // Survivor Rank
            int rank = SurvivorRank.RankFor(c.xp);
            int pct = Mathf.RoundToInt(SurvivorRank.Progress01(c.xp) * 100f);
            var rankLabel = UIFactory.Label(panel, $"SURVIVOR RANK {rank}  ·  {SurvivorRank.Title(rank)}", 44, TextAnchor.MiddleCenter, 64f, title: true);
            rankLabel.color = rank >= SurvivorRank.MaxRank ? Gold : UITheme.Current.title;
            Bar(panel, SurvivorRank.Progress01(c.xp));
            var next = SurvivorRank.Next(rank);
            string nextText = next.rank > 0 ? $"{pct}% to rank {rank + 1}  ·  rank {next.rank}: {next.name}" : "Max rank. The dead fear you.";
            UIFactory.Label(panel, nextText, 30, TextAnchor.MiddleCenter, 46f).color = UITheme.Current.subtle;

            // Mode picker
            var modes = UIFactory.Rect("Modes", panel);
            var mle = modes.gameObject.AddComponent<LayoutElement>();
            mle.preferredHeight = mle.minHeight = 96f;
            mle.flexibleHeight = 0f;
            var row = modes.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 18f;
            row.childControlWidth = true; row.childControlHeight = true;
            row.childForceExpandWidth = true; row.childForceExpandHeight = true;
            Button selected = null;
            foreach (SurvivalMode m in Enum.GetValues(typeof(SurvivalMode)))
            {
                var mm = m;
                var b = UIFactory.Button(modes, SurvivalModes.Name(m), () => { _mode = mm; Career.lastMode = (int)mm; Build(); }, 96f);
                bool on = m == _mode;
                UIFactory.Tint(b, on ? (Color)ModeColor(m) : new Color32(60, 56, 72, 255), on ? Color.white : new Color(1f, 1f, 1f, 0.7f));
                if (on) { UIFactory.Highlight(b, Gold); selected = b; }
            }
            UIFactory.Label(panel, SurvivalModes.Blurb(_mode), 30, TextAnchor.MiddleCenter, 50f).color = UITheme.Current.subtle;

            // The games
            var zl = save != null ? save.zombieLand : null;
            string zlSub = zl == null || zl.seed == 0 ? "A city of the dead · 19 missions in 2 chapters · drive anything · hunt the First Turned"
                         : zl.chapter2Done ? "Both chapters cleared! Roam free, or start a new mystery."
                         : zl.bossDefeated ? $"CHAPTER 2: BLACKWATER · sigils {zl.Sigils}/{ZLCatalog.SigilsNeeded} · cross the causeway"
                         : $"Shards {zl.Shards}/{ZLCatalog.ShardsNeeded} · missions {zl.completed.Count}/{ZLCatalog.Missions.Length} · scrap {zl.scrap}";
            var zlButton = Big(panel, "ZOMBIE LAND", zlSub, new Color32(110, 60, 150, 255), () => Go(SceneIds.ZombieLand));

            int lsBest = 0;
            foreach (var m in LastStandMap.All) lsBest = Mathf.Max(lsBest, c.Best(m.Id, _mode));
            string lsSub = (lsBest > 0 ? $"{SurvivalModes.Name(_mode)} best: round {lsBest}" : "Round-based")
                           + $" · {LastStandMap.All.Length} maps · solo, AI squad or online co-op";
            Big(panel, "LAST STAND", lsSub, new Color32(170, 70, 40, 255), OpenSquad,
                Application.CanStreamedLevelBeLoaded(SceneIds.LastStand));

            int manorBest = c.Best("manor", _mode);
            if (_mode == SurvivalMode.Endless && save != null) manorBest = Mathf.Max(manorBest, save.zombieBestRound);
            string mSub = (manorBest > 0 ? $"{SurvivalModes.Name(_mode)} best: round {manorBest}" : "Hold the manor windows")
                          + " · wall guns · crate · forge · lantern secret";
            Big(panel, "SURVIVAL: THE MANOR", mSub, new Color32(200, 70, 50, 255), () => Go(SceneIds.ZombieSurvival));

            // The weekly challenge
            var w = ZombieWeekly.Current(DateTime.UtcNow);
            bool done = ZombieWeekly.Done(c, w);
            string wSub = $"{LastStandMap.Get(w.MapId).Title} · {w.Set.Names()} · " +
                          (done ? "DONE! New one Monday" : $"reach round {w.GoalRound} · {ZombieWeekly.LeftText(DateTime.UtcNow)}");
            Big(panel, done ? "WEEKLY CHALLENGE: DONE" : "WEEKLY CHALLENGE", wSub, new Color32(130, 100, 30, 255), OpenWeekly,
                Application.CanStreamedLevelBeLoaded(SceneIds.LastStand));

            var foot = UIFactory.Rect("Foot", panel);
            var fle = foot.gameObject.AddComponent<LayoutElement>();
            fle.preferredHeight = fle.minHeight = UIFactory.RowHeight;
            fle.flexibleHeight = 0f;
            var fr = foot.gameObject.AddComponent<HorizontalLayoutGroup>();
            fr.spacing = 24f;
            fr.childControlWidth = true; fr.childControlHeight = true;
            fr.childForceExpandWidth = true; fr.childForceExpandHeight = true;
            UIFactory.Button(foot, "RANK & UNLOCKS", OpenUnlocks);
            UIFactory.Button(foot, "BACK", () => _onBack?.Invoke());
            UIFactory.Select(zlButton);
        }

        static Color32 ModeColor(SurvivalMode m) =>
            m == SurvivalMode.Raid ? new Color32(70, 130, 200, 255) : m == SurvivalMode.Hardcore ? new Color32(170, 30, 40, 255) : new Color32(70, 150, 90, 255);

        void OpenUnlocks()
        {
            UIFactory.Clear(_root);
            var c = Career;
            int rank = SurvivorRank.RankFor(c.xp);
            var panel = UIFactory.Panel(_root, new Vector2(980f, 1760f));
            var title = UIFactory.Label(panel, "SURVIVOR RANK", 80, TextAnchor.MiddleCenter, 110f, title: true);
            title.color = new Color32(255, 90, 70, 255);
            UIFactory.Label(panel, $"RANK {rank}  ·  {SurvivorRank.Title(rank)}  ·  {c.xp} XP", 38, TextAnchor.MiddleCenter, 56f).color = UITheme.Current.title;
            UIFactory.Label(panel, "Every zombie mode earns XP. Every rank pays Star Shards. Nothing here is for sale.", 28,
                            TextAnchor.MiddleCenter, 50f).color = UITheme.Current.subtle;
            foreach (var u in SurvivorRank.Unlocks)
            {
                bool have = rank >= u.rank;
                var t = UIFactory.Label(panel, $"{(have ? "UNLOCKED" : "RANK " + u.rank)}  {u.name}\n<size=24>{u.info}</size>", 32, TextAnchor.MiddleLeft, 88f);
                t.supportRichText = true;
                t.color = have ? (Color)Gold : new Color(1f, 1f, 1f, 0.55f);
            }
            string records = $"Raid wins {c.raidWins}  ·  zombies downed {c.totalKills}";
            if (c.Has("lantern_wraith")) records += "  ·  Lantern Wraith slain";
            foreach (var m in LastStandMap.All) if (c.Has(m.HiddenSecret)) records += $"  ·  {m.HiddenBossName} slain";
            if (c.weeklyWins > 0) records += $"  ·  weeklies won {c.weeklyWins}";
            UIFactory.Label(panel, records, 28, TextAnchor.MiddleCenter, 60f).color = UITheme.Current.subtle;
            var back = UIFactory.Button(panel, "BACK", Build);
            UIFactory.Select(back);
        }

        static void Bar(RectTransform panel, float t)
        {
            var holder = UIFactory.Rect("RankBar", panel);
            holder.gameObject.AddComponent<LayoutElement>().preferredHeight = 26f;
            var bg = holder.gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.45f);
            var fill = UIFactory.Rect("Fill", holder);
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(Mathf.Clamp01(t), 1f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            fill.gameObject.AddComponent<Image>().color = new Color32(220, 60, 50, 255);
        }

        static Button Big(RectTransform panel, string name, string sub, Color32 color, UnityEngine.Events.UnityAction go, bool interactable = true)
        {
            var b = UIFactory.Button(panel, name, go, 168f, interactable);
            UIFactory.Tint(b, color, Color.white);
            var t = b.GetComponentInChildren<Text>();
            if (t != null)
            {
                t.supportRichText = true;
                t.resizeTextForBestFit = false;
                t.fontSize = 44;
                t.text = $"{name}\n<size=26>{sub}</size>";
            }
            return b;
        }

        // ---- Last Stand: who's with you ------------------------------------------------------------------

        Text _status;
        bool _fillAI = true;

        void OpenSquad()
        {
            UIFactory.Clear(_root);
            var panel = UIFactory.Panel(_root, new Vector2(980f, 1460f));
            var title = UIFactory.Label(panel, "LAST STAND", 96, TextAnchor.MiddleCenter, 120f, title: true);
            title.color = new Color32(255, 120, 70, 255);
            var map = LastStandMap.Get(GameSession.ZombieMap);
            // map picker
            var maps = UIFactory.Rect("Maps", panel);
            var mle = maps.gameObject.AddComponent<LayoutElement>();
            mle.preferredHeight = mle.minHeight = 110f;
            mle.flexibleHeight = 0f;
            var row = maps.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 18f;
            row.childControlWidth = true; row.childControlHeight = true;
            row.childForceExpandWidth = true; row.childForceExpandHeight = true;
            foreach (var m in LastStandMap.All)
            {
                var mm = m;
                int best = Career.Best(m.Id, _mode);
                var b = UIFactory.Button(maps, m.Title, () => { GameSession.ZombieMap = mm.Id; OpenSquad(); }, 110f);
                bool on = m == map;
                UIFactory.Tint(b, on ? new Color32(m.R, m.G, m.B, 255) : new Color32(60, 56, 72, 255), on ? Color.white : new Color(1f, 1f, 1f, 0.7f));
                if (on) UIFactory.Highlight(b, Gold);
                var t = b.GetComponentInChildren<Text>();
                if (t != null) { t.supportRichText = true; t.resizeTextForBestFit = false; t.fontSize = 36; t.text = $"{m.Title}\n<size=22>{(best > 0 ? "best round " + best : "not played yet")}</size>"; }
            }
            UIFactory.Label(panel, $"{map.Blurb}  ·  {SurvivalModes.Name(_mode)}", 28, TextAnchor.MiddleCenter, 50f).color = UITheme.Current.subtle;
            UIFactory.Label(panel, "Who's holding the line with you?", 32, TextAnchor.MiddleCenter, 54f).color = UITheme.Current.subtle;
            var solo = Big(panel, "SOLO", "Just you and the dead", new Color32(120, 70, 50, 255), () => GoSquad(SquadKind.Solo));
            Big(panel, "AI SQUAD", "Three AI teammates: they cover you, revive you and grab better guns as the rounds climb",
                new Color32(60, 110, 160, 255), () => GoSquad(SquadKind.AISquad));
            bool online = PKRMatchService.Supported;
            Big(panel, "ONLINE CO-OP", online ? "Invite friends or find players on Game Center (2-4)" : "Needs an iPhone signed in to Game Center",
                new Color32(70, 140, 90, 255), FindOnline, online);
            var fill = UIFactory.Button(panel, _fillAI ? "EMPTY SEATS: AI TEAMMATES" : "EMPTY SEATS: LEAVE EMPTY", () => { _fillAI = !_fillAI; OpenSquad(); }, 90f);
            fill.GetComponentInChildren<Text>().fontSize = 30;
            _status = UIFactory.Label(panel, "", 30, TextAnchor.MiddleCenter, 60f);
            _status.color = new Color32(255, 214, 90, 255);
            UIFactory.Button(panel, "BACK", () => { var m = PKRMatchService.Instance; if (m != null && m.State != PKRMatchService.Phase.InGame) m.Leave(); Build(); });
            UIFactory.Select(solo);
        }

        void GoSquad(SquadKind kind)
        {
            GameSession.ZombieSquad = kind;
            Go(SceneIds.LastStand);
        }

        // ---- the weekly challenge --------------------------------------------------------------------------

        void OpenWeekly()
        {
            UIFactory.Clear(_root);
            var c = Career;
            var w = ZombieWeekly.Current(DateTime.UtcNow);
            var map = LastStandMap.Get(w.MapId);
            var panel = UIFactory.Panel(_root, new Vector2(980f, 1500f));
            var title = UIFactory.Label(panel, "WEEKLY CHALLENGE", 80, TextAnchor.MiddleCenter, 110f, title: true);
            title.color = Gold;
            UIFactory.Label(panel, $"{map.FullTitle}  ·  {SurvivalModes.Name(w.Mode)}", 40, TextAnchor.MiddleCenter, 60f, title: true).color = UITheme.Current.title;
            foreach (var m in w.Mutators)
            {
                var t = UIFactory.Label(panel, $"{MutatorSet.Name(m)}\n<size=26>{MutatorSet.Info(m)}</size>", 40, TextAnchor.MiddleCenter, 100f, title: true);
                t.supportRichText = true;
                t.color = new Color32(255, 150, 90, 255);
            }
            UIFactory.Label(panel, $"GOAL: reach round {w.GoalRound}", 40, TextAnchor.MiddleCenter, 60f, title: true).color = UITheme.Current.title;
            bool done = ZombieWeekly.Done(c, w);
            UIFactory.Label(panel, done ? "Done this week. You can keep playing for a better round." : $"Reward: +{w.RewardShards} Star Shards  ·  +{w.RewardXp} Survivor XP (once a week)",
                            30, TextAnchor.MiddleCenter, 50f).color = done ? (Color)Gold : UITheme.Current.subtle;
            int best = ZombieWeekly.BestThisWeek(c, w);
            UIFactory.Label(panel, (best > 0 ? $"Your best this week: round {best}  ·  " : "") + "New challenge every Monday  ·  " + ZombieWeekly.LeftText(DateTime.UtcNow),
                            28, TextAnchor.MiddleCenter, 46f).color = UITheme.Current.subtle;
            var solo = Big(panel, "PLAY SOLO", "Weekly runs keep their own best (no leaderboard)", new Color32(130, 100, 30, 255), () => GoWeekly(SquadKind.Solo));
            Big(panel, "WITH AI SQUAD", "Three AI teammates", new Color32(60, 110, 160, 255), () => GoWeekly(SquadKind.AISquad));
            UIFactory.Button(panel, "BACK", Build);
            UIFactory.Select(solo);
        }

        void GoWeekly(SquadKind kind)
        {
            var w = ZombieWeekly.Current(DateTime.UtcNow);
            GameSession.ZombieSquad = kind;
            GameSession.ZombieMap = w.MapId;
            _mode = w.Mode;
            Go(SceneIds.LastStand, weekly: true);
        }

        void FindOnline()
        {
            Career.lastMode = (int)_mode;
            GameSession.ZombieMode = _mode;
            GameSession.ZombieWeekly = false;
            var m = PKRMatchService.Ensure();
            m.MapId = GameSession.ZombieMap;
            m.FindPlayers(_mode, _fillAI);
            if (_status != null) _status.text = m.Status;
        }

        void Update()
        {
            var m = PKRMatchService.Instance;
            if (_status != null && m != null && m.State != PKRMatchService.Phase.Idle) _status.text = m.Status;
        }

        void Go(string scene, bool weekly = false)
        {
            if (scene != SceneIds.LastStand) GameSession.ZombieSquad = SquadKind.Solo;
            GameSession.ZombieWeekly = weekly;
            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                Debug.LogWarning($"[PKR] Zombie scene {scene} isn't in the build yet.");
                return;
            }
            if (!weekly) Career.lastMode = (int)_mode;
            if (Services.Save != null) Services.Save.MarkDirty();
            GameSession.Mode = SessionMode.Zombies;
            GameSession.ZombieMode = _mode;
            GameSession.EventScene = scene;
            if (Services.Scenes != null) Services.Scenes.Load(SceneIds.CharacterSelect);
        }
    }
}
