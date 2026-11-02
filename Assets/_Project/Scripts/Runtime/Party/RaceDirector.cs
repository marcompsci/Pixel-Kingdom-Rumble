using System.Collections;
using System.Collections.Generic;
using PKR.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// ITEM RACE: the hero against three CPU racers on the race course. A 3-2-1-GO start, item boxes along the way
    /// (catch-up odds), live places, a finish line at the gate, results and Star Shards. The course's own goal gate
    /// only marks the line; the race decides who wins.
    /// </summary>
    public class RaceDirector : MonoBehaviour, IPartyHost
    {
        LevelFlowController _flow;
        readonly List<GameObject> _racers = new List<GameObject>();
        readonly List<string> _names = new List<string>();
        readonly List<float> _times = new List<float>();
        readonly List<RacerBot> _bots = new List<RacerBot>();
        RaceStandings _standings;
        float _startX, _clock;
        bool _running, _over;
        Material _mat;

        static readonly Color[] Tints = { Color.white, new Color(1f, 0.6f, 0.6f), new Color(0.6f, 1f, 0.65f), new Color(1f, 0.92f, 0.5f) };

        IEnumerator Start()
        {
            _flow = GetComponent<LevelFlowController>();
            _mat = _flow != null ? _flow.SpriteMaterial : null;
            var hero = _flow != null ? _flow.Player : null;
            var goal = FindAnyObjectByType<GoalGate>();
            if (hero == null || goal == null) yield break;
            foreach (var c in goal.GetComponentsInChildren<Collider2D>()) c.enabled = false; // the race handles the line
            float finishX = goal.transform.position.x;
            var heroMotor = hero.GetComponent<PlatformerMotor2D>();
            _startX = hero.transform.position.x;

            // Grid: two CPUs in front, one behind, the hero in the middle.
            _racers.Add(hero);
            _names.Add("YOU");
            _times.Add(0f);
            var roster = _flow.Roster;
            var rng = new System.Random(System.Environment.TickCount);
            float[] offsets = { 3.4f, 1.8f, -1.6f };
            for (int i = 0; i < 3; i++)
            {
                var def = PickHero(roster, rng);
                if (def == null) break;
                var pos = new Vector2(_startX + offsets[i], hero.transform.position.y + 0.2f);
                var go = FighterFactory.CreateHero(def, _mat, pos, i + 1, DamageModel.StoryHealth, Tints[i + 1], "CPU " + (i + 1));
                var bot = go.AddComponent<RacerBot>();
                bot.Init(rng.Next());
                ItemBuffs.On(go).baseSpeed = 0.94f + (float)rng.NextDouble() * 0.06f;
                _racers.Add(go);
                _bots.Add(bot);
                _names.Add(def.displayName.ToUpperInvariant());
                _times.Add(0f);
            }
            ItemBuffs.On(hero);
            _standings = new RaceStandings(_racers.Count, finishX);

            // Item boxes every ~20 tiles, on whatever is under them (ground or a high plank).
            int seed = 3;
            for (float x = _startX + 14f; x < finishX - 8f; x += 20f)
            {
                var hit = Physics2D.Raycast(new Vector2(x, 13f), Vector2.down, 14f, PKRLayers.GroundMask);
                if (hit.collider == null) continue;
                ItemBox.Spawn(this, hit.point + new Vector2(0f, 1.1f), seed++);
            }

            StoryHUD.HideRunStats = true;
            PartyHUD.Show("ITEM RACE", Line);
            LivePresence.Enter(PartyModes.Info(PartyMode.ItemRace).playerGroup);

            // 3-2-1-GO with everyone held at the line.
            var router = hero.GetComponent<PlayerInputRouter>();
            if (router != null) router.enabled = false;
            if (heroMotor != null) heroMotor.Intent.ClearAll();
            yield return new WaitForSeconds(0.8f);
            foreach (var n in new[] { "3", "2", "1" })
            {
                PartyHUD.Flash(n);
                Sfx.Play(Sfx.Click, 0.8f, 0f);
                yield return new WaitForSeconds(0.8f);
            }
            PartyHUD.Flash("GO!");
            Sfx.Play(Sfx.RoundStart, 0.9f, 0f);
            if (router != null) router.enabled = true;
            foreach (var b in _bots) b.Running = true;
            _running = true;
        }

        static CharacterDefinition PickHero(CharacterRoster roster, System.Random rng)
        {
            if (roster == null || roster.heroes.Count == 0) return null;
            var pick = new List<CharacterDefinition>();
            foreach (var h in roster.heroes) if (h != null && h.playableInThisBuild && h.id != GameSession.SelectedCharacterId) pick.Add(h);
            if (pick.Count == 0) foreach (var h in roster.heroes) if (h != null) pick.Add(h);
            return pick.Count > 0 ? pick[rng.Next(pick.Count)] : null;
        }

        string Line()
        {
            if (_standings == null) return "";
            int place = _standings.Place(0);
            string time = $"{(int)_clock / 60}:{_clock % 60f:00.0}";
            return _standings.IsFinished(0) ? $"FINISHED {RumblePoints.Ordinal(place).ToUpperInvariant()}   ·   {time}"
                                            : $"{RumblePoints.Ordinal(place).ToUpperInvariant()} / {_racers.Count}   ·   {time}   ·   {Mathf.RoundToInt(_standings.Progress01(0, _startX) * 100f)}%";
        }

        void Update()
        {
            if (!_running || _over || _standings == null) return;
            _clock += Time.deltaTime;
            var hero = _racers[0];
            for (int i = 0; i < _racers.Count; i++)
            {
                var r = _racers[i];
                if (r == null) continue;
                if (_standings.Report(i, r.transform.position.x))
                {
                    _times[i] = _clock;
                    if (i == 0)
                    {
                        PartyHUD.Flash($"FINISH! {RumblePoints.Ordinal(_standings.Place(0)).ToUpperInvariant()}");
                        Sfx.Play(Sfx.Victory, 0.9f, 0f);
                        var router = hero.GetComponent<PlayerInputRouter>();
                        if (router != null) router.enabled = false;
                        if (hero.TryGetComponent(out PlatformerMotor2D m)) m.Intent.ClearAll();
                        StartCoroutine(Finish());
                    }
                    else if (i - 1 < _bots.Count) _bots[i - 1].Running = false;
                }
            }
            // A little catch-up for the CPUs: slower when far ahead of you, quicker when far behind.
            for (int b = 0; b < _bots.Count; b++)
            {
                var go = _racers[b + 1];
                if (go == null) continue;
                float gap = go.transform.position.x - hero.transform.position.x;
                var buffs = go.GetComponent<ItemBuffs>();
                if (buffs != null) buffs.baseSpeed = gap > 14f ? 0.86f : gap < -14f ? 1.1f : 0.97f;
            }
        }

        IEnumerator Finish()
        {
            float wait = 0f;
            while (!_standings.AllFinished && wait < 6f)
            {
                wait += Time.deltaTime;
                yield return null;
            }
            _over = true;
            foreach (var b in _bots) if (b != null) b.Running = false;
            int place = _standings.Place(0);
            int shards = 0;
            var save = Services.Save;
            if (save != null)
            {
                shards = PartyResults.Record(save.Data, PartyMode.ItemRace, place, _times[0]);
                save.SaveNow();
            }
            ProgressTracker.MatchPlayed(place == 1, versus: false);
            ShowResults(place, shards);
        }

        void ShowResults(int place, int shards)
        {
            var root = UIFactory.CreateCanvas(transform, "RaceResults", 60, landscape: true, out _);
            UIFactory.Dim(root);
            var panel = UIFactory.Panel(root, new Vector2(1300f, 900f));
            UiMotion.ModalIn(root);
            var p = UITheme.Current;
            UIFactory.Label(panel, place == 1 ? "ITEM RACE  ·  YOU WIN!" : $"ITEM RACE  ·  {RumblePoints.Ordinal(place).ToUpperInvariant()} PLACE",
                            64, TextAnchor.MiddleCenter, 90f, title: true);
            var order = new List<int>();
            for (int i = 0; i < _racers.Count; i++) order.Add(i);
            order.Sort((a, b) => _standings.Place(a).CompareTo(_standings.Place(b)));
            foreach (int i in order)
            {
                string time = _standings.IsFinished(i) ? $"{(int)_times[i] / 60}:{_times[i] % 60f:00.0}" : "--";
                var row = UIFactory.Label(panel, $"{RumblePoints.Ordinal(_standings.Place(i)).ToUpperInvariant()}    {_names[i]}    {time}",
                                          40, TextAnchor.MiddleLeft, 56f);
                row.color = i == 0 ? (Color)p.title : (i < ArenaMatchController.SlotColors.Length ? ArenaMatchController.SlotColors[i] : p.text);
            }
            var rec = Services.Save != null ? Services.Save.Data.party.Find(PartyMode.ItemRace) : null;
            string best = rec != null && rec.bestRaceSeconds > 0f ? $"   ·   best {(int)rec.bestRaceSeconds / 60}:{rec.bestRaceSeconds % 60f:00.0}" : "";
            UIFactory.Label(panel, $"+{shards} Star Shards{best}", 38, TextAnchor.MiddleCenter, 56f).color = p.title;
            var buttons = UIFactory.Rect("Buttons", panel);
            buttons.gameObject.AddComponent<LayoutElement>().preferredHeight = UIFactory.RowHeight;
            var h = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 24f;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = true; h.childForceExpandHeight = true;
            var again = UIFactory.Button(buttons, "RACE AGAIN", () => { if (Services.Scenes != null) Services.Scenes.ReloadCurrent(); });
            UIFactory.Button(buttons, "BACK TO PLAZA", GameSession.ReturnToPlaza);
            UIFactory.Select(again);
        }

        // ---- IPartyHost -----------------------------------------------------------------------------------------

        public bool IsRacer(GameObject go) => go != null && _racers.Contains(go);

        public int PlaceOf(GameObject racer)
        {
            int i = _racers.IndexOf(racer);
            return i >= 0 && _standings != null ? _standings.Place(i) : 1;
        }

        public int RacerCount => _racers.Count;

        public IEnumerable<GameObject> Rivals(GameObject racer)
        {
            foreach (var r in _racers) if (r != null && r != racer && r.activeInHierarchy) yield return r;
        }

        public Material SpriteMaterial => _mat;

        public void OnItemHit(GameObject victim, GameObject attacker) { }

        void OnDestroy()
        {
            PartyHUD.Hide();
            StoryHUD.HideRunStats = false;
        }
    }
}
