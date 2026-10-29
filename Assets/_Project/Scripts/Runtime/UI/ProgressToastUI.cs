using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// Always-on overlay (survives scene loads) that shows progress toasts one at a time at the top. A toast drops in
    /// from the top edge by its own height while fading in (220 ms, strong ease-out), holds, then goes back up the way
    /// it came, faster (150 ms). With Reduced motion it only fades.
    /// </summary>
    public class ProgressToastUI : MonoBehaviour
    {
        static ProgressToastUI _instance;
        readonly Queue<ProgressToast> _queue = new Queue<ProgressToast>();
        RectTransform _box;
        Text _title, _detail;
        Image _bg;
        CanvasGroup _group;
        float _shownAt = -99f;
        const float Seconds = 2.6f, In = 0.22f, Out = 0.15f, Rest = -30f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (_instance != null) return;
            var go = new GameObject("ProgressToastUI");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<ProgressToastUI>();
        }

        void Awake()
        {
            var safe = UIFactory.CreateCanvas(transform, "ToastCanvas", 300, landscape: true, out var canvas);
            var raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null) raycaster.enabled = false; // never blocks touches
            _box = UIFactory.Rect("Toast", safe);
            _box.anchorMin = _box.anchorMax = new Vector2(0.5f, 1f);
            _box.pivot = new Vector2(0.5f, 1f);
            _box.sizeDelta = new Vector2(820f, 150f);
            _bg = _box.gameObject.AddComponent<Image>();
            UISprites.ApplyPanelSprite(_bg);
            _bg.color = new Color(0.1f, 0.1f, 0.16f, 0.92f);
            _bg.raycastTarget = false;
            _group = _box.gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _title = MakeText("Title", 52, new Vector2(0f, -18f));
            _detail = MakeText("Detail", 32, new Vector2(0f, -86f));
            _box.gameObject.SetActive(false);
        }

        Text MakeText(string name, int size, Vector2 pos)
        {
            var rt = UIFactory.Rect(name, _box);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(780f, size * 1.4f);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = size > 40 && UISprites.HasDisplayFont ? UISprites.DisplayFont : UISprites.Font;
            t.fontSize = size;
            t.alignment = TextAnchor.MiddleCenter;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
        }

        void OnEnable() => EventBus<ProgressToast>.Subscribe(OnToast);
        void OnDisable() => EventBus<ProgressToast>.Unsubscribe(OnToast);

        void OnToast(ProgressToast t) => _queue.Enqueue(t);

        void Update()
        {
            float now = Time.unscaledTime;
            bool showing = now - _shownAt < Seconds;
            if (!showing && _queue.Count > 0)
            {
                var t = _queue.Dequeue();
                _title.text = t.title;
                _title.color = t.color;
                _detail.text = t.detail;
                _shownAt = now;
                showing = true;
            }
            _box.gameObject.SetActive(showing);
            if (showing)
            {
                float a = now - _shownAt;
                // 1 = fully in place; 0 = above the top edge and transparent
                float k = a < In ? PKR.Core.Ease.Out(a / In)
                        : a > Seconds - Out ? 1f - PKR.Core.Ease.Out((a - (Seconds - Out)) / Out) : 1f;
                float travel = SettingsService.ReducedMotion ? 0f : _box.sizeDelta.y;
                _box.anchoredPosition = new Vector2(0f, Rest + travel * (1f - k));
                _group.alpha = k;
            }
        }
    }
}
