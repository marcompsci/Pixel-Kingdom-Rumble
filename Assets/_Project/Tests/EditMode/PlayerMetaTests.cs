using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    /// <summary>The 20-phase round: play time, the login gift, hero mastery, head-to-head, fall markers, WHAT'S NEW,
    /// party bests, Item Race medals, the comfort preset and RESET HINTS.</summary>
    [TestFixture]
    public class PlayerMetaTests
    {
        [Test]
        public void PlayTime_AddsPerMode_AndFormats()
        {
            var s = SaveData.CreateNew();
            PlayTime.Add(s, 0, 90f);
            PlayTime.Add(s, 5, 3600f);
            PlayTime.Add(s, 5, 5000f); // one chunk over an hour is ignored (clock jumps)
            Assert.AreEqual(90f, PlayTime.Seconds(s, 0), 0.01f);
            Assert.AreEqual(3690f, PlayTime.Total(s), 0.01f);
            Assert.AreEqual("1h 1m", PlayTime.Format(3690f));
            Assert.AreEqual("under a minute", PlayTime.Format(20f));
        }

        [Test]
        public void LoginGift_OncePerDay_StreakGrowsAndResets()
        {
            var s = SaveData.CreateNew();
            Assert.AreEqual(LoginStreak.Gifts[0], LoginStreak.Claim(s, 20261120, 20261119));
            Assert.AreEqual(0, LoginStreak.Claim(s, 20261120, 20261119), "once a day");
            Assert.AreEqual(LoginStreak.Gifts[1], LoginStreak.Claim(s, 20261121, 20261120));
            Assert.AreEqual(2, s.lifetime.loginStreak);
            Assert.AreEqual(LoginStreak.Gifts[0], LoginStreak.Claim(s, 20261125, 20261124), "missed days restart the streak");
            Assert.AreEqual(2, s.lifetime.bestLoginStreak);
            Assert.AreEqual(LoginStreak.Gifts[0], LoginStreak.GiftFor(8), "7-day cycle");
        }

        [Test]
        public void HeroMastery_LevelsTitles_AndPaysAtFiveAndTen()
        {
            var s = SaveData.CreateNew();
            Assert.AreEqual(1, HeroMastery.Level(0));
            Assert.AreEqual("ROOKIE", HeroMastery.Title(1));
            var a = HeroMastery.Add(s, "nova", 460);
            Assert.AreEqual(5, a.levelAfter);
            Assert.IsTrue(a.LevelledUp);
            Assert.AreEqual(HeroMastery.Level5Shards, a.shards);
            Assert.AreEqual(0, HeroMastery.Add(s, "nova", 10).shards, "pays once");
            var b = HeroMastery.Add(s, "nova", 99999);
            Assert.AreEqual(HeroMastery.MaxLevel, b.levelAfter);
            Assert.AreEqual(HeroMastery.Level10Shards, b.shards);
            Assert.AreEqual(1f, HeroMastery.Progress01(HeroMastery.Xp(s, "nova")));
            HeroMastery.Add(s, "brick", 5);
            Assert.AreEqual("nova", HeroMastery.Favorite(s).id);
            Assert.AreEqual(55, HeroMastery.ToNext(5));
        }

        [Test]
        public void HeadToHead_CountsWinsAndLosses()
        {
            var s = SaveData.CreateNew();
            Assert.AreEqual("not met yet", HeadToHead.Line(HeadToHead.Get(s, "luma")));
            HeadToHead.Record(s, "luma", true);
            HeadToHead.Record(s, "luma", true);
            HeadToHead.Record(s, "luma", false);
            Assert.AreEqual("YOU 2 - 1", HeadToHead.Line(HeadToHead.Get(s, "luma")));
        }

        [Test]
        public void FallMemory_KeepsTheLastFew_AndForgetsOnACleanClear()
        {
            var s = SaveData.CreateNew();
            for (int i = 0; i < FallMemory.PerLevel + 3; i++) FallMemory.Add(s, "sq_a", i, 1f);
            var f = FallMemory.Get(s, "sq_a");
            Assert.AreEqual(FallMemory.PerLevel, f.xs.Count);
            Assert.AreEqual(3f, f.xs[0], "oldest dropped first");
            FallMemory.Clear(s, "sq_a");
            Assert.IsNull(FallMemory.Get(s, "sq_a"));
            for (int i = 0; i < FallMemory.MaxLevels + 4; i++) FallMemory.Add(s, "l" + i, 0f, 0f);
            Assert.AreEqual(FallMemory.MaxLevels, s.lifetime.falls.Count);
        }

        [Test]
        public void WhatsNew_OncePerVersion_NotForNewPlayers()
        {
            var s = SaveData.CreateNew();
            Assert.IsFalse(WhatsNew.ShouldShow(s, newPlayer: true));
            Assert.IsTrue(WhatsNew.ShouldShow(s, newPlayer: false));
            WhatsNew.MarkSeen(s);
            Assert.IsFalse(WhatsNew.ShouldShow(s, newPlayer: false));
        }

        [Test]
        public void PartyBests_AndRaceMedals()
        {
            var s = SaveData.CreateNew();
            var r = PartyProgress.Apply(s, new PartyOutcome { mode = PartyMode.CoinRush, placement = 2, score = 9 }, 20261120);
            Assert.IsTrue(r.newBestScore);
            Assert.AreEqual(9, s.party.Find(PartyMode.CoinRush).bestScore);
            r = PartyProgress.Apply(s, new PartyOutcome { mode = PartyMode.CoinRush, placement = 1, score = 5 }, 20261120);
            Assert.IsFalse(r.newBestScore);
            Assert.AreEqual(9, s.party.Find(PartyMode.CoinRush).bestScore);
            Assert.AreEqual(TimeMedal.Gold, RaceMedals.For(65f));
            Assert.AreEqual(TimeMedal.Silver, RaceMedals.For(80f));
            Assert.AreEqual(TimeMedal.Bronze, RaceMedals.For(99f));
            Assert.AreEqual(TimeMedal.None, RaceMedals.For(130f));
            Assert.AreEqual(TimeMedal.None, RaceMedals.For(0f));
        }

        [Test]
        public void ComfortPreset_AndResetHints()
        {
            var st = new SettingsData();
            Assert.IsFalse(st.ComfortPresetOn);
            st.ApplyComfortPreset();
            Assert.IsTrue(st.ComfortPresetOn);
            st.Sanitize();
            Assert.AreEqual(80, st.gameSpeed);
            var s = SaveData.CreateNew();
            s.MarkHintSeen("welcome_v1");
            s.ResetHints();
            Assert.AreEqual(0, s.seenHints.Count);
            Assert.IsTrue(s.lifetime.replayWelcome);
        }

        [Test]
        public void Lifetime_SanitizesBadData()
        {
            var s = SaveData.CreateNew();
            s.lifetime.playSeconds.Add(float.NaN);
            s.lifetime.heroXp.Add(null);
            s.lifetime.heroXp.Add(new HeroXp { id = "x", xp = -5, paidLevel = 99 });
            s.lifetime.falls.Add(new FallMarks { levelId = "a", xs = new System.Collections.Generic.List<float> { 1f }, ys = new System.Collections.Generic.List<float>() });
            s.SanitizeAndMigrate();
            Assert.AreEqual(0f, s.lifetime.playSeconds[0]);
            Assert.AreEqual(1, s.lifetime.heroXp.Count);
            Assert.AreEqual(0, s.lifetime.heroXp[0].xp);
            Assert.AreEqual(HeroMastery.MaxLevel, s.lifetime.heroXp[0].paidLevel);
            Assert.AreEqual(0, s.lifetime.falls.Count);
            s.lifetime = null;
            s.SanitizeAndMigrate();
            Assert.IsNotNull(s.lifetime);
        }
    }
}
