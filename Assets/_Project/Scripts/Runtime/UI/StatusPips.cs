using System.Collections.Generic;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Row of small circles above a fighter showing Story HP or Arena Guard Pips.
    /// When a fighter is Exposed (0 pips) the row flashes so the danger is readable at phone size.
    /// Built at runtime; copies the body sprite's material so it renders like the rest of the scene.
    /// </summary>
    [RequireComponent(typeof(Damageable))]
    public class StatusPips : MonoBehaviour
    {
        [SerializeField] float heightAbove = 1.25f;
        [SerializeField] float spacing = 0.28f;
        [SerializeField] float pipSize = 0.2f;
        [SerializeField] int maxShown = 10;

        static readonly Color Full = new Color32(120, 230, 120, 255);
        static readonly Color PipFull = new Color32(110, 200, 255, 255);
        static readonly Color Empty = new Color(0f, 0f, 0f, 0.45f);
        static readonly Color Danger = new Color32(255, 70, 70, 255);

        Damageable _d;
        Transform _row;
        readonly List<SpriteRenderer> _dots = new List<SpriteRenderer>();
        Material _material;

        void Awake()
        {
            _d = GetComponent<Damageable>();
            var body = GetComponentInChildren<SpriteRenderer>();
            if (body != null) _material = body.sharedMaterial;
            _row = new GameObject("StatusPips").transform;
            _row.SetParent(transform, false);
            _row.localPosition = new Vector3(0f, heightAbove, 0f);
        }

        void EnsureDots(int count)
        {
            count = Mathf.Clamp(count, 0, maxShown);
            while (_dots.Count < count)
            {
                var go = new GameObject("Pip");
                go.transform.SetParent(_row, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = UISprites.Circle;
                if (_material != null) sr.sharedMaterial = _material;
                sr.sortingOrder = 30;
                float s = pipSize / 1.28f; // UISprites.Circle is 1.28 units wide at 100 PPU
                go.transform.localScale = new Vector3(s, s, 1f);
                _dots.Add(sr);
            }
            for (int i = 0; i < _dots.Count; i++)
            {
                _dots[i].gameObject.SetActive(i < count);
                _dots[i].transform.localPosition = new Vector3((i - (count - 1) * 0.5f) * spacing, 0f, 0f);
            }
        }

        void LateUpdate()
        {
            bool arena = _d.Model == DamageModel.ArenaPips;
            int max = arena ? _d.Pips.MaxPips : _d.MaxHealth;
            int cur = arena ? _d.Pips.Current : _d.Health;
            EnsureDots(max);

            bool exposedFlash = arena && _d.IsExposed && Mathf.FloorToInt(Time.unscaledTime * 8f) % 2 == 0;
            for (int i = 0; i < _dots.Count && i < max; i++)
            {
                if (exposedFlash) _dots[i].color = Danger;
                else _dots[i].color = i < cur ? (arena ? PipFull : Full) : Empty;
            }

            // Keep the row upright when the root flips.
            _row.rotation = Quaternion.identity;
        }
    }
}
