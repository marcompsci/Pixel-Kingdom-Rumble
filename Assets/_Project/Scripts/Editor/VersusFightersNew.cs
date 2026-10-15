using System.Collections.Generic;
using PKR.Core;
using UnityEngine;
using Look = PKR.EditorTools.FighterArt.Look;
using B = PKR.EditorTools.FighterArt.Build;
using Hd = PKR.EditorTools.FighterArt.Head;
using Ex = PKR.EditorTools.FighterArt.Extra;

namespace PKR.EditorTools
{
    /// <summary>
    /// October 2026: eight more free fighters, drawn in the chibi style (all original). Same structure as the first ten:
    /// a three-hit combo with a SPECIAL finisher, Up + Attack, Air Attack, neutral / side / down Specials, a dodge and
    /// (most) a dive. Starting values only; tune the assets in the Inspector.
    /// </summary>
    public static partial class VersusFighters
    {
        public static List<CharacterDefinition> GetOrCreateNewCast() => new List<CharacterDefinition>
        {
            JuniBloom(), MarcoBrio(), TamsinReed(), DexOrtega(), PepperQuinn(), BoTidewell(), ZuriStarling(), OttoBrass()
        };

        static CharacterDefinition JuniBloom() => Fighter("JuniBloom", "juni_bloom", "Juni Bloom",
            "Gardener with a parasol and a green thumb.",
            "Juni tends the hanging gardens of Sunspire Meadows and talks to every plant by name. Her leaf parasol opens into a " +
            "shield, closes into a club, and throws razor leaves when she spins it. She hops like a seed on the wind.",
            new Look { build = B.Slim, head = Hd.Hair, extra = Ex.None, skin = C(250, 214, 186), hair = C(230, 150, 70),
                       top = C(120, 200, 120), accent = C(255, 160, 190), pants = C(250, 238, 210), shoes = C(160, 110, 70) },
            0.8f, Move(7.6f, 3.4f, 0.38f, airJumps: 1), 5, 3, 0f, Kit.Hop, "Seed Hop", null, () =>
            {
                var b = new Book("JuniBloom", "Juni");
                var fin = b.Strike("BloomBurst", "Bloom Burst", "Combo ender: the parasol snaps open in their face.", Fd(5, 4, 16),
                                   new Vector2(0.9f, 0.1f), new Vector2(1.3f, 1.1f), H(3, 8f, 40f), 2.5f);
                var vine = b.Strike("VineLash", "Vine Lash", "Combo finisher (SPECIAL): a vine whips out from the parasol tip.",
                                    Fd(7, 4, 18), new Vector2(1.4f, 0.1f), new Vector2(2.2f, 0.6f), H(3, 9f, 30f), 1f);
                var twirl = b.Strike("ParasolTwirl", "Parasol Twirl", "Second hit. Attack for Bloom Burst, SPECIAL for Vine Lash.",
                                     Fd(3, 4, 11, 8), new Vector2(0.8f, 0.2f), new Vector2(1.1f, 0.9f), L(1, 3.5f, 30f), 2f);
                var tap = b.Strike("ParasolTap", "Parasol Tap", "A quick poke with the closed parasol.", Fd(3, 3, 9, 8),
                                   new Vector2(0.85f, 0.05f), new Vector2(1.1f, 0.5f), L(1, 3f, 15f), 2f);
                Link(tap, twirl); Link(twirl, fin, vine);
                var up = b.Strike("SproutUp", "Sprout Up", "Up + Attack: a sprout bursts from the ground.", Fd(5, 4, 13),
                                  new Vector2(0.5f, 0.9f), new Vector2(1f, 1.3f), H(2, 7.5f, 85f), 0f);
                var air = b.Air("PetalSpin", "Petal Spin", "Air Attack: spins with the parasol open.", Fd(3, 8, 9), Vector2.zero, 1.05f, L(1, 4f, 40f));
                var leaf = b.Shot("RazorLeaf", "Razor Leaf", "Special: a spinning leaf.", Fd(6, 1, 14), L(1, 4.5f, 20f), 14f, 0.6f, 0.32f, C(150, 230, 140));
                var glide = b.Strike("BreezeGlide", "Breeze Glide", "Side + Special: drifts forward under the open parasol.",
                                     Fd(5, 8, 16), new Vector2(0.8f, 0.1f), new Vector2(1.2f, 0.9f), L(2, 6f, 35f, 14), 10f);
                var thorn = b.Trap("ThornPatch", "Thorn Patch", "Down + Special: plants a thorny bush that snags whoever steps in.",
                                   L(1, 5f, 75f, 36), C(90, 170, 80));
                var drop = b.Strike("SeedBomb", "Seed Bomb", "Air Special: drops a burst of seeds straight down.", Fd(6, 6, 14),
                                    new Vector2(0.2f, -0.7f), new Vector2(1.2f, 0.9f), H(2, 7f, 70f), 0f,
                                    m => { m.rootedOnGround = false; m.endsOnLanding = true; });
                return Set("JuniBloom", "Juni", tap, up, air, leaf, glide, thorn, drop, null);
            }, k => { if (k is LumaKitDefinition l) { l.hopUpSpeed = 14f; l.hopForwardSpeed = 6f; } });

