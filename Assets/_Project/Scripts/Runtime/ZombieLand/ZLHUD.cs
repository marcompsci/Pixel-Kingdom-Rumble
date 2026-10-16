using System.Collections.Generic;
using PKR.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// Zombie Land HUD: health and armor, medkits, Graystone shards, the hound timer, the active mission with a
    /// waypoint arrow, a scrolling minimap (MAP opens the full map, missions and the clue board), the gun and its
    /// ammo, what USE will do, toasts, and the "you died" screen.
    /// </summary>
    public class ZLHUD : MonoBehaviour
    {
        ZLGame _g;
        RectTransform _safe;
        Image _hp, _armor, _vehicleBar, _reload, _missionBar, _arrow, _deathDim;
        Text _hpText, _shards, _hounds, _mission, _objective, _gun, _ammo, _prompt, _toast, _med, _death, _vehicleText, _scrap, _perks, _powers, _squad;
        int _shownScrap = -1;
        float _scrapPulse;
        RectTransform _arrowRt, _mapInner, _playerDot, _missionGroup, _vehicleGroup;
        readonly List<(RectTransform rt, Image img, System.Func<bool> show, System.Func<Color> color)> _dots =
            new List<(RectTransform, Image, System.Func<bool>, System.Func<Color>)>();
        ZLMapPanel _map;
        const float MapPx = 4f;      // minimap pixels per tile
        const float MiniSize = 230f;

        void Start()
        {
            _g = ZLGame.Current;
            _safe = UIFactory.CreateCanvas(transform, "ZLHUDCanvas", 20, landscape: true, out _);
            Build();
        }

        // ---- building blocks ---------------------------------------------------------------------------

        static RectTransform Box(RectTransform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var rt = UIFactory.Rect(name, parent);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        static Text Txt(RectTransform parent, string text, int size, TextAnchor align, Color c, bool display = true)
        {
            var rt = UIFactory.Rect("Text", parent);
            UIFactory.Stretch(rt);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = display ? UISprites.DisplayFont : UISprites.Font;
            t.fontSize = size;
            t.fontStyle = display && UISprites.HasDisplayFont ? FontStyle.Normal : FontStyle.Bold;
            t.alignment = align;
            t.color = c;
            t.text = text;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            var sh = rt.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.9f);
            sh.effectDistance = new Vector2(3f, -3f);
            return t;
        }

        static Image Bar(RectTransform parent, string name, Vector2 pos, Vector2 size, Color fill, out Image back)
        {
            var rt = Box(parent, name, new Vector2(0f, 1f), new Vector2(0f, 1f), pos, size);
            back = rt.gameObject.AddComponent<Image>();
            UISprites.ApplyBarSprite(back);
            back.color = new Color(0.12f, 0.12f, 0.16f, 0.9f);
            back.raycastTarget = false;
            var f = UIFactory.Rect("Fill", rt);
            f.anchorMin = Vector2.zero; f.anchorMax = Vector2.one;
            f.offsetMin = new Vector2(6f, 6f); f.offsetMax = new Vector2(-6f, -6f);
            var img = f.gameObject.AddComponent<Image>();
            img.color = fill;
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.raycastTarget = false;
            return img;
        }

        Button SmallButton(RectTransform parent, string label, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction onClick)
        {
            var holder = Box(parent, "Btn_" + label, anchor, pivot, pos, size);
            var b = UIFactory.Button(holder, label, onClick, size.y);
            var rt = (RectTransform)b.transform;
            UIFactory.Stretch(rt);
            b.GetComponentInChildren<Text>().fontSize = 30;
            return b;
        }

        void Build()
        {
            var white = Color.white;
            // Dusk over New Hollow: darkened screen edges under the HUD.
            var vig = UIFactory.Rect("Vignette", _safe);
            UIFactory.Stretch(vig);
            vig.offsetMin = new Vector2(-300f, -300f); vig.offsetMax = new Vector2(300f, 300f);
            var vi = vig.gameObject.AddComponent<Image>();
            vi.sprite = _g.Sprite("vignette");
            vi.raycastTarget = false;
            // Health / armor (top-left)
            _hp = Bar(_safe, "Health", new Vector2(30f, -24f), new Vector2(380f, 46f), new Color(0.9f, 0.25f, 0.25f), out var hpBack);
            _hpText = Txt((RectTransform)hpBack.transform, "100", 28, TextAnchor.MiddleCenter, white);
            _armor = Bar(_safe, "Armor", new Vector2(30f, -74f), new Vector2(380f, 26f), new Color(0.35f, 0.6f, 1f), out _);
            var medBtn = SmallButton(_safe, "MED", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(424f, -24f), new Vector2(150f, 76f), () => _g.UseMedkit());
            _med = medBtn.GetComponentInChildren<Text>();

            // Shards + hounds (top-centre)
            var top = Box(_safe, "Top", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(700f, 50f));
            _shards = Txt(top, "", 40, TextAnchor.MiddleCenter, new Color(0.85f, 0.85f, 1f));
            var hb = Box(_safe, "Hounds", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -68f), new Vector2(700f, 40f));
            _hounds = Txt(hb, "", 30, TextAnchor.MiddleCenter, white);

            // Active mission
            _missionGroup = Box(_safe, "Mission", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(760f, 110f));
            var mbg = _missionGroup.gameObject.AddComponent<Image>();
            UISprites.ApplyPanelSprite(mbg);
            mbg.color = new Color(0.08f, 0.08f, 0.14f, 0.75f);
            mbg.raycastTarget = false;
            var mt = Box(_missionGroup, "Name", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(720f, 36f));
            _mission = Txt(mt, "", 30, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.4f));
            var ot = Box(_missionGroup, "Obj", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -48f), new Vector2(720f, 30f));
            _objective = Txt(ot, "", 26, TextAnchor.MiddleCenter, white);
            var pbRt = Box(_missionGroup, "Prog", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(560f, 22f));
            var pbBack = pbRt.gameObject.AddComponent<Image>();
            UISprites.ApplyBarSprite(pbBack);
            pbBack.color = new Color(0.1f, 0.1f, 0.12f, 0.9f);
            var pbFill = UIFactory.Rect("Fill", pbRt);
            pbFill.anchorMin = Vector2.zero; pbFill.anchorMax = Vector2.one;
            pbFill.offsetMin = new Vector2(5f, 5f); pbFill.offsetMax = new Vector2(-5f, -5f);
            _missionBar = pbFill.gameObject.AddComponent<Image>();
            _missionBar.color = new Color(1f, 0.8f, 0.3f);
            _missionBar.type = Image.Type.Filled;
            _missionBar.fillMethod = Image.FillMethod.Horizontal;

            // Waypoint arrow (screen edge)
            _arrowRt = Box(_safe, "Arrow", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64f, 64f));
            _arrow = _arrowRt.gameObject.AddComponent<Image>();
            _arrow.sprite = _g.Sprite("Props/pk_arrow");
            _arrow.raycastTarget = false;

            // Minimap (top-right) + MAP
            // (top-left, under health: the top-right belongs to pause and the touch FIRE/USE buttons)
            var mini = Box(_safe, "Minimap", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -110f), new Vector2(MiniSize, MiniSize));
            var frame = mini.gameObject.AddComponent<Image>();
            UISprites.ApplyPanelSprite(frame);
            frame.color = UITheme.Current.panelEdge;
            var view = UIFactory.Rect("View", mini);
            UIFactory.Stretch(view);
            view.offsetMin = new Vector2(10f, 10f); view.offsetMax = new Vector2(-10f, -10f);
            view.gameObject.AddComponent<RectMask2D>();
            var bg = view.gameObject.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.15f, 0.25f);
            _mapInner = UIFactory.Rect("Map", view);
            _mapInner.anchorMin = _mapInner.anchorMax = new Vector2(0.5f, 0.5f);
            _mapInner.pivot = Vector2.zero;
            var city = _g.City;
            _mapInner.sizeDelta = new Vector2(city.w * MapPx, city.h * MapPx);
            var mapImg = _mapInner.gameObject.AddComponent<Image>();
            mapImg.sprite = _g.Minimap;
            mapImg.raycastTarget = false;
            BuildDots(_mapInner, MapPx, 10f, _dots);
            _playerDot = Box(view, "You", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(18f, 18f));
            var pd = _playerDot.gameObject.AddComponent<Image>();
            pd.sprite = _g.Sprite("Props/pk_arrow");
            pd.color = Color.white;
            if (!_g.IsLastStand)
                SmallButton(_safe, "MAP", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f + MiniSize + 12f, -110f), new Vector2(150f, 76f), OpenMap);

            // Scrap (Zombie Land) or points (Last Stand), perks and power-ups, under the minimap
            var sc = Box(_safe, "Scrap", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f, -350f), new Vector2(420f, 52f));
            _scrap = Txt(sc, "", 44, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.35f));
            var pk = Box(_safe, "Perks", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f, -400f), new Vector2(520f, 34f));
            _perks = Txt(pk, "", 24, TextAnchor.MiddleLeft, new Color(0.6f, 0.9f, 1f), false);
            var pw = Box(_safe, "Powers", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -232f), new Vector2(900f, 44f));
            _powers = Txt(pw, "", 34, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.35f));
            // Co-op: the squad (names, health, points)
            var sq = Box(_safe, "Squad", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f, -440f), new Vector2(520f, 150f));
            _squad = Txt(sq, "", 26, TextAnchor.UpperLeft, Color.white, false);
            _squad.supportRichText = true;

            // Vehicle health
            _vehicleGroup = Box(_safe, "Vehicle", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-190f, 90f), new Vector2(380f, 60f));
            _vehicleBar = Bar(_vehicleGroup, "VehBar", new Vector2(0f, -26f), new Vector2(380f, 30f), new Color(1f, 0.7f, 0.3f), out _);
            var vt = Box(_vehicleGroup, "VehName", new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(380f, 24f));
            _vehicleText = Txt(vt, "", 22, TextAnchor.MiddleLeft, white);

            // Gun (bottom-centre) + prompt
            var gunBox = Box(_safe, "Gun", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(520f, 90f));
            var gn = Box(gunBox, "Name", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(520f, 36f));
            _gun = Txt(gn, "", 28, TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.6f));
            var ga = Box(gunBox, "Ammo", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(520f, 40f));
            _ammo = Txt(ga, "", 38, TextAnchor.MiddleCenter, Color.white);
            var rl = Box(gunBox, "Reload", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(240f, 10f));
            _reload = rl.gameObject.AddComponent<Image>();
            _reload.color = new Color(1f, 0.85f, 0.4f);
            _reload.type = Image.Type.Filled;
            _reload.fillMethod = Image.FillMethod.Horizontal;
            var pr = Box(_safe, "Prompt", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 130f), new Vector2(800f, 44f));
            _prompt = Txt(pr, "", 32, TextAnchor.MiddleCenter, new Color(0.7f, 1f, 0.75f));

            // Toast
            var tt = Box(_safe, "Toast", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(1400f, 60f));
            _toast = Txt(tt, "", 34, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.8f));

            // Death
            var dd = UIFactory.Rect("Death", _safe);
            UIFactory.Stretch(dd);
            _deathDim = dd.gameObject.AddComponent<Image>();
            _deathDim.color = new Color(0.25f, 0f, 0f, 0.6f);
            _deathDim.raycastTarget = false;
            _death = Txt(dd, "YOU DIED", 110, TextAnchor.MiddleCenter, new Color(1f, 0.3f, 0.3f));

            _map = gameObject.AddComponent<ZLMapPanel>();
        }

        /// <summary>Markers on a map image: safehouses, known missions, the active objective, relic cases, lairs.</summary>
        public static void BuildDots(RectTransform inner, float px, float size,
                                     List<(RectTransform, Image, System.Func<bool>, System.Func<Color>)> dots)
        {
            var g = ZLGame.Current;
            void Dot(Vector2 world, Sprite s, float sz, System.Func<bool> show, System.Func<Color> color)
            {
                var rt = UIFactory.Rect("Dot", inner);
                rt.anchorMin = rt.anchorMax = Vector2.zero;
                rt.anchoredPosition = world * px;
                rt.sizeDelta = new Vector2(sz, sz);
                var img = rt.gameObject.AddComponent<Image>();
                img.sprite = s;
                img.raycastTarget = false;
                dots.Add((rt, img, show, color));
            }
            var dot = g.Sprite("Props/pk_dot");
            foreach (var s in g.Safehouses)
            {
                var sh = s;
                Dot(s.transform.position, g.Sprite("Props/pk_safehouse_lit"), size * 1.4f, () => true,
                    () => sh.Lit ? new Color(0.5f, 1f, 0.6f) : new Color(0.5f, 0.7f, 0.55f, 0.6f));
            }
            foreach (var m in g.Markers)
            {
                var mk = m;
                Dot(m.transform.position, dot, size * 1.3f,
                    () => !mk.Done && g.Progress.leads.Contains(mk.Spec.id),
                    () => g.Active != null && g.Active.Spec == mk.Spec ? new Color(1f, 0.3f, 0.3f) : mk.Spec.required ? new Color(1f, 0.85f, 0.3f) : new Color(0.5f, 0.85f, 1f));
            }
            foreach (var r in g.City.relics)
            {
                var spec = ZLCatalog.Relic((ZLRelic)System.Enum.Parse(typeof(ZLRelic), r.relic));
                Dot(new Vector2(r.x, r.y), g.Sprite("Props/pk_relic"), size * 1.3f, () => true, () => new Color32(spec.r, spec.g, spec.b, 255));
            }
            foreach (var l in g.City.lairs)
            {
                var id = l.id;
                Dot(new Vector2(l.x, l.y), g.Sprite("Props/pk_lair"), size * 1.3f, () => !g.Progress.bossDefeated,
                    () => g.Progress.lairsSearched.Contains(id) || !g.Puzzle.LairsLeft(g.Progress.completed).Contains(id) ? new Color(0.5f, 0.5f, 0.5f, 0.6f) : Color.white);
            }
            // Scout's Eye (Survivor Rank): guns and ammo lying about show on the map
            if (SurvivorRank.Has(g.CareerXp, "zl_scout"))
                foreach (var l in g.Loot)
                {
                    var loot = l;
                    if (loot == null || loot.temporary) continue;
                    Dot(loot.transform.position, g.Sprite("Props/pk_" + (ZLCatalog.Gun(loot.kind) != null ? "weapon" : loot.kind)), size * 1.1f,
                        () => loot != null && loot.Available, () => new Color(1f, 1f, 1f, 0.85f));
                }
            // the active objective
            Dot(Vector2.zero, g.Sprite("Props/pk_marker"), size * 1.8f, () => g.Active != null, () => new Color(1f, 0.4f, 0.3f));
        }

        public static void UpdateDots(List<(RectTransform rt, Image img, System.Func<bool> show, System.Func<Color> color)> dots, float px)
        {
            var g = ZLGame.Current;
            for (int i = 0; i < dots.Count; i++)
            {
                var d = dots[i];
                bool on = d.show();
                if (d.img.enabled != on) d.img.enabled = on;
                if (!on) continue;
                d.img.color = d.color();
                if (i == dots.Count - 1 && g.Active != null) d.rt.anchoredPosition = g.Active.Waypoint * px;
            }
        }

        void OpenMap()
        {
            if (_g.Paused) return;
            _map.Open();
        }

        void Update()
        {
            if (_g == null || !_g.Ready || _hp == null) return;
            var p = _g.Player;
            var prog = _g.Progress;
            if (p == null) return;

            _hp.fillAmount = p.Health / (float)p.MaxHealth;
            _hpText.text = p.Health.ToString();
            _armor.fillAmount = prog.armor / (float)ZLRules.MaxArmor;
            _armor.transform.parent.gameObject.SetActive(prog.armor > 0);
            _med.text = $"MED x{prog.medkits}";

            // Scrap / points, perks, power-ups
            int money = _g.IsLastStand ? _g.Stand.Run.Points : prog.scrap;
            if (money != _shownScrap)
            {
                if (_shownScrap >= 0 && money > _shownScrap) _scrapPulse = 1f;
                _shownScrap = money;
                _scrap.text = _g.IsLastStand ? $"{money}" : $"SCRAP {money}";
            }
            _scrapPulse = Mathf.MoveTowards(_scrapPulse, 0f, Time.unscaledDeltaTime * 4f);
            _scrap.transform.localScale = Vector3.one * (1f + _scrapPulse * 0.12f);
            _scrap.color = _g.DoubleScrap ? new Color(1f, 1f, 0.6f) : new Color(1f, 0.85f, 0.35f);
            var ps = new System.Text.StringBuilder();
            foreach (var k in prog.perks)
                if (System.Enum.TryParse(k, out ZLPerk perk)) ps.Append(ps.Length > 0 ? " · " : "").Append(ZLShop.PerkName(perk));
            _perks.text = ps.ToString();
            ps.Clear();
            if (_g.InstaKillLeft > 0f) ps.Append($"INSTA-KILL {Mathf.CeilToInt(_g.InstaKillLeft)}   ");
            if (_g.DoubleScrapLeft > 0f) ps.Append((_g.IsLastStand ? "DOUBLE POINTS " : "DOUBLE SCRAP ") + Mathf.CeilToInt(_g.DoubleScrapLeft));
            _powers.text = ps.ToString();

            if (_g.IsLastStand)
            {
                UpdateLastStand(p);
                return;
            }

            _shards.text = prog.bossDefeated ? "ZOMBIE LAND CLEARED" : $"GRAYSTONE SHARDS {prog.Shards}/{ZLCatalog.ShardsNeeded}";
            if (prog.bossDefeated) _hounds.text = "";
            else if (_g.Boss != null) { _hounds.text = $"THE FIRST TURNED {Mathf.CeilToInt(_g.Boss.Health01 * 100f)}%"; _hounds.color = new Color(1f, 0.4f, 0.5f); }
            else if (_g.InSafehouse) { _hounds.text = "SAFEHOUSE: THE HOUNDS CAN'T SMELL YOU"; _hounds.color = new Color(0.6f, 1f, 0.7f); }
            else if (_g.HoundsIn > 0f)
            {
                int s = Mathf.CeilToInt(_g.HoundsIn);
                _hounds.text = $"HOUNDS IN {s / 60}:{s % 60:00}";
                _hounds.color = s < 30 ? new Color(1f, 0.5f, 0.4f) : new Color(1f, 1f, 1f, 0.8f);
            }
            else { _hounds.text = "THE BLOOD HOUNDS ARE HUNTING YOU"; _hounds.color = new Color(1f, 0.3f, 0.3f); }

            var run = _g.Active;
            _missionGroup.gameObject.SetActive(run != null);
            if (run != null)
            {
                _mission.text = run.Spec.name;
                _objective.text = run.Objective;
                _missionBar.fillAmount = run.Progress01;
            }

            // Waypoint arrow: the active mission, else the nearest known unfinished mission
            Vector2? wp = run != null ? run.Waypoint : NearestLead();
            if (_g.Boss != null) wp = _g.Boss.transform.position;
            var cam = Camera.main;
            if (wp.HasValue && cam != null)
            {
                Vector2 vp = cam.WorldToViewportPoint(wp.Value);
                bool onScreen = vp.x > 0.08f && vp.x < 0.92f && vp.y > 0.12f && vp.y < 0.88f;
                _arrow.enabled = !onScreen;
                if (!onScreen)
                {
                    Vector2 c = new Vector2(0.5f, 0.5f);
                    Vector2 dir = (vp - c).normalized;
                    var canvasSize = _safe.rect.size;
                    Vector2 edge = new Vector2(dir.x * canvasSize.x * 0.42f, dir.y * canvasSize.y * 0.38f);
                    _arrowRt.anchoredPosition = edge;
                    _arrowRt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
                    _arrow.color = run != null ? new Color(1f, 0.45f, 0.35f) : new Color(1f, 0.85f, 0.35f);
                }
            }
            else _arrow.enabled = false;

            // Minimap follows the hero
            Vector2 at = _g.TargetPos;
            _mapInner.anchoredPosition = -at * MapPx;
            UpdateDots(_dots, MapPx);
            float heading = p.Driving != null ? p.Driving.transform.eulerAngles.z - 90f : Mathf.Atan2(p.Aim.y, p.Aim.x) * Mathf.Rad2Deg - 90f;
            _playerDot.localRotation = Quaternion.Euler(0f, 0f, heading);

            // Vehicle
            _vehicleGroup.gameObject.SetActive(p.Driving != null);
            if (p.Driving != null)
            {
                _vehicleBar.fillAmount = p.Driving.Health01;
                _vehicleText.text = p.Driving.spec.name;
            }

            // Gun
            var gun = p.Gun;
            var ammo = p.Ammo;
            _gun.text = gun != null ? gun.name + (gun.isRelic ? "  (RELIC)" : "") : "";
            _gun.color = gun != null && gun.forged ? new Color(0.85f, 0.7f, 1f) : gun != null && gun.wonder ? new Color(0.6f, 0.9f, 1f) : new Color(1f, 0.9f, 0.6f);
            _ammo.text = ammo != null ? $"{ammo.mag} / {ammo.reserve}" : "";
            _ammo.color = ammo != null && ammo.mag + ammo.reserve == 0 ? new Color(1f, 0.4f, 0.4f) : Color.white;
            _reload.fillAmount = p.Reloading ? p.Reload01 : 0f;
            _gun.transform.parent.parent.gameObject.SetActive(p.Driving == null);

            // Prompt
            if (p.Driving != null) _prompt.text = "USE: GET OUT";
            else _prompt.text = p.Nearby != null ? "USE: " + p.Nearby.Prompt : "";

            _toast.text = _g.ToastLeft > 0f ? _g.Toast : "";
            var tc = _toast.color; tc.a = Mathf.Clamp01(_g.ToastLeft * 2f); _toast.color = tc;

            bool dead = _g.RespawnPending;
            _deathDim.enabled = dead;
            _death.enabled = dead;
        }

        /// <summary>Last Stand: round, what's left, and an arrow to the last few of the dead.</summary>
        void UpdateLastStand(ZLPlayer p)
        {
            var st = _g.Stand;
            var run = st.Run;
            _missionGroup.gameObject.SetActive(false);
            _shards.text = run.Round == 0 ? "LAST STAND" : st.Mode == SurvivalMode.Raid ? $"ROUND {run.Round}/{SurvivalModes.RaidRounds}" : $"ROUND {run.Round}";
            _shards.color = new Color(1f, 0.35f, 0.3f);
            if (st.Over) _hounds.text = "";
            else if (st.Intermission) { _hounds.text = run.Round == 0 ? $"GET READY  {Mathf.CeilToInt(st.IntermissionLeft)}" : $"NEXT ROUND IN {Mathf.CeilToInt(st.IntermissionLeft)}"; _hounds.color = Color.white; }
            else { _hounds.text = (st.HoundRound ? "HOUNDS LEFT " : "ZOMBIES LEFT ") + st.ZombiesLeft; _hounds.color = st.HoundRound ? new Color(1f, 0.4f, 0.3f) : Color.white; }
            if (st.Mode == SurvivalMode.Hardcore && !st.Over) _hounds.text = "HARDCORE   " + _hounds.text;

            // arrow to the nearest of the last three
            Vector2? wp = null;
            if (!st.Intermission && st.ZombiesLeft <= 3)
            {
                float bd = float.MaxValue;
                foreach (var z in st.Alive)
                {
                    if (z == null || !z.Alive) continue;
                    float d = Vector2.Distance(z.transform.position, _g.TargetPos);
                    if (d < bd) { bd = d; wp = z.transform.position; }
                }
            }
            var cam = Camera.main;
            _arrow.enabled = false;
            if (wp.HasValue && cam != null)
            {
                Vector2 vp = cam.WorldToViewportPoint(wp.Value);
                bool onScreen = vp.x > 0.08f && vp.x < 0.92f && vp.y > 0.12f && vp.y < 0.88f;
                if (!onScreen)
                {
                    _arrow.enabled = true;
                    Vector2 dir = (vp - new Vector2(0.5f, 0.5f)).normalized;
                    var canvasSize = _safe.rect.size;
                    _arrowRt.anchoredPosition = new Vector2(dir.x * canvasSize.x * 0.42f, dir.y * canvasSize.y * 0.38f);
                    _arrowRt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
                    _arrow.color = new Color(1f, 0.35f, 0.3f);
                }
            }

            _mapInner.anchoredPosition = -_g.TargetPos * MapPx;
            float heading = Mathf.Atan2(p.Aim.y, p.Aim.x) * Mathf.Rad2Deg - 90f;
            _playerDot.localRotation = Quaternion.Euler(0f, 0f, heading);
            _vehicleGroup.gameObject.SetActive(false);

            var gun = p.Gun;
            var ammo = p.Ammo;
            _gun.text = gun != null ? gun.name : "";
            _gun.color = gun != null && gun.forged ? new Color(0.85f, 0.7f, 1f) : gun != null && gun.wonder ? new Color(0.6f, 0.9f, 1f) : new Color(1f, 0.9f, 0.6f);
            _ammo.text = ammo != null ? $"{ammo.mag} / {ammo.reserve}" : "";
            _ammo.color = ammo != null && ammo.mag + ammo.reserve == 0 ? new Color(1f, 0.4f, 0.4f) : Color.white;
            _reload.fillAmount = p.Reloading ? p.Reload01 : 0f;
            if (st.Over) _prompt.text = "";
            else if (p.Downed) _prompt.text = $"YOU'RE DOWN  {Mathf.CeilToInt(p.BleedOut)}s  ·  A TEAMMATE CAN REVIVE YOU";
            else if (p.Out) _prompt.text = "OUT UNTIL THE NEXT ROUND";
            else if (p.ReviveTarget != null)
                _prompt.text = $"HOLD USE: REVIVE {p.ReviveTarget.DisplayName}  {Mathf.RoundToInt(100f * p.ReviveTarget.ReviveProgress / SquadRules.ReviveSeconds)}%";
            else _prompt.text = p.Nearby != null ? "USE: " + p.Nearby.Prompt : "";
            // the squad
            if (_g.Squad.Count > 1)
            {
                var sb = new System.Text.StringBuilder();
                foreach (var h in _g.Squad)
                {
                    if (h == null || h == p) continue;
                    string col = ColorUtility.ToHtmlStringRGB(h.Downed ? new Color(1f, 0.4f, 0.4f) : h.Out ? new Color(0.6f, 0.6f, 0.6f) : Color.white);
                    string state = h.Downed ? $"DOWN {Mathf.CeilToInt(h.BleedOut)}s" : h.Out ? "OUT" : $"{h.Health} HP";
                    int pts = h.Wallet != null ? h.Wallet.Points : 0;
                    sb.Append($"<color=#{col}>{h.DisplayName}  {state}  ·  {pts}</color>\n");
                }
                _squad.text = sb.ToString();
            }
            else _squad.text = "";
            _toast.text = _g.ToastLeft > 0f ? _g.Toast : "";
            var tc = _toast.color; tc.a = Mathf.Clamp01(_g.ToastLeft * 2f); _toast.color = tc;
            _deathDim.enabled = st.Over && !st.Run.Won;
            _death.enabled = false;
        }

        Vector2? NearestLead()
        {
            Vector2? best = null;
            float bd = float.MaxValue;
            foreach (var m in _g.Markers)
            {
                if (m.Done || !_g.Progress.leads.Contains(m.Spec.id)) continue;
                float d = Vector2.Distance(m.transform.position, _g.TargetPos);
                if (d < bd) { bd = d; best = m.transform.position; }
            }
            return best;
        }
    }
}
