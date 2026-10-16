using System.Collections.Generic;
using System.Text;
using PKR.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// Zombie Survival HUD: hearts, the round number (big, top left, like a tally on the wall), points (top right),
    /// the gun and its ammo, perks, power-up timers, what USE would do, banners, and the game-over panel.
    /// </summary>
    public class ZombieHUD : MonoBehaviour
    {
        public int sortingOrder = 40;

        static readonly Color Blood = new Color32(255, 110, 60, 255);
        static readonly Color Gold = new Color32(255, 214, 90, 255);

        Text _hp, _round, _points, _left, _gun, _ammo, _perks, _powers, _prompt, _banner, _lanterns;
        RectTransform _safe, _heartRow;
        readonly List<Image> _hearts = new List<Image>();
        Sprite _heartFull, _heartEmpty;
        int _shownHp = -1, _shownMax = -1, _shownPoints = -1;
        float _bannerUntil, _pointsPulse;
        ZombieSurvival _game;
        bool _overShown;

        void Start()
        {
            Build();
            _game = ZombieSurvival.Current;
            if (_game != null)
            {
                _game.Banner += ShowBanner;
                _game.RunEnded += ShowGameOver;
            }
        }

        void OnDestroy()
        {
            if (_game != null)
            {
                _game.Banner -= ShowBanner;
                _game.RunEnded -= ShowGameOver;
            }
        }

        void ShowBanner(string text)
        {
            _banner.text = text;
            _banner.enabled = true;
            _bannerUntil = Time.unscaledTime + 2.2f;
        }

        void Update()
        {
            if (_game == null) { _game = ZombieSurvival.Current; if (_game == null) return; }
            var run = _game.Run;
            var hp = _game.PlayerHealth;
            if (hp != null) UpdateHealth(hp.Health, hp.MaxHealth);

            _round.text = run.Round > 0 ? run.Round.ToString() : "";
            if (run.Points != _shownPoints)
            {
                if (_shownPoints >= 0 && run.Points > _shownPoints) _pointsPulse = 1f;
                _shownPoints = run.Points;
                _points.text = $"{run.Points}";
            }
            _pointsPulse = Mathf.MoveTowards(_pointsPulse, 0f, Time.unscaledDeltaTime * 4f);
            _points.transform.localScale = Vector3.one * (1f + _pointsPulse * 0.15f);
            _points.color = run.DoublePoints ? Gold : Color.white;

            _left.text = _game.GameOver ? ""
                : _game.Intermission ? (run.Round == 0 ? $"GET READY  {Mathf.CeilToInt(_game.IntermissionLeft)}" : $"NEXT ROUND IN {Mathf.CeilToInt(_game.IntermissionLeft)}")
                : $"ZOMBIES LEFT {_game.ZombiesLeft}";
            if (_game.Mode != SurvivalMode.Endless && !_game.GameOver)
                _left.text = (_game.Mode == SurvivalMode.Raid ? $"RAID {run.Round}/{SurvivalModes.RaidRounds}" : "HARDCORE") + "   " + _left.text;

            var gun = _game.Gunner;
            var slot = gun != null ? gun.Current : null;
            if (slot != null)
            {
                _gun.text = slot.spec.name + (gun.Slots.Count > 1 ? "   (USE TO SWAP)" : "");
                _gun.color = slot.spec.upgraded ? new Color32(200, 150, 255, 255) : Color.white;
                _ammo.text = gun.Reloading ? "RELOADING..." : $"{slot.mag} / {slot.reserve}";
                _ammo.color = slot.mag == 0 && slot.reserve == 0 ? Blood : Color.white;
            }
            else { _gun.text = ""; _ammo.text = ""; }

            var sb = new StringBuilder();
            foreach (var p in run.Perks) sb.Append(sb.Length > 0 ? "  " : "").Append(ZombieRules.PerkName(p));
            if (run.PerkCount > 0) sb.Append($"  ({run.PerkCount}/{run.PerkLimit})");
            _perks.text = sb.ToString();
            sb.Clear();
            if (_game.InstaKillLeft > 0f) sb.Append($"SPOOKY STRIKE {Mathf.CeilToInt(_game.InstaKillLeft)}   ");
            if (_game.DoublePointsLeft > 0f) sb.Append($"TWIN TREATS {Mathf.CeilToInt(_game.DoublePointsLeft)}");
            _powers.text = sb.ToString();
            _prompt.text = _game.GameOver ? "" : _game.Prompt;
            _lanterns.text = _game.LanternsLit > 0 && _game.LanternsLit < _game.LanternsTotal ? $"LANTERNS {_game.LanternsLit}/{_game.LanternsTotal}" : "";

            if (_banner.enabled && Time.unscaledTime > _bannerUntil) _banner.enabled = false;
        }

        void UpdateHealth(int cur, int max)
        {
            if (_heartFull == null || _heartEmpty == null)
            {
                var b = new StringBuilder("HP ");
                for (int i = 0; i < max; i++) b.Append(i < cur ? '#' : '-');
                _hp.text = b.ToString();
                return;
            }
            if (cur == _shownHp && max == _shownMax)
            {
                float k = 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime);
                foreach (var h in _hearts) h.transform.localScale = Vector3.Lerp(h.transform.localScale, Vector3.one, k);
                return;
            }
            _hp.text = "HP";
            while (_hearts.Count < max)
            {
                var rt = new GameObject("Heart", typeof(RectTransform)).GetComponent<RectTransform>();
                rt.SetParent(_heartRow, false);
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
                rt.sizeDelta = new Vector2(52f, 48f);
                rt.anchoredPosition = new Vector2(_hearts.Count * 58f, 0f);
                var img = rt.gameObject.AddComponent<Image>();
                img.raycastTarget = false;
                _hearts.Add(img);
            }
            for (int i = 0; i < _hearts.Count; i++)
            {
                _hearts[i].gameObject.SetActive(i < max);
                _hearts[i].sprite = i < cur ? _heartFull : _heartEmpty;
                if (_shownHp > cur && i >= cur && i < _shownHp) _hearts[i].transform.localScale = Vector3.one * 1.6f;
            }
            _shownHp = cur;
            _shownMax = max;
        }

        // ---- construction -------------------------------------------------------------------------------------------

        void Build()
        {
            var canvasGo = new GameObject("ZombieHUDCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;

            _safe = new GameObject("SafeArea", typeof(RectTransform)).GetComponent<RectTransform>();
            _safe.SetParent(canvasGo.transform, false);
            _safe.anchorMin = Vector2.zero; _safe.anchorMax = Vector2.one;
            _safe.offsetMin = _safe.offsetMax = Vector2.zero;
            _safe.gameObject.AddComponent<SafeAreaFitter>();

            _hp = Label("HP", new Vector2(0f, 1f), new Vector2(40f, -30f), TextAnchor.UpperLeft, 44);
            _heartFull = Resources.Load<Sprite>("PKR/UI/ui_heart");
            _heartEmpty = Resources.Load<Sprite>("PKR/UI/ui_heart_empty");
            _heartRow = new GameObject("Hearts", typeof(RectTransform)).GetComponent<RectTransform>();
            _heartRow.SetParent(_safe, false);
            _heartRow.anchorMin = _heartRow.anchorMax = _heartRow.pivot = new Vector2(0f, 1f);
            _heartRow.anchoredPosition = new Vector2(120f, -28f);
            _heartRow.sizeDelta = new Vector2(600f, 50f);

            _round = Label("Round", new Vector2(0f, 1f), new Vector2(40f, -90f), TextAnchor.UpperLeft, 120);
            _round.color = Blood;
            _left = Label("Left", new Vector2(0f, 1f), new Vector2(40f, -230f), TextAnchor.UpperLeft, 34);
            _lanterns = Label("Lanterns", new Vector2(0f, 1f), new Vector2(40f, -280f), TextAnchor.UpperLeft, 30);
            _lanterns.color = Gold;
            _points = Label("Points", new Vector2(1f, 1f), new Vector2(-310f, -30f), TextAnchor.UpperRight, 64);
            _points.rectTransform.pivot = new Vector2(1f, 1f);
            _powers = Label("Powers", new Vector2(1f, 1f), new Vector2(-310f, -110f), TextAnchor.UpperRight, 34);
            _powers.rectTransform.pivot = new Vector2(1f, 1f);
            _powers.color = Gold;
            _perks = Label("Perks", new Vector2(1f, 1f), new Vector2(-310f, -160f), TextAnchor.UpperRight, 28);
            _perks.rectTransform.pivot = new Vector2(1f, 1f);
            _perks.color = new Color32(150, 230, 255, 255);
            _gun = Label("Gun", new Vector2(0.5f, 0f), new Vector2(0f, 120f), TextAnchor.LowerCenter, 34);
            _ammo = Label("Ammo", new Vector2(0.5f, 0f), new Vector2(0f, 70f), TextAnchor.LowerCenter, 44);
            _prompt = Label("Prompt", new Vector2(0.5f, 0.32f), Vector2.zero, TextAnchor.MiddleCenter, 40);
            _prompt.color = Gold;
            _banner = Label("Banner", new Vector2(0.5f, 0.66f), Vector2.zero, TextAnchor.MiddleCenter, 72);
            _banner.rectTransform.sizeDelta = new Vector2(1600f, 300f);
            _banner.enabled = false;
            foreach (var t in new[] { _round, _points, _banner })
                if (UISprites.HasDisplayFont) { t.font = UISprites.DisplayFont; t.fontStyle = FontStyle.Normal; }
        }

        Text Label(string name, Vector2 anchor, Vector2 pos, TextAnchor align, int size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(_safe, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(anchor.x, anchor.y >= 1f ? 1f : anchor.y <= 0f ? 0f : 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(1000f, size * 1.4f);
            var t = go.AddComponent<Text>();
            t.font = UISprites.Font;
            t.fontSize = size;
            t.fontStyle = FontStyle.Bold;
            t.alignment = align;
            t.color = Color.white;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var o = go.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.85f);
            o.effectDistance = new Vector2(3f, -3f);
            return t;
        }

        // ---- game over ----------------------------------------------------------------------------------------------

        void ShowGameOver()
        {
            if (_overShown || _game == null) return;
            _overShown = true;
            var run = _game.Run;
            var save = Services.Save != null ? Services.Save.Data : null;
            var safe = UIFactory.CreateCanvas(transform, "GameOverCanvas", sortingOrder + 20, landscape: true, out _);
            UIFactory.Dim(safe);
            var panel = UIFactory.Panel(safe, new Vector2(1180f, 980f));
            var title = UIFactory.Label(panel, run.Won ? "RAID COMPLETE!" : "THE HORDE GOT YOU", 80, TextAnchor.MiddleCenter, 110f, title: true);
            title.color = run.Won ? Gold : Blood;
            int shown = run.Won ? run.Round : run.Round;
            UIFactory.Label(panel, $"{SurvivalModes.Name(_game.Mode)}  ·  ROUND {shown}" + (_game.WraithBeaten ? "  ·  WRAITH SLAIN" : ""), 52,
                            TextAnchor.MiddleCenter, 74f, title: true);
            UIFactory.Label(panel, $"{run.Kills} zombies  ·  {run.TotalEarned} points  ·  {_game.CachesEarned} supply cache{(_game.CachesEarned == 1 ? "" : "s")}",
                            36, TextAnchor.MiddleCenter, 56f);
            var career = save != null ? save.zombieCareer : null;
            int best = career != null ? career.Best(ZombieSurvival.MapId, _game.Mode) : 0;
            var bestLabel = UIFactory.Label(panel, (_game.NewBest ? "NEW BEST!  " : "") + $"Best {SurvivalModes.Name(_game.Mode)} round: {best}", 36,
                                            TextAnchor.MiddleCenter, 54f);
            bestLabel.color = _game.NewBest ? Gold : UITheme.Current.subtle;
            if (_game.ShardReward > 0)
                UIFactory.Label(panel, $"+{_game.ShardReward} Star Shards   ·   +{_game.XpEarned} Survivor XP", 38, TextAnchor.MiddleCenter, 56f).color = Gold;
            if (career != null)
            {
                int rank = SurvivorRank.RankFor(career.xp);
                string rk = _game.RanksReached.Count > 0 ? $"RANK UP!  SURVIVOR RANK {rank} · {SurvivorRank.Title(rank)}"
                          : $"SURVIVOR RANK {rank}  ·  {Mathf.RoundToInt(SurvivorRank.Progress01(career.xp) * 100f)}% to the next";
                UIFactory.Label(panel, rk, 32, TextAnchor.MiddleCenter, 50f).color = _game.RanksReached.Count > 0 ? (Color)Gold : UITheme.Current.subtle;
            }

            var buttons = UIFactory.Rect("Buttons", panel);
            var ble = buttons.gameObject.AddComponent<LayoutElement>();
            ble.preferredHeight = ble.minHeight = UIFactory.RowHeight;
            ble.flexibleHeight = 0f;
            var h = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 24f;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = true; h.childForceExpandHeight = true;
            var again = UIFactory.Button(buttons, "PLAY AGAIN", () => { if (Services.Scenes != null) Services.Scenes.ReloadCurrent(); });
            int round = run.Round, kills = run.Kills;
            bool won = run.Won;
            UIFactory.Button(buttons, Highlights.Available ? "SHARE CLIP" : "SHARE", () => Highlights.Share(
                (won ? $"I beat the Raid ({kills} zombies down)" : $"I survived {round} round{(round == 1 ? "" : "s")} of zombies ({kills} down)") +
                " in Pixel Kingdom Rumble! " + ShareCard.Link));
            bool zombiesMenu = GameSession.Mode == SessionMode.Zombies;
            UIFactory.Button(buttons, zombiesMenu ? "ZOMBIES" : "HALLOWEEN", () =>
            {
                GameSession.ReopenEventMenu = true;
                if (Services.Scenes != null) Services.Scenes.Load(SceneIds.MainMenu);
            });
            UIFactory.Select(again);
        }
    }
}
