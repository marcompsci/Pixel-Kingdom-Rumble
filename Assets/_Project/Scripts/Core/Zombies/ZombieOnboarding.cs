using System;
using System.Collections.Generic;

namespace PKR.Core
{
    /// <summary>
    /// First-time coaching for the zombie modes: one short tip the first time each thing matters (shown once per save,
    /// one at a time). New players in other mobile zombie games said they were dropped in with no idea what points,
    /// wall guns or the box were for; this teaches each idea at the moment it's useful instead of in a wall of text.
    /// </summary>
    public static class ZombieTips
    {
        public const string Points = "z_points", WallGun = "z_wallgun", Door = "z_door", Box = "z_box", Perk = "z_perk",
            LowAmmo = "z_lowammo", Hounds = "z_hounds", Riot = "z_riot", Leaper = "z_leaper", Downed = "z_downed",
            PowerUp = "z_powerup", Safehouse = "z_zl_safehouse", Drive = "z_zl_drive", Leads = "z_zl_map";

        public static readonly Dictionary<string, string> Texts = new Dictionary<string, string>
        {
            { Points, "EVERY HIT AND KILL PAYS POINTS\nSPEND THEM ON GUNS, DOORS AND PERKS" },
            { WallGun, "WALL GUN: TAP USE TO BUY IT\nBUY IT AGAIN FOR AMMO AT HALF PRICE" },
            { Door, "YOU CAN AFFORD THE DEBRIS\nCLEAR IT TO OPEN MORE OF THE MAP" },
            { Box, "THE MYSTERY BOX GIVES A RANDOM GUN\nTHE WONDER WEAPONS ARE IN THERE" },
            { Perk, "PERK MACHINES BOOST YOU ALL RUN\nYOU CAN HOLD FOUR" },
            { LowAmmo, "LOW ON AMMO? BUY MORE AT A WALL GUN\nOR GRAB A MAX AMMO POWER-UP" },
            { Hounds, "HOUND ROUND! KEEP MOVING\nDON'T GET TRAPPED IN A CORNER" },
            { Riot, "RIOT SHIELDS STOP BULLETS FROM THE FRONT\nFLANK THEM, KNIFE THEM OR BLOW THEM UP" },
            { Leaper, "A LEAPER CROUCHES (!) BEFORE IT JUMPS\nDODGE WHEN YOU SEE THE !" },
            { Downed, "A TEAMMATE IS DOWN!\nSTAND NEXT TO THEM AND HOLD USE TO REVIVE" },
            { PowerUp, "POWER-UP! WALK OVER IT TO GRAB IT" },
            { Safehouse, "SAFEHOUSES ARE CHECKPOINTS\nTAP USE INSIDE FOR SUPPLIES AND REST" },
            { Drive, "TAP USE NEXT TO A CAR OR BIKE TO DRIVE IT" },
            { Leads, "OPEN THE MAP TO SEE YOUR LEADS\nYELLOW MARKERS ARE STORY MISSIONS" },
        };

        /// <summary>Seconds between two tips, so they never pile up.</summary>
        public const float Gap = 7f;

        /// <summary>Seconds a tip stays on screen.</summary>
        public const float Show = 5.5f;
    }

    /// <summary>
    /// Gentler zombies for new and assisted players. ROOKIE WARM-UP: a player's first three Last Stand runs open with
    /// three slower, sparser rounds. ASSIST MODE (Settings) makes every zombie mode easier: slower, softer dead.
    /// </summary>
    public static class ZombieAssist
    {
        public const int RookieRuns = 3, RookieRounds = 3;

        public static bool Rookie(int lastStandRuns, int round) => lastStandRuns < RookieRuns && round <= RookieRounds;

        /// <summary>Multiplies zombie speed.</summary>
        public static float SpeedScale(bool assist, bool rookie) => (assist ? 0.85f : 1f) * (rookie ? 0.85f : 1f);

        /// <summary>Multiplies zombie health.</summary>
        public static float HealthScale(bool assist, bool rookie) => assist ? 0.8f : 1f;

        /// <summary>Multiplies the time between spawns (bigger = sparser).</summary>
        public static float SpawnScale(bool assist, bool rookie) => (assist ? 1.15f : 1f) * (rookie ? 1.4f : 1f);

        /// <summary>Auto-aim: the cone (degrees either side) and extra range. Strong aim assist is a Settings option.</summary>
        public static float AimCone(bool strong) => strong ? 110f : 75f;
        public static float AimRangeBonus(bool strong) => strong ? 2f : 0f;
    }
}
