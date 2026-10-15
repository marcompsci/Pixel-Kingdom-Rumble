using System.Collections.Generic;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>Shared look for world markers: a pulsing sprite and an optional floating label.</summary>
    public abstract class ZLMarkerBase : MonoBehaviour
    {
        protected ZLGame game;
        protected SpriteRenderer sr;
        protected TextMesh label;
        protected float t;

        protected void Setup(ZLGame g, Sprite s, string text, Color labelColor, float scale = 1.2f)
        {
            game = g;
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sortingOrder = ZLSort.ByY(transform.position.y);
            if (g.material != null) sr.sharedMaterial = g.material;
            transform.localScale = Vector3.one * scale;
            if (text != null)
            {
                var go = new GameObject("Label");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                label = go.AddComponent<TextMesh>();
                label.font = UISprites.DisplayFont;
                label.fontSize = 40;
                label.characterSize = 0.045f / scale;
                label.anchor = TextAnchor.MiddleCenter;
                label.color = labelColor;
                label.text = text;
                var mr = go.GetComponent<MeshRenderer>();
                mr.sharedMaterial = UISprites.DisplayFont.material;
                mr.sortingOrder = ZLSort.Overhead;
            }
        }

        protected virtual void Update()
        {
            t += Time.deltaTime;
        }
    }

    /// <summary>A safehouse: walk up to light it (it becomes your respawn checkpoint). Nothing spawns next to it, the
    /// hound timer pauses inside, and resting (USE) heals you once a minute (plus rewards you've earned).</summary>
    public class ZLSafehouse : ZLMarkerBase, IZLUse
    {
        public const float SafeRadius = 4f;
        public ZLCityData.Safehouse Data { get; private set; }
        float _restCool;
        bool _lit;

        public Vector2 UsePos => transform.position;
        public float UseRange => 2.2f;
        public bool CanUse => _lit && _restCool <= 0f;
        public string Prompt => "REST";
        public bool Lit => _lit;

        public void Init(ZLGame g, ZLCityData.Safehouse s)
        {
            Data = s;
            _lit = g.Progress.safehouses.Contains(s.id);
            Setup(g, g.Sprite(_lit ? "Props/pk_safehouse_lit" : "Props/pk_safehouse"), s.name, new Color(0.6f, 1f, 0.7f), 1.6f);
            var ring = new GameObject("Zone").AddComponent<SpriteRenderer>();
            ring.transform.SetParent(transform, false);
            ring.sprite = GlowHalo.Sprite;
            ring.color = new Color(0.4f, 1f, 0.5f, 0.12f);
            ring.transform.localScale = Vector3.one * SafeRadius * 1.2f;
            ring.sortingOrder = ZLSort.Decal + 2;
        }

        public void Touch()
        {
            if (Vector2.Distance(transform.position, game.TargetPos) > 3f) return;
            if (!_lit)
            {
                _lit = true;
                sr.sprite = game.Sprite("Props/pk_safehouse_lit");
            }
            game.LightSafehouse(this);
        }

        public void Use(ZLPlayer p)
        {
            _restCool = 60f;
            p.Heal(ZLRules.HeroHealth);
            var prog = game.Progress;
            if (prog.rewards.Contains("assault_rifle")) { ZLRules.AmmoBox(prog); ZLRules.AmmoBox(prog); game.ShowToast("RESTED. HEALTH AND AMMO TOPPED UP."); }
            else game.ShowToast("RESTED. HEALTH RESTORED.");
            if (prog.rewards.Contains("medkits")) prog.medkits = ZLRules.MaxMedkits;
            game.Save(true);
        }

        protected override void Update()
        {
            base.Update();
            _restCool -= Time.deltaTime;
            if (sr != null) sr.color = _lit ? Color.Lerp(Color.white, new Color(0.7f, 1f, 0.7f), Mathf.PingPong(t, 1f)) : Color.white;
        }
    }

    /// <summary>Ammo, medkits, armor and guns lying around the city. Guns are taken for good; the rest come back.</summary>
    public class ZLLoot : ZLMarkerBase
    {
        public string id, kind;
        public bool temporary;
        float _respawn, _life = 30f;
        public bool Available => _respawn <= 0f;

        public void Init(ZLGame g, string lootId, string lootKind, Sprite s)
        {
            id = lootId; kind = lootKind;
            var gun = ZLCatalog.Gun(lootKind);
            Setup(g, s, gun != null ? gun.name : null, new Color(1f, 0.9f, 0.6f), 1f);
        }

        public void Take()
        {
            if (ZLCatalog.Gun(kind) != null) { Destroy(gameObject); return; }
            _respawn = 240f;
            sr.enabled = false;
        }

        public void Tick(float dt)
        {
            if (temporary) { _life -= dt; if (_life <= 0f) { Destroy(gameObject); return; } }
            if (_respawn > 0f)
            {
                _respawn -= dt;
                if (_respawn <= 0f) sr.enabled = true;
            }
        }

        protected override void Update()
        {
            base.Update();
            if (sr != null && sr.enabled) sr.transform.localPosition = new Vector3(0f, Mathf.Sin(t * 3f) * 0.05f, 0f);
        }
    }

    /// <summary>A relic case: take its relic weapon (you can carry one relic at a time) and refill it here.</summary>
    public class ZLRelicCase : ZLMarkerBase, IZLUse
    {
        public ZLRelic relic;
        public Vector2 UsePos => transform.position;
        public float UseRange => 1.8f;
        public bool CanUse => true;
        public string Prompt => "TAKE " + ZLCatalog.Relic(relic).name;

        public void Init(ZLGame g, ZLRelic r)
        {
            relic = r;
            var spec = ZLCatalog.Relic(r);
            Setup(g, g.Sprite("Props/pk_relic"), spec.name, new Color32(spec.r, spec.g, spec.b, 255), 1.5f);
        }

        public void Use(ZLPlayer p)
        {
            var gun = ZLCatalog.RelicGun(relic);
            bool had = game.Progress.Gun(gun.id) != null;
            ZLRules.GiveGun(game.Progress, gun, gun.magazine + gun.maxReserve);
            game.Progress.equipped = gun.id;
            p.CancelReload();
            Sfx.Play(Sfx.Relic, 0.9f);
            game.Save(true);
            if (had) game.ShowToast(gun.name + " REFILLED.");
            else game.Story("", new[] { ZLCatalog.Relic(relic).blurb, "Only one of the four relics can hurt the First Turned. You can carry one at a time." }, "zl_city", null);
        }

        protected override void Update()
        {
            base.Update();
            if (sr != null) sr.color = Color.Lerp(Color.white, new Color(1f, 0.9f, 1f), Mathf.PingPong(t * 2f, 1f));
        }
    }

    /// <summary>One of the six possible lairs. Sealed until you carry ten shards; only the real one holds him.</summary>
    public class ZLLairDoor : ZLMarkerBase, IZLUse
    {
        public string lairId;
        public Vector2 UsePos => transform.position;
        public float UseRange => 2.2f;
        public bool CanUse => !game.Progress.bossDefeated && game.Boss == null;
        public string Prompt => "SEARCH " + ZLCatalog.Lair(lairId).name.ToUpperInvariant();

        public void Init(ZLGame g, string id)
        {
            lairId = id;
            Setup(g, g.Sprite("Props/pk_lair"), "?", new Color(1f, 0.5f, 0.5f), 1.4f);
        }

        public void Use(ZLPlayer p) => game.TryLair(lairId, transform.position);

        protected override void Update()
        {
            base.Update();
            if (label != null)
                label.text = game.Progress.lairsSearched.Contains(lairId) ? "EMPTY" : game.Progress.Shards >= ZLCatalog.ShardsNeeded ? "LAIR?" : "SEALED";
        }
    }

    /// <summary>A mission giver / start point. Unknown missions show "?" until you find the lead.</summary>
    public class ZLMissionMarker : ZLMarkerBase, IZLUse
    {
        public ZLMissionSpec Spec { get; private set; }
        public ZLCityData.Mission Site { get; private set; }
        public bool Done => game.Progress.Done(Spec.id);

        public Vector2 UsePos => transform.position;
        public float UseRange => 2.2f;
        public bool CanUse => !Done && game.Active == null;
        public string Prompt => "START " + Spec.name;

        public void Init(ZLGame g, ZLMissionSpec spec, ZLCityData.Mission site)
        {
            Spec = spec; Site = site;
            Setup(g, g.Sprite("Props/pk_marker"), spec.name, spec.required ? new Color(1f, 0.85f, 0.4f) : new Color(0.6f, 0.9f, 1f), 1.8f);
            Refresh();
        }

        public void Refresh()
        {
            bool known = game.Progress.leads.Contains(Spec.id);
            if (label != null) label.text = Done ? "" : known ? Spec.name : "?";
            if (sr != null) sr.enabled = !Done;
        }

        public void Use(ZLPlayer p) => game.StartMission(this);

        protected override void Update()
        {
            base.Update();
            if (Done) return;
            float k = 1f + Mathf.Sin(t * 4f) * 0.08f;
            sr.transform.localScale = Vector3.one * k;
            if (label != null && Time.frameCount % 30 == 0) Refresh();
            if (game.Active != null && game.Active.Spec == Spec) sr.color = new Color(1f, 1f, 1f, 0.35f);
            else sr.color = Color.white;
        }
    }

    /// <summary>A simple mission prop (cooler, folder, generator, survivor, destination flag).</summary>
    public class ZLMissionProp : ZLMarkerBase
    {
        public float progress;
        public bool done;
        public void Init(ZLGame g, Sprite s, string text, Color c, float scale) => Setup(g, s, text, c, scale);
        public void SetSprite(Sprite s) { if (sr != null) sr.sprite = s; }
        public void SetColor(Color c, int order) { if (sr != null) { sr.color = c; sr.sortingOrder = order; } _fixedOrder = true; }
        bool _fixedOrder;
        public void SetLabel(string text) { if (label != null) label.text = text; }
        public void Show(bool on) { if (sr != null) sr.enabled = on; if (label != null) label.gameObject.SetActive(on); }
        protected override void Update()
        {
            base.Update();
            if (sr != null && !_fixedOrder) sr.sortingOrder = ZLSort.ByY(transform.position.y);
        }
    }
}
