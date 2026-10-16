using System;
using System.Collections.Generic;

namespace PKR.Core
{
    /// <summary>The Halloween Zombie Survival cast.</summary>
    public enum ZombieKind { Shambler, Runner, Bloater, Spitter, Brute, Bat, PumpkinKing }

    /// <summary>Power-ups a zombie can drop.</summary>
    public enum PowerUpKind { AmmoPinata, SpookyStrike, TwinTreats, Moonburst, BoardUp }

    /// <summary>Perk shrines (each bought once per run; at most ZombieRules.PerkLimit at a time).</summary>
    public enum PerkKind { IronHeart, QuickDraw, SecondWind, FleetFeet, Deadeye, Scavenger }

    /// <summary>
    /// How a Survival run is played. Endless: rounds forever. Raid: ten rounds and the boss on the tenth (a short
    /// session that can be won). Hardcore: tougher, faster dead, fewer drops, more points, and the lantern secret
    /// summons a hidden boss.
    /// </summary>
    public enum SurvivalMode { Endless, Raid, Hardcore }

    /// <summary>
    /// Zombie Survival numbers (pure, so they can be unit-tested): how many zombies a round sends and how fast,
    /// how tough each kind is, which kinds appear in which rounds, points, drops and prices.
    /// Rounds get harder forever; every 5th round the Pumpkin King leads the horde.
    /// </summary>
    public static class ZombieRules
    {
        // ---- rounds -------------------------------------------------------------------------------------------

        /// <summary>Zombies in a round: 6, 8, 11, 14, 19 ... capped at 90.</summary>
        public static int ZombiesInRound(int round)
        {
            int r = Math.Max(1, round);
            return Math.Min(90, 4 + 2 * r + r * r / 5);
        }

        /// <summary>How many may be on screen at once (keeps phones smooth): 6 in round 1, up to 16.</summary>
        public static int MaxAlive(int round) => Math.Min(16, 5 + Math.Max(1, round));

        /// <summary>Seconds between spawns: 2.2 s in round 1 down to 0.45 s.</summary>
        public static float SpawnInterval(int round) => Math.Max(0.45f, 2.2f - 0.12f * (Math.Max(1, round) - 1));

        /// <summary>Breather between rounds (seconds).</summary>
        public const float Intermission = 7f;

        public static bool IsBossRound(int round) => round > 0 && round % 5 == 0;

        /// <summary>Roof drops (the spawns without a barricade) join from this round on.</summary>
        public const int RoofSpawnsFromRound = 4;

        // ---- the cast -----------------------------------------------------------------------------------------

        public static int BaseHealth(ZombieKind k)
        {
            switch (k)
            {
                case ZombieKind.Runner: return 2;
                case ZombieKind.Bloater: return 4;
                case ZombieKind.Spitter: return 3;
                case ZombieKind.Brute: return 9;
                case ZombieKind.Bat: return 1;
                case ZombieKind.PumpkinKing: return 40;
                default: return 2;
            }
        }

        /// <summary>Health grows 18% per round (the Pumpkin King gains 12 a round on top).</summary>
        public static int Health(ZombieKind k, int round)
        {
            int r = Math.Max(1, round);
            int hp = (int)Math.Round(BaseHealth(k) * (1f + 0.18f * (r - 1)));
            if (k == ZombieKind.PumpkinKing) hp += 12 * r;
            return Math.Max(1, hp);
        }

        /// <summary>Walk speed as a fraction of the hero's run (the enemy motor's moveSpeed).</summary>
        public static float Speed(ZombieKind k, int round)
        {
            float r = Math.Max(1, round) - 1;
            switch (k)
            {
                case ZombieKind.Runner: return Math.Min(0.86f, 0.66f + 0.02f * r);
                case ZombieKind.Bloater: return Math.Min(0.4f, 0.24f + 0.01f * r);
                case ZombieKind.Spitter: return Math.Min(0.46f, 0.3f + 0.012f * r);
                case ZombieKind.Brute: return Math.Min(0.5f, 0.32f + 0.012f * r);
                case ZombieKind.PumpkinKing: return 0.42f;
                case ZombieKind.Bat: return 0f; // flies
                default: return Math.Min(0.56f, 0.3f + 0.018f * r);
            }
        }

