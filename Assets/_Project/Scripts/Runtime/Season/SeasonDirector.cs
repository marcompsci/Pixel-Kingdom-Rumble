using System.Collections.Generic;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Dresses a Story Quest level for the live season (by the device's date; Settings can turn it off):
    ///   sky and background colour, falling leaves or snow, snow or leaves on every open ledge, props (pumpkins, hay,
    ///   corn, snowmen), holiday decorations at checkpoints and the goal (feast tables, a lit tree with gifts, lanterns),
    ///   one hidden season token, hats on the foes and the player's keepsake. It also counts the season's challenges.
    /// Everything it adds is decoration: no colliders except the token's trigger, nothing a jump can land on.
    /// </summary>
    public class SeasonDirector : MonoBehaviour
    {
        public static SeasonDirector Current { get; private set; }

        public SeasonState State { get; private set; }
        public SeasonLook Look => State.Look;
        public int Foes { get; private set; }

        LevelFlowController _flow;
        Material _mat;
        Transform _root;
        int _seed;
        float _nextFoeScan;
        readonly List<Vector2> _keepClear = new List<Vector2>();

        /// <summary>Every Story Quest level counts toward the season's tokens.</summary>
        public static int StoryLevelCount
        {
            get
            {
                int n = 0;
                foreach (var w in StoryWorlds.All) n += w.levels.Length;
                return n;
            }
        }

        /// <summary>Should this level be dressed? Story Quest levels only (not events, contracts or modes).</summary>
        public static bool AppliesTo(LevelDefinition level) =>
            level != null && (level.id == KingdomPlaza.PlazaLevelId || (!level.isEvent && StoryWorlds.WorldOf(level.id) >= 0))
            && SettingsService.Season.Active;

        public static SeasonDirector Attach(LevelFlowController flow, Material mat)
        {
            var d = flow.gameObject.AddComponent<SeasonDirector>();
            d._flow = flow;
            d._mat = mat;
            return d;
        }

        void Awake()
        {
            Current = this;
            State = SettingsService.Season;
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        void OnEnable() => EventBus<PlayerRespawned>.Subscribe(OnRespawn);
        void OnDisable() => EventBus<PlayerRespawned>.Unsubscribe(OnRespawn);

        void Start()
        {
            if (!State.Active) { enabled = false; return; }
            var level = _flow != null ? _flow.Level : null;
            _seed = level != null ? Hash(level.id + State.Key) : 1;
            _root = new GameObject("Season_" + State.Key).transform;

            var start = FindAnyObjectByType<PlayerMarker>();
            if (start != null) _keepClear.Add(start.transform.position);
            foreach (var g in FindObjectsByType<GoalGate>()) _keepClear.Add(g.transform.position);
            foreach (var c in FindObjectsByType<Checkpoint>()) _keepClear.Add(c.transform.position);

            Tint();
            var cam = Camera.main;
            if (cam != null) SeasonWeather.Attach(cam, State, _mat);
            DressGround();
            Decorate();
            PlaceToken(level);
            DressFoes();
            DressHero();
            if (State.Peak == SeasonPeak.NewYear && !SettingsService.ReducedMotion && cam != null)
                cam.gameObject.AddComponent<SeasonFireworks>().Init(_mat);
        }

        void Update()
        {
            // Foes can join later (respawns, waves): give newcomers their hats now and then.
            if (Time.time >= _nextFoeScan) { _nextFoeScan = Time.time + 2.5f; DressFoes(); }
        }

        void OnRespawn(PlayerRespawned e) => _nextFoeScan = 0f;

        public void CountFoe() => Foes++;

        /// <summary>The level was cleared: this run counts toward the season's challenges.</summary>
        public void RecordClear(SaveData data, int shards)
        {
            if (data == null || !State.Active) return;
            int before = Seasons.Unclaimed(data, State);
            Seasons.RecordLevel(data, State, shards, Foes);
            if (Seasons.Unclaimed(data, State) > before)
                ProgressTracker.Toast("SEASON CHALLENGE DONE", "Claim the reward from the season banner on the main menu",
                                      new Color32(255, 214, 90, 255));
        }

        static int Hash(string s)
        {
            int h = 23;
            foreach (char c in s) h = unchecked(h * 31 + c);
            return h & 0x7fffffff;
        }

        // ---- sky ---------------------------------------------------------------------------------------------------

        void Tint()
        {
            Color topTo, bottomTo, layerTo;
            float k = State.AtPeak ? 0.5f : 0.36f;
            bool evening = State.Peak == SeasonPeak.Christmas || State.Peak == SeasonPeak.NewYear || State.Peak == SeasonPeak.HalloweenWeek;
            if (Look == SeasonLook.Autumn)
            {
                topTo = evening ? new Color(0.28f, 0.16f, 0.32f) : new Color(0.95f, 0.58f, 0.3f);
                bottomTo = evening ? new Color(0.95f, 0.52f, 0.3f) : new Color(1f, 0.84f, 0.56f);
                layerTo = new Color(1f, 0.8f, 0.62f);
            }
            else
            {
                topTo = evening ? new Color(0.1f, 0.12f, 0.3f) : new Color(0.32f, 0.42f, 0.66f);
                bottomTo = evening ? new Color(0.5f, 0.48f, 0.74f) : new Color(0.86f, 0.92f, 1f);
                layerTo = evening ? new Color(0.62f, 0.66f, 0.9f) : new Color(0.82f, 0.9f, 1f);
            }
            if (evening) k = 0.62f;
            else if (Look == SeasonLook.Autumn) k = State.AtPeak ? 0.6f : 0.5f; // a blue sky lerped weakly toward orange turns grey
            foreach (var sky in FindObjectsByType<SkyGradient>())
                sky.SetColors(Color.Lerp(sky.top, topTo, k), Color.Lerp(sky.bottom, bottomTo, k));
            foreach (var layer in FindObjectsByType<ParallaxLayer>())
                layer.SetTint(Color.Lerp(Color.white, layerTo, k * 0.8f));
            var cam = Camera.main;
            if (cam != null) cam.backgroundColor = Color.Lerp(cam.backgroundColor, bottomTo, k);
        }

        // ---- ground ------------------------------------------------------------------------------------------------

        bool NearKeepClear(float x, float y, float r)
        {
            foreach (var p in _keepClear) if (Mathf.Abs(p.x - x) < r && Mathf.Abs(p.y - y) < 2.5f) return true;
            return false;
        }

        void DressGround()
        {
            var rng = new System.Random(_seed);
            int mask = PKRLayers.GroundMask;
            int props = 0;
            float nextProp = 6f + rng.Next(0, 6);
            var cols = FindObjectsByType<BoxCollider2D>();
            System.Array.Sort(cols, (a, b) => a.bounds.min.x.CompareTo(b.bounds.min.x));
            foreach (var col in cols)
            {
                if (col == null || col.isTrigger || col.gameObject.layer != PKRLayers.Ground || col.attachedRigidbody != null) continue;
                string n = col.gameObject.name;
                if (n != "Ground" && n != "Stone") continue;
                var b = col.bounds;
                float top = b.max.y;
                int x0 = Mathf.RoundToInt(b.min.x), x1 = Mathf.RoundToInt(b.max.x);
                int run = -1;
                for (int x = x0; x <= x1; x++)
                {
                    bool open = x < x1 && Physics2D.OverlapPoint(new Vector2(x + 0.5f, top + 0.45f), mask) == null;
                    if (open && run < 0) run = x;
                    if (!open && run >= 0)
                    {
                        int len = x - run;
                        Strip(run, len, top);
                        for (float px = run + 1; px < run + len - 1 && props < 45; px += 1f)
                        {
                            if (px < nextProp) continue;
                            nextProp = px + 7 + rng.Next(0, 8);
                            float cx = px + 0.5f;
                            if (NearKeepClear(cx, top, 2.2f)) continue;
                            if (Physics2D.OverlapBox(new Vector2(cx, top + 0.85f), new Vector2(1.1f, 1.5f), 0f) != null) continue;
                            if (Prop(rng, new Vector2(cx, top - 0.04f))) props++;
                        }
                        run = -1;
                    }
                }
            }
        }

        void Strip(int x, int len, float top)
        {
            var sprite = Look == SeasonLook.Winter ? SeasonArt.SnowCap() : SeasonArt.LeafLitter();
            float h = sprite.bounds.size.y;
            var sr = Sr("Ledge", sprite, 2);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(len + (Look == SeasonLook.Winter ? 0.15f : 0f), h);
            float sink = Look == SeasonLook.Winter ? 0.13f : 0.08f;
            sr.transform.position = new Vector3(x + len * 0.5f, top + h * 0.5f - sink, 0f);
        }

        bool Prop(System.Random rng, Vector2 at)
        {
            Sprite s;
            int roll = rng.Next(0, 100);
            if (Look == SeasonLook.Winter)
            {
                if (State.Event == SeasonEvent.Winterfest && roll < 25) s = SeasonArt.Gift(rng.Next(0, 3));
                else if (roll < 70) s = SeasonArt.Snowman();
                else return false; // open snow is part of the look
            }
            else
            {
                bool spooky = State.Peak == SeasonPeak.HalloweenWeek;
                if (roll < 45) s = SeasonArt.Pumpkin(spooky && roll < 30);
                else if (roll < 72) s = SeasonArt.HayBale();
                else s = SeasonArt.CornBundle();
            }
            var sr = Sr("Prop", s, 3);
            sr.transform.position = at;
            if (rng.Next(0, 2) == 0) sr.flipX = true;
            if (s == SeasonArt.Pumpkin(true)) Glow(sr.transform, new Color(1f, 0.7f, 0.25f, 0.45f), 1.4f, 0.3f);
            return true;
        }

        SpriteRenderer Sr(string name, Sprite sprite, int order, Transform parent = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent != null ? parent : _root, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            if (_mat != null) sr.sharedMaterial = _mat;
            return sr;
        }

        void Glow(Transform t, Color c, float size, float lift)
        {
            var g = GlowHalo.Attach(t, c, size, 3, _mat);
            g.transform.localPosition = new Vector3(0f, lift, 0f);
            if (SettingsService.ReducedMotion) g.pulse = 0f;
        }

        // ---- decorations -------------------------------------------------------------------------------------------

        /// <summary>A spot beside x on this floor that isn't inside a wall.</summary>
        static Vector2 Beside(Vector2 basePos, float offset)
        {
            foreach (float o in new[] { offset, -offset, offset * 1.6f, -offset * 1.6f })
            {
                var p = new Vector2(basePos.x + o, basePos.y);
                if (Physics2D.OverlapPoint(p + new Vector2(0f, 0.4f), PKRLayers.GroundMask) == null &&
                    Physics2D.OverlapPoint(p + new Vector2(0f, -0.2f), PKRLayers.GroundMask) != null) return p;
            }
            return basePos + new Vector2(offset, 0f);
        }

        void Decorate()
        {
            bool feast = State.Peak == SeasonPeak.ThanksgivingWeek;
            bool christmas = State.Peak == SeasonPeak.Christmas;
            foreach (var cp in FindObjectsByType<Checkpoint>())
            {
                var at = Beside(cp.transform.position, -1.5f);
                switch (State.Event)
                {
                    case SeasonEvent.Winterfest:
                    {
                        var tree = Sr("CheckpointTree", SeasonArt.Pine(), 3);
                        tree.transform.position = at;
                        tree.transform.localScale = Vector3.one * 0.55f;
                        Lights(tree, 0.55f, 6);
                        break;
                    }
                    case SeasonEvent.Frost:
                        Sr("CheckpointSnowman", SeasonArt.Snowman(), 3).transform.position = at;
                        break;
                    case SeasonEvent.Harvest:
                        if (feast) Sr("CheckpointFeast", SeasonArt.FeastTable(), 3).transform.position = at;
                        else
                        {
                            Sr("CheckpointHay", SeasonArt.HayBale(), 3).transform.position = at;
                            Sr("CheckpointCorn", SeasonArt.CornBundle(), 2).transform.position = at + new Vector2(-0.7f, 0f);
                        }
                        break;
                    default:
                    {
                        bool lantern = State.Peak == SeasonPeak.HalloweenWeek;
                        var p = Sr("CheckpointPumpkin", SeasonArt.Pumpkin(lantern), 3);
                        p.transform.position = at;
                        if (lantern) Glow(p.transform, new Color(1f, 0.7f, 0.25f, 0.5f), 1.6f, 0.3f);
                        break;
                    }
                }
            }

            foreach (var goal in FindObjectsByType<GoalGate>())
            {
                Vector2 g = goal.transform.position;
                if (Look == SeasonLook.Winter)
                {
                    var at = Beside(g, -2.4f);
                    var tree = Sr("GoalTree", SeasonArt.Pine(), 3);
                    tree.transform.position = at;
                    Lights(tree, 1f, christmas ? SeasonArt.PineBulbs.Length : 7);
                    var star = Sr("GoalStar", SeasonArt.Star(), 5, tree.transform);
                    star.transform.localPosition = new Vector3(0f, 60f / SeasonArt.PPU, 0f);
                    Glow(star.transform, new Color(1f, 0.9f, 0.45f, 0.55f), 1.3f, 0f);
                    int gifts = christmas ? 5 : State.Event == SeasonEvent.Winterfest ? 3 : 0;
                    for (int i = 0; i < gifts; i++)
                    {
                        var gift = Sr("Gift", SeasonArt.Gift(i), 4);
                        gift.transform.position = at + new Vector2(-0.8f + i * 0.42f, 0f);
                        gift.transform.localScale = Vector3.one * (i % 2 == 0 ? 1f : 0.8f);
                    }
                    if (State.Event == SeasonEvent.Winterfest)
                    {
                        var wreath = Sr("GoalWreath", SeasonArt.Wreath(), 6);
                        wreath.transform.position = g + new Vector2(0f, 3.2f);
                    }
                }
                else
                {
                    var at = Beside(g, -2.2f);
                    if (State.Event == SeasonEvent.Harvest)
                    {
                        Sr("GoalFeast", SeasonArt.FeastTable(), 3).transform.position = at;
                        Sr("GoalCorn", SeasonArt.CornBundle(), 2).transform.position = at + new Vector2(-1.1f, 0f);
                        Sr("GoalPumpkin", SeasonArt.Pumpkin(false), 4).transform.position = at + new Vector2(1.0f, 0f);
                    }
                    else
                    {
                        bool lantern = State.Peak == SeasonPeak.HalloweenWeek;
                        for (int i = 0; i < 3; i++)
                        {
                            var p = Sr("GoalPumpkin", SeasonArt.Pumpkin(lantern), 4);
                            p.transform.position = at + new Vector2(i * 0.75f - 0.75f, 0f);
                            p.transform.localScale = Vector3.one * (i == 1 ? 1f : 0.8f);
                            if (lantern) Glow(p.transform, new Color(1f, 0.7f, 0.25f, 0.45f), 1.4f, 0.3f);
                        }
                    }
                }
            }
        }

        /// <summary>Coloured bulbs on a pine, gently twinkling (steady with Reduced motion).</summary>
        void Lights(SpriteRenderer tree, float scale, int count)
        {
            var spots = SeasonArt.PineBulbs;
            var tw = tree.gameObject.AddComponent<Twinkle>();
            float w = tree.sprite.rect.width;
            for (int i = 0; i < Mathf.Min(count, spots.Length); i++)
            {
                var b = Sr("Bulb", SeasonArt.Bulb(), 4, tree.transform);
                b.transform.localPosition = new Vector3((spots[i].x - w * 0.5f) / SeasonArt.PPU, spots[i].y / SeasonArt.PPU, 0f);
                b.color = SeasonArt.BulbColours[i % SeasonArt.BulbColours.Length];
                tw.Add(b);
            }
        }

        // ---- token -------------------------------------------------------------------------------------------------

        void PlaceToken(LevelDefinition level)
        {
            if (level == null || level.id == KingdomPlaza.PlazaLevelId) return; // tokens are hidden in Story levels only
            var spots = new List<Vector2>();
            float minX = float.MaxValue, maxX = float.MinValue;
            foreach (var s in FindObjectsByType<PickupSpot>())
            {
                if (s.kind != PickupKind.StarShard) continue;
                Vector2 p = s.transform.position;
                spots.Add(p);
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
            }
            Vector2 at;
            if (spots.Count > 0)
            {
                // somewhere in the back half of the level, picked the same way every time for this level and season
                float cut = Mathf.Lerp(minX, maxX, 0.45f);
                spots.RemoveAll(p => p.x < cut);
                spots.Sort((a, b) => a.x.CompareTo(b.x));
                var p0 = spots[_seed % spots.Count];
                var up = p0 + new Vector2(0f, 1.1f);
                at = Physics2D.OverlapCircle(up, 0.45f, PKRLayers.GroundMask) == null ? up : p0 + new Vector2(0f, 0.55f);
            }
            else
            {
                var cps = FindObjectsByType<Checkpoint>();
                if (cps.Length == 0) return;
                System.Array.Sort(cps, (a, b) => b.index.CompareTo(a.index));
                at = (Vector2)cps[0].transform.position + new Vector2(1.5f, 1.6f);
            }
            var save = Services.Save != null ? Services.Save.Data : null;
            bool taken = save != null && Seasons.HasToken(save, State, level.id);
            SeasonToken.Spawn(_root, at, State, level.id, taken, _mat);
        }

        // ---- hats --------------------------------------------------------------------------------------------------

        SeasonArt.Hat FoeHat => State.Event == SeasonEvent.Frost ? SeasonArt.Hat.Earmuffs
                              : Look == SeasonLook.Winter ? SeasonArt.Hat.Beanie : SeasonArt.Hat.LeafCrown;

        void DressFoes()
        {
            if (!State.Active) return;
            foreach (var ai in FindObjectsByType<EnemyAI>())
            {
                if (ai == null) continue;
                var outfit = ai.GetComponent<SeasonOutfit>();
                if (outfit == null) outfit = ai.gameObject.AddComponent<SeasonOutfit>();
                outfit.Wear(SeasonArt.HatSprite(FoeHat), FoeHat, _mat);
            }
        }

        void DressHero()
        {
            var save = Services.Save != null ? Services.Save.Data : null;
            if (save == null || save.season == null || string.IsNullOrEmpty(save.season.worn)) return;
            var hat = SeasonArt.KeepsakeHat(save.season.worn);
            var player = PlayerMarker.Current != null ? PlayerMarker.Current : FindAnyObjectByType<PlayerMarker>();
            if (!hat.HasValue || player == null) return;
            var outfit = player.GetComponent<SeasonOutfit>();
            if (outfit == null) outfit = player.gameObject.AddComponent<SeasonOutfit>();
            outfit.alwaysOn = true;
            outfit.Wear(SeasonArt.HatSprite(hat.Value), hat.Value, _mat);
        }
    }
}
