using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    [TestFixture]
    public class ResultsMathTests
    {
        [Test]
        public void FormatTime_MinutesSecondsTenths()
        {
            Assert.AreEqual("0:00.0", ResultsMath.FormatTime(0f));
            Assert.AreEqual("1:05.3", ResultsMath.FormatTime(65.34f));
            Assert.AreEqual("0:59.9", ResultsMath.FormatTime(59.99f)); // truncates, never shows 0:60.0
            Assert.AreEqual("12:00.0", ResultsMath.FormatTime(720f));
            Assert.AreEqual("0:00.0", ResultsMath.FormatTime(-4f));
            Assert.AreEqual("0:00.0", ResultsMath.FormatTime(float.NaN));
        }

        [Test]
        public void Percent_ClampsAndHandlesZeroTotal()
        {
            Assert.AreEqual(50, ResultsMath.Percent(12, 24));
            Assert.AreEqual(100, ResultsMath.Percent(30, 24)); // enemy drops can exceed placed shards
            Assert.AreEqual(100, ResultsMath.Percent(0, 0));
            Assert.AreEqual(0, ResultsMath.Percent(-3, 10));
        }

        [Test]
        public void Rank_PerfectRunIsS()
        {
            Assert.AreEqual(ClearRank.S, ResultsMath.Rank(90f, 120f, 24, 24, 1, 1, 0));
        }

        [Test]
        public void Rank_SlowAndSparseIsC()
        {
            Assert.AreEqual(ClearRank.C, ResultsMath.Rank(300f, 120f, 2, 24, 0, 1, 4));
        }

        [Test]
        public void Rank_MiddleCases()
        {
            // On par, all shards, no secret, no deaths: 0.35 + 0.35 + 0 + 0.15 = 0.85 -> A
            Assert.AreEqual(ClearRank.A, ResultsMath.Rank(120f, 120f, 24, 24, 0, 1, 0));
            // 1.5x par (0.5 time), half shards, secret, 1 death: 0.175 + 0.175 + 0.15 + 0.1125 = 0.6125 -> B
            Assert.AreEqual(ClearRank.B, ResultsMath.Rank(180f, 120f, 12, 24, 1, 1, 1));
        }
    }
}
