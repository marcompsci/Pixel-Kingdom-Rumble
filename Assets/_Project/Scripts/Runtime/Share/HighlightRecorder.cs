using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PKR.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace PKR
{
    /// <summary>
    /// Highlight clips: while a gameplay scene is running, keeps the last few seconds as small frames (8 per second,
    /// 320 px wide) in a ring buffer. SHARE CLIP on a results screen turns them into an animated GIF (GifEncoder, on a
    /// worker thread) and opens the share sheet. Costs one small GPU readback per frame captured; nothing is written to
    /// disk until you share.
    /// </summary>
    public static class Highlights
    {
        /// <summary>Shares the last few seconds as an animated GIF with the text (just the text if there's no clip).</summary>
        public static void Share(string text)
        {
            var r = HighlightRecorder.Instance;
            if (r != null && r.HasClip) r.ShareClip(text);
            else ShareService.Share(text);
        }

        public static bool Available => HighlightRecorder.Instance != null && HighlightRecorder.Instance.HasClip;
    }

    public class HighlightRecorder : MonoBehaviour
    {
        public static HighlightRecorder Instance { get; private set; }

        public const int Width = 320;
        public const float Fps = 8f;
        public const float Seconds = 6f;
        static int MaxFrames => Mathf.RoundToInt(Fps * Seconds);

        readonly LinkedList<byte[]> _frames = new LinkedList<byte[]>();
        RenderTexture _full, _small;
        int _height;
        float _next;
        bool _pending, _sceneOk;

        /// <summary>Frames held right now (a clip needs at least a second's worth).</summary>
        public int FrameCount => _frames.Count;
        public bool HasClip => _frames.Count >= Fps;
        public bool Encoding { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (Instance != null || !SystemInfo.supportsAsyncGPUReadback) return;
            var go = new GameObject("HighlightRecorder");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<HighlightRecorder>();
        }

        void OnEnable() => SceneManager.activeSceneChanged += OnScene;
        void OnDisable() => SceneManager.activeSceneChanged -= OnScene;

        void Start() => OnScene(default, SceneManager.GetActiveScene());

        void OnScene(Scene _, Scene now)
        {
            _sceneOk = now.IsValid() && SceneIds.IsGameplay(now.name);
            if (_sceneOk) _frames.Clear(); // a new level starts a new clip
        }

        void OnDestroy()
        {
            if (_full != null) _full.Release();
            if (_small != null) _small.Release();
        }

        void Update()
        {
            if (!_sceneOk || _pending || Encoding) return;
            var state = Services.State;
            // Keep recording through the results screen's first moments so the K.O. is in the clip, but not while paused.
            if (state != null && state.State == GameState.Paused) return;
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 1f / Fps;
            StartCoroutine(Capture());
        }

        IEnumerator Capture()
        {
            _pending = true;
            yield return new WaitForEndOfFrame();
            int sw = Screen.width, sh = Screen.height;
            if (sw < 16 || sh < 16) { _pending = false; yield break; }
            int h = Mathf.Max(16, Mathf.RoundToInt(Width * (float)sh / sw)) & ~1;
            if (_full == null || _full.width != sw || _full.height != sh)
            {
                if (_full != null) _full.Release();
                _full = new RenderTexture(sw, sh, 0, RenderTextureFormat.ARGB32);
            }
            if (_small == null || _small.height != h)
            {
                if (_small != null) _small.Release();
                _small = new RenderTexture(Width, h, 0, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Bilinear };
                _frames.Clear();
            }
            _height = h;
            ScreenCapture.CaptureScreenshotIntoRenderTexture(_full);
            // On some graphics APIs the capture is upside down relative to texture order; flip while shrinking.
            bool flip = SystemInfo.graphicsUVStartsAtTop;
            if (flip) Graphics.Blit(_full, _small, new Vector2(1f, -1f), new Vector2(0f, 1f));
            else Graphics.Blit(_full, _small);
            AsyncGPUReadback.Request(_small, 0, TextureFormat.RGBA32, OnReadback);
        }

        void OnReadback(AsyncGPUReadbackRequest req)
        {
            _pending = false;
            if (req.hasError || !_sceneOk) return;
            var data = req.GetData<byte>();
            if (data.Length != Width * _height * 4) return;
            byte[] frame = _frames.Count >= MaxFrames ? _frames.First.Value : new byte[data.Length];
            if (_frames.Count >= MaxFrames) _frames.RemoveFirst();
            if (frame.Length != data.Length) frame = new byte[data.Length];
            data.CopyTo(frame);
            _frames.AddLast(frame);
        }

        /// <summary>Encodes the held frames to a GIF and opens the share sheet with it (and the text).</summary>
        public void ShareClip(string text)
        {
            if (Encoding || !HasClip) { ShareService.Share(text); return; }
            StartCoroutine(EncodeAndShare(text));
        }

        IEnumerator EncodeAndShare(string text)
        {
            Encoding = true;
            var frames = new List<byte[]>(_frames.Count);
            foreach (var f in _frames) frames.Add((byte[])f.Clone());
            int w = Width, h = _height;
            int delay = Mathf.RoundToInt(100f / Fps);
            string path = Path.Combine(Application.temporaryCachePath, "pkr_highlight.gif");
            var task = Task.Run(() =>
            {
                var gif = GifEncoder.Encode(frames, w, h, delay, bottomUp: true);
                File.WriteAllBytes(path, gif);
            });
            while (!task.IsCompleted) yield return null;
            Encoding = false;
            if (task.IsFaulted)
            {
                Debug.LogWarning("[PKR] Highlight clip failed: " + task.Exception?.GetBaseException().Message);
                ShareService.Share(text);
                yield break;
            }
            ShareService.Share(text, path);
        }
    }
}
