using System.Collections.Generic;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// The party quests played in the Skyforge Arena on top of the normal brawl (ArenaMatchController):
    ///   BALLOON BATTLE  three balloons each (the stocks); a launch, three hits or an item pops one; last one floating wins
    ///   RING-OUT BRAWL  three lives with item boxes; after a minute the ring (blast zone) closes in
    ///   COIN RUSH       coins keep appearing; most coins in 90 s wins; a hit drops 2, a fall drops 5
    ///   KING OF THE HILL score a point a second standing alone in the crown zone; it moves every 15 s
    /// Item boxes appear in every party arena. CPU fighters go for coins, the zone and item boxes, not only for you.
    /// </summary>
    public class PartyArena : MonoBehaviour, IPartyHost
    {
        ArenaMatchController _arena;
        PartyMode _mode;
        float _elapsed;
        Rect _blastStart;
        readonly List<ItemBox> _boxes = new List<ItemBox>();
        readonly List<PartyCoin> _coins = new List<PartyCoin>();
        readonly Dictionary<int, int> _hitsTaken = new Dictionary<int, int>();
        readonly List<System.Action> _unsubscribe = new List<System.Action>();
        float _nextCoin, _nextScore;
        Transform _zone;
        SpriteRenderer _zoneArt, _crown;
        int _zoneSpot = -1;
        Vector2[] _spots;
        bool _shrinkAnnounced;

        public bool Running { get; private set; }
        public PartyMode Mode => _mode;
        public const float ShrinkAfter = 60f, ShrinkOver = 30f, ShrinkTo = 0.72f;

        public void Begin(ArenaMatchController arena)
        {
            Clear();
            _arena = arena;
            _mode = arena.Config.party;
            _elapsed = 0f;
            _blastStart = arena.BlastZone;
            _shrinkAnnounced = false;
            Running = true;
            var stage = arena.StageRect;
            float half = stage.width * 0.5f, floor = stage.yMax;
            // item boxes over the main stage
            foreach (float k in new[] { -0.62f, 0f, 0.62f })
                _boxes.Add(ItemBox.Spawn(this, new Vector2(stage.center.x + half * k, floor + 1.1f), (int)(k * 100f) + 77));
            foreach (var f in arena.Fighters)
            {
                var fighter = f;
                ItemBuffs.On(f.go);
                if (f.health != null)
                {
                    System.Action<HitResult> onHit = r => OnHit(fighter, r);
                    f.health.Hit += onHit;
                    _unsubscribe.Add(() => { if (fighter.health != null) fighter.health.Hit -= onHit; });
                }
                if (_mode == PartyMode.BalloonBattle) BalloonCluster.Attach(f.go, f.slot, f.color, arena, arena.SpriteMaterial);
            }
            if (_mode == PartyMode.KingOfTheHill)
            {
                _spots = new[]
                {
                    new Vector2(stage.center.x - half * 0.7f, floor), new Vector2(stage.center.x - half * 0.22f, floor),
                    new Vector2(stage.center.x + half * 0.22f, floor), new Vector2(stage.center.x + half * 0.7f, floor),
                };
                BuildZone();
            }
            var info = PartyModes.Info(_mode);
            PartyHUD.Show(info.name, Line);
            LivePresence.Enter(info.playerGroup);
        }

        public void Clear()
        {
            Running = false;
            foreach (var u in _unsubscribe) u();
            _unsubscribe.Clear();
            foreach (var b in _boxes) if (b != null) Destroy(b.gameObject);
            _boxes.Clear();
            foreach (var c in _coins) if (c != null) Destroy(c.gameObject);
            _coins.Clear();
            _hitsTaken.Clear();
            if (_zone != null) Destroy(_zone.gameObject);
            _zone = null;
            _zoneSpot = -1;
            if (_arena != null && _blastStart.width > 0f) _arena.BlastZone = _blastStart;
            PartyHUD.Hide();
        }

        string Line()
        {
            if (_arena == null || _arena.Match == null) return "";
            var m = _arena.Match;
            switch (_mode)
            {
                case PartyMode.CoinRush:
                    return $"{Clock(m.TimeRemaining)}   ·   YOUR COINS {m.Get(0).score}";
                case PartyMode.KingOfTheHill:
                    float next = CrownZone.MoveEvery - Mathf.Repeat(_elapsed, CrownZone.MoveEvery);
                    return $"{Clock(m.TimeRemaining)}   ·   ZONE MOVES IN {Mathf.CeilToInt(next)}   ·   YOU {m.Get(0).score}";
                case PartyMode.RingOut:
                    if (_elapsed < ShrinkAfter) return $"THE RING CLOSES IN {Mathf.CeilToInt(ShrinkAfter - _elapsed)}";
                    return "THE RING IS CLOSING!";
                default:
                    return $"YOUR BALLOONS {m.Get(0).stocks}   ·   HEROES FLOATING {CountAlive()}";
            }
        }

        static string Clock(float s) => $"{(int)s / 60}:{(int)s % 60:00}";

        int CountAlive()
        {
            int n = 0;
            foreach (var f in _arena.Fighters) if (!_arena.Match.Get(f.slot).IsEliminated) n++;
            return n;
        }

        void Update()
        {
            if (!Running || _arena == null || !_arena.IsLive) return;
            _elapsed += Time.deltaTime;
            switch (_mode)
            {
                case PartyMode.CoinRush: TickCoins(); break;
                case PartyMode.KingOfTheHill: TickZone(); break;
                case PartyMode.RingOut: TickShrink(); break;
            }
        }

        // ---- balloons / hits ------------------------------------------------------------------------------------

        void OnHit(ArenaMatchController.Fighter f, HitResult r)
        {
            if (!Running || r.blocked) return;
            int attacker = f.health.LastAttackerTeam;
            if (_mode == PartyMode.BalloonBattle)
            {
                _hitsTaken.TryGetValue(f.slot, out int n);
                n++;
                if (r.isLaunch || n >= 3)
                {
                    n = 0;
                    _arena.Pop(f.slot, attacker);
                }
                _hitsTaken[f.slot] = n;
            }
            else if (_mode == PartyMode.CoinRush) DropCoins(f, false);
        }

        public void OnFall(int slot)
        {
            if (!Running || _mode != PartyMode.CoinRush) return;
            foreach (var f in _arena.Fighters) if (f.slot == slot) DropCoins(f, true);
        }

        // ---- coins ----------------------------------------------------------------------------------------------

        void TickCoins()
        {
            _coins.RemoveAll(c => c == null);
            if (Time.time < _nextCoin || _coins.Count >= 14) return;
            _nextCoin = Time.time + 0.8f;
            var stage = _arena.StageRect;
            var at = new Vector2(Random.Range(stage.xMin + 0.8f, stage.xMax - 0.8f), stage.yMax + Random.Range(0.6f, 4f));
            _coins.Add(PartyCoin.Spawn(this, at, 0f));
        }

        void DropCoins(ArenaMatchController.Fighter f, bool fell)
        {
            if (_arena.Match == null) return;
            int drop = CoinRules.Drop(_arena.Match.Get(f.slot).score, fell);
            if (drop <= 0) return;
            _arena.Match.AddScore(f.slot, -drop);
            var stage = _arena.StageRect;
            Vector2 from = f.go != null && !fell ? (Vector2)f.go.transform.position : new Vector2(stage.center.x, stage.yMax + 3f);
            for (int i = 0; i < drop; i++)
            {
                var at = new Vector2(Mathf.Clamp(from.x + Random.Range(-2.5f, 2.5f), stage.xMin + 0.6f, stage.xMax - 0.6f),
                                     Mathf.Max(stage.yMax + 0.6f, from.y + Random.Range(0.2f, 1.5f)));
                _coins.Add(PartyCoin.Spawn(this, at, 0.6f));
            }
        }

        /// <summary>A fighter touched a coin.</summary>
        public bool Collect(GameObject who)
        {
            foreach (var f in _arena.Fighters)
            {
                if (f.go != who) continue;
                _arena.Match.AddScore(f.slot, 1);
                if (f.human) Sfx.Play(Sfx.Shard, 0.6f);
                return true;
            }
            return false;
        }

        // ---- crown zone -----------------------------------------------------------------------------------------

        void BuildZone()
        {
            var go = new GameObject("CrownZone");
            _zone = go.transform;
            _zoneArt = new GameObject("Area").AddComponent<SpriteRenderer>();
            _zoneArt.transform.SetParent(_zone, false);
            _zoneArt.sprite = PlazaArt.Solid();
            _zoneArt.sortingOrder = -5;
            _zoneArt.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            _zoneArt.transform.localScale = new Vector3(3f * PlazaArt.PPU / 4f, 2.5f * PlazaArt.PPU / 4f, 1f);
            _crown = new GameObject("Crown").AddComponent<SpriteRenderer>();
            _crown.transform.SetParent(_zone, false);
            _crown.sprite = PlazaArt.Crown();
            _crown.sortingOrder = 20;
            _crown.transform.localPosition = new Vector3(0f, 2.7f, 0f);
            _crown.transform.localScale = Vector3.one * 1.6f;
            if (_arena.SpriteMaterial != null) { _zoneArt.sharedMaterial = _arena.SpriteMaterial; _crown.sharedMaterial = _arena.SpriteMaterial; }
        }

        void TickZone()
        {
            int spot = CrownZone.SpotAt(_elapsed, _spots.Length, 11);
            if (spot != _zoneSpot)
            {
                _zoneSpot = spot;
                _zone.position = _spots[spot];
                PuffEffect.Play(_spots[spot] + Vector2.up, new Color32(255, 214, 90, 255), _arena.SpriteMaterial, 0.3f, 5f, 0.5f);
            }
            var inZone = new bool[_arena.Fighters.Count];
            for (int i = 0; i < inZone.Length; i++) inZone[i] = InZone(_arena.Fighters[i]);
            int scorer = CrownZone.Scorer(inZone);
            Color c = scorer >= 0 ? _arena.Fighters[scorer].color : Color.white;
            _zoneArt.color = new Color(c.r, c.g, c.b, scorer >= 0 ? 0.32f : 0.16f);
            if (Time.time >= _nextScore)
            {
                _nextScore = Time.time + 1f;
                if (scorer >= 0) _arena.Match.AddScore(_arena.Fighters[scorer].slot, 1);
            }
        }

        bool InZone(ArenaMatchController.Fighter f)
        {
            if (f.go == null || !f.go.activeSelf || _arena.IsRespawning(f.slot)) return false;
            Vector2 p = f.go.transform.position;
            Vector2 z = _zone.position;
            return Mathf.Abs(p.x - z.x) <= 1.5f && p.y >= z.y - 0.6f && p.y <= z.y + 2.8f;
        }

        // ---- the closing ring -----------------------------------------------------------------------------------

        void TickShrink()
        {
            if (_elapsed < ShrinkAfter) return;
            if (!_shrinkAnnounced)
            {
                _shrinkAnnounced = true;
                PartyHUD.Flash("THE RING IS CLOSING!");
            }
            float k = Mathf.Lerp(1f, ShrinkTo, Mathf.Clamp01((_elapsed - ShrinkAfter) / ShrinkOver));
            var c = _blastStart.center;
            var size = new Vector2(_blastStart.width * k, _blastStart.height * Mathf.Lerp(1f, 0.85f, 1f - k));
            _arena.BlastZone = new Rect(c - size * 0.5f, size);
        }

        // ---- what CPU fighters go for ---------------------------------------------------------------------------

        public GameObject ObjectiveFor(GameObject self, Vector2 from)
        {
            GameObject best = null;
            float bestD = float.MaxValue;
            if (_mode == PartyMode.CoinRush)
                foreach (var c in _coins)
                {
                    if (c == null || !c.Collectable) continue;
                    float d = ((Vector2)c.transform.position - from).sqrMagnitude;
                    if (d < bestD) { bestD = d; best = c.gameObject; }
                }
            if (_mode == PartyMode.KingOfTheHill && _zone != null)
            {
                foreach (var f in _arena.Fighters)
                    if (f.go == self && InZone(f)) return null; // in the zone: fight whoever comes near
                return _zone.gameObject;
            }
            if (best != null) return best;
            foreach (var b in _boxes)
            {
                if (b == null || !b.Ready) continue;
                float d = ((Vector2)b.transform.position - from).sqrMagnitude;
                if (d < 25f && d < bestD) { bestD = d; best = b.gameObject; }
            }
            return best;
        }

        // ---- IPartyHost -----------------------------------------------------------------------------------------

        public bool IsRacer(GameObject go)
        {
            if (_arena == null) return false;
            foreach (var f in _arena.Fighters) if (f.go == go && go.activeSelf && !_arena.IsRespawning(f.slot)) return true;
            return false;
        }

        public int PlaceOf(GameObject racer)
        {
            if (_arena == null || _arena.Match == null) return 1;
            var placements = _arena.Match.Placements();
            foreach (var f in _arena.Fighters) if (f.go == racer) return placements[f.slot];
            return 1;
        }

        public int RacerCount => _arena != null ? _arena.Fighters.Count : 1;

        public IEnumerable<GameObject> Rivals(GameObject racer)
        {
            foreach (var f in _arena.Fighters)
                if (f.go != racer && f.go != null && f.go.activeSelf && !_arena.IsRespawning(f.slot)) yield return f.go;
        }

        public Material SpriteMaterial => _arena != null ? _arena.SpriteMaterial : null;

        public void OnItemHit(GameObject victim, GameObject attacker)
        {
            int v = -1, a = -1;
            foreach (var f in _arena.Fighters)
            {
                if (f.go == victim) v = f.slot;
                if (f.go == attacker) a = f.slot;
            }
            if (v < 0) return;
            if (_mode == PartyMode.BalloonBattle) _arena.Pop(v, a);
            else if (_mode == PartyMode.CoinRush) DropCoins(_arena.Fighters[v], false);
        }

        void OnDestroy() => Clear();
    }
}
