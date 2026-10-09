using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PKR.EditorTools
{
    /// <summary>
    /// Generates ORIGINAL placeholder pixel sprites as PNGs (16 px per unit, point filtered).
    /// Every file is prefixed "ph_" and lives in Art/Placeholder so it is obvious what to replace.
    /// Existing files are left alone, so you can hand-edit or swap them freely.
    /// </summary>
    public static class PlaceholderArt
    {
        public const string Folder = "Assets/_Project/Art/Placeholder/Sprites";
        public const int PPU = 16;

        // Palette: warm, high-contrast for small screens.
        static readonly Color32 Clear = new Color32(0, 0, 0, 0);
        static readonly Color32 GrassLight = new Color32(126, 214, 84, 255);
        static readonly Color32 GrassDark = new Color32(72, 160, 64, 255);
        static readonly Color32 Soil = new Color32(176, 112, 64, 255);
        static readonly Color32 SoilDark = new Color32(132, 78, 46, 255);
        static readonly Color32 Stone = new Color32(120, 128, 168, 255);
        static readonly Color32 StoneLight = new Color32(168, 178, 214, 255);
        static readonly Color32 StoneDark = new Color32(78, 84, 120, 255);
        static readonly Color32 Plank = new Color32(214, 150, 82, 255);
        static readonly Color32 PlankDark = new Color32(150, 96, 52, 255);
        static readonly Color32 Brass = new Color32(236, 186, 74, 255);
        // Nova: sky courier. Teal coat, coral scarf, pale face, comet-gold trim.
        static readonly Color32 NovaCoat = new Color32(46, 168, 178, 255);
        static readonly Color32 NovaCoatDark = new Color32(28, 112, 130, 255);
        static readonly Color32 NovaScarf = new Color32(255, 112, 96, 255);
        static readonly Color32 NovaSkin = new Color32(255, 214, 170, 255);
        static readonly Color32 NovaHair = new Color32(64, 40, 96, 255);
        static readonly Color32 Ink = new Color32(30, 24, 40, 255);

        public static Sprite Ground() => GetOrCreate("ph_ground", 16, 16, false, (x, y) =>
        {
            if (y >= 12) return ((x + y) % 5 == 0) ? GrassDark : GrassLight;       // grass cap
            if (y == 11) return (x % 3 == 0) ? GrassDark : SoilDark;                // grass roots
            return ((x * 7 + y * 3) % 11 == 0) ? SoilDark : Soil;                   // speckled soil
        });

        public static Sprite StoneBlock() => GetOrCreate("ph_stone", 16, 16, false, (x, y) =>
        {
            if (y == 15 || x == 0) return StoneLight;
            if (y == 0 || x == 15) return StoneDark;
            if (y == 8 || (y > 8 ? x == 7 : x == 11)) return StoneDark;              // brick seams
            return Stone;
        });

        public static Sprite OneWay() => GetOrCreate("ph_oneway", 16, 16, false, (x, y) =>
        {
            if (y < 11) return Clear;
            if (y == 15) return Brass;                                              // brass edge = "you can stand here"
            return (x == 0 || x == 8 || y == 11) ? PlankDark : Plank;
        });

        public static Sprite Nova() => GetOrCreate("ph_nova", 16, 24, true, NovaPixel);

        static readonly Color32 Straw = new Color32(232, 196, 112, 255);
        static readonly Color32 StrawDark = new Color32(190, 150, 74, 255);
        static readonly Color32 Ring = new Color32(214, 64, 64, 255);
        static readonly Color32 Post = new Color32(140, 92, 54, 255);

        /// <summary>Training dummy: a wooden post with a round straw target. 16x24, bottom pivot.</summary>
        public static Sprite Dummy() => GetOrCreate("ph_dummy", 16, 24, true, (x, y) =>
        {
            // Base plank
            if (y <= 1) return (x >= 3 && x <= 12) ? PlankDark : Clear;
            // Post
            if (y <= 9) return (x >= 7 && x <= 8) ? Post : Clear;
            // Round straw target centered at (7.5, 16), radius 6.5, with red rings
            float dx = x - 7.5f, dy = y - 16f;
            float d = (float)Math.Sqrt(dx * dx + dy * dy);
            if (d > 6.5f) return (x >= 7 && x <= 8 && y <= 11) ? Post : Clear;
            if (d > 5.6f) return StrawDark;
            if ((d > 3.4f && d <= 4.4f) || d <= 1.3f) return Ring;
            return ((x + y) % 4 == 0) ? StrawDark : Straw;
        });

        static Color32 NovaPixel(int x, int y)
        {
            // 16x24, facing right. Built from simple shapes; replace with production art later.
            // Boots
            if (y <= 1) return (x >= 4 && x <= 6) || (x >= 9 && x <= 11) ? Ink : Clear;
            // Legs
            if (y <= 5) return (x >= 5 && x <= 6) || (x >= 9 && x <= 10) ? NovaCoatDark : Clear;
            // Coat body with flared hem
            if (y <= 13)
            {
                int half = y <= 7 ? 6 : 5;
                if (x < 8 - half || x > 7 + half) return Clear;
                if (y == 13 && x >= 4 && x <= 11) return Brass;                       // belt
                return (x == 8 - half || x == 7 + half) ? NovaCoatDark : NovaCoat;
            }
            // Scarf, trailing behind (left) like a comet tail
            if (y <= 15)
            {
                if (x >= 4 && x <= 11) return NovaScarf;
                if (x >= 1 && x <= 3 && y == 15 - (3 - x) / 2) return NovaScarf;
                return Clear;
            }
            // Head
            if (y <= 21)
            {
                if (x < 5 || x > 11) return Clear;
                if (y >= 20) return NovaHair;
                if (x == 10 && y == 18) return Ink;                                   // eye
                if (x == 5) return NovaHair;
                return NovaSkin;
            }
            // Hair tuft + star clip
            if (y <= 23)
            {
                if (x == 11 && y == 22) return Brass;
                return (x >= 5 && x <= 10) ? NovaHair : Clear;
            }
            return Clear;
        }

        // ---- Sunspire Meadows set ----------------------------------------------------------------
        static readonly Color32 BrassDark = new Color32(170, 120, 40, 255);
        static readonly Color32 Steel = new Color32(190, 200, 214, 255);
        static readonly Color32 SteelDark = new Color32(110, 118, 140, 255);
        static readonly Color32 White = new Color32(255, 255, 255, 255);
        static readonly Color32 Gray = new Color32(170, 170, 180, 255);
        static readonly Color32 ShardGold = new Color32(255, 214, 80, 255);
        static readonly Color32 ShardLight = new Color32(255, 248, 196, 255);
        static readonly Color32 Ruby = new Color32(232, 64, 92, 255);
        static readonly Color32 RubyLight = new Color32(255, 150, 170, 255);

        static float Dist(float x, float y, float cx, float cy) => (float)Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));

        /// <summary>Cog Beetle: a brass dome shell with a gear-tooth rim and stubby legs. 16x12, faces right.</summary>
        public static Sprite CogBeetle() => GetOrCreate("ph_cog_beetle", 16, 12, true, (x, y) =>
        {
            if (y <= 1) return (x == 3 || x == 6 || x == 9 || x == 12) ? Ink : Clear;                 // legs
            float d = Dist(x, y, 7.5f, 2f);
            if (y >= 2 && d <= 7.5f)
            {
                if (d > 6.6f) return (x % 2 == 0) ? BrassDark : Clear;                                  // gear teeth
                if (x >= 11 && y >= 3 && y <= 5) return (x == 13 && y == 4) ? Ink : SteelDark;          // face plate
                return ((x + y) % 5 == 0) ? BrassDark : Brass;
            }
            return Clear;
        });

        /// <summary>Spring Tick: a round clockwork critter on a coiled spring. 14x14, faces right.</summary>
        public static Sprite SpringTick() => GetOrCreate("ph_spring_tick", 14, 14, true, (x, y) =>
        {
            if (y <= 4) return (x >= 5 && x <= 8 && y % 2 == 0) ? SteelDark : ((x == 4 || x == 9) && y % 2 == 1 ? Steel : Clear); // spring
            float d = Dist(x, y, 6.5f, 9f);
            if (d <= 4.6f)
            {
                if (x == 9 && y == 10) return Ink;                                                      // eye
                if (d > 3.8f) return SteelDark;
                return (y == 9) ? Brass : Steel;                                                        // brass band
            }
            if (x == 6 && y == 13) return Brass;                                                        // winding key
            return Clear;
        });

        /// <summary>Gyro Moth: a round brass body with two spinning steel wings and a glass eye. 16x12, faces right.</summary>
        public static Sprite GyroMoth() => GetOrCreate("ph_gyro_moth", 16, 12, true, (x, y) =>
        {
            float body = Dist(x, y, 8f, 5.5f);
            if (body <= 3.4f)
            {
                if (x == 10 && y == 6) return Ink;                                                     // eye
                if (body > 2.6f) return BrassDark;
                return (y == 5) ? Steel : Brass;
            }
            // Wings: two thin ovals above the body, angled out.
            float wl = Dist(x * 0.6f, y, 2.4f, 9.5f), wr = Dist(x * 0.6f, y, 7.2f, 9.5f);
            if (y >= 8 && (wl <= 1.6f || wr <= 1.6f)) return ((x + y) % 3 == 0) ? SteelDark : Steel;
            if (y <= 1 && x >= 7 && x <= 8) return SteelDark;                                         // stinger
            return Clear;
        });

        /// <summary>Bolt Knight: a squat steel automaton with a riveted helm. 14x20, faces right.</summary>
        public static Sprite BoltKnight() => GetOrCreate("ph_bolt_knight", 14, 20, true, (x, y) =>
        {
            if (y <= 2) return (x >= 3 && x <= 5) || (x >= 8 && x <= 10) ? SteelDark : Clear;      // feet
            if (y <= 11 && x >= 2 && x <= 11) return ((x == 2 || x == 11) ? SteelDark : ((x + y) % 4 == 0 ? Brass : Steel)); // body
            if (y <= 18 && x >= 3 && x <= 10)
            {
                if (y == 15 && x >= 7 && x <= 9) return Ink;                                         // visor slit
                return (y == 18 || x == 3 || x == 10) ? SteelDark : Steel;                           // helm
            }
            if (y == 19 && x >= 6 && x <= 7) return Brass;                                          // crest bolt
            return Clear;
        });

        /// <summary>Bolt Knight's tower shield: a tall riveted brass plate. 6x16.</summary>
        public static Sprite BoltShield() => GetOrCreate("ph_bolt_shield", 6, 16, false, (x, y) =>
        {
            if (x == 0 || x == 5 || y == 0 || y == 15) return BrassDark;
            if ((x == 2 || x == 3) && y % 4 == 2) return Ink;                                         // rivets
            return Brass;
        });

        /// <summary>Spikes: steel teeth on a dark base, tiles horizontally. 16x8.</summary>
        public static Sprite Spikes() => GetOrCreate("ph_spikes", 16, 8, false, (x, y) =>
        {
            if (y <= 1) return SteelDark;
            int local = x % 4;                        // four teeth per tile
            float half = (7 - y) * 0.36f;            // triangle teeth: wide at the base, sharp at the top
            if (Math.Abs(local - 1.5f) <= half) return local < 2 ? Steel : White;
            return Clear;
        });

        /// <summary>Checkpoint flag in grayscale so it can be tinted (inactive gray, active teal). 16x32, bottom pivot.</summary>
        public static Sprite CheckpointFlag() => GetOrCreate("ph_checkpoint", 16, 32, true, (x, y) =>
        {
            if (y <= 1) return (x >= 4 && x <= 9) ? Gray : Clear;                                      // base
            if (x == 6 || x == 7) return (y <= 30) ? Gray : White;                                     // pole + cap
            if (y >= 20 && y <= 28 && x >= 8)
            {
                int w = 8 - Math.Abs(y - 24) * 2;                                                       // pennant
                if (x - 8 <= w) return (y == 24 && x > 9) ? Gray : White;
            }
            return Clear;
        });

        /// <summary>Goal gate: a brass arch with a gear keystone. 32x48, bottom pivot.</summary>
        public static Sprite GoalGate() => GetOrCreate("ph_goal_gate", 32, 48, true, (x, y) =>
        {
            bool pillar = (x <= 4 || x >= 27) && y <= 34;
            float d = Dist(x, y, 15.5f, 30f);
            bool arch = y > 30 && d <= 15.5f && d >= 11f;
            if (pillar) return (x == 0 || x == 31 || y % 6 == 0) ? BrassDark : Brass;
            if (arch)
            {
                if (d >= 14.6f) return BrassDark;
                return Brass;
            }
            float k = Dist(x, y, 15.5f, 44f);
            if (k <= 3.6f) return k <= 1.2f ? Ink : ShardGold;                                          // keystone gear
            if (y > 30 && d < 11f && y < 44) return new Color32(255, 240, 180, 90);                     // shimmer
            return Clear;
        });

        /// <summary>Star Shard: a faceted gold diamond. 12x12.</summary>
        public static Sprite StarShard() => GetOrCreate("ph_star_shard", 12, 12, false, (x, y) =>
        {
            float m = Math.Abs(x - 5.5f) + Math.Abs(y - 5.5f);
            if (m > 5.6f) return Clear;
            if (m > 4.6f) return BrassDark;
            return (x < 6 && y > 5) ? ShardLight : ShardGold;
        });

        /// <summary>Health pickup: a ruby crystal. 12x12.</summary>
        public static Sprite HealthCrystal() => GetOrCreate("ph_health_crystal", 12, 12, false, (x, y) =>
        {
            float cx = 5.5f;
            int half = y >= 7 ? 5 - (y - 7) : (int)(y * 0.8f);
            if (y > 10 || Math.Abs(x - cx) > half + 0.5f) return Clear;
            if (Math.Abs(x - cx) > half - 0.5f) return new Color32(150, 30, 50, 255);
            return (x < 6 && y > 5) ? RubyLight : Ruby;
        });

        /// <summary>Background gear (decor only). 32x32.</summary>
        public static Sprite DecorGear() => GetOrCreate("ph_decor_gear", 32, 32, false, (x, y) =>
        {
            float d = Dist(x, y, 15.5f, 15.5f);
            double ang = Math.Atan2(y - 15.5, x - 15.5);
            bool tooth = d <= 15.5f && d > 12f && Math.Cos(ang * 10) > 0.3;
            if (tooth || (d <= 12f && d > 4f)) return new Color32(200, 150, 90, 120);
            if (d <= 2.5f) return new Color32(200, 150, 90, 120);
            return Clear;
        });

        /// <summary>Moving platform plate: brass with rivets, tiles horizontally. 16x8.</summary>
        public static Sprite PlatformPlate() => GetOrCreate("ph_platform_plate", 16, 8, false, (x, y) =>
        {
            if (y == 7) return ShardLight;
            if (y == 0) return BrassDark;
            if ((x == 2 || x == 13) && y == 4) return Ink;
            return Brass;
        });

        // ---- Locked heroes (shown as silhouettes in Phase 1, but drawn in full for later) ---------
        static readonly Color32 Granite = new Color32(140, 136, 150, 255);
        static readonly Color32 GraniteDark = new Color32(92, 88, 104, 255);
        static readonly Color32 Moss = new Color32(96, 176, 84, 255);
        static readonly Color32 Amber = new Color32(255, 176, 48, 255);
        static readonly Color32 Overalls = new Color32(240, 132, 52, 255);
        static readonly Color32 OverallsDark = new Color32(186, 92, 34, 255);
        static readonly Color32 Glove = new Color32(150, 86, 210, 255);
        static readonly Color32 GloveDark = new Color32(100, 52, 150, 255);
        static readonly Color32 TealHair = new Color32(40, 190, 180, 255);
        static readonly Color32 Lens = new Color32(170, 240, 255, 255);
        static readonly Color32 Scale = new Color32(92, 196, 96, 255);
        static readonly Color32 ScaleDark = new Color32(56, 138, 70, 255);
        static readonly Color32 Belly = new Color32(250, 230, 140, 255);
        static readonly Color32 SkateBlue = new Color32(70, 120, 230, 255);

        /// <summary>Brick: a broad stone guardian with a moss crown and amber eyes. 16x24, bottom pivot.</summary>
        public static Sprite Brick() => GetOrCreate("ph_brick", 16, 24, true, (x, y) =>
        {
            if (y <= 2) return (x >= 2 && x <= 6) || (x >= 9 && x <= 13) ? GraniteDark : Clear;        // feet
            if (y <= 16)
            {
                if (x < 1 || x > 14) return Clear;
                if (y >= 6 && y <= 13 && (x <= 2 || x >= 13)) return GraniteDark;                     // fists/arms
                if (x == 1 || x == 14) return Clear;
                return ((x * 3 + y * 5) % 9 == 0) ? GraniteDark : Granite;                             // chiselled body
            }
            if (y <= 21)
            {
                if (x < 3 || x > 12) return Clear;
                if (y == 18 && (x == 6 || x == 10)) return Amber;                                     // eyes
                return Granite;
            }
            if (y <= 23) return (x >= 3 && x <= 12 && (x + y) % 3 != 0) ? Moss : Clear;               // moss crown
            return Clear;
        });

        /// <summary>Luma: an inventor in orange overalls with oversized magnet gloves and goggles. 16x24.</summary>
        public static Sprite Luma() => GetOrCreate("ph_luma", 16, 24, true, (x, y) =>
        {
            if (y <= 1) return (x >= 5 && x <= 6) || (x >= 9 && x <= 10) ? Ink : Clear;
            if (y <= 4) return (x >= 5 && x <= 6) || (x >= 9 && x <= 10) ? OverallsDark : Clear;
            if (y <= 13)
            {
                if (y >= 6 && y <= 10 && (x <= 3 || x >= 12)) return (x == 0 || x == 15 || y == 6) ? GloveDark : Glove; // big gloves
                if (x < 4 || x > 11) return Clear;
                if (y == 12 && (x == 6 || x == 9)) return Brass;                                     // buttons
                return (x == 4 || x == 11) ? OverallsDark : Overalls;
            }
            if (y <= 20)
            {
                if (x < 5 || x > 11) return Clear;
                if (y == 17 && (x == 7 || x == 10)) return Ink;
                return NovaSkin;
            }
            if (y <= 23)
            {
                if (y == 21 && x >= 5 && x <= 11) return (x == 7 || x == 10) ? Lens : SteelDark;      // goggles
                return (x >= 4 && x <= 12) ? TealHair : Clear;
            }
            return Clear;
        });

        /// <summary>Rex Rollo: a green lizard on roller skates with a long tail. 16x24, faces right.</summary>
        public static Sprite RexRollo() => GetOrCreate("ph_rex_rollo", 16, 24, true, (x, y) =>
        {
            if (y == 0) return (x == 4 || x == 6 || x == 9 || x == 11) ? Ink : Clear;                 // wheels
            if (y <= 2) return (x >= 3 && x <= 7) || (x >= 8 && x <= 12) ? SkateBlue : Clear;         // skates
            if (y <= 5) return (x >= 5 && x <= 6) || (x >= 9 && x <= 10) ? ScaleDark : Clear;         // legs
            if (y <= 14)
            {
                if (x <= 3 && y <= 9 && y >= 6 - (3 - x)) return ScaleDark;                           // tail
                if (x < 4 || x > 12) return Clear;
                if (x >= 7 && x <= 11 && y <= 12) return Belly;
                return Scale;
            }
            if (y <= 20)
            {
                if (x < 6 || x > 14) return Clear;
                if (y == 18 && x == 12) return Ink;                                                    // eye
                if (y <= 16 && x >= 12) return Belly;                                                  // snout
                return Scale;
            }
            if (y <= 22) return (x >= 7 && x <= 12 && (x % 2 == 0)) ? ScaleDark : Clear;              // head spines
            return Clear;
        });

        // ---- Phase 3.2/3.3: stealth and jump-and-run props ------------------------------------------
        static readonly Color32 Cloak = new Color32(70, 64, 110, 255);
        static readonly Color32 CloakDark = new Color32(44, 40, 76, 255);
        static readonly Color32 Lantern = new Color32(255, 214, 110, 255);
        static readonly Color32 Hay = new Color32(232, 196, 92, 255);
        static readonly Color32 HayDark = new Color32(186, 146, 60, 255);
        static readonly Color32 Leaf = new Color32(84, 170, 72, 255);
        static readonly Color32 LeafDark = new Color32(46, 116, 54, 255);
        static readonly Color32 Water = new Color32(70, 130, 220, 200);
        static readonly Color32 WaterLight = new Color32(140, 196, 255, 220);
        static readonly Color32 Crate = new Color32(196, 120, 60, 255);
        static readonly Color32 CrateDark = new Color32(130, 74, 36, 255);

        /// <summary>Gearwatch Sentry: a hooded night watchman with a lantern held forward. 14x22, faces right.</summary>
        public static Sprite GearwatchSentry() => GetOrCreate("ph_gearwatch_sentry", 14, 22, true, (x, y) =>
        {
            if (y <= 1) return (x >= 3 && x <= 5) || (x >= 7 && x <= 9) ? Ink : Clear;                 // boots
            if (y <= 13 && x >= 2 && x <= 10)
            {
                if (x == 2 || x == 10) return CloakDark;
                if (y == 8) return Brass;                                                             // belt
                return (x + y) % 6 == 0 ? CloakDark : Cloak;
            }
            if (y >= 5 && y <= 9 && x >= 11 && x <= 13) return y == 9 || y == 5 ? BrassDark : Lantern; // lantern
            if (y <= 20 && x >= 3 && x <= 9)
            {
                if (y >= 15 && y <= 17 && x >= 6 && x <= 8) return y == 16 && x == 8 ? Lantern : Ink; // face in shadow, one glowing eye
                return (y == 20 || x == 3) ? CloakDark : Cloak;                                        // hood
            }
            if (y == 21 && x >= 4 && x <= 7) return CloakDark;
            return Clear;
        });

        /// <summary>Spring pad: a brass coil under a red cap. 16x8.</summary>
        public static Sprite SpringPad() => GetOrCreate("ph_spring_pad", 16, 8, false, (x, y) =>
        {
            if (y >= 6) return x >= 1 && x <= 14 ? (y == 7 ? new Color32(255, 120, 110, 255) : new Color32(220, 70, 70, 255)) : Clear;
            if (y == 0) return x >= 2 && x <= 13 ? BrassDark : Clear;
            if (x >= 4 && x <= 11) return (x + y * 2) % 4 < 2 ? Brass : BrassDark;                    // coil
            return Clear;
        });

        /// <summary>Shard crate: a riveted wooden crate with a star mark. 16x16.</summary>
        public static Sprite ShardCrate() => GetOrCreate("ph_shard_crate", 16, 16, false, (x, y) =>
        {
            if (x == 0 || y == 0 || x == 15 || y == 15) return CrateDark;
            if ((x == 2 || x == 13) && (y == 2 || y == 13)) return Brass;
            float m = Math.Abs(x - 7.5f) + Math.Abs(y - 7.5f);
            if (m < 3.6f) return m < 2.4f ? ShardLight : ShardGold;
            return (y % 5 == 0) ? CrateDark : Crate;
        });

        /// <summary>An opened (empty) shard crate. 16x16.</summary>
        public static Sprite ShardCrateEmpty() => GetOrCreate("ph_shard_crate_empty", 16, 16, false, (x, y) =>
        {
            if (x == 0 || y == 0 || x == 15 || y == 15) return StoneDark;
            return (y % 5 == 0) ? StoneDark : Stone;
        });

        /// <summary>Climbing vine segment (tiles vertically). 16x16.</summary>
        public static Sprite Vine() => GetOrCreate("ph_vine", 16, 16, false, (x, y) =>
        {
            int stem = 7 + ((y / 4) % 2 == 0 ? 0 : 1);
            if (x == stem || x == stem + 1) return LeafDark;
            if ((y % 6 == 2 && x >= stem - 4 && x < stem) || (y % 6 == 5 && x > stem + 1 && x <= stem + 5)) return Leaf;
            return Clear;
        });

        /// <summary>Hay bale / hiding spot (tiles horizontally). 16x16.</summary>
        public static Sprite HayBale() => GetOrCreate("ph_hay_bale", 16, 16, false, (x, y) =>
        {
            if (y > 13 && (x * 3 + y) % 4 == 0) return Clear;                                      // ragged top
            if (y == 4 || y == 11) return HayDark;                                                  // bindings
            return (x * 5 + y * 3) % 7 == 0 ? HayDark : Hay;
        });

        /// <summary>Water surface for pits (decor, tiles). 16x16.</summary>
        public static Sprite WaterTile() => GetOrCreate("ph_water", 16, 16, false, (x, y) =>
        {
            if (y == 15) return (x % 6 < 3) ? WaterLight : Water;
            return Water;
        });

        /// <summary>The Gearwright's Ledger: a gold-banded book. 12x12.</summary>
        public static Sprite Ledger() => GetOrCreate("ph_ledger", 12, 12, false, (x, y) =>
        {
            if (x < 1 || x > 10 || y < 1 || y > 10) return Clear;
            if (x == 1) return BrassDark;                                                           // spine
            if (y == 5 || y == 6) return Brass;                                                     // band
            return (x == 10 || y == 1 || y == 10) ? new Color32(110, 30, 40, 255) : new Color32(170, 50, 60, 255);
        });

        /// <summary>Creates (once) a PNG sprite from a pixel function and imports it as a point-filtered sprite.</summary>
        public static Sprite GetOrCreate(string name, int w, int h, bool bottomPivot, Func<int, int, Color32> pixel)
        {
            EditorUtil.EnsureFolder(Folder);
            string path = $"{Folder}/{name}.png";
            if (!File.Exists(path))
            {
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                var px = new Color32[w * h];
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[y * w + x] = pixel(x, y);
                tex.SetPixels32(px);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

                var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = PPU;
                ti.filterMode = FilterMode.Point;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                var settings = new TextureImporterSettings();
                ti.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect; // required for SpriteDrawMode.Tiled
                settings.spriteAlignment = (int)(bottomPivot ? SpriteAlignment.BottomCenter : SpriteAlignment.Center);
                ti.SetTextureSettings(settings);
                ti.SaveAndReimport();
            }
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) Debug.LogError($"[PKR] Failed to load generated sprite {path}");
            return sprite;
        }
    }
}
