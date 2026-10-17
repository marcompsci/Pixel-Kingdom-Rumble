using System;
using System.Collections.Generic;

namespace PKR.Core
{
    // =====================================================================================================
    //  ZOMBIE LAND (all original). The quarantined city of New Hollow, seen from above.
    //
    //  The story: a meteor fragment, the Graystone, was brought to Larkin Labs on College Hill. The night
    //  guard, Silas Vane, touched it first and became the First Turned. Every zombie in the city is a piece
    //  of his will: they wander, carry rot to the nests, call the horde and guard his lair. Somewhere in the
    //  city he waits, growing. Ten missions across the city each recover a Graystone shard (the lair is sealed
    //  until all ten are carried in) and a clue. The clues tell which of the four relic weapons can hurt him
    //  and which of six places is his lair. Beat him and every zombie in Zombie Land falls with him.
    // =====================================================================================================

    /// <summary>What the city is made of: who works for the First Turned and how.</summary>
    public enum ZLFoe
    {
        Shambler,   // the workforce: wanders the streets, carries rot back to the nests
        Runner,     // scouts: fast, call others when they see you
        Screamer,   // living alarms: a scream pulls a horde to where you stand
        Bloater,    // burst into a rot cloud when they die
        Spitter,    // lob acid from range
        Brute,      // nest guards: slow, huge, hit hard
        Hound,      // blood hounds: his hunting pack, loosed when he senses you (after 5 minutes out in the open)
        Herald,     // district lieutenants (mission targets): elite, armored
        FirstTurned, // the master
        Riot,       // riot police who turned with their shields up: bullets glance off the front, flank them or blow them up
        Leaper,     // wiry, low to the ground: crouches, then springs across the gap at you
        Splitter,   // two bodies stitched into one coat: kill it and both halves keep coming
        SplitterHalf, // one of those halves: small, quick, fragile
        Burrower    // tunnels under the street (a moving mound, untouchable), then bursts up beside you
    }

    public enum ZLMissionType { Signal, Retrieve, Rescue, PurgeNest, PowerUp, HuntHerald, Delivery, HoldOut }

    public enum ZLVehicleKind { Car, Truck, Bike, Motorcycle }

    /// <summary>The four relic weapons. Only one of them can hurt the First Turned (it changes every new game).</summary>
    public enum ZLRelic { Sunflare, Frostbite, Arc, Cinder }

    public class ZLDistrict
    {
        public string id, name, blurb;
        /// <summary>1..5: zombie density and toughness (deeper districts are worse, like the regions of an outbreak).</summary>
        public int threat;
    }

    public class ZLMissionSpec
    {
        public string id, name, district, giver;
        public ZLMissionType type;
        /// <summary>Required missions carry a Graystone shard and a clue. Bonus missions carry a reward instead.</summary>
        public bool required;
        public string briefing, done;
        /// <summary>Bonus reward id (ZLReward), empty for required missions.</summary>
        public string reward = "";
        /// <summary>Type numbers: seconds to hold or drive, items to find, generators to start.</summary>
        public float seconds;
        public int count;
        /// <summary>Missions whose lead you learn when this one is done (the trail across the city).</summary>
        public string[] reveals = new string[0];
        /// <summary>1 = New Hollow (Graystone shards), 2 = Blackwater Isle (sea-glass sigils).</summary>
        public int chapter = 1;
    }

    public class ZLLair
    {
        public string id, name, district;
        /// <summary>A clue that rules this place out (shown when it's NOT the lair).</summary>
        public string notHere;
        /// <summary>A clue that points at this place (shown when it IS the lair).</summary>
        public string hint;
    }

    public class ZLRelicSpec
    {
        public ZLRelic id;
        public string name, element, blurb;
        public string notThis;   // rules it out
        public string hint;      // points at it
        public byte r, g, b;
    }

    public class ZLGun
    {
        public string id, name;
        public int damage, magazine, maxReserve, pellets;
        public float shotsPerSecond, reloadSeconds, speed, range, spreadDegrees;
        public bool automatic;
        /// <summary>Loudness of a shot (0..1): loud guns draw the dead from further away.</summary>
        public float noise;
        public byte r, g, b;
        public string shot;
        public ZLRelic relic;
        public bool isRelic;
        /// <summary>Box-only wonder weapon (never lying around the city).</summary>
        public bool wonder;
        /// <summary>Freezes what it hits for this many seconds (the Rime Cannon); frozen zombies crawl and take extra damage.</summary>
        public float freeze;
        /// <summary>Pulls the dead toward where it lands for a moment before it bursts (the Singularity Cannon).</summary>
        public float pull;
        /// <summary>The Kindler: the first of the dead it hits fights for you for this many seconds.</summary>
        public float kindle;
        /// <summary>Never in the mystery box: only a boss drops it.</summary>
        public bool bossDrop;
        /// <summary>Hollow-forged at the upgrade forge (a copy made by ZLShop.Effective).</summary>
        public bool forged;

