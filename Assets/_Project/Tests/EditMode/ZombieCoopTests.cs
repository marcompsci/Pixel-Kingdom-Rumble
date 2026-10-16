using System.Collections.Generic;
using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    /// <summary>Last Stand co-op: squad rules and the online protocol (every message survives a round trip).</summary>
    [TestFixture]
    public class ZombieCoopTests
    {
        [Test]
        public void Squad_MoreZombiesAndTougherBosses_ForBiggerSquads()
        {
            Assert.AreEqual(LastStandRules.ZombiesInRound(5), SquadRules.ZombiesInRound(5, 1));
            Assert.Greater(SquadRules.ZombiesInRound(5, 4), SquadRules.ZombiesInRound(5, 2));
            Assert.Greater(SquadRules.MaxAlive(5, 4), SquadRules.MaxAlive(5, 1));
            Assert.Greater(SquadRules.BossHealth(1000, 3), 1000);
            Assert.AreEqual(1000, SquadRules.BossHealth(1000, 1));
            Assert.AreEqual(ZLCatalog.Pistol.id, SquadRules.AIGunFor(1));
            Assert.AreEqual("assault_rifle", SquadRules.AIGunFor(20));
            Assert.IsNotNull(ZLCatalog.Gun(SquadRules.AIGunFor(9)));
            Assert.IsTrue(SquadRules.AllDown(new List<bool> { false, false }));
            Assert.IsFalse(SquadRules.AllDown(new List<bool> { false, true }));
        }

        [Test]
        public void HostElection_IsTheSameOnEveryPeer()
        {
            var a = new[] { "G:300", "G:100", "G:200" };
            var b = new[] { "G:200", "G:300", "G:100" };
            Assert.AreEqual("G:100", SquadRules.Host(a));
            Assert.AreEqual(SquadRules.Host(a), SquadRules.Host(b));
            CollectionAssertEqual(SquadRules.Slots(a), SquadRules.Slots(b));
            Assert.AreEqual("G:100", SquadRules.Slots(b)[0], "the host is slot 0");
        }

        static void CollectionAssertEqual(List<string> x, List<string> y)
        {
            Assert.AreEqual(x.Count, y.Count);
            for (int i = 0; i < x.Count; i++) Assert.AreEqual(x[i], y[i]);
        }

        [Test]
        public void HelloStartToastOver_RoundTrip()
        {
            Assert.IsTrue(ZLNet.ReadHello(ZLNet.Hello("Omari", "nova"), out var name, out var hero));
            Assert.AreEqual("Omari", name);
            Assert.AreEqual("nova", hero);
            var slots = new List<NetSlot>
            {
                new NetSlot { slot = 0, playerId = "G:1", name = "Host", heroId = "nova" },
                new NetSlot { slot = 1, playerId = "G:2", name = "Friend", heroId = "brick" },
                new NetSlot { slot = 2, playerId = "", name = "AI", heroId = "luma", ai = true },
            };
            var start = ZLNet.Start(SurvivalMode.Hardcore, slots);
            Assert.AreEqual(NetMsg.Start, ZLNet.Kind(start));
            Assert.IsTrue(ZLNet.ReadStart(start, out var mode, out var back));
            Assert.AreEqual(SurvivalMode.Hardcore, mode);
            Assert.AreEqual(3, back.Count);
            Assert.AreEqual("Friend", back[1].name);
            Assert.IsTrue(back[2].ai);
            Assert.IsTrue(ZLNet.ReadToast(ZLNet.Toast("ROUND 5", 3), out var text, out int sfx));
            Assert.AreEqual("ROUND 5", text);
            Assert.AreEqual(3, sfx);
            Assert.IsTrue(ZLNet.ReadOver(ZLNet.Over(true, 10, 321), out bool won, out int round, out int kills));
            Assert.IsTrue(won); Assert.AreEqual(10, round); Assert.AreEqual(321, kills);
            Assert.AreEqual(NetMsg.Bye, ZLNet.Kind(ZLNet.Bye()));
        }

        [Test]
        public void Input_RoundTrip_AndPressCountersWrap()
        {
            var i = new NetInput { seq = 70000, moveX = 0.5f, moveY = -1f, aimX = 0.7f, aimY = 0.7f, posX = 31.25f, posY = 8.5f, fire = true, useHeld = true, useCount = 255, dodgeCount = 2, meleeCount = 3, swapCount = 4 };
            Assert.IsTrue(ZLNet.ReadInput(ZLNet.Input(i), out var o));
            Assert.AreEqual(70000 & 0xffff, o.seq);
            Assert.AreEqual(0.5f, o.moveX, 0.01f);
            Assert.AreEqual(-1f, o.moveY, 0.01f);
            Assert.AreEqual(31.25f, o.posX, 0.01f);
            Assert.IsTrue(o.fire && o.useHeld);
            Assert.AreEqual(255, o.useCount);
            Assert.AreEqual(1, ZLNet.Presses(255, 0), "counters wrap");
            Assert.AreEqual(0, ZLNet.Presses(7, 7));
            Assert.AreEqual(3, ZLNet.Presses(4, 7));
        }

        [Test]
        public void Snapshot_RoundTrip_KeepsEverything()
        {
            var s = new NetSnapshot { tick = 1234, round = 12, zombiesLeft = 40, intermissionLeft = 5, intermission = true, houndRound = true, boxBusy = true, doublePoints = true, doorMask = 0b1011, boxSpot = 2 };
            s.members.Add(new NetMember { slot = 1, x = 10.5f, y = 20.25f, aimX = 1f, hp = 75, maxHp = 150, points = 12345, kills = 99, mag = 30, reserve = 210, gunIndex = ZLNet.GunIndex("assault_rifle"), perkMask = ZLNet.PerkMask(new[] { "ThickSkin", "QuickHands" }), downed = true, forged = true });
            for (int k = 0; k < 30; k++) s.zombies.Add(new NetZombie { id = 1000 + k, kind = (int)ZLFoe.Runner, x = k, y = 45.5f, hp01 = 200 });
            s.powerUps.Add(new NetPowerUp { id = 7, kind = (int)ZLPowerUp.Nuke, x = 3, y = 4 });
            var bytes = ZLNet.Snapshot(s);
            Assert.Less(bytes.Length, 600, "small enough to send ten times a second");
            var o = ZLNet.ReadSnapshot(bytes);
            Assert.IsNotNull(o);
            Assert.AreEqual(12, o.round);
            Assert.IsTrue(o.intermission && o.houndRound && o.boxBusy && o.doublePoints && !o.over);
            Assert.AreEqual(0b1011, o.doorMask);
            Assert.AreEqual(1, o.members.Count);
            var m = o.members[0];
            Assert.AreEqual(12345, m.points);
            Assert.AreEqual(20.25f, m.y, 0.01f);
            Assert.IsTrue(m.downed && m.forged && !m.out_);
            Assert.AreEqual("assault_rifle", ZLNet.GunId(m.gunIndex));
            CollectionAssertEqual(new List<string> { "ThickSkin", "QuickHands" }, ZLNet.PerksFromMask(m.perkMask));
            Assert.AreEqual(30, o.zombies.Count);
            Assert.AreEqual(1029, o.zombies[29].id);
            Assert.AreEqual(45.5f, o.zombies[3].y, 0.01f);
            Assert.AreEqual((int)ZLPowerUp.Nuke, o.powerUps[0].kind);
            Assert.IsNull(ZLNet.ReadSnapshot(new byte[] { 4, 1, 2 }), "a cut-off packet is rejected");
        }

        [Test]
        public void Shot_RoundTrip()
        {
            Assert.IsTrue(ZLNet.ReadShot(ZLNet.Shot(3, 5.5f, 6.25f, 0f, -1f, 2), out int slot, out float x, out float y, out float dx, out float dy, out int g));
            Assert.AreEqual(3, slot); Assert.AreEqual(5.5f, x, 0.01f); Assert.AreEqual(-1f, dy, 0.01f); Assert.AreEqual(2, g);
        }
    }
}
