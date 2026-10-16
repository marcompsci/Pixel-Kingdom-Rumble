using System;
using System.Collections.Generic;
using System.Text;

namespace PKR.Core
{
    /// <summary>Message kinds for online Last Stand (first byte of every packet).</summary>
    public enum NetMsg : byte { Hello = 1, Start = 2, Input = 3, Snapshot = 4, Toast = 5, Shot = 6, Over = 7, Bye = 8 }

    /// <summary>Little-endian packet writer (positions as hundredths of a tile in 16 bits).</summary>
    public class NetWriter
    {
        readonly List<byte> _b = new List<byte>(256);
        public int Length => _b.Count;
        public byte[] ToArray() => _b.ToArray();
        public void Byte(int v) => _b.Add((byte)v);
        public void SByte(int v) => _b.Add((byte)(sbyte)Math.Max(-127, Math.Min(127, v)));
        public void Bool(bool v) => _b.Add((byte)(v ? 1 : 0));
        public void UShort(int v) { v = Math.Max(0, Math.Min(65535, v)); _b.Add((byte)v); _b.Add((byte)(v >> 8)); }
        public void Short(int v) { v = Math.Max(-32768, Math.Min(32767, v)); _b.Add((byte)v); _b.Add((byte)(v >> 8)); }
        public void Int(int v) { _b.Add((byte)v); _b.Add((byte)(v >> 8)); _b.Add((byte)(v >> 16)); _b.Add((byte)(v >> 24)); }
        public void Pos(float v) => Short((int)Math.Round(v * 100f));
        public void Unit(float v) => SByte((int)Math.Round(Math.Max(-1f, Math.Min(1f, v)) * 127f));
        public void Str(string s)
        {
            var bytes = Encoding.UTF8.GetBytes(s ?? "");
            int n = Math.Min(255, bytes.Length);
            Byte(n);
            for (int i = 0; i < n; i++) _b.Add(bytes[i]);
        }
    }

    public class NetReader
    {
        readonly byte[] _d;
        int _i;
        public NetReader(byte[] data) { _d = data ?? new byte[0]; }
        public bool Ok { get; private set; } = true;
        public int Left => _d.Length - _i;
        byte Next() { if (_i >= _d.Length) { Ok = false; return 0; } return _d[_i++]; }
        public int Byte() => Next();
        public int SByte() => (sbyte)Next();
        public bool Bool() => Next() != 0;
        public int UShort() { int a = Next(); int b = Next(); return a | (b << 8); }
        public int Short() { int a = Next(); int b = Next(); return (short)(a | (b << 8)); }
        public int Int() { int a = Next(), b = Next(), c = Next(), d = Next(); return a | (b << 8) | (c << 16) | (d << 24); }
        public float Pos() => Short() / 100f;
        public float Unit() => SByte() / 127f;
        public string Str()
        {
            int n = Byte();
            if (_i + n > _d.Length) { Ok = false; return ""; }
            var s = Encoding.UTF8.GetString(_d, _i, n);
            _i += n;
            return s;
        }
    }

    /// <summary>One player in the Start message.</summary>
    public struct NetSlot
    {
        public int slot;
        public string playerId, name, heroId;
        public bool ai;
    }

    /// <summary>A client's controls this tick. Presses are counters so a lost packet never loses a press.</summary>
    public struct NetInput
    {
        public int seq;
        public float moveX, moveY, aimX, aimY, posX, posY;
        public bool fire, useHeld;
        public int useCount, dodgeCount, meleeCount, swapCount;
    }

    public struct NetMember
    {
        public int slot;
        public float x, y, aimX, aimY;
        public int hp, maxHp, points, kills, mag, reserve, gunIndex, perkMask;
        public bool downed, out_, forged;
    }

    public struct NetZombie
    {
        public int id, kind, hp01;
        public float x, y;
    }

    public struct NetPowerUp
    {
        public int id, kind;
        public float x, y;
    }

    /// <summary>The whole visible state of the block, sent by the host about ten times a second.</summary>
    public class NetSnapshot
    {
        public int tick, round, zombiesLeft, intermissionLeft, doorMask, boxSpot;
        public bool intermission, houndRound, over, won, boxBusy, instaKill, doublePoints;
        public List<NetMember> members = new List<NetMember>();
        public List<NetZombie> zombies = new List<NetZombie>();
        public List<NetPowerUp> powerUps = new List<NetPowerUp>();
    }

    /// <summary>Encoding and decoding of every message (pure, unit-tested).</summary>
    public static class ZLNet
    {
        /// <summary>2: the Start message carries the map.</summary>
        public const int Version = 2;

        /// <summary>Guns by index (the catalog order) so snapshots carry one byte per gun.</summary>
        public static int GunIndex(string id)
        {
            for (int i = 0; i < ZLCatalog.Guns.Length; i++) if (ZLCatalog.Guns[i].id == id) return i;
            return 0;
        }