        public ZLGun Clone() => (ZLGun)MemberwiseClone();
    }

    public class ZLVehicleSpec
    {
        public ZLVehicleKind kind;
        public string name;
        public float topSpeed, accel, turn, grip, mass;
        public int health;
        /// <summary>How loud it is (bikes are silent).</summary>
        public float noise;
        /// <summary>Damage to a zombie per unit of speed on impact.</summary>
        public float ram;
    }

    public static class ZLCatalog
    {
        public const string CityName = "New Hollow";
        public const string BossName = "THE FIRST TURNED";
        public const string BossRealName = "Silas Vane";
        public const int ShardsNeeded = 10;

        public static readonly ZLDistrict[] Districts =
        {
            new ZLDistrict { id = "suburbs", name = "Maple Row", threat = 1, blurb = "Quiet streets and picket fences. Was quiet." },
            new ZLDistrict { id = "oldtown", name = "Old Quarter", threat = 2, blurb = "Cobbled lanes around the cathedral." },
            new ZLDistrict { id = "park", name = "Greenway Park", threat = 2, blurb = "The pond, the rose garden and something growing in it." },
            new ZLDistrict { id = "downtown", name = "Downtown", threat = 3, blurb = "Towers, the subway and the radio mast." },
            new ZLDistrict { id = "harbor", name = "Saltmouth Harbor", threat = 3, blurb = "Cranes, containers and a freighter that never left." },
            new ZLDistrict { id = "university", name = "College Hill", threat = 4, blurb = "Larkin Labs and the stadium. Where it started." },
            new ZLDistrict { id = "industrial", name = "Ironside", threat = 4, blurb = "Warehouses, the rail depot and the waterworks." },
            new ZLDistrict { id = "isle", name = "Blackwater Isle", threat = 5, blurb = "The fishing village, the cannery and the old prison. Chapter 2." },
            new ZLDistrict { id = "labs", name = "The Larkin Sublevel", threat = 5, blurb = "Sealed research floors under Larkin Labs. Chapter 3." },
        };

        public static ZLDistrict District(string id)
        {
            foreach (var d in Districts) if (d.id == id) return d;
            return Districts[0];
        }

        // ---- the six possible lairs --------------------------------------------------------------------

        public static readonly ZLLair[] Lairs =
        {
            new ZLLair { id = "lair_cathedral", name = "St. Brigid's Cathedral", district = "oldtown",
                notHere = "The bell ringer swears nothing has climbed the cathedral tower. It's not there.",
                hint = "Something ate the candles in St. Brigid's. Only the bell still rings, all by itself." },
            new ZLLair { id = "lair_subway", name = "Union Subway Depot", district = "downtown",
                notHere = "A transit worker walked every subway tunnel. Empty. It's not underground downtown.",
                hint = "The trains in the Union Depot keep starting on their own. Nobody is driving them." },
            new ZLLair { id = "lair_freighter", name = "The Freighter Marigold", district = "harbor",
                notHere = "The harbor master sealed the Marigold before the outbreak. Nothing got aboard.",
                hint = "Fishermen hear knocking from inside the Marigold's cold hold. Slow. Patient." },
            new ZLLair { id = "lair_stadium", name = "Hollow Field Stadium", district = "university",
                notHere = "Hollow Field is wide open to the sky. It hates open sky. Not there.",
                hint = "The stadium lights still burn at night, and the crowd still cheers. There is no crowd." },
            new ZLLair { id = "lair_waterworks", name = "Ironside Waterworks", district = "industrial",
                notHere = "The waterworks pumps are clean. Rot would have reached every tap in the city.",
                hint = "Tap water in Ironside comes out gray. Something is sitting in the filter beds." },
            new ZLLair { id = "lair_labs", name = "Larkin Labs Basement", district = "university",
                notHere = "Larkin Labs burned on day one. Whatever it is, it left and never came back.",
                hint = "He went back to where he first touched the stone. The lab's basement door is warm." },
        };

        public static ZLLair Lair(string id)
        {
            foreach (var l in Lairs) if (l.id == id) return l;
            return null;
        }

