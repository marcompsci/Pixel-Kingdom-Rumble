using System.Collections.Generic;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>A relic case: take its relic weapon (you can carry one relic at a time) and refill it here.</summary>
    public class ZLRelicCase : ZLMarkerBase, IZLUse
    {
        public ZLRelic relic;
        public Vector2 UsePos => transform.position;
        public float UseRange => 1.8f;
        public bool CanUse => true;
        public string Prompt => "TAKE " + ZLCatalog.Relic(relic).name;

        public void Init(ZLGame g, ZLRelic r)
        {
            relic = r;
            var spec = ZLCatalog.Relic(r);
            Setup(g, g.Sprite("Props/pk_relic"), spec.name, new Color32(spec.r, spec.g, spec.b, 255), 1.5f);
        }

        public void Use(ZLPlayer p)
        {
            var gun = ZLCatalog.RelicGun(relic);
            bool had = game.Progress.Gun(gun.id) != null;
            ZLRules.GiveGun(game.Progress, gun, gun.magazine + gun.maxReserve);
            game.Progress.equipped = gun.id;
            p.CancelReload();
            Sfx.Play(Sfx.Relic, 0.9f);
            game.Save(true);
            if (had) game.ShowToast(gun.name + " REFILLED.");
            else game.Story("", new[] { ZLCatalog.Relic(relic).blurb, "Only one of the four relics can hurt the First Turned. You can carry one at a time." }, "zl_city", null);
        }

        protected override void Update()
        {
            base.Update();
            if (sr != null) sr.color = Color.Lerp(Color.white, new Color(1f, 0.9f, 1f), Mathf.PingPong(t * 2f, 1f));
        }
    }
}
