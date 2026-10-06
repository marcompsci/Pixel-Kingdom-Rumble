using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Pooled burst of small dots (enemy defeat, pickup collect, landing dust). Placeholder for real particles.
    /// </summary>
    public class PuffEffect : MonoBehaviour
    {
        const string PoolKey = "pkr.fx.puff";
        const int Dots = 8;

        SpriteRenderer[] _dots;
        Vector2[] _vel;
        float _age, _life;
        float _size;

        public static void Play(Vector2 position, Color color, Material material, float size = 0.18f, float speed = 3.5f, float life = 0.35f)
        {
            var go = PoolService.Get(PoolKey, Create, maxSize: 32);
            go.GetComponent<PuffEffect>().Begin(position, color, material, size, speed, life);
        }

        static GameObject Create()
        {
            var go = new GameObject("Puff");
            var fx = go.AddComponent<PuffEffect>();
            fx._dots = new SpriteRenderer[Dots];
            fx._vel = new Vector2[Dots];
            for (int i = 0; i < Dots; i++)
            {
                var d = new GameObject("Dot");
                d.transform.SetParent(go.transform, false);
                var sr = d.AddComponent<SpriteRenderer>();
                sr.sprite = UISprites.Circle;
                sr.sortingOrder = 25;
                fx._dots[i] = sr;
            }
            return go;
        }

        void Begin(Vector2 pos, Color color, Material material, float size, float speed, float life)
        {
            transform.position = pos;
            _age = 0f;
            _life = Mathf.Max(0.05f, life);
            _size = size;
            for (int i = 0; i < Dots; i++)
            {
                float a = (i / (float)Dots) * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
                _vel[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * speed * Random.Range(0.6f, 1.1f);
                _dots[i].transform.localPosition = Vector3.zero;
                _dots[i].color = color;
                if (material != null) _dots[i].sharedMaterial = material;
            }
            Apply(0f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            _age += dt;
            for (int i = 0; i < Dots; i++)
            {
                _dots[i].transform.localPosition += (Vector3)(_vel[i] * dt);
                _vel[i] *= 1f - Mathf.Clamp01(6f * dt); // drag
            }
            Apply(_age / _life);
            if (_age >= _life) PoolService.Release(PoolKey, gameObject);
        }

        void Apply(float t)
        {
            float s = _size * (1f - t) / 1.28f; // UISprites.Circle is 1.28 units at 100 PPU
            for (int i = 0; i < Dots; i++) _dots[i].transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
