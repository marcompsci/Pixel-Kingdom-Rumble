using System.Collections.Generic;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>A safehouse: walk up to light it (it becomes your respawn checkpoint). Nothing spawns next to it, the
    /// hound timer pauses inside, and USE opens its supply crate: rest (once a minute, plus rewards you've earned),
    /// and spend SCRAP on supplies, perks, the mystery box and the upgrade forge.</summary>
    public class ZLSafehouse : ZLMarkerBase, IZLUse
    {
        public const float SafeRadius = 4f;
        public ZLCityData.Safehouse Data { get; private set; }
        float _restCool;
        bool _lit;

        public Vector2 UsePos => transform.position;
        public float UseRange => 2.2f;
        public bool CanUse => _lit;
        public string Prompt => "SUPPLIES & REST";
        public bool CanRest => _restCool <= 0f;
        public int RestSeconds => Mathf.CeilToInt(Mathf.Max(0f, _restCool));
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
            var panel = game.GetComponent<ZLShopPanel>();
            if (panel == null) panel = game.gameObject.AddComponent<ZLShopPanel>();
            panel.Open(this);
        }

        /// <summary>Rest: full health once a minute (and the rewards you've earned).</summary>
        public void Rest(ZLPlayer p)
        {
            if (!CanRest) return;
            _restCool = 60f;
            p.Heal(p.MaxHealth);
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
}