        // ---- the four relic weapons ----------------------------------------------------------------------

        public static readonly ZLRelicSpec[] Relics =
        {
            new ZLRelicSpec { id = ZLRelic.Sunflare, name = "SUNFLARE LANCE", element = "light", r = 255, g = 230, b = 120,
                blurb = "A searchlight lens on a spear shaft. Fires a beam of bottled daylight.",
                notThis = "A survivor blinded him with a flare. He didn't even flinch. Light won't do it.",
                hint = "He only walks at night, and he hides from the dawn." },
            new ZLRelicSpec { id = ZLRelic.Frostbite, name = "FROSTBITE CANNON", element = "cold", r = 150, g = 220, b = 255,
                blurb = "A fire-suppression tank run backwards. Shoots freezing slugs.",
                notThis = "He walked through the ice-plant freezers like a summer breeze. Cold won't stop him.",
                hint = "The Graystone fell burning hot. The only thing it ever cracked from was ice." },
            new ZLRelicSpec { id = ZLRelic.Arc, name = "ARC CASTER", element = "lightning", r = 170, g = 150, b = 255,
                blurb = "A substation coil on a rifle stock. Throws chain lightning.",
                notThis = "He stood under a falling power line and laughed. Lightning won't do it.",
                hint = "Every time a transformer blows, the dead nearby drop for a second. He feels it too." },
            new ZLRelicSpec { id = ZLRelic.Cinder, name = "CINDER BELLOWS", element = "fire", r = 255, g = 120, b = 50,
                blurb = "A forge bellows with a pilot light. Breathes a cone of fire.",
                notThis = "The refinery fire burned for a week around him. Fire won't do it.",
                hint = "His own nest burned once by accident. He screamed loud enough to crack windows." },
        };

        public static ZLRelicSpec Relic(ZLRelic id) => Relics[(int)id];

        // ---- guns ----------------------------------------------------------------------------------------

        /// <summary>You start with this and 25 rounds, nothing more.</summary>
        public static readonly ZLGun Pistol = new ZLGun
        {
            id = "service_pistol", name = "SERVICE PISTOL", damage = 4, magazine = 12, maxReserve = 96, pellets = 1,
            shotsPerSecond = 3.2f, reloadSeconds = 1.1f, speed = 22f, range = 10f, noise = 0.35f, r = 255, g = 235, b = 160, shot = "ball"
        };
        public const int StartingRounds = 25;

        /// <summary>The mystery box's wonder weapon: twin bolts of storm light that chew through crowds.</summary>
        public static readonly ZLGun StormCoil = new ZLGun
        {
            id = "storm_coil", name = "STORM COIL", damage = 11, magazine = 30, maxReserve = 150, pellets = 2, spreadDegrees = 9f,
            shotsPerSecond = 6f, reloadSeconds = 1.7f, speed = 26f, range = 12f, automatic = true, noise = 0.5f, r = 140, g = 220, b = 255,
            shot = "spark", wonder = true
        };

        /// <summary>The second wonder weapon: a frost bolt that bursts into rime, freezing everything near where it lands.</summary>
        public static readonly ZLGun RimeCannon = new ZLGun
        {
            id = "rime_cannon", name = "RIME CANNON", damage = 14, magazine = 8, maxReserve = 64, pellets = 1,
            shotsPerSecond = 2.2f, reloadSeconds = 1.9f, speed = 20f, range = 12f, noise = 0.45f, r = 170, g = 235, b = 255,
            shot = "lance", wonder = true, freeze = 3f
        };

        /// <summary>
        /// The third wonder weapon, dropped by bosses (THE CURATOR in Zombie Land, THE IRON WIDOW on the Rail Yard):
        /// a slow dark orb that drags the dead in, then bursts.
        /// </summary>
        public static readonly ZLGun Singularity = new ZLGun
        {
            id = "singularity", name = "SINGULARITY CANNON", damage = 40, magazine = 3, maxReserve = 18, pellets = 1,
            shotsPerSecond = 0.8f, reloadSeconds = 2.4f, speed = 9f, range = 11f, noise = 0.6f, r = 190, g = 120, b = 255,
            shot = "comet", wonder = true, pull = 4f, bossDrop = true
        };