        public static string GunId(int index) => index >= 0 && index < ZLCatalog.Guns.Length ? ZLCatalog.Guns[index].id : ZLCatalog.Pistol.id;

        public static int PerkMask(IEnumerable<string> perks)
        {
            int m = 0;
            foreach (var p in perks)
                if (Enum.TryParse(p, out ZLPerk k)) m |= 1 << (int)k;
            return m;
        }

        public static List<string> PerksFromMask(int mask)
        {
            var l = new List<string>();
            foreach (ZLPerk k in Enum.GetValues(typeof(ZLPerk))) if ((mask & (1 << (int)k)) != 0) l.Add(k.ToString());
            return l;
        }

        public static NetMsg Kind(byte[] data) => data != null && data.Length > 0 ? (NetMsg)data[0] : 0;

        // ---- hello / start / bye / over ----------------------------------------------------------------

        public static byte[] Hello(string name, string heroId)
        {
            var w = new NetWriter();
            w.Byte((int)NetMsg.Hello); w.Byte(Version); w.Str(name); w.Str(heroId);
            return w.ToArray();
        }

        public static bool ReadHello(byte[] d, out string name, out string heroId)
        {
            var r = new NetReader(d); r.Byte();
            int v = r.Byte(); name = r.Str(); heroId = r.Str();
            return r.Ok && v == Version;
        }

        public static byte[] Start(SurvivalMode mode, IList<NetSlot> slots) => Start(mode, slots, LastStandRules.MapId);

        public static byte[] Start(SurvivalMode mode, IList<NetSlot> slots, string mapId)
        {
            var w = new NetWriter();
            w.Byte((int)NetMsg.Start); w.Byte(Version); w.Byte((int)mode); w.Byte(slots.Count);
            foreach (var s in slots) { w.Byte(s.slot); w.Str(s.playerId); w.Str(s.name); w.Str(s.heroId); w.Bool(s.ai); }
            w.Str(mapId ?? "");
            return w.ToArray();
        }

        public static bool ReadStart(byte[] d, out SurvivalMode mode, out List<NetSlot> slots) => ReadStart(d, out mode, out slots, out _);

        public static bool ReadStart(byte[] d, out SurvivalMode mode, out List<NetSlot> slots, out string mapId)
        {
            var r = new NetReader(d); r.Byte();
            int v = r.Byte();
            mode = (SurvivalMode)r.Byte();
            int n = r.Byte();
            slots = new List<NetSlot>();
            for (int i = 0; i < n && r.Ok; i++)
                slots.Add(new NetSlot { slot = r.Byte(), playerId = r.Str(), name = r.Str(), heroId = r.Str(), ai = r.Bool() });
            mapId = r.Str();
            if (!r.Ok) mapId = LastStandRules.MapId;
            return r.Ok && v == Version;
        }

        public static byte[] Toast(string text, int sfx)
        {
            var w = new NetWriter();
            w.Byte((int)NetMsg.Toast); w.Str(text); w.Byte(sfx);
            return w.ToArray();
        }

        public static bool ReadToast(byte[] d, out string text, out int sfx)
        {
            var r = new NetReader(d); r.Byte();
            text = r.Str(); sfx = r.Byte();
            return r.Ok;
        }

        public static byte[] Over(bool won, int round, int kills)
        {
            var w = new NetWriter();
            w.Byte((int)NetMsg.Over); w.Bool(won); w.UShort(round); w.UShort(kills);
            return w.ToArray();
        }

        public static bool ReadOver(byte[] d, out bool won, out int round, out int kills)
        {
            var r = new NetReader(d); r.Byte();
            won = r.Bool(); round = r.UShort(); kills = r.UShort();
            return r.Ok;
        }

        public static byte[] Bye() => new[] { (byte)NetMsg.Bye };

        // ---- input -------------------------------------------------------------------------------------

        public static byte[] Input(NetInput i)
        {
            var w = new NetWriter();
            w.Byte((int)NetMsg.Input); w.UShort(i.seq & 0xffff);
            w.Unit(i.moveX); w.Unit(i.moveY); w.Unit(i.aimX); w.Unit(i.aimY);
            w.Pos(i.posX); w.Pos(i.posY);
            w.Byte((i.fire ? 1 : 0) | (i.useHeld ? 2 : 0));
            w.Byte(i.useCount); w.Byte(i.dodgeCount); w.Byte(i.meleeCount); w.Byte(i.swapCount);
            return w.ToArray();
        }

        public static bool ReadInput(byte[] d, out NetInput i)
        {
            var r = new NetReader(d); r.Byte();
            i = new NetInput { seq = r.UShort(), moveX = r.Unit(), moveY = r.Unit(), aimX = r.Unit(), aimY = r.Unit(), posX = r.Pos(), posY = r.Pos() };
            int b = r.Byte();
            i.fire = (b & 1) != 0; i.useHeld = (b & 2) != 0;
            i.useCount = r.Byte(); i.dodgeCount = r.Byte(); i.meleeCount = r.Byte(); i.swapCount = r.Byte();
            return r.Ok;
        }

