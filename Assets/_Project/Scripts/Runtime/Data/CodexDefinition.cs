using System.Collections.Generic;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// What the Codex lists: heroes (from the roster), foes (this list) and places (the Story Quest world's levels).
    /// The text itself lives on each definition (lore / codexEntry). Unlock rules are in Core <see cref="Codex"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "PKR/Codex", fileName = "Codex")]
    public class CodexDefinition : ScriptableObject
    {
        public CharacterRoster roster;
        [Tooltip("Foes in display order. Add new enemy types here so they get a page.")]
        public List<EnemyDefinition> enemies = new List<EnemyDefinition>();
        [Tooltip("Places come from this world's levels, in play order.")]
        public WorldDefinition world;

        public List<CharacterDefinition> Heroes()
        {
            var list = new List<CharacterDefinition>();
            if (roster != null)
                foreach (var h in roster.heroes)
                    if (h != null && h.playableInThisBuild) list.Add(h);
            return list;
        }

        public List<EnemyDefinition> Enemies()
        {
            var list = new List<EnemyDefinition>();
            foreach (var e in enemies) if (e != null && !string.IsNullOrEmpty(e.id)) list.Add(e);
            return list;
        }

        public List<LevelDefinition> Places()
        {
            var list = new List<LevelDefinition>();
            if (world != null)
                foreach (var l in world.levels) if (l != null && !string.IsNullOrEmpty(l.id)) list.Add(l);
            return list;
        }

        public static CodexEntryRef Ref(CharacterDefinition h) => new CodexEntryRef(CodexCategory.Hero, h.id, h.unlockedByDefault);
        public static CodexEntryRef Ref(EnemyDefinition e) => new CodexEntryRef(CodexCategory.Enemy, e.id);
        public static CodexEntryRef Ref(LevelDefinition l) => new CodexEntryRef(CodexCategory.Place, l.id);

        public List<CodexEntryRef> Entries(CodexCategory category)
        {
            var refs = new List<CodexEntryRef>();
            switch (category)
            {
                case CodexCategory.Hero: foreach (var h in Heroes()) refs.Add(Ref(h)); break;
                case CodexCategory.Enemy: foreach (var e in Enemies()) refs.Add(Ref(e)); break;
                default: foreach (var l in Places()) refs.Add(Ref(l)); break;
            }
            return refs;
        }

        public List<CodexEntryRef> AllEntries()
        {
            var all = Entries(CodexCategory.Hero);
            all.AddRange(Entries(CodexCategory.Enemy));
            all.AddRange(Entries(CodexCategory.Place));
            return all;
        }

        /// <summary>Progress for the current save (nothing unlocked without a save service).</summary>
        public CodexProgress Progress(CodexCategory? category = null)
        {
            var save = Services.Save != null ? Services.Save.Data : null;
            return Codex.Progress(save, category.HasValue ? Entries(category.Value) : AllEntries());
        }
    }
}
