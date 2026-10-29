using System;
using System.Collections;
using System.Collections.Generic;
using PKR.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// Small, interruptible UI tweens on unscaled time (they run while the game is paused), all using the Ease curves.
    /// One tween per target: starting a new one on the same object takes over from wherever the old one was, so rapid
    /// opens and closes never flash. With Reduced motion, scale and slide are dropped and only fades remain.
    ///
    /// The rules they follow (Emil Kowalski's design-engineering guidance, applied to uGUI): modals fade in from 97%
    /// scale (never from nothing) over 200 ms with a strong ease-out, the scrim fades with them, exits are faster than
    /// entrances, and switching pages inside an open menu does not replay the entrance.
    /// </summary>
    public class UiMotion : MonoBehaviour
    {
        static UiMotion _runner;
        static readonly Dictionary<UnityEngine.Object, Coroutine> Running = new Dictionary<UnityEngine.Object, Coroutine>();

        static UiMotion Runner
        {
            get
            {
                if (_runner != null) return _runner;
                var go = new GameObject("[UiMotion]");
                DontDestroyOnLoad(go);
                go.hideFlags = HideFlags.HideInHierarchy;
                _runner = go.AddComponent<UiMotion>();
                return _runner;
            }
        }

        public static bool Reduced => SettingsService.ReducedMotion;

        static void Run(UnityEngine.Object key, IEnumerator routine)
        {
            UnityEngine.Object id = key;
            if (Running.TryGetValue(id, out var c) && c != null) Runner.StopCoroutine(c);
            Running[id] = Runner.StartCoroutine(Wrap(id, routine));
        }

        static IEnumerator Wrap(UnityEngine.Object id, IEnumerator routine)
        {
            yield return routine;
            Running.Remove(id);
        }

        /// <summary>Stops whatever is animating this object (it stays where it is).</summary>
        public static void Stop(UnityEngine.Object key)
        {
            if (key == null) return;
            if (Running.TryGetValue(key, out var c) && c != null && _runner != null) _runner.StopCoroutine(c);
            Running.Remove(key);
        }

        /// <summary>A modal that was fading out is wanted again: stop the fade and show it fully.</summary>
        public static void Restore(RectTransform root)
        {
            if (root == null) return;
            Stop(root);
            var g = root.GetComponent<CanvasGroup>();
            if (g == null) return;
            g.alpha = 1f;
            g.blocksRaycasts = true;
            g.interactable = true;
        }

        static CanvasGroup Group(Component c)
        {
            var g = c.GetComponent<CanvasGroup>();
            return g != null ? g : c.gameObject.AddComponent<CanvasGroup>();
        }

        /// <summary>
        /// A modal just built under <paramref name="root"/> (UIFactory.Dim + UIFactory.Panel): the scrim fades in and the
        /// panel fades in from 97% scale, centred. Call once when the modal opens, not on every rebuild.
        /// </summary>
        public static void ModalIn(RectTransform root)
        {
            if (root == null) return;
            var g = Group(root);
            g.alpha = 1f;
            g.blocksRaycasts = true;
            g.interactable = true;
            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child.name == "Dim")
                {
                    var img = child.GetComponent<Image>();
                    if (img != null) FadeGraphic(img, 0f, img.color.a, Ease.Enter);
                }
                else if (child.name == "PanelEdge") Enter((RectTransform)child, Ease.Enter, Ease.ModalFrom, Vector2.zero);
            }
        }

        /// <summary>A modal going away: fades quickly, stops taking touches at once, then <paramref name="done"/>.</summary>
        public static void ModalOut(RectTransform root, Action done)
        {
            if (root == null) { done?.Invoke(); return; }
            var g = Group(root);
            g.blocksRaycasts = false;
            g.interactable = false;
            if (!root.gameObject.activeInHierarchy) { done?.Invoke(); return; }
            Run(root, FadeGroup(g, g.alpha, 0f, Ease.Exit, () => { g.alpha = 1f; g.blocksRaycasts = true; g.interactable = true; done?.Invoke(); }));
        }

        /// <summary>
        /// Something appearing: opacity 0 to 1 while it grows from <paramref name="fromScale"/> and slides from
        /// <paramref name="fromOffset"/> (px) to where it sits. Ease-out.
        /// </summary>
        public static void Enter(RectTransform rt, float seconds, float fromScale, Vector2 fromOffset)
        {
            if (rt == null) return;
            var g = Group(rt);
            // interrupted half way (fast taps)? slide from the real resting place, not from where it got to
            UnityEngine.Object id = rt;
            if (!(Running.ContainsKey(id) && Homes.TryGetValue(id, out var home))) home = rt.anchoredPosition;
            Homes[id] = home;
            Run(rt, EnterRoutine(rt, g, seconds, Reduced ? 1f : fromScale, Reduced ? Vector2.zero : fromOffset, home));
        }

        static readonly Dictionary<UnityEngine.Object, Vector2> Homes = new Dictionary<UnityEngine.Object, Vector2>();

        static IEnumerator EnterRoutine(RectTransform rt, CanvasGroup g, float seconds, float fromScale, Vector2 fromOffset, Vector2 home)
        {
            float t = 0f;
            while (t < seconds && rt != null)
            {
                float k = Ease.Out(t / seconds);
                g.alpha = k;
                rt.localScale = Vector3.one * Mathf.LerpUnclamped(fromScale, 1f, k);
                rt.anchoredPosition = home + fromOffset * (1f - k);
                yield return null;
                t += Time.unscaledDeltaTime;
            }
            if (rt == null) yield break;
            g.alpha = 1f;
            rt.localScale = Vector3.one;
            rt.anchoredPosition = home;
            Homes.Remove(rt);
        }

        public static void FadeGraphic(Graphic gr, float from, float to, float seconds)
        {
            if (gr == null) return;
            Run(gr, FadeGraphicRoutine(gr, from, to, seconds));
        }

        static IEnumerator FadeGraphicRoutine(Graphic gr, float from, float to, float seconds)
        {
            float t = 0f;
            var c = gr.color;
            while (t < seconds && gr != null)
            {
                c.a = Mathf.LerpUnclamped(from, to, Ease.Out(t / seconds));
                gr.color = c;
                yield return null;
                t += Time.unscaledDeltaTime;
            }
            if (gr == null) yield break;
            c.a = to;
            gr.color = c;
        }

        static IEnumerator FadeGroup(CanvasGroup g, float from, float to, float seconds, Action done)
        {
            float t = 0f;
            while (t < seconds && g != null)
            {
                g.alpha = Mathf.LerpUnclamped(from, to, Ease.Out(t / seconds));
                yield return null;
                t += Time.unscaledDeltaTime;
            }
            if (g == null) yield break;
            g.alpha = to;
            done?.Invoke();
        }

        /// <summary>Eases a scale to <paramref name="to"/> from wherever it is now (interruptible).</summary>
        public static void ScaleTo(Transform tr, float to, float seconds)
        {
            if (tr == null) return;
            if (Reduced || seconds <= 0f) { Stop(tr); tr.localScale = new Vector3(to, to, 1f); return; }
            Run(tr, ScaleRoutine(tr, tr.localScale.x, to, seconds));
        }

        static IEnumerator ScaleRoutine(Transform tr, float from, float to, float seconds)
        {
            float t = 0f;
            while (t < seconds && tr != null)
            {
                float s = Mathf.LerpUnclamped(from, to, Ease.Out(t / seconds));
                tr.localScale = new Vector3(s, s, 1f);
                yield return null;
                t += Time.unscaledDeltaTime;
            }
            if (tr != null) tr.localScale = new Vector3(to, to, 1f);
        }
    }
}
