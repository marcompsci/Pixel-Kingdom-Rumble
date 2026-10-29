using System;
using System.Collections;
using PKR.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// Plays a StoryBeat as comic panels over the game: a framed picture (built from simple shapes and the hero's
    /// sprite), a caption box with the speaker's name, NEXT to advance and SKIP to jump to the end. The game is frozen
    /// (time scale 0) while it plays. Not shown in batch mode, so automated tests never wait on a tap.
    /// </summary>
    public class CutscenePlayer : MonoBehaviour
    {
        public static bool Allowed => !Application.isBatchMode;
        public static bool IsPlaying { get; private set; }

        StoryBeat _beat;
        Action _onDone;
        int _index;
        RectTransform _root, _art;
        Text _caption, _speaker, _counter;
        Image _bg;
        float _prevScale = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => IsPlaying = false;

        public static CutscenePlayer Play(StoryBeat beat, Action onDone)
        {
            if (beat.panels == null || beat.panels.Length == 0) { onDone?.Invoke(); return null; }
            var go = new GameObject("Cutscene_" + beat.id);
            var p = go.AddComponent<CutscenePlayer>();
            p.Begin(beat, onDone);
            return p;
        }

        void Begin(StoryBeat beat, Action onDone)
        {
            _beat = beat;
            _onDone = onDone;
            _index = 0;
            IsPlaying = true;
            _prevScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            Build();
            Show();
            UiMotion.ModalIn(_root);
        }

        void Build()
        {
            var safe = UIFactory.CreateCanvas(transform, "CutsceneCanvas", 400, landscape: true, out _);
            _root = UIFactory.Rect("Cutscene", safe);
            UIFactory.Stretch(_root);
            var dim = UIFactory.Dim(_root);
            dim.color = new Color(0f, 0f, 0f, 0.85f);

            // The comic frame (pixel panel, like every menu): picture on top, caption underneath.
            var theme = UITheme.Current;
            var frame = UIFactory.Rect("Frame", _root);
            frame.anchorMin = frame.anchorMax = new Vector2(0.5f, 0.55f);
            frame.sizeDelta = new Vector2(1500f, 760f);
            var border = frame.gameObject.AddComponent<Image>();
            UISprites.ApplyPanelSprite(border);
            border.color = theme.panelEdge;
            var inner = UIFactory.Rect("Inner", frame);
            UIFactory.Stretch(inner);
            inner.offsetMin = new Vector2(14f, 14f); inner.offsetMax = new Vector2(-14f, -14f);
            var innerImg = inner.gameObject.AddComponent<Image>();
            UISprites.ApplyPanelSprite(innerImg);
            innerImg.color = theme.panel;

            var pic = UIFactory.Rect("Picture", inner);
            pic.anchorMin = new Vector2(0f, 0.36f); pic.anchorMax = Vector2.one;
            pic.offsetMin = new Vector2(24f, 0f); pic.offsetMax = new Vector2(-24f, -24f);
            _bg = pic.gameObject.AddComponent<Image>();
            pic.gameObject.AddComponent<RectMask2D>();
            var picEdge = pic.gameObject.AddComponent<Outline>();
            picEdge.effectColor = new Color(0f, 0f, 0f, 0.6f);
            picEdge.effectDistance = new Vector2(4f, -4f);
            _art = UIFactory.Rect("Art", pic);
            UIFactory.Stretch(_art);

            var cap = UIFactory.Rect("Caption", inner);
            cap.anchorMin = Vector2.zero; cap.anchorMax = new Vector2(1f, 0.36f);
            cap.offsetMin = new Vector2(40f, 18f); cap.offsetMax = new Vector2(-40f, -12f);
            _speaker = MakeText(cap, "Speaker", 40, TextAnchor.UpperLeft, theme.title, display: true);
            _speaker.rectTransform.anchorMin = new Vector2(0f, 0.7f);
            _caption = MakeText(cap, "Text", 40, TextAnchor.MiddleLeft, theme.text);
            _caption.rectTransform.anchorMax = new Vector2(1f, 0.78f);
            _counter = MakeText(cap, "Counter", 30, TextAnchor.UpperRight, theme.subtle, display: true);
            _counter.rectTransform.anchorMin = new Vector2(0.75f, 0.7f);

            var row = UIFactory.Rect("Buttons", _root);
            row.anchorMin = row.anchorMax = new Vector2(0.5f, 0.1f);
            row.sizeDelta = new Vector2(1100f, UIFactory.RowHeight);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 30f;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = true; h.childForceExpandHeight = true;
            UIFactory.Button(row, "SKIP", Finish);
            var next = UIFactory.Button(row, "NEXT", Next);
            UIFactory.Select(next);

            // Tapping the picture also advances.
            var tap = pic.gameObject.AddComponent<Button>();
            tap.transition = Selectable.Transition.None;
            tap.onClick.AddListener(Next);
        }

        static Text MakeText(RectTransform parent, string name, int size, TextAnchor align, Color color, bool display = false)
        {
            var rt = UIFactory.Rect(name, parent);
            UIFactory.Stretch(rt);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = display ? UISprites.DisplayFont : UISprites.Font;
            t.fontSize = size;
            t.fontStyle = display && UISprites.HasDisplayFont ? FontStyle.Normal : FontStyle.Bold;
            if (display)
            {
                var sh = rt.gameObject.AddComponent<Shadow>();
                sh.effectColor = new Color(0f, 0f, 0f, 0.85f);
                sh.effectDistance = new Vector2(4f, -4f);
            }
            t.alignment = align;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public void Next()
        {
            if (_index >= _beat.panels.Length - 1) { Finish(); return; }
            _index++;
            Show();
        }

        void Show()
        {
            var panel = _beat.panels[_index];
            _speaker.text = string.IsNullOrEmpty(panel.speaker) ? "" : panel.speaker;
            _caption.text = panel.text;
            // Narration reads a little softer than dialogue.
            bool narration = string.IsNullOrEmpty(panel.speaker);
            _caption.fontStyle = FontStyle.Bold;
            _caption.color = narration ? UITheme.Current.subtle : UITheme.Current.text;
            _counter.text = $"{_index + 1} / {_beat.panels.Length}";
            DrawArt(panel.art);
            // the next page comes in from the right (reading direction) and fades up: 160 ms, ease-out
            UiMotion.Enter(_art, PKR.Core.Ease.Quick, 1f, new Vector2(28f, 0f));
            Sfx.Play(Sfx.Click, 0.6f, 0f);
        }

        public void Finish()
        {
            if (!IsPlaying) return;
            IsPlaying = false;
            if (Time.timeScale == 0f) Time.timeScale = _prevScale;
            var done = _onDone;
            _onDone = null;
            Destroy(gameObject);
            done?.Invoke();
        }

        void OnDestroy()
        {
            if (IsPlaying) { IsPlaying = false; if (Time.timeScale == 0f) Time.timeScale = _prevScale; }
        }

        // ---- Picture: simple shape compositions per art key ---------------------------------------------

        void DrawArt(string key)
        {
            UIFactory.Clear(_art);
            // Pixel-art picture for the beat (Resources/PKR/Story); the hero stands in front on the outdoor ones.
            var picture = Resources.Load<Sprite>("PKR/Story/story_" + (string.IsNullOrEmpty(key) ? "hero" : key));
            if (picture != null)
            {
                _bg.sprite = picture;
                _bg.type = Image.Type.Simple;
                _bg.preserveAspect = false;
                _bg.color = Color.white;
                switch (key)
                {
                    case "hero": case "isles": case "dawn": case "peaks": case "starfall": case null: case "":
                        Hero(new Vector2(0.5f, 0.42f), 360f);
                        break;
                }
                return;
            }
            _bg.sprite = UISprites.RoundedRect;
            _bg.type = Image.Type.Sliced;
            switch (key)
            {
                case "isles":
                    _bg.color = new Color32(150, 200, 255, 255);
                    Blob(new Vector2(0.25f, 0.45f), 380f, 120f, new Color32(96, 176, 84, 255));
                    Blob(new Vector2(0.7f, 0.6f), 300f, 100f, new Color32(126, 214, 84, 255));
                    Blob(new Vector2(0.5f, 0.2f), 240f, 240f, new Color32(236, 186, 74, 255)); // the engine's gear below
                    Blob(new Vector2(0.5f, 0.2f), 120f, 120f, new Color32(150, 110, 60, 255));
                    break;
                case "warden":
                case "sovereign":
                    bool sov = key == "sovereign";
                    _bg.color = sov ? new Color32(26, 16, 36, 255) : new Color32(48, 36, 30, 255);
                    if (sov)
                    {
                        var realm = new Color32[] { new Color32(232, 96, 48, 255), new Color32(120, 196, 240, 255), new Color32(48, 170, 160, 255), new Color32(214, 84, 150, 255) };
                        for (int i = 0; i < 4; i++) Blob(new Vector2(0.38f + (i % 2) * 0.24f, 0.36f + (i / 2) * 0.32f), 260f, 260f, realm[i]);
                    }
                    else Blob(new Vector2(0.5f, 0.52f), 520f, 520f, new Color32(150, 120, 80, 255));
                    Blob(new Vector2(0.5f, 0.52f), 200f, 200f, new Color32(40, 30, 30, 255));
                    Blob(new Vector2(0.5f, 0.52f), 120f, 120f, new Color32(255, 220, 90, 255)); // the core
                    break;
                case "ledger":
                    _bg.color = new Color32(60, 36, 72, 255);
                    Box(new Vector2(0.5f, 0.5f), 520f, 360f, new Color32(236, 220, 180, 255));
                    for (int i = 0; i < 5; i++) Box(new Vector2(0.5f, 0.66f - i * 0.08f), 420f, 14f, new Color32(120, 100, 90, 255));
                    break;
                case "peaks":
                    _bg.color = new Color32(170, 210, 240, 255);
                    Blob(new Vector2(0.3f, 0.1f), 600f, 400f, new Color32(236, 250, 255, 255));
                    Blob(new Vector2(0.75f, 0.05f), 500f, 320f, new Color32(200, 230, 250, 255));
                    Hero(new Vector2(0.5f, 0.42f), 260f);
                    break;
                case "mainspring":
                    _bg.color = new Color32(20, 30, 60, 255);
                    for (int i = 0; i < 4; i++) Blob(new Vector2(0.5f, 0.5f), 520f - i * 110f, 520f - i * 110f, i % 2 == 0 ? new Color32(255, 214, 80, 255) : new Color32(20, 30, 60, 255));
                    break;
                case "realms":
                    _bg.color = new Color32(20, 14, 30, 255);
                    var c = new Color32[] { new Color32(232, 96, 48, 255), new Color32(120, 196, 240, 255), new Color32(48, 170, 160, 255), new Color32(214, 84, 150, 255) };
                    for (int i = 0; i < 4; i++) Box(new Vector2(0.27f + (i % 2) * 0.46f, 0.27f + (i / 2) * 0.46f), 600f, 200f, c[i]);
                    Blob(new Vector2(0.5f, 0.5f), 150f, 150f, new Color32(255, 220, 90, 255));
                    break;
                case "dawn":
                    _bg.color = new Color32(255, 196, 140, 255);
                    Blob(new Vector2(0.5f, 0.1f), 600f, 600f, new Color32(255, 236, 160, 255));
                    Blob(new Vector2(0.3f, 0.5f), 380f, 110f, new Color32(96, 176, 84, 255));
                    Hero(new Vector2(0.62f, 0.5f), 240f);
                    break;
                default: // hero
                    _bg.color = new Color32(46, 168, 178, 255);
                    Blob(new Vector2(0.5f, 0.45f), 560f, 560f, new Color32(255, 255, 255, 40));
                    Hero(new Vector2(0.5f, 0.45f), 380f);
                    break;
            }
        }

        Image Blob(Vector2 anchor, float w, float h, Color color) => Shape(UISprites.Circle, anchor, w, h, color);
        Image Box(Vector2 anchor, float w, float h, Color color) => Shape(UISprites.RoundedRect, anchor, w, h, color);

        Image Shape(Sprite sprite, Vector2 anchor, float w, float h, Color color)
        {
            var rt = UIFactory.Rect("Shape", _art);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(w, h);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            if (sprite == UISprites.RoundedRect) img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>The player's own hero (whoever they picked), from the live sprite.</summary>
        void Hero(Vector2 anchor, float size)
        {
            Sprite s = null;
            Color tint = Color.white;
            var marker = PlayerMarker.Current;
            if (marker != null)
            {
                var sr = marker.GetComponentInChildren<SpriteRenderer>();
                if (sr != null) { s = sr.sprite; tint = sr.color; }
            }
            if (s == null) { Blob(anchor, size * 0.6f, size, new Color32(255, 112, 96, 255)); return; }
            var img = Shape(s, anchor, size, size, tint);
            img.preserveAspect = true;
        }
    }
}
