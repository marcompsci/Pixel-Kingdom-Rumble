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
            var panel = UIFactory.Panel(_root, new Vector2(1240f, string.IsNullOrEmpty(e.stealthRank) && !e.clueFound ? 1000f : 1050f));
            var p = UITheme.Current;

            var rank = ResultsMath.Rank(e.timeSeconds, e.parTimeSeconds, e.shards, e.totalShards,
                                        e.secrets, e.totalSecrets, e.deaths);
            UIFactory.Label(panel, $"{(e.levelName ?? "LEVEL").ToUpperInvariant()} CLEAR!", 64, TextAnchor.MiddleCenter, 90f, title: true);
            bool newHero = e.unlockedHeroes != null && e.unlockedHeroes.Length > 0;
            var rankLabel = UIFactory.Label(panel, $"RANK {rank}", newHero ? 80 : 96, TextAnchor.MiddleCenter, newHero ? 96f : 120f);
            rankLabel.color = rank == ClearRank.S ? p.title : p.accent;

            string best = e.newBestTime ? "   NEW BEST!" : "";
            Row(panel, "Time", $"{ResultsMath.FormatTime(e.timeSeconds)}  (par {ResultsMath.FormatTime(e.parTimeSeconds)}){best}");
            if (e.isContract)
            {
                Row(panel, "Contract score", $"{e.contractScore:N0}{(e.newBestScore ? "   NEW BEST!" : "")}").color = p.title;
                Row(panel, "Ancient relics", $"{e.relics} / {e.totalRelics}");
            }
            else
            {
                Row(panel, "Star Shards found", $"{e.shards} / {e.totalShards}  ({ResultsMath.Percent(e.shards, e.totalShards)}%)");
                Row(panel, "Secrets", $"{e.secrets} / {e.totalSecrets}");
            }
            Row(panel, "Falls", e.deaths.ToString());
            if (!string.IsNullOrEmpty(e.stealthRank))
                Row(panel, "Stealth", $"{e.stealthRank}   (spotted {e.timesSpotted}, takedowns {e.takedowns})").color = p.title;
            var reward = Row(panel, "Reward", "+0");
            if (UISprites.HasDisplayFont)
            {
                // the pixel font's digits are all one width, so the number doesn't wobble while it counts up
                reward.font = UISprites.DisplayFont;
                reward.fontStyle = FontStyle.Normal;
                reward.fontSize = 34;
            }
            if (e.luckyBonus > 0 || e.worldBonus > 0)
            {
                string extra = e.worldBonus > 0 ? $"{e.worldCompleted.ToUpperInvariant()} COMPLETE!  +{e.worldBonus} world bonus" : "";
                if (e.luckyBonus > 0) extra += (extra.Length > 0 ? "   ·   " : "") + $"+{e.luckyBonus} Lucky Shards";
                UIFactory.Label(panel, extra, 38, TextAnchor.MiddleCenter, 56f, title: true).color = p.title;
            }
            if (e.clueFound)
            {
                var clue = UIFactory.Label(panel, "SECRET CONTRACT REVEALED ON THE BOARD!", 44, TextAnchor.MiddleCenter, 60f, title: true);
                clue.color = p.accent;
            }
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
            var flowLevel = LevelFlowController.Current != null ? LevelFlowController.Current.Level : null;
            bool isEvent = flowLevel != null && flowLevel.isEvent;
            // Back to the level select when it exists, else the main menu. Holiday / Halloween levels go back to their
            // menu panel.
            bool hasLevels = !isEvent && Application.CanStreamedLevelBeLoaded(e.isContract ? SceneIds.MissionBoard : SceneIds.LevelSelect);
            bool hasMenu = Application.CanStreamedLevelBeLoaded(SceneIds.MainMenu);
            string backScene = hasLevels ? (e.isContract ? SceneIds.MissionBoard : SceneIds.LevelSelect) : SceneIds.MainMenu;
            string backText = isEvent ? (GameSession.Mode == SessionMode.Halloween ? "HALLOWEEN" : "HOLIDAYS")
                            : hasLevels ? (e.isContract ? "CONTRACTS" : "LEVELS") : (hasMenu ? "MAIN MENU" : "MENU (SOON)");
            UIFactory.Button(buttons, backText, () =>
                             {
                                 if (isEvent) GameSession.ReopenEventMenu = true;
                                 if (Services.Scenes != null) Services.Scenes.Load(backScene);
                             },
                             interactable: hasLevels || hasMenu);
            string next = flowLevel != null ? flowLevel.nextSceneName : "";
            bool hasNext = !string.IsNullOrEmpty(next) && Application.CanStreamedLevelBeLoaded(next);
            if (Highlights.Available)
            {
                string levelName = flowLevel != null ? flowLevel.displayName : "a level";
                UIFactory.Button(buttons, "CLIP", () => Highlights.Share(
                    $"Cleared {levelName} in {ResultsMath.FormatTime(e.timeSeconds)} in Pixel Kingdom Rumble! " + ShareCard.Link));
            }
            if (isEvent) { } // event levels stand alone: no NEXT
            else if (!e.isContract)
                UIFactory.Button(buttons, hasNext ? "NEXT" : "NEXT (SOON)",
                                 () => { if (Services.Scenes != null) Services.Scenes.Load(next); },
                                 interactable: hasNext);
            else
                ContractDoorButton(buttons);
            UIFactory.Select(again);
            UiMotion.ModalIn(_root);   // the results fade in from 97% (rare, so it can take its full 200 ms)

            if (Services.Haptics != null) Services.Haptics.Play(HapticStrength.Heavy);

            // Count the reward up so it feels earned: fast at first, settling onto the total (ease-out, not linear).
            int total = Services.Save != null ? Services.Save.Data.starShards : 0;
            float t = 0f;
            const float duration = 0.9f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                int shown = Mathf.RoundToInt(Mathf.Lerp(0, e.starShardReward, PKR.Core.Ease.Out(Mathf.Clamp01(t / duration))));
                reward.text = $"+{Plural.Of(shown, "Star Shard")}   (total {total - e.starShardReward + shown})";
                yield return null;
            }
            reward.text = $"+{Plural.Of(e.starShardReward, "Star Shard")}   (total {total})";
        }

        /// <summary>
        /// Shadow Contracts: the exit door. Leads to the next contract in the same world, or, after a world's last
        /// contract, through the big door into the next world (the board plays the door opening). Nothing when the next
        /// contract isn't playable yet (unlock not owned, or this world isn't fully cleared).
        /// </summary>
        static void ContractDoorButton(RectTransform buttons)
        {
            var mt = MissionTracker.Current;
            var board = mt != null ? mt.board : null;
            if (board == null) return;
            var infos = board.Infos();
            var save = Services.Save != null ? Services.Save.Data : null;
            int idx = MissionBoard.NextThroughDoor(infos, save, mt.contractId, out bool crosses);
            if (idx < 0) return;
            var next = board.contracts[idx];
            if (next == null || next.level == null || !Application.CanStreamedLevelBeLoaded(next.level.sceneName)) return;
            if (crosses)
            {
                var b = UIFactory.Button(buttons, $"ENTER {board.World(next.world).name.ToUpperInvariant()}", () =>
                {
                    GameSession.Mode = SessionMode.Contracts;
                    GameSession.PendingWorldDoor = next.world;
                    if (Services.Scenes != null) Services.Scenes.Load(SceneIds.MissionBoard);
                });
                UIFactory.Tint(b, board.World(next.world).color, Color.white);
            }
            else
            {
                UIFactory.Button(buttons, "NEXT DOOR", () =>
                {
                    GameSession.Mode = SessionMode.Contracts;
                    GameSession.ContractScene = next.level.sceneName;
                    GameSession.ContractWorldPage = next.world;
                    if (Services.Scenes != null) Services.Scenes.Load(next.level.sceneName);
                });
            }
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