        /// <summary>
        /// The fourth wonder weapon (mystery box): a slow ember that "kindles" one of the dead. For a while it hunts its
        /// own kind (its bites hit hard, and the points are yours), then it burns out. Bosses can't be kindled.
        /// </summary>
        public static readonly ZLGun Kindler = new ZLGun
        {
            id = "kindler", name = "THE KINDLER", damage = 6, magazine = 4, maxReserve = 24, pellets = 1,
            shotsPerSecond = 1.1f, reloadSeconds = 2f, speed = 14f, range = 12f, noise = 0.4f, r = 255, g = 170, b = 70,
            shot = "crescent", wonder = true, kindle = 10f
        };

        /// <summary>A kindled zombie's bite on the dead (scaled up by the round in Last Stand), and how often it bites.</summary>
        public const int KindledBite = 45;
        public const float KindledBiteEvery = 0.7f, KindledSpeedScale = 1.35f;

        /// <summary>How long the Singularity pulls before it bursts, and the burst radius.</summary>
        public const float PullSeconds = 0.6f, PullBurstRadius = 3.2f;

        /// <summary>Radius of the Rime Cannon's frost burst, and how much harder frozen zombies are hit.</summary>
        public const float FreezeRadius = 1.8f, FrozenDamageScale = 1.5f, FrozenSpeedScale = 0.25f;

        public static readonly ZLGun[] Guns =
        {
            Pistol,
            new ZLGun { id = "riot_shotgun", name = "RIOT SHOTGUN", damage = 4, magazine = 6, maxReserve = 36, pellets = 6, spreadDegrees = 26f,
                        shotsPerSecond = 1.3f, reloadSeconds = 2f, speed = 18f, range = 6f, noise = 0.8f, r = 255, g = 200, b = 120, shot = "spark" },
            new ZLGun { id = "street_smg", name = "STREET SWEEPER SMG", damage = 3, magazine = 30, maxReserve = 180, pellets = 1, spreadDegrees = 7f,
                        shotsPerSecond = 10f, reloadSeconds = 1.6f, speed = 22f, range = 9f, automatic = true, noise = 0.6f, r = 255, g = 220, b = 100, shot = "shard" },
            new ZLGun { id = "ranger_rifle", name = "RANGER RIFLE", damage = 14, magazine = 5, maxReserve = 40, pellets = 1,
                        shotsPerSecond = 1.4f, reloadSeconds = 2.1f, speed = 34f, range = 16f, noise = 0.9f, r = 240, g = 240, b = 220, shot = "lance" },
            new ZLGun { id = "assault_rifle", name = "MILITIA RIFLE", damage = 5, magazine = 30, maxReserve = 210, pellets = 1, spreadDegrees = 3f,
                        shotsPerSecond = 7f, reloadSeconds = 1.8f, speed = 28f, range = 13f, automatic = true, noise = 0.75f, r = 255, g = 210, b = 140, shot = "shard" },
            new ZLGun { id = "patrol_lmg", name = "PATROL LMG", damage = 5, magazine = 80, maxReserve = 240, pellets = 1, spreadDegrees = 8f,
                        shotsPerSecond = 9f, reloadSeconds = 3.2f, speed = 26f, range = 12f, automatic = true, noise = 1f, r = 255, g = 190, b = 90, shot = "shard" },
            new ZLGun { id = "thumper", name = "THUMPER LAUNCHER", damage = 30, magazine = 1, maxReserve = 12, pellets = 1,
                        shotsPerSecond = 0.9f, reloadSeconds = 1.6f, speed = 12f, range = 11f, noise = 1f, r = 255, g = 140, b = 60, shot = "comet" },
            StormCoil,
            RimeCannon,
            Singularity,
            Kindler,   // appended: online play sends guns by index
        };

        /// <summary>Can this kind be kindled (not bosses or the master)?</summary>
        public static bool Kindleable(ZLFoe f) => f != ZLFoe.Herald && f != ZLFoe.FirstTurned;


        /// <summary>Relic weapons: damage everything, but only the true one hurts the First Turned.</summary>
        public static ZLGun RelicGun(ZLRelic relic)
        {
            var s = Relic(relic);
            var g = new ZLGun
            {
                id = "relic_" + relic.ToString().ToLowerInvariant(), name = s.name, isRelic = true, relic = relic,
                damage = 12, magazine = 12, maxReserve = 36, pellets = 1, shotsPerSecond = 3f, reloadSeconds = 1.5f, speed = 20f,
                range = 11f, noise = 0.7f, r = s.r, g = s.g, b = s.b, shot = "crescent"
            };
            switch (relic)
            {
                case ZLRelic.Cinder: g.pellets = 3; g.spreadDegrees = 20f; g.range = 5f; g.automatic = true; g.shotsPerSecond = 8f; g.damage = 5; g.magazine = 40; g.maxReserve = 120; g.shot = "blob"; break;
                case ZLRelic.Arc: g.automatic = true; g.shotsPerSecond = 5f; g.damage = 8; g.magazine = 20; g.maxReserve = 60; g.shot = "spark"; break;
                case ZLRelic.Frostbite: g.damage = 16; g.shotsPerSecond = 1.6f; g.magazine = 8; g.maxReserve = 32; g.shot = "comet"; break;
            }
            return g;
        }