        static CharacterDefinition MarcoBrio() => Fighter("MarcoBrio", "marco_brio", "Marco Brio",
            "Street painter. Every fight is a masterpiece.",
            "Marco paints the murals on the Brasslight Quarter's walls, usually without asking. His giant brush leaves streaks of " +
            "colour wherever it lands, and a good splash of paint is surprisingly hard to see through.",
            new Look { build = B.Normal, head = Hd.Cap, extra = Ex.Scarf, skin = C(214, 160, 120), hair = C(60, 40, 30),
                       top = C(250, 246, 236), accent = C(220, 60, 70), pants = C(60, 90, 160), shoes = C(70, 50, 40) },
            1.0f, Move(7.8f, 3.1f, 0.37f), 5, 3, 0f, Kit.Dash, "Brush Dash", "Splash Down", () =>
            {
                var b = new Book("MarcoBrio", "Marco");
                var fin = b.Strike("BoldStroke", "Bold Stroke", "Combo ender: one big sweep of the brush.", Fd(5, 4, 16),
                                   new Vector2(1f, 0.1f), new Vector2(1.6f, 1f), H(3, 8.5f, 35f), 2.5f);
                var mural = b.Ring("MuralBlast", "Mural Blast", "Combo finisher (SPECIAL): a burst of every colour at once.",
                                   Fd(8, 4, 18), new Vector2(0.6f, 0.2f), 1.6f, H(3, 8.5f, 55f));
                var dab = b.Strike("DoubleDab", "Double Dab", "Second hit. Attack for Bold Stroke, SPECIAL for Mural Blast.",
                                   Fd(3, 3, 11, 8), new Vector2(0.9f, 0.1f), new Vector2(1.2f, 0.7f), L(1, 3.5f, 20f), 2.5f);
                var flick = b.Strike("BrushFlick", "Brush Flick", "A flick of the brush. Keep pressing Attack.", Fd(3, 3, 10, 8),
                                     new Vector2(0.9f, 0.05f), new Vector2(1.2f, 0.6f), L(1, 3f, 15f), 2f);
                Link(flick, dab); Link(dab, fin, mural);
                var up = b.Strike("RisingArc", "Rising Arc", "Up + Attack: paints an arc overhead.", Fd(4, 5, 13),
                                  new Vector2(0.3f, 1.1f), new Vector2(1.6f, 0.9f), H(2, 7.5f, 82f), 0f);
                var air = b.Air("SpinStroke", "Spin Stroke", "Air Attack: a full circle of paint.", Fd(4, 7, 10), Vector2.zero, 1.15f, L(1, 4.5f, 35f));
                var splat = b.Shot("PaintSplat", "Paint Splat", "Special: flings a blob of paint.", Fd(7, 1, 15), L(2, 5f, 25f), 12f, 0.6f, 0.4f, C(90, 170, 255));
                var streak = b.Strike("ColourStreak", "Colour Streak", "Side + Special: dashes through, leaving a streak of paint.",
                                      Fd(5, 7, 16), new Vector2(0.8f, 0f), new Vector2(1.3f, 0.8f), H(2, 7f, 30f), 12f);
                var puddle = b.Trap("SlickCoat", "Slick Coat", "Down + Special: a wet puddle of paint that trips foes.",
                                    L(1, 4f, 80f, 38), C(220, 60, 70));
                var imp = b.Impact("SplashDown", "Splash Down", 1.6f, H(2, 7.5f, 60f));
                return Set("MarcoBrio", "Marco", flick, up, air, splat, streak, puddle, null, imp);
            }, k => { if (k is NovaKitDefinition n) n.airDashSpeed = 15f; });

