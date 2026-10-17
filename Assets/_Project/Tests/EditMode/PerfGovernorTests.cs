using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    /// <summary>Phone performance: frame statistics and the graphics tier picker.</summary>
    [TestFixture]
    public class PerfGovernorTests
    {
        [Test]
        public void Stats_Percentiles_AndMissedFrames()
        {
            var s = new FrameStats(100);
            for (int i = 0; i < 90; i++) s.Add(1f / 60f);
            for (int i = 0; i < 10; i++) s.Add(0.05f);
            Assert.AreEqual(1f / 60f, s.Percentile(0.5f), 0.0001f);
            Assert.AreEqual(0.05f, s.Percentile(0.95f), 0.0001f);
            Assert.AreEqual(0.05f, s.Worst, 0.0001f);
            Assert.AreEqual(0.1f, s.MissedShare(1f / 60f), 0.001f);
            Assert.Greater(s.AverageFps, 40f);
            Assert.Less(s.AverageFps, 60f);
            StringAssert.Contains("p95 50.0ms", s.Summary(1f / 60f));
        }

        [Test]
        public void Stats_WindowRolls_AndStallsCountOnce()
        {
            var s = new FrameStats(10);
            for (int i = 0; i < 10; i++) s.Add(0.1f);
            for (int i = 0; i < 10; i++) s.Add(1f / 60f);
            Assert.AreEqual(10, s.Count);
            Assert.AreEqual(1f / 60f, s.Worst, 0.0001f, "old frames rolled out");
            s.Add(12f);
            Assert.AreEqual(1f, s.Worst, 0.0001f, "a stall is clamped");
            s.Add(float.NaN);
            s.Add(-1f);
            Assert.AreEqual(10, s.Count);
        }

        static void Run(PerfGovernor g, float seconds, float dt)
        {
            for (float t = 0f; t < seconds; t += dt) g.Frame(dt);
        }

        [Test]
        public void Auto_StepsDownWhenFramesMiss_AndBackUpSlowly()
        {
            var g = new PerfGovernor(GraphicsMode.Auto);
            Run(g, 10f, 1f / 60f);
            Assert.AreEqual(PerfTier.High, g.Tier, "smooth: stays high");
            Run(g, PerfGovernor.DownWindow + 0.5f, 1f / 40f);
            Assert.AreEqual(PerfTier.Medium, g.Tier, "40 FPS for five seconds: one step down");
            Run(g, PerfGovernor.DownWindow + 0.5f, 1f / 40f);
            Assert.AreEqual(PerfTier.Low, g.Tier);
            Run(g, 20f, 1f / 40f);
            Assert.AreEqual(PerfTier.Low, g.Tier, "never below low");
            Run(g, PerfGovernor.UpWindow - 6f, 1f / 60f);
            Assert.AreEqual(PerfTier.Low, g.Tier, "not back up too soon");
            Run(g, 12f, 1f / 60f);
            Assert.AreEqual(PerfTier.Medium, g.Tier, "a long good stretch steps up one");
            Assert.AreEqual(2, g.StepsDown);
        }

        [Test]
        public void Auto_IgnoresLoadingHitches()
        {
            var g = new PerfGovernor(GraphicsMode.Auto);
            for (int i = 0; i < 20; i++) { g.Frame(0.8f); Run(g, 0.5f, 1f / 60f); }
            Assert.AreEqual(PerfTier.High, g.Tier);
        }

        [Test]
        public void Heat_CapsTheTier_AndHoldsItUntilCool()
        {
            var g = new PerfGovernor(GraphicsMode.Auto);
            g.SetThermal(ThermalState.Serious);
            Assert.AreEqual(PerfTier.Medium, g.Tier);
            g.SetThermal(ThermalState.Critical);
            Assert.AreEqual(PerfTier.Low, g.Tier);
            g.SetThermal(ThermalState.Serious);
            Run(g, 60f, 1f / 60f);
            Assert.AreEqual(PerfTier.Medium, g.Tier, "smooth frames step up, but not past what the heat allows");
            g.SetThermal(ThermalState.Nominal);
            Run(g, 40f, 1f / 60f);
            Assert.AreEqual(PerfTier.High, g.Tier);
        }

        [Test]
        public void FixedModes_DontMove()
        {
            var high = new PerfGovernor(GraphicsMode.High);
            Run(high, 30f, 1f / 20f);
            high.SetThermal(ThermalState.Critical);
            Assert.AreEqual(PerfTier.High, high.Tier, "HIGH means every effect, always");
            Assert.AreEqual(60, high.TargetFps);

            var saver = new PerfGovernor(GraphicsMode.BatterySaver);
            Assert.AreEqual(PerfTier.Low, saver.Tier);
            Assert.AreEqual(30, saver.TargetFps);
            Run(saver, 60f, 1f / 30f);
            Assert.AreEqual(PerfTier.Low, saver.Tier);
        }

        [Test]
        public void LowPowerMode_HalvesAutoFrameRate_Only()
        {
            var auto = new PerfGovernor(GraphicsMode.Auto) { LowPower = true };
            Assert.AreEqual(30, auto.TargetFps);
            Run(auto, 20f, 1f / 30f);
            Assert.AreEqual(PerfTier.High, auto.Tier, "30 FPS is on budget in Low Power Mode");
            var high = new PerfGovernor(GraphicsMode.High) { LowPower = true };
            Assert.AreEqual(60, high.TargetFps);
        }

        [Test]
        public void TierLimits_ShrinkDownward()
        {
            Assert.Greater(PerfGovernor.MaxFx(PerfTier.High), PerfGovernor.MaxFx(PerfTier.Medium));
            Assert.Greater(PerfGovernor.MaxFx(PerfTier.Medium), PerfGovernor.MaxFx(PerfTier.Low));
            Assert.Greater(PerfGovernor.DecalLife(PerfTier.High), PerfGovernor.DecalLife(PerfTier.Low));
            Assert.AreEqual(1f, PerfGovernor.DecorShare(PerfTier.High));
            Assert.Less(PerfGovernor.DecorShare(PerfTier.Low), 0.5f);
            foreach (GraphicsMode m in System.Enum.GetValues(typeof(GraphicsMode)))
            {
                Assert.IsFalse(string.IsNullOrEmpty(PerfGovernor.Name(m)));
                Assert.IsFalse(string.IsNullOrEmpty(PerfGovernor.Info(m)));
            }
        }

        [Test]
        public void Settings_GraphicsModeSanitizes()
        {
            var s = new SettingsData { graphicsMode = 9 };
            s.Sanitize();
            Assert.AreEqual((int)GraphicsMode.Auto, s.graphicsMode);
            s.graphicsMode = (int)GraphicsMode.BatterySaver;
            s.Sanitize();
            Assert.AreEqual((int)GraphicsMode.BatterySaver, s.graphicsMode);
        }
    }
}
