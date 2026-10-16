using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace PKR
{
    /// <summary>The online link over a Game Center real-time match (PKRMatch.mm). iPhone only.</summary>
    public class GameCenterTransport : IZLTransport
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern int PKR_MP_State();
        [DllImport("__Internal")] static extern int PKR_MP_LocalId(byte[] buf, int cap);
        [DllImport("__Internal")] static extern int PKR_MP_PeerCount();
        [DllImport("__Internal")] static extern int PKR_MP_PeerId(int i, byte[] buf, int cap);
        [DllImport("__Internal")] static extern void PKR_MP_SendAll(byte[] data, int len, int reliable);
        [DllImport("__Internal")] static extern void PKR_MP_SendTo(string peerId, byte[] data, int len, int reliable);
        [DllImport("__Internal")] static extern int PKR_MP_Receive(byte[] buf, int cap, byte[] fromBuf, int fromCap);
        [DllImport("__Internal")] static extern int PKR_MP_TakeLeft(byte[] buf, int cap);
        [DllImport("__Internal")] static extern void PKR_MP_Disconnect();
#endif
        byte[] _buf = new byte[8192];
        readonly byte[] _idBuf = new byte[256];
        readonly List<string> _peers = new List<string>();

        static string Str(byte[] b, int n) => n > 0 ? Encoding.UTF8.GetString(b, 0, n) : "";

        public string LocalId
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR
                return Str(_idBuf, PKR_MP_LocalId(_idBuf, _idBuf.Length));
#else
                return "local";
#endif
            }
        }

        public IReadOnlyList<string> PeerIds
        {
            get
            {
                _peers.Clear();
#if UNITY_IOS && !UNITY_EDITOR
                int n = PKR_MP_PeerCount();
                for (int i = 0; i < n; i++) _peers.Add(Str(_idBuf, PKR_MP_PeerId(i, _idBuf, _idBuf.Length)));
#endif
                return _peers;
            }
        }

        public bool Connected
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR
                return PKR_MP_State() == 2;
#else
                return false;
#endif
            }
        }

        public void SendToAll(byte[] data, bool reliable)
        {
#if UNITY_IOS && !UNITY_EDITOR
            PKR_MP_SendAll(data, data.Length, reliable ? 1 : 0);
#endif
        }

        public void SendTo(string peerId, byte[] data, bool reliable)
        {
#if UNITY_IOS && !UNITY_EDITOR
            PKR_MP_SendTo(peerId, data, data.Length, reliable ? 1 : 0);
#endif
        }

        public bool TryReceive(out string fromId, out byte[] data)
        {
            fromId = null; data = null;
#if UNITY_IOS && !UNITY_EDITOR
            int n = PKR_MP_Receive(_buf, _buf.Length, _idBuf, _idBuf.Length);
            if (n < 0) { _buf = new byte[-n + 1024]; n = PKR_MP_Receive(_buf, _buf.Length, _idBuf, _idBuf.Length); }
            if (n <= 0) return false;
            data = new byte[n];
            System.Array.Copy(_buf, data, n);
            int end = System.Array.IndexOf(_idBuf, (byte)0);
            fromId = Str(_idBuf, end >= 0 ? end : _idBuf.Length);
            return true;
#else
            return false;
#endif
        }

        public string TakeLeft()
        {
#if UNITY_IOS && !UNITY_EDITOR
            int n = PKR_MP_TakeLeft(_idBuf, _idBuf.Length);
            return n > 0 ? Str(_idBuf, n - 1) : null;
#else
            return null;
#endif
        }

        public void Disconnect()
        {
#if UNITY_IOS && !UNITY_EDITOR
            PKR_MP_Disconnect();
#endif
        }
    }
}
