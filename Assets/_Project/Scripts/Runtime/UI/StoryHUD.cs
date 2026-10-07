using PKR.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// In-level HUD built at runtime: HP, Star Shards, secrets and timer along the top of the safe area,
    /// plus a center banner for checkpoints, secrets and level clear. Follows the high-contrast setting.
    /// The pause button lives in PauseMenu and the end screen in LevelCompleteScreen.
    /// </summary>
    public class StoryHUD : MonoBehaviour
    {
        public int sortingOrder = 50;

        Text _hp, _shards, _time, _secrets, _banner;
        float _bannerUntil;
        readonly System.Collections.Generic.Queue<string> _codexQueue = new System.Collections.Generic.Queue<string>();
        Damageable _playerHealth;

        void Awake() => Build();

        void OnEnable()
        {
            EventBus<CheckpointActivated>.Subscribe(OnCheckpoint);
            EventBus<SecretFound>.Subscribe(OnSecret);
            EventBus<CodexEntryUnlocked>.Subscribe(OnCodex);
            EventBus<SettingsChanged>.Subscribe(OnSettings);
        }

        void OnDisable()
        {
            EventBus<CheckpointActivated>.Unsubscribe(OnCheckpoint);
            EventBus<SecretFound>.Unsubscribe(OnSecret);
            EventBus<CodexEntryUnlocked>.Unsubscribe(OnCodex);
            EventBus<SettingsChanged>.Unsubscribe(OnSettings);
        }

        void OnCheckpoint(CheckpointActivated e) => ShowBanner("CHECKPOINT", 1.5f);
        void OnSecret(SecretFound e) => ShowBanner($"SECRET FOUND  {e.found}/{e.total}", 2f);
        void OnCodex(CodexEntryUnlocked e)
        {
            // Never cover a message that is still showing: queue it and show it next.
            _codexQueue.Enqueue($"NEW IN CODEX: {(e.displayName ?? e.id ?? "").ToUpperInvariant()}");
            if (!BannerBusy) ShowBanner(_codexQueue.Dequeue(), CodexBannerSeconds);
        }
        const float CodexBannerSeconds = 1.8f;
        bool BannerBusy => _banner.enabled && Time.unscaledTime <= _bannerUntil;
        void OnSettings(SettingsChanged e) => ApplyTheme();

        /// <summary>Kept for callers; formatting lives in Core so it is unit-tested.</summary>
        public static string FormatTime(float seconds) => ResultsMath.FormatTime(seconds);

        void ShowBanner(string text, float seconds)
        {
            _banner.text = text;
            _banner.enabled = true;
            _bannerUntil = float.IsInfinity(seconds) ? float.PositiveInfinity : Time.unscaledTime + seconds;
        }

        void Update()
        {
            var flow = LevelFlowController.Current;
            if (flow == null || flow.Run == null) return;
            if (_playerHealth == null) _playerHealth = flow.PlayerHealth;

            var run = flow.Run;
            if (_playerHealth != null) _hp.text = "HP " + Bar(_playerHealth.Health, _playerHealth.MaxHealth);
            _shards.text = $"SHARDS {run.Shards}/{flow.TotalShards}";
            _secrets.text = flow.TotalSecrets > 0 ? $"SECRETS {run.SecretsFound}/{flow.TotalSecrets}" : "";
            _time.text = FormatTime(run.ElapsedSeconds);

            if (_banner.enabled && Time.unscaledTime > _bannerUntil) _banner.enabled = false;
            if (!BannerBusy && _codexQueue.Count > 0) ShowBanner(_codexQueue.Dequeue(), CodexBannerSeconds);
        }

        static string Bar(int cur, int max)
        {
            // Filled/empty blocks read clearly at small sizes; '#' and '-' render in the built-in font.
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < max; i++) sb.Append(i < cur ? '#' : '-');
            return sb.ToString();
        }

        // ---- Construction ---------------------------------------------------------------------------

        void Build()
        {
            var canvasGo = new GameObject("HUDCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;

            var safe = new GameObject("SafeArea", typeof(RectTransform)).GetComponent<RectTransform>();
            safe.SetParent(canvasGo.transform, false);
            safe.anchorMin = Vector2.zero; safe.anchorMax = Vector2.one;
            safe.offsetMin = safe.offsetMax = Vector2.zero;
            safe.gameObject.AddComponent<SafeAreaFitter>();

            _hp = Label(safe, "HP", new Vector2(0f, 1f), new Vector2(40f, -30f), TextAnchor.UpperLeft, 44);
            _shards = Label(safe, "Shards", new Vector2(0f, 1f), new Vector2(40f, -90f), TextAnchor.UpperLeft, 40);
            _secrets = Label(safe, "Secrets", new Vector2(0f, 1f), new Vector2(40f, -145f), TextAnchor.UpperLeft, 34);
            _time = Label(safe, "Time", new Vector2(0.5f, 1f), new Vector2(0f, -30f), TextAnchor.UpperCenter, 44);
            _banner = Label(safe, "Banner", new Vector2(0.5f, 0.62f), Vector2.zero, TextAnchor.MiddleCenter, 64);
            _banner.rectTransform.sizeDelta = new Vector2(1600f, 400f);
            _banner.enabled = false;
            ApplyTheme();
        }

        static Text Label(RectTransform parent, string name, Vector2 anchor, Vector2 pos, TextAnchor align, int size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(anchor.x, anchor.y >= 1f ? 1f : 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(900f, 80f);
            var t = go.AddComponent<Text>();
            t.font = UISprites.Font;
            t.fontSize = size;
            t.fontStyle = FontStyle.Bold;
            t.alignment = align;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            go.AddComponent<Outline>();
            return t;
        }

        void ApplyTheme()
        {
            bool hc = Services.Settings != null && Services.Settings.Data.highContrastUI;
            foreach (var t in new[] { _hp, _shards, _secrets, _time, _banner })
            {
                if (t == null) continue;
                t.color = hc ? new Color(1f, 0.92f, 0.1f) : Color.white;
                var o = t.GetComponent<Outline>();
                o.effectColor = hc ? Color.black : new Color(0f, 0f, 0f, 0.6f);
                o.effectDistance = hc ? new Vector2(4f, -4f) : new Vector2(2f, -2f);
            }
        }
    }
}