        static CharacterDefinition TamsinReed() => Fighter("TamsinReed", "tamsin_reed", "Tamsin Reed",
            "Librarian who reads spells out loud.",
            "Tamsin runs the night shift at the Tickworks archive and has read every book in it, twice. Some of them read her " +
            "back. Her storybound tome fires pages like darts and conjures shapes straight out of the stories.",
            new Look { build = B.Slim, head = Hd.Hair, extra = Ex.None, skin = C(244, 200, 170), hair = C(120, 80, 60),
                       top = C(250, 200, 210), accent = C(150, 90, 160), pants = C(100, 70, 60), shoes = C(110, 70, 50) },
            0.8f, Move(7.2f, 3.2f, 0.37f), 4, 3, 0f, Kit.Dash, "Page Turn", null, () =>
            {
                var b = new Book("TamsinReed", "Tamsin");
                var fin = b.Strike("ChapterClose", "Chapter Close", "Combo ender: slams the book shut on them.", Fd(5, 3, 15),
                                   new Vector2(0.8f, 0.15f), new Vector2(1.1f, 1f), H(3, 8f, 40f), 2f);
                var tale = b.Shot("TallTale", "Tall Tale", "Combo finisher (SPECIAL): a huge glowing letter flies out.", Fd(7, 1, 17),
                                  H(3, 9f, 25f), 12f, 0.45f, 0.55f, C(220, 180, 255));
                var swipe = b.Strike("SpineSwipe", "Spine Swipe", "Second hit. Attack for Chapter Close, SPECIAL for Tall Tale.",
                                     Fd(3, 3, 11, 8), new Vector2(0.75f, 0.1f), new Vector2(1f, 0.7f), L(1, 3.5f, 20f), 2.5f);
                var bop = b.Strike("BookBop", "Book Bop", "A bop with the tome. Keep pressing Attack.", Fd(3, 3, 9, 8),
                                   new Vector2(0.7f, 0.1f), new Vector2(0.9f, 0.6f), L(1, 3f, 15f), 2f);
                Link(bop, swipe); Link(swipe, fin, tale);
                var up = b.Ring("Footnote", "Footnote", "Up + Attack: a ring of glowing words above her.", Fd(5, 4, 13),
                                new Vector2(0f, 1.2f), 1.1f, H(2, 7f, 85f));
                var air = b.Air("PageStorm", "Page Storm", "Air Attack: pages whirl around her.", Fd(3, 8, 9), Vector2.zero, 1.1f, L(1, 4f, 35f));
                var dart = b.Shot("PaperDart", "Paper Dart", "Special: a folded page, fast and straight.", Fd(5, 1, 12), L(1, 3.5f, 10f), 19f, 0.55f, 0.24f, C(240, 236, 220));
                var bookmark = b.Strike("Bookmark", "Bookmark", "Side + Special: blinks forward to her bookmark.", Fd(5, 6, 16),
                                        new Vector2(0.8f, 0f), new Vector2(1.2f, 0.9f), H(2, 7f, 30f), 13f);
                var spellbound = b.Trap("Spellbound", "Spellbound", "Down + Special: a glowing rune that holds whoever steps on it.",
                                        L(1, 4f, 80f, 44), C(170, 120, 230));
                var quill = b.Strike("QuillDive", "Quill Dive", "Air Special: dives nib-first.", Fd(5, 6, 14),
                                     new Vector2(0.6f, -0.4f), new Vector2(1f, 1f), H(2, 7.5f, 40f), 0f,
                                     m => { m.rootedOnGround = false; m.endsOnLanding = true; });
                return Set("TamsinReed", "Tamsin", bop, up, air, dart, bookmark, spellbound, quill, null);
            }, k => { if (k is NovaKitDefinition n) n.airDashSpeed = 16f; });