        /// <summary>Picks the kind of the next zombie (roll in [0, 1)). New kinds join as the rounds climb.</summary>
        public static ZombieKind Pick(int round, double roll)
        {
            int r = Math.Max(1, round);
            if (r <= 2) return ZombieKind.Shambler;
            double brute = r >= 8 ? Math.Min(0.1, 0.03 + 0.005 * (r - 8)) : 0;
            double spitter = r >= 6 ? Math.Min(0.14, 0.06 + 0.01 * (r - 6)) : 0;
            double bloater = r >= 4 ? 0.09 : 0;
            double bat = r >= 4 ? Math.Min(0.14, 0.06 + 0.01 * (r - 4)) : 0;
            double runner = Math.Min(0.4, 0.12 + 0.03 * (r - 3));
            double t = roll;
            if ((t -= brute) < 0) return ZombieKind.Brute;
            if ((t -= spitter) < 0) return ZombieKind.Spitter;
            if ((t -= bloater) < 0) return ZombieKind.Bloater;
            if ((t -= bat) < 0) return ZombieKind.Bat;
            if ((t -= runner) < 0) return ZombieKind.Runner;
            return ZombieKind.Shambler;
        }

        // ---- points -------------------------------------------------------------------------------------------

        public const int HitPoints = 10;
        public const int MeleeKillBonus = 70;
        public const int RepairPoints = 10;
        public const int StartingPoints = 500;

        public static int KillPoints(ZombieKind k)
        {
            switch (k)
            {
                case ZombieKind.Runner: return 70;
                case ZombieKind.Bloater:
                case ZombieKind.Spitter: return 90;
                case ZombieKind.Brute: return 150;
                case ZombieKind.Bat: return 50;
                case ZombieKind.PumpkinKing: return 1000;
                default: return 60;
            }
        }

        /// <summary>Points for finishing a round (rewards surviving, not just killing).</summary>
        public static int RoundBonus(int round) => 25 * Math.Max(1, round);

        // ---- drops --------------------------------------------------------------------------------------------

        public const double DropChance = 0.04;
        public const int MaxDropsPerRound = 4;
        public const float PowerUpSeconds = 20f;
        public const float DropLifetime = 15f;
        public const int MoonburstPoints = 400;
        public const int BoardUpPoints = 200;

        /// <summary>No drop for this many kills in a row makes the next kill drop one (players hated dry spells).</summary>
        public const int DropPity = 30;

        /// <summary>True if a kill drops a power-up (roll in [0, 1)); the boss always drops one.</summary>
        public static bool Drops(ZombieKind k, int dropsThisRound, double roll) =>
            k == ZombieKind.PumpkinKing || (dropsThisRound < MaxDropsPerRound && roll < DropChance);

        /// <summary>
        /// The full drop check: the boss always drops; otherwise a chance per kill (lower in Hardcore, higher with
        /// Scavenger), capped per round, and guaranteed after DropPity kills without one.
        /// </summary>
        public static bool Drops(ZombieKind k, int dropsThisRound, int killsSinceDrop, double roll, SurvivalMode mode, bool scavenger)
        {
            if (k == ZombieKind.PumpkinKing) return true;
            int cap = MaxDropsPerRound + (scavenger ? 2 : 0);
            if (dropsThisRound >= cap) return false;
            if (killsSinceDrop >= DropPity) return true;
            double chance = DropChance * (mode == SurvivalMode.Hardcore ? 0.75 : 1.0) * (scavenger ? 1.6 : 1.0);
            return roll < chance;
        }

        public static PowerUpKind PickPowerUp(double roll)
        {
            if (roll < 0.28) return PowerUpKind.AmmoPinata;
            if (roll < 0.48) return PowerUpKind.TwinTreats;
            if (roll < 0.68) return PowerUpKind.SpookyStrike;
            if (roll < 0.84) return PowerUpKind.BoardUp;
            return PowerUpKind.Moonburst;
        }

        public static string PowerUpName(PowerUpKind k)
        {
            switch (k)
            {
                case PowerUpKind.AmmoPinata: return "AMMO PINATA";
                case PowerUpKind.SpookyStrike: return "SPOOKY STRIKE";
                case PowerUpKind.TwinTreats: return "TWIN TREATS";
                case PowerUpKind.Moonburst: return "MOONBURST";
                default: return "BOARD UP";
            }
        }

