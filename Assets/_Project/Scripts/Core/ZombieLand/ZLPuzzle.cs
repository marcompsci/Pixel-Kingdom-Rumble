using System;
using System.Collections.Generic;

namespace PKR.Core
{
    /// <summary>One clue from a required mission: either about the weapon or about the lair.</summary>
    [Serializable]
    public class ZLClue
    {
        public string missionId;
        /// <summary>"weapon" or "lair".</summary>
        public string about;
        /// <summary>The relic (as int) or lair id this clue talks about.</summary>
        public string subject;
        /// <summary>True: rules the subject out. False: points at it (the subject is the answer).</summary>
        public bool rulesOut;
        public string text;
    }

    /// <summary>
    /// The mystery behind each new game: which relic weapon hurts the First Turned and which of the six places is his
    /// lair. Picked from the save's seed, so it's different every playthrough. The ten required missions hand out
    /// ten clues: five about the weapon (the three wrong relics ruled out, plus two hints at the right one) and five
    /// about the lair (the five wrong places ruled out). With every clue, exactly one answer is left; with fewer, more
    /// than one place or weapon is still possible.
    /// </summary>
    public class ZLPuzzle
    {
        public ZLRelic TrueRelic { get; private set; }
        public string TrueLair { get; private set; }
        /// <summary>missionId -> clue.</summary>
        public readonly Dictionary<string, ZLClue> Clues = new Dictionary<string, ZLClue>();

        public static ZLPuzzle FromSeed(int seed)
        {
            var rng = new Random(seed);
            var p = new ZLPuzzle();
            p.TrueRelic = (ZLRelic)rng.Next(4);
            p.TrueLair = ZLCatalog.Lairs[rng.Next(ZLCatalog.Lairs.Length)].id;

            var clues = new List<ZLClue>();
            foreach (var r in ZLCatalog.Relics)
                if (r.id != p.TrueRelic)
                    clues.Add(new ZLClue { about = "weapon", subject = ((int)r.id).ToString(), rulesOut = true, text = r.notThis });
            var tr = ZLCatalog.Relic(p.TrueRelic);
            clues.Add(new ZLClue { about = "weapon", subject = ((int)tr.id).ToString(), rulesOut = false, text = tr.hint });
            clues.Add(new ZLClue { about = "weapon", subject = ((int)tr.id).ToString(), rulesOut = false,
                                   text = $"The Larkin notes keep circling one word in red: \"{tr.element.ToUpperInvariant()}\"." });
            foreach (var l in ZLCatalog.Lairs)
                if (l.id != p.TrueLair)
                    clues.Add(new ZLClue { about = "lair", subject = l.id, rulesOut = true, text = l.notHere });

            // Deal the ten clues to the ten required missions in a seeded order.
            var required = new List<string>();
            foreach (var m in ZLCatalog.Missions) if (m.required) required.Add(m.id);
            for (int i = clues.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                var t = clues[i]; clues[i] = clues[j]; clues[j] = t;
            }
            for (int i = 0; i < required.Count && i < clues.Count; i++)
            {
                clues[i].missionId = required[i];
                p.Clues[required[i]] = clues[i];
            }
            return p;
        }

        /// <summary>Relics still possible given the clues found (a hint at one relic leaves only that one).</summary>
        public List<ZLRelic> RelicsLeft(IEnumerable<string> foundMissions)
        {
            var left = new List<ZLRelic> { ZLRelic.Sunflare, ZLRelic.Frostbite, ZLRelic.Arc, ZLRelic.Cinder };
            foreach (var m in foundMissions)
            {
                if (!Clues.TryGetValue(m, out var c) || c.about != "weapon") continue;
                var r = (ZLRelic)int.Parse(c.subject);
                if (c.rulesOut) left.Remove(r);
                else { left.Clear(); left.Add(r); break; }
            }
            return left;
        }

        /// <summary>Lairs still possible given the clues found.</summary>
        public List<string> LairsLeft(IEnumerable<string> foundMissions)
        {
            var left = new List<string>();
            foreach (var l in ZLCatalog.Lairs) left.Add(l.id);
            foreach (var m in foundMissions)
                if (Clues.TryGetValue(m, out var c) && c.about == "lair" && c.rulesOut) left.Remove(c.subject);
            return left;
        }
    }
}
