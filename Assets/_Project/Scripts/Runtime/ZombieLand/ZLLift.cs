using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// A way between maps. Chapter 3's freight lift: in the city it sits under the Graystone lab ("lab_lift") and goes
    /// down to the sublevel; down there ("lift") it goes back up. Chapter 4's service road: beside the rail depot
    /// ("air_road") it leads out to Cold Harbor airfield; at the airfield ("road") it leads back. Using one saves,
    /// swaps the map and reloads Zombie Land on the other side.
    /// </summary>
    public class ZLLift : ZLMarkerBase, IZLUse
    {
        public enum Kind { LiftDown, LiftUp, RoadOut, RoadBack }

        Kind _kind;
        public Kind Which => _kind;
        bool Road => _kind == Kind.RoadOut || _kind == Kind.RoadBack;
        public Vector2 UsePos => transform.position;
        public float UseRange => 2.2f;
        bool Open => game != null && (Road ? ZLRules.RoadOpen(game.Progress) : ZLRules.LiftOpen(game.Progress));
        public bool CanUse => game.Boss == null && game.Active == null && Open;
        public string Prompt => _kind == Kind.LiftDown ? "RIDE THE LIFT DOWN" : _kind == Kind.LiftUp ? "RIDE THE LIFT UP"
                              : _kind == Kind.RoadOut ? "TAKE THE ROAD TO COLD HARBOR" : "TAKE THE ROAD BACK TO THE CITY";

        public void Init(ZLGame g, Kind kind)
        {
            _kind = kind;
            Setup(g, g.Sprite("Props/pk_arrow"), Label, Road ? new Color(1f, 0.85f, 0.5f) : new Color(0.6f, 1f, 0.7f), 1.3f);
        }

        /// <summary>Chapter 3's lift (kept for older callers).</summary>
        public void Init(ZLGame g, bool goesDown) => Init(g, goesDown ? Kind.LiftDown : Kind.LiftUp);

        string Label => _kind == Kind.LiftDown ? "SUBLEVEL LIFT" : _kind == Kind.LiftUp ? "LIFT UP" : _kind == Kind.RoadOut ? "TO COLD HARBOR" : "TO THE CITY";

        public void Use(ZLPlayer p)
        {
            if (Road) game.TakeRoad(_kind == Kind.RoadOut);
            else game.RideLift(_kind == Kind.LiftDown);
        }

        protected override void Update()
        {
            base.Update();
            bool open = Open;
            if (sr != null) sr.enabled = open;
            if (label != null) label.text = open ? Label : "";
        }
    }
}
