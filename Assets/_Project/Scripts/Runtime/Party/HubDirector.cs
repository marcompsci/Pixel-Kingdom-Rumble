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
        RectTransform _card;
        Text _cardTitle, _cardInfo;
        Place _shown;
        float _nextRefresh;

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
            LivePresence.Watch(KingdomPlaza.PlazaGroup);
            LivePresence.Enter(KingdomPlaza.PlazaGroup);

            // Back from a quest: stand at its door.
            var hero = _flow != null ? _flow.Player : null;
            if (hero != null && GameSession.PlazaReturnX > 0f && hero.TryGetComponent(out PlatformerMotor2D motor))
            {
                var hit = Physics2D.Raycast(new Vector2(GameSession.PlazaReturnX + 0.5f, 10f), Vector2.down, 12f, PKRLayers.GroundMask);
                if (hit.collider != null) motor.Teleport(hit.point + new Vector2(0f, 0.9f));
            }
            GameSession.Party = PartyMode.None;
            GameSession.Mode = SessionMode.StoryQuest;

            PartyHUD.Show("KINGDOM PLAZA", Banner);
            BuildCard();
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
            return rec == null || rec.played == 0 ? mode : $"{mode}  ·  {Plural.Of(rec.wins, "WIN")}";
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
            _card.anchorMin = _card.anchorMax = new Vector2(0.5f, 0f);
            _card.pivot = new Vector2(0.5f, 0f);
            _card.anchoredPosition = new Vector2(0f, 230f);
            _card.sizeDelta = new Vector2(1000f, 250f);
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
            GameSession.Party = b.party;
            if (b.party == PartyMode.ItemRace)
            {
                if (Application.CanStreamedLevelBeLoaded(SceneIds.ItemRace)) Services.Scenes.Load(SceneIds.ItemRace);
                return;
            }
            GameSession.Mode = SessionMode.ArenaClash;
            if (Application.CanStreamedLevelBeLoaded(SceneIds.ArenaTest)) Services.Scenes.Load(SceneIds.ArenaTest);
        }

        void OnDestroy()
        {
            LivePresence.UnwatchAll();
            PartyHUD.Hide();
        }
    }
}
