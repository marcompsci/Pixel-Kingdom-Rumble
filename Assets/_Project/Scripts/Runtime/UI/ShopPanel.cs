using System;
using System.Collections.Generic;
using PKR.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// Star Shard shop (portrait, opened from the main menu): pick a hero with the arrows, see their palettes with a
    /// tinted preview, buy one (it's worn straight away), switch between owned ones, or go back to the default look.
    /// Cosmetic only. Purchases save immediately.
    /// </summary>
    public class ShopPanel : MonoBehaviour
    {
        RectTransform _container;
        CharacterRoster _roster;
        Action _onBack;
        int _heroIndex;
        string _status = "";
        readonly List<CharacterDefinition> _heroes = new List<CharacterDefinition>();

        public int HeroCount => _heroes.Count;
        public CharacterDefinition CurrentHero => _heroes.Count > 0 ? _heroes[_heroIndex] : null;
        public string Status => _status;

        public void Open(RectTransform container, CharacterRoster roster, Action onBack)
        {
            _container = container;
            _roster = roster;
            _onBack = onBack;
            _status = "";
            _heroes.Clear();
            if (roster != null && roster.cosmetics != null)
                foreach (var h in roster.heroes)
                    // Only heroes you've unlocked: the shop shouldn't reveal locked ones early.
                    if (h != null && roster.IsSelectable(h) && roster.cosmetics.ForHero(h.id).Count > 0) _heroes.Add(h);
            // Start on the hero you last played.
            _heroIndex = Mathf.Max(0, _heroes.FindIndex(h => h.id == GameSession.SelectedCharacterId));
            Rebuild();
        }

        /// <summary>Buy (and wear) or wear a palette. Returns the purchase result (Success when just equipping).</summary>
        public PurchaseResult Choose(CosmeticDefinition item)
        {
            var save = Services.Save;
            if (save == null || item == null || _roster == null || _roster.cosmetics == null) return PurchaseResult.InvalidItem;
            var catalog = _roster.cosmetics.AsItems();
            var result = PurchaseResult.Success;
            if (!Wardrobe.Owns(save.Data, item.id))
            {
                result = Wardrobe.Buy(save.Data, new CosmeticItem { id = item.id, heroId = item.heroId, cost = item.cost });
                if (result == PurchaseResult.NotEnoughShards)
                {
                    _status = $"YOU NEED {item.cost - save.Data.starShards} MORE STAR SHARDS";
                    if (Services.Haptics != null) Services.Haptics.Play(HapticStrength.Light);
                    Rebuild();
                    return result;
                }
                if (result != PurchaseResult.Success && result != PurchaseResult.AlreadyOwned) { Rebuild(); return result; }
                _status = $"BOUGHT {item.displayName.ToUpperInvariant()}!";
                if (Services.Haptics != null) Services.Haptics.Play(HapticStrength.Medium);
                if (result == PurchaseResult.Success)
                    EventBus<ShardsChanged>.Raise(new ShardsChanged { total = save.Data.starShards, delta = -item.cost });
            }
            else _status = $"WEARING {item.displayName.ToUpperInvariant()}";
            Wardrobe.Equip(save.Data, item.id, catalog);
            save.MarkDirty();
            save.SaveNow();
            Rebuild();
            return result;
        }

        public void WearDefault()
        {
            var save = Services.Save;
            var hero = CurrentHero;
            if (save == null || hero == null || _roster.cosmetics == null) return;
            Wardrobe.UnequipHero(save.Data, hero.id, _roster.cosmetics.AsItems());
            save.MarkDirty();
            save.SaveNow();
            _status = "DEFAULT LOOK";
            Rebuild();
        }

        public void StepHero(int dir)
        {
            if (_heroes.Count == 0) return;
            _heroIndex = RosterSelection.Step(_heroIndex, dir, _heroes.Count);
            _status = "";
            Rebuild();
        }

        void Rebuild()
        {
            if (_container == null) return;
            UIFactory.Clear(_container);
            var p = UITheme.Current;
            var panel = UIFactory.Panel(_container, new Vector2(1000f, 1780f));
            UIFactory.Label(panel, "STAR SHARD SHOP", 64, TextAnchor.MiddleCenter, 100f, title: true);
            var save = Services.Save != null ? Services.Save.Data : null;
            UIFactory.Label(panel, $"STAR SHARDS  {(save != null ? save.starShards : 0)}", 46, TextAnchor.MiddleCenter, 70f).color = p.title;
            UIFactory.Label(panel, "Palettes are cosmetic only. They show in Story Quest, Arena Clash and Character Select.",
                            28, TextAnchor.MiddleCenter, 70f).color = p.subtle;

            var hero = CurrentHero;
            if (hero == null)
            {
                UIFactory.Label(panel, "Nothing for sale yet. Unlock heroes to see their palettes.", 40, TextAnchor.MiddleCenter, 120f);
                UIFactory.Button(panel, "BACK", () => _onBack?.Invoke());
                return;
            }

            // Hero picker with a tinted preview of what they're wearing now.
            var row = UIFactory.Rect("HeroRow", panel);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 360f;
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 20f; h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = false; h.childForceExpandHeight = true;
            Arrow(row, "<", -1);
            var previewRt = UIFactory.Rect("Preview", row);
            previewRt.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var preview = previewRt.gameObject.AddComponent<Image>();
            preview.sprite = hero.bodySprite;
            preview.preserveAspect = true;
            preview.raycastTarget = false;
            preview.color = save != null && _roster.cosmetics != null ? _roster.cosmetics.TintFor(save, hero.id) : Color.white;
            Arrow(row, ">", +1);
            UIFactory.Label(panel, hero.displayName.ToUpperInvariant(), 56, TextAnchor.MiddleCenter, 80f, title: true);

            string equipped = save != null ? Wardrobe.EquippedFor(save, hero.id, _roster.cosmetics.AsItems()) : null;
            UIFactory.Button(panel, equipped == null ? "DEFAULT LOOK  ·  WEARING" : "DEFAULT LOOK", WearDefault, 110f);
            foreach (var item in _roster.cosmetics.ForHero(hero.id))
            {
                bool owned = save != null && Wardrobe.Owns(save, item.id);
                string state = item.id == equipped ? "WEARING" : owned ? "OWNED · TAP TO WEAR" : $"{item.cost} SHARDS";
                var it = item;
                var b = UIFactory.Button(panel, $"{item.displayName.ToUpperInvariant()}  ·  {state}", () => Choose(it), 110f);
                var label = b.GetComponentInChildren<Text>();
                label.fontSize = 38;
                label.rectTransform.offsetMin = new Vector2(100f, 0f); // leave room for the color swatch
                Swatch((RectTransform)b.transform, item.tint);
            }

            var status = UIFactory.Label(panel, _status, 34, TextAnchor.MiddleCenter, 60f);
            status.color = p.accent;
            var back = UIFactory.Button(panel, "BACK", () => _onBack?.Invoke());
            UIFactory.Select(back);
        }

        void Arrow(RectTransform parent, string label, int dir)
        {
            var b = UIFactory.Button(parent, label, () => StepHero(dir));
            var le = b.GetComponent<LayoutElement>();
            le.preferredWidth = 120f;
            le.flexibleWidth = 0f;
        }

        static void Swatch(RectTransform button, Color tint)
        {
            var rt = UIFactory.Rect("Swatch", button);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(20f, 0f);
            rt.sizeDelta = new Vector2(60f, 60f);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = UISprites.Circle;
            img.color = tint;
            img.raycastTarget = false;
        }
    }
}
