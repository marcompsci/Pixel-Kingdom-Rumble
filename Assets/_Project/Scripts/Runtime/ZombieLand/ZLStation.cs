using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>Last Stand: a wall gun board, a perk machine or the upgrade forge on Hollow Block.</summary>
    public class ZLStation : ZLMarkerBase, IZLUse
    {
        public ZLCityData.Station Data { get; private set; }
        ZLLastStand _stand;
        ZLGun _gun;
        ZLPerk _perk;

        public Vector2 UsePos => transform.position;
        public float UseRange => 1.7f;
        public bool CanUse => _stand != null && !_stand.Over;

        public void Init(ZLGame g, ZLLastStand stand, ZLCityData.Station s)
        {
            _stand = stand;
            Data = s;
            string art, text;
            Color c = new Color(1f, 0.9f, 0.6f);
            switch (s.k)
            {
                case "gun":
                    _gun = ZLCatalog.Gun(s.id);
                    art = "Props/wall_" + s.id;
                    text = (_gun != null ? _gun.name : s.id) + "  " + s.cost;
                    break;
                case "perk":
                    _perk = (ZLPerk)System.Enum.Parse(typeof(ZLPerk), s.id);
                    art = "Props/perk_" + s.id.ToLowerInvariant();
                    text = ZLShop.PerkName(_perk);
                    c = new Color(0.75f, 0.9f, 1f);
                    break;
                default:
                    art = "Props/forge";
                    text = "UPGRADE FORGE";
                    c = new Color(0.85f, 0.7f, 1f);
                    break;
            }
            Setup(g, g.Sprite(art), text, c, s.k == "gun" ? 1.3f : 1.4f);
        }

        public string Prompt
        {
            get
            {
                switch (Data.k)
                {
                    case "gun":
                        if (_gun == null) return "";
                        return game.Progress.Gun(_gun.id) != null ? $"{_gun.name} AMMO  {Data.cost / 2}" : $"BUY {_gun.name}  {Data.cost}";
                    case "perk":
                        return ZLShop.HasPerk(game.Progress, _perk) ? ZLShop.PerkName(_perk) + " (OWNED)"
                             : $"{ZLShop.PerkName(_perk)}  {LastStandRules.PerkCost(_perk)}  ·  {ZLShop.PerkInfo(_perk)}";
                    default:
                        var held = ZLCatalog.Gun(game.Progress.equipped);
                        return ZLShop.CanForge(game.Progress, held) ? $"FORGE {held.name}  {_stand.ForgeCost}" : "THIS GUN IS ALREADY FORGED";
                }
            }
        }

        public void Use(ZLPlayer p)
        {
            switch (Data.k)
            {
                case "gun": if (_gun != null) _stand.UseWallGun(p, _gun, Data.cost); break;
                case "perk": _stand.BuyPerk(p, _perk); break;
                default: _stand.UseForge(p); break;
            }
        }
    }
}