        static CharacterDefinition DexOrtega() => Fighter("DexOrtega", "dex_ortega", "Dex Ortega",
            "Rock guitarist on roller sneakers.",
            "Dex plays the loudest band in the Isles and skates to every gig. His star-shaped guitar doubles as a club, and when " +
            "he hits the big chord the shockwave clears the whole stage.",
            new Look { build = B.Normal, head = Hd.Mohawk, extra = Ex.None, skin = C(170, 110, 80), hair = C(210, 70, 170),
                       top = C(50, 50, 60), accent = C(255, 230, 90), pants = C(70, 70, 120), shoes = C(240, 240, 240) },
            0.95f, Move(8.6f, 3.2f, 0.36f, coast: 9f, skid: 38f), 5, 3, 0f, Kit.Boost, "Kick Turn", "Stage Dive", () =>
            {
                var b = new Book("DexOrtega", "Dex");
                var fin = b.Strike("Windmill", "Windmill", "Combo ender: a full windmill swing of the guitar.", Fd(5, 4, 16),
                                   new Vector2(0.9f, 0.2f), new Vector2(1.4f, 1.2f), H(3, 8.5f, 45f), 2.5f);
                var chord = b.Ring("PowerChord", "Power Chord", "Combo finisher (SPECIAL): one huge chord, all around him.",
                                   Fd(8, 3, 18), Vector2.zero, 1.8f, H(3, 8.5f, 60f));
                var riff = b.Strike("Riff", "Riff", "Second hit. Attack for Windmill, SPECIAL for Power Chord.",
                                    Fd(3, 3, 11, 8), new Vector2(0.85f, 0.1f), new Vector2(1.1f, 0.7f), L(1, 3.5f, 20f), 2.5f);
                var strum = b.Strike("Strum", "Strum", "A swing of the guitar neck. Keep pressing Attack.", Fd(3, 3, 9, 8),
                                     new Vector2(0.85f, 0.05f), new Vector2(1.1f, 0.6f), L(1, 3f, 15f), 2.5f);
                Link(strum, riff); Link(riff, fin, chord);
                var up = b.Strike("HighNote", "High Note", "Up + Attack: the guitar goes up like a torch.", Fd(4, 4, 12),
                                  new Vector2(0.2f, 1.2f), new Vector2(1.1f, 1.1f), H(2, 7.5f, 85f), 0f);
                var air = b.Air("Shred", "Shred", "Air Attack: a spinning solo.", Fd(3, 8, 9), Vector2.zero, 1.05f, L(1, 4.5f, 35f));
                var wave = b.Shot("SoundWave", "Sound Wave", "Special: a ring of sound that rolls across the floor.", Fd(6, 1, 14),
                                  L(1, 5f, 30f), 14f, 0.55f, 0.4f, C(255, 120, 200));
                var slide = b.Strike("KneeSlide", "Knee Slide", "Side + Special: a rock-star knee slide under attacks.", Fd(4, 9, 16),
                                     new Vector2(0.8f, -0.4f), new Vector2(1.3f, 0.6f), L(2, 6.5f, 70f, 14), 12f);
                var amp = b.Ring("AmpBlast", "Amp Blast", "Down + Special: stomps the pedal, the floor shakes.", Fd(9, 3, 18),
                                 new Vector2(0f, -0.4f), 1.8f, H(2, 7f, 80f));
                var imp = b.Impact("StageDive", "Stage Dive", 1.6f, H(2, 7.5f, 60f));
                return Set("DexOrtega", "Dex", strum, up, air, wave, slide, amp, null, imp);
            });

