using PKR.Core;
using UnityEditor;
using UnityEngine;

namespace PKR.EditorTools
{
    /// <summary>
    /// Builds the quick-thinking obstacles and maze pieces from a text map (see AsciiLevel's legend):
    /// crushers (v), laser gates (l), saw blades (o), keys (k), locked doors (n), floor switches (!) and red / blue
    /// switch blocks (1 / 2). Called by AsciiLevelBuilder for every map.
    /// </summary>
    public static class ObstacleBuilder
    {
        static Sprite S(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{PixelArt.Root}/Obstacles/{name}.png");

        static HitData Ouch(float knock = 12f, float angle = 70f) => new HitData
        {
            damage = 1, pipDamage = 1, baseKnockback = knock, exposedMultiplier = 1f,
            angleDegrees = angle, baseHitstunFrames = 16, hitstopFrames = 4, isHeavy = false
        };

        /// <summary>Everything at once; root is the level's parent object.</summary>
        public static void Build(AsciiLevel map, Transform root, Material mat)
        {
            foreach (var p in map.Points)
            {
                switch (p.tile)
                {
                    case 'v': Crusher(map, root, mat, p.x, p.y); break;
                    case 'o': Saw(map, root, mat, p.x, p.y); break;
                    case 'k': Key(root, mat, new Vector2(p.x + 0.5f, p.y + 0.5f)); break;
                    case '!': Switch(root, mat, new Vector2(p.x + 0.5f, p.y)); break;
                }
            }
            foreach (var r in map.MergedRects('l')) Laser(root, mat, r);
            foreach (var r in map.MergedRects('n')) Door(root, mat, r);
            foreach (var r in map.MergedRects('1')) Blocks(root, mat, r, true);
            foreach (var r in map.MergedRects('2')) Blocks(root, mat, r, false);
        }

        static SpriteRenderer Art(Transform parent, string name, Sprite sprite, Material mat, Vector2 localPos, int order, Vector2? tiledSize = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            if (tiledSize.HasValue) { sr.drawMode = SpriteDrawMode.Tiled; sr.size = tiledSize.Value; }
            if (mat != null) sr.sharedMaterial = mat;
            sr.sortingOrder = order;
            return sr;
        }

        static ContactDamage Hurt(Transform parent, Vector2 size, Vector2 localPos, float knock, float angle)
        {
            var go = new GameObject("Hurt") { layer = PKRLayers.Hazard };
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var cd = go.AddComponent<ContactDamage>();
            cd.team = TeamIds.Enemy;
            cd.size = size;
            cd.launchUpward = true;
            cd.inactiveWhileStunned = false;
            cd.hit = Ouch(knock, angle);
            return cd;
        }

        /// <summary>v: a 2x2 press hammer whose top-left cell is (x, y). It drops to the first floor under it.</summary>
        static void Crusher(AsciiLevel map, Transform root, Material mat, int x, int y)
        {
            int bottom = y - 1;
            int floor = bottom - 1;
            while (floor >= 0 && !map.IsSolid(x, floor) && !map.IsSolid(x + 1, floor)) floor--;
            float drop = Mathf.Max(0f, bottom - (floor + 1));
            var go = new GameObject("Crusher") { layer = PKRLayers.Ground };
            go.transform.SetParent(root, false);
            go.transform.position = new Vector2(x + 1f, y);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(1.9f, 2f);
            var art = Art(go.transform, "Art", S("crusher"), mat, Vector2.zero, 8);
            var c = go.AddComponent<Crusher>();
            c.drop = drop;
            c.art = art.transform;
            c.offset = (x * 0.37f) % 2.2f;
            c.hurt = Hurt(go.transform, new Vector2(1.8f, 0.5f), new Vector2(0f, -1.05f), 14f, 90f);
            c.hurt.enabled = false;
            // The track it runs on: a faint chain from the ceiling.
            Art(go.transform, "Rail", S("laser_beam"), mat, new Vector2(0f, 2f), 3, new Vector2(0.35f, 2f)).color = new Color(0.4f, 0.4f, 0.5f, 0.6f);
        }

        /// <summary>l: a column of laser cells with an emitter at each end.</summary>
        static void Laser(Transform root, Material mat, AsciiLevel.Rect r)
        {
            var go = new GameObject("LaserGate");
            go.transform.SetParent(root, false);
            go.transform.position = new Vector2(r.x + 0.5f, r.y + r.h * 0.5f);
            var g = go.AddComponent<LaserGate>();
            g.offset = (r.x * 0.29f) % 2.8f;
            g.beam = Art(go.transform, "Beam", S("laser_beam"), mat, Vector2.zero, 9, new Vector2(0.5f, r.h));
            var top = Art(go.transform, "EmitterTop", S("laser_emitter"), mat, new Vector2(0f, r.h * 0.5f - 0.15f), 10);
            top.flipY = true;
            var bottom = Art(go.transform, "EmitterBottom", S("laser_emitter"), mat, new Vector2(0f, -r.h * 0.5f + 0.15f), 10);
            g.emitters = new[] { top, bottom };
            g.hurt = Hurt(go.transform, new Vector2(0.45f, r.h - 0.2f), Vector2.zero, 11f, 60f);
        }

        /// <summary>o: a saw that rolls along its row between walls (at most 5 cells each way).</summary>
        static void Saw(AsciiLevel map, Transform root, Material mat, int x, int y)
        {
            int left = x, right = x;
            while (left > x - 5 && !map.IsSolid(left - 1, y) && map.At(left - 1, y) != 'l') left--;
            while (right < x + 5 && !map.IsSolid(right + 1, y) && map.At(right + 1, y) != 'l') right++;
            var go = new GameObject("Saw");
            go.transform.SetParent(root, false);
            go.transform.position = new Vector2(x + 0.5f, y + 0.5f);
            var s = go.AddComponent<SawBlade>();
            s.a = new Vector2(left + 0.5f, y + 0.5f);
            s.b = new Vector2(right + 0.5f, y + 0.5f);
            s.offset = (x * 0.41f) % 2f;
            s.speed = 3.2f;
            s.art = Art(go.transform, "Art", S("saw"), mat, Vector2.zero, 11).transform;
            Hurt(go.transform, new Vector2(1.15f, 1.15f), Vector2.zero, 12f, 70f);
        }

        static void Key(Transform root, Material mat, Vector2 pos)
        {
            var go = new GameObject("Key") { layer = PKRLayers.Pickup };
            go.transform.SetParent(root, false);
            go.transform.position = pos;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f;
            Art(go.transform, "Art", S("key"), mat, Vector2.zero, 15);
            go.AddComponent<KeyPickup>();
        }

        static void Door(Transform root, Material mat, AsciiLevel.Rect r)
        {
            var size = new Vector2(r.w, r.h);
            var go = SandboxBuilder.Block(root, "LockedDoor", S("door_tile"), mat, new Vector2(r.x + r.w * 0.5f, r.y + r.h * 0.5f), size);
            go.GetComponent<SpriteRenderer>().sortingOrder = 6;
            Art(go.transform, "Lock", S("door_lock"), mat, Vector2.zero, 7);
            go.AddComponent<LockedDoor>();
        }

        static void Blocks(Transform root, Material mat, AsciiLevel.Rect r, bool red)
        {
            // One object per cell so each block can wait individually for the hero to move off it.
            for (int yy = r.y; yy < r.y + r.h; yy++)
            for (int xx = r.x; xx < r.x + r.w; xx++)
            {
                var go = SandboxBuilder.Block(root, red ? "RedBlock" : "BlueBlock", S(red ? "block_red" : "block_blue"), mat,
                                              new Vector2(xx + 0.5f, yy + 0.5f), Vector2.one);
                go.GetComponent<SpriteRenderer>().sortingOrder = 6;
                go.AddComponent<SwitchBlock>().red = red;
            }
        }

        static void Switch(Transform root, Material mat, Vector2 groundPos)
        {
            var go = new GameObject("Switch");
            go.transform.SetParent(root, false);
            go.transform.position = groundPos;
            Art(go.transform, "Base", S("switch_base"), mat, new Vector2(0f, 0.25f), 7);
            var cap = Art(go.transform, "Cap", S("switch_cap"), mat, new Vector2(0f, 0.32f), 8);
            go.AddComponent<SwitchButton>().cap = cap;
        }
    }
}
