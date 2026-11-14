using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// The party quests' banner (mode name and a live line: time, zone countdown, place) plus a short flash for the
    /// item you just picked up. Never takes touches.
    /// </summary>
    public class PartyHUD : MonoBehaviour
    {
        static PartyHUD _instance;
        Text _title, _line, _flash, _tip;
        CanvasGroup _flashGroup, _tipGroup;
        float _tipUntil = -1f;
        float _flashAt = -99f;
        System.Func<string> _lineSource;

        public static PartyHUD Show(string title, System.Func<string> line, bool belowMatchClock = false)
        {
            if (_instance == null)
            {
                var go = new GameObject("PartyHUD");
                _instance = go.AddComponent<PartyHUD>();
            }
            // The arena's match clock sits top-centre: drop the banner under it there.
            float top = belowMatchClock ? -84f : -14f;
            _instance._title.rectTransform.anchoredPosition = new Vector2(0f, top);
            _instance._line.rectTransform.anchoredPosition = new Vector2(0f, top - 52f);
            _instance._title.text = title;
            _instance._lineSource = line;
            _instance.gameObject.SetActive(true);
            return _instance;
        }

        public static void Hide()
        {
            if (_instance != null) _instance.gameObject.SetActive(false);
        }

        /// <summary>A how-to-play card low on the screen for a few seconds (first time in each quest).</summary>
        public static void Tip(string text, float seconds)
        {
            if (_instance == null) return;
            _instance._tip.text = text;
            _instance._tipUntil = Time.unscaledTime + seconds;
            _instance._tipGroup.gameObject.SetActive(true);
        }

        public static void Flash(string text)
        {
            if (_instance == null) return;
            _instance._flash.text = text;
            _instance._flashAt = Time.unscaledTime;
        }

        void Awake()
        {
            var safe = UIFactory.CreateCanvas(transform, "PartyCanvas", 40, landscape: true, out var canvas);
            var ray = canvas.GetComponent<GraphicRaycaster>();
            if (ray != null) ray.enabled = false;
            _title = Make(safe, "Title", 44, -14f, UITheme.Current.title);
            _line = Make(safe, "Line", 30, -66f, Color.white);
            var flash = UIFactory.Rect("Flash", safe);
            flash.anchorMin = flash.anchorMax = new Vector2(0.5f, 0.62f);
            flash.sizeDelta = new Vector2(900f, 90f);
            _flash = flash.gameObject.AddComponent<Text>();
            _flash.font = UISprites.HasDisplayFont ? UISprites.DisplayFont : UISprites.Font;
            _flash.fontSize = 56;
            _flash.alignment = TextAnchor.MiddleCenter;
            _flash.color = new Color32(255, 230, 110, 255);
            _flash.raycastTarget = false;
            _flash.gameObject.AddComponent<Outline>().effectColor = PlazaArt.Ink;
            _flashGroup = flash.gameObject.AddComponent<CanvasGroup>();
            _flashGroup.alpha = 0f;

            var tip = UIFactory.Rect("Tip", safe);
            tip.anchorMin = tip.anchorMax = new Vector2(0.5f, 0.3f);
            tip.sizeDelta = new Vector2(1180f, 170f);
            var bg = tip.gameObject.AddComponent<Image>();
            UISprites.ApplyPanelSprite(bg);
            bg.color = new Color(0.1f, 0.08f, 0.14f, 0.9f);
            bg.raycastTarget = false;
            var tt = UIFactory.Rect("Text", tip);
            tt.anchorMin = Vector2.zero; tt.anchorMax = Vector2.one;
            tt.offsetMin = new Vector2(24f, 10f); tt.offsetMax = new Vector2(-24f, -10f);
            _tip = tt.gameObject.AddComponent<Text>();
            _tip.font = UISprites.HasDisplayFont ? UISprites.DisplayFont : UISprites.Font;
            _tip.fontSize = 32;
            _tip.resizeTextForBestFit = true; _tip.resizeTextMinSize = 18; _tip.resizeTextMaxSize = 32;
            _tip.alignment = TextAnchor.MiddleCenter;
            _tip.color = Color.white;
            _tip.raycastTarget = false;
            _tipGroup = tip.gameObject.AddComponent<CanvasGroup>();
            _tipGroup.blocksRaycasts = false;
            tip.gameObject.SetActive(false);
        }

        Text Make(RectTransform parent, string name, int size, float y, Color color)
        {
            var rt = UIFactory.Rect(name, parent);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(1400f, size * 1.3f);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = UISprites.HasDisplayFont ? UISprites.DisplayFont : UISprites.Font;
            t.fontSize = size;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = color;
            t.raycastTarget = false;
            t.gameObject.AddComponent<Outline>().effectColor = PlazaArt.Ink;
            return t;
        }

        void Update()
        {
            if (_lineSource != null) _line.text = _lineSource();
            float age = Time.unscaledTime - _flashAt;
            // in fast (180 ms), hold, out faster (130 ms)
            float a = age < 0.18f ? PKR.Core.Ease.Out(age / 0.18f) : age < 1.2f ? 1f : 1f - PKR.Core.Ease.Out((age - 1.2f) / 0.13f);
            _flashGroup.alpha = Mathf.Clamp01(a);
            if (_tipGroup.gameObject.activeSelf)
            {
                float left = _tipUntil - Time.unscaledTime;
                _tipGroup.alpha = Mathf.Clamp01(left / 0.3f);
                if (left <= 0f) _tipGroup.gameObject.SetActive(false);
            }
        }

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
