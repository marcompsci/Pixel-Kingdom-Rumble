using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Chapter 3's freight lift: in the city it sits under the Graystone lab ("lab_lift") and goes down to the sublevel;
    /// down there ("lift") it goes back up. Riding it saves, swaps the map and reloads Zombie Land on the other side.
    /// </summary>
    public class ZLLift : ZLMarkerBase, IZLUse
    {
        bool _down;   // true = this lift is in the city and goes down
        public Vector2 UsePos => transform.position;
        public float UseRange => 2.2f;
        public bool CanUse => game.Boss == null && game.Active == null && ZLRules.LiftOpen(game.Progress);
        public string Prompt => _down ? "RIDE THE LIFT DOWN" : "RIDE THE LIFT UP";

        public void Init(ZLGame g, bool goesDown)
        {
            _down = goesDown;
            Setup(g, g.Sprite("Props/pk_arrow"), goesDown ? "LIFT" : "LIFT UP", new Color(0.6f, 1f, 0.7f), 1.3f);
        }

        public void Use(ZLPlayer p) => game.RideLift(_down);

        protected override void Update()
        {
            base.Update();
            bool open = game != null && ZLRules.LiftOpen(game.Progress);
            if (sr != null) sr.enabled = open;
            if (label != null) label.text = open ? (_down ? "SUBLEVEL LIFT" : "LIFT UP") : "";
        }
    }
}
