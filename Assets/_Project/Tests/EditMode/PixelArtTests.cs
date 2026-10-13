using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace PKR.Tests
{
    /// <summary>The generated pixel art (Tools/art/build_all.py) is present and imported at the right scale.</summary>
    public class PixelArtTests
    {
        const string Root = "Assets/_Project/Art/Pixel";

        static readonly string[] Heroes =
        {
            "nova", "brick", "luma", "rex_rollo", "kai_tempest", "mara_vex", "volt_ramirez", "sola_brightwing", "grizz",
            "pixel_pip", "nyx_frost", "blaze_torres", "juno_strike", "ollie_kickflip"
        };

        static readonly (string anim, int count)[] Clips =
            { ("idle", 4), ("run", 6), ("jump", 1), ("fall", 1), ("attack", 3), ("special", 2), ("hurt", 1) };

        [Test]
        public void EveryHero_HasEveryAnimationFrame_At32PixelsPerUnit()
        {
            foreach (var id in Heroes)
                foreach (var (anim, count) in Clips)
                    for (int i = 0; i < count; i++)
                    {
                        string path = $"{Root}/Fighters/{id}/{id}_{anim}_{i}.png";
                        var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                        Assert.IsNotNull(s, path);
                        Assert.AreEqual(32f, s.pixelsPerUnit, 0.01f, path);
                        Assert.AreEqual(1.5f, s.bounds.size.y, 0.01f, path + " should be 1.5 units tall");
                        Assert.AreEqual(0f, s.bounds.min.y, 0.01f, path + " should pivot at the feet");
                    }
        }

        [Test]
        public void EveryWorldTheme_HasTilesAndBackdropLayers()
        {
            foreach (var theme in new[] { "meadow", "garden", "city", "ember", "frost", "spire", "shattered", "foundry", "sunken", "toy", "cave" })
            {
                foreach (var kind in new[] { "top", "fill", "stone", "plank" })
                {
                    var s = AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/Tiles/{theme}_{kind}.png");
                    Assert.IsNotNull(s, theme + " " + kind);
                    Assert.AreEqual(1f, s.bounds.size.x, 0.01f, $"{theme}_{kind} should be one unit wide");
                }
                foreach (var layer in new[] { "far", "mid", "near" })
                    Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/Backgrounds/{theme}_{layer}.png"), theme + " " + layer);
            }
        }

        [Test]
        public void RedrawnLegacySprites_KeepTheirWorldSize()
        {
            // Drawn at 2x and imported at 32 PPU, so colliders and layouts built around the old art still fit.
            var ground = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Placeholder/Sprites/ph_ground.png");
            var nova = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Placeholder/Sprites/ph_nova.png");
            var gate = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Placeholder/Sprites/ph_goal_gate.png");
            Assert.AreEqual(new Vector2(1f, 1f), (Vector2)ground.bounds.size);
            Assert.AreEqual(new Vector2(1f, 1.5f), (Vector2)nova.bounds.size);
            Assert.AreEqual(new Vector2(2f, 3f), (Vector2)gate.bounds.size);
        }

        [Test]
        public void EffectsLoadFromResources()
        {
            Assert.IsNotNull(Resources.Load<Sprite>("PKR/fx_glow"));
            Assert.IsNotNull(Resources.Load<Sprite>("PKR/fx_shadow"));
        }

        [Test]
        public void FighterFramesPick_LoopsAndHandlesEmpty()
        {
            var a = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
            var b = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
            var clip = new[] { a, b };
            Assert.AreSame(a, FighterFrames.Pick(clip, 0));
            Assert.AreSame(b, FighterFrames.Pick(clip, 3));
            Assert.AreSame(b, FighterFrames.Pick(clip, -1));
            Assert.IsNull(FighterFrames.Pick(new Sprite[0], 2));
            Assert.IsNull(FighterFrames.Pick(null, 0));
        }
    }
}
