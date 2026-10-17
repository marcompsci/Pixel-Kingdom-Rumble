using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>Top-down camera: follows the hero, eases out to a wider view while driving fast.</summary>
    public class CameraFollowTopDown : MonoBehaviour
    {
        public static CameraFollowTopDown Main { get; private set; }
        public Transform target;
        public float footSize = 6.5f, driveSize = 9.5f;
        Camera _cam;
        float _shake, _shakeAmp;
        Vector2 _look;

        void Awake()
        {
            Main = this;
            _cam = GetComponent<Camera>();
        }

        /// <summary>Keeps the view inside the map, so the empty void past its edge never shows (Last Stand maps are small).</summary>
        Vector3 ClampToMap(Vector3 pos)
        {
            var g = ZLGame.Current;
            var city = g != null ? g.City : null;
            if (city == null || _cam == null) return pos;
            float hh = _cam.orthographicSize, hw = hh * _cam.aspect;
            pos.x = city.w > hw * 2f ? Mathf.Clamp(pos.x, hw, city.w - hw) : city.w * 0.5f;
            pos.y = city.h > hh * 2f ? Mathf.Clamp(pos.y, hh, city.h - hh) : city.h * 0.5f;
            return pos;
        }

        /// <summary>Respects Screen shake and Reduced motion (Settings).</summary>
        public void Shake(float amp, float time)
        {
            amp *= SettingsService.ShakeScale;
            if (amp <= 0f) return;
            _shakeAmp = Mathf.Max(_shakeAmp, amp);
            _shake = Mathf.Max(_shake, time);
        }

        public void Snap()
        {
            if (target == null) return;
            transform.position = ClampToMap(new Vector3(target.position.x, target.position.y, -10f));
        }

        void LateUpdate()
        {
            if (target == null || _cam == null) return;
            var p = ZLPlayer.Current;
            var v = p != null ? p.Driving : null;
            float size = v != null ? Mathf.Lerp(footSize + 1.5f, driveSize, Mathf.Clamp01(Mathf.Abs(v.Speed) / 14f)) : footSize;
            _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, size, Time.unscaledDeltaTime * 2f);
            Vector2 lead = v != null ? (Vector2)v.transform.right * Mathf.Clamp(v.Speed, -4f, 16f) * 0.25f : Vector2.zero;
            _look = Vector2.Lerp(_look, lead, Time.unscaledDeltaTime * 3f);
            Vector3 goal = new Vector3(target.position.x + _look.x, target.position.y + _look.y, -10f);
            transform.position = ClampToMap(Vector3.Lerp(transform.position, goal, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime)));
            if (_shake > 0f)
            {
                _shake -= Time.unscaledDeltaTime;
                transform.position += (Vector3)(Random.insideUnitCircle * _shakeAmp);
            }
        }
    }
}