        // ---- shop ---------------------------------------------------------------------------------------------

        public const int CrateCost = 950;
        public const int ForgeCost = 5000;
        public const int DoorCost = 750;
        /// <summary>Boards on a full window barricade.</summary>
        public const int BarricadeBoards = 6;

        public static int PerkCost(PerkKind p)
        {
            switch (p)
            {
                case PerkKind.IronHeart: return 2500;
                case PerkKind.QuickDraw: return 3000;
                case PerkKind.SecondWind: return 1500;
                case PerkKind.Deadeye: return 3500;
                case PerkKind.Scavenger: return 2000;
                default: return 2000;
            }
        }

        /// <summary>Perks you can hold at once; lighting every lantern opens a fifth slot.</summary>
        public static int PerkLimit(bool lanternsLit) => lanternsLit ? 5 : 4;

        public static string PerkName(PerkKind p)
        {
            switch (p)
            {
                case PerkKind.IronHeart: return "IRON HEART";
                case PerkKind.QuickDraw: return "QUICK DRAW";
                case PerkKind.SecondWind: return "SECOND WIND";
                case PerkKind.Deadeye: return "DEADEYE";
                case PerkKind.Scavenger: return "SCAVENGER";
                default: return "FLEET FEET";
            }
        }

        public static string PerkInfo(PerkKind p)
        {
            switch (p)
            {
                case PerkKind.IronHeart: return "+3 max health";
                case PerkKind.QuickDraw: return "Faster fire and reloads";
                case PerkKind.SecondWind: return "Get back up once";
                case PerkKind.Deadeye: return "+1 damage on every shot";
                case PerkKind.Scavenger: return "More power-ups drop";
                default: return "Run 20% faster";
            }
        }

        public const int IronHeartBonusHealth = 3;
        public const float QuickDrawRate = 1.4f;
        public const float FleetFeetSpeed = 1.2f;
        public const int DeadeyeBonus = 1;

        /// <summary>Secret: light every jack-o'-lantern in the arena.</summary>
        public const int LanternReward = 2500;
    }

    /// <summary>One gun (or its forged upgrade). Damage is in enemy health points per projectile.</summary>
    public struct ZombieWeapon
    {
        public string id, name;
        public int damage;
        public float shotsPerSecond;
        public int magazine, reserve;
        public float reloadSeconds;
        public float speed, radius, lifetime;
        public int pellets;
        public float spreadDegrees;
        /// <summary>Hold FIRE to keep shooting.</summary>
        public bool automatic;
        /// <summary>Wall price (buying ammo for it again costs half). 0 = not on a wall.</summary>
        public int cost;
        /// <summary>Projectile colour (RGB) and FX art name (Art/Pixel/FX/shot_{shot}.png).</summary>
        public byte r, g, b;
        public string shot;
        public bool upgraded;

        public int AmmoCost => cost / 2;
    }

    /// <summary>The armory: wall weapons (bought at chalk outlines), mystery-crate weapons and the forge upgrade.</summary>
    public static class ZombieArmory
    {
        public static readonly ZombieWeapon PeaPopper = new ZombieWeapon
        {
            id = "pea_popper", name = "PEA POPPER", damage = 1, shotsPerSecond = 3.5f, magazine = 8, reserve = 64, reloadSeconds = 1.1f,
            speed = 16f, radius = 0.16f, lifetime = 0.9f, pellets = 1, automatic = false, cost = 500, r = 150, g = 230, b = 110, shot = "ball"
        };

        public static readonly ZombieWeapon CandyRepeater = new ZombieWeapon
        {
            id = "candy_repeater", name = "CANDY CORN REPEATER", damage = 1, shotsPerSecond = 9f, magazine = 30, reserve = 150,
            reloadSeconds = 1.6f, speed = 18f, radius = 0.14f, lifetime = 0.8f, pellets = 1, automatic = true, cost = 1000,
            r = 255, g = 180, b = 60, shot = "shard"
        };