        public static ZLGun Gun(string id)
        {
            foreach (var g in Guns) if (g.id == id) return g;
            foreach (ZLRelic r in Enum.GetValues(typeof(ZLRelic))) if (RelicGun(r).id == id) return RelicGun(r);
            return null;
        }

        // ---- vehicles ------------------------------------------------------------------------------------

        public static readonly ZLVehicleSpec[] Vehicles =
        {
            new ZLVehicleSpec { kind = ZLVehicleKind.Car, name = "SEDAN", topSpeed = 15f, accel = 11f, turn = 190f, grip = 6f, mass = 3f, health = 140, noise = 0.6f, ram = 2.2f },
            new ZLVehicleSpec { kind = ZLVehicleKind.Truck, name = "PICKUP TRUCK", topSpeed = 12.5f, accel = 8f, turn = 150f, grip = 7f, mass = 6f, health = 280, noise = 0.8f, ram = 3.5f },
            new ZLVehicleSpec { kind = ZLVehicleKind.Bike, name = "BICYCLE", topSpeed = 8.5f, accel = 9f, turn = 260f, grip = 9f, mass = 0.8f, health = 50, noise = 0f, ram = 0.6f },
            new ZLVehicleSpec { kind = ZLVehicleKind.Motorcycle, name = "MOTORCYCLE", topSpeed = 19f, accel = 15f, turn = 230f, grip = 5f, mass = 1.6f, health = 80, noise = 0.7f, ram = 1.4f },
        };

        public static ZLVehicleSpec Vehicle(ZLVehicleKind k) => Vehicles[(int)k];

        // ---- the fourteen missions -----------------------------------------------------------------------

