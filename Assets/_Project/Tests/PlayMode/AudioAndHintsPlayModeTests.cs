using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PKR.Tests
{
    public class AudioAndHintsPlayModeTests
    {
        static readonly string[] Clips =
        {
            Sfx.Jump, Sfx.Land, Sfx.Hit, Sfx.HitHeavy, Sfx.Hurt, Sfx.Swing, Sfx.Shard, Sfx.Relic, Sfx.Heal, Sfx.Checkpoint,
            Sfx.Spotted, Sfx.Takedown, Sfx.Spring, Sfx.Crate, Sfx.Click, Sfx.Secret, Sfx.KO, Sfx.Victory, Sfx.Purchase,
            Sfx.MusicStory, Sfx.MusicStealth, Sfx.MusicVersus
        };

        [Test]
        public void EverySoundClip_LoadsFromResources()
        {
            foreach (var name in Clips)
            {
                var clip = Sfx.Clip(name);
                Assert.IsNotNull(clip, "missing Resources/Audio/" + name + ".wav");
                Assert.Greater(clip.length, 0.01f, name);
            }
        }

        [Test]
        public void MusicFor_PicksTheRightLoop()
        {
            Assert.AreEqual(Sfx.MusicStory, AudioDirector.MusicFor(SceneIds.MainMenu));
            Assert.AreEqual(Sfx.MusicStory, AudioDirector.MusicFor(SceneIds.StoryTest));
            Assert.AreEqual(Sfx.MusicStealth, AudioDirector.MusicFor("MS_FirstLight"));
            Assert.AreEqual(Sfx.MusicStealth, AudioDirector.MusicFor(SceneIds.RooftopRun));
            Assert.AreEqual(Sfx.MusicVersus, AudioDirector.MusicFor(SceneIds.VersusStage));
            Assert.AreEqual(Sfx.MusicVersus, AudioDirector.MusicFor(SceneIds.ArenaTest));
            Assert.IsNull(AudioDirector.MusicFor(SceneIds.Boot));
        }

        [Test]
        public void Sfx_WithoutServices_DoesNothing()
        {
            Assert.DoesNotThrow(() => Sfx.Play(Sfx.Jump));
            Assert.DoesNotThrow(() => Sfx.Play("no_such_clip"));
        }

        [UnityTest]
        public IEnumerator HintTexts_AllExist_AndShowOnlyOnce()
        {
            foreach (var id in new[] { FirstTimeHints.Move, FirstTimeHints.Attack, FirstTimeHints.Dodge, FirstTimeHints.Climb,
                                       FirstTimeHints.Stealth, FirstTimeHints.Takedown, FirstTimeHints.Relics, FirstTimeHints.Checkpoint })
                Assert.IsTrue(FirstTimeHints.Texts.ContainsKey(id), id);
            // Without a save service there is nowhere to remember a hint, so nothing is shown.
            if (Services.Save == null) Assert.IsFalse(FirstTimeHints.Show(FirstTimeHints.Climb));
            yield return null;
        }
    }
}
