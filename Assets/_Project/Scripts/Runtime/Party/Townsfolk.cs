using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// Kingdom Plaza townsfolk: stroll back and forth along the street (no collider, can't be hit) and, when the hero
    /// walks up, say a short tip in a speech bubble. Purely decoration and guidance.
    /// </summary>
    public class Townsfolk : MonoBehaviour
    {
        SpriteRenderer _sr;
        Text _bubbleText;
        GameObject _bubble;
        Transform _hero;
        float _minX, _maxX, _speed, _ground, _pauseUntil, _animT;
        int _variant, _dir = 1, _line;
        string[] _lines;

        public static Townsfolk Spawn(Transform parent, int variant, float x, float ground, float range, Transform hero, Material mat, string[] lines)
        {
            var go = new GameObject("Townsfolk_" + variant);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, ground, 0f);
            var t = go.AddComponent<Townsfolk>();
            t._variant = variant;
            t._minX = x - range; t._maxX = x + range;
            t._ground = ground;
            t._speed = 0.9f + (variant % 3) * 0.25f;
            t._hero = hero;
            t._lines = lines;
            t._line = variant % Mathf.Max(1, lines.Length);
            t._sr = go.AddComponent<SpriteRenderer>();
            t._sr.sprite = PlazaArt.Townsfolk(variant, 0);
            t._sr.sortingOrder = -6; // in front of the buildings, behind the hero
            if (mat != null) t._sr.sharedMaterial = mat;
            t.BuildBubble();
            return t;
        }

        void BuildBubble()
        {
            _bubble = new GameObject("Bubble");
            _bubble.transform.SetParent(transform, false);
            _bubble.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            var canvas = _bubble.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 55;
            var rt = (RectTransform)_bubble.transform;
            rt.sizeDelta = new Vector2(620f, 120f);
            rt.localScale = Vector3.one * 0.0085f;
            var bg = _bubble.AddComponent<Image>();
            UISprites.ApplyPanelSprite(bg);
            bg.color = new Color(1f, 0.98f, 0.92f, 0.95f);
            bg.raycastTarget = false;
            var tr = UIFactory.Rect("Text", rt);
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(14f, 6f); tr.offsetMax = new Vector2(-14f, -6f);
            _bubbleText = tr.gameObject.AddComponent<Text>();
            _bubbleText.font = UISprites.Font;
            _bubbleText.fontSize = 34;
            _bubbleText.resizeTextForBestFit = true; _bubbleText.resizeTextMinSize = 16; _bubbleText.resizeTextMaxSize = 34;
            _bubbleText.alignment = TextAnchor.MiddleCenter;
            _bubbleText.color = PlazaArt.Ink;
            _bubbleText.raycastTarget = false;
            _bubble.SetActive(false);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            bool near = _hero != null && Mathf.Abs(_hero.position.x - transform.position.x) < 1.8f && Mathf.Abs(_hero.position.y - _ground) < 3f;
            if (near && !_bubble.activeSelf && _lines != null && _lines.Length > 0)
            {
                _bubbleText.text = _lines[_line % _lines.Length];
                _line++;
                _bubble.SetActive(true);
            }
            else if (!near && _bubble.activeSelf) _bubble.SetActive(false);

            if (near)
            {
                // stop and face the hero while talking
                _sr.flipX = _hero.position.x < transform.position.x;
                _sr.sprite = PlazaArt.Townsfolk(_variant, 0);
                return;
            }
            if (Time.time < _pauseUntil) return;
            var p = transform.position;
            p.x += _dir * _speed * dt;
            if (p.x > _maxX || p.x < _minX)
            {
                p.x = Mathf.Clamp(p.x, _minX, _maxX);
                _dir = -_dir;
                _pauseUntil = Time.time + 1f + (_variant % 3) * 0.6f; // look around before turning back
            }
            transform.position = p;
            _sr.flipX = _dir < 0;
            _animT += dt * _speed * 4f;
            _sr.sprite = PlazaArt.Townsfolk(_variant, SettingsService.ReducedMotion ? 0 : (int)_animT);
        }
    }
}
