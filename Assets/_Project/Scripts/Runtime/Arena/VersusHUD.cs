using PKR.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// Versus HUD (landscape): a health bar per fighter across the top (yours on the left, filling from the
    /// center out like classic fighting games), round-win pips under each, the round clock in the middle, a big
    /// center banner for ROUND / FIGHT / K.O., and the match result panel with Rematch / New Fighters / Menu.
    /// </summary>
    public class VersusHUD : MonoBehaviour
    {
        [SerializeField] VersusController match;
        public int sortingOrder = 60;

        RectTransform _safe, _results;
        Text _clock, _banner, _name0, _name1, _pips0, _pips1;
        RectTransform _fill0, _fill1, _lag0, _lag1;
        float _bannerUntil, _shown0 = 1f, _shown1 = 1f;

        public string BannerText => _banner != null && _banner.enabled ? _banner.text : "";
        public bool ResultsShowing => _results != null && _results.gameObject.activeSelf;

        void Awake()
        {
            if (match == null) match = FindAnyObjectByType<VersusController>();
            _safe = UIFactory.CreateCanvas(transform, "VersusHUDCanvas", sortingOrder, landscape: true, out _);

            Bar(out _fill0, out _lag0, out _name0, out _pips0, left: true);
            Bar(out _fill1, out _lag1, out _name1, out _pips1, left: false);

            _clock = UIFactory.Label(_safe, "60", 72, TextAnchor.MiddleCenter, 90f, title: true);
            Anchor(_clock.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(160f, 96f));
            _clock.gameObject.AddComponent<Outline>().effectDistance = new Vector2(3f, -3f);

            _banner = UIFactory.Label(_safe, "", 130, TextAnchor.MiddleCenter, 180f, title: true);
            Anchor(_banner.rectTransform, new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(1600f, 190f));
            _banner.gameObject.AddComponent<Outline>().effectDistance = new Vector2(5f, -5f);
            _banner.enabled = false;

            _results = UIFactory.Rect("Results", _safe);
            UIFactory.Stretch(_results);
            _results.gameObject.SetActive(false);
        }

        void OnEnable()
        {
            EventBus<VersusAnnounce>.Subscribe(OnAnnounce);
            EventBus<VersusMatchEnded>.Subscribe(OnEnded);
        }

        void OnDisable()
        {
            EventBus<VersusAnnounce>.Unsubscribe(OnAnnounce);
            EventBus<VersusMatchEnded>.Unsubscribe(OnEnded);
        }

        void OnAnnounce(VersusAnnounce e)
        {
            _banner.text = e.text;
            _banner.enabled = true;
            _bannerUntil = Time.unscaledTime + Mathf.Max(0.2f, e.seconds);
        }

        void Update()
        {
            if (_banner.enabled && Time.unscaledTime > _bannerUntil) _banner.enabled = false;
            if (match == null || match.Match == null || match.Player == null || match.Opponent == null) return;

            _name0.text = match.Player.name;
            _name1.text = match.Opponent.name;
            _clock.text = match.Match.Phase == VersusPhase.Waiting ? $"{(int)VersusMatch.DefaultRoundSeconds}" : match.Match.ClockSeconds.ToString();
            _pips0.text = Pips(match.Match.Wins(0), match.Match.RoundsToWin);
            _pips1.text = Pips(match.Match.Wins(1), match.Match.RoundsToWin);

            float h0 = Share(match.Player.health), h1 = Share(match.Opponent.health);
            SetFill(_fill0, h0, left: true);
            SetFill(_fill1, h1, left: false);
            // The pale "lag" bar drains slowly behind the real one so big hits read clearly.
            _shown0 = Mathf.MoveTowards(Mathf.Max(_shown0, h0), h0, Time.unscaledDeltaTime * 0.6f);
            _shown1 = Mathf.MoveTowards(Mathf.Max(_shown1, h1), h1, Time.unscaledDeltaTime * 0.6f);
            SetFill(_lag0, _shown0, left: true);
            SetFill(_lag1, _shown1, left: false);
        }

        static float Share(Damageable d) => d != null && d.MaxHealth > 0 ? Mathf.Clamp01((float)d.Health / d.MaxHealth) : 0f;

        static string Pips(int wins, int needed)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < needed; i++) sb.Append(i < wins ? "# " : "- ");
            return sb.ToString().TrimEnd();
        }

        // ---- Widgets -------------------------------------------------------------------------------

        void Bar(out RectTransform fill, out RectTransform lag, out Text name, out Text pips, bool left)
        {
            float side = left ? -1f : 1f;
            var frame = UIFactory.Rect(left ? "Bar_P1" : "Bar_CPU", _safe);
            Anchor(frame, new Vector2(0.5f, 1f), new Vector2(side * 470f, -30f), new Vector2(760f, 56f));
            var bg = frame.gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.55f);
            bg.raycastTarget = false;

            lag = Fill(frame, "Lag", new Color(1f, 1f, 1f, 0.55f));
            fill = Fill(frame, "Fill", left ? VersusController.PlayerColor : VersusController.OpponentColor);

            name = UIFactory.Label(_safe, "", 40, left ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight, 50f);
            Anchor(name.rectTransform, new Vector2(0.5f, 1f), new Vector2(side * 470f, -96f), new Vector2(760f, 50f));
            name.gameObject.AddComponent<Outline>();
            pips = UIFactory.Label(_safe, "", 40, left ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft, 50f, title: true);
            Anchor(pips.rectTransform, new Vector2(0.5f, 1f), new Vector2(side * 470f, -96f), new Vector2(760f, 50f));
            pips.gameObject.AddComponent<Outline>();
        }

        static RectTransform Fill(RectTransform frame, string name, Color color)
        {
            var rt = UIFactory.Rect(name, frame);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(6f, 6f);
            rt.offsetMax = new Vector2(-6f, -6f);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return rt;
        }

        /// <summary>Player 1's bar empties toward the left edge... i.e. it stays anchored at the center (right end).</summary>
        static void SetFill(RectTransform rt, float share, bool left)
        {
            share = Mathf.Clamp01(share);
            if (left) { rt.anchorMin = new Vector2(1f - share, 0f); rt.anchorMax = new Vector2(1f, 1f); }
            else { rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(share, 1f); }
        }

        static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, anchor.y >= 1f ? 1f : 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        // ---- Results -------------------------------------------------------------------------------

        void OnEnded(VersusMatchEnded e)
        {
            _banner.enabled = false;
            _results.gameObject.SetActive(true);
            UIFactory.Clear(_results);
            UIFactory.Dim(_results);
            var panel = UIFactory.Panel(_results, new Vector2(1100f, 860f));
            string head = e.winner == 0 ? "YOU WIN!" : e.winner == 1 ? "YOU LOSE" : "DRAW";
            UIFactory.Label(panel, head, 96, TextAnchor.MiddleCenter, 130f, title: true);
            UIFactory.Label(panel, $"{e.playerName}  {e.playerRounds} - {e.opponentRounds}  {e.opponentName}", 44,
                            TextAnchor.MiddleCenter, 70f);
            if (e.starShardReward > 0)
                UIFactory.Label(panel, $"+{e.starShardReward} STAR SHARDS", 44, TextAnchor.MiddleCenter, 70f).color = UITheme.Current.accent;
            var again = UIFactory.Button(panel, "REMATCH", Rematch);
            UIFactory.Button(panel, "NEW FIGHTERS", () => Load(SceneIds.VersusSelect));
            UIFactory.Button(panel, "MAIN MENU", () => Load(SceneIds.MainMenu));
            UIFactory.Select(again);
        }

        public void Rematch()
        {
            _results.gameObject.SetActive(false);
            if (match == null || match.Player == null) return;
            match.StartMatch(match.Player.def.id, match.Opponent.def.id, GameSession.VersusBotLevel);
        }

        static void Load(string scene)
        {
            if (Services.Scenes != null && Application.CanStreamedLevelBeLoaded(scene)) Services.Scenes.Load(scene);
            else if (Services.Scenes != null) Services.Scenes.Load(SceneIds.MainMenu);
        }
    }
}
