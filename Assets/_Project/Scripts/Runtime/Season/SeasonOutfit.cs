using UnityEngine;

namespace PKR
{
    /// <summary>A seasonal hat on a fighter's body sprite (flips and squashes with it). Hidden out of season.</summary>
    public class SeasonOutfit : MonoBehaviour
    {
        public bool alwaysOn;
        SpriteRenderer _hat;

        public void Wear(Sprite sprite, SeasonArt.Hat kind, Material mat)
        {
            var body = FindBody();
            if (body == null || body.sprite == null) return;
            if (_hat == null)
            {
                var go = new GameObject("SeasonHat");
                go.transform.SetParent(body.transform, false);
                _hat = go.AddComponent<SpriteRenderer>();
            }
            if (_hat.sprite == sprite && _hat.gameObject.activeSelf) return;
            _hat.sprite = sprite;
            if (mat != null) _hat.sharedMaterial = mat;
            _hat.sortingOrder = body.sortingOrder + 1;
            var b = body.sprite.bounds; // local, before any flip
            float hatW = sprite.bounds.size.x;
            float want = kind == SeasonArt.Hat.Earmuffs ? b.size.x * 0.85f : b.size.x * 0.62f;
            float s = Mathf.Clamp(want / Mathf.Max(0.05f, hatW), 0.6f, 1.6f);
            _hat.transform.localScale = new Vector3(s, s, 1f);
            float y = kind switch
            {
                SeasonArt.Hat.HarvestScarf => b.min.y + b.size.y * 0.42f,
                SeasonArt.Hat.Earmuffs => b.max.y - b.size.y * 0.3f,
                _ => b.max.y - 0.14f,
            };
            float x = b.center.x + (kind == SeasonArt.Hat.HarvestScarf ? 0f : -b.size.x * 0.04f);
            _hat.transform.localPosition = new Vector3(x, y, 0f);
            _hat.gameObject.SetActive(true);
        }

        SpriteRenderer FindBody()
        {
            // the body is the first child sprite (EnemyFactory and the hero prefab both put it there)
            for (int i = 0; i < transform.childCount; i++)
            {
                var sr = transform.GetChild(i).GetComponent<SpriteRenderer>();
                if (sr != null) return sr;
            }
            return GetComponentInChildren<SpriteRenderer>();
        }

        void OnEnable()
        {
            // pooled foes come back in other levels: only show the hat while a season is dressing the level
            if (_hat != null) _hat.gameObject.SetActive(alwaysOn || SeasonDirector.Current != null);
        }
    }
}
