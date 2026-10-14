using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PKR.EditorTools
{
    /// <summary>
    /// Hooks the generated pixel art (Tools/art/build_all.py, all original) into the game:
    /// fighter animation frames + signature weapons + projectile art, per-world tile sets, parallax backdrops
    /// and the HD post-processing (bloom, vignette, colour grade). Safe to run on every build.
    /// </summary>
    public static class PixelArt
    {
        public const string Root = "Assets/_Project/Art/Pixel";
        const string ProfilePath = "Assets/_Project/Settings/Rendering/PKR_PostFX.asset";

        static readonly Dictionary<string, string> Weapons = new Dictionary<string, string>
        {
            { "nova", "Comet Baton" }, { "brick", "Quarry Fists" }, { "luma", "Spark Wrench" }, { "rex_rollo", "Rink Stick" },
            { "kai_tempest", "Gale Fans" }, { "mara_vex", "Twin Kunai" }, { "volt_ramirez", "Thunder Gloves" },
            { "sola_brightwing", "Dawn Spear" }, { "grizz", "Timber Mallet" }, { "pixel_pip", "Gadget Blaster" },
            { "nyx_frost", "Rime Staff" }, { "blaze_torres", "Rescue Axe" }, { "juno_strike", "Strike Ball" },
            { "ollie_kickflip", "Grind Deck" },
        };

        public static string WeaponName(string id) => id != null && Weapons.TryGetValue(id, out var w) ? w : "";

        // ---- fighters ----------------------------------------------------------------------------------

        /// <summary>Assigns animation frames, the weapon name and projectile art to one hero (if its art exists).</summary>
        public static void ApplyFighter(CharacterDefinition def)
        {
            if (def == null || string.IsNullOrEmpty(def.id)) return;
            string folder = $"{Root}/Fighters/{def.id}";
            if (!AssetDatabase.IsValidFolder(folder)) return;
            if (def.frames == null) def.frames = new FighterFrames();
            var f = def.frames;
            bool dirty = false;
            dirty |= Assign(ref f.idle, Load(folder, def.id, "idle"));
            dirty |= Assign(ref f.run, Load(folder, def.id, "run"));
            dirty |= Assign(ref f.jump, Load(folder, def.id, "jump"));
            dirty |= Assign(ref f.fall, Load(folder, def.id, "fall"));
            dirty |= Assign(ref f.attack, Load(folder, def.id, "attack"));
            dirty |= Assign(ref f.special, Load(folder, def.id, "special"));
            dirty |= Assign(ref f.hurt, Load(folder, def.id, "hurt"));
            string weapon = WeaponName(def.id);
            if (f.weaponName != weapon) { f.weaponName = weapon; dirty = true; }
            if (dirty) EditorUtility.SetDirty(def);

            var shot = AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/FX/shot_{def.id}.png");
            if (shot != null && def.moveset != null)
                foreach (var m in AllMoves(def.moveset))
                    if (m.spawnsProjectile && m.projectileSprite != shot)
                    {
                        m.projectileSprite = shot;
                        EditorUtility.SetDirty(m);
                    }
        }

        // ---- enemies and bosses -----------------------------------------------------------------------

        /// <summary>Art folder name for an enemy: its id without the "enemy_" prefix (enemy_cog_beetle -> cog_beetle).</summary>
        public static string EnemyArtName(EnemyDefinition def) =>
            def == null || string.IsNullOrEmpty(def.id) ? "" : (def.id.StartsWith("enemy_") ? def.id.Substring(6) : def.id);

        /// <summary>Assigns walk / idle / air / hurt frames to an enemy (if its art exists).</summary>
        public static void ApplyEnemy(EnemyDefinition def)
        {
            string name = EnemyArtName(def);
            if (name.Length == 0) return;
            string folder = $"{Root}/Enemies/{name}";
            if (!AssetDatabase.IsValidFolder(folder)) return;
            if (def.frames == null) def.frames = new FighterFrames();
            var f = def.frames;
            bool dirty = false;
            dirty |= Assign(ref f.idle, Load(folder, name, "idle"));
            dirty |= Assign(ref f.run, Load(folder, name, "run"));
            dirty |= Assign(ref f.jump, Load(folder, name, "jump"));
            dirty |= Assign(ref f.fall, Load(folder, name, "fall"));
            dirty |= Assign(ref f.hurt, Load(folder, name, "hurt"));
            if (dirty) EditorUtility.SetDirty(def);
        }

        public static readonly string[] BossStates = { "idle_0", "idle_1", "angry", "dazed", "hurt" };

        /// <summary>Fills a boss's Sprite[] field with Bosses/{prefix}_{state}.png in BossStates order.</summary>
        public static void ApplyBossFrames(Object boss, string field, string prefix)
        {
            var so = new SerializedObject(boss);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[PKR] Field '{field}' not found on {boss.GetType().Name}"); return; }
            p.arraySize = BossStates.Length;
            for (int i = 0; i < BossStates.Length; i++)
                p.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/Bosses/{prefix}_{BossStates[i]}.png");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static bool Assign(ref Sprite[] slot, Sprite[] value)
        {
            if (value.Length == 0) return false;
            if (slot != null && slot.Length == value.Length)
            {
                bool same = true;
                for (int i = 0; i < value.Length; i++) same &= slot[i] == value[i];
                if (same) return false;
            }
            slot = value;
            return true;
        }

        static Sprite[] Load(string folder, string id, string anim)
        {
            var list = new List<Sprite>();
            for (int i = 0; i < 16; i++)
            {
                var s = AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/{id}_{anim}_{i}.png");
                if (s == null) break;
                list.Add(s);
            }
            return list.ToArray();
        }

        public static IEnumerable<MoveDefinition> AllMoves(Moveset ms)
        {
            var seen = new HashSet<MoveDefinition>();
            var stack = new Stack<MoveDefinition>();
            foreach (var m in new[] { ms.groundAttack, ms.groundUpAttack, ms.airAttack, ms.groundSpecial, ms.airSpecial,
                                      ms.sideSpecial, ms.downSpecial, ms.abilityImpact })
                if (m != null) stack.Push(m);
            while (stack.Count > 0)
            {
                var m = stack.Pop();
                if (m == null || !seen.Add(m)) continue;
                yield return m;
                if (m.followUp != null) stack.Push(m.followUp);
                if (m.specialFollowUp != null) stack.Push(m.specialFollowUp);
            }
        }

        // ---- worlds ------------------------------------------------------------------------------------

        static readonly Dictionary<string, string> BiomeThemes = new Dictionary<string, string>
        {
            { "Sunspire Meadows", "meadow" }, { "Sky Gardens", "garden" }, { "Brasslight Quarter", "city" },
            { "Ember Wastes", "ember" }, { "Frost Citadel", "frost" }, { "Frostgear Peaks", "frost" },
            { "Shadow Spire", "spire" }, { "Sovereign's Core", "spire" }, { "Shattered Crossing", "shattered" },
            { "Sky Foundry", "foundry" }, { "Tickworks Engine Hall", "foundry" }, { "Sunken Clockworks", "sunken" },
            { "Toy Fortress", "toy" }, { "Under the Isles", "cave" },
        };

        /// <summary>Art theme for a level's biome ("meadow" when unknown).</summary>
        public static string ThemeFor(string biome) =>
            biome != null && BiomeThemes.TryGetValue(biome, out var t) ? t : "meadow";

        /// <summary>A world tile: kind = top / fill / stone / plank. Null if the art is missing.</summary>
        public static Sprite Tile(string theme, string kind) =>
            AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/Tiles/{theme}_{kind}.png");

        public static bool HasTiles(string theme) => Tile(theme, "top") != null && Tile(theme, "fill") != null;

        /// <summary>
        /// Solid ground for map cells x..x+w-1, y..y+h-1 (y up): a tiled body plus a one-unit surface cap
        /// (grass / snow / roof) over every column whose cell above is open, and the collider.
        /// solid(x, y) tells whether a map cell is solid; null = the whole top row is exposed.
        /// stone = brick / plate / crystal blocks instead of earth.
        /// </summary>
        public static GameObject Ground(Transform parent, string name, string theme, Material mat, int x, int y, int w, int h,
                                        System.Func<int, int, bool> solid = null, bool stone = false)
        {
            var center = new Vector2(x + w * 0.5f, y + h * 0.5f);
            var size = new Vector2(w, h);
            var go = new GameObject(name) { layer = PKRLayers.Ground };
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;

            if (stone)
            {
                Piece(go.transform, "Art", Tile(theme, "stone"), mat, Vector2.zero, size, 0);
                return go;
            }
            Piece(go.transform, "Body", Tile(theme, "fill"), mat, Vector2.zero, size, 0);
            int top = y + h - 1;
            int runStart = -1;
            for (int cx = x; cx <= x + w; cx++)
            {
                bool exposed = cx < x + w && (solid == null || !solid(cx, top + 1));
                if (exposed && runStart < 0) runStart = cx;
                if (!exposed && runStart >= 0)
                {
                    int len = cx - runStart;
                    var local = new Vector2(runStart + len * 0.5f - center.x, top + 0.5f - center.y);
                    Piece(go.transform, "Cap", Tile(theme, "top"), mat, local, new Vector2(len, 1f), 1);
                    runStart = -1;
                }
            }
            return go;
        }

        static SpriteRenderer Piece(Transform parent, string name, Sprite sprite, Material mat, Vector2 local, Vector2 size, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = size;
            sr.sortingOrder = order;
            if (mat != null) sr.sharedMaterial = mat;
            return sr;
        }

        // ---- backdrops ---------------------------------------------------------------------------------

        struct LayerSpec { public string key; public float fx, fy, dy, shade; public int order; }

        static readonly LayerSpec[] Layers =
        {
            new LayerSpec { key = "far", fx = 0.88f, fy = 0.85f, dy = -1.2f, shade = 0.85f, order = -90 },
            new LayerSpec { key = "clouds", fx = 0.94f, fy = 0.9f, dy = 1.5f, shade = 1f, order = -88 },
            new LayerSpec { key = "mid", fx = 0.7f, fy = 0.65f, dy = -1.6f, shade = 0.7f, order = -80 },
            new LayerSpec { key = "near", fx = 0.45f, fy = 0.4f, dy = -2.0f, shade = 0.5f, order = -70 },
        };

        /// <summary>
        /// Sky gradient (from the level's sky colour) + far / clouds / mid / near parallax layers for a theme.
        /// groundY = roughly where the level's floor is at the camera's start.
        /// </summary>
        public static Transform Backdrop(string theme, Color sky, float groundY, Material mat, Vector2 cameraStart)
        {
            var root = new GameObject("Backdrop").transform;
            root.position = Vector3.zero;

            var skyGo = new GameObject("Sky");
            skyGo.transform.SetParent(root, false);
            var grad = skyGo.AddComponent<SkyGradient>();
            Color.RGBToHSV(sky, out float h, out float s, out float v);
            grad.top = Color.HSVToRGB(h, Mathf.Min(1f, s * 1.1f), v * 0.62f);
            grad.bottom = Color.Lerp(sky, Color.white, 0.22f);
            grad.material = mat;

            foreach (var spec in Layers)
            {
                string path = $"{Root}/Backgrounds/{theme}_{spec.key}.png";
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null) continue;
                var go = new GameObject("Layer_" + spec.key);
                go.transform.SetParent(root, false);
                go.transform.position = new Vector3(cameraStart.x, groundY + spec.dy, 0f);
                var layer = go.AddComponent<ParallaxLayer>();
                layer.sprite = sprite;
                layer.material = mat;
                layer.factorX = spec.fx;
                layer.factorY = spec.fy;
                layer.sortingOrder = spec.order;
                // Below the strip: the art's bottom colour, darker for nearer layers so the void under the
                // terrain reads as deep shadow rather than a flat band.
                var bottom = spec.key == "clouds" ? Color.clear : BottomColor(path);
                if (bottom.a > 0f) bottom = new Color(bottom.r * spec.shade, bottom.g * spec.shade, bottom.b * spec.shade, 1f);
                layer.baseColor = bottom;
            }
            return root;
        }

        /// <summary>Average opaque colour of the art's bottom row (fills below the layer).</summary>
        static Color BottomColor(string assetPath)
        {
            var bytes = File.ReadAllBytes(assetPath);
            var tex = new Texture2D(2, 2);
            if (!tex.LoadImage(bytes)) { Object.DestroyImmediate(tex); return Color.clear; }
            Color sum = Color.black;
            int n = 0, width = tex.width;
            for (int x = 0; x < width; x++)
            {
                var c = tex.GetPixel(x, 0);
                if (c.a < 0.9f) continue;
                sum += c;
                n++;
            }
            Object.DestroyImmediate(tex);
            if (n < width / 2) return Color.clear;
            var avg = sum / n;
            avg.a = 1f;
            return avg;
        }

        // ---- HD post-processing ------------------------------------------------------------------------------

        /// <summary>Bloom (glows on gems, lava, portals), a soft vignette and a light colour grade.</summary>
        public static void PostFX(Camera cam)
        {
            if (cam == null) return;
            var data = cam.GetUniversalAdditionalCameraData();
            if (data != null) data.renderPostProcessing = true;

            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                EditorUtil.EnsureFolder(Path.GetDirectoryName(ProfilePath).Replace('\\', '/'));
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }
            bool changed = false;
            if (!profile.TryGet(out Bloom bloom))
            {
                bloom = profile.Add<Bloom>(true);
                AssetDatabase.AddObjectToAsset(bloom, profile);
                changed = true;
            }
            bloom.threshold.Override(0.82f);
            bloom.intensity.Override(0.7f);
            bloom.scatter.Override(0.6f);
            if (!profile.TryGet(out Vignette vignette))
            {
                vignette = profile.Add<Vignette>(true);
                AssetDatabase.AddObjectToAsset(vignette, profile);
                changed = true;
            }
            vignette.intensity.Override(0.2f);
            vignette.smoothness.Override(0.45f);
            if (!profile.TryGet(out ColorAdjustments grade))
            {
                grade = profile.Add<ColorAdjustments>(true);
                AssetDatabase.AddObjectToAsset(grade, profile);
                changed = true;
            }
            grade.saturation.Override(10f);
            grade.contrast.Override(8f);
            EditorUtility.SetDirty(profile);
            if (changed) AssetDatabase.SaveAssets();

            var go = new GameObject("PostFX");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
        }
    }
}