        public static readonly ZombieWeapon HexScattergun = new ZombieWeapon
        {
            id = "hex_scattergun", name = "HEX SCATTERGUN", damage = 2, shotsPerSecond = 1.4f, magazine = 6, reserve = 36,
            reloadSeconds = 1.8f, speed = 14f, radius = 0.18f, lifetime = 0.35f, pellets = 5, spreadDegrees = 22f, cost = 1500,
            r = 190, g = 120, b = 255, shot = "spark"
        };

        public static readonly ZombieWeapon EctoSprayer = new ZombieWeapon
        {
            id = "ecto_sprayer", name = "ECTO SPRAYER", damage = 1, shotsPerSecond = 12f, magazine = 40, reserve = 200,
            reloadSeconds = 1.4f, speed = 15f, radius = 0.15f, lifetime = 0.5f, pellets = 1, spreadDegrees = 6f, automatic = true,
            cost = 1300, r = 120, g = 255, b = 170, shot = "blob"
        };

        public static readonly ZombieWeapon PumpkinLauncher = new ZombieWeapon
        {
            id = "pumpkin_launcher", name = "PUMPKIN LAUNCHER", damage = 8, shotsPerSecond = 1f, magazine = 4, reserve = 20,
            reloadSeconds = 2.2f, speed = 11f, radius = 0.42f, lifetime = 1.4f, pellets = 1, r = 255, g = 140, b = 40, shot = "comet"
        };

        public static readonly ZombieWeapon BoneRifle = new ZombieWeapon
        {
            id = "bone_rifle", name = "BONE RIFLE", damage = 6, shotsPerSecond = 1.6f, magazine = 6, reserve = 36,
            reloadSeconds = 1.9f, speed = 28f, radius = 0.15f, lifetime = 0.8f, pellets = 1, r = 240, g = 240, b = 220, shot = "lance"
        };

        public static readonly ZombieWeapon MoonbeamRay = new ZombieWeapon
        {
            id = "moonbeam_ray", name = "MOONBEAM RAY", damage = 4, shotsPerSecond = 6f, magazine = 24, reserve = 144,
            reloadSeconds = 1.5f, speed = 22f, radius = 0.22f, lifetime = 0.9f, pellets = 1, automatic = true,
            r = 180, g = 220, b = 255, shot = "crescent"
        };

        /// <summary>Wall weapons in the order the arena's x outlines use them (cycling).</summary>
        public static readonly ZombieWeapon[] Wall = { PeaPopper, CandyRepeater, HexScattergun, EctoSprayer };

        /// <summary>What the mystery crate can roll: the three crate-only wonders are rarer than wall guns.</summary>
        public static readonly ZombieWeapon[] Crate = { PumpkinLauncher, BoneRifle, MoonbeamRay, CandyRepeater, HexScattergun, EctoSprayer };
        public static readonly float[] CrateWeights = { 1f, 1.2f, 0.8f, 1.6f, 1.6f, 1.6f };

        /// <summary>The crate-only wonders.</summary>
        public static bool IsWonder(string id) => id == PumpkinLauncher.id || id == BoneRifle.id || id == MoonbeamRay.id;

        /// <summary>Spins without a wonder before the next spin is guaranteed one (bad-luck protection).</summary>
        public const int CratePity = 4;

        /// <summary>Crate roll with bad-luck protection: after CratePity spins without a wonder, a wonder is certain.</summary>
        public static ZombieWeapon RollCrate(double roll, string holdingId, int spinsSinceWonder)
        {
            if (spinsSinceWonder < CratePity) return RollCrate(roll, holdingId);
            var wonders = new List<ZombieWeapon>();
            foreach (var w in Crate) if (IsWonder(w.id) && w.id != holdingId) wonders.Add(w);
            if (wonders.Count == 0) return RollCrate(roll, holdingId);
            int i = Math.Min(wonders.Count - 1, (int)(roll * wonders.Count));
            return wonders[i];
        }

        /// <summary>Weighted crate roll (roll in [0, 1)), never the weapon the hero is holding.</summary>
        public static ZombieWeapon RollCrate(double roll, string holdingId)
        {
            float total = 0f;
            for (int i = 0; i < Crate.Length; i++) if (Crate[i].id != holdingId) total += CrateWeights[i];
            double t = roll * total;
            for (int i = 0; i < Crate.Length; i++)
            {
                if (Crate[i].id == holdingId) continue;
                if ((t -= CrateWeights[i]) < 0) return Crate[i];
            }
            for (int i = Crate.Length - 1; i >= 0; i--) if (Crate[i].id != holdingId) return Crate[i];
            return Crate[0];
        }

