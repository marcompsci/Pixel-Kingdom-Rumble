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
        public bool FillWithAI { get; set; } = true;
        /// <summary>Tests: don't load the scene when the game starts.</summary>
        public bool DontLoadScene { get; set; }

        readonly Dictionary<string, (string name, string hero)> _hellos = new Dictionary<string, (string, string)>();
        float _lobbyT, _helloT;
        bool _initDone;

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
            _lobbyT = 0f;
            _helloT = 0f;
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
                if (kind == NetMsg.Hello && IsHost && ZLNet.ReadHello(data, out var name, out var hero)) _hellos[from] = (name, hero);
                else if (kind == NetMsg.Start && !IsHost && ZLNet.ReadStart(data, out var mode, out var slots)) { Begin(mode, slots); return; }
                else if (kind == NetMsg.Bye) Status = "A player left.";
            }
            while (Transport.TakeLeft() != null) Status = "A player left.";
            if (!IsHost) return;
            var peers = Transport.PeerIds;
            bool all = true;
            foreach (var p in peers) if (!_hellos.ContainsKey(p)) all = false;
            if (peers.Count == 0) { State = Phase.Failed; Status = "Everyone left."; return; }
            if (!all && _lobbyT < 8f) return;

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
            Transport.SendToAll(ZLNet.Start(Mode, slotsOut), true);
            Begin(Mode, slotsOut);
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
