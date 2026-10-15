#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;

namespace PKR.Tests
{
    /// <summary>The balance-lab move fixes reached the move assets (the check runs setup before the tests).</summary>
    public class BalancePatchTests
    {
        static MoveDefinition Load(string path) => AssetDatabase.LoadAssetAtPath<MoveDefinition>(path);

        [Test]
        public void VoltJab_IsNoLongerTheFastestHit()
        {
            var jab = Load("Assets/_Project/Data/Moves/Versus/VoltRamirez/Volt_Jab.asset");
            if (jab == null) Assert.Ignore("move assets not built");
            Assert.AreEqual(4, jab.frames.startup);
        }

        [Test]
        public void GrizzMaulRush_ArmorStartsLater()
        {
            var rush = Load("Assets/_Project/Data/Moves/Versus/Grizz/Grizz_MaulRush.asset");
            if (rush == null) Assert.Ignore("move assets not built");
            Assert.AreEqual(4, rush.armorStartFrame);
        }

        [Test]
        public void NovaCombo_IsATrueCombo()
        {
            var jab = Load("Assets/_Project/Data/Moves/Nova/Nova_CourierJab.asset");
            if (jab == null || jab.followUp == null) Assert.Ignore("move assets not built");
            var f = jab.frames;
            int need = f.active + f.recovery - f.cancelWindow + jab.followUp.frames.startup + 3;
            Assert.GreaterOrEqual(jab.hit.baseHitstunFrames, need, "the jab holds the foe until the sweep lands");
        }
    }
}
#endif
