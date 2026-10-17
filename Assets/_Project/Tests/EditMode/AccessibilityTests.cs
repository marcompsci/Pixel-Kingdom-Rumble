using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    /// <summary>Accessibility options: palettes that stay apart, mirrored controls, motion and settings upgrades.</summary>
    [TestFixture]
    public class AccessibilityTests
    {
        [Test]
        public void EachPalette_KeepsDangerAndGoodApart_ForTheVisionItIsFor()
        {
            foreach (var mode in new[] { ColorAssist.RedGreen, ColorAssist.BlueYellow })
            {
                var danger = Accessibility.Color(ColorRole.Danger, mode);
                var good = Accessibility.Color(ColorRole.Good, mode);
                var warn = Accessibility.Color(ColorRole.Warning, mode);
                Assert.Greater(Accessibility.SeenDifference(danger, good, mode), 120f, $"{mode}: danger vs good");
                Assert.Greater(Accessibility.SeenDifference(warn, good, mode), 80f, $"{mode}: warning vs good");
            }
        }

        [Test]
        public void RedGreenPalette_BeatsTheDefault_ForRedGreenVision()
        {
            var vision = ColorAssist.RedGreen;
            float before = Accessibility.SeenDifference(Accessibility.Color(ColorRole.Danger, ColorAssist.Off), Accessibility.Color(ColorRole.Good, ColorAssist.Off), vision);
            float after = Accessibility.SeenDifference(Accessibility.Color(ColorRole.Danger, vision), Accessibility.Color(ColorRole.Good, vision), vision);
            Assert.Greater(after, before * 1.5f, "the standard red/green pair is hard to tell apart; the palette fixes it");
        }

        [Test]
        public void SquadColours_AreDistinct_InEveryPalette()
        {
            foreach (ColorAssist mode in System.Enum.GetValues(typeof(ColorAssist)))
                for (int a = 0; a < 4; a++)
                    for (int b = a + 1; b < 4; b++)
                    {
                        var ca = Accessibility.Color(Accessibility.SquadRole(a), mode);
                        var cb = Accessibility.Color(Accessibility.SquadRole(b), mode);
                        Assert.Greater(Accessibility.SeenDifference(ca, cb, mode), 40f, $"{mode}: seats {a} and {b}");
                    }
            Assert.AreEqual(ColorRole.Squad1, Accessibility.SquadRole(5));
            Assert.AreEqual(ColorRole.Squad3, Accessibility.SquadRole(-1));
        }

        [Test]
        public void LeftHanded_MirrorsTwice_BackToTheStart()
        {
            Assert.AreEqual(0.2f, Accessibility.MirrorX(0.8f, true), 1e-5f);
            Assert.AreEqual(0.8f, Accessibility.MirrorX(0.8f, false), 1e-5f);
            Assert.AreEqual(0.35f, Accessibility.MirrorX(Accessibility.MirrorX(0.35f, true), true), 1e-5f);
        }

        [Test]
        public void Motion_AndTargets()
        {
            Assert.AreEqual(1f, Accessibility.ShakeScale(true, false));
            Assert.AreEqual(0f, Accessibility.ShakeScale(false, false));
            Assert.AreEqual(0f, Accessibility.ShakeScale(true, true), "reduced motion stops shake even with shake on");
            Assert.Less(Accessibility.FlashScale(true), 1f);
            Assert.AreEqual(1f, Accessibility.FlashScale(false));
            Assert.Greater(Accessibility.HitPad(true), 1.2f);
            Assert.AreEqual(1f, Accessibility.HitPad(false));
        }

        [Test]
        public void Settings_NewFieldsDefaultOff_AndSanitize()
        {
            var s = new SettingsData();
            Assert.AreEqual((int)ColorAssist.Off, s.colorAssist);
            Assert.IsFalse(s.reducedMotion || s.leftHanded || s.bigTouchTargets || s.soundCues);
            s.colorAssist = 7;
            s.Sanitize();
            Assert.AreEqual((int)ColorAssist.Off, s.colorAssist);
            var old = new SettingsData { version = 2, musicVolume = 0.5f };
            old.Sanitize();
            Assert.AreEqual(0.5f, old.musicVolume, 1e-5f);
            Assert.IsFalse(old.reducedMotionChecked, "an old save hasn't copied iOS Reduce Motion yet");
            foreach (ColorAssist m in System.Enum.GetValues(typeof(ColorAssist)))
            {
                Assert.IsFalse(string.IsNullOrEmpty(Accessibility.Name(m)));
                Assert.IsFalse(string.IsNullOrEmpty(Accessibility.Info(m)));
            }
        }
    }
}
