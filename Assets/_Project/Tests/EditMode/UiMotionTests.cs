using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    /// <summary>The UI easing curves and timings (Emil Kowalski's design-engineering standards, applied to uGUI).</summary>
    [TestFixture]
    public class UiMotionTests
    {
        [Test]
        public void Curves_StartAtZero_EndAtOne_AndRise()
        {
            foreach (System.Func<float, float> f in new System.Func<float, float>[] { Ease.Out, Ease.InOut, Ease.Drawer })
            {
                Assert.AreEqual(0f, f(0f), 1e-5f);
                Assert.AreEqual(1f, f(1f), 1e-5f);
                Assert.AreEqual(0f, f(-3f), 1e-5f, "clamped below");
                Assert.AreEqual(1f, f(7f), 1e-5f, "clamped above");
                Assert.AreEqual(0f, f(float.NaN), 1e-5f);
                float prev = 0f;
                for (int i = 1; i <= 100; i++)
                {
                    float v = f(i / 100f);
                    Assert.GreaterOrEqual(v + 1e-4f, prev, "never goes backwards");
                    prev = v;
                }
            }
        }

        [Test]
        public void EaseOut_IsStrong_AnswersAtOnce()
        {
            // a strong ease-out is most of the way there early on (the user sees movement immediately)
            Assert.Greater(Ease.Out(0.2f), 0.6f);
            Assert.Greater(Ease.Out(0.5f), 0.9f);
            // and stronger than a plain cubic ease-out
            float cubic = 1f - (1f - 0.2f) * (1f - 0.2f) * (1f - 0.2f);
            Assert.Greater(Ease.Out(0.2f), cubic);
        }

        [Test]
        public void EaseInOut_IsSlowAtTheEnds_FastInTheMiddle()
        {
            // cubic-bezier(0.77, 0, 0.175, 1), checked against an independent solver
            Assert.AreEqual(0.5960f, Ease.InOut(0.5f), 0.002f);
            Assert.AreEqual(0.0305f, Ease.InOut(0.2f), 0.002f);
            Assert.Less(Ease.InOut(0.1f), 0.05f);
            Assert.Greater(Ease.InOut(0.9f), 0.95f);
        }

        [Test]
        public void Bezier_MatchesKnownPoints()
        {
            // the linear bezier is the identity
            for (int i = 0; i <= 10; i++) Assert.AreEqual(i / 10f, Ease.Bezier(0.25f, 0.25f, 0.75f, 0.75f, i / 10f), 1e-3f);
            // CSS "ease" (0.25, 0.1, 0.25, 1) at 0.5 is about 0.8024
            Assert.AreEqual(0.8024f, Ease.Bezier(0.25f, 0.1f, 0.25f, 1f, 0.5f), 0.002f);
        }

        [Test]
        public void Timings_FollowTheRules()
        {
            Assert.Less(Ease.Enter, 0.3f, "UI motion stays under 300 ms");
            Assert.Less(Ease.Exit, Ease.Enter, "exits are faster than entrances");
            Assert.LessOrEqual(Ease.Quick, 0.16f, "press and page feedback 100-160 ms");
            Assert.Less(Ease.Cover + Ease.Reveal, 0.45f, "a screen change adds under half a second");
            Assert.GreaterOrEqual(Ease.ModalFrom, 0.9f, "never grow from nothing");
            Assert.Less(Ease.ModalFrom, 1f);
        }

        [Test]
        public void Presence_EntersThenLeaves()
        {
            var (enter, exit) = Ease.Presence(0f, 3f, 0.18f, 0.14f);
            Assert.AreEqual(0f, enter, 1e-4f);
            Assert.AreEqual(1f, exit, 1e-4f);
            (enter, exit) = Ease.Presence(0.18f, 2f, 0.18f, 0.14f);
            Assert.AreEqual(1f, enter, 1e-4f);
            (enter, exit) = Ease.Presence(5f, 0.07f, 0.18f, 0.14f);
            Assert.Less(exit, 0.5f, "half the exit time left: already mostly gone (it starts leaving fast)");
            Assert.Greater(exit, 0f);
            (enter, exit) = Ease.Presence(5f, 0f, 0.18f, 0.14f);
            Assert.AreEqual(0f, exit, 1e-4f);
        }

        [Test]
        public void Plural_ReadsRight()
        {
            Assert.AreEqual("1 kill", Plural.Of(1, "kill"));
            Assert.AreEqual("0 kills", Plural.Of(0, "kill"));
            Assert.AreEqual("12 kills", Plural.Of(12, "kill"));
            Assert.AreEqual("1 Star Shard", Plural.Of(1, "Star Shard"));
            Assert.AreEqual("3 zombies", Plural.Of(3, "zombie"));
            Assert.AreEqual("2 geese", Plural.Of(2, "goose", "geese"));
        }
    }
}
