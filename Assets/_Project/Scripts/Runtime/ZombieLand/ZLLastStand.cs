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
    /// </summary>
    public class ZLLastStand : MonoBehaviour
    {
        public LastStandRun Ls { get; private set; }
        public SurvivalRun Run => Ls.Run;
        public SurvivalMode Mode => Run.Mode;
        public bool Intermission { get; private set; } = true;
        public float IntermissionLeft { get; private set; }
        public bool HoundRound { get; private set; }
        public bool Over { get; private set; }
        public int ZombiesLeft => _toSpawn + _alive.Count + (_bossPending ? 1 : 0) + (_kingPending ? 1 : 0);
        public IReadOnlyList<ZLZombie> Alive => _alive;
        public ZLMysteryBox Box { get; private set; }
        public int ShardReward { get; private set; }
        public int XpEarned { get; private set; }
        public bool NewBest { get; private set; }
        public List<int> RanksReached { get; private set; } = new List<int>();
        public ZLZombie Butcher { get; private set; }
        public ZLZombie HollowKing { get; private set; }

        ZLGame _g;
        readonly List<ZLZombie> _alive = new List<ZLZombie>();
        readonly HashSet<string> _openZones = new HashSet<string>();
        readonly List<ZLDebrisDoor> _doors = new List<ZLDebrisDoor>();
        int _toSpawn;
        bool _bossPending, _kingPending;
        float _spawnT;
        Vector2 _lastKillAt;
        RectTransform _overRoot;

        public void Init(ZLGame g)
        {
            _g = g;
            int xp = g.CareerXp;
            Ls = new LastStandRun(GameSession.Mode == SessionMode.Zombies ? GameSession.ZombieMode : SurvivalMode.Endless, SurvivorRank.BonusPoints(xp))
            {
                FreeSpinLeft = SurvivorRank.Has(xp, "kit_spin")
            };
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
                f.Init(g, this);
                g.RegisterUse(f);
            }
            IntermissionLeft = 4f;
            g.ShowToast($"LAST STAND: HOLLOW BLOCK  ·  {SurvivalModes.Name(Mode)}", 4f);
        }

        // ---- rounds -----------------------------------------------------------------------------------------

        public void Tick(float dt)
        {
            if (Over) return;
            if (Intermission)
            {
                IntermissionLeft -= dt;
                if (IntermissionLeft <= 0f) StartRound();
                return;
            }
            SpawnTick(dt);
            _alive.RemoveAll(z => z == null || !z.Alive);
            if (_toSpawn <= 0 && !_bossPending && !_kingPending && _alive.Count == 0) EndRound();
        }

        void StartRound()
        {
            int round = Run.NextRound();
            Intermission = false;
            HoundRound = LastStandRules.IsHoundRound(Mode, round);
            _toSpawn = HoundRound ? LastStandRules.HoundsInRound(round) : LastStandRules.ZombiesInRound(round);
            _bossPending = LastStandRules.IsBossRound(Mode, round);
            if (Ls.HiddenBossSummoned && !Ls.HiddenBossBeaten && HollowKing == null) _kingPending = true;
            _spawnT = 1.5f;
            Sfx.Play(Sfx.RoundStart, 0.9f, 0f);
            string head = Mode == SurvivalMode.Raid ? $"ROUND {round}/{SurvivalModes.RaidRounds}" : $"ROUND {round}";
            if (HoundRound) head += "  ·  THE HOUNDS ARE COMING";
            if (_bossPending) head += "  ·  " + LastStandRules.BossName + "!";
            if (_kingPending) head += "  ·  " + LastStandRules.HiddenBossName + " WAKES";
            _g.ShowToast(head, 3.5f);
            if (HoundRound && Services.Haptics != null) Services.Haptics.Play(HapticStrength.Heavy);
        }

        void EndRound()
        {
            int bonus = Run.Earn(ZombieRules.RoundBonus(Run.Round));
            if (HoundRound) _g.SpawnPowerUp(ZLPowerUp.MaxAmmo, _lastKillAt);   // the classic reward for a hound round
            if (SurvivalModes.IsFinalRound(Mode, Run.Round))
            {
                Run.Won = true;
                GameOver();
                return;
            }
            _g.ShowToast($"ROUND {Run.Round} SURVIVED!  +{bonus}", 2.5f);
            if (SurvivalModes.CacheAfter(Run.Round))
                _g.SpawnPowerUp(Run.Round % 2 == 0 ? ZLPowerUp.MaxAmmo : ZLShop.PickPowerUp(Random.value), _g.TargetPos + Vector2.up * 1.5f);
            Intermission = true;
            IntermissionLeft = SurvivalModes.Intermission(Mode);
            HoundRound = false;
        }

        void SpawnTick(float dt)
        {
            _spawnT -= dt;
            if (_spawnT > 0f) return;
            if (_alive.Count >= LastStandRules.MaxAlive(Run.Round)) { _spawnT = 0.4f; return; }
            if (_kingPending) { _kingPending = false; SpawnHollowKing(); _spawnT = 2f; return; }
            if (_bossPending && _toSpawn <= LastStandRules.ZombiesInRound(Run.Round) - 4) { _bossPending = false; SpawnButcher(); _spawnT = 2f; return; }
            if (_toSpawn <= 0)
            {
                if (_bossPending) { _bossPending = false; SpawnButcher(); }
                return;
            }
            var kind = HoundRound ? ZLFoe.Hound : LastStandRules.Pick(Run.Round, Random.value);
            if (Spawn(kind) != null) _toSpawn--;
            _spawnT = LastStandRules.SpawnInterval(Run.Round, Mode) * (HoundRound ? 0.7f : 1f);
        }

        /// <summary>A spawn in an open zone, out of view if possible, nearest the hero first.</summary>
        bool SpawnSpot(out Vector2 spot)
        {
            var cam = Camera.main;
            float hh = cam != null ? cam.orthographicSize + 1f : 8f;
            float hw = cam != null ? hh * cam.aspect : 14f;
            Vector2 cp = cam != null ? (Vector2)cam.transform.position : _g.TargetPos;
            var open = new List<Vector2>();
            var hidden = new List<Vector2>();
            foreach (var s in _g.City.spawns)
            {
                if (!_openZones.Contains(s.zone)) continue;
                var p = new Vector2(s.x, s.y);
                open.Add(p);
                if (Mathf.Abs(p.x - cp.x) > hw || Mathf.Abs(p.y - cp.y) > hh) hidden.Add(p);
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
            z.maxHp = z.hp = LastStandRules.Health(kind, Run.Round, Mode);
            z.speed = LastStandRules.Speed(kind, Run.Round, Mode) * Random.Range(0.9f, 1.1f);
            _alive.Add(z);
            return z;
        }

        void SpawnButcher()
        {
            var z = Spawn(ZLFoe.Herald);
            if (z == null) return;
            z.maxHp = z.hp = LastStandRules.BossHealth(Run.Round, Mode, false);
            z.speed = 2.8f * SurvivalModes.SpeedScale(Mode);
            Butcher = z;
            _g.ShowToast(LastStandRules.BossName + "!", 3f);
            if (CameraFollowTopDown.Main != null) CameraFollowTopDown.Main.Shake(0.3f, 0.5f);
            Sfx.Play(Sfx.Spotted, 1f, 0f);
        }

        void SpawnHollowKing()
        {
            var z = Spawn(ZLFoe.Herald);
            if (z == null) return;
            z.maxHp = z.hp = LastStandRules.BossHealth(Run.Round, Mode, true);
            z.speed = 3.1f * SurvivalModes.SpeedScale(Mode);
            z.transform.localScale = Vector3.one * 1.5f;
            foreach (var sr in z.GetComponentsInChildren<SpriteRenderer>()) sr.color = new Color(0.75f, 0.65f, 1f);
            HollowKing = z;
            _g.ShowToast(LastStandRules.HiddenBossName + "!", 3.5f);
            if (CameraFollowTopDown.Main != null) CameraFollowTopDown.Main.Shake(0.4f, 0.8f);
            Sfx.Play(Sfx.Spotted, 1f, 0f);
        }

        // ---- points ---------------------------------------------------------------------------------------

        public void ZombieHit(ZLZombie z, bool killing)
        {
            if (Over || killing) return;
            Run.Earn(LastStandRules.HitPoints);
        }

        public void ZombieDied(ZLZombie z)
        {
            _alive.Remove(z);
            _lastKillAt = z.transform.position;
            Run.KillFor(LastStandRules.KillPoints(z.kind));
            if (z == Butcher)
            {
                Butcher = null;
                _g.ShowToast(LastStandRules.BossName + " FALLS!", 3f);
                _g.SpawnPowerUp(ZLShop.PickPowerUp(Random.value), z.transform.position);
            }
            if (z == HollowKing)
            {
                HollowKing = null;
                HollowKingDefeated();
            }
        }

        public void Nuked() => Run.Earn(ZombieRules.MoonburstPoints);

        // ---- the shop on the block ----------------------------------------------------------------------

        public bool Pay(int cost)
        {
            if (Run.Spend(cost)) { Sfx.Play(Sfx.Purchase, 0.8f, 0f); return true; }
            Sfx.Play(Sfx.Click, 0.6f, 0f);
            _g.ShowToast("NOT ENOUGH POINTS", 1.5f);
            return false;
        }

        public void OpenDoor(ZLDebrisDoor door)
        {
            var d = door.Data;
            if (!Ls.OpenDoor(d.id, d.cost)) { Pay(d.cost); return; }
            Sfx.Play(Sfx.Secret, 0.8f, 0f);
            _g.City.Open(d);
            door.Clear();
            _g.UnregisterUse(door);
            bool opened = _openZones.Add(d.opens);
            string name = d.opens;
            foreach (var z in _g.City.zones) if (z.id == d.opens) name = z.name;
            _g.ShowToast(opened ? name + " IS OPEN" : "THE WAY IS CLEAR", 2.5f);
            // a debris pile can open a zone that another pile also leads to: clear the twin's purpose, not its body
        }

        public bool HasZone(string zone) => string.IsNullOrEmpty(zone) || _openZones.Contains(zone);

        public bool BuyPerk(ZLPerk k)
        {
            if (Ls.Has(k)) return false;
            if (Ls.PerkCount >= Ls.PerkLimit) { _g.ShowToast($"PERK LIMIT ({Ls.PerkLimit})" + (Ls.HiddenBossBeaten ? "" : ". THE HOLLOW KING GUARDS A FIFTH."), 2.5f); return false; }
            if (!Ls.BuyPerk(k)) { Pay(LastStandRules.PerkCost(k)); return false; }
            _g.Progress.perks.Add(ZLShop.PerkKey(k));   // the hero reads perks from the run's loadout
            if (k == ZLPerk.ThickSkin) _g.Player.Heal(50);
            Sfx.Play(Sfx.Purchase, 0.9f, 0f);
            _g.ShowToast(ZLShop.PerkName(k) + "!  " + ZLShop.PerkInfo(k), 2.5f);
            return true;
        }

        public bool UseSecondChance()
        {
            if (!Ls.UseSecondChance()) return false;
            _g.Progress.perks.Remove(ZLShop.PerkKey(ZLPerk.SecondChance));
            return true;
        }

        /// <summary>A wall gun: buy it (full ammo; holding 3 already swaps out the one in hand) or buy ammo.</summary>
        public void UseWallGun(ZLGun gun, int cost)
        {
            var p = _g.Progress;
            var owned = p.Gun(gun.id);
            if (owned != null)
            {
                if (!Pay(cost / 2)) return;
                var e = ZLShop.Effective(p, gun);
                owned.mag = e.magazine;
                owned.reserve = e.maxReserve;
                Sfx.Play(Sfx.Reload, 0.7f, 0f);
                _g.ShowToast(gun.name + " AMMO", 1.5f);
                return;
            }
            if (!Pay(cost)) return;
            GiveGun(gun);
            _g.ShowToast(gun.name + "!", 2f);
        }

        public void GiveGun(ZLGun gun)
        {
            var p = _g.Progress;
            if (p.Gun(gun.id) == null && p.guns.Count >= 3) p.guns.RemoveAll(x => x.id == p.equipped);
            var e = ZLShop.Effective(p, gun);
            ZLRules.GiveGun(p, gun, e.magazine + e.maxReserve);
            p.equipped = gun.id;
            _g.Player.CancelReload();
        }

        public void UseForge()
        {
            var p = _g.Progress;
            var held = ZLCatalog.Gun(p.equipped);
            if (!ZLShop.CanForge(p, held)) { _g.ShowToast(held != null && held.isRelic ? "NOT THAT ONE" : "ALREADY FORGED", 1.5f); return; }
            int cost = LastStandRules.ForgePrice(SurvivorRank.Has(_g.CareerXp, "kit_forge"));
            if (!Pay(cost)) return;
            ZLShop.Forge(p, held.id, 0);
            _g.Player.CancelReload();
            Sfx.Play(Sfx.Special, 0.9f, 0f);
            _g.ShowToast("HOLLOW-FORGED " + held.name + "!", 2.5f);
        }

        public int ForgeCost => LastStandRules.ForgePrice(SurvivorRank.Has(_g.CareerXp, "kit_forge"));

        // ---- the Easter egg --------------------------------------------------------------------------------

        public void FoundFragment(ZLFragment f)
        {
            bool last = Ls.FindFragment();
            Sfx.Play(Sfx.Secret, 1f, 0f);
            _g.UnregisterUse(f);
            if (!last)
            {
                _g.ShowToast($"A GRAYSTONE FRAGMENT HUMS... ({Ls.FragmentsFound}/{LastStandRules.Fragments})", 3f);
                return;
            }
            Ls.HiddenBossSummoned = true;
            _g.ShowToast("THE FRAGMENTS SING. SOMETHING UNDER THE BLOCK IS AWAKE.", 4f);
            if (CameraFollowTopDown.Main != null) CameraFollowTopDown.Main.Shake(0.3f, 0.8f);
        }

        void HollowKingDefeated()
        {
            Ls.HiddenBossBeaten = true;
            Run.Earn(3000);
            var save = Services.Save;
            if (save != null)
            {
                var c = save.Data.zombieCareer ?? (save.Data.zombieCareer = new ZombieCareer());
                if (!c.Has(LastStandRules.HiddenBossSecret)) c.secrets.Add(LastStandRules.HiddenBossSecret);
                Economy.Grant(save.Data, SurvivalModes.HiddenBossShards);
                save.SaveNow();
            }
            ProgressTracker.AddZombieXp(SurvivorRank.HiddenBossXp);
            _g.ShowToast($"THE HOLLOW KING IS DEAD!  +{SurvivalModes.HiddenBossShards} STAR SHARDS  ·  5TH PERK SLOT", 4.5f);
            Sfx.Play(Sfx.Victory, 1f, 0f);
        }

        // ---- the end ---------------------------------------------------------------------------------------

        public void GameOver()
        {
            if (Over) return;
            Over = true;
            var save = Services.Save;
            if (save != null)
            {
                var c = save.Data.zombieCareer ?? (save.Data.zombieCareer = new ZombieCareer());
                NewBest = c.RecordRound(LastStandRules.MapId, Mode, Run.Round);
                c.totalKills += Run.Kills;
                if (Run.Won) c.raidWins++;
                if (Mode == SurvivalMode.Hardcore) c.hardcoreRuns++;
                ShardReward = SurvivalModes.Shards(Mode, Run.Round, Run.Kills, Run.Won);
                Economy.Grant(save.Data, ShardReward);
                save.SaveNow();
            }
            ProgressTracker.Report(ChallengeKind.SurvivalRound, Run.Round);
            ProgressTracker.AddXp(PlayerLevel.SurvivalRoundXp * Mathf.Max(0, Run.Round - 1));
            XpEarned = SurvivorRank.RunXp(Mode, Run.Round, Run.Kills, Run.Won);
            RanksReached = ProgressTracker.AddZombieXp(XpEarned);
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
            var panel = UIFactory.Panel(safe, new Vector2(1240f, 960f));
            var title = UIFactory.Label(panel, Run.Won ? "RAID COMPLETE!" : "HOLLOW BLOCK FELL", 76, TextAnchor.MiddleCenter, 104f, title: true);
            title.color = Run.Won ? (Color)gold : new Color32(255, 100, 70, 255);
            UIFactory.Label(panel, $"{SurvivalModes.Name(Mode)}  ·  ROUND {Run.Round}" + (Ls.HiddenBossBeaten ? "  ·  HOLLOW KING SLAIN" : ""), 50,
                            TextAnchor.MiddleCenter, 70f, title: true);
            UIFactory.Label(panel, $"{Run.Kills} zombies  ·  {Run.TotalEarned} points  ·  {SurvivalModes.Caches(Run.Won ? Run.Round : Run.Round - 1)} supply caches",
                            34, TextAnchor.MiddleCenter, 54f);
            var career = Services.Save != null ? Services.Save.Data.zombieCareer : null;
            int best = career != null ? career.Best(LastStandRules.MapId, Mode) : Run.Round;
            UIFactory.Label(panel, (NewBest ? "NEW BEST!  " : "") + $"Best {SurvivalModes.Name(Mode)} round: {best}", 34, TextAnchor.MiddleCenter, 52f)
                .color = NewBest ? (Color)gold : UITheme.Current.subtle;
            UIFactory.Label(panel, $"+{ShardReward} Star Shards   ·   +{XpEarned} Survivor XP", 36, TextAnchor.MiddleCenter, 54f).color = gold;
            if (career != null)
            {
                int rank = SurvivorRank.RankFor(career.xp);
                UIFactory.Label(panel, RanksReached.Count > 0 ? $"RANK UP!  SURVIVOR RANK {rank} · {SurvivorRank.Title(rank)}"
                                                              : $"SURVIVOR RANK {rank}  ·  {Mathf.RoundToInt(SurvivorRank.Progress01(career.xp) * 100f)}% to the next",
                                30, TextAnchor.MiddleCenter, 48f).color = RanksReached.Count > 0 ? (Color)gold : UITheme.Current.subtle;
            }
            var buttons = UIFactory.Rect("Buttons", panel);
            var ble = buttons.gameObject.AddComponent<LayoutElement>();
            ble.preferredHeight = ble.minHeight = UIFactory.RowHeight;
            ble.flexibleHeight = 0f;
            var h = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 24f;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = true; h.childForceExpandHeight = true;
            var again = UIFactory.Button(buttons, "PLAY AGAIN", () => { if (Services.Scenes != null) Services.Scenes.ReloadCurrent(); });
            int round = Run.Round, kills = Run.Kills;
            bool won = Run.Won;
            UIFactory.Button(buttons, Highlights.Available ? "SHARE CLIP" : "SHARE", () => Highlights.Share(
                (won ? $"I held Hollow Block through the Raid ({kills} zombies down)" : $"I held Hollow Block for {round} round{(round == 1 ? "" : "s")} ({kills} down)") +
                " in Pixel Kingdom Rumble! " + ShareCard.Link));
            UIFactory.Button(buttons, "ZOMBIES", () =>
            {
                GameSession.ReopenEventMenu = true;
                if (Services.Scenes != null) Services.Scenes.Load(SceneIds.MainMenu);
            });
            UIFactory.Select(again);
        }

        // ---- screenshots and tests ------------------------------------------------------------------------

        public IEnumerator ShotSetup(string shot)
        {
            yield return new WaitForSeconds(1f);
            if (shot == "over") { Run.NextRound(); Run.NextRound(); Run.NextRound(); GameOver(); yield break; }
            DebugGivePoints(8000);
            _g.Player.DebugInvulnerable = true;
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