        public static readonly ZLMissionSpec[] Missions =
        {
            // Maple Row (start)
            new ZLMissionSpec { id = "m_medicine", name = "COLD MEDICINE", district = "suburbs", giver = "NANA RUTH", type = ZLMissionType.Retrieve, required = true, count = 3,
                briefing = "Nana Ruth's insulin is somewhere in the Maple Row pharmacy lot. Three coolers fell off the delivery van. Find them.",
                done = "\"Bless you. Take this, the pharmacist kept it in the safe. It hums at night.\" A Graystone shard.",
                reveals = new[] { "m_broadcast", "m_bus" } },
            new ZLMissionSpec { id = "b_gunshop", name = "GUN SHOP SIEGE", district = "suburbs", giver = "HANK", type = ZLMissionType.HoldOut, seconds = 60f,
                briefing = "Hank barricaded himself in his gun shop. Help him hold the door for one minute and he'll open the back room.",
                done = "\"Never thought I'd sell a shotgun to a hero.\" Hank gives you the Riot Shotgun and body armor.",
                reward = "riot_shotgun" },
            // Old Quarter
            new ZLMissionSpec { id = "m_bus", name = "BUS 19", district = "oldtown", giver = "THEO", type = ZLMissionType.Rescue, required = true,
                briefing = "Theo is trapped on the No. 19 bus in the Old Quarter. Get him to the nearest safehouse alive.",
                done = "Theo drew what he saw on the bus window: a man in a guard's uniform, walking the dead like dogs. He hands you a shard he found.",
                reveals = new[] { "m_bells" } },
            new ZLMissionSpec { id = "m_bells", name = "RING THE BELLS", district = "oldtown", giver = "FATHER ODUYA", type = ZLMissionType.HoldOut, required = true, seconds = 70f,
                briefing = "Father Oduya wants the cathedral bells rung so survivors know the Old Quarter still stands. Hold the plaza while he rings them.",
                done = "The bells ring out over New Hollow. In the bell chamber, a shard was wedged in the old clockwork.",
                reveals = new[] { "m_roses", "b_butcher" } },
            // Greenway Park
            new ZLMissionSpec { id = "m_roses", name = "ROT IN THE ROSES", district = "park", giver = "GROUNDSKEEPER MAE", type = ZLMissionType.PurgeNest, required = true,
                briefing = "A rot nest has grown in the rose garden. It feeds the whole park. Burn it out.",
                done = "The nest collapses. In the roots, still warm, a Graystone shard.",
                reveals = new[] { "m_notes" } },
            // Downtown
            new ZLMissionSpec { id = "m_broadcast", name = "LAST BROADCAST", district = "downtown", giver = "DJ KASSIDY", type = ZLMissionType.Signal, required = true, seconds = 30f,
                briefing = "DJ Kassidy can put every survivor on one channel if you restart the radio mast. Hold the switch and keep them off you.",
                done = "\"New Hollow, this is Kassidy. We are still here.\" She found a shard in the transmitter's crystal housing.",
                reveals = new[] { "m_engine7", "b_quartermaster" } },
            new ZLMissionSpec { id = "m_engine7", name = "ENGINE 7", district = "downtown", giver = "CAPTAIN REYES", type = ZLMissionType.Delivery, required = true, seconds = 75f,
                briefing = "The fire crew needs Engine 7 at the harbor barricade before the horde breaks through. Drive. Don't stop.",
                done = "The barricade holds. Captain Reyes pulls a shard from the engine's radiator grille. \"Been rattling since day one.\"",
                reveals = new[] { "m_dockmaster", "b_joyride" } },
            new ZLMissionSpec { id = "b_butcher", name = "THE BUTCHER", district = "downtown", giver = "OFFICER LIN", type = ZLMissionType.HuntHerald,
                briefing = "Something huge in a meat-packer's apron is clearing out the downtown shelters. Officer Lin wants it gone.",
                done = "The Butcher goes down. Officer Lin unlocks the precinct's supply cage: medkits and armor for every safehouse.",
                reward = "medkits" },
            // Saltmouth Harbor
            new ZLMissionSpec { id = "m_dockmaster", name = "THE DOCKMASTER", district = "harbor", giver = "OLD MAGGIE", type = ZLMissionType.HuntHerald, required = true,
                briefing = "The old dockmaster turned and now runs the harbor dead like a crew. Put him down.",
                done = "The Dockmaster wore a shard on a chain around his neck, like a ship's medal.",
                reveals = new[] { "m_power" } },
            new ZLMissionSpec { id = "b_joyride", name = "JOYRIDE", district = "harbor", giver = "PIXIE", type = ZLMissionType.Delivery, seconds = 50f,
                briefing = "Pixie bets you can't ride her motorcycle from the docks to the lighthouse road in under fifty seconds.",
                done = "\"Okay, okay, keep it.\" Motorcycles now wait outside every safehouse you've lit.",
                reward = "motorcycles" },
            // Ironside
            new ZLMissionSpec { id = "m_power", name = "LIGHTS ON", district = "industrial", giver = "ENGINEER BAO", type = ZLMissionType.PowerUp, required = true, count = 3,
                briefing = "Start the three Ironside generators so the street lights come back on. The dead hate the lights.",
                done = "Ironside lights up. In the last generator's housing, Bao finds a shard fused to the coil.",
                reveals = new[] { "m_stadium" } },
            new ZLMissionSpec { id = "b_quartermaster", name = "QUARTERMASTER", district = "industrial", giver = "SGT. OKAFOR", type = ZLMissionType.Rescue,
                briefing = "Sergeant Okafor is pinned in the rail depot with the army's supply manifest. Get her to a safehouse.",
                done = "\"Every safehouse you've lit gets an ammo crate from now on.\" She hands you her Militia Rifle.",
                reward = "assault_rifle" },
            // College Hill
            new ZLMissionSpec { id = "m_notes", name = "PATIENT ZERO FILES", district = "university", giver = "DR. HALE", type = ZLMissionType.Retrieve, required = true, count = 3,
                briefing = "Dr. Hale's research notes blew all over the Larkin Labs grounds. Three folders. Find them before the rain does.",
                done = "The files name the first man to touch the stone: night guard Silas Vane. Clipped to the last folder, a shard.",
                reveals = new[] { "m_stadium" } },
            new ZLMissionSpec { id = "m_stadium", name = "FINAL WHISTLE", district = "university", giver = "COACH DELACROIX", type = ZLMissionType.PurgeNest, required = true,
                briefing = "The biggest nest in the city has taken over Hollow Field's tunnel. Coach says it's feeding off the crowd.",
                done = "The stadium goes quiet. In the burst nest lies the last shard. Every shard in your pack starts humming at once.",
                reveals = new string[0] },
        };

