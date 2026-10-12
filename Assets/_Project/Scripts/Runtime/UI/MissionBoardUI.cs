using System.Collections;
using System.Collections.Generic;
using PKR.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// Portrait Shadow Contracts board (05_ShadowContracts), one WORLD per page (< > to switch). Worlds open like doors:
    /// clearing every main contract of a world opens the next one, and the first visit plays a door-opening scene.
    /// The first contract is a free teaser; the rest need the one-time unlock ($3.99, with RESTORE PURCHASE). Secret
    /// contracts show as "???" until their cipher scroll is found. Tapping an open contract shows its briefing; START
    /// goes to Character Select and then into the contract.
    /// </summary>
    public class MissionBoardUI : MonoBehaviour
    {
        [SerializeField] MissionBoardDefinition board;
        public int sortingOrder = 10;

        RectTransform _root;
        ContractSlot[] _slots = new ContractSlot[0];
        ContractInfo[] _infos = new ContractInfo[0];
        readonly List<Button> _cards = new List<Button>();
        string _status = "";
        bool _busy;
        int _world;
        Coroutine _door;

        /// <summary>Cards on the current world page, in board order.</summary>
        public IReadOnlyList<Button> Cards => _cards;
        public ContractSlot[] Slots => _slots;
        public bool BriefingOpen { get; private set; }
        public bool DoorPlaying => _door != null;
        public int WorldPage => _world;

        public void SetBoard(MissionBoardDefinition b) => board = b;

        static readonly Color SecretColor = new Color32(124, 104, 214, 255);
        static readonly Color FreeColor = new Color32(84, 196, 120, 255);
        static readonly Color SealedColor = new Color32(70, 66, 84, 255);

        void Awake()
        {
            var safe = UIFactory.CreateCanvas(transform, "ContractsCanvas", sortingOrder, landscape: false, out _);
            _root = UIFactory.Rect("Contracts", safe);
            UIFactory.Stretch(_root);
        }

        void OnEnable()
        {
            EventBus<SettingsChanged>.Subscribe(OnSettings);
            PurchaseService.EntitlementsChanged += Build;
        }

        void OnDisable()
        {
            EventBus<SettingsChanged>.Unsubscribe(OnSettings);
            PurchaseService.EntitlementsChanged -= Build;
        }

        void OnSettings(SettingsChanged e) { if (_door == null) Build(); }

        void Start()
        {
            if (board == null || board.contracts.Count == 0)
            {
                Debug.LogError("[MissionBoardUI] No board assigned. Run PKR > Build All Scenes.");
                return;
            }
            var save = Services.Save != null ? Services.Save.Data : null;
            _infos = board.Infos();
            int worlds = Mathf.Max(1, MissionBoard.WorldCount(_infos));
            int pending = GameSession.PendingWorldDoor;
            GameSession.PendingWorldDoor = -1;
            if (pending > 0 && pending < worlds && MissionBoard.WorldOpen(_infos, save, pending))
            {
                _world = pending;
                PlayDoor(pending, openFirstContract: true);
                return;
            }
            _world = GameSession.ContractWorldPage >= 0 ? Mathf.Clamp(GameSession.ContractWorldPage, 0, worlds - 1) : FrontierWorld(save);
            Build();
            MaybePlayDoor();
        }

        /// <summary>The newest open world: where the player is heading.</summary>
        int FrontierWorld(SaveData save)
        {
            int worlds = MissionBoard.WorldCount(_infos), w = 0;
            for (int i = 1; i < worlds; i++) if (MissionBoard.WorldOpen(_infos, save, i)) w = i;
            return w;
        }

        void MaybePlayDoor()
        {
            var save = Services.Save != null ? Services.Save.Data : null;
            if (_world > 0 && save != null && MissionBoard.WorldOpen(_infos, save, _world) && !save.openedWorldDoors.Contains(_world))
                PlayDoor(_world, openFirstContract: false);
        }

        void ShowWorld(int w)
        {
            int worlds = Mathf.Max(1, MissionBoard.WorldCount(_infos));
            _world = (w % worlds + worlds) % worlds;
            GameSession.ContractWorldPage = _world;
            _status = "";
            Build();
            MaybePlayDoor();
        }

        void Build()
        {
            if (_root == null || board == null) return;
            BriefingOpen = false;
            var save = Services.Save != null ? Services.Save.Data : null;
            _infos = board.Infos();
            _slots = MissionBoard.Build(_infos, save);
            bool owned = save != null && save.contractsUnlocked;
            int worlds = Mathf.Max(1, MissionBoard.WorldCount(_infos));
            var world = board.World(_world);
            bool open = MissionBoard.WorldOpen(_infos, save, _world);
            var progress = MissionBoard.WorldProgress(_infos, save, _world);

            var page = new List<int>();
            for (int i = 0; i < _infos.Length; i++) if (_infos[i].world == _world) page.Add(i);

            UIFactory.Clear(_root);
            _cards.Clear();
            var p = UITheme.Current;
            float card = page.Count > 6 ? 112f : 150f; // eight contracts still fit without scrolling
            float height = 80f + 120f + 50f + 90f + 60f + (owned ? 0f : UIFactory.RowHeight + 70f + 36f) +
                           (string.IsNullOrEmpty(_status) ? 0f : 60f + 18f) + (open ? 0f : 140f + 18f) +
                           page.Count * (card + 18f) + UIFactory.RowHeight + 7 * 18f;
#if PKR_TESTER || UNITY_EDITOR
            if (!owned) height += 70f + 18f;
#endif
            var panel = UIFactory.Panel(_root, new Vector2(1000f, Mathf.Min(1840f, height)));
            UIFactory.Label(panel, board.title, 60, TextAnchor.MiddleCenter, 120f, title: true);
            int total = MissionBoard.TotalScore(save);
            UIFactory.Label(panel, $"{MissionBoard.ShadowTitle(total)}  ·  {total:N0} PTS", 34, TextAnchor.MiddleCenter, 50f).color = p.subtle;

            // World header: < WORLD 2 · FROST CITADEL >
            var row = UIFactory.Rect("WorldRow", panel);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 90f;
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 16f; h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandHeight = true;
            h.childForceExpandWidth = false;
            var prev = UIFactory.Button(row, "<", () => ShowWorld(_world - 1), 90f, interactable: worlds > 1);
            prev.GetComponent<LayoutElement>().preferredWidth = 110f;
            var title = UIFactory.Button(row, $"WORLD {_world + 1} · {world.name.ToUpperInvariant()}", null, 90f);
            title.GetComponent<LayoutElement>().flexibleWidth = 1f;
            title.GetComponentInChildren<Text>().fontSize = 34;
            UIFactory.Tint(title, open ? world.color : SealedColor, Color.white);
            var next = UIFactory.Button(row, ">", () => ShowWorld(_world + 1), 90f, interactable: worlds > 1);
            next.GetComponent<LayoutElement>().preferredWidth = 110f;
            UIFactory.Label(panel, open ? $"{progress.done}/{progress.total} CLEARED" + (string.IsNullOrEmpty(world.tagline) ? "" : "  ·  " + world.tagline)
                                        : "THE DOOR IS SEALED", 26, TextAnchor.MiddleCenter, 60f).color = p.subtle;

            if (!owned)
            {
                var buy = UIFactory.Button(panel, $"UNLOCK ALL CONTRACTS  ·  {PurchaseService.ContractsPrice}", Buy,
                                           interactable: !_busy);
                UIFactory.Tint(buy, p.title, p.buttonText);
                var restore = UIFactory.Button(panel, "RESTORE PURCHASE", Restore, 70f, interactable: !_busy);
                restore.GetComponentInChildren<Text>().fontSize = 30;
                UIFactory.Label(panel, "One-time purchase. First contract is free.", 28, TextAnchor.MiddleCenter, 36f).color = p.subtle;
#if PKR_TESTER || UNITY_EDITOR
                var tester = UIFactory.Button(panel, "TESTER BUILD: UNLOCK ALL", PurchaseService.Grant, 70f);
                tester.GetComponentInChildren<Text>().fontSize = 28;
#endif
            }
            if (!string.IsNullOrEmpty(_status))
                UIFactory.Label(panel, _status, 30, TextAnchor.MiddleCenter, 60f).color = p.accent;
            if (!open)
            {
                var before = board.World(_world - 1);
                var need = MissionBoard.WorldProgress(_infos, save, _world - 1);
                UIFactory.Label(panel, $"Clear every contract in {before.name} to open this door ({need.done}/{need.total}).",
                                32, TextAnchor.MiddleCenter, 140f).color = p.accent;
            }

            foreach (int i in page)
            {
                var c = board.contracts[i];
                var slot = _slots[i];
                int idx = i;
                bool playable = slot.Playable && c != null && c.level != null && Application.CanStreamedLevelBeLoaded(c.level.sceneName);
                bool tappable = playable || slot.state == ContractState.NeedsUnlock;
                var b = UIFactory.Button(panel, CardText(i, c, slot), () => Tap(idx), card, interactable: tappable);
                b.GetComponentInChildren<Text>().fontSize = card < 150f ? 30 : 32;
                if (playable)
                    UIFactory.Tint(b, c.kind == ContractKind.Secret ? SecretColor : c.free && !owned ? FreeColor : p.button,
                                   c.kind == ContractKind.Secret ? Color.white : p.buttonText);
                _cards.Add(b);
            }
            UIFactory.Button(panel, "BACK", () => { if (Services.Scenes != null) Services.Scenes.Load(SceneIds.MainMenu); });
            foreach (var b in _cards) if (b.interactable) { UIFactory.Select(b); break; }
        }

        static string CardText(int index, MissionDefinition c, ContractSlot slot)
        {
            if (c == null) return "???";
            string n = $"{index + 1}. {c.displayName.ToUpperInvariant()}";
            switch (slot.state)
            {
                case ContractState.Hidden:
                    return $"{index + 1}. ???  SECRET CONTRACT\n{(string.IsNullOrEmpty(c.clueHint) ? "FIND A CIPHER SCROLL TO REVEAL IT" : c.clueHint.ToUpperInvariant())}";
                case ContractState.WorldSealed:
                    return $"{n}\nBEHIND A SEALED DOOR";
                case ContractState.NeedsUnlock:
                    return $"{n}{(c.kind == ContractKind.Secret ? "  [SECRET]" : "")}\nLOCKED  ·  UNLOCK ALL CONTRACTS";
            }
            string tag = c.kind == ContractKind.Secret ? "  [SECRET]" : c.free ? "  [FREE]" : "";
            if (!slot.completed) return $"{n}{tag}\nNEW CONTRACT  ·  {c.totalRelics} RELICS HIDDEN";
            return $"{n}{tag}\nBEST {slot.bestScore:N0} PTS  ·  RELICS {slot.bestRelics}/{c.totalRelics}";
        }

        void Tap(int index)
        {
            if (index < 0 || index >= _slots.Length) return;
            if (_slots[index].state == ContractState.NeedsUnlock) { Buy(); return; }
            if (_slots[index].Playable) ShowBriefing(index);
        }

        void ShowBriefing(int index)
        {
            var c = board.contracts[index];
            UIFactory.Clear(_root);
            BriefingOpen = true;
            var p = UITheme.Current;
            UIFactory.Dim(_root);
            var panel = UIFactory.Panel(_root, new Vector2(960f, 1180f));
            UIFactory.Label(panel, c.displayName.ToUpperInvariant(), 60, TextAnchor.MiddleCenter, 110f, title: true);
            UIFactory.Label(panel, c.kind == ContractKind.Secret ? "SECRET CONTRACT" : $"WORLD {c.world + 1} · {board.World(c.world).name.ToUpperInvariant()}",
                            32, TextAnchor.MiddleCenter, 50f).color = p.subtle;
            UIFactory.Label(panel, c.briefing, 36, TextAnchor.UpperLeft, 330f);
            UIFactory.Label(panel, $"ANCIENT RELICS: {c.totalRelics}  ·  {MissionScore.RelicPoints} PTS EACH\n" +
                                   $"STAY UNSEEN: +{MissionScore.UnseenBonus} PTS  ·  TAKEDOWNS: +{MissionScore.TakedownPoints}",
                            30, TextAnchor.MiddleCenter, 100f).color = p.accent;
            var start = UIFactory.Button(panel, "START CONTRACT", () => Play(index));
            UIFactory.Button(panel, "BACK TO BOARD", Build);
            UIFactory.Select(start);
        }

        void Play(int index)
        {
            var c = board.contracts[index];
            if (c == null || c.level == null || Services.Scenes == null) return;
            GameSession.Mode = SessionMode.Contracts;
            GameSession.ContractScene = c.level.sceneName;
            GameSession.ContractWorldPage = c.world;
            Services.Scenes.Load(SceneIds.CharacterSelect);
        }

        // ---- The door: two halves in the world's color slide apart to reveal the new world ----------------------

        void PlayDoor(int world, bool openFirstContract)
        {
            if (_door != null) StopCoroutine(_door);
            _door = StartCoroutine(DoorRoutine(world, openFirstContract));
        }

        IEnumerator DoorRoutine(int worldIndex, bool openFirstContract)
        {
            var w = board.World(worldIndex);
            UIFactory.Clear(_root);
            var bg = UIFactory.Dim(_root);
            bg.color = new Color(0f, 0f, 0f, 0.92f);

            var reveal = UIFactory.Rect("Reveal", _root);
            reveal.anchorMin = new Vector2(0.05f, 0.3f); reveal.anchorMax = new Vector2(0.95f, 0.7f);
            reveal.offsetMin = reveal.offsetMax = Vector2.zero;
            var v = reveal.gameObject.AddComponent<VerticalLayoutGroup>();
            v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandHeight = false; v.spacing = 20f;
            v.childAlignment = TextAnchor.MiddleCenter;
            UIFactory.Label(reveal, "THE DOOR OPENS", 44, TextAnchor.MiddleCenter, 70f).color = UITheme.Current.subtle;
            var name = UIFactory.Label(reveal, $"WORLD {worldIndex + 1}\n{w.name.ToUpperInvariant()}", 92, TextAnchor.MiddleCenter, 260f, title: true);
            name.color = Color.Lerp(w.color, Color.white, 0.35f);
            UIFactory.Label(reveal, w.tagline, 36, TextAnchor.MiddleCenter, 160f);

            var left = DoorHalf("DoorLeft", new Vector2(0f, 0f), new Vector2(0.5f, 1f), w.color);
            var right = DoorHalf("DoorRight", new Vector2(0.5f, 0f), new Vector2(1f, 1f), w.color);
            Sfx.Play(Sfx.Secret, 1f, 0f);
            if (Services.Haptics != null) Services.Haptics.Play(HapticStrength.Heavy);

            yield return new WaitForSecondsRealtime(0.5f);
            float t = 0f;
            const float open = 1.4f;
            float width = ((RectTransform)_root).rect.width * 0.5f;
            while (t < open)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / open));
                left.anchoredPosition = new Vector2(-width * k, 0f);
                right.anchoredPosition = new Vector2(width * k, 0f);
                yield return null;
            }
            Destroy(left.gameObject); Destroy(right.gameObject);

            var save = Services.Save;
            if (save != null && save.Data.MarkWorldDoorOpened(worldIndex)) save.SaveNow();

            var go = UIFactory.Button(reveal, openFirstContract ? "ENTER" : "CONTINUE", null, UIFactory.RowHeight);
            UIFactory.Tint(go, w.color, Color.white);
            bool pressed = false;
            go.onClick.AddListener(() => pressed = true);
            UIFactory.Select(go);
            float waited = 0f;
            while (!pressed && waited < 6f) { waited += Time.unscaledDeltaTime; yield return null; }

            _door = null;
            GameSession.ContractWorldPage = worldIndex;
            _world = worldIndex;
            Build();
            if (openFirstContract)
            {
                for (int i = 0; i < _infos.Length; i++)
                    if (_infos[i].world == worldIndex && _infos[i].kind == ContractKind.Main && _slots[i].Playable) { ShowBriefing(i); break; }
            }
        }

        RectTransform DoorHalf(string name, Vector2 min, Vector2 max, Color color)
        {
            var rt = UIFactory.Rect(name, _root);
            rt.anchorMin = min; rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = rt.gameObject.AddComponent<Image>();
            img.color = Color.Lerp(color, Color.black, 0.35f);
            // A bright seam and a few rivets so it reads as a heavy door.
            var seam = UIFactory.Rect("Seam", rt);
            bool isLeft = min.x < 0.25f;
            seam.anchorMin = new Vector2(isLeft ? 0.97f : 0f, 0f); seam.anchorMax = new Vector2(isLeft ? 1f : 0.03f, 1f);
            seam.offsetMin = seam.offsetMax = Vector2.zero;
            seam.gameObject.AddComponent<Image>().color = Color.Lerp(color, Color.white, 0.5f);
            for (int i = 1; i <= 5; i++)
            {
                var rivet = UIFactory.Rect("Rivet", rt);
                rivet.anchorMin = rivet.anchorMax = new Vector2(isLeft ? 0.85f : 0.15f, i / 6f);
                rivet.sizeDelta = new Vector2(36f, 36f);
                rivet.gameObject.AddComponent<Image>().color = Color.Lerp(color, Color.white, 0.25f);
            }
            return rt;
        }

        void Buy()
        {
            if (_busy) return;
            _busy = true;
            _status = "Contacting the App Store...";
            Build();
            PurchaseService.BuyContracts((ok, msg) =>
            {
                _busy = false;
                _status = ok ? "Shadow Contracts unlocked. Good hunting." : msg;
                Build();
            });
        }

        void Restore()
        {
            if (_busy) return;
            _busy = true;
            _status = "Restoring...";
            Build();
            PurchaseService.Restore((ok, msg) =>
            {
                _busy = false;
                _status = msg;
                Build();
            });
        }
    }
}
