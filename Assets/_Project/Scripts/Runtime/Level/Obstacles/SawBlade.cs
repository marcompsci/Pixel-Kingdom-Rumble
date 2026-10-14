using UnityEngine;

namespace PKR
{
    /// <summary>A spinning saw that rolls back and forth between two points (set by the builder). Touching it hurts.</summary>
    public class SawBlade : MonoBehaviour
    {
        public Vector2 a, b;
        public float speed = 3f, offset;
        public Transform art;
        float _t;

        void Awake() => _t = offset;

        void Update()
        {
            _t += Time.deltaTime;
            float len = Mathf.Max(0.01f, Vector2.Distance(a, b));
            float k = Mathf.PingPong(_t * speed / len, 1f);
            // Ease at the ends so the turn-around reads clearly.
            k = k * k * (3f - 2f * k);
            transform.position = Vector2.Lerp(a, b, k);
            if (art != null) art.Rotate(0f, 0f, -540f * Time.deltaTime);
        }
    }
}
