using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Last Stand Easter egg: one of three hidden pieces (graystone fragments on Hollow Block, brass bells on Salt Pier).
    /// Find all three to wake the map's hidden boss.
    /// </summary>
    public class ZLFragment : ZLMarkerBase, IZLUse
    {
        ZLLastStand _stand;
        bool _taken;

        public Vector2 UsePos => transform.position;
        public float UseRange => 1.2f;
        public bool CanUse => !_taken && _stand != null && !_stand.Over;
        public string Prompt => _prompt;
        string _prompt = "A HUMMING STONE";

        public void Init(ZLGame g, ZLLastStand stand) => Init(g, stand, LastStandMap.HollowBlock);

        public void Init(ZLGame g, ZLLastStand stand, LastStandMap map)
        {
            _stand = stand;
            _prompt = map.EggPrompt;
            Setup(g, g.Sprite("Props/" + map.EggSprite), null, Color.white, 1f);
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
