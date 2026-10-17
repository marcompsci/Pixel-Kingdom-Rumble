using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    [TestFixture]
    public class ZombieOnboardingTests
    {
        [Test]
        public void Tips_AreShort_TwoLinesAtMost_AndUniquelyPrefixed()
        {
            foreach (var kv in ZombieTips.Texts)
            {
                Assert.IsTrue(kv.Key.StartsWith("z_"), kv.Key);
                var lines = kv.Value.Split('\n');
                Assert.LessOrEqual(lines.Length, 2, kv.Key);
                foreach (var l in lines) Assert.LessOrEqual(l.Length, 44, kv.Key + ": " + l);
            }
            Assert.AreEqual(14, ZombieTips.Texts.Count);
            Assert.Greater(ZombieTips.Gap, ZombieTips.Show, "one tip at a time");
        }

        [Test]
        public void RookieWarmUp_FirstThreeRuns_FirstThreeRounds()
        {
            Assert.IsTrue(ZombieAssist.Rookie(0, 1));
            Assert.IsTrue(ZombieAssist.Rookie(2, 3));
            Assert.IsFalse(ZombieAssist.Rookie(2, 4));
            Assert.IsFalse(ZombieAssist.Rookie(3, 1));
            Assert.AreEqual(1f, ZombieAssist.SpeedScale(false, false));
            Assert.Less(ZombieAssist.SpeedScale(false, true), 1f);
            Assert.Less(ZombieAssist.SpeedScale(true, true), ZombieAssist.SpeedScale(true, false));
            Assert.Greater(ZombieAssist.SpawnScale(false, true), 1f, "rookie rounds are sparser");
            Assert.Less(ZombieAssist.HealthScale(true, false), 1f);
            Assert.AreEqual(1f, ZombieAssist.HealthScale(false, true));
        }

        [Test]
        public void StrongAimAssist_WidensTheCone()
        {
            Assert.Greater(ZombieAssist.AimCone(true), ZombieAssist.AimCone(false));
            Assert.AreEqual(75f, ZombieAssist.AimCone(false), "normal aim is what it always was");
            Assert.Greater(ZombieAssist.AimRangeBonus(true), 0f);
        }

        [Test]
        public void HudScale_IsClamped_AndOldSettingsDefault()
        {
            var s = new SettingsData { hudScale = 5f };
            s.Sanitize();
            Assert.AreEqual(SettingsData.MaxHudScale, s.hudScale);
            s.hudScale = 0f;
            s.Sanitize();
            Assert.AreEqual(1f, s.hudScale);
            var old = new SettingsData();
            old.Sanitize();
            Assert.AreEqual(1f, old.hudScale);
            Assert.IsFalse(old.strongAimAssist);
        }
    }
}
