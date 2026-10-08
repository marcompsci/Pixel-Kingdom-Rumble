using System.Collections.Generic;
using PKR.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// Arena HUD: one card per fighter along the bottom (name, stocks or score, Guard Pips, EXPOSED),
    /// mode/timer at the top center, and a KO banner. Built when a match starts.
    /// </summary>
    public class ArenaHUD : MonoBehaviour
    {
        [SerializeField] ArenaMatchController match;
        public int sortingOrder = 60;

        RectTransform _safe, _cardsRow;
        Text _top, _banner;
        float _bannerUntil;
        readonly List<Text> _cardTexts = new List<Text>();

        void Awake()
        {
            if (match == null) match = FindAnyObjectByType<ArenaMatchController>();
            _safe = UIFactory.CreateCanvas(transform, "ArenaHUDCanvas", sortingOrder, landscape: true, out _);

            _top = UIFactory.Label(_safe, "", 48, TextAnchor.UpperCenter, 70f);
            Anchor(_top.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(900f, 70f));
            _banner = UIFactory.Label(_safe, "", 110, TextAnchor.MiddleCenter, 160f, title: true);
            Anchor(_banner.rectTransform, new Vector2(0.5f, 0.68f), Vector2.zero, new Vector2(1400f, 160f));
            _banner.gameObject.AddComponent<Outline>().effectDistance = new Vector2(4f, -4f);
            _banner.enabled = false;

            _cardsRow = UIFactory.Rect("Cards", _safe);
            _cardsRow.anchorMin = new Vector2(0.18f, 0f);
            _cardsRow.anchorMax = new Vector2(0.82f, 0f);
            _cardsRow.pivot = new Vector2(0.5f, 0f);
            _cardsRow.sizeDelta = new Vector2(0f, 120f);
            _cardsRow.anchoredPosition = new Vector2(0f, 12f);
            var h = _cardsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 14f;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = true; h.childForceExpandHeight = true;
        }

        static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, anchor.y >= 1f ? 1f : 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        void OnEnable()
        {
            EventBus<ArenaMatchStarted>.Subscribe(OnStarted);
            EventBus<ArenaKO>.Subscribe(OnKO);
        }

        void OnDisable()
        {
            EventBus<ArenaMatchStarted>.Unsubscribe(OnStarted);
            EventBus<ArenaKO>.Unsubscribe(OnKO);
        }

        void OnStarted(ArenaMatchStarted e)
        {
            UIFactory.Clear(_cardsRow);
            _cardTexts.Clear();
            var p = UITheme.Current;
            foreach (var f in match.Fighters)
            {
                var card = UIFactory.Rect("Card_" + f.slot, _cardsRow);
                var bg = card.gameObject.AddComponent<Image>();
                bg.sprite = UISprites.RoundedRect;
                bg.type = Image.Type.Sliced;
                bg.color = new Color(p.panel.r, p.panel.g, p.panel.b, 0.85f);
                bg.raycastTarget = false;
                var strip = UIFactory.Rect("Strip", card);
                strip.anchorMin = new Vector2(0f, 0f); strip.anchorMax = new Vector2(0f, 1f);
                strip.pivot = new Vector2(0f, 0.5f); strip.sizeDelta = new Vector2(14f, -20f);
                strip.anchoredPosition = new Vector2(10f, 0f);
                var stripImg = strip.gameObject.AddComponent<Image>();
                stripImg.color = f.color;
                stripImg.raycastTarget = false;
                var label = UIFactory.Label(card, "", 30, TextAnchor.MiddleLeft, 120f);
                var lrt = label.rectTransform;
                lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
                lrt.offsetMin = new Vector2(34f, 6f); lrt.offsetMax = new Vector2(-8f, -6f);
                _cardTexts.Add(label);
            }
            ShowBanner(e.mode == MatchMode.Training ? "TRAINING" : "FIGHT!", 1.2f);
        }

        void OnKO(ArenaKO e)
        {
            string who = match.Fighters.Count > e.victimSlot ? match.Fighters[e.victimSlot].name : "";
            ShowBanner(e.eliminated ? $"{who} OUT!" : "KO!", 1.1f);
        }

        void ShowBanner(string text, float seconds)
        {
            _banner.text = text;
            _banner.enabled = true;
            _bannerUntil = Time.unscaledTime + seconds;
        }

        void Update()
        {
            if (_banner.enabled && Time.unscaledTime > _bannerUntil) _banner.enabled = false;
            var m = match != null ? match.Match : null;
            if (m == null || !match.IsLive)
            {
                _top.text = "";
                if (match != null && !match.IsLive) _cardsRow.gameObject.SetActive(m != null);
                return;
            }
            _cardsRow.gameObject.SetActive(true);

            _top.text = m.Mode == MatchMode.Timed ? ResultsMath.FormatTime(m.TimeRemaining)
                      : m.Mode == MatchMode.Training ? "TRAINING" : "STOCK";

            for (int i = 0; i < _cardTexts.Count && i < match.Fighters.Count; i++)
            {
                var f = match.Fighters[i];
                var rec = m.Get(i);
                string status = m.Mode == MatchMode.Stock ? (rec.IsEliminated ? "OUT" : $"x{rec.stocks}")
                              : m.Mode == MatchMode.Timed ? (rec.score >= 0 ? $"+{rec.score}" : rec.score.ToString())
                              : $"KOs {rec.knockouts}";
                string pips = "";
                if (f.health != null && f.health.Pips != null)
                    pips = f.health.IsExposed ? "EXPOSED!" : new string('#', f.health.Pips.Current) + new string('-', f.health.Pips.MaxPips - f.health.Pips.Current);
                _cardTexts[i].text = $"{f.name}  {status}\nGUARD {pips}";
                _cardTexts[i].color = f.health != null && f.health.IsExposed ? new Color(1f, 0.4f, 0.4f) : UITheme.Current.text;
            }
        }
    }
}
