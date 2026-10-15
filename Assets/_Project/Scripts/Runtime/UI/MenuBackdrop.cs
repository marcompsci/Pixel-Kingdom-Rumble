using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// The menus' backdrop: an evening Sunspire meadow (Resources/PKR/Menu) that scrolls slowly sideways behind every
    /// menu screen, with drifting clouds and a soft dark wash so the panels stay easy to read. Replaces the flat
    /// camera colour the menu scenes used to show. Created once and kept across scenes; hidden during gameplay.
    /// </summary>
    public class MenuBackdrop : MonoBehaviour
    {
        public static MenuBackdrop Instance { get; private set; }
        const float ScrollSpeed = 0.006f, CloudSpeed = 0.014f;

        Canvas _canvas;
        RawImage _land, _clouds;

        public bool Showing => _canvas != null && _canvas.enabled;

        public static bool IsMenuScene(string name) =>
            name == SceneIds.MainMenu || name == SceneIds.CharacterSelect || name == SceneIds.LevelSelect ||
            name == SceneIds.VersusSelect || name == SceneIds.MissionBoard;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (Instance != null) return;
            var land = Resources.Load<Texture2D>("PKR/Menu/menu_bg");
            if (land == null) return;
            var go = new GameObject("MenuBackdrop");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<MenuBackdrop>();
            Instance.Build(land, Resources.Load<Texture2D>("PKR/Menu/menu_clouds"));
        }

        void Build(Texture2D land, Texture2D clouds)
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = -100; // under every menu canvas
            var root = (RectTransform)transform;

            _land = Layer(root, "Land", land, 0f, 1f);
            if (clouds != null) { _clouds = Layer(root, "Clouds", clouds, 0.55f, 0.95f); _clouds.color = new Color(1f, 1f, 1f, 0.7f); }

            var wash = UIFactory.Rect("Wash", root);
            UIFactory.Stretch(wash);
            var img = wash.gameObject.AddComponent<Image>();
            img.color = new Color(0.08f, 0.09f, 0.2f, 0.35f);
            img.raycastTarget = false;
            Refresh(SceneManager.GetActiveScene());
        }

        static RawImage Layer(RectTransform root, string name, Texture2D tex, float yMin, float yMax)
        {
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Point;
            var rt = UIFactory.Rect(name, root);
            rt.anchorMin = new Vector2(0f, yMin);
            rt.anchorMax = new Vector2(1f, yMax);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var raw = rt.gameObject.AddComponent<RawImage>();
            raw.texture = tex;
            raw.raycastTarget = false;
            return raw;
        }

        void OnEnable() => SceneManager.activeSceneChanged += OnScene;
        void OnDisable() => SceneManager.activeSceneChanged -= OnScene;
        void OnScene(Scene _, Scene now) => Refresh(now);

        void Refresh(Scene s)
        {
            if (_canvas != null) _canvas.enabled = s.IsValid() && IsMenuScene(s.name);
        }

        void Update()
        {
            if (!Showing) return;
            float t = Time.unscaledTime;
            FitWidth(_land, t * ScrollSpeed);
            if (_clouds != null) FitWidth(_clouds, t * CloudSpeed);
        }

        /// <summary>Keeps the pixels square: show as much of the strip's width as the screen's aspect allows.</summary>
        static void FitWidth(RawImage raw, float offset)
        {
            var rt = raw.rectTransform;
            var tex = raw.texture;
            if (tex == null || rt.rect.height < 1f) return;
            float shownW = rt.rect.width / (rt.rect.height * tex.width / tex.height);
            raw.uvRect = new Rect(offset % 1f, 0f, shownW, 1f);
        }
    }
}
