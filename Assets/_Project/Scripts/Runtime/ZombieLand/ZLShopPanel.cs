using PKR.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// A safehouse's supply crate (Zombie Land): REST, then spend SCRAP on ammo, medkits and armor, four of five perks,
    /// a mystery box spin (the Storm Coil lives in there) and the upgrade forge. Pauses the game while open.
    /// </summary>
    public class ZLShopPanel : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }
        static readonly Color32 Gold = new Color32(255, 214, 90, 255);

        ZLGame _g;
        ZLSafehouse _house;
        RectTransform _root;
        Canvas _canvas;
        float _prevScale = 1f;
        TouchControlsUI _touch;
        string _message = "";

        bool Discount => _g != null && SurvivorRank.Has(_g.CareerXp, "zl_discount");
        int Price(int baseCost) => ZLShop.Price(baseCost, Discount);

        public void Open(ZLSafehouse house)
        {
            _g = ZLGame.Current;
            if (_g == null) return;
            _house = house;
            if (_root == null)
            {
                var safe = UIFactory.CreateCanvas(transform, "ZLShopCanvas", 62, landscape: true, out _canvas);
                _root = UIFactory.Rect("Shop", safe);
                UIFactory.Stretch(_root);
            }
            _canvas.enabled = true;
            IsOpen = true;
            _touch = Object.FindAnyObjectByType<TouchControlsUI>();
            if (_touch != null) _touch.gameObject.SetActive(false);
            _prevScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            _message = "";
            Build();
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            if (_canvas != null) _canvas.enabled = false;
            if (_touch != null) _touch.gameObject.SetActive(true);
            Time.timeScale = _prevScale;
            _g?.Save(true);
        }

        void OnDestroy()
        {
            if (IsOpen) { IsOpen = false; Time.timeScale = 1f; }
        }

        RectTransform Row(RectTransform parent, float h)
        {
            var row = UIFactory.Rect("Row", parent);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = h;
            var hl = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 16f;
            hl.childControlWidth = true; hl.childControlHeight = true;
            hl.childForceExpandWidth = true; hl.childForceExpandHeight = true;
            return row;
        }

        Button Item(RectTransform row, string name, string sub, bool enabled, Color32 color, UnityEngine.Events.UnityAction onClick)
        {
            var b = UIFactory.Button(row, name, onClick, 120f, enabled);
            UIFactory.Tint(b, color, Color.white);
            var t = b.GetComponentInChildren<Text>();
            if (t != null)
            {
                t.supportRichText = true;
                t.resizeTextForBestFit = false;
                t.fontSize = 30;
                t.text = $"{name}\n<size=22>{sub}</size>";
            }
            return b;
        }

        void Build()
        {
            UIFactory.Clear(_root);
            UIFactory.Dim(_root).color = new Color(0f, 0f, 0f, 0.8f);
            var p = _g.Progress;
            var panel = UIFactory.Panel(_root, new Vector2(1760f, 1000f));
            var head = Row(panel, 80f);
            var title = UIFactory.Label(head, _house != null ? "SAFEHOUSE: " + _house.Data.name.ToUpperInvariant() : "SAFEHOUSE", 46, TextAnchor.MiddleLeft, 80f, title: true);
            title.color = new Color(0.6f, 1f, 0.7f);
            var scrap = UIFactory.Label(head, $"SCRAP {p.scrap}" + (Discount ? "  (15% OFF)" : ""), 46, TextAnchor.MiddleRight, 80f, title: true);
            scrap.color = Gold;

            // Rest and supplies
            var r1 = Row(panel, 120f);
            bool canRest = _house == null || _house.CanRest;
            Item(r1, "REST", canRest ? "Full health" : $"Again in {_house.RestSeconds}s", canRest, new Color32(60, 130, 80, 255), () =>
            {
                if (_house != null) _house.Rest(_g.Player); else _g.Player.Heal(_g.Player.MaxHealth);
                _message = "RESTED.";
                Build();
            });
            int ammo = Price(ZLShop.AmmoCost), med = Price(ZLShop.MedkitCost), arm = Price(ZLShop.ArmorCost);
            Item(r1, "MAX AMMO", $"Every gun full · {ammo}", p.scrap >= ammo, new Color32(70, 100, 150, 255), () =>
            {
                if (ZLShop.Spend(p, ammo)) { ZLShop.MaxAmmo(p); _g.Player.CancelReload(); Bought("EVERY GUN FULL."); }
            });
            Item(r1, "MEDKIT", $"{p.medkits}/{ZLRules.MaxMedkits} · {med}", p.scrap >= med && p.medkits < ZLRules.MaxMedkits, new Color32(150, 60, 60, 255), () =>
            {
                if (p.medkits < ZLRules.MaxMedkits && ZLShop.Spend(p, med)) { p.medkits++; Bought("+1 MEDKIT."); }
            });
            Item(r1, "ARMOR", $"Full vest · {arm}", p.scrap >= arm && p.armor < ZLRules.MaxArmor, new Color32(60, 80, 140, 255), () =>
            {
                if (p.armor < ZLRules.MaxArmor && ZLShop.Spend(p, arm)) { p.armor = ZLRules.MaxArmor; Bought("ARMOR ON."); }
            });

            // Perks
            UIFactory.Label(panel, $"PERKS  {p.perks.Count}/{ZLShop.PerkLimit}   ·   kept when you fall (Second Chance is used up)", 30,
                            TextAnchor.MiddleCenter, 44f).color = UITheme.Current.subtle;
            var r2 = Row(panel, 120f);
            foreach (ZLPerk k in System.Enum.GetValues(typeof(ZLPerk)))
            {
                var kk = k;
                bool owned = ZLShop.HasPerk(p, k);
                int cost = Price(ZLShop.PerkCost(k));
                bool can = !owned && p.perks.Count < ZLShop.PerkLimit && p.scrap >= cost;
                string sub = owned ? "OWNED" : $"{ZLShop.PerkInfo(k)} · {cost}";
                Item(r2, ZLShop.PerkName(k), sub, can || owned, owned ? new Color32(60, 140, 90, 255) : new Color32(90, 60, 110, 255), () =>
                {
                    if (owned) return;
                    if (ZLShop.BuyPerk(p, kk, Discount))
                    {
                        if (kk == ZLPerk.ThickSkin) _g.Player.Heal(50);
                        Bought(ZLShop.PerkName(kk) + "!");
                    }
                    else { _message = p.perks.Count >= ZLShop.PerkLimit ? "FOUR PERKS AT A TIME." : "NOT ENOUGH SCRAP."; Build(); }
                });
            }

            // Box and forge
            var r3 = Row(panel, 120f);
            int box = Price(ZLShop.BoxCost), forge = Price(ZLShop.ForgeCost);
            Item(r3, "MYSTERY BOX", $"Any gun, maybe the Storm Coil · {box}", p.scrap >= box, new Color32(110, 60, 140, 255), () =>
            {
                var prize = ZLShop.SpinBox(p, Random.value, box);
                if (prize == null) { _message = "NOT ENOUGH SCRAP."; Build(); return; }
                _g.Player.CancelReload();
                Bought((prize.wonder ? "THE WONDER WEAPON! " : "") + prize.name + "!");
                if (prize.wonder) Sfx.Play(Sfx.Relic, 1f, 0f);
            });
            var held = ZLCatalog.Gun(p.equipped);
            bool canForge = ZLShop.CanForge(p, held);
            string fsub = held == null ? "Hold a gun" : held.isRelic ? "Relics can't be forged" : !canForge ? "Already forged" : $"{held.name}: x2 damage, more ammo · {forge}";
            Item(r3, "UPGRADE FORGE", fsub, canForge && p.scrap >= forge, new Color32(140, 90, 200, 255), () =>
            {
                if (held != null && ZLShop.Forge(p, held.id, forge)) { _g.Player.CancelReload(); Bought("HOLLOW-FORGED " + held.name + "!"); Sfx.Play(Sfx.Special, 0.9f, 0f); }
            });

            var msg = UIFactory.Label(panel, _message, 34, TextAnchor.MiddleCenter, 56f, title: true);
            msg.color = Gold;
            var close = UIFactory.Button(panel, "BACK TO THE STREETS", Close);
            UIFactory.Select(close);
        }

        void Bought(string what)
        {
            Sfx.Play(Sfx.Purchase, 0.9f, 0f);
            _message = what;
            Build();
        }

        void Update()
        {
            if (IsOpen && _g == null) Close();
        }
    }
}
