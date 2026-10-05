using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Lightweight platformer camera: smooth follow, look-ahead in the facing direction, a vertical dead zone
    /// so small hops don't bob the screen, optional level bounds, and screen shake.
    /// (Chosen over Cinemachine to keep Phase 1 dependency-free and easy to tune.)
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow2D : MonoBehaviour
    {
        public Transform target;
        [SerializeField] Vector2 offset = new Vector2(0f, 1.5f);
        [SerializeField] float smoothTimeX = 0.12f;
        [SerializeField] float smoothTimeY = 0.2f;
        [SerializeField] float lookAhead = 1.5f;
        [SerializeField] float verticalDeadZone = 1.2f;
        [Tooltip("World-space bounds the camera view must stay inside. Width/height 0 = unbounded.")]
        public Rect bounds;

        Camera _cam;
        PlatformerMotor2D _motor;
        Vector2 _vel;
        float _focusY;
        float _shakeTime, _shakeDuration, _shakeAmplitude;

        static CameraFollow2D _main;
        public static CameraFollow2D Main => _main;

        void Awake()
        {
            _cam = GetComponent<Camera>();
            _main = this;
        }

        void OnDestroy()
        {
            if (_main == this) _main = null;
        }

        void Start() => SnapToTarget();

        public void SetTarget(Transform t)
        {
            target = t;
            _motor = t != null ? t.GetComponent<PlatformerMotor2D>() : null;
            SnapToTarget();
        }

        public void SnapToTarget()
        {
            if (target == null) return;
            if (_motor == null) _motor = target.GetComponent<PlatformerMotor2D>();
            _focusY = target.position.y;
            Vector3 p = Desired();
            transform.position = new Vector3(p.x, p.y, transform.position.z);
            _vel = Vector2.zero;
        }

        /// <summary>Respects the Screen Shake accessibility setting.</summary>
        public void Shake(float amplitude, float duration)
        {
            var s = Services.Settings?.Data;
            if (s != null && !s.screenShakeEnabled) return;
            _shakeAmplitude = Mathf.Max(_shakeAmplitude * (_shakeTime > 0f ? 1f : 0f), amplitude);
            _shakeDuration = Mathf.Max(duration, 0.01f);
            _shakeTime = _shakeDuration;
        }

        Vector2 Desired()
        {
            Vector3 t = target.position;
            // Only re-focus vertically when grounded or when the target leaves the dead zone.
            bool grounded = _motor != null && _motor.IsGrounded;
            if (grounded || Mathf.Abs(t.y - _focusY) > verticalDeadZone) _focusY = t.y;
            float ahead = _motor != null ? _motor.Facing * lookAhead : 0f;
            return new Vector2(t.x + ahead + offset.x, _focusY + offset.y);
        }

        void LateUpdate()
        {
            if (target == null) return;
            Vector2 desired = Desired();
            Vector2 pos = transform.position;
            pos.x = Mathf.SmoothDamp(pos.x, desired.x, ref _vel.x, smoothTimeX);
            pos.y = Mathf.SmoothDamp(pos.y, desired.y, ref _vel.y, smoothTimeY);
            pos = ClampToBounds(pos);

            Vector2 shake = Vector2.zero;
            if (_shakeTime > 0f)
            {
                _shakeTime -= Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(_shakeTime / _shakeDuration);
                shake = Random.insideUnitCircle * _shakeAmplitude * k;
                if (_shakeTime <= 0f) _shakeAmplitude = 0f;
            }
            transform.position = new Vector3(pos.x + shake.x, pos.y + shake.y, transform.position.z);
        }

        Vector2 ClampToBounds(Vector2 p)
        {
            if (bounds.width <= 0f || bounds.height <= 0f || _cam == null || !_cam.orthographic) return p;
            float halfH = _cam.orthographicSize;
            float halfW = halfH * _cam.aspect;
            float minX = bounds.xMin + halfW, maxX = bounds.xMax - halfW;
            float minY = bounds.yMin + halfH, maxY = bounds.yMax - halfH;
            p.x = minX > maxX ? bounds.center.x : Mathf.Clamp(p.x, minX, maxX);
            p.y = minY > maxY ? bounds.center.y : Mathf.Clamp(p.y, minY, maxY);
            return p;
        }
    }
}
