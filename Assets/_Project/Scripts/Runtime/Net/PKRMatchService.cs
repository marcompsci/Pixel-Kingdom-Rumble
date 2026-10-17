using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Online co-op lobby (lives across scenes). FIND PLAYERS opens Game Center's own invite / matchmaking screen;
    /// accepted invites land here too. Once everyone is connected the host (lowest player id) collects each player's
    /// name and hero, fills empty seats with AI if asked, and sends START; every phone then loads Last Stand.
    /// </summary>
    public class PKRMatchService : MonoBehaviour
    {
        public enum Phase { Idle, Finding, Lobby, InGame, Failed }

        public static PKRMatchService Instance { get; private set; }

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void PKR_MP_Init();
        [DllImport("__Internal")] static extern void PKR_MP_Find(int minPlayers, int maxPlayers, int playerGroup);
        [DllImport("__Internal")] static extern int PKR_MP_State();
        [DllImport("__Internal")] static extern int PKR_MP_Error(byte[] buf, int cap);
        [DllImport("__Internal")] static extern int PKR_MP_LocalName(byte[] buf, int cap);
#endif

        public Phase State { get; private set; } = Phase.Idle;
        public string Status { get; private set; } = "";
        public IZLTransport Transport { get; private set; }
        public bool IsHost { get; private set; }
        public List<NetSlot> Slots { get; private set; } = new List<NetSlot>();
        public int MySlot { get; private set; }
        public SurvivalMode Mode { get; private set; }
        /// <summary>The Last Stand map (the host picks it; guests get it in START).</summary>
        public string MapId { get; set; } = LastStandRules.MapId;
        public bool FillWithAI { get; set; } = true;
        /// <summary>Tests: don't load the scene when the game starts.</summary>
        public bool DontLoadScene { get; set; }

        readonly Dictionary<string, (string name, string hero)> _hellos = new Dictionary<string, (string, string)>();
        readonly HashSet<string> _rejected = new HashSet<string>();
        float _lobbyT, _helloT, _rosterT;
        bool _initDone, _startNow;

        /// <summary>The host starts without waiting for anyone still joining (at least one guest must be in).</summary>
        public const float AutoStartAfter = 20f;

        /// <summary>Lobby: who's in (the host's list; guests get it from the host).</summary>
        public List<(string name, string heroId)> Roster { get; } = new List<(string, string)>();
        /// <summary>Host: at least one guest has said hello, so START NOW works.</summary>
        public bool CanStartNow => State == Phase.Lobby && IsHost && _hellos.Count > 0;

        /// <summary>Host: start with whoever has joined.</summary>
        public void StartNow() { if (CanStartNow) _startNow = true; }

        /// <summary>Game Center can run online play on this device.</summary>
        public static bool Supported
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        public static PKRMatchService Ensure()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("PKRMatch");
            DontDestroyOnLoad(go);
            return go.AddComponent<PKRMatchService>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        public static string LocalName
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR
                var b = new byte[128];
                int n = PKR_MP_LocalName(b, b.Length);
                if (n > 0) return Encoding.UTF8.GetString(b, 0, n);
#endif
                return "PLAYER";
            }
        }

        /// <summary>Opens Game Center to invite friends or find players for an online Last Stand.</summary>
        public void FindPlayers(SurvivalMode mode, bool fillWithAI)
        {
            Mode = mode;
            FillWithAI = fillWithAI;
#if UNITY_IOS && !UNITY_EDITOR
            PKR_MP_Init();
            _initDone = true;
            State = Phase.Finding;
            Status = "Waiting for Game Center...";
            PKR_MP_Find(2, SquadRules.MaxSquad, 7 + (int)mode);
#else
            State = Phase.Failed;
            Status = "Online co-op needs an iPhone signed in to Game Center.";
#endif
        }

        /// <summary>Everyone is connected: start talking (also used by tests with a loopback transport).</summary>
        public void BeginLobby(IZLTransport transport, SurvivalMode mode)
        {
            Transport = transport;
            Mode = mode;
            _hellos.Clear();
            _rejected.Clear();
            Roster.Clear();
            _lobbyT = 0f;
            _helloT = 0f;
            _rosterT = 0f;
            _startNow = false;
            var ids = new List<string>(transport.PeerIds) { transport.LocalId };
            IsHost = SquadRules.Host(ids) == transport.LocalId;
            State = Phase.Lobby;
            Status = IsHost ? "You're hosting. Waiting for everyone..." : "Joined. Waiting for the host...";
        }

        public void Leave()
        {
            if (Transport != null) { Transport.SendToAll(ZLNet.Bye(), true); Transport.Disconnect(); }
            Transport = null;
            State = Phase.Idle;
            Status = "";
            Slots.Clear();
        }

        void Update()
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (!_initDone && GameCenterService.SignedIn) { _initDone = true; PKR_MP_Init(); }
            int ns = PKR_MP_State();
            if (State == Phase.Finding || State == Phase.Idle)
            {
                if (ns == 2) BeginLobby(new GameCenterTransport(), State == Phase.Idle ? GameSession.ZombieMode : Mode);
                else if (ns == 3 && State == Phase.Finding)
                {
                    var b = new byte[256];
                    int n = PKR_MP_Error(b, b.Length);
                    State = Phase.Failed;
                    Status = n > 0 && Encoding.UTF8.GetString(b, 0, n) != "cancelled" ? "Couldn't connect: " + Encoding.UTF8.GetString(b, 0, n) : "";
                }
            }
