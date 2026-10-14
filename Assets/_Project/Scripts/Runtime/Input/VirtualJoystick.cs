using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// Left-side analog stick. Sits on a touch zone covering the left half of the safe area.
    /// Floating mode: the stick appears wherever the thumb lands, and returns home on release.
    /// Fixed mode: only touches that start on the stick count.
    /// Edit mode: dragging moves the stick's home position (saved to settings by TouchControlsUI).
    /// Multi-touch safe: tracks a single pointerId, so buttons on the right work at the same time.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform zone;
        public RectTransform stickBase;
        public RectTransform knob;
        public Image baseFill, baseRing, knobImage;
        [Tooltip("Optional wedge that lights up the pressed direction (rotated toward the stick).")]
        public Image direction;
        [Tooltip("Generated art: tint the base/knob with the art colors instead of the plain-circle colors.")]
        public bool artStyle;
        [Tooltip("D-pad mode: snap to 8 directions at full strength.")]
        public bool eightWay;

        [Range(0f, 0.5f)] public float deadzone = 0.15f;
        public bool floating = true;
        public bool editMode;

        public Vector2 Value { get; private set; }
        public bool IsActive => _pointerId != NoPointer;

        /// <summary>Fired in edit mode when the user drops the stick at a new home (normalized 0..1 within zone).</summary>
        public event Action<Vector2> HomeMoved;

        const int NoPointer = int.MinValue;
        int _pointerId = NoPointer;

        public void SetHome(Vector2 normalized, float scale)
        {
            stickBase.anchorMin = stickBase.anchorMax = normalized;
            stickBase.anchoredPosition = Vector2.zero;
            stickBase.localScale = Vector3.one * scale;
        }

        public void ApplyTheme(TouchTheme.Palette p)
        {
            if (artStyle)
            {
                if (baseFill) baseFill.color = p.artFill;
                if (knobImage) knobImage.color = IsActive ? p.artPressed : p.artFill;
                if (baseRing) baseRing.color = new Color(p.ring.r, p.ring.g, p.ring.b, IsActive ? 0.5f : 0f);
            }
            else
            {
                if (baseFill) baseFill.color = p.fill;
                if (baseRing) baseRing.color = p.ring;
                if (knobImage) knobImage.color = IsActive ? p.pressedFill : p.ring;
            }
            UpdateDirection();
        }

        /// <summary>Snaps a stick value to the nearest of 8 directions at full strength (zero stays zero).</summary>
        public static Vector2 SnapEightWay(Vector2 v)
        {
            if (v.sqrMagnitude < 0.0001f) return Vector2.zero;
            float a = Mathf.Atan2(v.y, v.x);
            float step = Mathf.PI / 4f;
            a = Mathf.Round(a / step) * step;
            var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            return new Vector2(Mathf.Abs(d.x) < 0.01f ? 0f : Mathf.Sign(d.x), Mathf.Abs(d.y) < 0.01f ? 0f : Mathf.Sign(d.y)) *
                   (Mathf.Abs(d.x) > 0.01f && Mathf.Abs(d.y) > 0.01f ? 0.7071f : 1f);
        }

        void UpdateDirection()
        {
            if (direction == null) return;
            bool on = IsActive && !editMode && Value.sqrMagnitude > 0.0001f;
            var c = TouchTheme.Current.ring;
            direction.color = new Color(c.r, c.g, c.b, on ? Mathf.Lerp(0.35f, 0.9f, Value.magnitude) : 0f);
            if (on) direction.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(Value.y, Value.x) * Mathf.Rad2Deg);
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (_pointerId != NoPointer) return;
            if (!editMode && !floating && !WithinBase(e.position)) return;
            _pointerId = e.pointerId;

            if (editMode) { MoveHome(e.position); return; }
            if (floating) stickBase.position = e.position; // Screen Space Overlay: world pos == screen pos
            UpdateKnob(e.position);
            ApplyTheme(TouchTheme.Current);
        }

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId != _pointerId) return;
            if (editMode) { MoveHome(e.position); return; }
            UpdateKnob(e.position);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId != _pointerId) return;
            if (editMode) HomeMoved?.Invoke(stickBase.anchorMin);
            Release();
        }

        void OnDisable() => Release();

        void Release()
        {
            _pointerId = NoPointer;
            Value = Vector2.zero;
            if (knob) knob.anchoredPosition = Vector2.zero;
            if (stickBase) stickBase.anchoredPosition = Vector2.zero; // back to home anchor
            UpdateDirection();
            ApplyTheme(TouchTheme.Current);
        }

        float RadiusPixels => stickBase.rect.width * 0.5f * stickBase.lossyScale.x;

        bool WithinBase(Vector2 screenPos) =>
            Vector2.Distance(screenPos, stickBase.position) <= RadiusPixels * 1.25f;

        void UpdateKnob(Vector2 screenPos)
        {
            float radius = Mathf.Max(1f, RadiusPixels);
            Vector2 delta = screenPos - (Vector2)stickBase.position;
            Vector2 clamped = Vector2.ClampMagnitude(delta, radius);
            knob.position = (Vector2)stickBase.position + clamped;

            Vector2 raw = clamped / radius;
            float mag = raw.magnitude;
            Value = mag < deadzone ? Vector2.zero : raw.normalized * ((mag - deadzone) / (1f - deadzone));
            if (eightWay) Value = SnapEightWay(Value);
            UpdateDirection();
        }

        void MoveHome(Vector2 screenPos)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(zone, screenPos, null, out var local)) return;
            Vector2 n = Rect.PointToNormalized(zone.rect, local);
            stickBase.anchorMin = stickBase.anchorMax = n;
            stickBase.anchoredPosition = Vector2.zero;
        }
    }
}
