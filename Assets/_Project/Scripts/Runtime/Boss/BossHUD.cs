using PKR.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// Boss health bar under the timer: name, a bar that drains as the Warden takes hits, its phase, and a
    /// "CORE EXPOSED" cue while it can be hurt. Follows the high-contrast setting.
    /// </summary>
    public class BossHUD : MonoBehaviour
    {
        [SerializeField] ClockworkWarden boss;
        public int sortingOrder = 55;

        Image _fill, _back;
        Text _name, _cue;
        RectTransform _fillRect;
        float _shown = 1f;

        void Awake()
        {
            if (boss == null) boss = FindFirstObjectByType<ClockworkWarden>();
            Build();
        }

        void OnEnable()
        {
            EventBus<BossStateChanged>.Subscribe(OnState);
            EventBus<SettingsChanged>.Subscribe(OnSettings);
        }

        void OnDisable()
        {
            EventBus<BossStateChanged>.Unsubscribe(OnState);
            EventBus<SettingsChanged>.Unsubscribe(OnSettings);
        }

        void OnSettings(SettingsChanged e) => ApplyTheme();

        void OnState(BossStateChanged e)
        {
            if (_cue == null) return;
            _cue.text = e.action == WardenAction.Defeated ? "DEFEATED!"
                      : e.vulnerable ? "CORE EXPOSED! STRIKE NOW!"
                      : (e.phase == WardenPhase.One ? "" : $"PHASE {(int)e.phase}");
        }

        void Update()
        {
            if (boss == null || boss.Health == null || _fillRect == null) return;
            float target = boss.Health.MaxHealth > 0 ? Mathf.Clamp01((float)boss.Health.Health / boss.Health.MaxHealth) : 0f;
            _shown = Mathf.MoveTowards(_shown, target, Time.unscaledDeltaTime * 0.8f);
            _fillRect.anchorMax = new Vector2(_shown, 1f);
            _fill.enabled = _shown > 0.02f; // a sliced bar narrower than its borders draws badly
            bool exposed = boss.Brain != null && boss.Brain.IsVulnerable;
            _cue.enabled = !exposed || Mathf.FloorToInt(Time.unscaledTime * 6f) % 2 == 0;
        }

        void Build()
        {
            var safe = UIFactory.CreateCanvas(transform, "BossCanvas", sortingOrder, landscape: true, out _);
            var root = UIFactory.Rect("BossBar", safe);
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 1f);
            root.pivot = new Vector2(0.5f, 1f);
            root.anchoredPosition = new Vector2(0f, -100f);
            root.sizeDelta = new Vector2(900f, 120f);

            _name = Label(root, boss != null ? boss.DisplayName : "BOSS", 38, new Vector2(0f, 0f));
            var bar = UIFactory.Rect("Bar", root);
            bar.anchorMin = new Vector2(0f, 0.5f); bar.anchorMax = new Vector2(1f, 0.5f);
            bar.sizeDelta = new Vector2(0f, 30f);
            bar.anchoredPosition = new Vector2(0f, -8f);
            _back = bar.gameObject.AddComponent<Image>();
            _back.sprite = UISprites.RoundedRect; _back.type = Image.Type.Sliced; _back.raycastTarget = false;

            _fillRect = UIFactory.Rect("Fill", bar);
            _fillRect.anchorMin = Vector2.zero; _fillRect.anchorMax = Vector2.one;
            _fillRect.offsetMin = new Vector2(4f, 4f); _fillRect.offsetMax = new Vector2(-4f, -4f);
            _fill = _fillRect.gameObject.AddComponent<Image>();
            _fill.sprite = UISprites.RoundedRect; _fill.type = Image.Type.Sliced; _fill.raycastTarget = false;

            _cue = Label(root, "", 34, new Vector2(0f, -92f));
            ApplyTheme();
        }

        static Text Label(RectTransform parent, string text, int size, Vector2 pos)
        {
            var rt = UIFactory.Rect("Label", parent);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(900f, 44f);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = UISprites.Font;
            t.fontSize = size;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.UpperCenter;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.text = text;
            rt.gameObject.AddComponent<Outline>();
            return t;
        }

        void ApplyTheme()
        {
            bool hc = Services.Settings != null && Services.Settings.Data.highContrastUI;
            if (_back != null) _back.color = hc ? Color.black : new Color(0f, 0f, 0f, 0.55f);
            if (_fill != null) _fill.color = hc ? new Color(1f, 0.92f, 0.1f) : new Color32(230, 120, 50, 255);
            foreach (var t in new[] { _name, _cue })
                if (t != null) t.color = hc ? new Color(1f, 0.92f, 0.1f) : Color.white;
        }
    }
}
