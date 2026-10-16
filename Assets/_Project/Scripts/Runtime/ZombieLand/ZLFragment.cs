using UnityEngine;

namespace PKR
{
    /// <summary>Last Stand Easter egg: a graystone fragment hidden on the block. Find all three to wake the Hollow King.</summary>
    public class ZLFragment : ZLMarkerBase, IZLUse
    {
        ZLLastStand _stand;
        bool _taken;

        public Vector2 UsePos => transform.position;
        public float UseRange => 1.2f;
        public bool CanUse => !_taken && _stand != null && !_stand.Over;
        public string Prompt => "A HUMMING STONE";

        public void Init(ZLGame g, ZLLastStand stand)
        {
            _stand = stand;
            Setup(g, g.Sprite("Props/fragment"), null, Color.white, 1f);
        }

        public void Use(ZLPlayer p)
        {
            if (!CanUse) return;
            _taken = true;
            ZLFx.Spawn(GlowHalo.Sprite, transform.position, new Color(0.7f, 0.5f, 1f, 0.9f), 0.6f, 0.5f, 4f, game.material);
            _stand.FoundFragment(this);
            Destroy(gameObject);
        }

        protected override void Update()
        {
            base.Update();
            if (sr != null) sr.color = new Color(1f, 1f, 1f, 0.75f + Mathf.Sin(t * 3f) * 0.25f);
        }
    }
}
