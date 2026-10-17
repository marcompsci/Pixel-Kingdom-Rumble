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
        bool TideGate => lairId == ZLCatalog.TideGate;
        bool Vault => lairId == ZLCatalog.Vault;
        bool Hangar => lairId == ZLCatalog.Hangar;
        public bool CanUse => game.Boss == null && (Hangar ? game.Progress.InChapter4 : Vault ? game.Progress.InChapter3 : TideGate ? game.Progress.InChapter2 : !game.Progress.bossDefeated);
        public string Prompt => Hangar ? "HANGAR 3" : Vault ? "THE CURATOR'S VAULT" : TideGate ? "THE TIDE GATE" : "SEARCH " + ZLCatalog.Lair(lairId).name.ToUpperInvariant();

        public void Init(ZLGame g, string id)
        {
            lairId = id;
            Setup(g, g.Sprite("Props/pk_lair"), "?", new Color(1f, 0.5f, 0.5f), 1.4f);
        }

        public void Use(ZLPlayer p) => game.TryLair(lairId, transform.position);

        protected override void Update()
        {
            base.Update();
            if (label == null) return;
            if (Hangar)
            {
                var p = game.Progress;
                label.text = !p.InChapter4 ? "" : p.Clearances >= ZLCatalog.ClearancesNeeded ? "HANGAR 3" : $"CLEARANCES {p.Clearances}/{ZLCatalog.ClearancesNeeded}";
                if (sr != null) sr.enabled = p.InChapter4;
                return;
            }
            if (Vault)
            {
                var p = game.Progress;
                label.text = !p.InChapter3 ? "" : p.Keycards >= ZLCatalog.KeycardsNeeded ? "VAULT" : $"KEYCARDS {p.Keycards}/{ZLCatalog.KeycardsNeeded}";
                if (sr != null) sr.enabled = p.InChapter3;
                return;
            }
            if (TideGate)
            {
                var p = game.Progress;
                label.text = !p.InChapter2 ? "" : p.Sigils >= ZLCatalog.SigilsNeeded ? "TIDE GATE" : $"SIGILS {p.Sigils}/{ZLCatalog.SigilsNeeded}";
                if (sr != null) sr.enabled = p.InChapter2;
                return;
            }
            label.text = game.Progress.lairsSearched.Contains(lairId) ? "EMPTY" : game.Progress.Shards >= ZLCatalog.ShardsNeeded ? "LAIR?" : "SEALED";
        }
    }
}
