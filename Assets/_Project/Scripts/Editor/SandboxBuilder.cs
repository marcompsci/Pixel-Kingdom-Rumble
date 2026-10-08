using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace PKR.EditorTools
{
    /// <summary>
    /// PKR > Build Movement Sandbox: generates a test course for tuning Nova's movement and touch controls.
    /// Safe to re-run; it rebuilds the scene from scratch (your tuning lives in the Nova data assets, not the scene).
    /// Course, left to right: flat run, step platforms, tall wall (air dash over it), one-way platform,
    /// high ledge (meteor drop down from it), a gap, then a second runway.
    /// </summary>
    public static class SandboxBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Sandbox/" + SceneIds.MovementSandbox + ".unity";
        static readonly Color SkyColor = new Color32(126, 200, 227, 255);

        [MenuItem("PKR/Build Movement Sandbox", priority = 20)]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildNoPrompt();
        }

        /// <summary>Build without the save prompt (used by PKR > Build All Scenes).</summary>
        public static void BuildNoPrompt()
        {

            EditorUtil.EnsureLayers();
            // New scene FIRST: opening a scene unloads assets loaded before it, and Unity 6.6 then throws
            // "has been destroyed" when a builder still holds them (seen in the first batch build).
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var nova = DataAssets.GetOrCreateNova();
            var groundSprite = PlaceholderArt.Ground();
            var stoneSprite = PlaceholderArt.StoneBlock();
            var oneWaySprite = PlaceholderArt.OneWay();
            var mat = EditorUtil.UnlitSpriteMaterial();


            // --- Camera -----------------------------------------------------------------------
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = SkyColor;
            camGo.transform.position = new Vector3(0f, 2f, -10f);
            var follow = camGo.AddComponent<CameraFollow2D>();

            // --- Course -----------------------------------------------------------------------
            var level = new GameObject("Course").transform;
            Block(level, "Floor_A", groundSprite, mat, new Vector2(15f, -1f), new Vector2(50f, 2f));
            Block(level, "Floor_B", groundSprite, mat, new Vector2(57f, -1f), new Vector2(26f, 2f));          // 4-unit gap at x 40..44
            Block(level, "Wall_Left", stoneSprite, mat, new Vector2(-10.5f, 5f), new Vector2(1f, 14f));
            Block(level, "Wall_Right", stoneSprite, mat, new Vector2(70.5f, 5f), new Vector2(1f, 14f));
            Block(level, "Step_1", stoneSprite, mat, new Vector2(8f, 2f), new Vector2(4f, 1f));
            Block(level, "Step_2", stoneSprite, mat, new Vector2(13f, 4f), new Vector2(3f, 1f));
            Block(level, "Step_High", stoneSprite, mat, new Vector2(18f, 6.5f), new Vector2(3f, 1f));       // full-height jump from Step_2
            Block(level, "Tall_Wall", stoneSprite, mat, new Vector2(24.5f, 2.5f), new Vector2(1f, 5f));      // taller than a jump (3.2); jump + upward air dash
            OneWay(level, "OneWay_1", oneWaySprite, mat, new Vector2(30f, 2.5f), 5f);
            Block(level, "Ledge_High", stoneSprite, mat, new Vector2(36f, 7f), new Vector2(4f, 1f));         // meteor drop from here
            OneWay(level, "OneWay_ToLedge", oneWaySprite, mat, new Vector2(33f, 4.75f), 3f);
            Block(level, "Gap_Marker", stoneSprite, mat, new Vector2(46f, 1.5f), new Vector2(2f, 1f));

            // --- Nova -------------------------------------------------------------------------
            var spawn = new Vector2(0f, 1.5f);
            var hero = BuildHero(nova, mat, spawn, DataAssets.GetOrCreateRoster());
            var respawn = hero.AddComponent<RespawnOnFall>();
            respawn.respawnPoint = spawn;
            respawn.killY = -12f;
            follow.target = hero.transform;

            // --- Training dummies ----------------------------------------------------------------
            // Story dummy (HP, armor-free) to the right; Arena dummy (Guard Pips) to the left.
            BuildDummy("Dummy_StoryHP", mat, new Vector2(4.5f, 1.5f), DamageModel.StoryHealth, 6);
            BuildDummy("Dummy_GuardPips", mat, new Vector2(-5f, 1.5f), DamageModel.ArenaPips, 0);

            // --- Input + UI -------------------------------------------------------------------
            // The module assigns its default UI actions itself when added in the Editor.
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var touch = new GameObject("TouchControls").AddComponent<TouchControlsUI>();
            EditorUtil.SetField(hero.GetComponent<PlayerInputRouter>(), "touchControls", touch);

            new GameObject("PauseMenu").AddComponent<PauseMenu>();
            var dbg = new GameObject("DebugPanel").AddComponent<SandboxDebugPanel>();
            dbg.motor = hero.GetComponent<PlatformerMotor2D>();

            // --- Save -------------------------------------------------------------------------
            EditorUtil.EnsureFolder(Path.GetDirectoryName(ScenePath).Replace('\\', '/'));
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorUtil.AddSceneToBuild(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[PKR] Movement sandbox built at {ScenePath}. Press Play. Keyboard: WASD/arrows, Space jump, " +
                      "J attack, L special (Comet Bolt / Meteor Drop), Shift dodge. Device Simulator shows touch controls.");
        }

        /// <summary>Shared fighter body: rigidbody, capsule, motor, hurtbox, damageable, visuals.</summary>
        static GameObject BuildFighterBase(string name, int layer, CharacterDefinition def, Sprite sprite, Material mat,
                                           Vector2 position, DamageModel model, int team)
        {
            var go = new GameObject(name);
            go.layer = layer;
            go.transform.position = position;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = go.AddComponent<CapsuleCollider2D>();
            col.direction = CapsuleDirection2D.Vertical;
            col.size = new Vector2(0.7f, 1.4f);
            col.sharedMaterial = NoFrictionMaterial();

            go.AddComponent<Invulnerability>();
            var motor = go.AddComponent<PlatformerMotor2D>();
            if (def != null) EditorUtil.SetField(motor, "definition", def);
            EditorUtil.SetField(motor, "bodyCollider", col);
            EditorUtil.SetLayerMask(motor, "groundMask", 1 << PKRLayers.Ground);

            var dmg = go.AddComponent<Damageable>();
            EditorUtil.SetInt(dmg, "model", (int)model);
            EditorUtil.SetInt(dmg, "team", team);

            // Hurtbox: trigger child on the Hurtbox layer, slightly smaller than the body.
            var hb = new GameObject("Hurtbox") { layer = PKRLayers.Hurtbox };
            hb.transform.SetParent(go.transform, false);
            var hbCol = hb.AddComponent<CapsuleCollider2D>();
            hbCol.isTrigger = true;
            hbCol.size = new Vector2(0.75f, 1.35f);
            var hurt = hb.AddComponent<Hurtbox>();
            EditorUtil.SetField(hurt, "owner", dmg);

            // Visual child: bottom-pivot sprite placed at the collider's feet.
            var body = new GameObject("Body");
            body.transform.SetParent(go.transform, false);
            body.transform.localPosition = new Vector3(0f, -0.7f, 0f);
            var sr = body.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            if (mat != null) sr.sharedMaterial = mat;
            sr.sortingOrder = 10;

            var visual = go.AddComponent<FighterVisual>();
            EditorUtil.SetField(visual, "body", body.transform);
            EditorUtil.SetField(visual, "bodyRenderer", sr);
            var flash = go.AddComponent<HitFlash>();
            EditorUtil.SetField(flash, "target", sr);
            go.AddComponent<StatusPips>();
            return go;
        }

        /// <summary>
        /// The player fighter, baked with def (Nova). With a roster, a SelectedHeroLoader swaps in the hero picked in
        /// Character Select at runtime, so one scene serves every hero.
        /// </summary>
        public static GameObject BuildHero(CharacterDefinition def, Material mat, Vector2 position, CharacterRoster roster = null)
        {
            var go = BuildFighterBase(def.displayName, PKRLayers.Player, def, def.bodySprite, mat, position,
                                      DamageModel.StoryHealth, TeamIds.Player);
            EditorUtil.SetFloat(go.GetComponent<Damageable>(), "postHitInvulnerability", 1f);

            var abilities = go.AddComponent<HeroAbilities>();
            EditorUtil.SetField(abilities, "kit", def.kit);

            var attacks = go.AddComponent<AttackRunner>();
            EditorUtil.SetField(attacks, "moveset", def.moveset);
            EditorUtil.SetInt(attacks, "team", TeamIds.Player);

            var router = go.AddComponent<PlayerInputRouter>();
            EditorUtil.SetField(router, "motor", go.GetComponent<PlatformerMotor2D>());
            go.AddComponent<PlayerMarker>();
            if (roster != null)
            {
                var loader = go.AddComponent<SelectedHeroLoader>();
                EditorUtil.SetField(loader, "roster", roster);
            }
            return go;
        }

        public static GameObject BuildDummy(string name, Material mat, Vector2 position, DamageModel model, int health)
        {
            var go = BuildFighterBase(name, PKRLayers.Enemy, null, PlaceholderArt.Dummy(), mat, position,
                                      model, TeamIds.Neutral);
            var dmg = go.GetComponent<Damageable>();
            EditorUtil.SetInt(dmg, "maxHealthOverride", health);
            EditorUtil.SetFloat(dmg, "fallbackWeight", 1.1f);
            go.AddComponent<TrainingDummy>();
            return go;
        }

        internal static PhysicsMaterial2D NoFrictionMaterial()
        {
            const string path = "Assets/_Project/Data/Settings/PM_NoFriction.physicsMaterial2D";
            var m = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
            if (m != null) return m;
            EditorUtil.EnsureFolder("Assets/_Project/Data/Settings");
            m = new PhysicsMaterial2D("PM_NoFriction") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        internal static GameObject Block(Transform parent, string name, Sprite sprite, Material mat, Vector2 center, Vector2 size)
        {
            var go = new GameObject(name);
            go.layer = PKRLayers.Ground;
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = size;
            if (mat != null) sr.sharedMaterial = mat;
            var box = go.AddComponent<BoxCollider2D>();
            box.size = size;
            return go;
        }

        internal static GameObject OneWay(Transform parent, string name, Sprite sprite, Material mat, Vector2 center, float width)
        {
            // Sprite is 1 unit tall with the plank in its top third; collider matches the plank only.
            var go = Block(parent, name, sprite, mat, center, new Vector2(width, 1f));
            var box = go.GetComponent<BoxCollider2D>();
            box.size = new Vector2(width, 0.3f);
            box.offset = new Vector2(0f, 0.35f);
            box.usedByEffector = true;
            var eff = go.AddComponent<PlatformEffector2D>();
            eff.useOneWay = true;
            eff.surfaceArc = 170f;
            return go;
        }
    }
}
