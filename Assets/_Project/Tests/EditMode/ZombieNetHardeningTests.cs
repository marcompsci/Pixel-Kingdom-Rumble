using System.Collections.Generic;
using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    /// <summary>Online co-op hardening: link health, interpolation, ordering, version checks and packet sizes.</summary>
    [TestFixture]
    public class ZombieNetHardeningTests
    {
        [Test]
        public void Health_GoesLaggyThenLost_AndRecoversWhenHeard()
        {
            var h = new NetHealth();
            h.Heard(10.0);
            Assert.AreEqual(NetQuality.Good, h.Quality(10.5));
            Assert.AreEqual(NetQuality.Laggy, h.Quality(10.0 + NetHealth.LaggyAfter + 0.1));
            StringAssert.Contains("RECONNECTING", h.Label(12.0));
            Assert.AreEqual(NetQuality.Lost, h.Quality(10.0 + NetHealth.HostLostAfter));
            Assert.AreEqual(NetQuality.Lost, h.Quality(10.0 + NetHealth.GuestDropAfter, NetHealth.GuestDropAfter), "guests drop sooner");
            h.Heard(30.0);
            Assert.AreEqual(NetQuality.Good, h.Quality(30.2));
        }

        [Test]
        public void Health_SmoothsRoundTrip_AndFlagsSlowLinks()
        {
            var h = new NetHealth();
            h.Pong(1.08, 1.0);
            Assert.AreEqual(80f, h.RttMs, 0.5f);
            h.Pong(2.48, 2.0);   // one slow pong only nudges it
            Assert.Greater(h.RttMs, 80f);
            Assert.Less(h.RttMs, 480f);
            for (int i = 0; i < 20; i++) h.Pong(3.0 + i + 0.4, 3.0 + i);
            Assert.AreEqual(NetQuality.Laggy, h.Quality(23.5));
            StringAssert.Contains("ms", h.Label(23.5));
        }

        [Test]
        public void Interp_DrawsBetweenSamples_InThePast()
        {
            var n = new NetInterp { Delay = 0.1f };
            n.Push(1.0, 0f, 0f);
            n.Push(1.1, 1f, 0f);
            n.Push(1.2, 2f, 0f);
            Assert.IsTrue(n.Sample(1.25, out float x, out _));
            Assert.AreEqual(1.5f, x, 0.01f, "0.1 s behind: halfway between the samples at 1.1 and 1.2");
            n.Sample(1.3, out x, out _);
            Assert.AreEqual(2f, x, 0.01f);
        }

        [Test]
        public void Interp_ExtrapolatesBriefly_ThenHolds()
        {
            var n = new NetInterp { Delay = 0.1f, MaxExtrapolate = 0.15f };
            n.Push(1.0, 0f, 0f);
            n.Push(1.1, 1f, 0f);
            n.Sample(1.25, out float x, out _);   // 0.05 s past the newest
            Assert.AreEqual(1.5f, x, 0.01f);
            n.Sample(5.0, out x, out _);
            Assert.AreEqual(2.5f, x, 0.01f, "stops 0.15 s past the newest sample");
        }

        [Test]
        public void Interp_SnapsBigJumps_AndIgnoresOldSamples()
        {
            var n = new NetInterp { Delay = 0.1f };
            n.Push(1.0, 0f, 0f);
            n.Push(1.1, 1f, 0f);
            n.Push(1.05, 50f, 0f);   // late packet
            n.Sample(1.2, out float x, out _);
            Assert.AreEqual(1f, x, 0.01f);
            n.Push(1.2, 40f, 0f);    // teleport
            Assert.AreEqual(1, n.Count);
            n.Sample(1.21, out x, out _);
            Assert.AreEqual(40f, x, 0.01f);
        }

        [Test]
        public void Seq_ComparesAcrossTheWrap()
        {
            Assert.IsTrue(ZLNet.SeqNewer(5, 4));
            Assert.IsFalse(ZLNet.SeqNewer(4, 5));
            Assert.IsFalse(ZLNet.SeqNewer(7, 7));
            Assert.IsTrue(ZLNet.SeqNewer(2, 65534), "2 comes after 65534");
            Assert.IsFalse(ZLNet.SeqNewer(65534, 2));
        }

        [Test]
        public void Hello_FromAnotherVersion_IsReadable_ButNotAccepted()
        {
            var d = ZLNet.Hello("FRIEND", "brick");
            d[1] = (byte)(ZLNet.Version - 1);
            Assert.IsFalse(ZLNet.ReadHello(d, out _, out _));
            Assert.IsTrue(ZLNet.ReadHello(d, out var name, out var hero, out int v));
            Assert.AreEqual("FRIEND", name);
            Assert.AreEqual("brick", hero);
            Assert.AreEqual(ZLNet.Version - 1, v);
        }

        [Test]
        public void Reject_Roster_PingPong_RoundTrip()
        {
            Assert.IsTrue(ZLNet.ReadReject(ZLNet.Reject(NetReject.Version), out var why, out int hv));
            Assert.AreEqual(NetReject.Version, why);
            Assert.AreEqual(ZLNet.Version, hv);
            StringAssert.Contains("newer", ZLNet.RejectText(NetReject.Version, ZLNet.Version + 1));
            StringAssert.Contains("older", ZLNet.RejectText(NetReject.Version, ZLNet.Version - 1));
            StringAssert.Contains("full", ZLNet.RejectText(NetReject.Full, ZLNet.Version));

            var roster = new List<(string, string)> { ("HOST", "nova"), ("FRIEND", "brick") };
            Assert.IsTrue(ZLNet.ReadRoster(ZLNet.Roster(roster), out var back));
            Assert.AreEqual(2, back.Count);
            Assert.AreEqual("FRIEND", back[1].name);
            Assert.AreEqual("brick", back[1].heroId);

            Assert.AreEqual(NetMsg.Ping, ZLNet.Kind(ZLNet.Ping(123456)));
            Assert.IsTrue(ZLNet.ReadStamp(ZLNet.Pong(-5), out int stamp));
            Assert.AreEqual(-5, stamp);
        }

        [Test]
        public void Snapshot_WorstCase_FitsOneUnreliablePacket()
        {
            var s = new NetSnapshot { tick = int.MaxValue, round = 999, zombiesLeft = 999 };
            for (int i = 0; i < 4; i++) s.members.Add(new NetMember { slot = i, points = 9999999, perkMask = 255 });
            for (int i = 0; i < 255; i++) s.zombies.Add(new NetZombie { id = i, kind = 3, x = 60f, y = 40f, hp01 = 200 });
            for (int i = 0; i < 20; i++) s.powerUps.Add(new NetPowerUp { id = i, kind = 1, x = 2f, y = 3f });
            var bytes = ZLNet.Snapshot(s);
            Assert.Less(bytes.Length, ZLNet.MaxUnreliableBytes);
            var back = ZLNet.ReadSnapshot(bytes);
            Assert.IsNotNull(back);
            Assert.AreEqual(ZLNet.MaxSnapshotZombies, back.zombies.Count);
            Assert.AreEqual(4, back.members.Count);
        }

        [Test]
        public void Snapshot_AtTheSquadCap_SendsEveryZombie()
        {
            int cap = SquadRules.MaxAlive(99, SquadRules.MaxSquad);
            Assert.LessOrEqual(cap, ZLNet.MaxSnapshotZombies, "a full Last Stand round always fits");
        }
    }
}
