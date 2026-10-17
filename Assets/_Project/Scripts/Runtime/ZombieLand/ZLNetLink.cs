using System.Collections.Generic;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Online Last Stand in play. The HOST runs the round (zombies, points, doors, the box) and sends a snapshot ten
    /// times a second; GUESTS move their own hero, send their controls twenty times a second and draw everything else
    /// from the snapshots. Both sides ping once a second, so each knows how the link is doing (shown on the HUD).
    /// A guest who goes quiet (or whom Game Center reports gone) is handed to an AI; if their controls arrive again,
    /// they get their hero back. A guest whose host goes quiet sees "reconnecting"; the run ends only when Game Center
    /// says the host left or nothing has arrived for <see cref="NetHealth.HostLostAfter"/> seconds.
    /// </summary>
    public class ZLNetLink : MonoBehaviour
    {
        public IZLTransport Transport { get; private set; }
        public bool IsHost { get; private set; }
        public int MySlot { get; private set; }
        public List<NetSlot> Slots { get; private set; }
        public int SnapshotsSent { get; private set; }
        public int SnapshotsReceived { get; private set; }
        public bool HostLost { get; private set; }
        /// <summary>Guest: the link to the host. Host: the worst link to a guest.</summary>
        public NetQuality Quality { get; private set; } = NetQuality.Good;
        public string StatusLine { get; private set; } = "";
        /// <summary>Guests handed to an AI who can still come back (slot -> player id).</summary>
        public int DroppedCount => _dropped.Count;
        public int OutOfOrderSnapshots { get; private set; }

        ZLGame _g;
        readonly Dictionary<string, int> _slotOf = new Dictionary<string, int>();
        readonly Dictionary<int, ZLRemoteBrain> _brains = new Dictionary<int, ZLRemoteBrain>();
        readonly Dictionary<string, NetHealth> _health = new Dictionary<string, NetHealth>();
        readonly Dictionary<int, string> _dropped = new Dictionary<int, string>();
        readonly Dictionary<int, string> _names = new Dictionary<int, string>();
        float _snapT, _inputT, _pingT;
        int _tick, _seq, _useCount, _meleeCount, _swapCount, _lastTick = -1;
        string _hostId;

        /// <summary>Tests: a clock to drive time-outs without waiting.</summary>
        public System.Func<double> Clock
        {
            get => _clock;
            set { _clock = value; foreach (var h in _health.Values) h.Reset(Now); }
        }
        System.Func<double> _clock;
        double Now => _clock != null ? _clock() : Time.unscaledTimeAsDouble;

        NetHealth Health(string id)
        {
            if (!_health.TryGetValue(id ?? "", out var h)) { h = new NetHealth(); h.Heard(Now); _health[id ?? ""] = h; }
            return h;
        }

        public NetHealth HostHealth => Health(_hostId);
        public NetHealth HealthOf(string playerId) => Health(playerId);

        public void Init(ZLGame g, IZLTransport transport, bool isHost, int mySlot, List<NetSlot> slots)
        {
            _g = g;
            Transport = transport;
            IsHost = isHost;
            MySlot = mySlot;
            Slots = slots;
            foreach (var s in slots)
            {
                if (s.ai || string.IsNullOrEmpty(s.playerId)) continue;
                _slotOf[s.playerId] = s.slot;
                _names[s.slot] = s.name;
                if (s.slot == 0) _hostId = s.playerId;
                if (s.playerId != transport.LocalId) Health(s.playerId);
            }
        }

        public void RegisterRemote(int slot, ZLRemoteBrain brain) => _brains[slot] = brain;

        // ---- sending -------------------------------------------------------------------------------------

        public void BroadcastToast(string text, int sfx)
        {
            if (IsHost && Transport != null) Transport.SendToAll(ZLNet.Toast(text, sfx), true);
        }

        public void BroadcastShot(int slot, Vector2 from, Vector2 dir, string gunId)
        {
            if (IsHost && Transport != null) Transport.SendToAll(ZLNet.Shot(slot, from.x, from.y, dir.x, dir.y, ZLNet.GunIndex(gunId)), false);
        }

        public void BroadcastOver(bool won, int round, int kills)
        {
            if (IsHost && Transport != null) Transport.SendToAll(ZLNet.Over(won, round, kills), true);
        }

        // ---- the loop ------------------------------------------------------------------------------------

        void Update()
        {
            if (_g == null || Transport == null) return;
            float dt = Time.unscaledDeltaTime;
            Receive();
            string left;
            while ((left = Transport.TakeLeft()) != null) PlayerLeft(left);
            _pingT -= dt;
            if (_pingT <= 0f)
            {
                _pingT = 1f;
                Transport.SendToAll(ZLNet.Ping(StampMs(Now)), false);
            }
            Watch();
            if (IsHost)
            {
                _snapT -= dt;
                if (_snapT <= 0f && _g.Stand != null)
                {
                    _snapT = 0.1f;
                    Transport.SendToAll(ZLNet.Snapshot(_g.Stand.BuildSnapshot(++_tick)), false);
                    SnapshotsSent++;
                }
            }
            else
            {
                CountPresses();
                _inputT -= dt;
                if (_inputT <= 0f) { _inputT = 0.05f; SendInput(); }
            }
        }

        static int StampMs(double t) => (int)(long)(t * 1000.0);

        /// <summary>Time-outs: hand a silent guest to an AI; tell a guest their host is slow or gone.</summary>
        void Watch()
        {
            double now = Now;
            if (IsHost)
            {
                var worst = NetQuality.Good;
                string worstLine = "";
                foreach (var kv in _slotOf)
                {
                    if (kv.Key == Transport.LocalId) continue;
                    var h = Health(kv.Key);
                    if (!_dropped.ContainsKey(kv.Value) && h.Silence(now) >= NetHealth.GuestDropAfter) Drop(kv.Value, kv.Key, "LOST CONNECTION");
                    var q = h.Quality(now, NetHealth.GuestDropAfter);
                    if (q > worst || worstLine == "") { worst = q > worst ? q : worst; worstLine = h.Label(now); }
                }
                Quality = worst;
                StatusLine = worstLine == "" ? "ONLINE" : worstLine;
                return;
            }
            if (HostLost) { Quality = NetQuality.Lost; StatusLine = "CONNECTION LOST"; return; }
            var hh = Health(_hostId);
            Quality = hh.Quality(now);
            StatusLine = hh.Label(now);
            if (Quality == NetQuality.Lost) LoseHost("LOST THE HOST. THE RUN IS OVER.");
        }

        /// <summary>Host: an AI plays this guest's hero until they're heard from again.</summary>
        void Drop(int slot, string id, string why)
        {
            if (_g.Stand == null || _dropped.ContainsKey(slot)) return;
            var h = _g.Stand.HeroInSlot(slot);
            if (h == null || h.Control != ZLControl.Remote) return;
            _dropped[slot] = id;
            _g.Stand.MakeAI(h);
            _g.Announce($"{Name(slot)} {why}. AN AI TAKES OVER.");
        }

        /// <summary>Host: a dropped guest's controls arrived again: they get their hero back.</summary>
        void Restore(int slot)
        {
            if (!_dropped.ContainsKey(slot) || _g.Stand == null) return;
            var h = _g.Stand.HeroInSlot(slot);
            if (h == null || !_brains.TryGetValue(slot, out var brain)) return;
            _dropped.Remove(slot);
            h.BecomeRemote(brain);
            h.DisplayName = Name(slot);
            _g.Announce($"{Name(slot)} IS BACK.");
        }

        string Name(int slot) => _names.TryGetValue(slot, out var n) && !string.IsNullOrEmpty(n) ? n : "PLAYER " + (slot + 1);

        void LoseHost(string text)
        {
            if (HostLost) return;
            HostLost = true;
            Quality = NetQuality.Lost;
            StatusLine = "CONNECTION LOST";
            _g.ShowToast(text, 4f);
            if (_g.Stand != null) _g.Stand.GuestOver(false);
        }

        void CountPresses()
        {
            var me = _g.Player;
            if (me == null) return;
            var f = me.LastFrame;
            if (f.usePressed) _useCount = (_useCount + 1) & 0xff;
            if (f.melee) _meleeCount = (_meleeCount + 1) & 0xff;
            if (f.swap) _swapCount = (_swapCount + 1) & 0xff;
        }

        void SendInput()
        {
            var me = _g.Player;
            if (me == null) return;
            var f = me.LastFrame;
            var i = new NetInput
            {
                seq = ++_seq, moveX = f.move.x, moveY = f.move.y, aimX = me.Aim.x, aimY = me.Aim.y,
                posX = me.transform.position.x, posY = me.transform.position.y, fire = f.fire, useHeld = f.useHeld,
                useCount = _useCount, meleeCount = _meleeCount, swapCount = _swapCount
            };
            Transport.SendTo(_hostId ?? "", ZLNet.Input(i), false);
        }

        void Receive()
        {
            while (Transport.TryReceive(out var from, out var data))
            {
                if (from != null && from != Transport.LocalId && (IsHost ? _slotOf.ContainsKey(from) : from == _hostId)) Health(from).Heard(Now);
                switch (ZLNet.Kind(data))
                {
                    case NetMsg.Ping:
                        if (ZLNet.ReadStamp(data, out int stamp)) Transport.SendTo(from, ZLNet.Pong(stamp), false);
                        break;
                    case NetMsg.Pong:
                        if (ZLNet.ReadStamp(data, out int sent) && (IsHost ? _slotOf.ContainsKey(from) : from == _hostId))
                        {
                            double now = Now;
                            // stamps are the low 32 bits of our clock in ms
                            double back = ((StampMs(now) - (long)sent) & 0xffffffffL) / 1000.0;
                            Health(from).Pong(now, now - back);
                        }
                        break;
                    case NetMsg.Input:
                        if (IsHost && _slotOf.TryGetValue(from, out int slot) && _brains.TryGetValue(slot, out var brain) && ZLNet.ReadInput(data, out var inp))
                        {
                            brain.Feed(inp);
                            if (_dropped.ContainsKey(slot)) Restore(slot);
                        }
                        break;
                    case NetMsg.Snapshot:
                        if (!IsHost && from == _hostId && !HostLost)
                        {
                            var snap = ZLNet.ReadSnapshot(data);
                            if (snap == null || _g.Stand == null) break;
                            // unreliable packets can arrive out of order: never step back in time
                            if (snap.tick <= _lastTick) { OutOfOrderSnapshots++; break; }
                            _lastTick = snap.tick;
                            _g.Stand.ApplySnapshot(snap, MySlot);
                            SnapshotsReceived++;
                        }
                        break;
                    case NetMsg.Toast:
                        if (!IsHost && ZLNet.ReadToast(data, out var text, out int sfx))
                        {
                            _g.ShowToast(text, 3f);
                            if (sfx == 1) Sfx.Play(Sfx.PowerUp, 0.8f, 0f);
                        }
                        break;
                    case NetMsg.Shot:
                        if (!IsHost && ZLNet.ReadShot(data, out int s, out float x, out float y, out float dx, out float dy, out int gi) && s != MySlot && _g.Stand != null)
                        {
                            var who = _g.Stand.HeroInSlot(s);
                            if (who != null) who.ShowShot(new Vector2(x, y), new Vector2(dx, dy), ZLCatalog.Gun(ZLNet.GunId(gi)));
                        }
                        break;
                    case NetMsg.Over:
                        if (!IsHost && ZLNet.ReadOver(data, out bool won, out int round, out int kills) && _g.Stand != null)
                            _g.Stand.GuestOver(won);
                        break;
                    case NetMsg.Bye:
                        PlayerLeft(from);
                        break;
                }
            }
        }

        void PlayerLeft(string id)
        {
            if (!IsHost && id == _hostId) { LoseHost("THE HOST LEFT. THE RUN IS OVER."); return; }
            // Keep their seat: if Game Center reconnects them (two-player matches), their controls hand the hero back.
            if (IsHost && _slotOf.TryGetValue(id, out int slot)) Drop(slot, id, "LEFT");
        }

        void OnDestroy()
        {
            // Leaving the scene ends the match for this phone.
            if (PKRMatchService.Instance != null && PKRMatchService.Instance.Transport == Transport) PKRMatchService.Instance.EndMatch();
            else if (Transport != null) { Transport.SendToAll(ZLNet.Bye(), true); Transport.Disconnect(); }
        }
    }
}
