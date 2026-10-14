using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using PKR.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// Photo mode (from the pause menu): the game stays frozen, every HUD and menu hides, and a small bar offers
    /// SNAP / FRAME / BACK. SNAP captures the screen (with an optional game-name banner), saves a PNG and opens the
    /// share sheet with it.
    /// </summary>
    public class PhotoMode : MonoBehaviour
    {
        public static bool Active { get; private set; }

        readonly List<Canvas> _hidden = new List<Canvas>();
        Canvas _canvas;
        RectTransform _bar, _banner;
        Image _flash;
        Action _onExit;
        bool _frame = true, _busy;

        /// <summary>Starts photo mode; onExit runs when the player taps BACK.</summary>
        public static PhotoMode Begin(Action onExit)
        {
            var go = new GameObject("PhotoMode");
            var pm = go.AddComponent<PhotoMode>();
            pm._onExit = onExit;
            pm.Open();
            return pm;
        }

        void Open()
        {
            Active = true;
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude))
                if (c.enabled && c.isRootCanvas) { c.enabled = false; _hidden.Add(c); }
            var safe = UIFactory.CreateCanvas(transform, "PhotoCanvas", 500, landscape: true, out _canvas);

            _banner = UIFactory.Rect("Banner", safe);
            _banner.anchorMin = _banner.anchorMax = new Vector2(0f, 0f);
            _banner.pivot = new Vector2(0f, 0f);
            _banner.anchoredPosition = new Vector2(36f, 30f);
            _banner.sizeDelta = new Vector2(620f, 96f);
            var bannerBg = _banner.gameObject.AddComponent<Image>();
            UISprites.ApplyPanelSprite(bannerBg);
            bannerBg.color = new Color(0.1f, 0.12f, 0.24f, 0.9f);
            var title = UIFactory.Rect("Title", _banner);
            UIFactory.Stretch(title);
            var t = title.gameObject.AddComponent<Text>();
            t.font = UISprites.DisplayFont;
            t.fontSize = 40;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = UITheme.Normal.title;
            t.text = "PIXEL KINGDOM RUMBLE";
            t.raycastTarget = false;

            _bar = UIFactory.Rect("Bar", safe);
            _bar.anchorMin = _bar.anchorMax = new Vector2(0.5f, 1f);
            _bar.pivot = new Vector2(0.5f, 1f);
            _bar.anchoredPosition = new Vector2(0f, -24f);
            _bar.sizeDelta = new Vector2(1000f, UIFactory.RowHeight);
            var h = _bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 20f;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = true; h.childForceExpandHeight = true;
            UIFactory.Button(_bar, "SNAP", Snap);
            Button frameBtn = null;
            frameBtn = UIFactory.Button(_bar, "FRAME: ON", () =>
            {
                _frame = !_frame;
                _banner.gameObject.SetActive(_frame);
                frameBtn.GetComponentInChildren<Text>().text = _frame ? "FRAME: ON" : "FRAME: OFF";
            });
            UIFactory.Button(_bar, "BACK", Close);

            var flash = UIFactory.Rect("Flash", safe);
            flash.anchorMin = Vector2.zero; flash.anchorMax = Vector2.one;
            flash.offsetMin = new Vector2(-400f, -400f); flash.offsetMax = new Vector2(400f, 400f);
            _flash = flash.gameObject.AddComponent<Image>();
            _flash.color = new Color(1f, 1f, 1f, 0f);
            _flash.raycastTarget = false;
        }

        void Snap()
        {
            if (!_busy) StartCoroutine(SnapRoutine());
        }

        IEnumerator SnapRoutine()
        {
            _busy = true;
            _bar.gameObject.SetActive(false);
            yield return new WaitForEndOfFrame();
            Texture2D shot = null;
            string path = null;
            try
            {
                shot = ScreenCapture.CaptureScreenshotAsTexture();
                string dir = Path.Combine(Application.persistentDataPath, "Photos");
                Directory.CreateDirectory(dir);
                path = Path.Combine(dir, "pkr_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
                File.WriteAllBytes(path, shot.EncodeToPNG());
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[PKR Photo] capture failed: " + ex.Message);
                path = null;
            }
            finally
            {
                if (shot != null) Destroy(shot);
            }
            _bar.gameObject.SetActive(true);
            Sfx.Play(Sfx.Click, 1f, 0f);
            if (Services.Haptics != null) Services.Haptics.Play(HapticStrength.Medium);
            for (float a = 0.9f; a > 0f; a -= Time.unscaledDeltaTime * 3f)
            {
                _flash.color = new Color(1f, 1f, 1f, a);
                yield return null;
            }
            _flash.color = new Color(1f, 1f, 1f, 0f);
            if (path != null) ShareService.Share("Pixel Kingdom Rumble \U0001F3AE " + ShareCard.Link, path);
            _busy = false;
        }

        void Close()
        {
            foreach (var c in _hidden) if (c != null) c.enabled = true;
            _hidden.Clear();
            Active = false;
            var cb = _onExit;
            Destroy(gameObject);
            cb?.Invoke();
        }

        void OnDestroy()
        {
            if (!Active) return;
            foreach (var c in _hidden) if (c != null) c.enabled = true;
            Active = false;
        }
    }
}