        static CharacterDefinition PepperQuinn() => Fighter("PepperQuinn", "pepper_quinn", "Pepper Quinn",
            "Food-truck cook. Handles the heat.",
            "Pepper runs the busiest food truck in the Night Market and has never burned a single order. Her cast-iron skillet " +
            "blocks anything thrown at her, and anything she throws back comes in hot.",
            new Look { build = B.Bulky, head = Hd.Band, extra = Ex.None, skin = C(120, 78, 52), hair = C(30, 24, 22),
                       top = C(255, 250, 240), accent = C(230, 70, 60), pants = C(60, 70, 90), shoes = C(50, 40, 40) },
            1.4f, Move(6.6f, 3.0f, 0.39f), 6, 4, 0.1f, Kit.Guard, "Skillet Shield", "Belly Flop Special", () =>
            {
                var b = new Book("PepperQuinn", "Pepper");
                var fin = b.Strike("CastIronSlam", "Cast-Iron Slam", "Combo ender: the skillet comes down hard.", Fd(6, 4, 17),
                                   new Vector2(0.9f, 0.1f), new Vector2(1.3f, 1.1f), H(4, 9f, 45f, 17, 8), 2f);
                var flambe = b.Ring("Flambe", "Flambé", "Combo finisher (SPECIAL): a burst of flame from the pan.", Fd(8, 4, 18),
                                    new Vector2(0.9f, 0.2f), 1.4f, H(3, 8.5f, 55f));
                var flip = b.Strike("PancakeFlip", "Pancake Flip", "Second hit. Attack for Cast-Iron Slam, SPECIAL for Flambé.",
                                    Fd(4, 4, 12, 8), new Vector2(0.85f, 0.3f), new Vector2(1.1f, 0.9f), L(2, 4.5f, 60f), 2f);
                var bonk = b.Strike("PanBonk", "Pan Bonk", "A heavy pan bonk. Keep pressing Attack.", Fd(4, 3, 11, 8),
                                    new Vector2(0.85f, 0.1f), new Vector2(1.1f, 0.8f), L(2, 4f, 20f), 1.5f);
                Link(bonk, flip); Link(flip, fin, flambe);
                var up = b.Strike("Toss", "Toss", "Up + Attack: tosses the foe like a pancake.", Fd(5, 4, 15),
                                  new Vector2(0.4f, 1f), new Vector2(1.3f, 1.2f), H(3, 8f, 85f), 0f);
                var air = b.Air("SkilletSpin", "Skillet Spin", "Air Attack: the pan, all the way round.", Fd(4, 7, 11), Vector2.zero, 1.15f, L(2, 5f, 35f));
                var meatball = b.Shot("HotMeatball", "Hot Meatball", "Special: lobs a sizzling meatball.", Fd(9, 1, 18),
                                      H(2, 7.5f, 30f), 9f, 0.8f, 0.45f, C(255, 170, 80));
                var rush = b.Strike("OrderUp", "Order Up!", "Side + Special: an armored rush, skillet first.", Fd(8, 9, 20),
                                    new Vector2(0.9f, 0f), new Vector2(1.3f, 1.1f), H(3, 8.5f, 35f), 9f,
                                    m => { m.superArmor = true; m.armorStartFrame = 0; });
                var grease = b.Trap("GreaseSpill", "Grease Spill", "Down + Special: a slick of hot grease.", L(1, 5f, 75f, 36), C(230, 200, 90));
                var imp = b.Impact("BellyFlopSpecial", "Belly Flop Special", 2f, H(3, 8.5f, 55f));
                return Set("PepperQuinn", "Pepper", bonk, up, air, meatball, rush, grease, null, imp);
            });

