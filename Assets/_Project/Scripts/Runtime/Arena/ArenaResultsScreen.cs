using System.Collections;
using PKR.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>Arena results: winner, standings with KOs/falls/score, Star Shard reward; Rematch / Change Rules / Menu.</summary>
    public class ArenaResultsScreen : MonoBehaviour
    {
        [SerializeField] ArenaMatchController match;
        [SerializeField] ArenaSetupUI setup;
        public int sortingOrder = 250;
        public float showDelay = 1.2f;

        RectTransform _root;

        void Awake()
        {
            if (match == null) match = FindFirstObjectByType<ArenaMatchController>();
            if (setup == null) setup = FindFirstObjectByType<ArenaSetupUI>();
            var safe = UIFactory.CreateCanvas(transform, "ArenaResultsCanvas", sortingOrder, landscape: true, out _);
            _root = UIFactory.Rect("Results", safe);
            UIFactory.Stretch(_root);
            _root.gameObject.SetActive(false);
        }

        void OnEnable() => EventBus<ArenaMatchEnded>.Subscribe(OnEnded);
        void OnDisable() => EventBus<ArenaMatchEnded>.Unsubscribe(OnEnded);
        void OnEnded(ArenaMatchEnded e) => StartCoroutine(Show(e));

        IEnumerator Show(ArenaMatchEnded e)
        {
            yield return new WaitForSecondsRealtime(showDelay);
            _root.gameObject.SetActive(true);
            UIFactory.Clear(_root);
            UIFactory.Dim(_root);
            var panel = UIFactory.Panel(_root, new Vector2(1300f, 940f));
            var p = UITheme.Current;

            int winner = System.Array.IndexOf(e.placements, 1);
            string title = e.playerPlacement == 1 ? "YOU WIN!" : (winner >= 0 ? $"{e.names[winner]} WINS" : "MATCH OVER");
            UIFactory.Label(panel, title, 80, TextAnchor.MiddleCenter, 110f, title: true);

            // Standings, best placement first.
            var order = new int[e.placements.Length];
            for (int i = 0; i < order.Length; i++) order[i] = i;
            System.Array.Sort(order, (a, b) => e.placements[a].CompareTo(e.placements[b]));
            foreach (int slot in order)
            {
                string score = e.mode == MatchMode.Timed ? $"   score {e.scores[slot]}" : "";
                var row = UIFactory.Label(panel, $"{Ordinal(e.placements[slot])}   {e.names[slot]}   KOs {e.knockouts[slot]}   falls {e.falls[slot]}{score}",
                                          40, TextAnchor.MiddleLeft, 64f);
                row.color = slot < ArenaMatchController.SlotColors.Length ? ArenaMatchController.SlotColors[slot] : p.text;
            }
            if (e.mode != MatchMode.Training)
            {
                int total = Services.Save != null ? Services.Save.Data.starShards : 0;
                UIFactory.Label(panel, $"+{e.starShardReward} Star Shards   (total {total})", 44, TextAnchor.MiddleCenter, 70f).color = p.title;
            }

            var buttons = UIFactory.Rect("Buttons", panel);
            buttons.gameObject.AddComponent<LayoutElement>().preferredHeight = UIFactory.RowHeight;
            var h = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 24f;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = true; h.childForceExpandHeight = true;
            var rematch = UIFactory.Button(buttons, "REMATCH", () =>
            {
                _root.gameObject.SetActive(false);
                if (match != null) match.StartMatch(match.Config);
            });
            UIFactory.Button(buttons, "CHANGE RULES", () =>
            {
                _root.gameObject.SetActive(false);
                if (setup != null) setup.Show();
            });
            UIFactory.Button(buttons, "MAIN MENU", () => { if (Services.Scenes != null) Services.Scenes.Load(SceneIds.MainMenu); },
                             interactable: Application.CanStreamedLevelBeLoaded(SceneIds.MainMenu));
            UIFactory.Select(rematch);
            if (Services.Haptics != null) Services.Haptics.Play(HapticStrength.Heavy);
        }

        static string Ordinal(int n) => n == 1 ? "1st" : n == 2 ? "2nd" : n == 3 ? "3rd" : n + "th";
    }
}
