using System.Collections.Generic;
using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    [TestFixture]
    public class TrapLogicTests
    {
        [Test]
        public void Trap_FallsArmsThenTriggersOnce()
        {
            var t = new TrapLogic(0.3f, 5f);
            Assert.AreEqual(TrapPhase.Falling, t.Phase);
            Assert.IsFalse(t.TryTrigger(), "can't fire while falling");
            t.Tick(0.5f);
            t.Land();
            Assert.AreEqual(TrapPhase.Arming, t.Phase);
            t.Tick(0.2f);
            Assert.IsFalse(t.TryTrigger(), "can't fire while arming");
            t.Tick(0.2f);
            Assert.IsTrue(t.IsArmed);
            Assert.IsTrue(t.TryTrigger());
            Assert.IsTrue(t.IsSpent);
            Assert.IsFalse(t.TryTrigger(), "only once");
        }

        [Test]
        public void Trap_ExpiresAfterLifetime_AndWhenNeverLanding()
        {
            var t = new TrapLogic(0f, 2f);
            t.Land();
            Assert.IsTrue(t.IsArmed, "no arm delay = armed on landing");
            t.Tick(1.5f);
            Assert.AreEqual(0.5f, t.TimeLeft, 0.001f);
            t.Tick(0.6f);
            Assert.IsTrue(t.IsSpent);

            var lost = new TrapLogic(0.3f, 5f, maxFallTime: 1f);
            lost.Tick(1.1f);
            Assert.IsTrue(lost.IsSpent, "fell off the stage");
            lost.Land();
            Assert.IsTrue(lost.IsSpent, "landing after being spent does nothing");
        }

        [Test]
        public void Trap_LargeTickCarriesOverArming()
        {
            var t = new TrapLogic(0.3f, 1f);
            t.Land();
            t.Tick(0.8f); // 0.3 arming + 0.5 armed
            Assert.IsTrue(t.IsArmed);
            Assert.AreEqual(0.5f, t.TimeLeft, 0.001f);
            t.Expire();
            Assert.IsTrue(t.IsSpent);
        }
    }

    [TestFixture]
    public class UnlockRulesTests
    {
        static readonly UnlockRule Luma = new UnlockRule { characterId = "luma", clearLevelId = "sq_sunspire_test" };

        [Test]
        public void ClearingTheLevel_UnlocksOnce()
        {
            var save = SaveData.CreateNew();
            Assert.IsFalse(UnlockRules.IsMet(save, Luma));
            Assert.AreEqual(0, UnlockRules.Apply(save, new List<UnlockRule> { Luma }).Count);

            save.RecordLevelResult("sq_sunspire_test", 90f, 10, 1);
            var first = UnlockRules.Apply(save, new List<UnlockRule> { Luma });
            Assert.AreEqual(1, first.Count);
            Assert.AreEqual("luma", first[0]);
            Assert.IsTrue(save.IsCharacterUnlocked("luma"));
            Assert.AreEqual(0, UnlockRules.Apply(save, new List<UnlockRule> { Luma }).Count, "already unlocked");
        }

        [Test]
        public void EmptyRules_NeverUnlock()
        {
            var save = SaveData.CreateNew();
            save.RecordLevelResult("sq_sunspire_test", 90f, 10, 1);
            var rules = new List<UnlockRule> { new UnlockRule { characterId = "rex_rollo", clearLevelId = "" } };
            Assert.AreEqual(0, UnlockRules.Apply(save, rules).Count);
            Assert.AreEqual(0, UnlockRules.Apply(save, null).Count);
        }
    }
}
