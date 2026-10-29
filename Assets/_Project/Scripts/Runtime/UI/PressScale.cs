using PKR.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// Press feedback for buttons without the pixel key art (icon buttons): scales to 96% while held and eases back
    /// when released. Disabled buttons don't react.
    /// </summary>
    public class PressScale : MonoBehaviour, UnityEngine.EventSystems.IPointerDownHandler, UnityEngine.EventSystems.IPointerUpHandler,
                              UnityEngine.EventSystems.IPointerExitHandler
    {
        public const float Pressed = 0.96f;
        bool _down;

        public void OnPointerDown(UnityEngine.EventSystems.PointerEventData e)
        {
            var b = GetComponent<Button>();
            if (b != null && !b.interactable) return;
            _down = true;
            UiMotion.ScaleTo(transform, Pressed, 0.08f);
        }

        public void OnPointerUp(UnityEngine.EventSystems.PointerEventData e) => Release();
        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData e) => Release();

        void Release()
        {
            if (!_down) return;
            _down = false;
            UiMotion.ScaleTo(transform, 1f, Ease.Quick);
        }

        void OnDisable()
        {
            _down = false;
            UiMotion.Stop(transform);
            transform.localScale = Vector3.one;
        }
    }
}