        public static ZLMissionSpec Mission(string id)
        {
            foreach (var m in Missions) if (m.id == id) return m;
            foreach (var m in Chapter2Missions) if (m.id == id) return m;
            foreach (var m in Chapter3Missions) if (m.id == id) return m;
            return null;
        }

        // ---- chapter 2: Blackwater Isle -----------------------------------------------------------------

        public const string Chapter2Name = "CHAPTER 2: BLACKWATER";
        public const string Chapter2BossName = "MOTHER BRINE";
        public const int SigilsNeeded = 4;
        /// <summary>The quarantine line on the causeway to the isle (a door in city.json).</summary>
        public const string IsleBridge = "isle_bridge";
        public const string TideGate = "tide_gate";

        public static readonly ZLMissionSpec[] Chapter2Missions =
        {
            new ZLMissionSpec { id = "c2_ferry", name = "THE LAST FERRY", district = "isle", giver = "CAPTAIN ADAEZE", type = ZLMissionType.Rescue, required = true, chapter = 2,
                briefing = "The ferry captain who saw it come out of the water is hiding in the village net shed. Get her to the Blackwater safehouse.",
                done = "\"It was a stone, like yours, but it sank. The tide's been bringing them back ever since.\" She presses a cold sea-glass sigil into your hand.",
                reveals = new[] { "c2_cannery", "c2_smuggler" } },
            new ZLMissionSpec { id = "c2_cannery", name = "CANNERY ROW", district = "isle", giver = "OLD TOMASZ", type = ZLMissionType.PurgeNest, required = true, chapter = 2,
                briefing = "Something is nesting in the cannery's freezer hall, and it smells of the deep. Old Tomasz wants it burned out.",
                done = "The nest bursts into brine and weed. Tangled in it, a sea-glass sigil glows green.",
                reveals = new[] { "c2_beacon" } },
            new ZLMissionSpec { id = "c2_beacon", name = "LIGHT THE BEACON", district = "isle", giver = "KEEPER IMOGEN", type = ZLMissionType.Signal, required = true, seconds = 40f, chapter = 2,
                briefing = "Keeper Imogen can relight the north beacon and warn the mainland boats away. Hold the lamp room while it warms up.",
                done = "The beam sweeps the water. Where it touches the waves, something huge flinches. Imogen hands you the sigil from the lens housing.",
                reveals = new[] { "c2_cellblock" } },
            new ZLMissionSpec { id = "c2_cellblock", name = "CELL BLOCK D", district = "isle", giver = "DR. HALE", type = ZLMissionType.Retrieve, required = true, count = 3, chapter = 2,
                briefing = "Dr. Hale tracked the second stone's research to Blackwater Penitentiary. Three logbooks are scattered across the prison yard.",
                done = "The logs end with the warden walking into the sea. The last page holds the fourth sigil, and a drawing of a gate beneath the prison.",
                reveals = new string[0] },
            new ZLMissionSpec { id = "c2_smuggler", name = "SMUGGLER'S RUN", district = "isle", giver = "PIXIE", type = ZLMissionType.Delivery, seconds = 55f, chapter = 2,
                briefing = "Pixie stashed a truck of army surplus in the village. Drive it to the lighthouse lot before the tide crawlers swarm it.",
                done = "\"Told you it was worth it.\" The truck held a Patrol LMG with your name on it.",
                reward = "patrol_lmg" },
        };

        public static readonly string[] Chapter2StartingLeads = { "c2_ferry" };

        public static readonly string[] Chapter2Intro =
        {
            "DR. HALE (radio): \"You did it. But I was wrong about one thing. There were TWO stones.\"",
            "\"The second fell into the sea off Blackwater Isle. The dead there don't answer to Silas. They answer to the water.\"",
            "The army's quarantine line on the causeway is open. Cross to the isle and find four sea-glass sigils.",
            "Watch for the new dead: riot police with their shields still up, and the leapers that spring from the dark.",
        };

        public static readonly string[] Chapter2BossIntro =
        {
            "The four sigils burn cold. Under the prison, the tide gate grinds open.",
            "MOTHER BRINE rises out of the black water, wearing the warden's keys.",
        };

        // ---- chapter 3: the Larkin sublevel ------------------------------------------------------------

        public const string Chapter3Name = "CHAPTER 3: THE SUBLEVEL";
        public const string Chapter3BossName = "THE CURATOR";
        public const int KeycardsNeeded = 3;
        /// <summary>The boss door in labs.json, and the freight lift sites (city.json "lab_lift", labs.json "lift").</summary>
        public const string Vault = "curator_vault", LabLift = "lab_lift", LabsMap = "labs";

