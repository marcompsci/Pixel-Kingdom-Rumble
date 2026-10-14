using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    [TestFixture]
    public class ShareCardTests
    {
        [Test]
        public void DailyNumber_CountsDaysFromLaunch()
        {
            Assert.AreEqual(1, ShareCard.DailyNumber(20261001));
            Assert.AreEqual(13, ShareCard.DailyNumber(20261013));
            Assert.AreEqual(32, ShareCard.DailyNumber(20261101));
            Assert.AreEqual(0, ShareCard.DailyNumber(20261340));
        }

        [Test]
        public void Squares_CapAndShowZero()
        {
            Assert.AreEqual("⬜", ShareCard.Squares("X", 0));
            Assert.AreEqual("XXX", ShareCard.Squares("X", 3));
            Assert.AreEqual("XXXXXXXX +2", ShareCard.Squares("X", 10));
        }

        [Test]
        public void DailyCard_HasNumberTwistPlacementSquaresStreakAndLink()
        {
            var s = ShareCard.Format(new ShareCardInput
            {
                day = 20261013, daily = true, twist = "ORBIT", hero = "Nova", placement = 1, fighters = 4,
                knockouts = 3, falls = 1, points = 62, streak = 4, tier = "GOLD"
            });
            StringAssert.StartsWith("Pixel Kingdom Rumble · Daily #13\n", s);
            StringAssert.Contains("ORBIT", s);
            StringAssert.Contains("1st of 4 · Nova", s);
            StringAssert.Contains("\U0001F7E9\U0001F7E9\U0001F7E9 KOs", s);
            StringAssert.Contains("\U0001F7E5 falls", s);
            StringAssert.Contains("4-day streak · +62 pts · GOLD", s);
            StringAssert.EndsWith(ShareCard.Link, s);
        }

        [Test]
        public void NormalMatchCard_HasNoDailyNumberOrStreak()
        {
            var s = ShareCard.Format(new ShareCardInput { day = 20261013, placement = 3, fighters = 4, knockouts = 0, falls = 2 });
            StringAssert.DoesNotContain("Daily", s);
            StringAssert.DoesNotContain("streak", s);
            StringAssert.Contains("3rd of 4", s);
        }
    }
}
