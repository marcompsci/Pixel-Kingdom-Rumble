using System.Collections.Generic;
using System.IO;
using PKR.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace PKR.EditorTools
{
    /// <summary>
    /// PKR > Build Map Levels: turns the text maps in StoryLayouts into Story Quest scenes (see AsciiLevel for the
    /// legend). One map cell is one world unit; cell (x, y) covers [x, x+1] x [y, y+1], and things that stand on the
    /// ground have their feet at y. Two jump-and-run levels (Sunspire Heights, Gearfall Caverns) and two stealth
    /// levels (Rooftop Run, Night Market Heist). Part of PKR > Build All Scenes.
    /// </summary>
    public static class AsciiLevelBuilder
    {
        public const string Folder = "Assets/_Project/Scenes/Story";
        public const string ContractFolder = "Assets/_Project/Scenes/Contracts";
        public const string EventFolder = "Assets/_Project/Scenes/Events";

        static Material _mat;
        static Transform _root;
        static int _shards, _crates;
        static Sprite _ground, _stone, _oneWay, _plate, _spikes, _flag, _gate, _gear, _crate, _crateEmpty,
                      _vine, _hay, _water, _spring, _ledger, _shard, _scroll;
        static Sprite[] _relics;
        static int _relicIndex;

        /// <summary>Shadow Contracts extras for one map: ids, what to steal, and where to save scores.</summary>
        public class ContractOptions
        {
            public string contractId, clueId, objectiveName = "the relic", objectiveDisplay = "Ancient Relic";
            public Sprite objectiveSprite;
        }

        static ContractOptions _contract;

        /// <summary>Holiday / Halloween look for one map: the collectible art, which foes replace the usual ones, and
        /// whether the zombie horde chases from the left.</summary>
        public class EventStyle
        {
            public string treat;
            public readonly Dictionary<char, System.Func<EnemyDefinition>> foes = new Dictionary<char, System.Func<EnemyDefinition>>();
            public bool horde;
        }

        static EventStyle _style;
        // Zombie Survival pieces found while building a survival map.
        static readonly List<Transform> _windowSpawns = new List<Transform>(), _roofSpawns = new List<Transform>();
        static readonly List<ZombieStation> _stations = new List<ZombieStation>();
        static readonly List<Barricade> _barricades = new List<Barricade>();
        static int _wallIndex, _perkIndex;

        /// <summary>The usual foe for a map character, or the event's replacement.</summary>
        static EnemyDefinition Foe(char c, System.Func<EnemyDefinition> usual) =>
            _style != null && _style.foes.TryGetValue(c, out var f) ? f() : usual();

        static Sprite EventArt(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{PixelArt.Root}/Events/{name}.png");
        static readonly string[] RelicNames = { "Sun Idol", "Jade Mask", "Moon Amulet", "Gilded Gear", "Storm Coin", "Star Chalice" };

        [MenuItem("PKR/Build Map Levels", priority = 23)]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildNoPrompt();
        }

        public static void BuildNoPrompt()
        {
            BuildLevel(SceneIds.SunspireHeights, StoryLayouts.SunspireHeights, () => DataAssets.GetOrCreateSunspireHeights());
            BuildLevel(SceneIds.GearfallCaverns, StoryLayouts.GearfallCaverns, () => DataAssets.GetOrCreateGearfallCaverns());
            BuildLevel(SceneIds.RooftopRun, StoryLayouts.RooftopRun, () => DataAssets.GetOrCreateRooftopRun());
            BuildLevel(SceneIds.NightMarketHeist, StoryLayouts.NightMarketHeist, () => DataAssets.GetOrCreateNightMarketHeist());
            BuildLevel(SceneIds.FrostgearAscent, StoryLayouts.FrostgearAscent, () => DataAssets.GetOrCreateFrostgearAscent());
            BuildLevel(SceneIds.ClockworkGlacier, StoryLayouts.ClockworkGlacier, () => DataAssets.GetOrCreateClockworkGlacier());
            // Story world 3: The Shattered Realms (the boss arena is built by BossSceneBuilder).
            BuildLevel(SceneIds.EmberWastes, StoryLayouts.EmberWastes, () => DataAssets.GetOrCreateEmberWastes());
            BuildLevel(SceneIds.SkyGardens, StoryLayouts.SkyGardens, () => DataAssets.GetOrCreateSkyGardens());
            BuildLevel(SceneIds.SunkenClockworks, StoryLayouts.SunkenClockworks, () => DataAssets.GetOrCreateSunkenClockworks());
            BuildLevel(SceneIds.ToyFortress, StoryLayouts.ToyFortress, () => DataAssets.GetOrCreateToyFortress());
            BuildLevel(SceneIds.ShatteredCrossing, StoryLayouts.ShatteredCrossing, () => DataAssets.GetOrCreateShatteredCrossing());
            // One maze per world.
            BuildLevel(SceneIds.GearworkMaze, MazeLayouts.GearworkMaze, () => DataAssets.GetOrCreateGearworkMaze());
            BuildLevel(SceneIds.FrozenVault, MazeLayouts.FrozenVault, () => DataAssets.GetOrCreateFrozenVault());
            BuildLevel(SceneIds.ShatteredLabyrinth, MazeLayouts.ShatteredLabyrinth, () => DataAssets.GetOrCreateShatteredLabyrinth());
            DataAssets.GetOrCreateStoryWorld(); // keep the level select route in order
            DataAssets.GetOrCreateShatteredRealms();
            BuildEvents();
            AssetDatabase.SaveAssets();
        }

        /// <summary>Holiday Mode (four holiday runs) and Halloween Mode (Haunted Escape, Zombie Survival).</summary>
        public static void BuildEvents()
        {
            var winter = new EventStyle { treat = "collect_gift" };
            winter.foes['B'] = DataAssets.GetOrCreateSnowman;
            BuildLevel(SceneIds.WinterGiftRun, EventLayouts.WinterGiftRun, () => DataAssets.GetOrCreateWinterGiftRun(), style: winter);

            var valentine = new EventStyle { treat = "collect_heart" };
            valentine.foes['M'] = DataAssets.GetOrCreateCupidBot;
            BuildLevel(SceneIds.SweetheartSprint, EventLayouts.SweetheartSprint, () => DataAssets.GetOrCreateSweetheartSprint(), style: valentine);

            var harvest = new EventStyle { treat = "collect_pie" };
            harvest.foes['T'] = DataAssets.GetOrCreateTurkey;
            BuildLevel(SceneIds.HarvestHustle, EventLayouts.HarvestHustle, () => DataAssets.GetOrCreateHarvestHustle(), style: harvest);

            var newYear = new EventStyle { treat = "collect_firework_star" };
            newYear.foes['B'] = DataAssets.GetOrCreateFirecracker;
            newYear.foes['M'] = DataAssets.GetOrCreateCupidBot;
            BuildLevel(SceneIds.CountdownClimb, EventLayouts.CountdownClimb, () => DataAssets.GetOrCreateCountdownClimb(), style: newYear);

            var haunted = new EventStyle { treat = "collect_candy", horde = true };
            haunted.foes['B'] = DataAssets.GetOrCreateZombieShambler;
            haunted.foes['W'] = DataAssets.GetOrCreateZombieRunner;
            haunted.foes['D'] = DataAssets.GetOrCreateZombieBloater;
            haunted.foes['Y'] = DataAssets.GetOrCreateZombieSpitter;
            haunted.foes['K'] = DataAssets.GetOrCreateZombieBrute;
            haunted.foes['M'] = DataAssets.GetOrCreateZombieBat;
            BuildLevel(SceneIds.HauntedEscape, EventLayouts.HauntedEscape, () => DataAssets.GetOrCreateHauntedEscape(), style: haunted);

            BuildLevel(SceneIds.ZombieSurvival, EventLayouts.ZombieArena, () => DataAssets.GetOrCreateZombieSurvivalLevel(),
                       style: new EventStyle());
        }

        /// <summary>Builds every Shadow Contracts scene (MS_*) from ContractLayouts.</summary>
        public static void BuildContracts()
        {
            DataAssets.GetOrCreateMissionBoard(); // first, so every contract scene can point at it
            AssetDatabase.SaveAssets();
            foreach (var spec in DataAssets.ContractSpecs)
            {
                var s = spec;
                BuildLevel(s.sceneName, s.rows, () => DataAssets.GetOrCreateContractLevel(s), new ContractOptions
                {
                    contractId = s.id, clueId = s.containsClue, objectiveName = s.objectiveName, objectiveDisplay = s.objectiveDisplay
                });
            }
            DataAssets.GetOrCreateMissionBoard();
            AssetDatabase.SaveAssets();
        }

        /// <summary>Terrain tint per biome: one tile set, many worlds.</summary>
        public static Color TintFor(string biome)
        {
            switch (biome)
            {
                case "Frostgear Peaks": return new Color(0.78f, 0.9f, 1f);
                case "Frost Citadel": return new Color(0.7f, 0.86f, 1f);
                case "Sky Foundry": return new Color(1f, 0.78f, 0.6f);
                case "Shadow Spire": return new Color(0.66f, 0.56f, 0.9f);
                case "Ember Wastes": return new Color(0.62f, 0.42f, 0.38f);
                case "Sky Gardens": return new Color(0.92f, 1f, 0.86f);
                case "Sunken Clockworks": return new Color(0.55f, 0.82f, 0.8f);
                case "Toy Fortress": return new Color(1f, 0.78f, 0.9f);
                case "Shattered Crossing": return new Color(0.8f, 0.74f, 0.95f);
                default: return Color.white;
            }
        }

        static void BuildLevel(string sceneName, string[] rows, System.Func<LevelDefinition> levelAsset, ContractOptions contract = null,
                               EventStyle style = null)
        {
            _contract = contract;
            _style = style;
            _windowSpawns.Clear(); _roofSpawns.Clear(); _stations.Clear(); _barricades.Clear();
            _wallIndex = _perkIndex = 0;
            _relicIndex = 0;
            var map = new AsciiLevel(rows);
            var errors = map.Validate();
            if (errors.Count > 0)
            {
                Debug.LogError($"[PKR] Map {sceneName} is invalid: {string.Join("; ", errors)}");
                return;
            }

            EditorUtil.EnsureLayers();
            // New scene FIRST: opening a scene unloads assets loaded before it (Unity 6.6 "has been destroyed").
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var level = levelAsset();
            var roster = DataAssets.GetOrCreateRoster();
            var nova = DataAssets.GetOrCreateNova();
            LoadArt();
            if (_style != null && !string.IsNullOrEmpty(_style.treat) && EventArt(_style.treat) != null) _shard = EventArt(_style.treat);
            _shards = 0;
            _crates = 0;
            bool stealth = map.Count('g') > 0 || map.Count('E') > 0 || map.Count('V') > 0;

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = level.skyColor;
            camGo.transform.position = new Vector3(4f, 4f, -10f);
            var follow = camGo.AddComponent<CameraFollow2D>();
            follow.bounds = new Rect(0f, -3f, map.Width, map.Height + 3f);

            _root = new GameObject("Level").transform;

            // --- Backdrop + HD post-processing ------------------------------------------------------------
            string theme = PixelArt.ThemeFor(level.biomeName);
            var startCell = map.First('P');
            float groundY = startCell.HasValue ? startCell.Value.y : 1f;
            PixelArt.Backdrop(theme, level.skyColor, groundY, _mat, new Vector2(4f, 4f));
            PixelArt.PostFX(cam);

            // --- Terrain ---------------------------------------------------------------------------------
            // Each world has its own tile set (grass, snow, brass roofs, basalt...); the tint is the fallback
            // when a world's art is missing.
            bool themed = PixelArt.HasTiles(theme);
            Color terrainTint = themed ? Color.white : TintFor(level.biomeName);
            foreach (var r in map.MergedRects('#'))
            {
                if (themed) PixelArt.Ground(_root, "Ground", theme, _mat, r.x, r.y, r.w, r.h, map.IsSolid);
                else Block("Ground", _ground, r).GetComponent<SpriteRenderer>().color = terrainTint;
            }
            foreach (var r in map.MergedRects('X'))
            {
                if (themed) PixelArt.Ground(_root, "Stone", theme, _mat, r.x, r.y, r.w, r.h, null, stone: true);
                else Block("Stone", _stone, r).GetComponent<SpriteRenderer>().color = terrainTint;
            }
            var plank = themed && PixelArt.Tile(theme, "plank") != null ? PixelArt.Tile(theme, "plank") : _oneWay;
            foreach (var r in map.MergedRects('~')) Water(r);
            foreach (var r in map.MergedRects('L')) Vine(map, r);
            foreach (var run in map.Runs)
            {
                switch (run.tile)
                {
                    case '=':
                        SandboxBuilder.OneWay(_root, "Plank", plank, _mat, new Vector2(run.x + run.length * 0.5f, run.y + 0.5f), run.length);
                        break;
                    case '^':
                        Spikes(map, run);
                        break;
                    case 'H':
                        Hay(run);
                        break;
                    case 'a':
                        Lava(map, run);
                        break;
                    case 'c':
                        Crumble(run, terrainTint);
                        break;
                    case '}':
                    case '{':
                        Conveyor(run, run.tile == '}' ? 1 : -1);
                        break;
                }
            }
            foreach (var r in map.MergedRects('>')) Push(r, ForceKind.Wind, 1);
            foreach (var r in map.MergedRects('<')) Push(r, ForceKind.Wind, -1);
            foreach (var r in map.MergedRects(')')) Push(r, ForceKind.Current, 1);
            foreach (var r in map.MergedRects('(')) Push(r, ForceKind.Current, -1);

            // --- Things ----------------------------------------------------------------------------------
            Vector2 start = Vector2.zero;
            int checkpoint = 0;
            foreach (var p in map.Points)
            {
                float cx = p.x + 0.5f;
                switch (p.tile)
                {
                    case 'P': start = new Vector2(cx, p.y + 0.75f); break;
                    case 'G': Goal(new Vector2(cx, p.y)); break;
                    case 'C': Checkpoint(new Vector2(cx, p.y), checkpoint++); break;
                    case '*': Spot(PickupKind.StarShard, new Vector2(cx, p.y + 0.5f)); break;
                    case '+': Spot(PickupKind.Health, new Vector2(cx, p.y + 0.5f)); break;
                    case '?': Crate(new Vector2(cx, p.y + 0.5f)); break;
                    case 'J': Spring(new Vector2(cx, p.y + 0.5f)); break;
                    case 'O': Objective(new Vector2(cx, p.y + 0.5f)); break;
                    case 'm':
                    {
                        int travel = map.MoverTravel(p.x, p.y);
                        Mover("MovingPlatform", new Vector2(cx, p.y + 0.25f), 3f, new Vector2(travel, 0f), Mathf.Max(1.5f, travel / 3f));
                        break;
                    }
                    case 'u': Mover("Lift", new Vector2(cx, p.y + 0.25f), 3f, new Vector2(0f, 7f), 3f); break;
                    case 'B': Enemy(Foe('B', DataAssets.GetOrCreateCogBeetle), cx, p.y); break;
                    case 'T': Enemy(Foe('T', DataAssets.GetOrCreateSpringTick), cx, p.y); break;
                    case 'M': Enemy(Foe('M', DataAssets.GetOrCreateGyroMoth), cx, p.y + 0.5f); break;
                    case 'K': Enemy(Foe('K', DataAssets.GetOrCreateBoltKnight), cx, p.y); break;
                    case 'F': Enemy(Foe('F', DataAssets.GetOrCreateFrostMite), cx, p.y); break;
                    case 'g': Enemy(DataAssets.GetOrCreateGearwatchSentry(), cx, p.y); break;
                    case 'E': Enemy(DataAssets.GetOrCreateGearwatchElite(), cx, p.y); break;
                    case 'V': Enemy(DataAssets.GetOrCreateGearwatchCaptain(), cx, p.y); break;
                    case 'R': Relic(new Vector2(cx, p.y + 0.5f)); break;
                    case 'Z': Scroll(new Vector2(cx, p.y + 0.5f)); break;
                    case 'f': Geyser(new Vector2(cx, p.y), p.x); break;
                    case 'Y': Enemy(Foe('Y', DataAssets.GetOrCreateCinderCrawler), cx, p.y); break;
                    case 'Q': Enemy(Foe('Q', DataAssets.GetOrCreatePuffGlider), cx, p.y + 0.5f); break;
                    case 'D': Enemy(Foe('D', DataAssets.GetOrCreateRustCrab), cx, p.y); break;
                    case 'W': Enemy(Foe('W', DataAssets.GetOrCreateWindupSoldier), cx, p.y); break;
                    // Zombie Survival
                    case 'z': ZombieSpawn(map, p.x, p.y); break;
                    case 'x': Station(StationKind.WallWeapon, _wallIndex++ % ZombieArmory.Wall.Length, cx, p.y); break;
                    case 'b': Station(StationKind.MysteryCrate, 0, cx, p.y); break;
                    case 'p': Station(StationKind.Perk, _perkIndex++ % 4, cx, p.y); break;
                    case 'U': Station(StationKind.Forge, 0, cx, p.y); break;
                    case 'j': Station(StationKind.Lantern, 0, cx, p.y); break;
                }
            }
            ObstacleBuilder.Build(map, _root, _mat); // crushers, lasers, saws, keys/doors, switch blocks
            foreach (var r in map.MergedRects('w')) BarricadeAt(r);
            foreach (var r in map.MergedRects('q')) PaidDoor(r);
            if (map.HasSecret)
            {
                var min = new Vector2(map.SecretMinX, map.SecretMinY);
                var size = new Vector2(map.SecretMaxX - map.SecretMinX + 1, map.SecretMaxY - map.SecretMinY + 1);
                SecretRoom($"secret_{level.id}", min + size * 0.5f, size);
            }
            if (_style == null)
                for (float x = 12f; x < map.Width - 6; x += 26f) Decor(new Vector2(x, map.Height - 5f), stealth ? 1.6f : 2.6f);
            if (_style != null && _style.horde) Horde(start, groundY);

            // --- Hero, flow, UI --------------------------------------------------------------------------
            var startT = new GameObject("StartPoint").transform;
            startT.position = start;
            var hero = SandboxBuilder.BuildHero(nova, _mat, start, roster);
            follow.target = hero.transform;

            if (map.IsSurvival) Survival(hero, startT);
            else
            {
                var flow = new GameObject("LevelFlow").AddComponent<LevelFlowController>();
                EditorUtil.SetField(flow, "level", level);
                EditorUtil.SetField(flow, "roster", roster);
                EditorUtil.SetField(flow, "player", hero);
                EditorUtil.SetField(flow, "startPoint", startT);
                EditorUtil.SetFloat(flow, "killY", -4f);
                EditorUtil.SetField(flow, "spriteMaterial", _mat);
                EditorUtil.SetField(flow, "shardSprite", _shard);
                EditorUtil.SetField(flow, "healthSprite", PlaceholderArt.HealthCrystal());
                if (stealth)
                {
                    var tracker = new GameObject("StealthTracker").AddComponent<StealthTracker>();
                    if (_contract != null) tracker.objectiveName = _contract.objectiveName;
                }
                if (_contract != null)
                {
                    var mt = new GameObject("MissionTracker").AddComponent<MissionTracker>();
                    mt.contractId = _contract.contractId;
                    mt.clueId = _contract.clueId ?? "";
                    // The board tells the results screen which contract is behind this map's exit door.
                    mt.board = AssetDatabase.LoadAssetAtPath<MissionBoardDefinition>(DataAssets.BoardAssetPath);
                }
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var touch = new GameObject("TouchControls").AddComponent<TouchControlsUI>();
            EditorUtil.SetField(hero.GetComponent<PlayerInputRouter>(), "touchControls", touch);
            if (map.IsSurvival) new GameObject("HUD").AddComponent<ZombieHUD>();
            else new GameObject("HUD").AddComponent<StoryHUD>();
            new GameObject("PauseMenu").AddComponent<PauseMenu>();
            if (!map.IsSurvival) new GameObject("LevelCompleteScreen").AddComponent<LevelCompleteScreen>();

            level.totalShards = _shards + _crates;
            level.totalSecrets = map.HasSecret ? 1 : 0;
            EditorUtility.SetDirty(level);

            string folder = _contract != null ? ContractFolder : _style != null ? EventFolder : Folder;
            string path = $"{folder}/{sceneName}.unity";
            EditorUtil.EnsureFolder(folder);
            EditorSceneManager.SaveScene(scene, path);
            EditorUtil.AddSceneToBuild(path);
            Debug.Log($"[PKR] Built {path}: {map.Width}x{map.Height}, {level.totalShards} shards, {map.Count('g')} guards, {map.Count('R')} relics.");
        }

        // ---- Halloween: the horde and Zombie Survival ----------------------------------------------------------

        /// <summary>Haunted Escape: the horde starts ten cells behind the hero (a fog bank with zombies in it).</summary>
        static void Horde(Vector2 start, float groundY)
        {
            var go = new GameObject("Horde");
            go.transform.SetParent(_root, false);
            go.transform.position = new Vector3(start.x - 10f, groundY, 0f);
            var wall = go.AddComponent<HordeWall>();
            var glow = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Resources/PKR/fx_glow.png");
            // A dark wall of fog trailing off to the left, with a sickly green edge.
            var dark = Art("Dark", _ground, new Vector2(start.x - 16f, groundY + 6f), new Vector2(12f, 16f), 12, go.transform);
            dark.color = new Color(0.04f, 0.05f, 0.04f, 0.92f);
            if (glow != null)
            {
                var fog = new GameObject("Fog");
                fog.transform.SetParent(go.transform, false);
                fog.transform.localPosition = new Vector3(-1.5f, 2.5f, 0f);
                fog.transform.localScale = new Vector3(1f, 1f, 1f);
                var sr = fog.AddComponent<SpriteRenderer>();
                sr.sprite = glow;
                sr.color = new Color(0.35f, 0.6f, 0.3f, 0.55f);
                if (_mat != null) sr.sharedMaterial = _mat;
                sr.sortingOrder = 13;
                fog.transform.localScale = new Vector3(5f, 7f, 1f);
                wall.fog = fog.transform;
            }
            var frames = new List<Sprite>();
            for (int i = 0; i < 6; i++)
            {
                var f = AssetDatabase.LoadAssetAtPath<Sprite>($"{PixelArt.Root}/Enemies/zombie_shambler/zombie_shambler_run_{i}.png");
                if (f != null) frames.Add(f);
            }
            wall.walkFrames = frames.ToArray();
            var walkers = new List<SpriteRenderer>();
            for (int i = 0; i < 7; i++)
            {
                var z = new GameObject("Walker" + i);
                z.transform.SetParent(go.transform, false);
                z.transform.localPosition = new Vector3(-0.4f - (i % 4) * 0.9f, i < 4 ? 0f : 1.3f, 0f);
                var sr = z.AddComponent<SpriteRenderer>();
                sr.sprite = frames.Count > 0 ? frames[0] : null;
                sr.color = i < 4 ? Color.white : new Color(0.55f, 0.6f, 0.55f, 1f);
                if (_mat != null) sr.sharedMaterial = _mat;
                sr.sortingOrder = i < 4 ? 15 : 14;
                walkers.Add(sr);
            }
            wall.walkers = walkers.ToArray();
        }

        /// <summary>z: a zombie spawn. Spawns at ground level beside a window barricade are window spawns; others drop from the roof.</summary>
        static void ZombieSpawn(AsciiLevel map, int x, int y)
        {
            bool window = false;
            foreach (var r in map.MergedRects('w'))
                if (Mathf.Abs(r.x - x) <= 4 && y >= r.y - 1 && y <= r.y + r.h) window = true;
            var go = new GameObject(window ? "WindowSpawn" : "RoofSpawn");
            go.transform.SetParent(_root, false);
            go.transform.position = new Vector2(x + 0.5f, y);
            (window ? _windowSpawns : _roofSpawns).Add(go.transform);
        }

        static void Station(StationKind kind, int index, float cx, float groundY)
        {
            var go = new GameObject("Station_" + kind);
            go.transform.SetParent(_root, false);
            go.transform.position = new Vector2(cx, groundY);
            var st = go.AddComponent<ZombieStation>();
            st.kind = kind;
            st.index = index;
            string art;
            Vector2 offset = Vector2.zero;
            Color tint = Color.white;
            switch (kind)
            {
                case StationKind.WallWeapon:
                    var w = ZombieArmory.Wall[index % ZombieArmory.Wall.Length];
                    art = "wall_" + w.id;
                    offset = new Vector2(0f, 1.1f);
                    tint = Color.Lerp(Color.white, new Color32(w.r, w.g, w.b, 255), 0.45f);
                    break;
                case StationKind.MysteryCrate: art = "mystery_crate"; break;
                case StationKind.Perk:
                    art = "perk_" + new[] { "iron_heart", "quick_draw", "second_wind", "fleet_feet" }[index % 4];
                    break;
                case StationKind.Forge: art = "forge"; break;
                default:
                    art = "jack_lantern";
                    st.reach = 1.1f;
                    break;
            }
            var sr = new GameObject("Art").AddComponent<SpriteRenderer>();
            sr.transform.SetParent(go.transform, false);
            sr.transform.localPosition = offset;
            sr.sprite = EventArt(art);
            sr.color = kind == StationKind.Lantern ? new Color(0.55f, 0.45f, 0.4f, 1f) : tint; // lanterns start dark
            if (_mat != null) sr.sharedMaterial = _mat;
            sr.sortingOrder = kind == StationKind.WallWeapon ? 2 : 6;
            st.art = sr;
            if (kind == StationKind.Lantern)
            {
                var glow = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Resources/PKR/fx_glow.png");
                var g = new GameObject("Glow");
                g.transform.SetParent(go.transform, false);
                g.transform.localPosition = new Vector3(0f, 0.45f, 0f);
                g.transform.localScale = Vector3.one * 2.2f;
                var gs = g.AddComponent<SpriteRenderer>();
                gs.sprite = glow;
                gs.color = new Color(1f, 0.6f, 0.15f, 0.6f);
                if (_mat != null) gs.sharedMaterial = _mat;
                gs.sortingOrder = 5;
                g.SetActive(false);
            }
            _stations.Add(st);
        }

        /// <summary>w: a boarded window in the manor wall (solid while it has boards).</summary>
        static void BarricadeAt(AsciiLevel.Rect r)
        {
            var go = new GameObject("Barricade") { layer = PKRLayers.Ground };
            go.transform.SetParent(_root, false);
            go.transform.position = new Vector2(r.x + r.w * 0.5f, r.y + r.h * 0.5f);
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(r.w, r.h);
            var frame = Art("Frame", EventArt("window_frame"), new Vector2(r.x + r.w * 0.5f, r.y), new Vector2(r.w, r.h), 3, go.transform);
            frame.drawMode = SpriteDrawMode.Simple;
            var fs = frame.sprite != null ? frame.sprite.bounds.size : Vector3.one;
            frame.transform.localScale = new Vector3(r.w / Mathf.Max(0.01f, fs.x), r.h / Mathf.Max(0.01f, fs.y), 1f);
            var boardArt = EventArt("board");
            var boards = new List<SpriteRenderer>();
            for (int i = 0; i < ZombieRules.BarricadeBoards; i++)
            {
                var b = new GameObject("Board" + i).AddComponent<SpriteRenderer>();
                b.transform.SetParent(go.transform, false);
                float t = (i + 0.5f) / ZombieRules.BarricadeBoards;
                b.transform.localPosition = new Vector3(0f, -r.h * 0.5f + t * r.h - 0.1f, 0f);
                b.transform.localRotation = Quaternion.Euler(0f, 0f, i % 2 == 0 ? 14f : -12f);
                b.transform.localScale = new Vector3(r.w * 1.3f, 1f, 1f);
                b.sprite = boardArt;
                if (_mat != null) b.sharedMaterial = _mat;
                b.sortingOrder = 7;
                boards.Add(b);
            }
            var bar = go.AddComponent<Barricade>();
            bar.boards = boards.ToArray();
            _barricades.Add(bar);
        }

        /// <summary>q: a door into the gallery that opens for points.</summary>
        static void PaidDoor(AsciiLevel.Rect r)
        {
            var go = new GameObject("PaidDoor") { layer = PKRLayers.Ground };
            go.transform.SetParent(_root, false);
            go.transform.position = new Vector2(r.x + r.w * 0.5f, r.y + r.h * 0.5f);
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(r.w, r.h);
            var art = EventArt("paid_door");
            var sr = Art("Door", art, new Vector2(r.x + r.w * 0.5f, r.y), new Vector2(r.w, r.h), 6, go.transform);
            var st = go.AddComponent<ZombieStation>();
            st.kind = StationKind.Door;
            st.blocker = box;
            st.art = sr;
            _stations.Add(st);
        }

        /// <summary>The survival runtime: rules controller, the hero's guns, and the cast.</summary>
        static void Survival(GameObject hero, Transform start)
        {
            var gunner = hero.AddComponent<ZombieGunner>();
            gunner.material = _mat;
            var game = new GameObject("ZombieSurvival").AddComponent<ZombieSurvival>();
            game.player = hero;
            game.startPoint = start;
            game.spriteMaterial = _mat;
            game.windowSpawns = _windowSpawns.ToArray();
            game.roofSpawns = _roofSpawns.ToArray();
            game.barricades = _barricades.ToArray();
            game.stations = _stations.ToArray();
            game.shambler = DataAssets.GetOrCreateZombieShambler();
            game.runner = DataAssets.GetOrCreateZombieRunner();
            game.bloater = DataAssets.GetOrCreateZombieBloater();
            game.spitter = DataAssets.GetOrCreateZombieSpitter();
            game.brute = DataAssets.GetOrCreateZombieBrute();
            game.bat = DataAssets.GetOrCreateZombieBat();
            game.pumpkinKing = DataAssets.GetOrCreatePumpkinKing();
            game.powerUpSprites = new[]
            {
                EventArt("power_ammo"), EventArt("power_instakill"), EventArt("power_double"), EventArt("power_nuke"), EventArt("power_boardup")
            };
            game.healthSprite = PlaceholderArt.HealthCrystal();
        }

        static void LoadArt()
        {
            _mat = EditorUtil.UnlitSpriteMaterial();
            _ground = PlaceholderArt.Ground();
            _stone = PlaceholderArt.StoneBlock();
            _oneWay = PlaceholderArt.OneWay();
            _plate = PlaceholderArt.PlatformPlate();
            _spikes = PlaceholderArt.Spikes();
            _flag = PlaceholderArt.CheckpointFlag();
            _gate = PlaceholderArt.GoalGate();
            _gear = PlaceholderArt.DecorGear();
            _crate = PlaceholderArt.ShardCrate();
            _crateEmpty = PlaceholderArt.ShardCrateEmpty();
            _vine = PlaceholderArt.Vine();
            _hay = PlaceholderArt.HayBale();
            _water = PlaceholderArt.WaterTile();
            _spring = PlaceholderArt.SpringPad();
            _ledger = PlaceholderArt.Ledger();
            _shard = PlaceholderArt.StarShard();
            _scroll = PlaceholderArt.CipherScroll();
            _relics = new[] { PlaceholderArt.RelicIdol(), PlaceholderArt.RelicMask(), PlaceholderArt.RelicAmulet() };
        }

        // ---- Pieces ----------------------------------------------------------------------------------

        static GameObject Block(string name, Sprite sprite, AsciiLevel.Rect r) =>
            SandboxBuilder.Block(_root, name, sprite, _mat, new Vector2(r.x + r.w * 0.5f, r.y + r.h * 0.5f), new Vector2(r.w, r.h));

        static SpriteRenderer Art(string name, Sprite sprite, Vector2 center, Vector2 size, int order, Transform parent = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent != null ? parent : _root, false);
            go.transform.position = center;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = size;
            if (_mat != null) sr.sharedMaterial = _mat;
            sr.sortingOrder = order;
            return sr;
        }

        static void Water(AsciiLevel.Rect r) =>
            Art("Water", _water, new Vector2(r.x + r.w * 0.5f, r.y + r.h * 0.5f - 1f), new Vector2(r.w, r.h + 2f), 12);

        static void Vine(AsciiLevel level, AsciiLevel.Rect r)
        {
            var center = new Vector2(r.x + r.w * 0.5f, r.y + r.h * 0.5f);
            var sr = Art("Vine", _vine, center, new Vector2(r.w, r.h), -2);
            var zone = sr.gameObject.AddComponent<ClimbZone>();
            zone.size = new Vector2(r.w, r.h);
            int topY = r.y + r.h - 1;
            zone.hopDirection = level.IsSolid(r.x + r.w, topY) ? 1 : level.IsSolid(r.x - 1, topY) ? -1 : 1;
        }

        static void Hay(AsciiLevel.Run run)
        {
            var center = new Vector2(run.x + run.length * 0.5f, run.y + 0.65f);
            var sr = Art("HayBale", _hay, center, new Vector2(run.length, 1.3f), 20);
            sr.color = new Color(1f, 1f, 1f, 0.92f); // draws over the hero hiding inside
            var spot = sr.gameObject.AddComponent<HideSpot>();
            spot.size = new Vector2(run.length, 1.6f);
            sr.gameObject.transform.position = center; // HideSpot measures from here
        }

        static void Spikes(AsciiLevel level, AsciiLevel.Run run)
        {
            var go = new GameObject("Spikes") { layer = PKRLayers.Hazard };
            go.transform.SetParent(_root, false);
            go.transform.position = new Vector2(run.x + run.length * 0.5f, run.y + 0.25f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _spikes;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(run.length, 0.5f);
            if (_mat != null) sr.sharedMaterial = _mat;
            sr.sortingOrder = 5;
            var cd = go.AddComponent<ContactDamage>();
            cd.team = TeamIds.Enemy;
            cd.size = new Vector2(run.length - 0.1f, 0.4f);
            cd.launchUpward = true;
            cd.inactiveWhileStunned = false;
            cd.hit = new HitData
            {
                damage = 1, pipDamage = 1, baseKnockback = 11f, exposedMultiplier = 1f,
                angleDegrees = 80f, baseHitstunFrames = 16, hitstopFrames = 4, isHeavy = false
            };
            // Spikes need something under them, or a launched hero could fall through the strip.
            if (!level.IsSolid(run.x, run.y - 1))
                SandboxBuilder.Block(_root, "SpikeBed", _stone, _mat, new Vector2(run.x + run.length * 0.5f, run.y - 0.5f), new Vector2(run.length, 1f));
        }

        // ---- Shattered Realms props -----------------------------------------------------------------------------

        static readonly Color LavaColor = new Color(1f, 0.42f, 0.12f);

        /// <summary>Lava: a glowing strip that hurts and launches like spikes, over a stone bed.</summary>
        static void Lava(AsciiLevel level, AsciiLevel.Run run)
        {
            var art = Art("Lava", _water, new Vector2(run.x + run.length * 0.5f, run.y + 0.3f), new Vector2(run.length, 0.8f), 6);
            art.color = LavaColor;
            var go = art.gameObject;
            go.layer = PKRLayers.Hazard;
            var cd = go.AddComponent<ContactDamage>();
            cd.team = TeamIds.Enemy;
            cd.size = new Vector2(run.length - 0.1f, 0.5f);
            cd.launchUpward = true;
            cd.inactiveWhileStunned = false;
            cd.hit = new HitData
            {
                damage = 1, pipDamage = 1, baseKnockback = 13f, exposedMultiplier = 1f,
                angleDegrees = 82f, baseHitstunFrames = 16, hitstopFrames = 4, isHeavy = false
            };
            if (!level.IsSolid(run.x, run.y - 1))
                SandboxBuilder.Block(_root, "LavaBed", _stone, _mat, new Vector2(run.x + run.length * 0.5f, run.y - 0.5f), new Vector2(run.length, 1f))
                    .GetComponent<SpriteRenderer>().color = new Color(0.35f, 0.2f, 0.18f);
        }

        /// <summary>Crumbling basalt / trapdoor: solid until you stand on it, then it drops out and comes back.</summary>
        static void Crumble(AsciiLevel.Run run, Color tint)
        {
            var go = SandboxBuilder.Block(_root, "Crumble", _plate, _mat, new Vector2(run.x + run.length * 0.5f, run.y + 0.5f), new Vector2(run.length, 1f));
            go.GetComponent<SpriteRenderer>().color = tint * new Color(0.8f, 0.7f, 0.7f);
            go.AddComponent<CrumblePlatform>();
        }

        /// <summary>Conveyor floor: solid, and carries a hero standing on it.</summary>
        static void Conveyor(AsciiLevel.Run run, int dir)
        {
            var center = new Vector2(run.x + run.length * 0.5f, run.y + 0.5f);
            var go = SandboxBuilder.Block(_root, dir > 0 ? "Conveyor_Right" : "Conveyor_Left", _plate, _mat, center, new Vector2(run.length, 1f));
            go.GetComponent<SpriteRenderer>().color = new Color(0.55f, 0.5f, 0.6f);
            var zone = go.AddComponent<ForceZone>();
            zone.kind = ForceKind.Conveyor;
            zone.direction = dir;
            zone.size = new Vector2(run.length, 1f);
            zone.strength = 3.2f;
            zone.streakSpeed = zone.strength;
            zone.streaks = Streaks(go.transform, run.length, 0.42f, 0.12f, new Color(1f, 0.85f, 0.3f, 0.9f), Mathf.Max(2, run.length));
        }

        /// <summary>Wind (pale streaks) or water current (teal, with a little lift) over a block of cells.</summary>
        static void Push(AsciiLevel.Rect r, ForceKind kind, int dir)
        {
            var center = new Vector2(r.x + r.w * 0.5f, r.y + r.h * 0.5f);
            var go = new GameObject(kind == ForceKind.Current ? "Current" : "Wind");
            go.transform.SetParent(_root, false);
            go.transform.position = center;
            var zone = go.AddComponent<ForceZone>();
            zone.kind = kind;
            zone.direction = dir;
            zone.size = new Vector2(r.w, r.h);
            zone.strength = kind == ForceKind.Current ? 26f : 34f;
            zone.lift = kind == ForceKind.Current ? 7f : 0f;
            zone.streakSpeed = kind == ForceKind.Current ? 3f : 6f;
            var color = kind == ForceKind.Current ? new Color(0.4f, 1f, 0.9f, 0.35f) : new Color(1f, 1f, 1f, 0.35f);
            if (kind == ForceKind.Current)
                Art("CurrentWater", _water, center, new Vector2(r.w, r.h), -3).color = new Color(0.3f, 0.75f, 0.8f, 0.35f);
            int count = Mathf.Max(2, r.w * r.h / 3);
            zone.streaks = Streaks(go.transform, r.w, r.h * 0.5f, 0.08f, color, count, r.h);
        }

        static SpriteRenderer[] Streaks(Transform parent, float width, float yOrHalfHeight, float thickness, Color color, int count, float height = 0f)
        {
            var list = new SpriteRenderer[count];
            var rng = new System.Random(count * 7919 + (int)(width * 31));
            for (int i = 0; i < count; i++)
            {
                float y = height > 0f ? (float)(rng.NextDouble() * height - height * 0.5f) : yOrHalfHeight;
                var sr = Art("Streak", _oneWay, (Vector2)parent.position + new Vector2(-width * 0.5f + (i + 0.5f) * width / count, y),
                             new Vector2(0.7f, thickness), 8, parent);
                sr.color = color;
                list[i] = sr;
            }
            return list;
        }

        /// <summary>Fire geyser: a vent on the ground that erupts a 4-tall fire column on a timer.</summary>
        static void Geyser(Vector2 groundPos, int cellX)
        {
            var go = new GameObject("FireGeyser");
            go.transform.SetParent(_root, false);
            go.transform.position = groundPos;
            var g = go.AddComponent<FireGeyser>();
            g.offset = (cellX * 0.37f) % 3.5f;
            g.vent = Art("Vent", _plate, groundPos + new Vector2(0f, 0.1f), new Vector2(1f, 0.25f), 7, go.transform);
            var col = Art("Fire", _water, groundPos, new Vector2(0.8f, 0.01f), 9, go.transform);
            g.column = col;
            var hurtGo = new GameObject("FireHurt") { layer = PKRLayers.Hazard };
            hurtGo.transform.SetParent(go.transform, false);
            hurtGo.transform.localPosition = new Vector3(0f, g.height * 0.5f, 0f);
            var cd = hurtGo.AddComponent<ContactDamage>();
            cd.team = TeamIds.Enemy;
            cd.size = new Vector2(0.7f, g.height);
            cd.launchUpward = true;
            cd.inactiveWhileStunned = false;
            cd.hit = new HitData
            {
                damage = 1, pipDamage = 1, baseKnockback = 12f, exposedMultiplier = 1f,
                angleDegrees = 75f, baseHitstunFrames = 16, hitstopFrames = 4, isHeavy = false
            };
            cd.enabled = false;
            g.hurt = cd;
        }

        static void Spot(PickupKind kind, Vector2 pos)
        {
            var go = new GameObject(kind == PickupKind.StarShard ? "Shard" : "Health");
            go.transform.SetParent(_root, false);
            go.transform.position = pos;
            var s = go.AddComponent<PickupSpot>();
            s.kind = kind;
            s.value = 1;
            if (kind == PickupKind.StarShard) _shards++;
        }

        static void Crate(Vector2 center)
        {
            var go = SandboxBuilder.Block(_root, "ShardCrate", _crate, _mat, center, Vector2.one);
            var crate = go.AddComponent<ShardCrate>();
            crate.art = go.GetComponent<SpriteRenderer>();
            crate.emptySprite = _crateEmpty;
            crate.shardSprite = _shard;
            _crates++;
        }

        static void Spring(Vector2 top)
        {
            var go = new GameObject("SpringPad");
            go.transform.SetParent(_root, false);
            go.transform.position = top;
            var art = new GameObject("Art");
            art.transform.SetParent(go.transform, false);
            art.transform.localPosition = new Vector3(0f, -0.25f, 0f);
            var sr = art.AddComponent<SpriteRenderer>();
            sr.sprite = _spring;
            if (_mat != null) sr.sharedMaterial = _mat;
            sr.sortingOrder = 6;
            var pad = go.AddComponent<SpringPad>();
            pad.art = art.transform;
        }

        static void Objective(Vector2 pos)
        {
            var go = new GameObject("Objective_Ledger") { layer = PKRLayers.Pickup };
            go.transform.SetParent(_root, false);
            go.transform.position = pos;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.6f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _contract != null ? PlaceholderArt.ContractPrize() : _ledger;
            if (_mat != null) sr.sharedMaterial = _mat;
            sr.sortingOrder = 15;
            go.AddComponent<StealthObjective>().displayName = _contract != null ? _contract.objectiveDisplay : "Gearwright's Ledger";
            var glow = Decor(pos, 0.7f); // a glow behind it so it reads at a distance (goes away with it)
            glow.color = new Color(1f, 0.9f, 0.5f, 0.5f);
            glow.transform.SetParent(go.transform, true);
        }

        static void Relic(Vector2 pos)
        {
            var go = new GameObject("Relic") { layer = PKRLayers.Pickup };
            go.transform.SetParent(_root, false);
            go.transform.position = pos;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.55f;
            var art = new GameObject("Art");
            art.transform.SetParent(go.transform, false);
            var sr = art.AddComponent<SpriteRenderer>();
            sr.sprite = _relics[_relicIndex % _relics.Length];
            if (_mat != null) sr.sharedMaterial = _mat;
            sr.sortingOrder = 15;
            go.AddComponent<RelicPickup>().relicName = RelicNames[_relicIndex % RelicNames.Length];
            _relicIndex++;
        }

        static void Scroll(Vector2 pos)
        {
            var go = new GameObject("CipherScroll") { layer = PKRLayers.Pickup };
            go.transform.SetParent(_root, false);
            go.transform.position = pos;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.55f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _scroll;
            if (_mat != null) sr.sharedMaterial = _mat;
            sr.sortingOrder = 15;
            go.AddComponent<ClueScroll>();
        }

        static void Enemy(EnemyDefinition def, float x, float feetY)
        {
            var go = new GameObject("Spawn_" + def.displayName);
            go.transform.SetParent(_root, false);
            go.transform.position = new Vector2(x, feetY + def.bodySize.y * 0.5f + 0.05f);
            var sp = go.AddComponent<EnemySpawnPoint>();
            sp.enemy = def;
            sp.startDirection = -1;
        }

        static void Mover(string name, Vector2 center, float width, Vector2 travel, float leg)
        {
            var go = new GameObject(name) { layer = PKRLayers.Ground };
            go.transform.SetParent(_root, false);
            go.transform.position = center;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(width, 0.5f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _plate;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(width, 0.5f);
            if (_mat != null) sr.sharedMaterial = _mat;
            var mp = go.AddComponent<MovingPlatform>();
            mp.travel = travel;
            mp.legDuration = leg;
            mp.pauseAtEnds = 0.6f;
        }

        static void Checkpoint(Vector2 groundPos, int index)
        {
            var go = new GameObject($"Checkpoint_{index + 1}") { layer = PKRLayers.Pickup };
            go.transform.SetParent(_root, false);
            go.transform.position = groundPos;
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(1.5f, 3f);
            box.offset = new Vector2(0f, 1.5f);
            var flag = new GameObject("Flag");
            flag.transform.SetParent(go.transform, false);
            var sr = flag.AddComponent<SpriteRenderer>();
            sr.sprite = _flag;
            if (_mat != null) sr.sharedMaterial = _mat;
            sr.sortingOrder = 4;
            var cp = go.AddComponent<Checkpoint>();
            cp.index = index;
            cp.flag = sr;
        }

        static void Goal(Vector2 groundPos)
        {
            var go = new GameObject("GoalGate") { layer = PKRLayers.Pickup };
            go.transform.SetParent(_root, false);
            go.transform.position = groundPos;
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(1.5f, 3.5f);
            box.offset = new Vector2(0f, 1.75f);
            var art = new GameObject("Arch");
            art.transform.SetParent(go.transform, false);
            var sr = art.AddComponent<SpriteRenderer>();
            sr.sprite = _gate;
            if (_mat != null) sr.sharedMaterial = _mat;
            sr.sortingOrder = 3;
            go.AddComponent<GoalGate>();
        }

        static void SecretRoom(string id, Vector2 center, Vector2 size)
        {
            var go = new GameObject("SecretArea") { layer = PKRLayers.Pickup };
            go.transform.SetParent(_root, false);
            go.transform.position = center;
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = size - new Vector2(0.4f, 0.2f);
            var area = go.AddComponent<SecretArea>();
            area.secretId = id;
            var cover = Art("FakeWall", _stone, center, size, 40, go.transform);
            area.covers.Add(cover);
        }

        static SpriteRenderer Decor(Vector2 pos, float scale)
        {
            var go = new GameObject("Decor_Gear");
            go.transform.SetParent(_root, false);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _gear;
            if (_mat != null) sr.sharedMaterial = _mat;
            sr.sortingOrder = -10;
            return sr;
        }
    }
}
