using System.Collections.Generic;
using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    /// <summary>The November improvement round: edge-guarding CPUs, the training lab, the Versus ladder, the
    /// zombie field guide, the Bargain Bell, the straggler finder, Story ghosts, FLAWLESS and game speed.</summary>
    [TestFixture]
    public class ImprovementRoundTests
    {
        static BotView Stage() => new BotView
        {
            position = new Vec2(0f, 0.7f), grounded = true, hasTarget = true,
            targetPosition = new Vec2(12f, -2f), stageLeft = -9f, stageRight = 9f, stageTop = 0f
        };

        static BotDifficulty Guarding(float edgeGuard) => new BotDifficulty
        {
            reactionTime = 0.1f, accuracy = 1f, aggression = 1f, recoverySkill = 1f, dodgeChance = 0f, edgeGuard = edgeGuard
        };

        [Test]
        public void EdgeGuard_WalksToTheLedgeOnTheRivalsSide()
        {
            var c = BotBrain.Decide(Stage(), Guarding(1f), new System.Random(1));
            Assert.Greater(c.moveX, 0f, "heads right, toward the rival's ledge");
            var v = Stage();
            v.targetPosition = new Vec2(-12f, -2f);
            Assert.Less(BotBrain.Decide(v, Guarding(1f), new System.Random(1)).moveX, 0f);
        }

        [Test]
        public void EdgeGuard_WaitsAtTheLedge_AndSwingsWhenTheyComeBack()
        {
            var v = Stage();
            v.position = new Vec2(BotBrain.LedgeSpot(v), 0.7f);
            var wait = BotBrain.Decide(v, Guarding(1f), new System.Random(2));
            Assert.AreEqual(0f, wait.moveX, 0.001f, "in position: wait");
            Assert.IsFalse(wait.attack);
            v.targetPosition = new Vec2(v.position.x + 1.2f, 0f);
            var swing = BotBrain.Decide(v, Guarding(1f), new System.Random(2));
            Assert.IsTrue(swing.attack, "swings as they climb back");
        }

        [Test]
        public void EdgeGuard_NeverWalksOffTheStage_AndEasyNeverGuards()
        {
            var v = Stage();
            v.position = new Vec2(8.9f, 0.7f);
            var c = BotBrain.Decide(v, Guarding(1f), new System.Random(3));
            Assert.LessOrEqual(c.moveX, 0f, "never steps off the ledge");
            Assert.AreEqual(0f, BotDifficulty.For(BotLevel.Easy).edgeGuard);
            Assert.Greater(BotDifficulty.For(BotLevel.Hard).edgeGuard, BotDifficulty.For(BotLevel.Normal).edgeGuard);
            Assert.AreEqual(BotDifficulty.For(BotLevel.Hard).edgeGuard, AdaptiveDifficulty.Blend(1f).edgeGuard, 0.0001f);
        }

        [Test]
        public void ArenaItems_AreUnrated_AndConfigKeepsCpuPick()
        {
            var c = new ArenaMatchConfig { items = true };
            Assert.IsFalse(c.Rated, "items are luck: unrated");
            c.items = false;
            Assert.IsTrue(c.Rated);
            c.cpuCharacterId = "brick";
            Assert.AreEqual("brick", c.Copy().cpuCharacterId);
        }

        [Test]
        public void ComboCounter_ChainsWithinHitstun_AndDropsAfter()
        {
            var cc = new ComboCounter();
            Assert.AreEqual(1, cc.Register(0f, 0.3f));
            Assert.AreEqual(2, cc.Register(0.4f, 0.3f));
            Assert.AreEqual(3, cc.Register(0.8f, 0.3f, pipsBroken: 1, launch: true));
            Assert.AreEqual(3, cc.Current(1.0f));
            Assert.AreEqual(0, cc.Current(2f), "chain dropped");
            Assert.AreEqual(1, cc.Register(3f, 0.3f));
            Assert.AreEqual(3, cc.Best);
            Assert.AreEqual(1, cc.Launches);
            Assert.AreEqual(4, cc.TotalHits);
            Assert.AreEqual(TrainingAct.Stand, TrainingActs.Next(TrainingAct.Fight));
        }

        [Test]
        public void Ladder_ClimbsFromEasyToHard_AndSkipsYourOwnFighter()
        {
            var ids = new List<string> { "nova", "brick", "luma", "rex", "juno", "kai", "bo" };
            var l = VersusLadder.Build("nova", ids, 7);
            Assert.AreEqual(VersusLadder.MaxRungs, l.Length);
            Assert.IsFalse(System.Array.IndexOf(l.rivals, "nova") >= 0);
            Assert.AreEqual(BotLevel.Easy, l.levels[0]);
            Assert.AreEqual(BotLevel.Hard, l.levels[l.Length - 1]);
            Assert.AreEqual(BotLevel.Hard, l.levels[l.Length - 2]);
            Assert.AreEqual(BotLevel.Normal, l.levels[1]);
            Assert.AreEqual(string.Join(",", l.rivals), string.Join(",", VersusLadder.Build("nova", ids, 7).rivals), "deterministic for a seed");
        }

        [Test]
        public void Ladder_LossEndsIt_RetryResumes_ClearPaysOnce()
        {
            var l = VersusLadder.Build("nova", new List<string> { "nova", "brick" }, 1);
            Assert.AreEqual(3, l.Length, "short rosters still climb 3 rungs");
            Assert.IsFalse(l.Report(true));
            Assert.IsFalse(l.Report(false));
            Assert.IsFalse(l.Active);
            l.Retry();
            Assert.IsTrue(l.Active);
            Assert.AreEqual(1, l.rung, "retry the rung you fell on");
            Assert.IsFalse(l.Report(true));
            Assert.IsTrue(l.Report(true), "the last win clears it");
            Assert.IsTrue(l.cleared);
            var save = new LadderSave();
            Assert.IsTrue(save.Record(l));
            Assert.AreEqual(1, save.clears);
            Assert.AreEqual(3, save.bestRungs);
            Assert.IsTrue(save.clearedWith.Contains("nova"));
            Assert.Greater(VersusLadder.ClearReward(5), VersusLadder.ClearReward(3));
        }

        [Test]
        public void FieldGuide_TiersAndMasteryPayOnce()
        {
            var c = new ZombieCareer();
            Assert.AreEqual(FieldGuideTier.Unknown, ZombieFieldGuide.Tier(ZLFoe.Runner, 0));
            Assert.AreEqual(0, ZombieFieldGuide.Add(c, ZLFoe.Runner));
            Assert.AreEqual(FieldGuideTier.Seen, ZombieFieldGuide.Tier(ZLFoe.Runner, ZombieFieldGuide.Kills(c, ZLFoe.Runner)));
            var e = ZombieFieldGuide.Get(ZLFoe.Runner);
            Assert.AreEqual(ZombieFieldGuide.MasteryShards, ZombieFieldGuide.Add(c, ZLFoe.Runner, e.mastered));
            Assert.AreEqual(0, ZombieFieldGuide.Add(c, ZLFoe.Runner, 5), "pays once");
            Assert.AreEqual(FieldGuideTier.Mastered, ZombieFieldGuide.Tier(ZLFoe.Runner, ZombieFieldGuide.Kills(c, ZLFoe.Runner)));
            Assert.AreEqual((1, 1), ZombieFieldGuide.Count(c));
            Assert.AreEqual(0, ZombieFieldGuide.Kills(c, ZLFoe.Brute));
            Assert.Greater(ZombieFieldGuide.ToNext(ZLFoe.Brute, 0), 0);
            foreach (ZLFoe k in System.Enum.GetValues(typeof(ZLFoe)))
                Assert.IsFalse(string.IsNullOrEmpty(ZombieFieldGuide.Get(k).tip), $"{k} has a tip");
            c.foeKills[(int)ZLFoe.Runner] = -4;
            c.Sanitize();
            Assert.AreEqual(0, ZombieFieldGuide.Kills(c, ZLFoe.Runner));
        }

        [Test]
        public void BargainBell_OnlyInLastStand()
        {
            bool seen = false;
            for (int i = 0; i < 100; i++)
            {
                double r = i / 100.0;
                Assert.AreNotEqual(ZLPowerUp.BargainBell, ZLShop.PickPowerUp(r), "Zombie Land never drops it");
                if (ZLShop.PickPowerUp(r, true) == ZLPowerUp.BargainBell) seen = true;
            }
            Assert.IsTrue(seen);
            Assert.AreEqual("pu_bargain", ZLShop.PowerUpArt(ZLPowerUp.BargainBell));
            Assert.Less(ZLShop.BargainSpinCost, LastStandRules.BoxCost);
            var s = new NetSnapshot { bargain = true };
            Assert.IsTrue(ZLNet.ReadSnapshot(ZLNet.Snapshot(s)).bargain, "guests see the bell too");
        }

        [Test]
        public void StoryGhosts_KeepTheFastestClear()
        {
            var s = SaveData.CreateNew();
            var run = new GhostTrack();
            for (int i = 0; i < 20; i++) run.Add(i * 0.5f, 1f);
            Assert.IsTrue(StoryGhosts.Offer(s, "sq_a", 30f, run));
            Assert.IsFalse(StoryGhosts.Offer(s, "sq_a", 31f, run), "slower doesn't replace it");
            Assert.IsTrue(StoryGhosts.Offer(s, "sq_a", 25f, run));
            Assert.AreEqual(25f, StoryGhosts.Find(s, "sq_a").seconds);
            Assert.AreEqual(20, GhostTrack.Decode(StoryGhosts.Find(s, "sq_a").data).Count);
            for (int i = 0; i < StoryGhosts.MaxLevels + 5; i++) StoryGhosts.Offer(s, "lvl" + i, 10f, run);
            Assert.LessOrEqual(s.storyGhosts.Count, StoryGhosts.MaxLevels);
            s.storyGhosts.Add(null);
            s.storyGhosts.Add(new StoryGhost { levelId = "x", seconds = 0f, data = "abc" });
            s.SanitizeAndMigrate();
            Assert.IsFalse(s.storyGhosts.Exists(g => g == null || g.seconds <= 0f));
        }

        [Test]
        public void Flawless_PaysOncePerLevel()
        {
            var rec = new LevelRecord { levelId = "sq_a" };
            Assert.AreEqual(0, StoryFlawless.Record(rec, tookHit: true));
            Assert.AreEqual(StoryFlawless.Shards, StoryFlawless.Record(rec, tookHit: false));
            Assert.AreEqual(0, StoryFlawless.Record(rec, tookHit: false));
            var s = SaveData.CreateNew();
            s.levels.Add(rec);
            Assert.AreEqual(1, StoryFlawless.Count(s));
        }

        [Test]
        public void GameSpeed_StepsAndRecords()
        {
            Assert.AreEqual(90, GameSpeed.Next(100));
            Assert.AreEqual(100, GameSpeed.Next(70));
            Assert.AreEqual(100, GameSpeed.Clamp(55));
            Assert.AreEqual(0.8f, GameSpeed.Scale(80), 0.0001f);
            Assert.IsTrue(GameSpeed.CountsForRecords(100));
            Assert.IsFalse(GameSpeed.CountsForRecords(70));
            var st = new SettingsData { gameSpeed = 42 };
            st.Sanitize();
            Assert.AreEqual(100, st.gameSpeed);
        }
    }
}
