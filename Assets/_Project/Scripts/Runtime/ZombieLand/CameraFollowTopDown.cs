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

        public void Shake(float amp, float time) { _shakeAmp = Mathf.Max(_shakeAmp, amp); _shake = Mathf.Max(_shake, time); }

        public void Snap()
        {
            if (target == null) return;
            transform.position = new Vector3(target.position.x, target.position.y, -10f);
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
            transform.position = Vector3.Lerp(transform.position, goal, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));
            if (_shake > 0f)
            {
                _shake -= Time.unscaledDeltaTime;
                transform.position += (Vector3)(Random.insideUnitCircle * _shakeAmp);
            }
        }
    }
}
