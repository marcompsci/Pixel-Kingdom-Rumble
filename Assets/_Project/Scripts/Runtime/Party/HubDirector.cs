using System.Collections.Generic;
using PKR.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// KINGDOM PLAZA: Story Quest's roamable world. Walk the street between the quest buildings (the four Story worlds
    /// and the five party quests). Above each building: its name, your progress and how many players are live in it;
    /// the banner shows how many are live in the whole kingdom. Live numbers come from Game Center (LivePresence) and
    /// read "--" when they can't be known. Stand at a door to see the quest and ENTER.
    /// </summary>
    public class HubDirector : MonoBehaviour
    {
        class Place
        {
            public HubBuilding b;
            public float x, groundY;
            public Text count, progress;
        }

        LevelFlowController _flow;
        readonly List<Place> _places = new List<Place>();
        RectTransform _card, _boardCard, _mapPanel;
        Text _cardTitle, _cardInfo, _boardDaily, _diffLabel, _cupLabel, _autoLabel;
        Place _shown;
        bool _nearBoard;
        float _nextRefresh, _boardX = -1f, _boardGround;

        /// <summary>The notice board stands just right of where you arrive (difficulty, daily challenge, Kingdom Cup).</summary>
        public const float BoardX = 7f;

        static readonly string[] TownTips =
        {
            "The notice board has today's\nparty challenge!",
            "Win the Kingdom Cup for a\nshiny trophy.",
            "Behind in a race? Item boxes\nhelp the back of the pack.",
            "Tap MAP to jump to any door.",
            "Beat your best Item Race time\nto race your own ghost.",
            "Hard CPUs pay more Star Shards.",
            "Stand ALONE in the crown zone\nto score.",
        };

        void Start()
        {
            _flow = GetComponent<LevelFlowController>();
            var goal = FindAnyObjectByType<GoalGate>();
            if (goal != null) goal.gameObject.SetActive(false); // the plaza has no finish line

            var mat = _flow != null ? _flow.SpriteMaterial : null;
            var root = new GameObject("Plaza").transform;
            for (int i = 0; i < KingdomPlaza.Buildings.Length; i++)
            {
                var b = KingdomPlaza.Buildings[i];
                var hit = Physics2D.Raycast(new Vector2(b.x + 0.5f, 10f), Vector2.down, 12f, PKRLayers.GroundMask);
                float ground = hit.collider != null ? hit.point.y : 2f;
                var place = new Place { b = b, x = b.x + 0.5f, groundY = ground };
                var sr = new GameObject("Building_" + b.id).AddComponent<SpriteRenderer>();
                sr.transform.SetParent(root, false);
                sr.transform.position = new Vector3(place.x, ground - 0.05f, 0f);
                sr.sprite = b.kind == HubKind.World
                    ? PlazaArt.Building(b.id, PlazaArt.WorldColours[Mathf.Clamp(b.world, 0, PlazaArt.WorldColours.Length - 1)], false)
                    : PlazaArt.Building(b.id, PlazaArt.PartyColours[Mathf.Clamp((int)b.party - 1, 0, PlazaArt.PartyColours.Length - 1)], true);
                sr.sortingOrder = -20; // behind the hero and the street
                if (mat != null) sr.sharedMaterial = mat;
                Sign(place, root);
                _places.Add(place);
                LivePresence.Watch(b.playerGroup);
            }
            BuildBoard(root, mat);
            var heroT = _flow != null && _flow.Player != null ? _flow.Player.transform : null;
            float[] folkX = { 19f, 47f, 75f, 103f, 131f };
            for (int i = 0; i < folkX.Length; i++)
            {
                var hit = Physics2D.Raycast(new Vector2(folkX[i], 10f), Vector2.down, 12f, PKRLayers.GroundMask);
                if (hit.collider == null) continue;
                Townsfolk.Spawn(root, i, folkX[i], hit.point.y, 3.5f, heroT, mat, TownTips);
            }
            LivePresence.Watch(KingdomPlaza.PlazaGroup);
            LivePresence.Enter(KingdomPlaza.PlazaGroup, _flow != null ? (Object)_flow : this);

            // Back from a quest: stand at its door.
            var hero = _flow != null ? _flow.Player : null;
            if (hero != null && GameSession.PlazaReturnX > 0f && hero.TryGetComponent(out PlatformerMotor2D motor))
            {
                var hit = Physics2D.Raycast(new Vector2(GameSession.PlazaReturnX + 0.5f, 10f), Vector2.down, 12f, PKRLayers.GroundMask);
                if (hit.collider != null) motor.Teleport(hit.point + new Vector2(0f, 0.9f));
            }
            GameSession.Party = PartyMode.None;
            GameSession.Mode = SessionMode.StoryQuest;

            StoryHUD.HideRunStats = true;
            MetaTracker.Played(ContinueKind.Plaza);
            PartyHUD.Show("KINGDOM PLAZA", Banner);
            BuildCard();
            BuildBoardCard();
            BuildMap();
            // Screenshot runs: "SQ_KingdomPlaza@board" stands at the notice board, "@map" opens the map.
            if (BootLoader.TakeShotPanel("board") != null && hero != null && hero.TryGetComponent(out PlatformerMotor2D bm))
                bm.Teleport(new Vector2(BoardX + 0.5f, _boardGround + 0.9f));
            if (BootLoader.TakeShotPanel("map") != null) ToggleMap();
            Refresh();
        }

        string Banner()
        {
            var counts = new List<int?> { LivePresence.Count(KingdomPlaza.PlazaGroup) };
            foreach (var p in _places) counts.Add(LivePresence.Count(p.b.playerGroup));
            var total = LiveCount.Total(counts);
            string live = total.HasValue ? $"{LiveCount.Label(total)} IN THE KINGDOM" : "LIVE PLAYERS: SIGN IN TO GAME CENTER";
            return live + "   ·   WALK TO A DOOR TO ENTER";
        }

        // ---- signs over the buildings ---------------------------------------------------------------------------

        void Sign(Place place, Transform root)
        {
            var go = new GameObject("Sign_" + place.b.id);
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(place.x, place.groundY + 6.1f, 0f);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 60;
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(560f, 190f);
            rt.localScale = Vector3.one * 0.01f;
            var bg = go.AddComponent<Image>();
            UISprites.ApplyPanelSprite(bg);
            bg.color = new Color(0.1f, 0.08f, 0.14f, 0.88f);
            bg.raycastTarget = false;
            Line(rt, place.b.name.ToUpperInvariant(), 44, 0.62f, 1f, UITheme.Current.title);
            place.progress = Line(rt, "", 28, 0.36f, 0.64f, new Color(1f, 1f, 1f, 0.85f));
            place.count = Line(rt, "", 32, 0.04f, 0.38f, new Color32(120, 240, 140, 255));
        }

        static Text Line(RectTransform parent, string text, int size, float yMin, float yMax, Color color)
        {
            var r = UIFactory.Rect("Line", parent);
            r.anchorMin = new Vector2(0f, yMin);
            r.anchorMax = new Vector2(1f, yMax);
            r.offsetMin = new Vector2(12f, 0f);
            r.offsetMax = new Vector2(-12f, 0f);
            var t = r.gameObject.AddComponent<Text>();
            t.font = UISprites.HasDisplayFont ? UISprites.DisplayFont : UISprites.Font;
            t.fontSize = size;
            t.alignment = TextAnchor.MiddleCenter;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 14;
            t.resizeTextMaxSize = size;
            t.color = color;
            t.text = text;
            t.raycastTarget = false;
            return t;
        }

        string Progress(HubBuilding b)
        {
            var save = Services.Save != null ? Services.Save.Data : null;
            if (b.kind == HubKind.World)
            {
                if (b.world < 0 || b.world >= StoryWorlds.All.Length) return "";
                var levels = StoryWorlds.All[b.world].levels;
                int done = 0;
                if (save != null) foreach (var id in levels) { var r = save.GetLevel(id); if (r != null && r.completed) done++; }
                return $"STORY WORLD {b.world + 1}  ·  {done}/{levels.Length} CLEARED";
            }
            var rec = save != null && save.party != null ? save.party.Find(b.party) : null;
            string mode = PartyModes.Info(b.party).name;
            var daily = DailyParty.For(PartyFlow.Today);
            string star = daily.mode == b.party && (save == null || save.party == null || save.party.dailyDoneDay != PartyFlow.Today) ? "DAILY!  ·  " : "";
            if (rec == null || rec.played == 0) return star + mode;
            return $"{star}{Plural.Of(rec.wins, "WIN")}  ·  {PartyRanks.Name(PartyRanks.For(rec.rankPoints))}" + BestText(b.party, rec);
        }

        /// <summary>The quest's personal best: race time (and medal), coins, crown seconds or balloons left.</summary>
        static string BestText(PartyMode m, PartyRecord rec)
        {
            switch (m)
            {
                case PartyMode.ItemRace:
                    if (rec.bestRaceSeconds <= 0f) return "";
                    var medal = RaceMedals.For(rec.bestRaceSeconds);
                    return $"  ·  BEST {(int)rec.bestRaceSeconds / 60}:{rec.bestRaceSeconds % 60f:00.0}" + (medal != TimeMedal.None ? " " + StoryMedals.Name(medal) : "");
                case PartyMode.CoinRush: return rec.bestScore > 0 ? $"  ·  BEST {rec.bestScore} COINS" : "";
                case PartyMode.KingOfTheHill: return rec.bestScore > 0 ? $"  ·  BEST {rec.bestScore}s" : "";
                case PartyMode.BalloonBattle: return rec.bestScore > 0 ? $"  ·  BEST {rec.bestScore} LEFT" : "";
                default: return "";
            }
        }

        void Refresh()
        {
            foreach (var p in _places)
            {
                p.progress.text = Progress(p.b);
                p.count.text = LiveCount.Label(LivePresence.Count(p.b.playerGroup));
            }
        }

        // ---- the enter card -------------------------------------------------------------------------------------

        void BuildCard()
        {
            var safe = UIFactory.CreateCanvas(transform, "PlazaCard", 45, landscape: true, out _);
            _card = UIFactory.Rect("Card", safe);
            // Middle of the screen, clear of the move stick (bottom left) and the action buttons (bottom right).
            _card.anchorMin = _card.anchorMax = new Vector2(0.5f, 0.56f);
            _card.pivot = new Vector2(0.5f, 0.5f);
            _card.anchoredPosition = Vector2.zero;
            _card.sizeDelta = new Vector2(900f, 250f);
            var bg = _card.gameObject.AddComponent<Image>();
            UISprites.ApplyPanelSprite(bg);
            bg.color = new Color(0.1f, 0.08f, 0.14f, 0.94f);
            _cardTitle = Line(_card, "", 46, 0.66f, 0.96f, UITheme.Current.title);
            _cardInfo = Line(_card, "", 28, 0.4f, 0.68f, Color.white);
            var btnHolder = UIFactory.Rect("Enter", _card);
            btnHolder.anchorMin = new Vector2(0.3f, 0.06f);
            btnHolder.anchorMax = new Vector2(0.7f, 0.38f);
            btnHolder.offsetMin = btnHolder.offsetMax = Vector2.zero;
            var enter = UIFactory.Button(btnHolder, "ENTER", () => { if (_shown != null) Enter(_shown.b); });
            var ert = (RectTransform)enter.transform;
            ert.anchorMin = Vector2.zero; ert.anchorMax = Vector2.one;
            ert.offsetMin = ert.offsetMax = Vector2.zero;
            UIFactory.Tint(enter, new Color32(90, 190, 110, 255), Color.white);
            _card.gameObject.SetActive(false);
        }

        void Update()
        {
            if (Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + 1f;
                Refresh();
            }
            var hero = _flow != null ? _flow.Player : null;
            if (hero == null) return;
            Vector2 h = hero.transform.position;
            bool board = _boardX > 0f && Mathf.Abs(h.x - _boardX) <= 1.6f && h.y >= _boardGround - 0.5f && h.y <= _boardGround + 3.5f;
            if (board != _nearBoard)
            {
                _nearBoard = board;
                _boardCard.gameObject.SetActive(board);
                if (board) { RefreshBoard(); UiMotion.Enter(_boardCard, PKR.Core.Ease.Enter, 0.97f, new Vector2(0f, -16f)); }
            }
            Place near = null;
            foreach (var p in _places)
                if (Mathf.Abs(h.x - p.x) <= 1.4f && h.y >= p.groundY - 0.5f && h.y <= p.groundY + 3.5f) near = p;
            if (near == _shown) return;
            _shown = near;
            if (near == null)
            {
                _card.gameObject.SetActive(false);
                return;
            }
            _cardTitle.text = near.b.name.ToUpperInvariant();
            _cardInfo.text = near.b.kind == HubKind.World
                ? $"{Progress(near.b)}   ·   {LiveCount.Label(LivePresence.Count(near.b.playerGroup))}"
                : $"{PartyModes.Info(near.b.party).blurb}   ·   {LiveCount.Label(LivePresence.Count(near.b.playerGroup))}";
            _card.gameObject.SetActive(true);
            UiMotion.Enter(_card, PKR.Core.Ease.Enter, 0.97f, new Vector2(0f, -16f));
        }

        void Enter(HubBuilding b)
        {
            if (Services.Scenes == null) return;
            GameSession.PlazaReturnX = b.x;
            if (b.kind == HubKind.World)
            {
                GameSession.Mode = SessionMode.StoryQuest;
                GameSession.PlazaWorld = b.world;
                Services.Scenes.Load(SceneIds.LevelSelect);
                return;
            }
            // A single quest from its own door isn't part of a cup run.
            if (PartyFlow.CupActive) PartyFlow.QuitCup();
            GameSession.StartPartyQuest(b.party);
        }

        // ---- the notice board -----------------------------------------------------------------------------------

        void BuildBoard(Transform root, Material mat)
        {
            var hit = Physics2D.Raycast(new Vector2(BoardX + 0.5f, 10f), Vector2.down, 12f, PKRLayers.GroundMask);
            if (hit.collider == null) return;
            _boardX = BoardX + 0.5f;
            _boardGround = hit.point.y;
            var sr = new GameObject("NoticeBoard").AddComponent<SpriteRenderer>();
            sr.transform.SetParent(root, false);
            sr.transform.position = new Vector3(_boardX, _boardGround - 0.05f, 0f);
            sr.sprite = PlazaArt.NoticeBoard();
            sr.sortingOrder = -10;
            if (mat != null) sr.sharedMaterial = mat;
        }

        void BuildBoardCard()
        {
            var safe = UIFactory.CreateCanvas(transform, "BoardCard", 46, landscape: true, out _);
            _boardCard = UIFactory.Rect("Board", safe);
            _boardCard.anchorMin = _boardCard.anchorMax = new Vector2(0.5f, 0.55f);
            _boardCard.pivot = new Vector2(0.5f, 0.5f);
            _boardCard.anchoredPosition = Vector2.zero;
            _boardCard.sizeDelta = new Vector2(920f, 330f);
            var bg = _boardCard.gameObject.AddComponent<Image>();
            UISprites.ApplyPanelSprite(bg);
            bg.color = new Color(0.1f, 0.08f, 0.14f, 0.95f);
            Line(_boardCard, "NOTICE BOARD", 44, 0.78f, 0.98f, UITheme.Current.title);
            _boardDaily = Line(_boardCard, "", 26, 0.5f, 0.78f, Color.white);
            var row = UIFactory.Rect("Buttons", _boardCard);
            row.anchorMin = new Vector2(0.03f, 0.06f);
            row.anchorMax = new Vector2(0.97f, 0.46f);
            row.offsetMin = row.offsetMax = Vector2.zero;
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 18f;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = true; h.childForceExpandHeight = true;
            var diff = UIFactory.Button(row, "", () =>
            {
                var party = PartyFlow.Party;
                if (party == null) return;
                party.difficulty = (int)PartyDifficulties.Next(party.Difficulty);
                if (Services.Save != null) Services.Save.SaveNow();
                RefreshBoard();
            });
            _diffLabel = diff.GetComponentInChildren<Text>();
            var cup = UIFactory.Button(row, "", () =>
            {
                var party = PartyFlow.Party;
                if (party == null) return;
                if (PartyFlow.CupActive) GameSession.StartPartyQuest(party.cup.CurrentEvent);
                else PartyFlow.StartCup();
            });
            _cupLabel = cup.GetComponentInChildren<Text>();
            UIFactory.Tint(cup, new Color32(240, 190, 60, 255), PlazaArt.Ink);
            var auto = UIFactory.Button(row, "", () =>
            {
                var party = PartyFlow.Party;
                if (party == null) return;
                party.autoRun = !party.autoRun;
                if (Services.Save != null) Services.Save.SaveNow();
                RefreshBoard();
            });
            _autoLabel = auto.GetComponentInChildren<Text>();
            _boardCard.gameObject.SetActive(false);
        }

        void RefreshBoard()
        {
            var party = PartyFlow.Party;
            int today = PartyFlow.Today;
            var daily = DailyParty.For(today);
            bool done = party != null && party.dailyDoneDay == today;
            int streak = party != null ? party.dailyStreak : 0;
            _boardDaily.text = done
                ? $"TODAY'S CHALLENGE DONE!  ·  {Plural.Of(streak, "day")} in a row  ·  come back tomorrow"
                : $"TODAY: {daily.text.ToUpperInvariant()}  ·  +{daily.reward} STAR SHARDS" + (streak > 0 ? $"  ·  STREAK {streak}" : "");
            var d = PartyFlow.Difficulty;
            if (_diffLabel != null) _diffLabel.text = "CPUs: " + PartyDifficulties.Name(d);
            if (_cupLabel != null)
            {
                _cupLabel.text = PartyFlow.CupActive
                    ? $"CONTINUE CUP ({party.cup.eventIndex + 1}/{KingdomCup.Events.Length})"
                    : (party != null && party.BestTrophy(d) != CupTrophy.None ? $"KINGDOM CUP · {KingdomCup.TrophyName(party.BestTrophy(d))}" : "KINGDOM CUP");
            }
            if (_autoLabel != null) _autoLabel.text = "AUTO-RUN: " + (party != null && party.autoRun ? "ON" : "OFF");
        }

        // ---- fast travel ----------------------------------------------------------------------------------------

        void BuildMap()
        {
            var safe = UIFactory.CreateCanvas(transform, "PlazaMap", 47, landscape: true, out _);
            var btnHolder = UIFactory.Rect("MapButton", safe);
            btnHolder.anchorMin = btnHolder.anchorMax = new Vector2(0f, 1f);
            btnHolder.pivot = new Vector2(0f, 1f);
            btnHolder.anchoredPosition = new Vector2(40f, -100f);
            btnHolder.sizeDelta = new Vector2(190f, 84f);
            var open = UIFactory.Button(btnHolder, "MAP", ToggleMap, 84f);
            var ort = (RectTransform)open.transform;
            ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
            ort.offsetMin = ort.offsetMax = Vector2.zero;

            _mapPanel = UIFactory.Rect("Map", safe);
            _mapPanel.anchorMin = _mapPanel.anchorMax = new Vector2(0.5f, 0.5f);
            _mapPanel.sizeDelta = new Vector2(1500f, 520f);
            var bg = _mapPanel.gameObject.AddComponent<Image>();
            UISprites.ApplyPanelSprite(bg);
            bg.color = new Color(0.1f, 0.08f, 0.14f, 0.97f);
            Line(_mapPanel, "KINGDOM PLAZA  ·  TAP A PLACE TO GO THERE", 40, 0.84f, 0.97f, UITheme.Current.title);
            var grid = UIFactory.Rect("Places", _mapPanel);
            grid.anchorMin = new Vector2(0.03f, 0.2f);
            grid.anchorMax = new Vector2(0.97f, 0.82f);
            grid.offsetMin = grid.offsetMax = Vector2.zero;
            var g = grid.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(266f, 130f);
            g.spacing = new Vector2(14f, 14f);
            g.childAlignment = TextAnchor.MiddleCenter;
            AddMapButton(grid, "NOTICE BOARD", BoardX, new Color32(150, 98, 56, 255));
            foreach (var b in KingdomPlaza.Buildings)
            {
                var col = b.kind == HubKind.World
                    ? PlazaArt.WorldColours[Mathf.Clamp(b.world, 0, PlazaArt.WorldColours.Length - 1)]
                    : PlazaArt.PartyColours[Mathf.Clamp((int)b.party - 1, 0, PlazaArt.PartyColours.Length - 1)];
                AddMapButton(grid, b.name.ToUpperInvariant(), b.x, col);
            }
            var closeHolder = UIFactory.Rect("Close", _mapPanel);
            closeHolder.anchorMin = new Vector2(0.38f, 0.03f);
            closeHolder.anchorMax = new Vector2(0.62f, 0.17f);
            closeHolder.offsetMin = closeHolder.offsetMax = Vector2.zero;
            var close = UIFactory.Button(closeHolder, "CLOSE", ToggleMap, 70f);
            var crt = (RectTransform)close.transform;
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
            crt.offsetMin = crt.offsetMax = Vector2.zero;
            _mapPanel.gameObject.SetActive(false);
        }

        void AddMapButton(RectTransform grid, string label, float x, Color32 colour)
        {
            var b = UIFactory.Button(grid, label, () => TravelTo(x));
            UIFactory.Tint(b, colour, PlazaArt.Ink);
            var t = b.GetComponentInChildren<Text>();
            if (t != null) { t.resizeTextForBestFit = true; t.resizeTextMinSize = 16; t.resizeTextMaxSize = 30; }
        }

        void ToggleMap()
        {
            bool open = !_mapPanel.gameObject.activeSelf;
            _mapPanel.gameObject.SetActive(open);
            if (open) UiMotion.ModalIn(_mapPanel);
        }

        void TravelTo(float x)
        {
            _mapPanel.gameObject.SetActive(false);
            var hero = _flow != null ? _flow.Player : null;
            if (hero == null || !hero.TryGetComponent(out PlatformerMotor2D motor)) return;
            var hit = Physics2D.Raycast(new Vector2(x + 0.5f, 10f), Vector2.down, 12f, PKRLayers.GroundMask);
            if (hit.collider == null) return;
            PuffEffect.Play(hero.transform.position, new Color32(255, 240, 200, 255), _flow.SpriteMaterial, 0.3f, 5f, 0.4f);
            motor.Teleport(hit.point + new Vector2(0f, 0.9f));
            PuffEffect.Play(hit.point + Vector2.up, new Color32(255, 240, 200, 255), _flow.SpriteMaterial, 0.3f, 5f, 0.4f);
            Sfx.Play(Sfx.Spring, 0.6f, 0f);
        }

        void OnDestroy()
        {
            LivePresence.UnwatchAll();
            PartyHUD.Hide();
            StoryHUD.HideRunStats = false;
        }
    }
}
