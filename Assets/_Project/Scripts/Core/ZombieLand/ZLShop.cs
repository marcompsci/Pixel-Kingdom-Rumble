using System;
using System.Collections.Generic;

namespace PKR.Core
{
    /// <summary>Perks sold at safehouse supply crates (Zombie Land) and perk machines (Last Stand).</summary>
    public enum ZLPerk { ThickSkin, QuickHands, SteadyAim, FleetFoot, SecondChance }

    /// <summary>Power-ups the dead drop now and then.</summary>
    public enum ZLPowerUp { InstaKill, DoubleScrap, Nuke, MaxAmmo }

    /// <summary>
    /// The classic zombies loop for the top-down modes: SCRAP from kills, nests and missions buys supplies, perks,
    /// mystery box spins (with a box-only wonder weapon) and the upgrade forge; power-ups drop with a pity timer so
    /// dry spells never last. Pure numbers and progress changes, unit-tested.
    /// </summary>
    public static class ZLShop
    {
        // ---- scrap ------------------------------------------------------------------------------------------
        public static int ScrapForKill(ZLFoe f)
        {
            switch (f)
            {
                case ZLFoe.Runner: return 40;
                case ZLFoe.Screamer:
                case ZLFoe.Bloater:
                case ZLFoe.Spitter: return 50;
                case ZLFoe.Hound: return 60;
                case ZLFoe.Splitter:
                case ZLFoe.Burrower: return 60;
                case ZLFoe.SplitterHalf: return 15;
                case ZLFoe.Brute: return 150;
                case ZLFoe.Herald: return 500;
                case ZLFoe.FirstTurned: return 2000;
                default: return 30;
            }
        }

        public const int NestScrap = 400, StoryMissionScrap = 800, BonusMissionScrap = 500, NukeScrap = 400;

        /// <summary>Adds scrap (doubled under Double Scrap). Returns what was added.</summary>
        public static int Earn(ZombieLandProgress p, int amount, bool doubled)
        {
            if (p == null || amount <= 0) return 0;
            int got = doubled ? amount * 2 : amount;
            p.scrap += got;
            p.scrapEarned += got;
            return got;
        }

        public static bool Spend(ZombieLandProgress p, int cost)
        {
            if (p == null || cost < 0 || p.scrap < cost) return false;
            p.scrap -= cost;
            return true;
        }

        // ---- prices -----------------------------------------------------------------------------------------
        public const int AmmoCost = 500, MedkitCost = 600, ArmorCost = 800, BoxCost = 950, ForgeCost = 4000;

        /// <summary>The Survivor Rank "Regular" unlock takes 15% off safehouse supplies.</summary>
        public static int Price(int baseCost, bool discount) => discount ? (int)Math.Round(baseCost * 0.85f / 10f) * 10 : baseCost;

        // ---- perks ------------------------------------------------------------------------------------------
        public const int PerkLimit = 4;

        public static int PerkCost(ZLPerk k)
        {
            switch (k)
            {
                case ZLPerk.ThickSkin: return 2500;
                case ZLPerk.QuickHands: return 2000;
                case ZLPerk.SteadyAim: return 3000;
                case ZLPerk.FleetFoot: return 1500;
                default: return 1500;
            }
        }

        public static string PerkName(ZLPerk k)
        {
            switch (k)
            {
                case ZLPerk.ThickSkin: return "THICK SKIN";
                case ZLPerk.QuickHands: return "QUICK HANDS";
                case ZLPerk.SteadyAim: return "STEADY AIM";
                case ZLPerk.FleetFoot: return "FLEET FOOT";
                default: return "SECOND CHANCE";
            }
        }

        public static string PerkInfo(ZLPerk k)
        {
            switch (k)
            {
                case ZLPerk.ThickSkin: return "+50 max health";
                case ZLPerk.QuickHands: return "Reload 40% faster";
                case ZLPerk.SteadyAim: return "+25% gun damage";
                case ZLPerk.FleetFoot: return "Move 12% faster";
                default: return "Get back up where you fell, once";
            }
        }

        public static string PerkKey(ZLPerk k) => k.ToString();

