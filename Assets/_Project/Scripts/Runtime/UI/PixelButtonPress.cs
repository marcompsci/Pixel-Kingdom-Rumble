using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// Pixel menu buttons: while held, swap to the sunken key sprite and drop the label onto it, so a tap feels like
    /// pressing a physical key. Added by UIFactory.Button when the pixel UI art is present.
    /// </summary>
    public class PixelButtonPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        Image _image;
        RectTransform _label;
        Vector2 _min, _max;
        bool _down;

        public void Init(Image image, RectTransform label)
        {
            _image = image;
            _label = label;
            _min = label.offsetMin;
            _max = label.offsetMax;
        }

        public void OnPointerDown(PointerEventData e)
        {
            var b = GetComponent<Button>();
            if (b != null && !b.interactable) return;
            Set(true);
        }

        public void OnPointerUp(PointerEventData e) => Set(false);
        public void OnPointerExit(PointerEventData e) => Set(false);
        void OnDisable() => Set(false);

        void Set(bool down)
        {
            if (_down == down || _image == null) return;
            _down = down;
            var pressed = UISprites.PixelButtonPressed;
            _image.sprite = down && pressed != null ? pressed : UISprites.PixelButton;
            if (_label == null) return;
            var drop = down ? new Vector2(0f, -12f) : Vector2.zero;
            _label.offsetMin = _min + drop;
            _label.offsetMax = _max + drop;
        }
    }
}
