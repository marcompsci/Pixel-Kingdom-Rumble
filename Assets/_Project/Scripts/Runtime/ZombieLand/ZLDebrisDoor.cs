using UnityEngine;

namespace PKR
{
    /// <summary>Last Stand: a debris barricade across a gap. Pay to clear it and open more of the block.</summary>
    public class ZLDebrisDoor : MonoBehaviour, IZLUse
    {
        public ZLCityData.Door Data { get; private set; }
        public bool Cleared { get; private set; }
        ZLLastStand _stand;
        BoxCollider2D _box;
        TextMesh _label;

        public Vector2 UsePos => transform.position;
        public float UseRange => Mathf.Max(Data.w, Data.h) * 0.5f + 1.6f;
        public bool CanUse => !Cleared && _stand != null && !_stand.Over;
        public string Prompt => $"CLEAR THE DEBRIS: {Data.name}  {Cost}";
        public int Cost => _stand != null ? _stand.DoorCost(Data) : Data.cost;

        public void Init(ZLGame g, ZLLastStand stand, ZLCityData.Door d)
        {
            _stand = stand;
            Data = d;
            for (int y = d.y; y < d.y + d.h; y++)
                for (int x = d.x; x < d.x + d.w; x++)
                {
                    var t = new GameObject("Debris");
                    t.transform.SetParent(transform, false);
                    t.transform.position = new Vector2(x + 0.5f, y + 0.5f);
                    var sr = t.AddComponent<SpriteRenderer>();
                    sr.sprite = g.Sprite("Props/debris_" + ((x * 7 + y * 3) % 3));
                    sr.sortingOrder = ZLSort.ByY(y + 0.5f);
                    if (g.material != null) sr.sharedMaterial = g.material;
                }
            _box = gameObject.AddComponent<BoxCollider2D>();
            _box.size = new Vector2(d.w, d.h);
            var lg = new GameObject("Label");
            lg.transform.SetParent(transform, false);
            lg.transform.localPosition = new Vector3(0f, d.h * 0.5f + 0.4f, 0f);
            _label = lg.AddComponent<TextMesh>();
            _label.font = UISprites.DisplayFont;
            _label.fontSize = 40;
            _label.characterSize = 0.07f;
            _label.anchor = TextAnchor.MiddleCenter;
            _label.color = new Color(1f, 0.85f, 0.5f);
            _label.text = $"{d.name}  {Cost}";
            var mr = lg.GetComponent<MeshRenderer>();
            mr.sharedMaterial = UISprites.DisplayFont.material;
            mr.sortingOrder = ZLSort.Overhead;
        }

        public void Use(ZLPlayer p) => _stand.OpenDoor(this, p);

        /// <summary>The debris is gone: no collider, no art.</summary>
        public void Clear()
        {
            Cleared = true;
            ZLFx.Spawn(GlowHalo.Sprite, transform.position, new Color(1f, 0.9f, 0.6f, 0.7f), 0.5f, 1f, 5f, ZLGame.Current != null ? ZLGame.Current.material : null);
            Destroy(gameObject);
        }
    }
}