        public static bool HasPerk(ZombieLandProgress p, ZLPerk k) => p != null && p.perks.Contains(PerkKey(k));

        /// <summary>Buys a perk: false if owned, four are already held, or there isn't enough scrap.</summary>
        public static bool BuyPerk(ZombieLandProgress p, ZLPerk k, bool discount, int limit = PerkLimit)
        {
            if (p == null || HasPerk(p, k) || p.perks.Count >= limit) return false;
            if (!Spend(p, Price(PerkCost(k), discount))) return false;
            p.perks.Add(PerkKey(k));
            return true;
        }

        /// <summary>Second Chance is used up when it saves you.</summary>
        public static bool UseSecondChance(ZombieLandProgress p) => p != null && p.perks.Remove(PerkKey(ZLPerk.SecondChance));

        public static int MaxHealth(ZombieLandProgress p) => ZLRules.HeroHealth + (HasPerk(p, ZLPerk.ThickSkin) ? 50 : 0);
        public static float ReloadScale(ZombieLandProgress p) => HasPerk(p, ZLPerk.QuickHands) ? 0.6f : 1f;
        public static float SpeedScale(ZombieLandProgress p) => HasPerk(p, ZLPerk.FleetFoot) ? 1.12f : 1f;
        public static float DamageScale(ZombieLandProgress p) => HasPerk(p, ZLPerk.SteadyAim) ? 1.25f : 1f;

        // ---- the forge --------------------------------------------------------------------------------------
        public static bool IsForged(ZombieLandProgress p, string gunId) => p != null && p.forged.Contains(gunId);

        /// <summary>Whether the forge will take this gun (not relics, not twice).</summary>
        public static bool CanForge(ZombieLandProgress p, ZLGun g) => g != null && !g.isRelic && !IsForged(p, g.id);

        /// <summary>The gun as the hero holds it: forged guns hit twice as hard and carry half again as much ammo.</summary>
        public static ZLGun Effective(ZombieLandProgress p, ZLGun g)
        {
            if (g == null || !IsForged(p, g.id)) return g;
            if (ForgedCache.TryGetValue(g.id, out var cached)) return cached;
            var f = g.Clone();
            f.forged = true;
            f.name = "HOLLOW-FORGED " + g.name;
            f.damage = g.damage * 2;
            f.magazine = (int)Math.Round(g.magazine * 1.5f);
            f.maxReserve = (int)Math.Round(g.maxReserve * 1.5f);
            f.speed = g.speed * 1.1f;
            ForgedCache[g.id] = f;
            return f;
        }

        /// <summary>Forged copies are the same every time, so they're made once (no garbage every frame).</summary>
        static readonly Dictionary<string, ZLGun> ForgedCache = new Dictionary<string, ZLGun>();

        /// <summary>Forges a gun the hero owns and fills it. False if not allowed or too poor.</summary>
        public static bool Forge(ZombieLandProgress p, string gunId, int cost)
        {
            var g = ZLCatalog.Gun(gunId);
            var owned = p != null ? p.Gun(gunId) : null;
            if (owned == null || !CanForge(p, g) || !Spend(p, cost)) return false;
            p.forged.Add(gunId);
            var e = Effective(p, g);
            owned.mag = e.magazine;
            owned.reserve = e.maxReserve;
            return true;
        }

        /// <summary>Damage of one shot, with the forge and Steady Aim.</summary>
        public static int ShotDamage(ZombieLandProgress p, ZLGun g)
        {
            var e = Effective(p, g);
            if (e == null) return 0;
            return Math.Max(1, (int)Math.Round(e.damage * DamageScale(p)));
        }

        // ---- the mystery box --------------------------------------------------------------------------------
        /// <summary>Spins without the wonder weapon before it's certain (players hated pure luck).</summary>
        public const int BoxPity = 5;

        /// <summary>What the box can give: every gun but the pistol and the relics; the wonder weapon is rarer.</summary>
        public static List<(ZLGun gun, float weight)> BoxPool()
        {
            var list = new List<(ZLGun, float)>();
            foreach (var g in ZLCatalog.Guns)
            {
                if (g.isRelic || g.bossDrop || g.id == ZLCatalog.Pistol.id) continue;
                list.Add((g, g.wonder ? 0.6f : g.id == "thumper" ? 0.7f : 1f));
            }
            return list;
        }

