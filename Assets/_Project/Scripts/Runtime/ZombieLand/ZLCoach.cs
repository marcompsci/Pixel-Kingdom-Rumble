using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// First-time coaching in the zombie modes (Last Stand and Zombie Land): watches what's happening and shows each
    /// ZombieTips tip once per save, the moment it matters, one at a time. The HUD draws ZLGame.CoachTip.
    /// </summary>
    public class ZLCoach : MonoBehaviour
    {
        ZLGame _g;
        float _scan, _gap;

        public void Init(ZLGame g) { _g = g; _gap = 2f; }

        /// <summary>Shows a tip if this save hasn't had it. True when shown.</summary>
        public bool Show(string id)
        {
            if (_g == null || _gap > 0f || !ZombieTips.Texts.TryGetValue(id, out var text)) return false;
            var save = Services.Save;
            if (save == null || !save.Data.MarkHintSeen(id)) return false;
            save.MarkDirty();
            _g.ShowCoach(text);
            _gap = ZombieTips.Gap;
            return true;
        }

        void Update()
        {
            if (_g == null || !_g.Ready || _g.Paused || _g.Player == null) return;
            _gap -= Time.deltaTime;
            if (_gap > 0f) return;
            _scan -= Time.deltaTime;
            if (_scan > 0f) return;
            _scan = 0.5f;
            var p = _g.Player;
            Vector2 me = p.transform.position;

            if (_g.IsLastStand)
            {
                var st = _g.Stand;
                if (st == null || st.Over) return;
                if (st.Run.Round >= 1 && Show(ZombieTips.Points)) return;
                if (st.HoundRound && Show(ZombieTips.Hounds)) return;
                foreach (var h in _g.Squad)
                    if (h != null && h != p && h.Downed && Show(ZombieTips.Downed)) return;
            }
            else
            {
                if (_g.Progress != null && _g.Progress.playSeconds > 20f && Show(ZombieTips.Leads)) return;
            }

            // what's next to us
            var near = p.Nearby;
            if (near is ZLStation s && s.Data != null)
            {
                if (s.Data.k == "gun" && !s.Boarded && Show(ZombieTips.WallGun)) return;
                if (s.Data.k == "perk" && Show(ZombieTips.Perk)) return;
            }
            if (near is ZLMysteryBox && Show(ZombieTips.Box)) return;
            if (near is ZLSafehouse && Show(ZombieTips.Safehouse)) return;
            if (near is ZLVehicle && Show(ZombieTips.Drive)) return;

            // a door we can afford (Last Stand, after round 1)
            if (_g.IsLastStand && _g.Stand.Run.Round >= 2)
                foreach (var d in _g.City.doors)
                    if (_g.Stand.Run.Points >= _g.Stand.DoorCost(d) && Vector2.Distance(me, new Vector2(d.x + d.w * 0.5f, d.y + d.h * 0.5f)) < 12f
                        && _g.City.Blocked(d.x, d.y) && Show(ZombieTips.Door)) return;

            // ammo
            var ammo = p.Ammo;
            var gun = p.Gun;
            if (ammo != null && gun != null && ammo.mag + ammo.reserve < gun.magazine && Show(ZombieTips.LowAmmo)) return;
            if (gun != null && gun.kindle > 0f && Show(ZombieTips.Kindler)) return;

            // the new dead and power-ups in view
            foreach (var z in _g.Zombies)
            {
                if (z == null || !z.Alive) continue;
                float d = Vector2.Distance(z.transform.position, me);
                if (d > 9f) continue;
                if (z.kind == ZLFoe.Riot && Show(ZombieTips.Riot)) return;
                if (z.kind == ZLFoe.Leaper && Show(ZombieTips.Leaper)) return;
                if (z.kind == ZLFoe.Splitter && Show(ZombieTips.Splitter)) return;
                if (z.kind == ZLFoe.Burrower && Show(ZombieTips.Burrower)) return;
            }
            foreach (var pu in _g.PowerUpsOnGround())
                if (Vector2.Distance(pu.at, me) < 8f && Show(ZombieTips.PowerUp)) return;
        }
    }
}
