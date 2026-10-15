using System.Collections.Generic;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>One of the six possible lairs. Sealed until you carry ten shards; only the real one holds him.</summary>
    public class ZLLairDoor : ZLMarkerBase, IZLUse
    {
        public string lairId;
        public Vector2 UsePos => transform.position;
        public float UseRange => 2.2f;
        public bool CanUse => !game.Progress.bossDefeated && game.Boss == null;
        public string Prompt => "SEARCH " + ZLCatalog.Lair(lairId).name.ToUpperInvariant();

        public void Init(ZLGame g, string id)
        {
            lairId = id;
            Setup(g, g.Sprite("Props/pk_lair"), "?", new Color(1f, 0.5f, 0.5f), 1.4f);
        }

        public void Use(ZLPlayer p) => game.TryLair(lairId, transform.position);

        protected override void Update()
        {
            base.Update();
            if (label != null)
                label.text = game.Progress.lairsSearched.Contains(lairId) ? "EMPTY" : game.Progress.Shards >= ZLCatalog.ShardsNeeded ? "LAIR?" : "SEALED";
        }
    }
}