#endif
            if (State == Phase.Lobby) Lobby(Time.unscaledDeltaTime);
        }

        void Lobby(float dt)
        {
            _lobbyT += dt;
            string me = Transport.LocalId;
            if (!IsHost)
            {
                _helloT -= dt;
                if (_helloT <= 0f) { _helloT = 1f; Transport.SendToAll(ZLNet.Hello(LocalName, GameSession.SelectedCharacterId), true); }
            }
            while (Transport.TryReceive(out var from, out var data))
            {
                var kind = ZLNet.Kind(data);
                if (kind == NetMsg.Hello && IsHost && ZLNet.ReadHello(data, out var name, out var hero, out int version))
                {
                    if (version != ZLNet.Version)
                    {
                        if (_rejected.Add(from)) Transport.SendTo(from, ZLNet.Reject(NetReject.Version), true);
                    }
                    else if (!_hellos.ContainsKey(from) && _hellos.Count + 1 >= SquadRules.MaxSquad)
                    {
                        if (_rejected.Add(from)) Transport.SendTo(from, ZLNet.Reject(NetReject.Full), true);
                    }
                    else _hellos[from] = (name, hero);
                }
                else if (kind == NetMsg.Reject && !IsHost && ZLNet.ReadReject(data, out var why, out int hv))
                {
                    Leave();
                    State = Phase.Failed;
                    Status = ZLNet.RejectText(why, hv);
                    return;
                }
                else if (kind == NetMsg.Roster && !IsHost && ZLNet.ReadRoster(data, out var roster)) { Roster.Clear(); Roster.AddRange(roster); }
                else if (kind == NetMsg.Ping && ZLNet.ReadStamp(data, out int stamp)) Transport.SendTo(from, ZLNet.Pong(stamp), false);
                else if (kind == NetMsg.Start && !IsHost && ZLNet.ReadStart(data, out var mode, out var slots, out var map)) { MapId = map; Begin(mode, slots); return; }
                else if (kind == NetMsg.Bye) _hellos.Remove(from);
            }
            while (true)
            {
                string gone = Transport.TakeLeft();
                if (gone == null) break;
                _hellos.Remove(gone);
            }
            if (!IsHost) { Status = LobbyText(false); return; }
            var peers = new List<string>();
            foreach (var p in Transport.PeerIds) if (!_rejected.Contains(p)) peers.Add(p);
            if (peers.Count == 0) { State = Phase.Failed; Status = _rejected.Count > 0 ? "The other player needs the same version of the game." : "Everyone left."; return; }
            Roster.Clear();
            Roster.Add((LocalName, GameSession.SelectedCharacterId));
            foreach (var id in SquadRules.Slots(peers)) if (_hellos.TryGetValue(id, out var h)) Roster.Add((h.name, h.hero));
            _rosterT -= dt;
            if (_rosterT <= 0f) { _rosterT = 1f; Transport.SendToAll(ZLNet.Roster(Roster), false); }
            Status = LobbyText(true);
            bool all = true;
            foreach (var p in peers) if (!_hellos.ContainsKey(p)) all = false;
            bool go = _startNow || (all && _lobbyT >= 0.5f) || (_hellos.Count > 0 && _lobbyT >= AutoStartAfter);
            if (!go) return;
            peers.RemoveAll(p => !_hellos.ContainsKey(p));

            // START: the host is slot 0, everyone else by id, then AI teammates in the empty seats.
            var slotsOut = new List<NetSlot>
            {
                new NetSlot { slot = 0, playerId = me, name = LocalName, heroId = GameSession.SelectedCharacterId }
            };
            var others = SquadRules.Slots(peers);
            foreach (var id in others)
            {
                if (slotsOut.Count >= SquadRules.MaxSquad) break;
                _hellos.TryGetValue(id, out var h);
                slotsOut.Add(new NetSlot { slot = slotsOut.Count, playerId = id, name = string.IsNullOrEmpty(h.name) ? "PLAYER " + (slotsOut.Count + 1) : h.name, heroId = h.hero ?? "" });
            }
            if (FillWithAI)
                while (slotsOut.Count < SquadRules.MaxSquad) slotsOut.Add(new NetSlot { slot = slotsOut.Count, playerId = "", name = "AI", heroId = "", ai = true });
            Transport.SendToAll(ZLNet.Start(Mode, slotsOut, MapId), true);
            Begin(Mode, slotsOut);
        }

        string LobbyText(bool host)
        {
            var sb = new StringBuilder(host ? "You're hosting." : "Joined. Waiting for the host to start.");
            if (Roster.Count > 0)
            {
                sb.Append("  In the squad: ");
                for (int i = 0; i < Roster.Count; i++) sb.Append(i > 0 ? ", " : "").Append(string.IsNullOrEmpty(Roster[i].name) ? "PLAYER" : Roster[i].name);
            }
            if (host && Roster.Count < (Transport != null ? Transport.PeerIds.Count + 1 : 1)) sb.Append("  (someone is still joining)");
            return sb.ToString();
        }

        void Begin(SurvivalMode mode, List<NetSlot> slots)
        {
            Mode = mode;
            Slots = slots;
            MySlot = 0;
            foreach (var s in slots) if (!s.ai && s.playerId == Transport.LocalId) MySlot = s.slot;
            State = Phase.InGame;
            Status = "";
            GameSession.Mode = SessionMode.Zombies;
            GameSession.ZombieMode = mode;
            GameSession.ZombieSquad = SquadKind.Online;
            GameSession.ZombieMap = LastStandMap.Get(MapId).Id;
            GameSession.ZombieWeekly = false;
            GameSession.EventScene = SceneIds.LastStand;
            if (!DontLoadScene && Services.Scenes != null) Services.Scenes.Load(SceneIds.LastStand);
        }

        /// <summary>The game ended or someone backed out: the match is over.</summary>
        public void EndMatch()
        {
            Leave();
            if (GameSession.ZombieSquad == SquadKind.Online) GameSession.ZombieSquad = SquadKind.Solo;
        }
    }
}
