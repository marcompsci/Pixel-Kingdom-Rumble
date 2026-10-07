using System.Collections;
using PKR.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// Shown after the goal: rank, time vs par (with NEW BEST), shards, secrets, falls, and the Star Shard
    /// reward counting up into the saved total. Buttons: Play Again, Levels (Main Menu if there is no level select), Next (when available).
    /// </summary>
    public class LevelCompleteScreen : MonoBehaviour
    {
        public int sortingOrder = 250;
        [Tooltip("Pause before the screen appears so the player sees Nova reach the gate.")]
        public float showDelay = 0.9f;

        RectTransform _root;

        void Awake()
        {
            var safe = UIFactory.CreateCanvas(transform, "ResultsCanvas", sortingOrder, landscape: true, out _);
            _root = UIFactory.Rect("Results", safe);
            UIFactory.Stretch(_root);
            _root.gameObject.SetActive(false);
        }

        void OnEnable() => EventBus<LevelCompleted>.Subscribe(OnCompleted);
        void OnDisable() => EventBus<LevelCompleted>.Unsubscribe(OnCompleted);

        void OnCompleted(LevelCompleted e) => StartCoroutine(Show(e));

        IEnumerator Show(LevelCompleted e)
        {
            yield return new WaitForSecondsRealtime(showDelay);
            _root.gameObject.SetActive(true);
            UIFactory.Clear(_root);
            UIFactory.Dim(_root);
            var panel = UIFactory.Panel(_root, new Vector2(1240f, 1000f));
            var p = UITheme.Current;

            var rank = ResultsMath.Rank(e.timeSeconds, e.parTimeSeconds, e.shards, e.totalShards,
                                        e.secrets, e.totalSecrets, e.deaths);
            UIFactory.Label(panel, $"{(e.levelName ?? "LEVEL").ToUpperInvariant()} CLEAR!", 64, TextAnchor.MiddleCenter, 90f, title: true);
            bool newHero = e.unlockedHeroes != null && e.unlockedHeroes.Length > 0;
            var rankLabel = UIFactory.Label(panel, $"RANK {rank}", newHero ? 80 : 96, TextAnchor.MiddleCenter, newHero ? 96f : 120f);
            rankLabel.color = rank == ClearRank.S ? p.title : p.accent;

            string best = e.newBestTime ? "   NEW BEST!" : "";
            Row(panel, "Time", $"{ResultsMath.FormatTime(e.timeSeconds)}  (par {ResultsMath.FormatTime(e.parTimeSeconds)}){best}");
            Row(panel, "Star Shards found", $"{e.shards} / {e.totalShards}  ({ResultsMath.Percent(e.shards, e.totalShards)}%)");
            Row(panel, "Secrets", $"{e.secrets} / {e.totalSecrets}");
            Row(panel, "Falls", e.deaths.ToString());
            var reward = Row(panel, "Reward", "+0");
            if (newHero)
            {
                var unlock = UIFactory.Label(panel, $"NEW HERO UNLOCKED: {string.Join(", ", e.unlockedHeroes).ToUpperInvariant()}!",
                                             44, TextAnchor.MiddleCenter, 60f, title: true);
                unlock.color = p.title;
            }

            var buttons = UIFactory.Rect("Buttons", panel);
            buttons.gameObject.AddComponent<LayoutElement>().preferredHeight = UIFactory.RowHeight;
            var h = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 24f;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = true; h.childForceExpandHeight = true;
            var again = UIFactory.Button(buttons, "PLAY AGAIN", () => { if (Services.Scenes != null) Services.Scenes.ReloadCurrent(); });
            // Back to the level select when it exists, else the main menu.
            bool hasLevels = Application.CanStreamedLevelBeLoaded(SceneIds.LevelSelect);
            bool hasMenu = Application.CanStreamedLevelBeLoaded(SceneIds.MainMenu);
            string backScene = hasLevels ? SceneIds.LevelSelect : SceneIds.MainMenu;
            UIFactory.Button(buttons, hasLevels ? "LEVELS" : (hasMenu ? "MAIN MENU" : "MENU (SOON)"),
                             () => { if (Services.Scenes != null) Services.Scenes.Load(backScene); },
                             interactable: hasLevels || hasMenu);
            var flowLevel = LevelFlowController.Current != null ? LevelFlowController.Current.Level : null;
            string next = flowLevel != null ? flowLevel.nextSceneName : "";
            bool hasNext = !string.IsNullOrEmpty(next) && Application.CanStreamedLevelBeLoaded(next);
            UIFactory.Button(buttons, hasNext ? "NEXT" : "NEXT (SOON)",
                             () => { if (Services.Scenes != null) Services.Scenes.Load(next); },
                             interactable: hasNext);
            UIFactory.Select(again);

            if (Services.Haptics != null) Services.Haptics.Play(HapticStrength.Heavy);

            // Count the reward up so it feels earned.
            int total = Services.Save != null ? Services.Save.Data.starShards : 0;
            float t = 0f;
            const float duration = 1.2f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                int shown = Mathf.RoundToInt(Mathf.Lerp(0, e.starShardReward, Mathf.Clamp01(t / duration)));
                reward.text = $"+{shown} Star Shards   (total {total - e.starShardReward + shown})";
                yield return null;
            }
            reward.text = $"+{e.starShardReward} Star Shards   (total {total})";
        }

        /// <summary>"Name ........ value" row; returns the value label so it can be animated.</summary>
        static Text Row(RectTransform parent, string name, string value)
        {
            var row = UIFactory.Rect("Row_" + name, parent);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = true; h.childForceExpandHeight = true;
            var left = UIFactory.Label(row, name, 42, TextAnchor.MiddleLeft, 64f);
            left.color = UITheme.Current.subtle;
            return UIFactory.Label(row, value, 42, TextAnchor.MiddleRight, 64f);
        }
    }
}