        /// <summary>How many presses happened between two 8-bit counters (wraps at 256).</summary>
        public static int Presses(int before, int now) => (now - before + 256) % 256;

        // ---- shots (so other players see the bullets) -------------------------------------------------

        public static byte[] Shot(int slot, float x, float y, float dirX, float dirY, int gunIndex)
        {
            var w = new NetWriter();
            w.Byte((int)NetMsg.Shot); w.Byte(slot); w.Pos(x); w.Pos(y); w.Unit(dirX); w.Unit(dirY); w.Byte(gunIndex);
            return w.ToArray();
        }

        public static bool ReadShot(byte[] d, out int slot, out float x, out float y, out float dx, out float dy, out int gunIndex)
        {
            var r = new NetReader(d); r.Byte();
            slot = r.Byte(); x = r.Pos(); y = r.Pos(); dx = r.Unit(); dy = r.Unit(); gunIndex = r.Byte();
            return r.Ok;
        }

        // ---- snapshot ----------------------------------------------------------------------------------

        public static byte[] Snapshot(NetSnapshot s)
        {
            var w = new NetWriter();
            w.Byte((int)NetMsg.Snapshot);
            w.Int(s.tick); w.UShort(s.round); w.UShort(s.zombiesLeft); w.Byte(s.intermissionLeft);
            w.Byte((s.intermission ? 1 : 0) | (s.houndRound ? 2 : 0) | (s.over ? 4 : 0) | (s.won ? 8 : 0) | (s.boxBusy ? 16 : 0) |
                   (s.instaKill ? 32 : 0) | (s.doublePoints ? 64 : 0));
            w.UShort(s.doorMask); w.Byte(s.boxSpot);
            w.Byte(s.members.Count);
            foreach (var m in s.members)
            {
                w.Byte(m.slot); w.Pos(m.x); w.Pos(m.y); w.Unit(m.aimX); w.Unit(m.aimY);
                w.UShort(m.hp); w.UShort(m.maxHp); w.Int(m.points); w.UShort(m.kills);
                w.UShort(m.mag); w.UShort(m.reserve); w.Byte(m.gunIndex); w.Byte(m.perkMask);
                w.Byte((m.downed ? 1 : 0) | (m.out_ ? 2 : 0) | (m.forged ? 4 : 0));
            }
            w.Byte(Math.Min(255, s.zombies.Count));
            for (int i = 0; i < s.zombies.Count && i < 255; i++)
            {
                var z = s.zombies[i];
                w.UShort(z.id); w.Byte(z.kind); w.Pos(z.x); w.Pos(z.y); w.Byte(z.hp01);
            }
            w.Byte(s.powerUps.Count);
            foreach (var p in s.powerUps) { w.UShort(p.id); w.Byte(p.kind); w.Pos(p.x); w.Pos(p.y); }
            return w.ToArray();
        }

        public static NetSnapshot ReadSnapshot(byte[] d)
        {
            var r = new NetReader(d); r.Byte();
            var s = new NetSnapshot { tick = r.Int(), round = r.UShort(), zombiesLeft = r.UShort(), intermissionLeft = r.Byte() };
            int f = r.Byte();
            s.intermission = (f & 1) != 0; s.houndRound = (f & 2) != 0; s.over = (f & 4) != 0; s.won = (f & 8) != 0;
            s.boxBusy = (f & 16) != 0; s.instaKill = (f & 32) != 0; s.doublePoints = (f & 64) != 0;
            s.doorMask = r.UShort(); s.boxSpot = r.Byte();
            int n = r.Byte();
            for (int i = 0; i < n && r.Ok; i++)
            {
                var m = new NetMember
                {
                    slot = r.Byte(), x = r.Pos(), y = r.Pos(), aimX = r.Unit(), aimY = r.Unit(), hp = r.UShort(), maxHp = r.UShort(),
                    points = r.Int(), kills = r.UShort(), mag = r.UShort(), reserve = r.UShort(), gunIndex = r.Byte(), perkMask = r.Byte()
                };
                int mf = r.Byte();
                m.downed = (mf & 1) != 0; m.out_ = (mf & 2) != 0; m.forged = (mf & 4) != 0;
                s.members.Add(m);
            }
            int zc = r.Byte();
            for (int i = 0; i < zc && r.Ok; i++)
                s.zombies.Add(new NetZombie { id = r.UShort(), kind = r.Byte(), x = r.Pos(), y = r.Pos(), hp01 = r.Byte() });
            int pc = r.Byte();
            for (int i = 0; i < pc && r.Ok; i++)
                s.powerUps.Add(new NetPowerUp { id = r.UShort(), kind = r.Byte(), x = r.Pos(), y = r.Pos() });
            return r.Ok ? s : null;
        }
    }
}