        /// <summary>The forge: double damage, a bigger magazine and more reserve ammo. Upgrading twice does nothing.</summary>
        public static ZombieWeapon Upgrade(ZombieWeapon w)
        {
            if (w.upgraded) return w;
            w.upgraded = true;
            w.name = "CURSED " + w.name;
            w.damage *= 2;
            w.magazine = (int)Math.Round(w.magazine * 1.5f);
            w.reserve *= 2;
            w.speed *= 1.1f;
            return w;
        }

        public static IEnumerable<ZombieWeapon> All()
        {
            yield return PeaPopper; yield return CandyRepeater; yield return HexScattergun; yield return EctoSprayer;
            yield return PumpkinLauncher; yield return BoneRifle; yield return MoonbeamRay;
        }
    }

    /// <summary>The state of one Zombie Survival run: round, points (with Twin Treats doubling), kills, perks.</summary>
    public class SurvivalRun
    {
        public SurvivalMode Mode { get; }
        public int Round { get; private set; }
        public int Points { get; private set; } = ZombieRules.StartingPoints;
        /// <summary>Everything earned this run (the score; spending doesn't lower it).</summary>
        public int TotalEarned { get; private set; }
        public int Kills { get; private set; }
        public int DropsThisRound { get; private set; }
        /// <summary>Kills since the last power-up dropped (the drop pity timer).</summary>
        public int KillsSinceDrop { get; private set; }
        /// <summary>Mystery crate spins since the last wonder weapon (bad-luck protection).</summary>
        public int SpinsSinceWonder { get; private set; }
        public bool DoublePoints { get; set; }
        /// <summary>Every lantern lit this run: a fifth perk slot.</summary>
        public bool ExtraPerkSlot { get; set; }
        /// <summary>Raid won (the final boss fell).</summary>
        public bool Won { get; set; }
        readonly HashSet<PerkKind> _perks = new HashSet<PerkKind>();
        public IEnumerable<PerkKind> Perks => _perks;
        public int PerkCount => _perks.Count;
        public int PerkLimit => ZombieRules.PerkLimit(ExtraPerkSlot);

        public SurvivalRun() : this(SurvivalMode.Endless, 0) { }

        public SurvivalRun(SurvivalMode mode, int bonusStartPoints)
        {
            Mode = mode;
            Points = ZombieRules.StartingPoints + Math.Max(0, bonusStartPoints);
        }

        /// <summary>Online guest: the host's numbers for this run (the guest doesn't simulate the round).</summary>
        public void Mirror(int round, int points, int kills)
        {
            Round = Math.Max(0, round);
            if (points > Points) TotalEarned += points - Points;
            Points = Math.Max(0, points);
            Kills = Math.Max(0, kills);
        }

        public int NextRound()
        {
            Round++;
            DropsThisRound = 0;
            return Round;
        }

        public int Earn(int amount)
        {
            if (amount <= 0) return 0;
            int got = (int)Math.Round(amount * SurvivalModes.PointScale(Mode));
            if (DoublePoints) got *= 2;
            Points += got;
            TotalEarned += got;
            return got;
        }

        public bool CanAfford(int cost) => cost <= Points;

        public bool Spend(int cost)
        {
            if (cost < 0 || cost > Points) return false;
            Points -= cost;
            return true;
        }

        public int Kill(ZombieKind kind, bool melee) => KillFor(ZombieRules.KillPoints(kind) + (melee ? ZombieRules.MeleeKillBonus : 0));

        /// <summary>Counts a kill worth this many points (Last Stand uses its own table).</summary>
        public int KillFor(int points)
        {
            Kills++;
            KillsSinceDrop++;
            return Earn(points);
        }

        public void CountDrop()
        {
            DropsThisRound++;
            KillsSinceDrop = 0;
        }

        public void CountSpin(bool wonder) => SpinsSinceWonder = wonder ? 0 : SpinsSinceWonder + 1;

        public bool HasPerk(PerkKind p) => _perks.Contains(p);

        public bool PerkSlotFree => _perks.Count < PerkLimit;