        static CharacterDefinition BoTidewell() => Fighter("BoTidewell", "bo_tidewell", "Bo Tidewell",
            "Lighthouse keeper with an anchor on a chain.",
            "Bo keeps the lighthouse at the edge of the Sky Gardens, where the cloud tides come in every evening. He swings a ship's " +
            "anchor like it weighs nothing, and once he plants his feet nothing moves him.",
            new Look { build = B.Bulky, head = Hd.Cap, extra = Ex.None, skin = C(232, 186, 150), hair = C(200, 120, 60),
                       top = C(250, 200, 50), accent = C(40, 80, 140), pants = C(40, 60, 100), shoes = C(70, 60, 50) },
            1.5f, Move(6.4f, 2.9f, 0.4f), 7, 4, 0.15f, Kit.Guard, "Brace", "Anchor Drop", () =>
            {
                var b = new Book("BoTidewell", "Bo");
                var fin = b.Strike("AnchorSwing", "Anchor Swing", "Combo ender: a wide, heavy swing.", Fd(7, 4, 18),
                                   new Vector2(1f, 0.1f), new Vector2(1.7f, 1.1f), H(4, 9f, 40f, 17, 8), 2f);
                var tide = b.Ring("RipTide", "Rip Tide", "Combo finisher (SPECIAL): a whirlpool pulls everything in, then out.",
                                  Fd(9, 5, 20), new Vector2(0.4f, 0.1f), 1.8f, H(3, 8.5f, 70f));
                var hook = b.Strike("ChainHook", "Chain Hook", "Second hit. Attack for Anchor Swing, SPECIAL for Rip Tide.",
                                    Fd(4, 4, 13, 8), new Vector2(1.1f, 0.1f), new Vector2(1.5f, 0.7f), L(2, 4.5f, 20f), 1.5f);
                var shove = b.Strike("ShoulderShove", "Shoulder Shove", "A big shove. Keep pressing Attack.", Fd(4, 3, 11, 8),
                                     new Vector2(0.75f, 0.1f), new Vector2(1f, 0.9f), L(2, 4f, 20f), 2f);
                Link(shove, hook); Link(hook, fin, tide);
                var up = b.Strike("Haul", "Haul", "Up + Attack: hauls the anchor up overhead.", Fd(6, 4, 16),
                                  new Vector2(0.3f, 1.1f), new Vector2(1.5f, 1.2f), H(3, 8f, 85f), 0f);
                var air = b.Air("Plummet", "Plummet", "Air Attack: the anchor swings under him.", Fd(5, 6, 12), new Vector2(0f, -0.3f), 1.2f, H(2, 6.5f, 40f));
                var spray = b.Shot("SeaSpray", "Sea Spray", "Special: a burst of sea water.", Fd(8, 1, 17), L(2, 6f, 25f), 11f, 0.55f, 0.5f, C(120, 210, 255));
                var reel = b.Strike("ReelIn", "Reel In", "Side + Special: throws the anchor out and drags the foe back.",
                                    Fd(8, 6, 20), new Vector2(1.6f, 0.1f), new Vector2(2.2f, 0.7f), H(2, 7f, 160f), 0f);
                var wave = b.Ring("Breaker", "Breaker", "Down + Special: slams the anchor and a wave rolls out.", Fd(11, 3, 22),
                                  new Vector2(0f, -0.4f), 2.1f, H(2, 7f, 80f));
                var imp = b.Impact("AnchorDrop", "Anchor Drop", 2.1f, H(3, 9f, 55f));
                return Set("BoTidewell", "Bo", shove, up, air, spray, reel, wave, null, imp);
            });

