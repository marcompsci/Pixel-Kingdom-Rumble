using System.Collections.Generic;
using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    [TestFixture]
    public class AchievementTests
    {
        [Test]
        public void StoryClear_EarnsFirstSteps_AndPerfectRunExtras()
        {
            var a = Achievements.ForClear(new ClearInfo { levelId = "sq_sunspire_meadows", shards = 30, totalShards = 30, deaths = 0 });
            Assert.IsTrue(a.Contains(Achievements.FirstSteps), Achievements.FirstSteps);
            Assert.IsTrue(a.Contains(Achievements.ShardHoarder), Achievements.ShardHoarder);
            Assert.IsTrue(a.Contains(Achievements.Untouchable), Achievements.Untouchable);
            Assert.IsFalse(a.Contains(Achievements.Warden), Achievements.Warden);
            Assert.IsFalse(a.Contains(Achievements.FirstContract), Achievements.FirstContract);
        }

        [Test]
        public void StoryClear_MissingShardsOrDeaths_NoExtras()
        {
            var a = Achievements.ForClear(new ClearInfo { levelId = "x", shards = 29, totalShards = 30, deaths = 2 });
            Assert.AreEqual(1, a.Count); Assert.AreEqual(Achievements.FirstSteps, a[0]);
        }

        [Test]
        public void WardenClear_EarnsGearsUnwound()
        {
            Assert.IsTrue(Achievements.ForClear(new ClearInfo { levelId = "sq_clockwork_warden", deaths = 1 }).Contains(Achievements.Warden));
        }

        [Test]
        public void ContractClear_RelicsCipherAndGhost()
        {
            var a = Achievements.ForClear(new ClearInfo
            {
                isContract = true, stealthRank = "GHOST", relics = 7, totalRelics = 7, clueFound = true
            });
            foreach (var id in new[] { Achievements.FirstContract, Achievements.RelicHunter, Achievements.Cipher, Achievements.Ghost }) Assert.IsTrue(a.Contains(id), id);
            Assert.IsFalse(a.Contains(Achievements.FirstSteps), Achievements.FirstSteps);

            var b = Achievements.ForClear(new ClearInfo { isContract = true, stealthRank = "SHADOW", relics = 3, totalRelics = 7 });
            Assert.AreEqual(1, b.Count); Assert.AreEqual(Achievements.FirstContract, b[0]);
        }

        [Test]
        public void AchievementIds_AreUnique_AndPointsFitGameCenterLimit()
        {
            var ids = new HashSet<string>();
            int points = 0;
            foreach (var a in Achievements.All)
            {
                Assert.IsTrue(ids.Add(a.id), "duplicate " + a.id);
                Assert.IsTrue(a.points >= 1 && a.points <= 100, a.id + " points");
                points += a.points;
            }
            Assert.LessOrEqual(points, 1000, "Game Center allows at most 1000 points per app");
        }

        [Test]
        public void Save_EarnsAchievementsAndHintsOnce()
        {
            var save = SaveData.CreateNew();
            Assert.IsTrue(save.EarnAchievement(Achievements.Ghost));
            Assert.IsFalse(save.EarnAchievement(Achievements.Ghost));
            Assert.IsTrue(save.MarkHintSeen("climb"));
            Assert.IsFalse(save.MarkHintSeen("climb"));
            save.achievements = null; save.seenHints = null;
            save.SanitizeAndMigrate();
            Assert.IsNotNull(save.achievements);
            Assert.IsNotNull(save.seenHints);
        }
    }
}
