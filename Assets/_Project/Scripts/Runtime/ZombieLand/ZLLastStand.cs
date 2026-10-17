using System.Collections;
using System.Collections.Generic;
using PKR.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// LAST STAND: HOLLOW BLOCK. Round-based survival on the Zombie Land engine (ZLGame in LastStand mode builds the
    /// block, the hero and this). Each round sends more of the dead over the fences of every open part of the block;
    /// every 5th (15th, 25th...) is a hound round, every 10th brings THE BUTCHER. Points from hits and kills buy wall
    /// guns, debris clearing (more of the block), perks, the mystery box (which moves) and the forge. Three graystone
    /// fragments hidden on the block summon THE HOLLOW KING. Endless / Raid / Hardcore come from the ZOMBIES menu.
    ///
    /// CO-OP: up to four heroes — three AI teammates, or players online through Game Center (with AI in empty seats).
    /// Each hero has their own points, guns and perks; a hero who falls is downed and can be revived; the run ends
    /// when nobody is standing. Online, the host runs everything here and guests mirror it (ApplySnapshot).
    /// </summary>
    public class ZLLastStand : MonoBehaviour
    {
        public LastStandRun Ls { get; private set; }
        /// <summary>The round, and the local hero's points and kills.</summary>
        public SurvivalRun Run => Ls.Run;
        public SurvivalMode Mode => Run.Mode;
        public bool Intermission { get; private set; } = true;
        public float IntermissionLeft { get; private set; }
        public bool HoundRound { get; private set; }
        public bool Over { get; private set; }
        public bool Won => Run.Won;
        public int ZombiesLeft => _guest ? _guestLeft : _toSpawn + _alive.Count + (_bossPending ? 1 : 0) + (_kingPending ? 1 : 0);
        public IReadOnlyList<ZLZombie> Alive => _alive;
        public ZLMysteryBox Box { get; private set; }
        public int ShardReward { get; private set; }
        public int XpEarned { get; private set; }
        public bool NewBest { get; private set; }
        public List<int> RanksReached { get; private set; } = new List<int>();
        public ZLZombie Butcher { get; private set; }
        public ZLZombie HollowKing { get; private set; }
        public int SquadSize => _g != null ? _g.Squad.Count : 1;
        public SquadKind Kind { get; private set; } = SquadKind.Solo;
        /// <summary>The map (Hollow Block, Salt Pier...).</summary>
        public LastStandMap Map => _g != null ? _g.LsMap : LastStandMap.HollowBlock;
        /// <summary>The weekly challenge being played (null for a normal run).</summary>
        public WeeklyChallenge Weekly { get; private set; }
        /// <summary>The rule changes for this run (the weekly challenge's mutators, or none).</summary>
        public MutatorSet Mut { get; private set; } = MutatorSet.None;
        public bool WeeklyPaid { get; private set; }
        public int BoxCost => Mut.BoxCost(LastStandRules.BoxCost);
        /// <summary>Assist mode (Settings) and the rookie warm-up (a player's first Last Stand runs).</summary>
        public bool Assist => Services.Settings != null && Services.Settings.Data.assistMode;
        public bool RookieRound => ZombieAssist.Rookie(_lsRuns, Run.Round);
        int _lsRuns;
        public int DoorCost(ZLCityData.Door d) => Mut.DoorCost(d.cost);

        ZLGame _g;
        readonly List<ZLZombie> _alive = new List<ZLZombie>();
        readonly HashSet<string> _openZones = new HashSet<string>();
        readonly List<ZLDebrisDoor> _doors = new List<ZLDebrisDoor>();
        readonly Dictionary<ZLPlayer, int> _revives = new Dictionary<ZLPlayer, int>();
        int _toSpawn, _nextNetId, _guestLeft;
        bool _bossPending, _kingPending, _guest;
        float _spawnT;
        Vector2 _lastKillAt;
        RectTransform _overRoot;

        /// <summary>Each seat's colour (ring, name, scoreboard), from the player's colour-vision palette.</summary>
        static Color SlotColor(int slot) => SettingsService.RoleColor(Accessibility.SquadRole(slot));
        static readonly string[] AINames = { "", "DOC", "MAGS", "TANK" };

        public void Init(ZLGame g)
        {
            _g = g;
            int xp = g.CareerXp;
            var careerNow = Services.Save != null ? Services.Save.Data.zombieCareer : null;
            _lsRuns = careerNow != null ? careerNow.lastStandRuns : 0;
            if (GameSession.Mode == SessionMode.Zombies && GameSession.ZombieWeekly && GameSession.ZombieSquad != SquadKind.Online)
            {
                Weekly = ZombieWeekly.Current(System.DateTime.UtcNow);
                Mut = Weekly.Set;
                GameSession.ZombieMode = Weekly.Mode;
            }
            Ls = new LastStandRun(GameSession.Mode == SessionMode.Zombies ? GameSession.ZombieMode : SurvivalMode.Endless, SurvivorRank.BonusPoints(xp))
            {
                FreeSpinLeft = SurvivorRank.Has(xp, "kit_spin")
            };
            g.Player.Wallet = Run;
            g.Player.DisplayName = "YOU";
            var city = g.City;
            if (city.zones.Count > 0) _openZones.Add(city.zones[0].id);
            foreach (var d in city.doors)
            {
                var go = new GameObject("Debris_" + d.id);
                go.transform.position = new Vector2(d.x + d.w * 0.5f, d.y + d.h * 0.5f);
                var door = go.AddComponent<ZLDebrisDoor>();
                door.Init(g, this, d);
                _doors.Add(door);
                g.RegisterUse(door);
            }
            foreach (var s in city.stations)
            {
                var go = new GameObject("Station_" + s.id);
                go.transform.position = new Vector2(s.x, s.y);
                var st = go.AddComponent<ZLStation>();
                st.Init(g, this, s);
                g.RegisterUse(st);
            }
            if (city.boxSpots.Count > 0)
            {
                var go = new GameObject("MysteryBox");
                Box = go.AddComponent<ZLMysteryBox>();
                Box.Init(g, this, 0);
                g.RegisterUse(Box);
            }
            for (int i = 0; i < city.eggs.Count; i++)
            {
                var e = city.eggs[i];
                var go = new GameObject("Fragment_" + i);
                go.transform.position = new Vector2(e.x, e.y);
                var f = go.AddComponent<ZLFragment>();
                f.Init(g, this, Map);
                g.RegisterUse(f);
            }
            IntermissionLeft = 4f;

            // The squad
            var kind = GameSession.Mode == SessionMode.Zombies ? GameSession.ZombieSquad : SquadKind.Solo;
            var match = PKRMatchService.Instance;
            if (kind == SquadKind.Online && match != null && match.Transport != null && match.State == PKRMatchService.Phase.InGame)
                SetupOnline(match.Transport, match.IsHost, match.MySlot, match.Slots);
            else if (kind == SquadKind.AISquad) SetupAI(3);
            string squad = Kind == SquadKind.AISquad ? "  ·  AI SQUAD" : Kind == SquadKind.Online ? "  ·  ONLINE CO-OP" : "";
            ApplyHeroMutators();
            if (Weekly != null) g.ShowToast($"WEEKLY  ·  {Map.Title}  ·  {Mut.Names()}  ·  REACH ROUND {Weekly.GoalRound}{squad}", 5f);
            else g.ShowToast($"{Map.FullTitle}  ·  {SurvivalModes.Name(Mode)}{squad}", 4f);
        }

        // ---- the squad -----------------------------------------------------------------------------------

        Vector2 StartSpot(int slot)
        {
            Vector2 s = new Vector2(_g.City.start.x, _g.City.start.y);
            Vector2[] offs = { Vector2.zero, new Vector2(-1.5f, 0.6f), new Vector2(1.5f, 0.6f), new Vector2(0f, -1.4f) };
            var p = s + offs[Mathf.Clamp(slot, 0, 3)];
            return _g.City.Walkable(p) ? p : s;
        }

        ZombieLandProgress NewLoadout()
        {
            var lo = new ZombieLandProgress();
            ZLRules.NewGame(lo, 1);
            var pistol = lo.Gun(ZLCatalog.Pistol.id);
            pistol.reserve = LastStandRules.PistolRounds - pistol.mag;
            lo.medkits = 0;
            return lo;
        }

        public ZLPlayer AddHero(int slot, string name, string heroId, ZLControl control)
        {
            var h = _g.SpawnSquadHero(heroId, control, StartSpot(slot), slot, name, SlotColor(slot));
            h.Loadout = NewLoadout();
            h.Wallet = new SurvivalRun(Mode, 0);
            h.MaxHealthScale = Mut.HeroHealth;
            h.SetHealth(h.MaxHealth);
            if (control == ZLControl.AI) h.Brain = new ZLAIBrain(_g, slot);
            return h;
        }

        /// <summary>Three AI teammates (DOC, MAGS and TANK) join you.</summary>
        public void SetupAI(int count)
        {
            Kind = SquadKind.AISquad;
            _g.Player.Slot = 0;
            _g.Player.ShowSquadMarker(SlotColor(0), "YOU");
            string mine = GameSession.SelectedCharacterId;
            int added = 0;
            if (_g.roster != null)
                foreach (var def in _g.roster.heroes)
                {
                    if (added >= count) break;
                    if (def == null || def.id == mine) continue;
                    added++;
                    AddHero(added, AINames[added], def.id, ZLControl.AI);
                }
            for (; added < count; added++) AddHero(added + 1, AINames[added + 1], "", ZLControl.AI);
        }

        /// <summary>Online: the host simulates (remote players + AI in empty seats); a guest sees puppets.</summary>
        public void SetupOnline(IZLTransport transport, bool isHost, int mySlot, List<NetSlot> slots)
        {
            Kind = SquadKind.Online;
            _guest = !isHost;
            var link = _g.gameObject.AddComponent<ZLNetLink>();
            link.Init(_g, transport, isHost, mySlot, slots);
            _g.Net = link;
            _g.Player.Slot = mySlot;
            _g.Player.transform.position = StartSpot(mySlot);
            _g.Player.ShowSquadMarker(SlotColor(mySlot), "YOU");
            foreach (var s in slots)
            {
                if (s.slot == mySlot) continue;
                string name = s.ai ? AINames[Mathf.Clamp(s.slot, 1, 3)] : s.name;
                if (!isHost) { AddHero(s.slot, name, s.heroId, ZLControl.Puppet); continue; }
                if (s.ai) { AddHero(s.slot, name, s.heroId, ZLControl.AI); continue; }
                var h = AddHero(s.slot, name, s.heroId, ZLControl.Remote);
                var brain = new ZLRemoteBrain();
                h.Brain = brain;
                link.RegisterRemote(s.slot, brain);
            }
            if (_guest) IntermissionLeft = 99f;
        }

        public ZLPlayer HeroInSlot(int slot)
        {
            foreach (var h in _g.Squad) if (h != null && h.Slot == slot) return h;
            return null;
        }

        void ApplyHeroMutators()
        {
            foreach (var h in _g.Squad)
            {
                if (h == null) continue;
                h.MaxHealthScale = Mut.HeroHealth;
                h.SetHealth(h.MaxHealth);
            }
        }

        public void MakeAI(ZLPlayer h)
        {
            h.BecomeAI(new ZLAIBrain(_g, h.Slot));
            h.DisplayName = "AI " + h.DisplayName;
        }

        public void CountRevive(ZLPlayer by)
        {
            _revives.TryGetValue(by, out int n);
            _revives[by] = n + 1;
            if (by == _g.Player) Tell(ZombieDailyTracker.Revived(Map.Id));
        }

        /// <summary>Daily orders, achievements and LOCKER unlocks report back as toasts.</summary>
        void Tell(List<string> lines)
        {
            if (lines == null || lines.Count == 0) return;
            if (Over) { _endLines.AddRange(lines); return; }
            _g.ShowToast(string.Join("\n", lines), 4.5f);
            Sfx.Play(Sfx.Purchase, 0.8f, 0f);
        }

        readonly List<string> _endLines = new List<string>();

        public int RevivesBy(ZLPlayer h) => _revives.TryGetValue(h, out int n) ? n : 0;

        SurvivalRun Wallet(ZLPlayer p) => p != null && p.Wallet != null ? p.Wallet : Run;

        int Earn(ZLPlayer p, int amount) => Wallet(p).Earn(_g.DoubleScrap ? amount * 2 : amount);

        // ---- rounds -----------------------------------------------------------------------------------------

        public void Tick(float dt)
        {
            if (Over || _guest) return;
            ResupplyAI(dt);
            if (Intermission)
            {
                IntermissionLeft -= dt;
                if (IntermissionLeft <= 0f) StartRound();
                return;
            }
            SpawnTick(dt);
            BossPhases();
            _alive.RemoveAll(z => z == null || !z.Alive);
            if (_toSpawn <= 0 && !_bossPending && !_kingPending && _alive.Count == 0) EndRound();
        }

        void StartRound()
        {
            int round = Run.NextRound();
            foreach (var h in _g.Squad) if (h != null && h.Wallet != null && h.Wallet != Run) h.Wallet.NextRound();
            Tell(ZombieDailyTracker.Report(DailyKind.ReachRound, round, Map.Id));
            Intermission = false;
            HoundRound = Mut.IsHoundRound(Mode, round);
            int n = SquadSize;
            _toSpawn = HoundRound ? SquadRules.HoundsInRound(round, n) : SquadRules.ZombiesInRound(round, n);
            _bossPending = LastStandRules.IsBossRound(Mode, round);
            if (Ls.HiddenBossSummoned && !Ls.HiddenBossBeaten && HollowKing == null) _kingPending = true;
            _spawnT = 1.5f;
            // Bled-out heroes come back; AI teammates get their kit for the round.
            Vector2 near = _g.TargetPos;
            foreach (var h in _g.Squad) if (h != null && h.Standing) { near = h.transform.position; break; }
            foreach (var h in _g.Squad)
            {
                if (h == null) continue;
                if (h.Out)
                {
                    h.Revive(_g.City.Walkable(near + Vector2.right) ? near + Vector2.right : near);
                    var pistol = h.Loadout.Gun(ZLCatalog.Pistol.id);
                    if (pistol != null && pistol.mag + pistol.reserve < ZLCatalog.Pistol.magazine * 3) { pistol.mag = ZLCatalog.Pistol.magazine; pistol.reserve = ZLCatalog.Pistol.magazine * 2; }
                }
                if (h.Control == ZLControl.AI) KitAI(h, round);
            }
            Sfx.Play(Sfx.RoundStart, 0.9f, 0f);
            string head = Mode == SurvivalMode.Raid ? $"ROUND {round}/{SurvivalModes.RaidRounds}" : $"ROUND {round}";
            if (HoundRound) head += "  ·  THE HOUNDS ARE COMING";
            if (_bossPending) head += "  ·  " + Map.BossName + "!";
            if (_kingPending) head += "  ·  " + Map.HiddenBossName + " WAKES";
            _g.Announce(head, 3.5f);
            if (HoundRound && Services.Haptics != null) Services.Haptics.Play(HapticStrength.Heavy);
        }

        readonly Dictionary<ZLZombie, int> _bossPhase = new Dictionary<ZLZombie, int>();
        readonly Dictionary<ZLZombie, float> _bossBaseSpeed = new Dictionary<ZLZombie, float>();

        /// <summary>Bosses change as they're hurt: enraged at half health (faster, calls runners), the horde at a quarter.</summary>
        void BossPhases()
        {
            CheckBoss(Butcher, false);
            CheckBoss(HollowKing, true);
        }

        void CheckBoss(ZLZombie b, bool hidden)
        {
            if (b == null || !b.Alive) return;
            if (!_bossBaseSpeed.ContainsKey(b)) _bossBaseSpeed[b] = b.speed;
            _bossPhase.TryGetValue(b, out int was);
            if (was == 0) was = 1;
            int now = LastStandRules.BossPhase(b.hp / (float)Mathf.Max(1, b.maxHp));
            if (now <= was) return;
            _bossPhase[b] = now;
            b.speed = _bossBaseSpeed[b] * LastStandRules.BossPhaseSpeed(now);
            string name = hidden ? Map.HiddenBossName : Map.BossName;
            _g.Announce(now == 2 ? name + " IS ENRAGED!" : name + " CALLS THE HORDE!", 2.5f);
            if (CameraFollowTopDown.Main != null) CameraFollowTopDown.Main.Shake(0.25f, 0.4f);
            if (Services.Haptics != null) Services.Haptics.Play(HapticStrength.Heavy);
            var (kind, count) = LastStandRules.BossSummons(now, hidden);
            for (int i = 0; i < count; i++)
            {
                var z = Spawn(kind);
                if (z == null) continue;
                Vector2 at = (Vector2)b.transform.position + Random.insideUnitCircle.normalized * 2.5f;
                if (!_g.City.Walkable(at)) continue;   // stays at its spawn point and walks in
                z.transform.position = at;
                if (z.Body != null) z.Body.position = at;
            }
        }

        public int BossPhaseOf(ZLZombie b) => b != null && _bossPhase.TryGetValue(b, out int p) ? p : 1;

        readonly Dictionary<ZLPlayer, float> _aiDry = new Dictionary<ZLPlayer, float>();

        /// <summary>
        /// AI teammates don't buy ammo, so one that has run dry scrounges a few magazines after a short wait (the
        /// balance lab found whole AI squads standing empty-handed in long rounds while the round never ended).
        /// </summary>
        void ResupplyAI(float dt)
        {
            foreach (var h in _g.Squad)
            {
                if (h == null || h.Control != ZLControl.AI || h.Loadout == null) continue;
                var ammo = h.Ammo;
                if (ammo == null || ammo.mag + ammo.reserve > 0) { _aiDry[h] = 0f; continue; }
                _aiDry.TryGetValue(h, out float t);
                t += dt;
                if (t >= LastStandRules.AIResupplySeconds)
                {
                    var gun = h.Gun;
                    ammo.reserve = (gun != null ? gun.magazine : 12) * 3;
                    if (h == _g.Player || Kind == SquadKind.AISquad && Random.value < 0.3f) ZLFloater.Show("+AMMO", h.transform.position, new Color(1f, 0.9f, 0.5f), 0.7f);
                    t = 0f;
                }
                _aiDry[h] = t;
            }
        }

        void KitAI(ZLPlayer h, int round)
        {
            var gun = ZLCatalog.Gun(SquadRules.AIGunFor(round));
            var lo = h.Loadout;
            if (gun != null && lo.Gun(gun.id) == null)
            {
                ZLRules.GiveGun(lo, gun, gun.magazine + gun.maxReserve);
                lo.equipped = gun.id;
            }
            ZLShop.MaxAmmo(lo);
            h.CancelReload();
        }

        void EndRound()
        {
            int bonus = Earn(_g.Player, ZombieRules.RoundBonus(Run.Round));
            foreach (var h in _g.Squad) if (h != null && h != _g.Player && h.Wallet != null) Earn(h, ZombieRules.RoundBonus(Run.Round));
            if (HoundRound) _g.SpawnPowerUp(ZLPowerUp.MaxAmmo, _lastKillAt);   // the classic reward for a hound round
            if (SurvivalModes.IsFinalRound(Mode, Run.Round))
            {
                Run.Won = true;
                GameOver();
                return;
            }
            if (CheckWeekly(Run.Round)) _g.Announce($"WEEKLY CHALLENGE COMPLETE!  +{Weekly.RewardShards} STAR SHARDS  ·  +{Weekly.RewardXp} XP", 4f);
            else _g.Announce($"ROUND {Run.Round} SURVIVED!  +{bonus}", 2.5f);
            if (SurvivalModes.CacheAfter(Run.Round))
                _g.SpawnPowerUp(Run.Round % 2 == 0 ? ZLPowerUp.MaxAmmo : ZLShop.PickPowerUp(Random.value), _g.TargetPos + Vector2.up * 1.5f);
            Intermission = true;
            IntermissionLeft = SurvivalModes.Intermission(Mode);
            HoundRound = false;
        }

        /// <summary>Weekly: records the round; pays the reward the first time the goal is reached this week.</summary>
        bool CheckWeekly(int roundsSurvived)
        {
            if (Weekly == null || _guest) return false;
            var save = Services.Save;
            if (save == null) return false;
            var c = save.Data.zombieCareer ?? (save.Data.zombieCareer = new ZombieCareer());
            if (!ZombieWeekly.Record(c, Weekly, roundsSurvived)) return false;
            WeeklyPaid = true;
            Economy.Grant(save.Data, Weekly.RewardShards);
            save.SaveNow();
            ProgressTracker.AddZombieXp(Weekly.RewardXp);
            Sfx.Play(Sfx.Victory, 0.9f, 0f);
            return true;
        }

        void SpawnTick(float dt)
        {
            _spawnT -= dt;
            if (_spawnT > 0f) return;
            int n = SquadSize;
            if (_alive.Count >= SquadRules.MaxAlive(Run.Round, n)) { _spawnT = 0.4f; return; }
            if (_kingPending) { _kingPending = false; SpawnHollowKing(); _spawnT = 2f; return; }
            if (_bossPending && _toSpawn <= SquadRules.ZombiesInRound(Run.Round, n) - 4) { _bossPending = false; SpawnButcher(); _spawnT = 2f; return; }
            if (_toSpawn <= 0)
            {
                if (_bossPending) { _bossPending = false; SpawnButcher(); }
                return;
            }
            var kind = HoundRound ? ZLFoe.Hound : LastStandRules.Pick(Run.Round, Random.value);
            if (Spawn(kind) != null) _toSpawn--;
            _spawnT = LastStandRules.SpawnInterval(Run.Round, Mode) * (HoundRound ? 0.7f : 1f) / Mathf.Sqrt(n)
                      * ZombieAssist.SpawnScale(Assist, RookieRound);
        }

        /// <summary>A spawn in an open zone, out of every hero's view if possible.</summary>
        bool SpawnSpot(out Vector2 spot)
        {
            var cam = Camera.main;
            float hh = cam != null ? cam.orthographicSize + 1f : 8f;
            float hw = cam != null ? hh * cam.aspect : 14f;
            var open = new List<Vector2>();
            var hidden = new List<Vector2>();
            foreach (var s in _g.City.spawns)
            {
                if (!_openZones.Contains(s.zone)) continue;
                var p = new Vector2(s.x, s.y);
                open.Add(p);
                bool seen = false;
                foreach (var h in _g.Squad)
                {
                    if (h == null || !h.Standing) continue;
                    Vector2 c = h.transform.position;
                    if (Mathf.Abs(p.x - c.x) < hw && Mathf.Abs(p.y - c.y) < hh) { seen = true; break; }
                }
                if (!seen) hidden.Add(p);
            }
            var pool = hidden.Count > 0 ? hidden : open;
            if (pool.Count == 0) { spot = _g.TargetPos; return false; }
            spot = pool[Random.Range(0, pool.Count)] + Random.insideUnitCircle * 0.4f;
            return true;
        }

        ZLZombie Spawn(ZLFoe kind)
        {
            if (!SpawnSpot(out var p)) return null;
            var z = _g.SpawnZombie(kind, p, 1, true);
            z.pinned = true;
            z.NetId = ++_nextNetId & 0xffff;
            z.maxHp = z.hp = Mathf.Max(1, Mathf.RoundToInt(Mut.Health(LastStandRules.Health(kind, Run.Round, Mode)) * ZombieAssist.HealthScale(Assist, RookieRound)));
            z.speed = LastStandRules.Speed(kind, Run.Round, Mode) * Mut.ZombieSpeed * ZombieAssist.SpeedScale(Assist, RookieRound) * Random.Range(0.9f, 1.1f);
            _alive.Add(z);
            return z;
        }

        void SpawnButcher()
        {
            var z = Spawn(ZLFoe.Herald);
            if (z == null) return;
            z.maxHp = z.hp = Mut.Health(SquadRules.BossHealth(LastStandRules.BossHealth(Run.Round, Mode, false), SquadSize));
            z.speed = 2.8f * SurvivalModes.SpeedScale(Mode) * Mut.ZombieSpeed;
            Butcher = z;
            _g.Announce(Map.BossName + "!", 3f);
            if (CameraFollowTopDown.Main != null) CameraFollowTopDown.Main.Shake(0.3f, 0.5f);
            Sfx.Play(Sfx.Spotted, 1f, 0f);
        }

        void SpawnHollowKing()
        {
            var z = Spawn(ZLFoe.Herald);
            if (z == null) return;
            z.maxHp = z.hp = Mut.Health(SquadRules.BossHealth(LastStandRules.BossHealth(Run.Round, Mode, true), SquadSize));
            z.speed = 3.1f * SurvivalModes.SpeedScale(Mode) * Mut.ZombieSpeed;
            z.transform.localScale = Vector3.one * 1.5f;
            z.SetTint(new Color32(Map.HR, Map.HG, Map.HB, 255));
            HollowKing = z;
            _g.Announce(Map.HiddenBossName + "!", 3.5f);
            if (CameraFollowTopDown.Main != null) CameraFollowTopDown.Main.Shake(0.4f, 0.8f);
            Sfx.Play(Sfx.Spotted, 1f, 0f);
        }

        // ---- points ---------------------------------------------------------------------------------------

        public void ZombieHit(ZLZombie z, bool killing)
        {
            if (Over || killing || _guest) return;
            int pts = Mut.Hit(LastStandRules.HitPoints);
            if (pts > 0) Earn(z.LastHitBy != null ? z.LastHitBy : _g.Player, pts);
        }

        public void ZombieDied(ZLZombie z)
        {
            _alive.Remove(z);
            _lastKillAt = z.transform.position;
            var by = z.LastHitBy != null ? z.LastHitBy : _g.Player;
            int pts = Mut.Kill(LastStandRules.KillPoints(z.kind));
            int paid = _g.DoubleScrap ? pts * 2 : pts;
            Wallet(by).KillFor(paid);
            by.Kills++;
            if (by == _g.Player && !_guest)
            {
                ZLFloater.Show("+" + paid, z.transform.position, new Color(1f, 0.85f, 0.35f), 0.75f);
                Tell(ZombieDailyTracker.Report(DailyKind.Kills, 1, Map.Id));
            }
            if (z == Butcher)
            {
                Butcher = null;
                _g.Announce(Map.BossName + " FALLS!", 3f);
                Tell(ZombieDailyTracker.BossKilled(Map.Id));
                _g.SpawnPowerUp(ZLShop.PickPowerUp(Random.value), z.transform.position);
            }
            if (z == HollowKing)
            {
                HollowKing = null;
                var before = ZombieDailyTracker.Snapshot();
                HollowKingDefeated();
                var lines = ZombieDailyTracker.BossKilled(Map.Id);
                lines.AddRange(ZombieDailyTracker.AfterProgress(before));
                Tell(lines);
            }
        }

        public void Nuked()
        {
            foreach (var h in _g.Squad) if (h != null) Wallet(h).Earn(ZombieRules.MoonburstPoints);
        }

        // ---- the shop on the block ----------------------------------------------------------------------

        public bool Pay(int cost) => Pay(_g.Player, cost);

        public bool Pay(ZLPlayer p, int cost)
        {
            if (Wallet(p).Spend(cost)) { if (p == _g.Player) Sfx.Play(Sfx.Purchase, 0.8f, 0f); return true; }
            if (p == _g.Player)
            {
                Sfx.Play(Sfx.Click, 0.6f, 0f);
                _g.ShowToast("NOT ENOUGH POINTS", 1.5f);
            }
            return false;
        }

        public void OpenDoor(ZLDebrisDoor door) => OpenDoor(door, _g.Player);

        public void OpenDoor(ZLDebrisDoor door, ZLPlayer by)
        {
            var d = door.Data;
            if (Ls.DoorsOpen.Contains(d.id) || !Pay(by, DoorCost(d))) return;
            Ls.DoorsOpen.Add(d.id);
            Sfx.Play(Sfx.Secret, 0.8f, 0f);
            if (by == _g.Player) Tell(ZombieDailyTracker.Report(DailyKind.Doors, 1, Map.Id));
            ClearDoor(door);
            bool opened = _openZones.Add(d.opens);
            string name = d.opens;
            foreach (var z in _g.City.zones) if (z.id == d.opens) name = z.name;
            _g.Announce(opened ? name + " IS OPEN" : "THE WAY IS CLEAR", 2.5f);
        }

        void ClearDoor(ZLDebrisDoor door)
        {
            if (door == null || door.Cleared) return;
            _g.City.Open(door.Data);
            door.Clear();
            _g.UnregisterUse(door);
        }

        public bool HasZone(string zone) => string.IsNullOrEmpty(zone) || _openZones.Contains(zone);

        public int PerkLimit => Ls.PerkLimit;

        public bool BuyPerk(ZLPerk k) => BuyPerk(_g.Player, k);

        public bool BuyPerk(ZLPlayer p, ZLPerk k)
        {
            var lo = p.Loadout;
            if (ZLShop.HasPerk(lo, k)) return false;
            if (lo.perks.Count >= Ls.PerkLimit)
            {
                if (p == _g.Player) _g.ShowToast($"PERK LIMIT ({Ls.PerkLimit})" + (Ls.HiddenBossBeaten ? "" : $". {Map.HiddenBossName} GUARDS A FIFTH."), 2.5f);
                return false;
            }
            if (!Pay(p, LastStandRules.PerkCost(k))) return false;
            lo.perks.Add(ZLShop.PerkKey(k));
            if (k == ZLPerk.ThickSkin) p.Heal(50);
            if (p == _g.Player)
            {
                Sfx.Play(Sfx.Purchase, 0.9f, 0f);
                _g.ShowToast(ZLShop.PerkName(k) + "!  " + ZLShop.PerkInfo(k), 2.5f);
                Tell(ZombieDailyTracker.Report(DailyKind.Perks, 1, Map.Id));
            }
            return true;
        }

        public bool UseSecondChance() => ZLShop.UseSecondChance(_g.Player.Loadout);

        /// <summary>A wall gun: buy it (full ammo; holding 3 already swaps out the one in hand) or buy ammo.</summary>
        public void UseWallGun(ZLGun gun, int cost) => UseWallGun(_g.Player, gun, cost);

        public void UseWallGun(ZLPlayer p, ZLGun gun, int cost)
        {
            var lo = p.Loadout;
            var owned = lo.Gun(gun.id);
            if (owned != null)
            {
                if (!Pay(p, cost / 2)) return;
                var e = ZLShop.Effective(lo, gun);
                owned.mag = e.magazine;
                owned.reserve = e.maxReserve;
                if (p == _g.Player) { Sfx.Play(Sfx.Reload, 0.7f, 0f); _g.ShowToast(gun.name + " AMMO", 1.5f); }
                return;
            }
            if (!Pay(p, cost)) return;
            GiveGun(p, gun);
            if (p == _g.Player) _g.ShowToast(gun.name + "!", 2f);
        }

        public void GiveGun(ZLGun gun) => GiveGun(_g.Player, gun);

        public void GiveGun(ZLPlayer p, ZLGun gun)
        {
            var lo = p.Loadout;
            if (lo.Gun(gun.id) == null && lo.guns.Count >= 3) lo.guns.RemoveAll(x => x.id == lo.equipped);
            var e = ZLShop.Effective(lo, gun);
            ZLRules.GiveGun(lo, gun, e.magazine + e.maxReserve);
            lo.equipped = gun.id;
            p.CancelReload();
        }

        public void UseForge() => UseForge(_g.Player);

        public void UseForge(ZLPlayer p)
        {
            var lo = p.Loadout;
            var held = ZLCatalog.Gun(lo.equipped);
            if (!ZLShop.CanForge(lo, held)) { if (p == _g.Player) _g.ShowToast(held != null && held.isRelic ? "NOT THAT ONE" : "ALREADY FORGED", 1.5f); return; }
            int cost = p == _g.Player ? ForgeCost : LastStandRules.ForgeCost;
            if (!Pay(p, cost)) return;
            ZLShop.Forge(lo, held.id, 0);
            p.CancelReload();
            if (p == _g.Player) { Sfx.Play(Sfx.Special, 0.9f, 0f); _g.ShowToast("HOLLOW-FORGED " + held.name + "!", 2.5f); }
        }

        public int ForgeCost => LastStandRules.ForgePrice(SurvivorRank.Has(_g.CareerXp, "kit_forge"));

        /// <summary>Pays for a box spin (the local hero's Lucky First Spin is free).</summary>
        public bool PayForBox(ZLPlayer p)
        {
            if (p == _g.Player)
            {
                bool paid = Ls.PayForSpin(BoxCost) || Pay(p, BoxCost) && CountSpin();
                if (paid) Tell(ZombieDailyTracker.Report(DailyKind.BoxSpins, 1, Map.Id));
                return paid;
            }
            if (!Pay(p, BoxCost)) return false;
            Ls.CountBoxSpin();
            return true;
        }

        bool CountSpin() { Ls.CountBoxSpin(); return true; }

        // ---- the Easter egg --------------------------------------------------------------------------------

        public void FoundFragment(ZLFragment f)
        {
            bool last = Ls.FindFragment();
            Sfx.Play(Sfx.Secret, 1f, 0f);
            _g.UnregisterUse(f);
            if (!last)
            {
                _g.Announce($"{Map.EggName} {Map.EggFound}... ({Ls.FragmentsFound}/{LastStandRules.Fragments})", 3f);
                return;
            }
            Ls.HiddenBossSummoned = true;
            _g.Announce(Map.EggAll, 4f);
            if (CameraFollowTopDown.Main != null) CameraFollowTopDown.Main.Shake(0.3f, 0.8f);
        }

        void HollowKingDefeated()
        {
            Ls.HiddenBossBeaten = true;
            foreach (var h in _g.Squad) if (h != null) Wallet(h).Earn(3000);
            RecordHollowKing();
            var drop = string.IsNullOrEmpty(Map.HiddenDrop) ? null : ZLCatalog.Gun(Map.HiddenDrop);
            if (drop != null)
            {
                foreach (var h in _g.Squad) if (h != null && h.Loadout != null) GiveGun(h, drop);
                _g.ShowToast($"{Map.HiddenBossName} DROPS THE {drop.name}!", 4f);
            }
            _g.Announce($"{Map.HiddenBossName} IS DEAD!  +{SurvivalModes.HiddenBossShards} STAR SHARDS  ·  5TH PERK SLOT", 4.5f);
            Sfx.Play(Sfx.Victory, 1f, 0f);
        }

        void RecordHollowKing()
        {
            var save = Services.Save;
            if (save != null)
            {
                var c = save.Data.zombieCareer ?? (save.Data.zombieCareer = new ZombieCareer());
                if (!c.Has(Map.HiddenSecret)) c.secrets.Add(Map.HiddenSecret);
                Economy.Grant(save.Data, SurvivalModes.HiddenBossShards);
                save.SaveNow();
            }
            ProgressTracker.AddZombieXp(SurvivorRank.HiddenBossXp);
        }

        // ---- online: snapshots -------------------------------------------------------------------------

        /// <summary>Host: the state of the block for the guests.</summary>
        public NetSnapshot BuildSnapshot(int tick)
        {
            var s = new NetSnapshot
            {
                tick = tick, round = Run.Round, zombiesLeft = ZombiesLeft, intermissionLeft = Mathf.CeilToInt(Mathf.Max(0f, IntermissionLeft)),
                intermission = Intermission, houndRound = HoundRound, over = Over, won = Run.Won,
                instaKill = _g.InstaKillLeft > 0f, doublePoints = _g.DoubleScrap,
                boxSpot = Box != null ? Box.Spot : 0, boxBusy = Box != null && Box.Busy
            };
            for (int i = 0; i < _doors.Count; i++) if (_doors[i] == null || _doors[i].Cleared) s.doorMask |= 1 << i;
            foreach (var h in _g.Squad)
            {
                if (h == null) continue;
                var lo = h.Loadout;
                var ammo = h.Ammo;
                s.members.Add(new NetMember
                {
                    slot = h.Slot, x = h.transform.position.x, y = h.transform.position.y, aimX = h.Aim.x, aimY = h.Aim.y,
                    hp = h.Health, maxHp = h.MaxHealth, points = Wallet(h).Points, kills = h.Kills,
                    mag = ammo != null ? ammo.mag : 0, reserve = ammo != null ? ammo.reserve : 0,
                    gunIndex = ZLNet.GunIndex(lo != null ? lo.equipped : ""), perkMask = lo != null ? ZLNet.PerkMask(lo.perks) : 0,
                    downed = h.Downed, out_ = h.Out, forged = lo != null && ZLShop.IsForged(lo, lo.equipped)
                });
            }
            foreach (var z in _g.Zombies)
            {
                if (z == null || !z.Alive) continue;
                s.zombies.Add(new NetZombie { id = z.NetId, kind = (int)z.kind, x = z.transform.position.x, y = z.transform.position.y, hp01 = Mathf.Clamp(Mathf.RoundToInt(255f * z.hp / Mathf.Max(1, z.maxHp)), 1, 255) });
            }
            if (s.zombies.Count > ZLNet.MaxSnapshotZombies)
            {
                // too many to fit one packet: guests get the ones nearest the squad
                float Near(NetZombie z)
                {
                    float best = float.MaxValue;
                    foreach (var m in s.members) { float dx = m.x - z.x, dy = m.y - z.y; best = Mathf.Min(best, dx * dx + dy * dy); }
                    return best;
                }
                s.zombies.Sort((a, b) => Near(a).CompareTo(Near(b)));
            }
            foreach (var p in _g.PowerUpsOnGround()) s.powerUps.Add(new NetPowerUp { id = p.id, kind = (int)p.kind, x = p.at.x, y = p.at.y });
            return s;
        }

        readonly Dictionary<int, ZLZombie> _puppets = new Dictionary<int, ZLZombie>();
        readonly Dictionary<int, GameObject> _puVisuals = new Dictionary<int, GameObject>();
        readonly HashSet<int> _seen = new HashSet<int>();

        /// <summary>Guest: show what the host says.</summary>
        public void ApplySnapshot(NetSnapshot s, int mySlot)
        {
            if (!_guest) return;
            Intermission = s.intermission;
            IntermissionLeft = s.intermissionLeft;
            HoundRound = s.houndRound;
            _guestLeft = s.zombiesLeft;
            _g.MirrorPowerUps(s.instaKill, s.doublePoints);
            foreach (var m in s.members)
            {
                var h = HeroInSlot(m.slot);
                if (h == null) continue;
                if (m.slot == mySlot)
                {
                    Run.Mirror(s.round, m.points, m.kills);
                    h.Kills = m.kills;
                    MirrorLoadout(h.Loadout, m);
                    h.MirrorState(m.hp, m.downed, m.out_);
                }
                else
                {
                    if (h.Wallet != null) h.Wallet.Mirror(s.round, m.points, m.kills);
                    h.Kills = m.kills;
                    MirrorLoadout(h.Loadout, m);
                    h.SetNetPosition(new Vector2(m.x, m.y), new Vector2(m.aimX, m.aimY));
                    h.MirrorState(m.hp, m.downed, m.out_);
                }
            }
            // zombies
            _seen.Clear();
            foreach (var nz in s.zombies)
            {
                _seen.Add(nz.id);
                if (!_puppets.TryGetValue(nz.id, out var z) || z == null)
                {
                    z = _g.SpawnZombie((ZLFoe)nz.kind, new Vector2(nz.x, nz.y), 1, false);
                    z.MakePuppet(nz.id);
                    if ((ZLFoe)nz.kind == ZLFoe.Herald && s.round % 10 != 0) { z.transform.localScale = Vector3.one * 1.5f; }
                    _puppets[nz.id] = z;
                    _alive.Add(z);
                }
                z.SetNet(new Vector2(nz.x, nz.y), nz.hp01 / 255f);
            }
            var gone = new List<int>();
            foreach (var kv in _puppets) if (!_seen.Contains(kv.Key)) gone.Add(kv.Key);
            foreach (int id in gone)
            {
                var z = _puppets[id];
                _puppets.Remove(id);
                _alive.Remove(z);
                if (z != null) z.PuppetDie();
            }
            // power-ups
            _seen.Clear();
            foreach (var p in s.powerUps)
            {
                _seen.Add(p.id);
                if (_puVisuals.TryGetValue(p.id, out var go) && go != null) continue;
                go = new GameObject("PowerUpView");
                go.transform.position = new Vector2(p.x, p.y);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _g.Sprite("Props/" + ZLShop.PowerUpArt((ZLPowerUp)p.kind));
                sr.sortingOrder = ZLSort.Canopy - 1;
                if (_g.material != null) sr.sharedMaterial = _g.material;
                _puVisuals[p.id] = go;
            }
            gone.Clear();
            foreach (var kv in _puVisuals) if (!_seen.Contains(kv.Key)) gone.Add(kv.Key);
            foreach (int id in gone) { if (_puVisuals[id] != null) Destroy(_puVisuals[id]); _puVisuals.Remove(id); }
            foreach (var kv in _puVisuals) if (kv.Value != null) kv.Value.transform.localScale = Vector3.one * (SettingsService.ReducedMotion ? 1.1f : 1.1f + Mathf.Sin(Time.time * 5f) * 0.1f);
            // doors and the box
            for (int i = 0; i < _doors.Count; i++)
                if ((s.doorMask & (1 << i)) != 0 && _doors[i] != null && !_doors[i].Cleared) { _openZones.Add(_doors[i].Data.opens); ClearDoor(_doors[i]); }
            if (Box != null) Box.MirrorState(s.boxSpot, s.boxBusy);
            if (s.over && !Over) GuestOver(s.won);
        }

        static void MirrorLoadout(ZombieLandProgress lo, NetMember m)
        {
            if (lo == null) return;
            string gid = ZLNet.GunId(m.gunIndex);
            var gun = lo.Gun(gid);
            if (gun == null) { gun = new ZLOwnedGun { id = gid }; lo.guns.Add(gun); }
            lo.equipped = gid;
            gun.mag = m.mag;
            gun.reserve = m.reserve;
            if (m.forged && !lo.forged.Contains(gid)) lo.forged.Add(gid);
            lo.perks.Clear();
            lo.perks.AddRange(ZLNet.PerksFromMask(m.perkMask));
        }

        /// <summary>Guest: the host ended the run (or left).</summary>
        public void GuestOver(bool won)
        {
            if (Over) return;
            Run.Won = won;
            GameOver();
        }

        // ---- the end ---------------------------------------------------------------------------------------

        public void GameOver()
        {
            if (Over) return;
            var before = ZombieDailyTracker.Snapshot();
            Over = true;
            Intermission = false;
            if (_g.Net != null && _g.Net.IsHost) _g.Net.BroadcastOver(Run.Won, Run.Round, Run.Kills);
            var save = Services.Save;
            if (save != null)
            {
                var c = save.Data.zombieCareer ?? (save.Data.zombieCareer = new ZombieCareer());
                // Weekly runs (mutators) keep their own best; they don't touch the map's records or leaderboards.
                if (Weekly == null) NewBest = c.RecordRound(Map.Id, Mode, Run.Round);
                c.totalKills += Run.Kills;
                if (Run.Won) c.raidWins++;
                if (Mode == SurvivalMode.Hardcore) c.hardcoreRuns++;
                if (Kind != SquadKind.Solo) c.coopRuns++;
                c.lastStandRuns++;
                ShardReward = SurvivalModes.Shards(Mode, Run.Round, Run.Kills, Run.Won);
                Economy.Grant(save.Data, ShardReward);
                save.SaveNow();
            }
            ProgressTracker.Report(ChallengeKind.SurvivalRound, Run.Round);
            ProgressTracker.AddXp(PlayerLevel.SurvivalRoundXp * Mathf.Max(0, Run.Round - 1));
            XpEarned = SurvivorRank.RunXp(Mode, Run.Round, Run.Kills, Run.Won);
            RanksReached = ProgressTracker.AddZombieXp(XpEarned);
            if (Weekly != null) CheckWeekly(Mathf.Max(0, Run.Won ? Run.Round : Run.Round - 1));
            else ZombieBoards.Submit(Map.Id, Mode, Run.Round);
            Tell(ZombieDailyTracker.AfterProgress(before));
            if (Services.Save != null) Services.Save.SaveNow();
            Sfx.Play(Run.Won ? Sfx.Victory : Sfx.KO, 0.9f, 0f);
            StartCoroutine(ShowOver());
        }

        IEnumerator ShowOver()
        {
            yield return new WaitForSecondsRealtime(1.6f);
            var safe = UIFactory.CreateCanvas(transform, "LastStandOver", 70, landscape: true, out _);
            _overRoot = safe;
            UIFactory.Dim(safe);
            var touch = FindAnyObjectByType<TouchControlsUI>();
            if (touch != null) touch.gameObject.SetActive(false);
            var gold = new Color32(255, 214, 90, 255);
            bool squad = _g.Squad.Count > 1;
            var panel = UIFactory.Panel(safe, new Vector2(1300f, squad ? 1040f : 1000f));
            var title = UIFactory.Label(panel, Run.Won ? "RAID COMPLETE!" : Map.Title + " FELL", 70, TextAnchor.MiddleCenter, 96f, title: true);
            title.color = Run.Won ? (Color)gold : new Color32(255, 100, 70, 255);
            UIFactory.Label(panel, $"{SurvivalModes.Name(Mode)}  ·  ROUND {Run.Round}" + (Ls.HiddenBossBeaten ? $"  ·  {Map.HiddenBossName} SLAIN" : ""), 46,
                            TextAnchor.MiddleCenter, 64f, title: true);
            if (squad)
            {
                // the squad's scoreboard
                var sorted = new List<ZLPlayer>(_g.Squad);
                sorted.Sort((a, b) => b.Kills.CompareTo(a.Kills));
                foreach (var h in sorted)
                {
                    if (h == null) continue;
                    var row = UIFactory.Label(panel, $"{h.DisplayName}   {h.Kills} kills   {Wallet(h).TotalEarned} pts   {RevivesBy(h)} revives", 30, TextAnchor.MiddleCenter, 40f);
                    row.color = SlotColor(h.Slot);
                }
            }
            else
                UIFactory.Label(panel, $"{Run.Kills} zombies  ·  {Run.TotalEarned} points  ·  {SurvivalModes.Caches(Run.Won ? Run.Round : Run.Round - 1)} supply caches",
                                34, TextAnchor.MiddleCenter, 54f);
            var career = Services.Save != null ? Services.Save.Data.zombieCareer : null;
            if (Weekly != null)
            {
                bool done = ZombieWeekly.Done(career, Weekly);
                UIFactory.Label(panel, (WeeklyPaid ? "WEEKLY CHALLENGE COMPLETE!  " : done ? "Weekly done  ·  " : $"Weekly goal: round {Weekly.GoalRound}  ·  ")
                                       + $"best this week: round {ZombieWeekly.BestThisWeek(career, Weekly)}", 32, TextAnchor.MiddleCenter, 48f)
                    .color = WeeklyPaid ? (Color)gold : UITheme.Current.subtle;
            }
            else
            {
                int best = career != null ? career.Best(Map.Id, Mode) : Run.Round;
                UIFactory.Label(panel, (NewBest ? "NEW BEST!  " : "") + $"Best {SurvivalModes.Name(Mode)} round: {best}", 32, TextAnchor.MiddleCenter, 48f)
                    .color = NewBest ? (Color)gold : UITheme.Current.subtle;
            }
            UIFactory.Label(panel, $"+{ShardReward} Star Shards   ·   +{XpEarned} Survivor XP", 34, TextAnchor.MiddleCenter, 50f).color = gold;
            var news = new List<string>();
            foreach (var l in _endLines) if (l.StartsWith("NEW IN THE LOCKER")) news.Add(l);
            int day = ZombieDailyTracker.Today;
            string daily = $"DAILY ORDERS {ZombieDaily.DoneCount(career, day)}/{ZombieDaily.Orders}  ·  STREAK {ZombieDaily.StreakNow(career, day)}";
            UIFactory.Label(panel, news.Count > 0 ? string.Join("   ", news) : daily, 28, TextAnchor.MiddleCenter, 40f)
                .color = news.Count > 0 ? (Color)gold : UITheme.Current.subtle;
            if (career != null)
            {
                int rank = SurvivorRank.RankFor(career.xp);
                var worn = ZombieCosmetics.Worn(career, CosmeticSlot.Title, ZombieDailyTracker.Snapshot());
                UIFactory.Label(panel, (worn != null ? worn.Name + "  ·  " : "") + (RanksReached.Count > 0 ? $"RANK UP!  SURVIVOR RANK {rank} · {SurvivorRank.Title(rank)}"
                                                              : $"SURVIVOR RANK {rank}  ·  {Mathf.RoundToInt(SurvivorRank.Progress01(career.xp) * 100f)}% to the next"),
                                28, TextAnchor.MiddleCenter, 44f).color = RanksReached.Count > 0 ? (Color)gold : UITheme.Current.subtle;
            }
            var buttons = UIFactory.Rect("Buttons", panel);
            var ble = buttons.gameObject.AddComponent<LayoutElement>();
            ble.preferredHeight = ble.minHeight = UIFactory.RowHeight;
            ble.flexibleHeight = 0f;
            var h2 = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
            h2.spacing = 24f;
            h2.childControlWidth = true; h2.childControlHeight = true;
            h2.childForceExpandWidth = true; h2.childForceExpandHeight = true;
            Button first = null;
            if (Kind != SquadKind.Online)
                first = UIFactory.Button(buttons, "PLAY AGAIN", () => { if (Services.Scenes != null) Services.Scenes.ReloadCurrent(); });
            int round = Run.Round, kills = Run.Kills;
            bool won = Run.Won;
            string mapName = Map.Short;
            string with = Kind == SquadKind.Online ? " with friends" : Kind == SquadKind.AISquad ? " with my squad" : "";
            UIFactory.Button(buttons, Highlights.Available ? "SHARE CLIP" : "SHARE", () => Highlights.Share(
                (won ? $"I held {mapName} through the Raid{with} ({kills} zombies down)" : $"I held {mapName} for {round} round{(round == 1 ? "" : "s")}{with} ({kills} down)") +
                " in Pixel Kingdom Rumble! " + ShareCard.Link));
            var back = UIFactory.Button(buttons, "ZOMBIES", () =>
            {
                if (PKRMatchService.Instance != null) PKRMatchService.Instance.EndMatch();
                GameSession.ReopenEventMenu = true;
                if (Services.Scenes != null) Services.Scenes.Load(SceneIds.MainMenu);
            });
            UIFactory.Select(first != null ? first : back);
        }

        // ---- screenshots and tests ------------------------------------------------------------------------

        public IEnumerator ShotSetup(string shot)
        {
            yield return new WaitForSeconds(1f);
            if (shot == "over") { Run.NextRound(); Run.NextRound(); Run.NextRound(); GameOver(); yield break; }
            if (shot.EndsWith("demo"))
            {
                // The trailer: everyone (you too) is played by the AI, deep enough in for riot shields and leapers,
                // with both wonder weapons out.
                var touch = FindAnyObjectByType<TouchControlsUI>();
                if (touch != null) touch.gameObject.SetActive(false);
                if (_g.Squad.Count < 2) SetupAI(3);
                _g.Player.Loadout = _g.Progress;
                _g.Player.BecomeAI(new ZLAIBrain(_g, 0));
                foreach (var h in _g.Squad) if (h != null) h.DebugInvulnerable = true;
                DebugGivePoints(12000);
                foreach (var dd in _doors.ToArray()) if (dd != null && !dd.Cleared) OpenDoor(dd);
                DebugSkipToRound(9);
                yield return null;
                yield return null;   // round 9 starts (the AI kit their guns)...
                GiveGun(_g.Player, Map == LastStandMap.RailYard ? ZLCatalog.Singularity : ZLCatalog.RimeCannon);   // ...then the wonders come out
                var second = HeroInSlot(1);
                if (second != null) GiveGun(second, ZLCatalog.StormCoil);
                yield break;
            }
            DebugGivePoints(8000);
            foreach (var h in _g.Squad) if (h != null) h.DebugInvulnerable = true;
            if (shot == "box" && Box != null)
            {
                _g.Player.transform.position = Box.UsePos + Vector2.down * 1.2f;
                Box.Use(_g.Player);
            }
            IntermissionLeft = 0f;
            for (int i = 0; i < 40; i++) yield return null;
            _g.Player.transform.position = _g.Player.transform.position + Vector3.up * 2f;
        }

        public void DebugGivePoints(int n) => Run.Earn(n);

        /// <summary>Test hook: jump to a round (it starts at once).</summary>
        public void DebugSkipToRound(int round)
        {
            foreach (var z in _alive.ToArray()) if (z != null) z.Vanish(false);
            _alive.Clear();
            _toSpawn = 0; _bossPending = false; _kingPending = false;
            while (Run.Round < round - 1) Run.NextRound();
            Intermission = true;
            IntermissionLeft = 0f;
        }

        /// <summary>Test hook: every zombie now and still to come is gone (the round ends).</summary>
        public void DebugClearRound()
        {
            _toSpawn = 0; _bossPending = false; _kingPending = false;
            foreach (var z in _alive.ToArray()) if (z != null && z.Alive) z.Hit(999999, Vector2.up, null);
        }

        public void DebugFindAllFragments()
        {
            foreach (var f in FindObjectsByType<ZLFragment>(FindObjectsSortMode.None)) if (f.CanUse) f.Use(_g.Player);
        }
    }
}
