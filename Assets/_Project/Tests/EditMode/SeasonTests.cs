using System;
using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    /// <summary>Live seasons in Story Quest: the real-date calendar, tokens, challenges, keepsakes and the weather maths.</summary>
    [TestFixture]
    public class SeasonTests
    {
        static DateTime D(int y, int m, int d) => new DateTime(y, m, d);

        [Test]
        public void Thanksgiving_IsTheFourthThursdayOfNovember()
        {
            Assert.AreEqual(D(2026, 11, 26), SeasonCalendar.Thanksgiving(2026));
            Assert.AreEqual(D(2025, 11, 27), SeasonCalendar.Thanksgiving(2025));
            Assert.AreEqual(D(2027, 11, 25), SeasonCalendar.Thanksgiving(2027));
            Assert.AreEqual(D(2029, 11, 22), SeasonCalendar.Thanksgiving(2029)); // 1 Nov 2029 is a Thursday
        }

        [Test]
        public void Calendar_PicksTheSeasonForEveryDate()
        {
            Assert.AreEqual(SeasonEvent.None, SeasonCalendar.At(D(2026, 9, 21)).Event);
            Assert.AreEqual(SeasonEvent.Autumn, SeasonCalendar.At(D(2026, 9, 22)).Event);
            Assert.AreEqual(SeasonEvent.Autumn, SeasonCalendar.At(D(2026, 10, 31)).Event);
            Assert.AreEqual(SeasonEvent.Harvest, SeasonCalendar.At(D(2026, 11, 1)).Event);
            Assert.AreEqual(SeasonEvent.Harvest, SeasonCalendar.At(D(2026, 11, 30)).Event);
            Assert.AreEqual(SeasonEvent.Winterfest, SeasonCalendar.At(D(2026, 12, 1)).Event);
            Assert.AreEqual(SeasonEvent.Winterfest, SeasonCalendar.At(D(2027, 1, 1)).Event);
            Assert.AreEqual(SeasonEvent.Frost, SeasonCalendar.At(D(2027, 1, 2)).Event);
            Assert.AreEqual(SeasonEvent.Frost, SeasonCalendar.At(D(2028, 2, 29)).Event, "leap day");
            Assert.AreEqual(SeasonEvent.None, SeasonCalendar.At(D(2027, 3, 1)).Event);
            Assert.AreEqual(SeasonEvent.None, SeasonCalendar.At(D(2027, 7, 4)).Event);
        }

        [Test]
        public void Calendar_Peaks_FollowTheHolidays()
        {
            Assert.AreEqual(SeasonPeak.None, SeasonCalendar.At(D(2026, 11, 22)).Peak);
            Assert.AreEqual(SeasonPeak.ThanksgivingWeek, SeasonCalendar.At(D(2026, 11, 23)).Peak);
            Assert.AreEqual(SeasonPeak.ThanksgivingWeek, SeasonCalendar.At(D(2026, 11, 26)).Peak);
            Assert.AreEqual(SeasonPeak.ThanksgivingWeek, SeasonCalendar.At(D(2026, 11, 29)).Peak);
            Assert.AreEqual(SeasonPeak.None, SeasonCalendar.At(D(2026, 11, 30)).Peak);
            Assert.AreEqual(SeasonPeak.None, SeasonCalendar.At(D(2026, 12, 19)).Peak);
            Assert.AreEqual(SeasonPeak.Christmas, SeasonCalendar.At(D(2026, 12, 25)).Peak);
            Assert.AreEqual(SeasonPeak.None, SeasonCalendar.At(D(2026, 12, 27)).Peak);
            Assert.AreEqual(SeasonPeak.NewYear, SeasonCalendar.At(D(2026, 12, 31)).Peak);
            Assert.AreEqual(SeasonPeak.NewYear, SeasonCalendar.At(D(2027, 1, 1)).Peak);
            Assert.AreEqual(SeasonPeak.HalloweenWeek, SeasonCalendar.At(D(2026, 10, 25)).Peak);
            Assert.AreEqual(1f, SeasonCalendar.At(D(2026, 12, 25)).Intensity, 1e-6);
            Assert.Less(SeasonCalendar.At(D(2026, 12, 5)).Intensity, 1f);
        }

        [Test]
        public void Keys_AndLooks()
        {
            Assert.AreEqual("harvest_2026", SeasonCalendar.At(D(2026, 11, 1)).Key);
            Assert.AreEqual("winterfest_2026", SeasonCalendar.At(D(2027, 1, 1)).Key, "New Year's Day still belongs to December's Winterfest");
            Assert.AreEqual("frost_2027", SeasonCalendar.At(D(2027, 1, 2)).Key);
            Assert.AreEqual(SeasonLook.Autumn, SeasonCalendar.At(D(2026, 11, 10)).Look);
            Assert.AreEqual(SeasonLook.Winter, SeasonCalendar.At(D(2026, 12, 10)).Look);
            Assert.AreEqual(SeasonLook.None, SeasonCalendar.At(D(2026, 5, 10)).Look);
            Assert.AreEqual("", SeasonCalendar.At(D(2026, 5, 10)).Key);
        }

        [Test]
        public void DaysLeft_AndNext()
        {
            var s = SeasonCalendar.At(D(2026, 11, 1));
            Assert.AreEqual(30, s.DaysLeft(D(2026, 11, 1)));
            Assert.AreEqual(1, s.DaysLeft(D(2026, 11, 30)));
            var next = SeasonCalendar.Next(D(2026, 11, 1));
            Assert.AreEqual(SeasonEvent.Winterfest, next.ev);
            Assert.AreEqual(D(2026, 12, 1), next.start);
            var fromSpring = SeasonCalendar.Next(D(2027, 4, 1));
            Assert.AreEqual(SeasonEvent.Autumn, fromSpring.ev);
            Assert.AreEqual(D(2027, 9, 22), fromSpring.start);
            var peak = SeasonCalendar.NextPeak(D(2026, 11, 1));
            Assert.AreEqual(SeasonPeak.ThanksgivingWeek, peak.peak);
            Assert.AreEqual(D(2026, 11, 23), peak.start);
            var during = SeasonCalendar.NextPeak(D(2026, 12, 24));
            Assert.AreEqual(SeasonPeak.Christmas, during.peak);
            Assert.AreEqual(D(2026, 12, 20), during.start, "a running peak reports its own first day");
            Assert.AreEqual("tomorrow", SeasonCalendar.InDays(1));
            Assert.AreEqual("in 22 days", SeasonCalendar.InDays(22));
        }

        [Test]
        public void SettingOff_MeansNoSeason()
        {
            try
            {
                SeasonCalendar.OverrideToday = D(2026, 12, 25);
                Assert.IsTrue(SeasonCalendar.Current(SeasonCalendar.ModeLive).Active);
                Assert.IsFalse(SeasonCalendar.Current(SeasonCalendar.ModeOff).Active);
            }
            finally { SeasonCalendar.OverrideToday = null; }
        }

        [Test]
        public void Tokens_OnePerLevel_AndKeepsakeAtTheGoal()
        {
            var save = SaveData.CreateNew();
            var s = SeasonCalendar.At(D(2026, 11, 10));
            Assert.IsTrue(Seasons.AddToken(save, s, "lvl_a", 3, out bool k1));
            Assert.IsFalse(k1);
            Assert.IsFalse(Seasons.AddToken(save, s, "lvl_a", 3, out _), "one token per level");
            Assert.IsTrue(Seasons.AddToken(save, s, "lvl_b", 3, out _));
            Assert.IsTrue(Seasons.AddToken(save, s, "lvl_c", 3, out bool k3));
            Assert.IsTrue(k3, "the goal is every level when there are fewer than 8");
            Assert.IsTrue(Seasons.Owns(save, "keepsake_harvest_scarf"));
            Assert.AreEqual(3, Seasons.Tokens(save, s));
            Assert.IsTrue(Seasons.HasToken(save, s, "lvl_b"));
            // next year's Harvest starts again from zero, but the keepsake stays
            var next = SeasonCalendar.At(D(2027, 11, 10));
            Assert.AreEqual(0, Seasons.Tokens(save, next));
            Assert.IsTrue(Seasons.Owns(save, "keepsake_harvest_scarf"));
            Assert.AreEqual(8, Seasons.KeepsakeGoal(40));
        }

        [Test]
        public void Challenges_PayOnce()
        {
            var save = SaveData.CreateNew();
            var s = SeasonCalendar.At(D(2026, 12, 3));
            var list = Seasons.Challenges(s.Event);
            Assert.AreEqual(4, list.Length);
            StringAssert.Contains("Gift", list[0].text);
            Assert.AreEqual(0, Seasons.Claim(save, s, 1), "not done yet");
            for (int i = 0; i < 5; i++) Seasons.RecordLevel(save, s, 20, 6);
            Assert.IsTrue(Seasons.IsDone(save, s, 1));
            Assert.IsFalse(Seasons.IsDone(save, s, 2), "100 of 150 shards");
            Assert.AreEqual(1, Seasons.Unclaimed(save, s));
            int before = save.starShards;
            Assert.AreEqual(200, Seasons.Claim(save, s, 1));
            Assert.AreEqual(before + 200, save.starShards);
            Assert.AreEqual(0, Seasons.Claim(save, s, 1), "paid once");
            Assert.AreEqual(0, Seasons.Unclaimed(save, s));
            Assert.AreEqual(0, Seasons.Claim(save, s, 99));
        }

        [Test]
        public void Keepsakes_WearOnlyOwned_AndSanitize()
        {
            var save = SaveData.CreateNew();
            Assert.IsFalse(Seasons.Wear(save, "keepsake_holly_beanie"));
            save.season.keepsakes.Add("keepsake_holly_beanie");
            Assert.IsTrue(Seasons.Wear(save, "keepsake_holly_beanie"));
            Assert.AreEqual("keepsake_holly_beanie", save.season.worn);
            save.season.keepsakes.Add("not_a_keepsake");
            save.season.records.Add(null);
            save.season = null;
            save.SanitizeAndMigrate();
            Assert.IsNotNull(save.season);
            save.season.keepsakes.Add("not_a_keepsake");
            save.season.worn = "keepsake_frost_earmuffs"; // not owned
            save.SanitizeAndMigrate();
            Assert.AreEqual(0, save.season.keepsakes.Count);
            Assert.AreEqual("", save.season.worn);
            Assert.IsTrue(Seasons.Wear(save, ""));
            foreach (var k in Seasons.Keepsakes) Assert.IsTrue(Seasons.IsKeepsake(k.id));
            Assert.AreEqual(4, Seasons.Keepsakes.Length);
        }

        [Test]
        public void Weather_StaysInsideTheView_AndReducedMotionIsCalmer()
        {
            var sim = new WeatherSim(WeatherKind.Snow, 60, 20f, 12f, 7, reduced: false);
            for (int i = 0; i < 600; i++) sim.Step(1f / 30f, 0.05f, -0.02f);
            foreach (var f in sim.Flakes)
            {
                Assert.GreaterOrEqual(f.x, 0f); Assert.Less(f.x, 20f);
                Assert.GreaterOrEqual(f.y, 0f); Assert.Less(f.y, 12f);
            }
            var calm = new WeatherSim(WeatherKind.Leaves, 20, 20f, 12f, 7, reduced: true);
            foreach (var f in calm.Flakes) { Assert.AreEqual(0f, f.spin, 1e-6); Assert.AreEqual(0f, f.sway, 1e-6); }
            Assert.Greater(WeatherSim.CountFor(WeatherKind.Snow, 0, 1f, false), WeatherSim.CountFor(WeatherKind.Snow, 2, 1f, false));
            Assert.Greater(WeatherSim.CountFor(WeatherKind.Snow, 0, 1f, false), WeatherSim.CountFor(WeatherKind.Snow, 0, 1f, true));
            Assert.Greater(WeatherSim.CountFor(WeatherKind.Snow, 0, 1f, false), WeatherSim.CountFor(WeatherKind.Snow, 0, 0.65f, false));
            Assert.AreEqual(0, WeatherSim.CountFor(WeatherKind.None, 0, 1f, false));
            sim.Resize(40f, 24f);
            Assert.AreEqual(40f, sim.Width, 1e-6);
        }
    }
}