        static CharacterDefinition ZuriStarling() => Fighter("ZuriStarling", "zuri_starling", "Zuri Starling",
            "Young stargazer with a moon on a string.",
            "Zuri maps the stars from the observatory dome above the Frostgear Peaks. She is the smallest fighter in the Isles and " +
            "the hardest to pin down, and her crescent yo-yo hits from much further away than it looks.",
            new Look { build = B.Small, head = Hd.Hair, extra = Ex.Cape, skin = C(110, 70, 48), hair = C(40, 28, 34),
                       top = C(70, 60, 150), accent = C(255, 220, 110), pants = C(50, 44, 110), shoes = C(255, 220, 110) },
            0.65f, Move(8.2f, 3.4f, 0.35f, airJumps: 1), 4, 3, 0f, Kit.Dash, "Comet Step", "Meteor Drop", () =>
            {
                var b = new Book("ZuriStarling", "Zuri");
                var fin = b.Strike("AroundTheMoon", "Around the Moon", "Combo ender: the yo-yo loops all the way round.", Fd(4, 5, 15),
                                   new Vector2(0.9f, 0.3f), new Vector2(1.6f, 1.3f), H(2, 8f, 50f), 2f);
                var nova = b.Shot("Supernova", "Supernova", "Combo finisher (SPECIAL): a bright star bursts out of the yo-yo.",
                                  Fd(6, 1, 16), H(3, 8.5f, 30f), 15f, 0.3f, 0.55f, C(255, 240, 160));
                var walk = b.Strike("WalkTheDog", "Walk the Dog", "Second hit. Attack for Around the Moon, SPECIAL for Supernova.",
                                    Fd(3, 4, 11, 8), new Vector2(1.1f, -0.3f), new Vector2(1.6f, 0.5f), L(1, 3.5f, 25f), 2f);
                var toss = b.Strike("YoYoToss", "Yo-Yo Toss", "A long-reach toss. Keep pressing Attack.", Fd(3, 3, 10, 8),
                                    new Vector2(1.2f, 0.1f), new Vector2(1.8f, 0.45f), L(1, 3f, 15f), 1f);
                Link(toss, walk); Link(walk, fin, nova);
                var up = b.Strike("ShootingStar", "Shooting Star", "Up + Attack: the yo-yo shoots straight up.", Fd(3, 4, 12),
                                  new Vector2(0.2f, 1.5f), new Vector2(0.7f, 1.8f), H(2, 7f, 88f), 0f);
                var air = b.Air("Orbit", "Orbit", "Air Attack: the yo-yo orbits around her.", Fd(3, 9, 9), Vector2.zero, 1.25f, L(1, 4f, 40f));
                var star = b.Shot("Stardust", "Stardust", "Special: a twinkling star.", Fd(5, 1, 12), L(1, 3.5f, 15f), 17f, 0.55f, 0.28f, C(255, 240, 160));
                var swing = b.Strike("Moonswing", "Moonswing", "Side + Special: swings forward on the yo-yo string.", Fd(5, 7, 16),
                                     new Vector2(0.8f, 0.2f), new Vector2(1.2f, 0.9f), L(2, 6f, 35f, 14), 12f);
                var grav = b.Trap("GravityWell", "Gravity Well", "Down + Special: a tiny black star that holds foes in place.",
                                  L(1, 4f, 85f, 42), C(110, 90, 200));
                var imp = b.Impact("MeteorDrop", "Meteor Drop", 1.4f, H(2, 7f, 60f));
                return Set("ZuriStarling", "Zuri", toss, up, air, star, swing, grav, null, imp);
            }, k => { if (k is NovaKitDefinition n) n.airDashSpeed = 17f; });