        /// <summary>Buys a perk once (false if owned, no slot is free, or it's too expensive).</summary>
        public bool BuyPerk(PerkKind p) => BuyPerk(p, ZombieRules.PerkCost(p));

        public bool BuyPerk(PerkKind p, int cost)
        {
            if (_perks.Contains(p) || !PerkSlotFree || !Spend(cost)) return false;
            _perks.Add(p);
            return true;
        }

        /// <summary>A perk given for free (a Survivor Rank kit); still needs a free slot.</summary>
        public bool GrantPerk(PerkKind p)
        {
            if (_perks.Contains(p) || !PerkSlotFree) return false;
            _perks.Add(p);
            return true;
        }

        /// <summary>Second Wind is used up when it saves you.</summary>
        public bool UseSecondWind() => _perks.Remove(PerkKind.SecondWind);
    }

    /// <summary>Survival mode numbers (Endless / Raid / Hardcore) and the run's rewards.</summary>
    public static class SurvivalModes
    {
        public const int RaidRounds = 10;

        public static string Name(SurvivalMode m) => m == SurvivalMode.Raid ? "RAID" : m == SurvivalMode.Hardcore ? "HARDCORE" : "ENDLESS";

        public static string Blurb(SurvivalMode m)
        {
            switch (m)
            {
                case SurvivalMode.Raid: return "10 rounds, then the boss. A quick run you can win.";
                case SurvivalMode.Hardcore: return "Tougher dead, fewer drops, more points. The secret wakes a hidden boss.";
                default: return "Rounds forever. How far can you get?";
            }
        }

        /// <summary>Boss rounds: every 5th; in a Raid the 5th and the final 10th.</summary>
        public static bool IsBossRound(SurvivalMode m, int round) =>
            m == SurvivalMode.Raid ? round == 5 || round == RaidRounds : ZombieRules.IsBossRound(round);

        public static bool IsFinalRound(SurvivalMode m, int round) => m == SurvivalMode.Raid && round >= RaidRounds;

        public static float HealthScale(SurvivalMode m) => m == SurvivalMode.Hardcore ? 1.4f : 1f;
        public static float SpeedScale(SurvivalMode m) => m == SurvivalMode.Hardcore ? 1.1f : 1f;
        /// <summary>Multiplies the time between spawns.</summary>
        public static float SpawnScale(SurvivalMode m) => m == SurvivalMode.Hardcore ? 0.85f : 1f;
        public static float PointScale(SurvivalMode m) => m == SurvivalMode.Hardcore ? 1.25f : 1f;
        public static float Intermission(SurvivalMode m) => m == SurvivalMode.Raid ? 5f : ZombieRules.Intermission;

        /// <summary>The final Raid boss is tougher than a normal boss round.</summary>
        public static int BossHealth(SurvivalMode m, int round, int normal) =>
            m == SurvivalMode.Raid && round >= RaidRounds ? (int)Math.Round(normal * 1.8f) : (int)Math.Round(normal * HealthScale(m));

        /// <summary>A supply cache every 3 rounds survived (CODM paid a reward every 3 rounds), at most 10 a run.</summary>
        public const int CacheEvery = 3, MaxCaches = 10;

        public static int Caches(int roundsSurvived) => Math.Min(MaxCaches, Math.Max(0, roundsSurvived) / CacheEvery);

        /// <summary>True when finishing this round earns a supply cache.</summary>
        public static bool CacheAfter(int round) => round > 0 && round % CacheEvery == 0 && round / CacheEvery <= MaxCaches;

        public const int ShardsPerCache = 15;
        public const int RaidWinShards = 150;
        public const int HiddenBossShards = 500;

        /// <summary>
        /// Star Shards for a run: 5 a round survived, 1 per 4 kills, the supply caches, a Raid win bonus, and
        /// Hardcore pays half again.
        /// </summary>
        public static int Shards(SurvivalMode m, int round, int kills, bool won)
        {
            int survived = Math.Max(0, won ? round : round - 1);
            int s = survived * 5 + kills / 4 + Caches(survived) * ShardsPerCache;
            if (won) s += RaidWinShards;
            if (m == SurvivalMode.Hardcore) s = (int)Math.Round(s * 1.5f);
            return s;
        }
    }
}