        /// <summary>A weighted roll (roll in [0,1)), never the gun in hand; certain wonder after BoxPity dry spins.</summary>
        public static ZLGun RollBox(double roll, string holdingId, int spinsSinceWonder)
        {
            var pool = BoxPool();
            // Pity: after BoxPity dry spins a wonder weapon is certain (whichever of the two you're not holding).
            if (spinsSinceWonder >= BoxPity)
            {
                var w = roll < 0.5 ? ZLCatalog.StormCoil : ZLCatalog.RimeCannon;
                if (holdingId == w.id) w = w == ZLCatalog.StormCoil ? ZLCatalog.RimeCannon : ZLCatalog.StormCoil;
                return w;
            }
            float total = 0f;
            foreach (var (g, w) in pool) if (g.id != holdingId) total += w;
            double t = roll * total;
            foreach (var (g, w) in pool)
            {
                if (g.id == holdingId) continue;
                if ((t -= w) < 0) return g;
            }
            for (int i = pool.Count - 1; i >= 0; i--) if (pool[i].gun.id != holdingId) return pool[i].gun;
            return pool[0].gun;
        }

        /// <summary>Applies a box spin to Zombie Land progress (gives the gun, counts the pity). Returns the prize.</summary>
        public static ZLGun SpinBox(ZombieLandProgress p, double roll, int cost)
        {
            if (!Spend(p, cost)) return null;
            var prize = RollBox(roll, p.equipped, p.spinsSinceWonder);
            p.spinsSinceWonder = prize.wonder ? 0 : p.spinsSinceWonder + 1;
            var e = Effective(p, prize);
            ZLRules.GiveGun(p, prize, e.magazine + e.maxReserve);
            p.equipped = prize.id;
            return prize;
        }

        // ---- supplies -----------------------------------------------------------------------------------------
        /// <summary>Every gun's magazine and reserve full.</summary>
        public static void MaxAmmo(ZombieLandProgress p)
        {
            foreach (var g in p.guns)
            {
                var e = Effective(p, ZLCatalog.Gun(g.id));
                if (e == null) continue;
                g.mag = e.magazine;
                g.reserve = e.maxReserve;
            }
        }

        // ---- power-ups ----------------------------------------------------------------------------------------
        public const float PowerUpChance = 0.03f;
        public const int PowerUpPity = 35;
        public const float PowerUpSeconds = 20f, PowerUpLifetime = 18f, NukeRadius = 16f;

        /// <summary>Whether a kill drops a power-up: a small chance, certain after PowerUpPity dry kills.</summary>
        public static bool DropsPowerUp(int killsSinceDrop, double roll, float chance = PowerUpChance, int pity = PowerUpPity) =>
            killsSinceDrop >= pity || roll < chance;

        public static ZLPowerUp PickPowerUp(double roll)
        {
            if (roll < 0.3) return ZLPowerUp.MaxAmmo;
            if (roll < 0.58) return ZLPowerUp.DoubleScrap;
            if (roll < 0.84) return ZLPowerUp.InstaKill;
            return ZLPowerUp.Nuke;
        }

        public static string PowerUpName(ZLPowerUp k)
        {
            switch (k)
            {
                case ZLPowerUp.InstaKill: return "INSTA-KILL";
                case ZLPowerUp.DoubleScrap: return "DOUBLE SCRAP";
                case ZLPowerUp.Nuke: return "NUKE";
                default: return "MAX AMMO";
            }
        }

        /// <summary>Art name for a power-up (Resources/PKR/ZombieLand/Props/pu_{name}).</summary>
        public static string PowerUpArt(ZLPowerUp k)
        {
            switch (k)
            {
                case ZLPowerUp.InstaKill: return "pu_instakill";
                case ZLPowerUp.DoubleScrap: return "pu_double";
                case ZLPowerUp.Nuke: return "pu_nuke";
                default: return "pu_ammo";
            }
        }
    }
}
