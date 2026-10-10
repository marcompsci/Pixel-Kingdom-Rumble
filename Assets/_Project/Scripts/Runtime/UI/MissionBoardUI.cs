using System.Collections.Generic;
using PKR.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// Portrait Shadow Contracts board (05_ShadowContracts). Lists the contracts with best score and relics; the first
    /// is a free teaser, the rest need the one-time unlock ($3.99, with RESTORE PURCHASE). Secret contracts show as
    /// "???" until their cipher scroll is found. Tapping an open contract shows its briefing; START goes to
    /// Character Select and then into the contract.
    /// </summary>
    public class MissionBoardUI : MonoBehaviour
    {
        [SerializeField] MissionBoardDefinition board;
        public int sortingOrder = 10;

        RectTransform _root;
        ContractSlot[] _slots = new ContractSlot[0];
        readonly List<Button> _cards = new List<Button>();
        string _status = "";
        bool _busy;

        public IReadOnlyList<Button> Cards => _cards;
        public ContractSlot[] Slots => _slots;
        public bool BriefingOpen { get; private set; }

        public void SetBoard(MissionBoardDefinition b) => board = b;

        static readonly Color SecretColor = new Color32(124, 104, 214, 255);
        static readonly Color FreeColor = new Color32(84, 196, 120, 255);

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

        void OnSettings(SettingsChanged e) => Build();

        void Start()
        {
            if (board == null || board.contracts.Count == 0)
            {
                Debug.LogError("[MissionBoardUI] No board assigned. Run PKR > Build All Scenes.");
                return;
            }
            Build();
        }

        void Build()
        {
            if (_root == null || board == null) return;
            BriefingOpen = false;
            var save = Services.Save != null ? Services.Save.Data : null;
            _slots = MissionBoard.Build(board.Infos(), save);
            bool owned = save != null && save.contractsUnlocked;

            UIFactory.Clear(_root);
            _cards.Clear();
            var p = UITheme.Current;
            const float card = 150f;
            float height = 80f + 130f + 60f + (owned ? 0f : UIFactory.RowHeight + 70f + 36f) + (string.IsNullOrEmpty(_status) ? 0f : 60f + 18f) +
                           board.contracts.Count * (card + 18f) + UIFactory.RowHeight + 5 * 18f;
#if PKR_TESTER || UNITY_EDITOR
            if (!owned) height += 70f + 18f;
#endif
            var panel = UIFactory.Panel(_root, new Vector2(1000f, Mathf.Min(1840f, height)));
            UIFactory.Label(panel, board.title, 64, TextAnchor.MiddleCenter, 130f, title: true);
            int total = MissionBoard.TotalScore(save);
            UIFactory.Label(panel, $"{MissionBoard.ShadowTitle(total)}  ·  {total:N0} PTS", 36, TextAnchor.MiddleCenter, 60f).color = p.subtle;

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

            for (int i = 0; i < board.contracts.Count; i++)
            {
                var c = board.contracts[i];
                var slot = _slots[i];
                int idx = i;
                bool playable = slot.Playable && c != null && c.level != null && Application.CanStreamedLevelBeLoaded(c.level.sceneName);
                bool tappable = playable || slot.state == ContractState.NeedsUnlock;
                var b = UIFactory.Button(panel, CardText(i, c, slot), () => Tap(idx), card, interactable: tappable);
                var t = b.GetComponentInChildren<Text>();
                t.fontSize = 32;
                if (playable)
                    UIFactory.Tint(b, c.kind == ContractKind.Secret ? SecretColor : c.free && !owned ? FreeColor : p.button,
                                   c.kind == ContractKind.Secret ? Color.white : p.buttonText);
                _cards.Add(b);
            }
            UIFactory.Button(panel, "BACK", () => { if (Services.Scenes != null) Services.Scenes.Load(SceneIds.MainMenu); });
            if (_cards.Count > 0 && _cards[0].interactable) UIFactory.Select(_cards[0]);
        }

        static string CardText(int index, MissionDefinition c, ContractSlot slot)
        {
            if (c == null) return "???";
            string n = $"{index + 1}. {c.displayName.ToUpperInvariant()}";
            switch (slot.state)
            {
                case ContractState.Hidden:
                    return $"{index + 1}. ???  SECRET CONTRACT\n{(string.IsNullOrEmpty(c.clueHint) ? "FIND A CIPHER SCROLL TO REVEAL IT" : c.clueHint.ToUpperInvariant())}";
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
            UIFactory.Label(panel, c.kind == ContractKind.Secret ? "SECRET CONTRACT" : "CONTRACT " + (index + 1), 32,
                            TextAnchor.MiddleCenter, 50f).color = p.subtle;
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
            Services.Scenes.Load(SceneIds.CharacterSelect);
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
