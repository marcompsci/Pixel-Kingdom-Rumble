using System.Collections;
using NUnit.Framework;
using PKR.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace PKR.Tests
{
    /// <summary>
    /// Shop panel with a runtime catalog. Only an unaffordable purchase is attempted, so the real save is never changed.
    /// </summary>
    public class ShopPlayModeTests
    {
        GameObject _host;
        CharacterRoster _roster;

        [TearDown]
        public void TearDown()
        {
            if (_host != null) Object.Destroy(_host);
            if (_roster != null)
            {
                foreach (var h in _roster.heroes) Object.Destroy(h);
                if (_roster.cosmetics != null)
                {
                    foreach (var c in _roster.cosmetics.items) Object.Destroy(c);
                    Object.Destroy(_roster.cosmetics);
                }
                Object.Destroy(_roster);
            }
        }

        static CharacterDefinition Hero(string id)
        {
            var d = ScriptableObject.CreateInstance<CharacterDefinition>();
            d.id = id; d.displayName = id;
            d.playableInThisBuild = true; d.unlockedByDefault = true;
            return d;
        }

        static CosmeticDefinition Palette(string id, string hero, int cost)
        {
            var c = ScriptableObject.CreateInstance<CosmeticDefinition>();
            c.id = id; c.displayName = id; c.heroId = hero; c.cost = cost; c.tint = Color.red;
            return c;
        }

        [UnityTest]
        public IEnumerator Shop_ListsHeroesWithPalettes_AndRefusesUnaffordable()
        {
            _roster = ScriptableObject.CreateInstance<CharacterRoster>();
            _roster.heroes.Add(Hero("pkr_test_a"));
            _roster.heroes.Add(Hero("pkr_test_b"));
            _roster.heroes.Add(Hero("pkr_test_nothing_for_sale"));
            var locked = Hero("pkr_test_locked");
            locked.unlockedByDefault = false;
            _roster.heroes.Add(locked);
            _roster.cosmetics = ScriptableObject.CreateInstance<CosmeticCatalog>();
            _roster.cosmetics.items.Add(Palette("pkr_test_pal_a", "pkr_test_a", Economy.MaxShards + 1));
            _roster.cosmetics.items.Add(Palette("pkr_test_pal_b", "pkr_test_b", Economy.MaxShards + 1));
            _roster.cosmetics.items.Add(Palette("pkr_test_pal_locked", "pkr_test_locked", 1));

            _host = new GameObject("Shop");
            var container = new GameObject("Root", typeof(RectTransform)).GetComponent<RectTransform>();
            container.SetParent(_host.transform, false);
            var shop = _host.AddComponent<ShopPanel>();
            shop.Open(container, _roster, null);
            yield return null;

            Assert.AreEqual(2, shop.HeroCount, "only unlocked heroes with palettes are listed");
            string first = shop.CurrentHero.id;
            shop.StepHero(+1);
            Assert.AreNotEqual(first, shop.CurrentHero.id);

            int before = Services.Save != null ? Services.Save.Data.starShards : 0;
            var result = shop.Choose(_roster.cosmetics.items[0]);
            if (Services.Save != null)
            {
                Assert.AreEqual(PurchaseResult.NotEnoughShards, result);
                Assert.AreEqual(before, Services.Save.Data.starShards, "nothing spent");
                StringAssert.Contains("MORE STAR SHARDS", shop.Status);
            }
            else Assert.AreEqual(PurchaseResult.InvalidItem, result, "no save service: nothing to buy with");
        }
    }
}
