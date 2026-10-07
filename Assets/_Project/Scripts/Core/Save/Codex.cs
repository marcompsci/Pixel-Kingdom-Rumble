using System;
using System.Collections.Generic;

namespace PKR.Core
{
    public enum CodexCategory { Hero, Enemy, Place }

    /// <summary>How many of one enemy the player has defeated (all runs). JsonUtility-friendly.</summary>
    [Serializable]
    public class EnemyTally
    {
        public string enemyId = "";
        public int defeated;
    }

    /// <summary>One codex page as the UI lists it.</summary>
    public struct CodexEntryRef
    {
        public CodexCategory category;
        public string id;
        /// <summary>Unlocked from the start (e.g. starting heroes). Never counts as "new".</summary>
        public bool alwaysUnlocked;

        public CodexEntryRef(CodexCategory category, string id, bool alwaysUnlocked = false)
        {
            this.category = category;
            this.id = id;
            this.alwaysUnlocked = alwaysUnlocked;
        }
    }

    public struct CodexProgress
    {
        public int unlocked;
        public int total;
        /// <summary>Unlocked entries the player hasn't opened yet.</summary>
        public int unseen;
    }

    /// <summary>
    /// Codex rules (pure, unit-tested). Entries unlock through play:
    ///  - Heroes: when the hero is unlocked.
    ///  - Enemies: the first time one is defeated (defeats are also counted).
    ///  - Places: the first time the level is entered (or once it has been cleared).
    /// Opening an entry marks it seen, which clears its NEW badge.
    /// </summary>
    public static class Codex
    {
        public static string Key(CodexCategory category, string id)
        {
            switch (category)
            {
                case CodexCategory.Hero: return "hero:" + id;
                case CodexCategory.Enemy: return "enemy:" + id;
                default: return "place:" + id;
            }
        }

        public static bool IsUnlocked(SaveData save, in CodexEntryRef entry)
        {
            if (string.IsNullOrEmpty(entry.id)) return false;
            if (entry.alwaysUnlocked) return true;
            if (save == null) return false;
            if (save.unlockedCodexEntries.Contains(Key(entry.category, entry.id))) return true;
            switch (entry.category)
            {
                case CodexCategory.Hero: return save.IsCharacterUnlocked(entry.id);
                case CodexCategory.Place:
                    var rec = save.GetLevel(entry.id);
                    return rec != null && rec.completed;
                default: return false;
            }
        }

        /// <summary>Unlock an entry. Returns true only the first time.</summary>
        public static bool Discover(SaveData save, CodexCategory category, string id)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));
            if (string.IsNullOrEmpty(id)) return false;
            if (IsUnlocked(save, new CodexEntryRef(category, id))) return false;
            save.unlockedCodexEntries.Add(Key(category, id));
            return true;
        }

        /// <summary>Count a defeat and unlock the enemy's entry. Returns true if this was the first defeat (new entry).</summary>
        public static bool RecordDefeat(SaveData save, string enemyId)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));
            if (string.IsNullOrEmpty(enemyId)) return false;
            var tally = FindTally(save, enemyId);
            if (tally == null)
            {
                tally = new EnemyTally { enemyId = enemyId };
                save.enemyDefeats.Add(tally);
            }
            if (tally.defeated < int.MaxValue) tally.defeated++;
            return Discover(save, CodexCategory.Enemy, enemyId);
        }

        public static int DefeatCount(SaveData save, string enemyId)
        {
            var t = save != null && !string.IsNullOrEmpty(enemyId) ? FindTally(save, enemyId) : null;
            return t != null ? t.defeated : 0;
        }

        public static bool IsNew(SaveData save, in CodexEntryRef entry) =>
            !entry.alwaysUnlocked && IsUnlocked(save, entry) && !save.seenCodexEntries.Contains(Key(entry.category, entry.id));

        /// <summary>Mark an unlocked entry as read. Returns true if that cleared a NEW badge.</summary>
        public static bool MarkSeen(SaveData save, in CodexEntryRef entry)
        {
            if (!IsNew(save, entry)) return false;
            save.seenCodexEntries.Add(Key(entry.category, entry.id));
            return true;
        }

        public static CodexProgress Progress(SaveData save, IEnumerable<CodexEntryRef> entries)
        {
            var p = new CodexProgress();
            if (entries == null) return p;
            foreach (var e in entries)
            {
                if (string.IsNullOrEmpty(e.id)) continue;
                p.total++;
                if (!IsUnlocked(save, e)) continue;
                p.unlocked++;
                if (IsNew(save, e)) p.unseen++;
            }
            return p;
        }

        // ---- List paging (the codex shows a fixed number of entries per page) ----------------------

        public static int PageCount(int count, int perPage) =>
            perPage <= 0 || count <= 0 ? 1 : (count + perPage - 1) / perPage;

        /// <summary>Wraps like the hero arrows: past the last page goes back to the first.</summary>
        public static int StepPage(int page, int dir, int count, int perPage)
        {
            int pages = PageCount(count, perPage);
            int p = (page + dir) % pages;
            return p < 0 ? p + pages : p;
        }

        /// <summary>Index range [start, end) of a page, clamped to the list.</summary>
        public static void PageRange(int page, int perPage, int count, out int start, out int end)
        {
            if (perPage <= 0 || count <= 0) { start = end = 0; return; }
            int pages = PageCount(count, perPage);
            page = Math.Max(0, Math.Min(page, pages - 1));
            start = page * perPage;
            end = Math.Min(count, start + perPage);
        }

        static EnemyTally FindTally(SaveData save, string enemyId)
        {
            foreach (var t in save.enemyDefeats)
                if (t != null && t.enemyId == enemyId) return t;
            return null;
        }
    }
}
