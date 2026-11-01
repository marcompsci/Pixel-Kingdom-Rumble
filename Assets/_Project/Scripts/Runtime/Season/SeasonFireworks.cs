using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>New Year: small firework bursts in the sky behind the level now and then.</summary>
    public class SeasonFireworks : MonoBehaviour
    {
        Camera _cam;
        Material _mat;
        float _next = 1.5f;
        readonly SpriteRenderer[] _sparks = new SpriteRenderer[18];
        readonly Vector2[] _vel = new Vector2[18];
        float _age = 99f;
        Vector2 _origin;

        public void Init(Material mat)
        {
            _cam = GetComponent<Camera>();
            _mat = mat;
            var root = new GameObject("Fireworks").transform;
            for (int i = 0; i < _sparks.Length; i++)
            {
                var sr = new GameObject("Spark").AddComponent<SpriteRenderer>();
                sr.transform.SetParent(root, false);
                sr.sprite = SeasonArt.Dot();
                if (mat != null) sr.sharedMaterial = mat;
                sr.sortingOrder = -65;
                sr.transform.localScale = Vector3.one * 1.6f;
                sr.enabled = false;
                _sparks[i] = sr;
            }
        }

        void Update()
        {
            if (_cam == null) return;
            float dt = Time.deltaTime;
            _next -= dt;
            if (_next <= 0f)
            {
                _next = 2.6f + Random.value * 2.2f;
                var c = _cam.transform.position;
                float h = _cam.orthographicSize;
                _origin = new Vector2(c.x + Random.Range(-h, h) * _cam.aspect * 0.8f, c.y + h * Random.Range(0.35f, 0.75f));
                var col = (Color)SeasonArt.BulbColours[Random.Range(0, SeasonArt.BulbColours.Length)];
                for (int i = 0; i < _sparks.Length; i++)
                {
                    float a = i * Mathf.PI * 2f / _sparks.Length;
                    _vel[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(2.2f, 2.8f);
                    _sparks[i].color = col;
                    _sparks[i].enabled = true;
                    _sparks[i].transform.position = _origin;
                }
                _age = 0f;
            }
            if (_age > 1.3f) return;
            _age += dt;
            float alpha = 1f - Ease.Out(_age / 1.3f);
            for (int i = 0; i < _sparks.Length; i++)
            {
                _vel[i] += new Vector2(0f, -2.2f) * dt;
                _vel[i] *= 1f - 1.6f * dt;
                var t = _sparks[i].transform;
                t.position += (Vector3)(_vel[i] * dt);
                var c = _sparks[i].color; c.a = alpha; _sparks[i].color = c;
                if (_age > 1.3f) _sparks[i].enabled = false;
            }
        }
    }
}
