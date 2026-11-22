using System;
using System.Collections.Generic;
using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    [TestFixture]
    public class PlayerMeta7Tests
    {
        [Test]
        public void WeekStreak_CountsConsecutiveWeeks()
        {
            var s = SaveData.CreateNew();
            var mon = new DateTime(2026, 11, 2);
            Assert.AreEqual(1, WeekStreak.Update(s, mon));
            Assert.AreEqual(1, WeekStreak.Update(s, mon.AddDays(3)), "same week");
            Assert.AreEqual(2, WeekStreak.Update(s, mon.AddDays(8)));
            Assert.AreEqual(3, WeekStreak.Update(s, mon.AddDays(14)));
            Assert.AreEqual(1, WeekStreak.Update(s, mon.AddDays(35)), "a missed week resets");
            Assert.AreEqual(3, s.lifetime.bestWeekStreak);
        }

        [Test]
        public void Shop_PurchasesAffordableAndGoal()
        {
            var s = SaveData.CreateNew();
            s.starShards = 0;
            Assert.AreEqual(0, ShopMeta.AffordableUpgrades(s));
            StringAssert.Contains("shards to go", ShopMeta.NextUpgradeGoal(s));
            s.starShards = 100000;
            Assert.AreEqual(Upgrades.All.Length, ShopMeta.AffordableUpgrades(s));
            Assert.AreEqual("", ShopMeta.NextUpgradeGoal(s), "nothing to save for");
            int before = ShopMeta.Purchases(s);
            Upgrades.Buy(s, Upgrades.All[0].id);
            Assert.AreEqual(before + 1, ShopMeta.Purchases(s));
        }

        [Test]
        public void HeroMeta_StatusFavoriteRandom()
        {
            var s = SaveData.CreateNew();
            HeroMastery.Add(s, "luma", 500);
            HeroRecords.Record(s, "luma", true);
            var line = HeroMeta.Status(s, "luma");
            StringAssert.Contains("FAVORITE", line);
            StringAssert.Contains("VERSUS 1W-0L", line);
            StringAssert.DoesNotContain("FAVORITE", HeroMeta.Status(s, "rex"));
            var ok = new List<bool> { true, false, true, true };
            var rng = new Random(3);
            for (int i = 0; i < 20; i++)
            {
                int p = HeroMeta.RandomPick(ok, 0, rng);
                Assert.IsTrue(p == 2 || p == 3, "a different selectable hero");
            }
            Assert.AreEqual(0, HeroMeta.RandomPick(new List<bool> { true, false }, 0, rng), "only one choice");
            Assert.AreEqual(-1, HeroMeta.RandomPick(new List<bool> { false }, 0, rng));
        }

        [Test]
        public void Rivalry_GrudgeMostBeatenOverall()
        {
            var s = SaveData.CreateNew();
            Assert.AreEqual("", Rivalry.Overall(s));
            Assert.IsTrue(Rivalry.IsGrudge(new HeadToHeadRecord { wins = 1, losses = 4 }));
            Assert.IsFalse(Rivalry.IsGrudge(new HeadToHeadRecord { wins = 2, losses = 4 }));
            HeadToHead.Record(s, "a", true); HeadToHead.Record(s, "b", true); HeadToHead.Record(s, "b", true);
            Assert.AreEqual("b", Rivalry.MostBeaten(s).id);
            HeroRecords.Record(s, "luma", true); HeroRecords.Record(s, "luma", false);
            StringAssert.Contains("1W-1L", Rivalry.Overall(s));
        }

        [Test]
        public void StruggleTwoTapShareAndBadges()
        {
            Assert.IsTrue(StruggleHint.Due(3, false, false));
            Assert.IsFalse(StruggleHint.Due(3, true, false), "already assisted");
            Assert.IsFalse(StruggleHint.Due(5, false, true), "once");
            Assert.IsFalse(StruggleHint.Due(2, false, false));
            Assert.IsTrue(TwoTap.Confirmed(10f, 11f));
            Assert.IsFalse(TwoTap.Confirmed(10f, 14f));
            Assert.IsFalse(TwoTap.Confirmed(0f, 0.5f));
            StringAssert.Contains("3/10 levels cleared", StoryShare.Text(3, 10, 1, 0, 95f));
            var s = SaveData.CreateNew();
            for (int i = 0; i < 30; i++) s.unlockedCodexEntries.Add("c" + i);
            for (int i = 0; i < 6; i++) s.UnlockCharacter("h" + i);
            s.lifetime.bestWeekStreak = 4;
            Badges.Update(s);
            foreach (var id in new[] { "codex30", "heroes6", "weeks4" }) Assert.IsTrue(Badges.Has(s, id), id);
            Assert.GreaterOrEqual(Badges.All.Length, 36);
            Assert.GreaterOrEqual(TipOfTheDay.Tips.Length, 28);
        }
    }
}