        public static readonly ZLMissionSpec[] Chapter3Missions =
        {
            new ZLMissionSpec { id = "c3_power", name = "EMERGENCY POWER", district = "labs", giver = "DR. HALE", type = ZLMissionType.PowerUp, required = true, count = 3, chapter = 3,
                briefing = "The sublevel runs on three emergency generators. Start them and the doors down here will listen to us again.",
                done = "The lights stutter on, floor by floor. In the generator room's lockbox: a red security keycard.",
                reveals = new[] { "c3_samples", "c3_armory" } },
            new ZLMissionSpec { id = "c3_samples", name = "SPECIMEN B", district = "labs", giver = "DR. HALE", type = ZLMissionType.Retrieve, required = true, count = 3, chapter = 3,
                briefing = "Three sealed sample cases from the first stone are loose in specimen storage. Get them before something eats them.",
                done = "\"These were cut from the stone the night it fell. Someone kept them.\" Clipped to the last case, a blue keycard.",
                reveals = new[] { "c3_seal" } },
            new ZLMissionSpec { id = "c3_seal", name = "QUARANTINE DOORS", district = "labs", giver = "SGT. OKAFOR", type = ZLMissionType.HoldOut, required = true, seconds = 60f, chapter = 3,
                briefing = "Okafor is sealing the quarantine wing by hand. Hold the dead off her for one minute.",
                done = "The doors slam shut. Okafor hands you the last keycard: \"Director's vault. Whatever's in there signed the orders.\"",
                reveals = new string[0] },
            new ZLMissionSpec { id = "c3_armory", name = "THE ARMORY", district = "labs", giver = "SGT. OKAFOR", type = ZLMissionType.HuntHerald, chapter = 3,
                briefing = "Something wearing a guard's armor is camped in the sublevel armory. Clear it and the racks are yours.",
                done = "The armory is ours. On the rack, a Thumper Launcher with a full belt of shells.",
                reward = "thumper" },
        };

        public static readonly string[] Chapter3StartingLeads = { "c3_power" };

        public static readonly string[] Chapter3Intro =
        {
            "DR. HALE (radio): \"Blackwater's quiet. But Larkin Labs kept a piece of the first stone.\"",
            "\"There's a freight lift beside the labs on College Hill. It goes down to the sublevel the army sealed.\"",
            "Find three keycards and open the director's vault. End this where it started.",
        };

        public static readonly string[] Chapter3BossIntro =
        {
            "The vault door rolls back. Rows of glass. One of them is empty.",
            "THE CURATOR, once the lab's director, steps out wearing the stone like a heart.",
        };

        public static readonly string[] Chapter3Ending =
        {
            "The Curator falls, and the last piece of the stone goes dark.",
            "Under its case you find the Singularity Cannon, the weapon it was building to keep the dead in line.",
            "CHAPTER 3 COMPLETE. New Hollow is yours.",
        };

        public static readonly string[] Chapter2Ending =
        {
            "Mother Brine sinks, and the second stone cracks apart in the tide.",
            "On Blackwater Isle the dead lie down in the shallows and do not get up again.",
            "CHAPTER 2 COMPLETE. New Hollow and the isle are yours to roam.",
        };

        public static int RequiredCount
        {
            get { int n = 0; foreach (var m in Missions) if (m.required) n++; return n; }
        }

        /// <summary>Missions whose leads you know at the very start.</summary>
        public static readonly string[] StartingLeads = { "m_medicine", "b_gunshop", "m_broadcast" };

        // ---- story beats ---------------------------------------------------------------------------------

        public static readonly string[] Intro =
        {
            "New Hollow. Day 9 of the quarantine. The army fenced the city in and left.",
            "The radio says it started at Larkin Labs, with a stone from the sky and the man who touched it first.",
            "They call him the First Turned. Every corpse in this city walks for him.",
            "Find the ten Graystone shards. Find out what can hurt him, and where he hides. Then end it.",
            "You have a pistol and 25 rounds. Stay out in the open too long and he'll send the hounds.",
        };

        public static readonly string[] BossIntro =
        {
            "The ten shards blaze in your pack. The seal cracks open.",
            "SILAS VANE: \"I was only the night guard. It chose me. It will choose you too.\"",
        };

        public static readonly string[] Ending =
        {
            "The First Turned falls, and with him, every last one of the dead in New Hollow.",
            "The streets go quiet. Then, one by one, the survivors come out into the light.",
            "ZOMBIE LAND IS CLEAR. Thank you for playing.",
        };
    }
}
