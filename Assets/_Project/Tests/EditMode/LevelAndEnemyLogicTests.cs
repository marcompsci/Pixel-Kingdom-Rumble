using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    [TestFixture]
    public class EnemyLogicTests
    {
        [Test]
        public void Patrol_TurnsAtWallsAndLedges()
        {
            Assert.AreEqual(1, PatrolLogic.NextDirection(1, false, true, true));
            Assert.AreEqual(-1, PatrolLogic.NextDirection(1, true, true, true));
            Assert.AreEqual(-1, PatrolLogic.NextDirection(1, false, false, true));
            Assert.AreEqual(1, PatrolLogic.NextDirection(-1, false, false, true));
        }

        [Test]
        public void Patrol_DoesNotFlipFromMissingGroundWhileAirborne()
        {
            Assert.AreEqual(1, PatrolLogic.NextDirection(1, false, false, false));
        }

        [Test]
        public void Hopper_WaitsInitialDelay_ThenHopsTowardTargetInRange()
        {
            var h = new HopperLogic(cooldown: 1f, range: 5f, initialDelay: 0.5f);
            Assert.IsFalse(h.Tick(0.3f, true, -3f, 0f, out _));
            Assert.IsFalse(h.Tick(0.3f, true, -3f, 0f, out _)); // timer just ran out this step
            Assert.IsTrue(h.Tick(0.016f, true, -3f, 0f, out int dir));
            Assert.AreEqual(-1, dir);
            Assert.IsFalse(h.Tick(0.016f, true, -3f, 0f, out _)); // cooling down
        }

        [Test]
        public void Hopper_IgnoresOutOfRangeAndWaitsWhileAirborne()
        {
            var h = new HopperLogic(1f, 5f, 0f);
            Assert.IsFalse(h.Tick(0.1f, true, 9f, 0f, out _));
            Assert.IsFalse(h.Tick(0.1f, false, 2f, 0f, out _));
            Assert.IsTrue(h.Tick(0.1f, true, 2f, 0f, out int dir));
            Assert.AreEqual(1, dir);
        }
    }

    [TestFixture]
    public class LevelRunTests
    {
        [Test]
        public void TracksTimeShardsSecretsDeaths()
        {
            var r = new LevelRun("sq_test");
            r.Tick(1.5f);
            r.CollectShards(3);
            r.CollectShards(-2);
            Assert.IsTrue(r.FindSecret("cave"));
            Assert.IsFalse(r.FindSecret("cave"));
            r.RecordDeath();
            Assert.AreEqual(1.5f, r.ElapsedSeconds, 0.0001f);
            Assert.AreEqual(3, r.Shards);
            Assert.AreEqual(1, r.SecretsFound);
            Assert.AreEqual(1, r.Deaths);
        }

        [Test]
        public void Checkpoints_OnlyMoveForward()
        {
            var r = new LevelRun("sq_test");
            Assert.IsTrue(r.ReachCheckpoint(0));
            Assert.IsTrue(r.ReachCheckpoint(2));
            Assert.IsFalse(r.ReachCheckpoint(1));
            Assert.IsFalse(r.ReachCheckpoint(2));
            Assert.AreEqual(2, r.CheckpointIndex);
        }

        [Test]
        public void Complete_SavesBestsAndGrantsRewardOnce()
        {
            var save = SaveData.CreateNew();
            var r = new LevelRun("sq_test");
            r.Tick(60f);
            r.CollectShards(10);
            r.FindSecret("a");
            int reward = r.Complete(save);
            Assert.AreEqual(Economy.LevelReward(10, 1, true), reward);
            Assert.AreEqual(reward, save.starShards);
            Assert.IsTrue(save.GetLevel("sq_test").completed);
            Assert.AreEqual(0, r.Complete(save)); // second call does nothing
            r.Tick(5f);
            r.CollectShards(5);
            Assert.AreEqual(60f, r.ElapsedSeconds, 0.0001f); // frozen after completion
            Assert.AreEqual(10, r.Shards);
        }

        [Test]
        public void Complete_SecondClearHasNoFirstClearBonus()
        {
            var save = SaveData.CreateNew();
            new LevelRun("sq_test").Complete(save);
            int before = save.starShards;
            int reward = new LevelRun("sq_test").Complete(save);
            Assert.AreEqual(Economy.LevelReward(0, 0, false), reward);
            Assert.AreEqual(before + reward, save.starShards);
        }
    }
}
