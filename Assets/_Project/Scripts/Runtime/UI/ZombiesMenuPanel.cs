using System;
using PKR.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// The ZOMBIES menu (main menu, all year): Survivor Rank, the mode picker (Endless / Raid / Hardcore) and the three
    /// zombie games — Zombie Land (the open-city campaign), Last Stand: Hollow Block and Survival: The Manor — plus the
    /// rank unlocks list. Players of other mobile zombie modes were angriest when zombies were taken away or only came
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
            var panel = UIFactory.Panel(_root, new Vector2(980f, 1460f));
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
            string zlSub = zl == null || zl.seed == 0 ? "A city of the dead · 14 missions · drive anything · hunt the First Turned"
                         : zl.bossDefeated ? "Cleared! Roam free, or start a new mystery."
                         : $"Shards {zl.Shards}/{ZLCatalog.ShardsNeeded} · missions {zl.completed.Count}/{ZLCatalog.Missions.Length} · scrap {zl.scrap}";
            var zlButton = Big(panel, "ZOMBIE LAND", zlSub, new Color32(110, 60, 150, 255), () => Go(SceneIds.ZombieLand));

            int lsBest = c.Best(LastStandRules.MapId, _mode);
            string lsSub = (lsBest > 0 ? $"{SurvivalModes.Name(_mode)} best: round {lsBest}" : "NEW!  Round-based on a New Hollow city block")
                           + " · doors · perks · box · hound rounds";
            Big(panel, "LAST STAND: HOLLOW BLOCK", lsSub, new Color32(170, 70, 40, 255), () => Go(SceneIds.LastStand),
                Application.CanStreamedLevelBeLoaded(SceneIds.LastStand));

            int manorBest = c.Best("manor", _mode);
            if (_mode == SurvivalMode.Endless && save != null) manorBest = Mathf.Max(manorBest, save.zombieBestRound);
            string mSub = (manorBest > 0 ? $"{SurvivalModes.Name(_mode)} best: round {manorBest}" : "Hold the manor windows")
                          + " · wall guns · crate · forge · lantern secret";
            Big(panel, "SURVIVAL: THE MANOR", mSub, new Color32(200, 70, 50, 255), () => Go(SceneIds.ZombieSurvival));

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
            if (c.Has(LastStandRules.HiddenBossSecret)) records += "  ·  Hollow King slain";
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

        void Go(string scene)
        {
            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                Debug.LogWarning($"[PKR] Zombie scene {scene} isn't in the build yet.");
                return;
            }
            Career.lastMode = (int)_mode;
            if (Services.Save != null) Services.Save.MarkDirty();
            GameSession.Mode = SessionMode.Zombies;
            GameSession.ZombieMode = _mode;
            GameSession.EventScene = scene;
            if (Services.Scenes != null) Services.Scenes.Load(SceneIds.CharacterSelect);
        }
    }
}
