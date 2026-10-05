using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// One on-screen action button (Jump / Attack / Special / Dodge).
    /// Reports Held and a one-shot press flag the input router consumes each frame.
    /// Edit mode: drag to reposition within its parent (the right half of the safe area).
    /// </summary>
    public class TouchActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        public string controlId;
        public Image fill, ring;
        public Text label;
        public bool editMode;

        public bool Held { get; private set; }
        public event Action<Vector2> HomeMoved;

        const int NoPointer = int.MinValue;
        int _pointerId = NoPointer;
        bool _pressedFlag;
        float _scale = 1f;

        /// <summary>True once per physical press.</summary>
        public bool ConsumePressed()
        {
            bool p = _pressedFlag;
            _pressedFlag = false;
            return p;
        }

        public void SetHome(Vector2 normalized, float scale)
        {
            var rt = (RectTransform)transform;
            rt.anchorMin = rt.anchorMax = normalized;
            rt.anchoredPosition = Vector2.zero;
            _scale = scale;
            rt.localScale = Vector3.one * scale;
        }

        public void ApplyTheme(TouchTheme.Palette p)
        {
            if (fill) fill.color = Held ? p.pressedFill : p.fill;
            if (ring) ring.color = p.ring;
            if (label) label.color = p.label;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (_pointerId != NoPointer) return;
            _pointerId = e.pointerId;
            if (editMode) return;

            Held = true;
            _pressedFlag = true;
            transform.localScale = Vector3.one * (_scale * 0.92f);
            ApplyTheme(TouchTheme.Current);
            if (Services.Haptics != null) Services.Haptics.Play(HapticStrength.Light);
        }

        public void OnDrag(PointerEventData e)
        {
            if (!editMode || e.pointerId != _pointerId) return;
            var parent = transform.parent as RectTransform;
            if (parent == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, e.position, null, out var local)) return;
            var rt = (RectTransform)transform;
            rt.anchorMin = rt.anchorMax = Rect.PointToNormalized(parent.rect, local);
            rt.anchoredPosition = Vector2.zero;
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId != _pointerId) return;
            if (editMode) HomeMoved?.Invoke(((RectTransform)transform).anchorMin);
            Release();
        }

        void OnDisable() => Release();

        void Release()
        {
            _pointerId = NoPointer;
            Held = false;
            transform.localScale = Vector3.one * _scale;
            ApplyTheme(TouchTheme.Current);
        }
    }
}
