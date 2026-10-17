using System;
using System.Collections.Generic;

namespace PKR.Core
{
    /// <summary>
    /// LAST STAND: HOLLOW BLOCK — round-based survival on a New Hollow city block (top-down, on the Zombie Land
    /// engine). Classic structure players asked for: a round counter, points for hits and kills, wall guns, doors that
    /// open more of the block, perk machines, the mystery box, the forge, power-ups, hound rounds, a boss, and a
    /// hidden Easter egg. Endless / Raid / Hardcore like the Manor.
    /// </summary>
    public static class LastStandRules
    {
        public const string MapId = "hollow_block";
        public const string BossName = "THE BUTCHER";
        public const string HiddenBossName = "THE HOLLOW KING";
        public const string HiddenBossSecret = "hollow_king";

        // ---- rounds -----------------------------------------------------------------------------------------
        public static int ZombiesInRound(int round) => ZombieRules.ZombiesInRound(round) + Math.Max(0, round - 1);

        /// <summary>At once on the block (top-down handles more than the side view): 8 in round 1, up to 24.</summary>
        public static int MaxAlive(int round) => Math.Min(24, 7 + Math.Max(1, round));

        public static float SpawnInterval(int round, SurvivalMode m) =>
            Math.Max(0.35f, 2f - 0.12f * (Math.Max(1, round) - 1)) * SurvivalModes.SpawnScale(m);

        /// <summary>Boss rounds: every 10th (Raid: the 10th, the last).</summary>
        public static bool IsBossRound(SurvivalMode m, int round) => round > 0 && round % 10 == 0;

        /// <summary>Hound rounds: the 5th, 15th, 25th... (only hounds, then a Max Ammo).</summary>
        public static bool IsHoundRound(SurvivalMode m, int round) => round > 0 && round % 10 == 5;

        public static int HoundsInRound(int round) => Math.Min(30, 6 + round / 2);

        // ---- the dead ---------------------------------------------------------------------------------------
        /// <summary>Which kind comes next (roll in [0,1)); new kinds join as the rounds climb.</summary>
        public static ZLFoe Pick(int round, double roll)
        {
            int r = Math.Max(1, round);
            if (r <= 2) return ZLFoe.Shambler;
            double brute = r >= 8 ? Math.Min(0.08, 0.03 + 0.005 * (r - 8)) : 0;
            double spitter = r >= 6 ? Math.Min(0.12, 0.05 + 0.01 * (r - 6)) : 0;
            double bloater = r >= 4 ? 0.08 : 0;
            double runner = Math.Min(0.45, 0.12 + 0.03 * (r - 3));
            double riot = r >= 7 ? Math.Min(0.08, 0.03 + 0.005 * (r - 7)) : 0;
            double leaper = r >= 9 ? Math.Min(0.1, 0.03 + 0.007 * (r - 9)) : 0;
            double splitter = r >= 5 ? Math.Min(0.08, 0.03 + 0.005 * (r - 5)) : 0;
            double burrower = r >= 11 ? Math.Min(0.07, 0.025 + 0.005 * (r - 11)) : 0;
            if ((roll -= riot) < 0) return ZLFoe.Riot;
            if ((roll -= leaper) < 0) return ZLFoe.Leaper;
            if ((roll -= splitter) < 0) return ZLFoe.Splitter;
            if ((roll -= burrower) < 0) return ZLFoe.Burrower;
            if ((roll -= brute) < 0) return ZLFoe.Brute;
            if ((roll -= spitter) < 0) return ZLFoe.Spitter;
            if ((roll -= bloater) < 0) return ZLFoe.Bloater;
            if ((roll -= runner) < 0) return ZLFoe.Runner;
            return ZLFoe.Shambler;
        }

        /// <summary>Health climbs 14% a round (Hardcore 40% more).</summary>
        public static int Health(ZLFoe f, int round, SurvivalMode m)
        {
            int r = Math.Max(1, round);
            if (f == ZLFoe.Herald) return BossHealth(round, m, false);
            float hp = ZLRules.Health(f, 1) * (1f + 0.14f * (r - 1)) * SurvivalModes.HealthScale(m);
            return Math.Max(1, (int)Math.Round(hp));
        }

        public static int BossHealth(int round, SurvivalMode m, bool hidden)
        {
            int baseHp = hidden ? 2600 : 600 + 120 * Math.Max(1, round);
            float scale = SurvivalModes.HealthScale(m) * (m == SurvivalMode.Raid && round >= SurvivalModes.RaidRounds && !hidden ? 1.5f : 1f);
            return (int)Math.Round(baseHp * scale);
        }

        /// <summary>Speed: shamblers start shuffling and break into a jog from round 4 (the classic sprinters later on).</summary>
        public static float Speed(ZLFoe f, int round, SurvivalMode m)
        {
            int r = Math.Max(1, round);
            float s = ZLRules.Speed(f, 1);
            if (f == ZLFoe.Shambler && r >= 4) s = Math.Min(3.6f, s + 0.18f * (r - 3));
            if (f == ZLFoe.Runner) s = Math.Min(5.2f, s + 0.05f * (r - 1));
            return s * SurvivalModes.SpeedScale(m);
        }

        // ---- points -----------------------------------------------------------------------------------------
        public const int HitPoints = 10, MeleeKillBonus = 70;

