using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    [TestFixture]
    public class RosterSelectionTests
    {
        [Test]
        public void Wrap_HandlesNegativesAndOverflow()
        {
            Assert.AreEqual(3, RosterSelection.Wrap(-1, 4));
            Assert.AreEqual(0, RosterSelection.Wrap(4, 4));
            Assert.AreEqual(2, RosterSelection.Wrap(-6, 4));
            Assert.AreEqual(0, RosterSelection.Wrap(5, 0));
        }

        [Test]
        public void Step_WrapsBothWays()
        {
            Assert.AreEqual(1, RosterSelection.Step(0, 1, 4));
            Assert.AreEqual(3, RosterSelection.Step(0, -1, 4));
            Assert.AreEqual(0, RosterSelection.Step(3, 1, 4));
        }

        [Test]
        public void Initial_PrefersSavedSelectableHero()
        {
            var sel = new[] { true, false, true, false };
            Assert.AreEqual(2, RosterSelection.Initial(sel, 2));
            Assert.AreEqual(0, RosterSelection.Initial(sel, 1));   // locked -> first selectable
            Assert.AreEqual(0, RosterSelection.Initial(sel, 9));   // out of range
            Assert.AreEqual(0, RosterSelection.Initial(new[] { false, false }, 1));
            Assert.AreEqual(0, RosterSelection.Initial(null, 1));
        }

        [Test]
        public void IndexOf_FindsIds()
        {
            var ids = new[] { "nova", "brick", "luma", "rex_rollo" };
            Assert.AreEqual(3, RosterSelection.IndexOf(ids, "rex_rollo"));
            Assert.AreEqual(-1, RosterSelection.IndexOf(ids, "nobody"));
            Assert.AreEqual(-1, RosterSelection.IndexOf(ids, null));
        }
    
        [Test]
        public void PickRandom_OnlyPicksSelectable()
        {
            var sel = new[] { true, true, false, false };
            var rng = new System.Random(3);
            bool sawNova = false, sawBrick = false;
            for (int i = 0; i < 50; i++)
            {
                int k = RosterSelection.PickRandom(sel, rng);
                Assert.IsTrue(k == 0 || k == 1);
                if (k == 0) sawNova = true; else sawBrick = true;
            }
            Assert.IsTrue(sawNova && sawBrick);
            Assert.AreEqual(-1, RosterSelection.PickRandom(new[] { false, false }, rng));
            Assert.AreEqual(-1, RosterSelection.PickRandom(null, rng));
        }
    }
}
