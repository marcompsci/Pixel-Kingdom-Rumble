using System.Collections.Generic;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Online Last Stand in play. The HOST runs the round (zombies, points, doors, the box) and sends a snapshot ten
    /// times a second; GUESTS move their own hero, send their controls twenty times a second and draw everything else
    /// from the snapshots. If a guest drops, an AI takes over their hero; if the host drops, the run ends for the guests.
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

        ZLGame _g;
        readonly Dictionary<string, int> _slotOf = new Dictionary<string, int>();
        readonly Dictionary<int, ZLRemoteBrain> _brains = new Dictionary<int, ZLRemoteBrain>();
        float _snapT, _inputT;
        int _tick, _seq, _useCount, _meleeCount, _swapCount;
        string _hostId;

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
                if (s.slot == 0) _hostId = s.playerId;
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
                switch (ZLNet.Kind(data))
                {
                    case NetMsg.Input:
                        if (IsHost && _slotOf.TryGetValue(from, out int slot) && _brains.TryGetValue(slot, out var brain) && ZLNet.ReadInput(data, out var inp))
                            brain.Feed(inp);
                        break;
                    case NetMsg.Snapshot:
                        if (!IsHost && from == _hostId)
                        {
                            var snap = ZLNet.ReadSnapshot(data);
                            if (snap != null && _g.Stand != null) { _g.Stand.ApplySnapshot(snap, MySlot); SnapshotsReceived++; }
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
            if (!IsHost && id == _hostId)
            {
                if (HostLost) return;
                HostLost = true;
                _g.ShowToast("THE HOST LEFT. THE RUN IS OVER.", 4f);
                if (_g.Stand != null) _g.Stand.GuestOver(false);
                return;
            }
            if (IsHost && _slotOf.TryGetValue(id, out int slot) && _g.Stand != null)
            {
                _slotOf.Remove(id);
                _brains.Remove(slot);
                var h = _g.Stand.HeroInSlot(slot);
                if (h != null)
                {
                    _g.Stand.MakeAI(h);
                    _g.Announce($"{h.DisplayName} LEFT. AN AI TAKES OVER.");
                }
            }
        }

        void OnDestroy()
        {
            // Leaving the scene ends the match for this phone.
            if (PKRMatchService.Instance != null && PKRMatchService.Instance.Transport == Transport) PKRMatchService.Instance.EndMatch();
            else if (Transport != null) { Transport.SendToAll(ZLNet.Bye(), true); Transport.Disconnect(); }
        }
    }
}
