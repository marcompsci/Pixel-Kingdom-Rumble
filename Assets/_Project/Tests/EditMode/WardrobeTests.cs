using System.Collections.Generic;
using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    [TestFixture]
    public class WardrobeTests
    {
        static readonly List<CosmeticItem> Catalog = new List<CosmeticItem>
        {
            new CosmeticItem { id = "nova_dawn", heroId = "nova", cost = 60 },
            new CosmeticItem { id = "nova_nebula", heroId = "nova", cost = 120 },
            new CosmeticItem { id = "brick_moss", heroId = "brick", cost = 60 },
        };

        static SaveData Rich(int shards)
        {
            var s = SaveData.CreateNew();
            s.starShards = shards;
            return s;
        }

        [Test]
        public void Buy_SpendsShards_OnlyOnce()
        {
            var s = Rich(100);
            Assert.AreEqual(PurchaseResult.Success, Wardrobe.Buy(s, Catalog[0]));
            Assert.AreEqual(40, s.starShards);
            Assert.AreEqual(PurchaseResult.AlreadyOwned, Wardrobe.Buy(s, Catalog[0]));
            Assert.AreEqual(PurchaseResult.NotEnoughShards, Wardrobe.Buy(s, Catalog[1]));
            Assert.AreEqual(40, s.starShards);
        }

        [Test]
        public void Equip_NeedsOwnership_AndSwapsWithinAHero()
        {
            var s = Rich(1000);
            Assert.IsFalse(Wardrobe.Equip(s, "nova_dawn", Catalog), "not owned yet");
            Wardrobe.Buy(s, Catalog[0]);
            Wardrobe.Buy(s, Catalog[1]);
            Wardrobe.Buy(s, Catalog[2]);
            Assert.IsTrue(Wardrobe.Equip(s, "nova_dawn", Catalog));
            Assert.IsTrue(Wardrobe.Equip(s, "brick_moss", Catalog));
            Assert.IsTrue(Wardrobe.Equip(s, "nova_nebula", Catalog));
            Assert.AreEqual("nova_nebula", Wardrobe.EquippedFor(s, "nova", Catalog), "one palette per hero");
            Assert.AreEqual("brick_moss", Wardrobe.EquippedFor(s, "brick", Catalog), "other heroes untouched");
            Assert.IsFalse(s.equippedCosmetics.Contains("nova_dawn"));
        }

        [Test]
        public void Unequip_ReturnsToDefault_UnknownIdsIgnored()
        {
            var s = Rich(1000);
            Wardrobe.Buy(s, Catalog[0]);
            Wardrobe.Equip(s, "nova_dawn", Catalog);
            Wardrobe.UnequipHero(s, "nova", Catalog);
            Assert.IsNull(Wardrobe.EquippedFor(s, "nova", Catalog));
            s.ownedCosmetics.Add("mystery");
            Assert.IsFalse(Wardrobe.Equip(s, "mystery", Catalog), "not in the catalog");
            Assert.IsNull(Wardrobe.EquippedFor(s, "luma", Catalog));
        }
    }
}
