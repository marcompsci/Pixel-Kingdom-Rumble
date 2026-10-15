using System.Collections.Generic;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// One mission in progress. Types: Signal / Hold Out (stay in the zone while waves come), Retrieve (collect the
    /// items around the site), Rescue (lead a survivor to a safehouse), Purge Nest (destroy the big nest), Power Up
    /// (start every generator), Hunt Herald (kill the district lieutenant), Delivery (drive the vehicle to the
    /// destination before time runs out).
    /// </summary>
    public class ZLMissionRun
    {
        public ZLMissionSpec Spec { get; }
        public ZLCityData.Mission Site { get; }
        readonly ZLGame _g;
        readonly List<ZLMissionProp> _props = new List<ZLMissionProp>();
        readonly List<ZLZombie> _spawned = new List<ZLZombie>();
        float _timer, _wave, _clock;
        int _count;
        ZLMissionProp _survivor, _dest, _zone;
        ZLNest _nest;
        ZLZombie _herald;
        ZLVehicle _vehicle;
        bool _running;
        Vector2 SitePos => new Vector2(Site.x, Site.y);

        public ZLMissionRun(ZLGame g, ZLMissionSpec spec, ZLCityData.Mission site)
        {
            _g = g; Spec = spec; Site = site;
        }

        /// <summary>Where the objective is right now (for the HUD arrow and the map).</summary>
        public Vector2 Waypoint
        {
            get
            {
                switch (Spec.type)
                {
                    case ZLMissionType.Retrieve:
                    case ZLMissionType.PowerUp:
                        foreach (var p in _props) if (!p.done) return p.transform.position;
                        return SitePos;
                    case ZLMissionType.Rescue:
                        if (_survivor != null && !_running) return _survivor.transform.position;
                        return NearestSafehouse(_g.TargetPos);
                    case ZLMissionType.HuntHerald:
                        return _herald != null ? (Vector2)_herald.transform.position : SitePos;
                    case ZLMissionType.Delivery:
                        return _running && Site.dest != null ? new Vector2(Site.dest.x, Site.dest.y) : _vehicle != null ? (Vector2)_vehicle.transform.position : SitePos;
                    default:
                        return SitePos;
                }
            }
        }

        public string Objective
        {
            get
            {
                switch (Spec.type)
                {
                    case ZLMissionType.Signal: return $"HOLD THE MAST: {Mathf.CeilToInt(Spec.seconds - _timer)}s";
                    case ZLMissionType.HoldOut: return $"HOLD THE ZONE: {Mathf.CeilToInt(Spec.seconds - _timer)}s";
                    case ZLMissionType.Retrieve: return $"FIND THEM: {_count}/{Spec.count}";
                    case ZLMissionType.PowerUp: return $"START THE GENERATORS: {_count}/{Spec.count}";
                    case ZLMissionType.Rescue: return _running ? "LEAD THEM TO A SAFEHOUSE" : "FIND THE SURVIVOR";
                    case ZLMissionType.PurgeNest: return _nest != null ? $"DESTROY THE NEST: {Mathf.CeilToInt(100f * _nest.hp / Mathf.Max(1, _nest.maxHp))}%" : "DESTROY THE NEST";
                    case ZLMissionType.HuntHerald: return _herald != null ? $"KILL THE HERALD: {Mathf.CeilToInt(100f * _herald.hp / Mathf.Max(1, _herald.maxHp))}%" : "KILL THE HERALD";
                    case ZLMissionType.Delivery: return _running ? $"DRIVE! {Mathf.CeilToInt(Spec.seconds - _clock)}s" : "GET IN THE VEHICLE";
                }
                return "";
            }
        }

        public float Progress01
        {
            get
            {
                switch (Spec.type)
                {
                    case ZLMissionType.Signal:
                    case ZLMissionType.HoldOut: return _timer / Mathf.Max(1f, Spec.seconds);
                    case ZLMissionType.Retrieve:
                    case ZLMissionType.PowerUp: return _count / (float)Mathf.Max(1, Spec.count);
                    case ZLMissionType.PurgeNest: return _nest != null ? 1f - _nest.hp / (float)Mathf.Max(1, _nest.maxHp) : 0f;
                    case ZLMissionType.HuntHerald: return _herald != null ? 1f - _herald.hp / (float)Mathf.Max(1, _herald.maxHp) : 0f;
                    case ZLMissionType.Delivery: return _running ? _clock / Mathf.Max(1f, Spec.seconds) : 0f;
                }
                return 0f;
            }
        }

        int Threat => _g.ThreatAt(SitePos);

        public void Begin()
        {
            switch (Spec.type)
            {
                case ZLMissionType.Signal:
                case ZLMissionType.HoldOut:
                    _zone = Prop(SitePos, GlowHalo.Sprite, null, Color.white, Spec.type == ZLMissionType.Signal ? 3.5f : 6f);
                    _zone.SetColor(new Color(1f, 0.85f, 0.3f, 0.22f), ZLSort.Decal + 3);
                    break;
                case ZLMissionType.Retrieve:
                {
                    string art = Spec.id == "m_notes" ? "Props/pk_folder" : "Props/pk_cooler";
                    foreach (var p in Points(Spec.count)) Prop(p, _g.Sprite(art), null, Color.white, 1.4f);
                    break;
                }
                case ZLMissionType.PowerUp:
                    foreach (var p in Points(Spec.count)) Prop(p, _g.Sprite("Props/pk_generator"), "GENERATOR", new Color(1f, 0.9f, 0.5f), 1.6f);
                    break;
                case ZLMissionType.Rescue:
                    _survivor = Prop(SitePos + Vector2.right * 1.2f, _g.survivorFrames.Length > 0 ? _g.survivorFrames[0] : _g.Sprite("Props/pk_marker"),
                                     Spec.giver, new Color(0.7f, 1f, 0.8f), 0.9f);
                    break;
                case ZLMissionType.PurgeNest:
                    _nest = _g.SpawnMissionNest(SitePos + Vector2.up * 2.5f, Spec.id, Threat);
                    for (int i = 0; i < 4; i++) Guard(SitePos + Random.insideUnitCircle * 4f, ZLFoe.Brute);
                    break;
                case ZLMissionType.HuntHerald:
                {
                    var at = SitePos + Vector2.up * 3f;
                    if (!_g.City.Walkable(at)) at = SitePos;
                    _herald = _g.SpawnZombie(ZLFoe.Herald, at, Mathf.Max(3, Threat), false);
                    _herald.pinned = true;
                    _herald.missionTag = Spec.id;
                    _spawned.Add(_herald);
                    for (int i = 0; i < 5; i++) Guard(at + Random.insideUnitCircle * 3f, i < 2 ? ZLFoe.Brute : ZLFoe.Runner);
                    break;
                }
                case ZLMissionType.Delivery:
                {
                    var kind = Spec.id == "b_joyride" ? ZLVehicleKind.Motorcycle : ZLVehicleKind.Truck;
                    var at = SitePos + Vector2.right * 2f;
                    if (!_g.City.Walkable(at)) at = SitePos;
                    _vehicle = _g.SpawnVehicle(kind, at, 0f, 1, kind == ZLVehicleKind.Truck ? new Color(1f, 0.55f, 0.5f) : (Color?)null);
                    _vehicle.missionTag = Spec.id;
                    if (Site.dest != null)
                        _dest = Prop(new Vector2(Site.dest.x, Site.dest.y), _g.Sprite("Props/pk_marker"), "DROP-OFF", new Color(0.6f, 1f, 0.6f), 2.5f);
                    break;
                }
            }
        }

        IEnumerable<Vector2> Points(int n)
        {
            int k = 0;
            if (Site.points != null)
                foreach (var p in Site.points) { if (k++ >= n) yield break; yield return new Vector2(p.x, p.y); }
            for (; k < n; k++) yield return SitePos + Random.insideUnitCircle.normalized * (4f + k * 2f);
        }

        ZLMissionProp Prop(Vector2 at, Sprite s, string text, Color c, float scale)
        {
            var go = new GameObject("MissionProp");
            go.transform.position = at;
            var p = go.AddComponent<ZLMissionProp>();
            p.Init(_g, s, text, c, scale);
            _props.Add(p);
            return p;
        }

        void Guard(Vector2 at, ZLFoe kind)
        {
            if (!_g.City.Walkable(at)) return;
            var z = _g.SpawnZombie(kind, at, Threat, false);
            z.pinned = true;
            _spawned.Add(z);
        }

        void Wave(int n)
        {
            for (int i = 0; i < n; i++)
                if (_g.SpawnSpot(SitePos, 11f, 16f, out var p))
                {
                    var z = _g.SpawnZombie(ZLRules.Pick(Threat, Random.value), p, Threat, true);
                    _spawned.Add(z);
                }
        }

        public void Update(float dt)
        {
            Vector2 at = _g.TargetPos;
            switch (Spec.type)
            {
                case ZLMissionType.Signal:
                case ZLMissionType.HoldOut:
                {
                    float r = Spec.type == ZLMissionType.Signal ? 3.5f : 6f;
                    if (_zone != null) _zone.transform.localScale = Vector3.one * r * (1f + Mathf.Sin(Time.time * 3f) * 0.03f);
                    if (Vector2.Distance(at, SitePos) < r)
                    {
                        _timer += dt;
                        _wave -= dt;
                        if (_wave <= 0f) { _wave = Spec.type == ZLMissionType.Signal ? 5f : 6.5f; Wave(2 + Threat / 2); }
                    }
                    if (_timer >= Spec.seconds) _g.MissionComplete(this);
                    break;
                }
                case ZLMissionType.Retrieve:
                    if (_g.PlayerDriving) break;
                    foreach (var p in _props)
                    {
                        if (p.done || Vector2.Distance(p.transform.position, at) > 1f) continue;
                        p.done = true; p.Show(false);
                        _count++;
                        Sfx.Play(Sfx.Shard, 0.8f);
                        ZLFloater.Show($"{_count}/{Spec.count}", at, new Color(1f, 0.9f, 0.5f));
                        _g.MakeNoise(at, 0.5f);
                    }
                    if (_count >= Spec.count) _g.MissionComplete(this);
                    break;
                case ZLMissionType.PowerUp:
                    if (_g.PlayerDriving) break;
                    foreach (var p in _props)
                    {
                        if (p.done) continue;
                        if (Vector2.Distance(p.transform.position, at) < 1.6f)
                        {
                            p.progress += dt / 2.5f;
                            p.SetLabel($"STARTING {Mathf.RoundToInt(p.progress * 100f)}%");
                            if (p.progress >= 1f)
                            {
                                p.done = true;
                                p.SetSprite(_g.Sprite("Props/pk_generator_on"));
                                p.SetLabel("ON");
                                _count++;
                                Sfx.Play(Sfx.PowerUp, 0.9f);
                                _g.MakeNoise(p.transform.position, 1f);
                                Wave(3);
                            }
                        }
                        else if (p.progress > 0f && p.progress < 1f) p.SetLabel("GENERATOR");
                    }
                    if (_count >= Spec.count) _g.MissionComplete(this);
                    break;
                case ZLMissionType.Rescue:
                    if (_survivor == null) break;
                    if (!_running && Vector2.Distance(_survivor.transform.position, at) < 1.8f)
                    {
                        _running = true;
                        _g.ShowToast(Spec.giver + " IS WITH YOU. GET TO A SAFEHOUSE.");
                        Wave(3);
                    }
                    if (_running)
                    {
                        bool riding = _g.PlayerDriving;
                        _survivor.Show(!riding);
                        Vector2 sp = _survivor.transform.position;
                        if (riding) _survivor.transform.position = at;
                        else if (Vector2.Distance(sp, at) > 1.2f)
                        {
                            var step = Vector2.MoveTowards(sp, at - (at - sp).normalized * 1f, 5.5f * dt);
                            if (Vector2.Distance(step, at) > 12f) step = at; // never lose them
                            _survivor.transform.position = step;
                            if (_survivor.transform.childCount > 0) { }
                        }
                        if (Vector2.Distance(_survivor.transform.position, NearestSafehouse(_survivor.transform.position)) < 3.2f)
                            _g.MissionComplete(this);
                    }
                    break;
                case ZLMissionType.PurgeNest:
                    // completion comes from NestDestroyed
                    break;
                case ZLMissionType.HuntHerald:
                    if (_herald == null || !_herald.Alive) _g.MissionComplete(this);
                    break;
                case ZLMissionType.Delivery:
                    if (_vehicle == null || _vehicle.wrecked) { _g.MissionFailed(this, "THE VEHICLE IS WRECKED."); break; }
                    if (_running)
                    {
                        _clock += dt;
                        if (Site.dest != null && Vector2.Distance(_vehicle.transform.position, new Vector2(Site.dest.x, Site.dest.y)) < 4f)
                            _g.MissionComplete(this);
                        else if (_clock >= Spec.seconds) _g.MissionFailed(this, "OUT OF TIME.");
                    }
                    break;
            }
        }

        Vector2 NearestSafehouse(Vector2 from)
        {
            Vector2 best = from;
            float bd = float.MaxValue;
            foreach (var s in _g.Safehouses)
            {
                float d = Vector2.Distance(s.transform.position, from);
                if (d < bd) { bd = d; best = s.transform.position; }
            }
            return best;
        }

        public IZLUse NearestUse(Vector2 at) => null;

        public void NestDestroyed(ZLNest n)
        {
            if (n == _nest) _g.MissionComplete(this);
        }

        public void ZombieDied(ZLZombie z) { _spawned.Remove(z); }

        public void VehicleEntered(ZLVehicle v)
        {
            if (Spec.type == ZLMissionType.Delivery && v == _vehicle && !_running)
            {
                _running = true;
                _clock = 0f;
                _g.ShowToast("GO! " + Mathf.RoundToInt(Spec.seconds) + " SECONDS.");
                Sfx.Play(Sfx.RoundStart, 0.9f);
            }
        }

        public void VehicleExited(ZLVehicle v) { }
        public void VehicleWrecked(ZLVehicle v) { }

        public void Cleanup()
        {
            foreach (var p in _props) if (p != null) Object.Destroy(p.gameObject);
            _props.Clear();
            if (_vehicle != null && !(_g.Active == null && Spec.type == ZLMissionType.Delivery && _g.Progress.Done(Spec.id))) _g.RemoveVehicle(_vehicle);
            else if (_vehicle != null) _vehicle.missionTag = null; // keep the delivered vehicle
            foreach (var z in _spawned) if (z != null && z.Alive) z.pinned = false;
            if (_nest != null && _nest.Alive) Object.Destroy(_nest.gameObject);
        }
    }
}
