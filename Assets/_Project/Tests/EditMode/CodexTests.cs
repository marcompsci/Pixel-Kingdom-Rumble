using System.Collections.Generic;
using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    [TestFixture]
    public class CodexTests
    {
        static CodexEntryRef Enemy(string id) => new CodexEntryRef(CodexCategory.Enemy, id);
        static CodexEntryRef Place(string id) => new CodexEntryRef(CodexCategory.Place, id);
        static CodexEntryRef Hero(string id, bool always = false) => new CodexEntryRef(CodexCategory.Hero, id, always);

        [Test]
        public void Enemy_UnlocksOnFirstDefeat_AndCountsEveryDefeat()
        {
            var s = SaveData.CreateNew();
            Assert.IsFalse(Codex.IsUnlocked(s, Enemy("cog_beetle")));
            Assert.IsTrue(Codex.RecordDefeat(s, "cog_beetle"), "first defeat is a new entry");
            Assert.IsFalse(Codex.RecordDefeat(s, "cog_beetle"), "second defeat is not");
            Codex.RecordDefeat(s, "cog_beetle");
            Assert.IsTrue(Codex.IsUnlocked(s, Enemy("cog_beetle")));
            Assert.AreEqual(3, Codex.DefeatCount(s, "cog_beetle"));
            Assert.AreEqual(0, Codex.DefeatCount(s, "spring_tick"));
            Assert.AreEqual(1, s.enemyDefeats.Count);
        }

        [Test]
        public void RecordDefeat_IgnoresEmptyIds()
        {
            var s = SaveData.CreateNew();
            Assert.IsFalse(Codex.RecordDefeat(s, ""));
            Assert.IsFalse(Codex.RecordDefeat(s, null));
            Assert.AreEqual(0, s.enemyDefeats.Count);
            Assert.AreEqual(0, s.unlockedCodexEntries.Count);
        }

        [Test]
        public void Place_UnlocksOnDiscover_OrWhenAlreadyCleared()
        {
            var s = SaveData.CreateNew();
            Assert.IsFalse(Codex.IsUnlocked(s, Place("meadows")));
            Assert.IsTrue(Codex.Discover(s, CodexCategory.Place, "meadows"));
            Assert.IsFalse(Codex.Discover(s, CodexCategory.Place, "meadows"), "only once");
            Assert.IsTrue(Codex.IsUnlocked(s, Place("meadows")));

            // Saves from before the codex: a cleared level counts without a codex key.
            var old = SaveData.CreateNew();
            old.RecordLevelResult("warden", 60f, 0, 0);
            Assert.IsTrue(Codex.IsUnlocked(old, Place("warden")));
            Assert.IsFalse(Codex.Discover(old, CodexCategory.Place, "warden"), "already unlocked, no duplicate key");
            Assert.AreEqual(0, old.unlockedCodexEntries.Count);
        }

        [Test]
        public void Hero_FollowsHeroUnlocks()
        {
            var s = SaveData.CreateNew();
            Assert.IsTrue(Codex.IsUnlocked(s, Hero(SaveData.DefaultCharacterId)));
            Assert.IsFalse(Codex.IsUnlocked(s, Hero("luma")));
            Assert.IsTrue(Codex.IsUnlocked(s, Hero("brick", always: true)), "unlocked-by-default heroes");
            s.UnlockCharacter("luma");
            Assert.IsTrue(Codex.IsUnlocked(s, Hero("luma")));
            Assert.IsTrue(Codex.IsUnlocked(null, Hero("brick", always: true)), "no save: starting heroes still show");
            Assert.IsFalse(Codex.IsUnlocked(null, Hero("luma")));
            Assert.IsFalse(Codex.IsNew(null, Hero("luma")));
        }

        [Test]
        public void New_UntilSeen_AndNeverForAlwaysUnlocked()
        {
            var s = SaveData.CreateNew();
            Assert.IsFalse(Codex.IsNew(s, Enemy("gyro_moth")), "locked entries aren't new");
            Codex.RecordDefeat(s, "gyro_moth");
            Assert.IsTrue(Codex.IsNew(s, Enemy("gyro_moth")));
            Assert.IsTrue(Codex.MarkSeen(s, Enemy("gyro_moth")));
            Assert.IsFalse(Codex.IsNew(s, Enemy("gyro_moth")));
            Assert.IsFalse(Codex.MarkSeen(s, Enemy("gyro_moth")), "only once");

            Assert.IsFalse(Codex.IsNew(s, Hero("brick", always: true)));
            Assert.IsFalse(Codex.MarkSeen(s, Enemy("bolt_knight")), "can't see a locked entry");
            Assert.AreEqual(1, s.seenCodexEntries.Count);
        }

        [Test]
        public void Progress_CountsUnlockedAndUnseen()
        {
            var s = SaveData.CreateNew();
            s.UnlockCharacter("luma");
            Codex.RecordDefeat(s, "cog_beetle");
            Codex.RecordDefeat(s, "spring_tick");
            Codex.MarkSeen(s, Enemy("spring_tick"));
            var entries = new List<CodexEntryRef>
            {
                Hero("nova", always: true), Hero("brick", always: true), Hero("luma"), Hero("rex_rollo"),
                Enemy("cog_beetle"), Enemy("spring_tick"), Enemy("gyro_moth"),
                Place("meadows"), new CodexEntryRef(CodexCategory.Place, "") // empty ids are skipped
            };
            var p = Codex.Progress(s, entries);
            Assert.AreEqual(8, p.total);
            Assert.AreEqual(5, p.unlocked);
            Assert.AreEqual(2, p.unseen, "luma and cog_beetle");
            Assert.AreEqual(0, Codex.Progress(s, null).total);
        }

        [Test]
        public void Keys_DontCollideAcrossCategories()
        {
            var s = SaveData.CreateNew();
            Codex.Discover(s, CodexCategory.Place, "same");
            Assert.IsFalse(Codex.IsUnlocked(s, Enemy("same")));
            Assert.AreNotEqual(Codex.Key(CodexCategory.Enemy, "x"), Codex.Key(CodexCategory.Place, "x"));
        }

        [Test]
        public void Paging_CountsWrapsAndClamps()
        {
            Assert.AreEqual(1, Codex.PageCount(0, 5));
            Assert.AreEqual(1, Codex.PageCount(5, 5));
            Assert.AreEqual(2, Codex.PageCount(6, 5));
            Assert.AreEqual(1, Codex.PageCount(6, 0));

            Assert.AreEqual(1, Codex.StepPage(0, +1, 6, 5));
            Assert.AreEqual(0, Codex.StepPage(1, +1, 6, 5), "wraps forward");
            Assert.AreEqual(1, Codex.StepPage(0, -1, 6, 5), "wraps back");
            Assert.AreEqual(0, Codex.StepPage(0, +1, 3, 5), "one page stays put");

            Codex.PageRange(1, 5, 7, out int start, out int end);
            Assert.AreEqual(5, start); Assert.AreEqual(7, end);
            Codex.PageRange(9, 5, 7, out start, out end);
            Assert.AreEqual(5, start, "clamped to the last page"); Assert.AreEqual(7, end);
            Codex.PageRange(0, 5, 0, out start, out end);
            Assert.AreEqual(0, start); Assert.AreEqual(0, end);
        }

        [Test]
        public void Sanitize_RepairsCodexFields()
        {
            var s = SaveData.CreateNew();
            s.unlockedCodexEntries = null;
            s.seenCodexEntries = null;
            s.enemyDefeats = new List<EnemyTally> { null, new EnemyTally { enemyId = "" }, new EnemyTally { enemyId = "cog_beetle", defeated = -4 } };
            s.SanitizeAndMigrate();
            Assert.IsNotNull(s.unlockedCodexEntries);
            Assert.IsNotNull(s.seenCodexEntries);
            Assert.AreEqual(1, s.enemyDefeats.Count);
            Assert.AreEqual(0, Codex.DefeatCount(s, "cog_beetle"));

            s.enemyDefeats = null;
            s.SanitizeAndMigrate();
            Assert.IsNotNull(s.enemyDefeats);
        }
    }
}
