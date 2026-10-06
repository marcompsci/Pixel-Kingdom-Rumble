using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// Picks the CanvasScaler match axis from the screen shape so fixed-size menus always fit:
    /// screens wider than the reference aspect match height; narrower ones (e.g. a 4:3 iPad) match width.
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    public class CanvasMatchByAspect : MonoBehaviour
    {
        CanvasScaler _scaler;
        int _w, _h;

        void Awake()
        {
            _scaler = GetComponent<CanvasScaler>();
            Apply();
        }

        void Update()
        {
            if (Screen.width != _w || Screen.height != _h) Apply();
        }

        void Apply()
        {
            _w = Screen.width;
            _h = Screen.height;
            if (_h <= 0 || _scaler == null) return;
            var r = _scaler.referenceResolution;
            float screenAspect = (float)_w / _h;
            float refAspect = r.y > 0f ? r.x / r.y : 1f;
            _scaler.matchWidthOrHeight = screenAspect > refAspect ? 1f : 0f;
        }
    }
}
