using System;
using System.Collections.Generic;

using UnityEditor;

namespace PKR.EditorTools
{
    /// <summary>
    /// Move tuning from the balance lab (Oct 2026), applied to the existing move assets every setup (the builders only
    /// fill in new assets, so changes to their starting values would never reach moves that already exist).
    /// Values are absolute, so applying twice changes nothing.
    ///   Volt (won 93% of lab games): his 2-frame jab beat every other first hit; now 4 frames like the rest; finishers -1.
    ///   Grizz (83%): armor from the first frame of Maul Rush made it unanswerable; paw swipe and toss hit a little softer.
    ///   Nova (14%): the shortest combo in the cast; her sweep, rising arc and comet bolt hit harder and the sweep is faster.
    ///   Run 3: heavies still won most (Grizz 86%, Bo 88%): their two-damage openers and extra health stacked with
    ///   knockback resistance. Openers now hit for 1 like everyone's, and weight adds half as much health.
    ///   Nova, Brick, Luma, Rex (14-25%): their combo hits now hold the foe long enough to connect (chain hitstun).
    /// </summary>
    public static class BalancePatch
    {
        struct Patch
        {
            public int startup, damage, armorStart; // -1 = leave as is
        }

        static Patch P(int startup = -1, int damage = -1, int armorStart = -1) =>
            new Patch { startup = startup, damage = damage, armorStart = armorStart };

        static readonly Dictionary<string, Patch> Moves = new Dictionary<string, Patch>
        {
            { "Volt_Jab", P(startup: 4) },
            { "Volt_HaymakerOverload", P(damage: 3) },
            { "Volt_VoltUppercut", P(damage: 2) },
            { "Grizz_MaulRush", P(armorStart: 4) },
            { "Grizz_PawSwipe", P(damage: 1) },
            { "Grizz_BearHugToss", P(damage: 3) },
            { "Grizz_DoubleSwipe", P(damage: 1) },
            { "Grizz_MaulSlam", P(damage: 3) },
            { "Bo_ShoulderShove", P(damage: 1) },
            { "Bo_ChainHook", P(damage: 1) },
            { "Nova_CometSweep", P(startup: 4, damage: 3) },
            { "Nova_RisingArc", P(damage: 3) },
            { "Nova_CometBolt", P(damage: 2) },
        };

        /// <summary>Patched move asset names (tests).</summary>
        public static IEnumerable<string> PatchedMoves => Moves.Keys;

        public static void Apply(IEnumerable<CharacterDefinition> heroes)
        {
            foreach (var h in heroes)
            {
                var ms = h != null ? h.moveset : null;
                if (ms == null) continue;
                var seen = new HashSet<MoveDefinition>();
                foreach (var m in new[] { ms.groundAttack, ms.groundUpAttack, ms.airAttack, ms.groundSpecial, ms.sideSpecial,
                                          ms.downSpecial, ms.airSpecial, ms.abilityImpact })
                    Walk(m, seen);
                // Chain hitstun: every combo hit must hold the foe until the next one lands. The ten Versus fighters got
                // this when built; the original four (Nova, Brick, Luma, Rex) never did, so their combos could be
                // escaped between hits.
                VersusFighters.Tune(h);
            }
        }

        static void Walk(MoveDefinition m, HashSet<MoveDefinition> seen)
        {
            if (m == null || !seen.Add(m)) return;
            if (Moves.TryGetValue(m.name, out var p)) ApplyTo(m, p);
            Walk(m.followUp, seen);
            Walk(m.specialFollowUp, seen);
        }

        static void ApplyTo(MoveDefinition m, Patch p)
        {
            bool dirty = false;
            if (p.startup >= 0 && m.frames.startup != p.startup)
            {
                var f = m.frames; f.startup = p.startup; m.frames = f; dirty = true;
            }
            if (p.damage >= 0 && m.hit.damage != p.damage)
            {
                var h = m.hit; h.damage = p.damage; m.hit = h; dirty = true;
            }
            if (p.armorStart >= 0 && m.armorStartFrame != p.armorStart)
            {
                m.armorStartFrame = p.armorStart; dirty = true;
            }
            if (dirty) EditorUtility.SetDirty(m);
        }
    }
}
