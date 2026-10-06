using System;
using System.Collections.Generic;

namespace PKR.Core
{
    /// <summary>One way to earn a hero: clear a Story Quest level.</summary>
    public struct UnlockRule
    {
        public string characterId;
        /// <summary>Level that must be cleared (any rank). Empty = no rule (never unlocked by this).</summary>
        public string clearLevelId;
    }

    /// <summary>Applies hero unlock rules to a save. Pure, so it is unit-tested.</summary>
    public static class UnlockRules
    {
        public static bool IsMet(SaveData save, in UnlockRule rule)
        {
            if (save == null || string.IsNullOrEmpty(rule.characterId) || string.IsNullOrEmpty(rule.clearLevelId)) return false;
            var rec = save.GetLevel(rule.clearLevelId);
            return rec != null && rec.completed;
        }

        /// <summary>Unlocks every hero whose rule is now met. Returns the ids that were newly unlocked, in rule order.</summary>
        public static List<string> Apply(SaveData save, IEnumerable<UnlockRule> rules)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));
            var unlocked = new List<string>();
            if (rules == null) return unlocked;
            foreach (var r in rules)
                if (IsMet(save, r) && save.UnlockCharacter(r.characterId)) unlocked.Add(r.characterId);
            return unlocked;
        }
    }
}