        static CharacterDefinition OttoBrass() => Fighter("OttoBrass", "otto_brass", "Otto Brass",
            "Old clockmaker who winds up his punches.",
            "Otto built half the clocks in the Tickworks and knows the other half personally. He's slower than he used to be, " +
            "but he winds his giant key between swings, and a fully wound Otto hits like a falling clock tower.",
            new Look { build = B.Normal, head = Hd.Bald, extra = Ex.None, skin = C(240, 200, 170), hair = C(230, 230, 236),
                       top = C(120, 80, 60), accent = C(220, 170, 70), pants = C(80, 70, 70), shoes = C(60, 44, 36) },
            1.15f, Move(6.8f, 3.0f, 0.39f), 6, 3, 0.1f, Kit.Hop, "Spring Step", null, () =>
            {
                var b = new Book("OttoBrass", "Otto");
                var fin = b.Strike("Chime", "Chime", "Combo ender: rings the foe like a bell.", Fd(5, 4, 16),
                                   new Vector2(0.9f, 0.1f), new Vector2(1.3f, 1f), H(3, 8.5f, 40f), 2f);
                var wound = b.Strike("FullyWound", "Fully Wound", "Combo finisher (SPECIAL): unleashes every turn of the key at once.",
                                     Fd(10, 4, 22), new Vector2(0.9f, 0.1f), new Vector2(1.4f, 1.1f), H(5, 10.5f, 35f, 18, 9), 4f,
                                     m => { m.shakeAmplitude = 0.3f; m.haptic = HapticStrength.Heavy; });
                var tock = b.Strike("Tock", "Tock", "Second hit. Attack for Chime, SPECIAL for Fully Wound.",
                                    Fd(4, 3, 12, 8), new Vector2(0.85f, 0.1f), new Vector2(1.1f, 0.7f), L(1, 4f, 20f), 2f);
                var tick = b.Strike("Tick", "Tick", "A sharp tap with the key. Keep pressing Attack.", Fd(3, 3, 10, 8),
                                    new Vector2(0.85f, 0.05f), new Vector2(1.1f, 0.55f), L(1, 3.5f, 15f), 2f);
                Link(tick, tock); Link(tock, fin, wound);
                var up = b.Strike("Mainspring", "Mainspring", "Up + Attack: a spring pops out of the key.", Fd(5, 4, 14),
                                  new Vector2(0.3f, 1.1f), new Vector2(1f, 1.3f), H(2, 8f, 88f), 0f);
                var air = b.Air("Gearwheel", "Gearwheel", "Air Attack: spins like a gear.", Fd(4, 7, 10), Vector2.zero, 1.1f, L(1, 4.5f, 35f));
                var cog = b.Shot("CogToss", "Cog Toss", "Special: throws a spinning cog.", Fd(7, 1, 15), L(2, 5f, 20f), 13f, 0.65f, 0.36f, C(255, 210, 120));
                var clockwork = b.Strike("ClockworkCharge", "Clockwork Charge", "Side + Special: winds up and charges, armored.",
                                         Fd(9, 7, 20), new Vector2(0.9f, 0.1f), new Vector2(1.3f, 1f), H(3, 8.5f, 35f), 9f,
                                         m => { m.superArmor = true; m.armorStartFrame = 2; });
                var alarm = b.Ring("AlarmBell", "Alarm Bell", "Down + Special: rings an alarm that knocks everyone back.", Fd(9, 4, 20),
                                   Vector2.zero, 1.8f, H(2, 7.5f, 70f));
                var cuckoo = b.Strike("CuckooDrop", "Cuckoo Drop", "Air Special: drops like a cuckoo out of a clock.", Fd(6, 6, 15),
                                      new Vector2(0.3f, -0.6f), new Vector2(1.1f, 0.9f), H(2, 7.5f, 60f), 0f,
                                      m => { m.rootedOnGround = false; m.endsOnLanding = true; });
                return Set("OttoBrass", "Otto", tick, up, air, cog, clockwork, alarm, cuckoo, null);
            }, k => { if (k is LumaKitDefinition l) { l.hopUpSpeed = 12.5f; l.hopForwardSpeed = 5f; } });
    }
}
