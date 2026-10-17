using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using PKR.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PKR.Tests
{
    /// <summary>
    /// Last Stand co-op in the real scene: the AI squad fights and revives; a wiped squad ends the run; online play
    /// over an in-memory link (the host moves a remote player and sends snapshots; a guest mirrors them) and the lobby.
    /// </summary>
    public class ZombieCoopPlayModeTests
    {
        string _careerBackup;
        int _shardsBackup;

        void Backup()
        {
            var save = Services.Save != null ? Services.Save.Data : null;
            if (save == null) return;
            if (save.zombieCareer == null) save.zombieCareer = new ZombieCareer();
            _careerBackup = JsonUtility.ToJson(save.zombieCareer);
            _shardsBackup = save.starShards;
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new ZombieCareer()), save.zombieCareer);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            GameSession.Mode = SessionMode.StoryQuest;
            GameSession.ZombieMode = SurvivalMode.Endless;
            GameSession.ZombieSquad = SquadKind.Solo;
            var save = Services.Save != null ? Services.Save.Data : null;
            if (save != null && _careerBackup != null)
            {
                JsonUtility.FromJsonOverwrite(_careerBackup, save.zombieCareer);
                save.starShards = _shardsBackup;
            }
            _careerBackup = null;
            if (PKRMatchService.Instance != null) Object.Destroy(PKRMatchService.Instance.gameObject);
        }

        IEnumerator Load(SquadKind squad)
        {
            if (!Application.CanStreamedLevelBeLoaded(SceneIds.LastStand)) Assert.Ignore("Last Stand isn't in the build.");
            Backup();
            GameSession.Mode = SessionMode.Zombies;
            GameSession.ZombieMode = SurvivalMode.Endless;
            GameSession.ZombieSquad = squad;
            SceneManager.LoadScene(SceneIds.LastStand, LoadSceneMode.Single);
            yield return null;
            float t = 0f;
            while ((ZLGame.Current == null || !ZLGame.Current.Ready || !ZLGame.Current.IsLastStand) && t < 10f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsNotNull(ZLGame.Current);
            Assert.IsNotNull(ZLGame.Current.Stand);
        }

        IEnumerator Calm()
        {
            if (Application.CanStreamedLevelBeLoaded(SceneIds.StoryTest))
            {
                SceneManager.LoadScene(SceneIds.StoryTest, LoadSceneMode.Single);
                yield return null;
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator AISquad_FourHeroes_TheyMoveAndShoot()
        {
            yield return Load(SquadKind.AISquad);
            var g = ZLGame.Current;
            var st = g.Stand;
            Assert.AreEqual(4, g.Squad.Count, "you and three AI teammates");
            Assert.AreEqual(SquadKind.AISquad, st.Kind);
            int ai = 0;
            foreach (var h in g.Squad) { h.DebugInvulnerable = true; if (h.Control == ZLControl.AI) { ai++; Assert.IsNotNull(h.Brain); Assert.IsNotNull(h.Wallet); } }
            Assert.AreEqual(3, ai);
            Assert.Greater(SquadRules.ZombiesInRound(1, 4), LastStandRules.ZombiesInRound(1), "bigger rounds for a squad");
            // straight into round 1, sped up (the dead walk in from the edges of the block)
            st.DebugSkipToRound(1);
            Time.timeScale = 3f;
            float t = 0f;
            int aiKills = 0;
            while (aiKills == 0 && t < 90f)
            {
                t += Time.deltaTime;
                aiKills = 0;
                foreach (var h in g.Squad) if (h.Control == ZLControl.AI) aiKills += h.Kills;
                yield return null;
            }
            Assert.Greater(aiKills, 0, "the AI teammates kill zombies");
            yield return Calm();
        }

        [UnityTest]
        public IEnumerator Downed_AITeammateRevives_AndAWipeEndsTheRun()
        {
            yield return Load(SquadKind.AISquad);
            var g = ZLGame.Current;
            var st = g.Stand;
            foreach (var h in g.Squad) h.DebugInvulnerable = h != g.Player;
            var me = g.Player;
            me.Hurt(99999, me.transform.position + Vector3.right);
            Assert.IsTrue(me.Downed, "down, not dead");
            Assert.IsFalse(st.Over);
            float t = 0f;
            while (me.Downed && t < 20f) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(me.Standing, "an AI teammate revived you");
            Assert.Greater(me.Health, 0);
            int revives = 0;
            foreach (var h in g.Squad) revives += st.RevivesBy(h);
            Assert.AreEqual(1, revives);

            foreach (var h in g.Squad) if (h.Standing) { h.SetHealth(0); g.HeroFell(h); }
            foreach (var h in g.Squad) Assert.IsFalse(h.Standing);
            Assert.IsTrue(st.Over, "nobody standing ends the run");
            yield return Calm();
        }

        [UnityTest]
        public IEnumerator Online_Host_MovesTheRemotePlayer_AndSendsSnapshots()
        {
            yield return Load(SquadKind.Solo);
            var g = ZLGame.Current;
            var st = g.Stand;
            var ends = LoopbackTransport.Create("A", "B");
            var slots = new List<NetSlot>
            {
                new NetSlot { slot = 0, playerId = "A", name = "HOST", heroId = "" },
                new NetSlot { slot = 1, playerId = "B", name = "GUEST", heroId = "" },
                new NetSlot { slot = 2, playerId = "", name = "AI", heroId = "", ai = true },
            };
            st.SetupOnline(ends[0], true, 0, slots);
            Assert.IsFalse(g.IsGuest);
            Assert.AreEqual(3, g.Squad.Count);
            var remote = st.HeroInSlot(1);
            Assert.AreEqual(ZLControl.Remote, remote.Control);
            Assert.AreEqual(ZLControl.AI, st.HeroInSlot(2).Control);
            foreach (var h in g.Squad) h.DebugInvulnerable = true;
            Vector2 goal = (Vector2)remote.transform.position + Vector2.right * 2f;
            var guest = ends[1];
            NetSnapshot last = null;
            float t = 0f;
            int seq = 0;
            while (t < 8f)
            {
                t += Time.deltaTime;
                guest.SendTo("A", ZLNet.Input(new NetInput { seq = ++seq, posX = goal.x, posY = goal.y, aimX = 1f }), false);
                while (guest.TryReceive(out var from, out var data))
                    if (ZLNet.Kind(data) == NetMsg.Snapshot) last = ZLNet.ReadSnapshot(data);
                if (last != null && last.zombies.Count > 0 && Vector2.Distance(remote.transform.position, goal) < 0.3f) break;
                yield return null;
            }
            Assert.Less(Vector2.Distance(remote.transform.position, goal), 0.3f, "the remote hero follows the guest's position");
            Assert.IsNotNull(last, "the host sends snapshots");
            Assert.AreEqual(3, last.members.Count);
            Assert.Greater(last.round, 0);
            Assert.Greater(last.zombies.Count, 0, "zombies are in the snapshot");
            Assert.Greater(g.Net.SnapshotsSent, 5);

            // the guest leaves: an AI takes over their hero
            guest.Disconnect();
            yield return null;
            yield return null;
            Assert.AreEqual(ZLControl.AI, remote.Control);
            yield return Calm();
        }

        [UnityTest]
        public IEnumerator Online_Guest_MirrorsTheHost()
        {
            yield return Load(SquadKind.Solo);
            var g = ZLGame.Current;
            var st = g.Stand;
            var ends = LoopbackTransport.Create("A", "B");
            var slots = new List<NetSlot>
            {
                new NetSlot { slot = 0, playerId = "A", name = "HOST", heroId = "" },
                new NetSlot { slot = 1, playerId = "B", name = "ME", heroId = "" },
            };
            st.SetupOnline(ends[1], false, 1, slots);
            Assert.IsTrue(g.IsGuest);
            var host = ends[0];
            var hostHero = st.HeroInSlot(0);
            Assert.AreEqual(ZLControl.Puppet, hostHero.Control);
            var snap = new NetSnapshot { tick = 1, round = 4, zombiesLeft = 2, doorMask = 1 };
            snap.members.Add(new NetMember { slot = 0, x = 20f, y = 8f, hp = 100, maxHp = 100, points = 900, gunIndex = 0 });
            snap.members.Add(new NetMember { slot = 1, x = g.Player.transform.position.x, y = g.Player.transform.position.y, hp = 40, maxHp = 100, points = 1234, kills = 7, mag = 5, reserve = 20, gunIndex = ZLNet.GunIndex("street_smg") });
            snap.zombies.Add(new NetZombie { id = 11, kind = (int)ZLFoe.Shambler, x = 25f, y = 6f, hp01 = 255 });
            snap.zombies.Add(new NetZombie { id = 12, kind = (int)ZLFoe.Runner, x = 26f, y = 6f, hp01 = 128 });
            host.SendTo("B", ZLNet.Snapshot(snap), false);
            yield return null;
            yield return null;
            Assert.AreEqual(4, st.Run.Round);
            Assert.AreEqual(1234, st.Run.Points);
            Assert.AreEqual(40, g.Player.Health, "the host decides your health");
            Assert.AreEqual("street_smg", g.Progress.equipped);
            Assert.AreEqual(2, st.Alive.Count, "puppet zombies");
            foreach (var z in st.Alive) Assert.IsTrue(z.Puppet);
            Assert.IsFalse(g.City.Blocked(g.City.doors[0].x, g.City.doors[0].y), "the first debris pile is cleared");
            var doorLeft = new NetSnapshot { tick = 2, round = 4, zombiesLeft = 1, doorMask = 1 };
            doorLeft.members.AddRange(snap.members);
            doorLeft.zombies.Add(snap.zombies[0]);
            host.SendTo("B", ZLNet.Snapshot(doorLeft), false);
            yield return null;
            yield return null;
            Assert.AreEqual(1, st.Alive.Count, "a zombie the host killed is gone");
            host.SendTo("B", ZLNet.Over(false, 4, 30), true);
            yield return null;
            yield return null;
            Assert.IsTrue(st.Over, "the host ended the run");
            yield return Calm();
        }

        [UnityTest]
        public IEnumerator Lobby_HostCollectsHellos_AndSendsStartWithAI()
        {
            var svc = PKRMatchService.Ensure();
            svc.DontLoadScene = true;
            svc.FillWithAI = true;
            var ends = LoopbackTransport.Create("G:1", "G:2");
            svc.BeginLobby(ends[0], SurvivalMode.Raid);
            Assert.IsTrue(svc.IsHost, "the lowest id hosts");
            ends[1].SendTo("G:1", ZLNet.Hello("FRIEND", "brick"), true);
            float t = 0f;
            while (svc.State != PKRMatchService.Phase.InGame && t < 3f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.AreEqual(PKRMatchService.Phase.InGame, svc.State);
            Assert.AreEqual(SquadRules.MaxSquad, svc.Slots.Count, "two players and two AI");
            Assert.AreEqual("FRIEND", svc.Slots[1].name);
            Assert.IsTrue(svc.Slots[3].ai);
            Assert.AreEqual(SquadKind.Online, GameSession.ZombieSquad);
            bool gotStart = false;
            while (ends[1].TryReceive(out var from, out var data))
                if (ZLNet.Kind(data) == NetMsg.Start && ZLNet.ReadStart(data, out var mode, out var slots)) { gotStart = mode == SurvivalMode.Raid && slots.Count == 4; }
            Assert.IsTrue(gotStart, "the guest gets START");
        }

        [UnityTest]
        public IEnumerator Online_SilentGuest_GoesToAI_AndGetsTheirHeroBack()
        {
            yield return Load(SquadKind.Solo);
            var g = ZLGame.Current;
            var st = g.Stand;
            var ends = LoopbackTransport.Create("A", "B");
            var slots = new List<NetSlot>
            {
                new NetSlot { slot = 0, playerId = "A", name = "HOST", heroId = "" },
                new NetSlot { slot = 1, playerId = "B", name = "FRIEND", heroId = "" },
            };
            st.SetupOnline(ends[0], true, 0, slots);
            double clock = 100.0;
            g.Net.Clock = () => clock;
            foreach (var h in g.Squad) h.DebugInvulnerable = true;
            var remote = st.HeroInSlot(1);
            var guest = ends[1];
            int seq = 0;
            guest.SendTo("A", ZLNet.Input(new NetInput { seq = ++seq, posX = remote.transform.position.x, posY = remote.transform.position.y, aimX = 1f }), false);
            yield return null;
            Assert.AreEqual(ZLControl.Remote, remote.Control);
            // a ping reaches the guest, and their pong gives the host a round trip
            bool pinged = false;
            while (guest.TryReceive(out var from, out var data))
                if (ZLNet.Kind(data) == NetMsg.Ping && ZLNet.ReadStamp(data, out int stamp)) { pinged = true; guest.SendTo("A", ZLNet.Pong(stamp), false); }
            Assert.IsTrue(pinged, "the host pings");
            clock += 0.06;
            yield return null;
            Assert.AreEqual(60f, g.Net.HealthOf("B").RttMs, 5f, "round trip from the pong");

            // silence longer than the drop time: an AI takes the seat
            clock += NetHealth.GuestDropAfter + 0.5;
            yield return null;
            yield return null;
            Assert.AreEqual(ZLControl.AI, remote.Control, "a silent guest is handed to an AI");
            Assert.AreEqual(1, g.Net.DroppedCount);

            // their controls come back: so does their hero
            guest.SendTo("A", ZLNet.Input(new NetInput { seq = ++seq, posX = remote.transform.position.x, posY = remote.transform.position.y, aimX = 1f }), false);
            yield return null;
            yield return null;
            Assert.AreEqual(ZLControl.Remote, remote.Control);
            Assert.AreEqual(0, g.Net.DroppedCount);
            Assert.AreEqual("FRIEND", remote.DisplayName);

            // an old, late packet is ignored
            guest.SendTo("A", ZLNet.Input(new NetInput { seq = seq - 1, posX = 0f, posY = 0f }), false);
            yield return null;
            Assert.Greater(Vector2.Distance(remote.transform.position, Vector2.zero), 0.5f);
            yield return Calm();
        }

        [UnityTest]
        public IEnumerator Online_Guest_IgnoresOldSnapshots_AndEndsWhenTheHostGoesQuiet()
        {
            yield return Load(SquadKind.Solo);
            var g = ZLGame.Current;
            var st = g.Stand;
            var ends = LoopbackTransport.Create("A", "B");
            var slots = new List<NetSlot>
            {
                new NetSlot { slot = 0, playerId = "A", name = "HOST", heroId = "" },
                new NetSlot { slot = 1, playerId = "B", name = "ME", heroId = "" },
            };
            st.SetupOnline(ends[1], false, 1, slots);
            double clock = 50.0;
            g.Net.Clock = () => clock;
            var host = ends[0];
            NetSnapshot Snap(int tick, int round)
            {
                var s = new NetSnapshot { tick = tick, round = round, zombiesLeft = 3 };
                s.members.Add(new NetMember { slot = 0, x = 20f, y = 8f, hp = 100, maxHp = 100 });
                s.members.Add(new NetMember { slot = 1, x = g.Player.transform.position.x, y = g.Player.transform.position.y, hp = 100, maxHp = 100 });
                return s;
            }
            host.SendTo("B", ZLNet.Snapshot(Snap(10, 6)), false);
            yield return null;
            host.SendTo("B", ZLNet.Snapshot(Snap(9, 5)), false);
            yield return null;
            Assert.AreEqual(6, st.Run.Round, "a late snapshot doesn't step back");
            Assert.AreEqual(1, g.Net.OutOfOrderSnapshots);
            Assert.AreEqual(NetQuality.Good, g.Net.Quality);
            clock += 3.0;
            yield return null;
            Assert.AreEqual(NetQuality.Laggy, g.Net.Quality);
            StringAssert.Contains("RECONNECTING", g.Net.StatusLine);
            Assert.IsFalse(st.Over);
            clock += NetHealth.HostLostAfter;
            yield return null;
            yield return null;
            Assert.IsTrue(g.Net.HostLost);
            Assert.IsTrue(st.Over, "a host gone quiet ends the run");
            yield return Calm();
        }

        [UnityTest]
        public IEnumerator Lobby_TurnsAwayAnotherVersion_AndStartsNowOnRequest()
        {
            var svc = PKRMatchService.Ensure();
            svc.DontLoadScene = true;
            svc.FillWithAI = false;
            var ends = LoopbackTransport.Create("G:1", "G:2", "G:3");
            svc.BeginLobby(ends[0], SurvivalMode.Endless);
            Assert.IsTrue(svc.IsHost);
            var old = ZLNet.Hello("OLDGAME", "");
            old[1] = (byte)(ZLNet.Version - 1);
            ends[2].SendTo("G:1", old, true);
            yield return null;
            bool rejected = false;
            while (ends[2].TryReceive(out var from, out var data))
                if (ZLNet.Kind(data) == NetMsg.Reject && ZLNet.ReadReject(data, out var why, out _)) rejected = why == NetReject.Version;
            Assert.IsTrue(rejected, "an older game is told to update");
            Assert.IsFalse(svc.CanStartNow, "nobody valid has joined yet");
            ends[1].SendTo("G:1", ZLNet.Hello("FRIEND", "luma"), true);
            float t = 0f;
            while (svc.State != PKRMatchService.Phase.InGame && t < 3f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.AreEqual(PKRMatchService.Phase.InGame, svc.State, "everyone valid is in: it starts");
            Assert.AreEqual(2, svc.Slots.Count, "the old game isn't seated");
            Assert.AreEqual("FRIEND", svc.Slots[1].name);
        }
    }
}
