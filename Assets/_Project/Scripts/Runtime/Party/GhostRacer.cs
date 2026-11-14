using UnityEngine;

namespace PKR
{
    /// <summary>
    /// The Item Race ghost: a see-through copy of the hero that replays your best run (RaceDirector moves it along the
    /// saved GhostTrack). It copies the hero's current sprite each frame; it has no collider and can't touch anything.
    /// </summary>
    public class GhostRacer : MonoBehaviour
    {
        SpriteRenderer _src, _sr;
        float _lastX;

        public static GhostRacer Spawn(GameObject hero, Material mat)
        {
            var go = new GameObject("RaceGhost");
            var g = go.AddComponent<GhostRacer>();
            g._src = hero != null ? hero.GetComponentInChildren<SpriteRenderer>() : null;
            g._sr = go.AddComponent<SpriteRenderer>();
            g._sr.color = new Color(0.65f, 0.85f, 1f, 0.42f);
            g._sr.sortingOrder = g._src != null ? g._src.sortingOrder - 1 : 0;
            if (mat != null) g._sr.sharedMaterial = mat;
            if (hero != null) go.transform.position = hero.transform.position;
            go.transform.localScale = hero != null ? hero.transform.localScale : Vector3.one;
            return g;
        }

        void LateUpdate()
        {
            if (_src != null) _sr.sprite = _src.sprite;
            float x = transform.position.x;
            if (Mathf.Abs(x - _lastX) > 0.002f) _sr.flipX = x < _lastX;
            _lastX = x;
        }
    }
}
