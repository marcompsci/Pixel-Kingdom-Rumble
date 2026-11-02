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
        Text _title, _line, _flash;
        CanvasGroup _flashGroup;
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
        }

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