        public static int KillPoints(ZLFoe f)
        {
            switch (f)
            {
                case ZLFoe.Runner: return 70;
                case ZLFoe.Bloater:
                case ZLFoe.Spitter: return 90;
                case ZLFoe.Hound: return 80;
                case ZLFoe.Brute: return 150;
                case ZLFoe.Riot: return 110;
                case ZLFoe.Leaper: return 90;
                case ZLFoe.Splitter: return 80;
                case ZLFoe.SplitterHalf: return 40;
                case ZLFoe.Burrower: return 110;
                case ZLFoe.Herald: return 1000;
                default: return 60;
            }
        }

        public const int BoxCost = 950, ForgeCost = 5000, PerkLimit = 4;

        /// <summary>Last Stand boss phases: 1 above half health, 2 (enraged, calls runners) to a quarter, 3 (the horde).</summary>
        public static int BossPhase(float health01) => health01 <= 0.25f ? 3 : health01 <= 0.5f ? 2 : 1;

        /// <summary>Speed multiplier for a boss in this phase.</summary>
        public static float BossPhaseSpeed(int phase) => phase >= 3 ? 1.45f : phase == 2 ? 1.25f : 1f;

        /// <summary>The dead a boss calls when it enters a phase (kind, count).</summary>
        public static (ZLFoe kind, int count) BossSummons(int phase, bool hidden) =>
            phase == 2 ? (hidden ? ZLFoe.Leaper : ZLFoe.Runner, 4) : phase >= 3 ? (hidden ? ZLFoe.Riot : ZLFoe.Shambler, hidden ? 3 : 6) : (ZLFoe.Shambler, 0);

        /// <summary>An AI teammate with no ammo at all scrounges three magazines after this long.</summary>
        public const float AIResupplySeconds = 4f;

        /// <summary>The box moves to another spot after this many spins (the classic teddy bear moment).</summary>
        public const int BoxMovesAfter = 8;

        public static int PerkCost(ZLPerk k) => ZLShop.PerkCost(k) + (k == ZLPerk.SecondChance ? 0 : 500);

        public static int ForgePrice(bool forgeFriend) => forgeFriend ? (int)Math.Round(ForgeCost * 0.6f) : ForgeCost;

        /// <summary>Starting gun from the Survivor Rank Starter Gun tier.</summary>
        public static ZLGun StarterGun(int tier)
        {
            switch (tier)
            {
                case 1: return ZLCatalog.Gun("street_smg");
                case 2: return ZLCatalog.Gun("riot_shotgun");
                case 3: return ZLCatalog.Gun("assault_rifle");
                default: return ZLCatalog.Pistol;
            }
        }

        /// <summary>Rounds the starting pistol carries in Last Stand (a fair fight, not Zombie Land's 25).</summary>
        public const int PistolRounds = 60;

        // ---- the Easter egg ---------------------------------------------------------------------------------
        /// <summary>Graystone fragments hidden around the block; knife all of them to summon the Hollow King.</summary>
        public const int Fragments = 3;
    }

    /// <summary>The state of one Last Stand run: a SurvivalRun for points/rounds plus the top-down perks.</summary>
    public class LastStandRun
    {
        public SurvivalRun Run { get; }
        readonly HashSet<ZLPerk> _perks = new HashSet<ZLPerk>();
        public IEnumerable<ZLPerk> Perks => _perks;
        public int PerkCount => _perks.Count;
        public int FragmentsFound { get; private set; }
        public bool HiddenBossSummoned { get; set; }
        public bool HiddenBossBeaten { get; set; }
        public int BoxSpins { get; private set; }
        public bool FreeSpinLeft { get; set; }
        public List<string> DoorsOpen { get; } = new List<string>();

        public LastStandRun(SurvivalMode mode, int bonusPoints) { Run = new SurvivalRun(mode, bonusPoints); }

        public int PerkLimit => LastStandRules.PerkLimit + (HiddenBossBeaten ? 1 : 0);
        public bool Has(ZLPerk k) => _perks.Contains(k);

        public bool BuyPerk(ZLPerk k)
        {
            if (_perks.Contains(k) || _perks.Count >= PerkLimit) return false;
            if (!Run.Spend(LastStandRules.PerkCost(k))) return false;
            _perks.Add(k);
            return true;
        }

        public bool UseSecondChance() => _perks.Remove(ZLPerk.SecondChance);

        public bool OpenDoor(string id, int cost)
        {
            if (DoorsOpen.Contains(id) || !Run.Spend(cost)) return false;
            DoorsOpen.Add(id);
            return true;
        }

        /// <summary>Pays for a spin (free once with the Lucky First Spin kit). True if paid.</summary>
        public bool PayForSpin() => PayForSpin(LastStandRules.BoxCost);

        /// <summary>Pays for a spin at this price (weekly mutators can change it).</summary>
        public bool PayForSpin(int cost)
        {
            if (FreeSpinLeft) { FreeSpinLeft = false; BoxSpins++; return true; }
            if (!Run.Spend(cost)) return false;
            BoxSpins++;
            return true;
        }

        /// <summary>A spin paid for some other way (another player's points in co-op).</summary>
        public void CountBoxSpin() => BoxSpins++;

        public bool BoxShouldMove => BoxSpins > 0 && BoxSpins % LastStandRules.BoxMovesAfter == 0;

        /// <summary>Finds a fragment; true when that was the last one.</summary>
        public bool FindFragment()
        {
            if (FragmentsFound >= LastStandRules.Fragments) return false;
            FragmentsFound++;
            return FragmentsFound == LastStandRules.Fragments;
        }
    }
}
