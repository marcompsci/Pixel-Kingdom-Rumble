using System;
using PKR.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// A three-card welcome for brand-new players on the main menu (shown once per save): where to start, what else
    /// there is, and the comfort settings. Skippable at any point; START STORY QUEST goes straight into the plaza.
    /// </summary>
    public class WelcomeGuide : MonoBehaviour
    {
        public const string HintId = "welcome_v1";
        /// <summary>PlayMode tests set this to see the guide (batch-mode runs skip it so other menu tests aren't blocked).</summary>
        public static bool ForceShow;

        static readonly string[] Titles = { "WELCOME, HERO!", "MORE WAYS TO PLAY", "PLAY YOUR WAY" };
        static readonly string[] Bodies =
        {
            "Start with STORY QUEST. You'll arrive in the Kingdom Plaza: walk to a door and tap ENTER to jump into a " +
            "world, or try a party quest. Clear levels to find Star Shards and unlock new heroes.",
            "ZOMBIES: hold the line in Last Stand or explore Zombie Land.\nARENA CLASH and VERSUS: knock rivals off the stage " +
            "or win a best-of-3 duel (try the VERSUS LADDER).\nDAILY: fresh challenges every day.",
            "Settings has Assist mode (+2 hearts), a slower Story game speed, bigger touch targets, left-handed controls, " +
            "colour-vision palettes and reduced motion. Move the touch controls anywhere with EDIT LAYOUT.",
        };

        int _page;
        RectTransform _root;
        Action _onStory, _onDone;

        public static bool ShouldShow(SaveData save)
        {
            if (save == null) return false;
            if (Application.isBatchMode && !ForceShow) return false;
            if (save.seenHints != null && save.seenHints.Contains(HintId)) return false;
            if (save.lifetime != null && save.lifetime.replayWelcome) return true; // Settings > RESET HINTS & GUIDES
            return IsNewPlayer(save);
        }

        /// <summary>No level cleared yet: a brand-new player (they get this guide, not WHAT'S NEW).</summary>
        public static bool IsNewPlayer(SaveData save)
        {
            if (save == null) return true;
            foreach (var l in save.levels) if (l != null && l.completed) return false;
            return true;
        }

        public static WelcomeGuide Show(Transform parent, Action onStory, Action onDone)
        {
            var go = new GameObject("WelcomeGuide");
            go.transform.SetParent(parent, false);
            var g = go.AddComponent<WelcomeGuide>();
            g._onStory = onStory;
            g._onDone = onDone;
            g._root = UIFactory.CreateCanvas(go.transform, "WelcomeCanvas", 90, landscape: false, out _);
            g.Build();
            UiMotion.ModalIn(g._root);
            return g;
        }

        public int Page => _page;

        void Build()
        {
            UIFactory.Clear(_root);
            UIFactory.Dim(_root);
            var panel = UIFactory.Panel(_root, new Vector2(900f, 1100f));
            UIFactory.Label(panel, Titles[_page], 72, TextAnchor.MiddleCenter, 110f, title: true);
            UIFactory.Label(panel, $"{_page + 1} / {Titles.Length}", 30, TextAnchor.MiddleCenter, 44f).color = UITheme.Current.subtle;
            UIFactory.Label(panel, Bodies[_page], 38, TextAnchor.MiddleCenter, 560f);
            bool last = _page == Titles.Length - 1;
            var main = UIFactory.Button(panel, last ? "START STORY QUEST" : "NEXT", () =>
            {
                if (last) Finish(story: true);
                else { _page++; Build(); }
            });
            UIFactory.Button(panel, last ? "LOOK AROUND FIRST" : "SKIP", () => Finish(story: false));
            UIFactory.Select(main);
        }

        public void Finish(bool story)
        {
            var save = Services.Save;
            if (save != null)
            {
                save.Data.MarkHintSeen(HintId);
                save.Data.lifetime.replayWelcome = false;
                WhatsNew.MarkSeen(save.Data); // the guide already covers what's in the game
                save.SaveNow();
            }
            Destroy(gameObject);
            if (story) _onStory?.Invoke();
            else _onDone?.Invoke();
        }
    }
}
