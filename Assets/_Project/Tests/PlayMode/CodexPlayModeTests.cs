using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using PKR.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace PKR.Tests
{
    /// <summary>
    /// Codex panel with runtime data. Test ids are never unlocked in a real save, so opening entries changes nothing.
    /// </summary>
    public class CodexPlayModeTests
    {
        GameObject _host;
        readonly List<Object> _assets = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            if (_host != null) Object.Destroy(_host);
            foreach (var a in _assets) if (a != null) Object.Destroy(a);
            _assets.Clear();
        }

        T Make<T>() where T : ScriptableObject
        {
            var a = ScriptableObject.CreateInstance<T>();
            _assets.Add(a);
            return a;
        }

        CodexDefinition BuildCodex(int enemyCount)
        {
            var codex = Make<CodexDefinition>();
            codex.roster = Make<CharacterRoster>();
            var starter = Make<CharacterDefinition>();
            starter.id = "pkr_test_starter"; starter.displayName = "Starter"; starter.unlockedByDefault = true;
            starter.lore = "Test lore.";
            var locked = Make<CharacterDefinition>();
            locked.id = "pkr_test_locked_hero"; locked.displayName = "Secret Hero"; locked.unlockHint = "Clear a test level";
            codex.roster.heroes.Add(starter);
            codex.roster.heroes.Add(locked);
            for (int i = 0; i < enemyCount; i++)
            {
                var e = Make<EnemyDefinition>();
                e.id = "pkr_test_enemy_" + i; e.displayName = "Foe " + i;
                codex.enemies.Add(e);
            }
            codex.world = Make<WorldDefinition>();
            var level = Make<LevelDefinition>();
            level.id = "pkr_test_place"; level.displayName = "Test Place";
            codex.world.levels.Add(level);
            return codex;
        }

        CodexPanel OpenPanel(CodexDefinition codex)
        {
            _host = new GameObject("Codex");
            var container = new GameObject("Root", typeof(RectTransform)).GetComponent<RectTransform>();
            container.SetParent(_host.transform, false);
            var panel = _host.AddComponent<CodexPanel>();
            panel.Open(container, codex, null);
            return panel;
        }

        [UnityTest]
        public IEnumerator Codex_ShowsUnlockedAndLockedEntries_AndPagesFoes()
        {
            var codex = BuildCodex(enemyCount: CodexPanel.EntriesPerPage + 2);
            var panel = OpenPanel(codex);
            yield return null;

            Assert.AreEqual(CodexCategory.Hero, panel.Tab);
            Assert.AreEqual(2, panel.EntryCount);
            panel.OpenEntry(0);
            Assert.IsTrue(panel.IsShowingEntry);
            Assert.AreEqual("Starter", panel.OpenTitle, "starting heroes are always readable");
            panel.CloseEntry();
            panel.OpenEntry(1);
            Assert.AreEqual("???", panel.OpenTitle, "locked hero stays hidden");
            panel.CloseEntry();
            Assert.IsFalse(panel.IsShowingEntry);

            panel.ShowTab(CodexCategory.Enemy);
            Assert.AreEqual(CodexPanel.EntriesPerPage + 2, panel.EntryCount);
            panel.StepPage(+1);
            Assert.AreEqual(1, panel.Page);
            panel.StepPage(+1);
            Assert.AreEqual(0, panel.Page, "wraps around");
            panel.OpenEntry(0);
            Assert.AreEqual("???", panel.OpenTitle, "undefeated foes are hidden");
            yield return null;

            panel.ShowTab(CodexCategory.Place);
            Assert.AreEqual(1, panel.EntryCount);
            Assert.IsFalse(panel.IsShowingEntry, "switching tabs returns to the list");
        }

        [Test]
        public void HeroStats_ListMovesAndKit()
        {
            var h = Make<CharacterDefinition>();
            h.maxHealth = 6; h.weight = 1.6f; h.guardPips = 4;
            h.moveset = Make<Moveset>();
            var jab = Make<MoveDefinition>(); jab.displayName = "Jab";
            var sweep = Make<MoveDefinition>(); sweep.displayName = "Sweep";
            jab.followUp = sweep;
            sweep.followUp = jab; // a loop must not hang the codex
            h.moveset.groundAttack = jab;
            var kit = Make<NovaKitDefinition>();
            kit.airDodgeName = "Blink"; kit.diveName = "Plunge"; kit.hasDive = true;
            h.kit = kit;

            string stats = CodexPanel.HeroStats(h);
            StringAssert.Contains("HEALTH 6", stats);
            StringAssert.Contains("HEAVY", stats);
            StringAssert.Contains("ATTACK: Jab > Sweep", stats);
            StringAssert.Contains("AIR SPECIAL: Plunge", stats);
            StringAssert.Contains("AIR DODGE: Blink", stats);
        }
    }
}
