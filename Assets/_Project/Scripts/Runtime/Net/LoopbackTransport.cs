using System.Collections.Generic;

namespace PKR
{
    /// <summary>
    /// Two (or more) transports wired together in memory: what one sends, the others receive. Used by tests and the
    /// Editor to run the online code without Game Center.
    /// </summary>
    public class LoopbackTransport : IZLTransport
    {
        class Hub { public readonly List<LoopbackTransport> Ends = new List<LoopbackTransport>(); }

        readonly Hub _hub;
        readonly Queue<(string, byte[])> _in = new Queue<(string, byte[])>();
        readonly Queue<string> _left = new Queue<string>();
        readonly List<string> _peers = new List<string>();
        bool _open = true;

        public string LocalId { get; }
        public IReadOnlyList<string> PeerIds
        {
            get
            {
                _peers.Clear();
                foreach (var e in _hub.Ends) if (e != this && e._open) _peers.Add(e.LocalId);
                return _peers;
            }
        }
        public bool Connected => _open;
        /// <summary>Messages sent by this end (tests read them).</summary>
        public int SentCount { get; private set; }

        LoopbackTransport(Hub hub, string id) { _hub = hub; LocalId = id; }

        /// <summary>Makes a set of connected ends, one per id.</summary>
        public static List<LoopbackTransport> Create(params string[] ids)
        {
            var hub = new Hub();
            var list = new List<LoopbackTransport>();
            foreach (var id in ids) { var e = new LoopbackTransport(hub, id); hub.Ends.Add(e); list.Add(e); }
            return list;
        }

        public void SendToAll(byte[] data, bool reliable)
        {
            if (!_open) return;
            SentCount++;
            foreach (var e in _hub.Ends) if (e != this && e._open) e._in.Enqueue((LocalId, (byte[])data.Clone()));
        }

        public void SendTo(string peerId, byte[] data, bool reliable)
        {
            if (!_open) return;
            SentCount++;
            foreach (var e in _hub.Ends) if (e.LocalId == peerId && e._open) e._in.Enqueue((LocalId, (byte[])data.Clone()));
        }

        public bool TryReceive(out string fromId, out byte[] data)
        {
            if (_in.Count == 0) { fromId = null; data = null; return false; }
            (fromId, data) = _in.Dequeue();
            return true;
        }

        public string TakeLeft() => _left.Count > 0 ? _left.Dequeue() : null;

        public void Disconnect()
        {
            if (!_open) return;
            _open = false;
            foreach (var e in _hub.Ends) if (e != this && e._open) e._left.Enqueue(LocalId);
        }
    }
}
