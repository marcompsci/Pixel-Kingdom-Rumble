using System;
using System.Collections;
using PKR.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// Async scene transitions. Applies the right orientation and game state for the target scene. A screen change
    /// is covered by a quick fade to the backdrop colour (160 ms) and revealed by a slightly longer one (220 ms), both
    /// strong ease-out, instead of cutting hard from one screen to the next. The cover takes no touches once it's gone.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        public bool IsLoading { get; private set; }
        public event Action<string> SceneLoaded;

        public void Load(string sceneName)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"[SceneLoader] Ignoring load of '{sceneName}' while another load is in progress.");
                return;
            }
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[SceneLoader] Scene '{sceneName}' is not in Build Settings. Run PKR > Build Test Scenes.");
                return;
            }
            StartCoroutine(LoadRoutine(sceneName));
        }

        public void ReloadCurrent() => Load(SceneManager.GetActiveScene().name);

        CanvasGroup _cover;

        CanvasGroup Cover()
        {
            if (_cover != null) return _cover;
            var go = new GameObject("SceneCover", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            go.AddComponent<GraphicRaycaster>();
            var img = new GameObject("Fill", typeof(RectTransform)).AddComponent<Image>();
            img.transform.SetParent(go.transform, false);
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            img.color = new Color(0.04f, 0.03f, 0.07f, 1f);
            _cover = go.AddComponent<CanvasGroup>();
            _cover.alpha = 0f;
            _cover.blocksRaycasts = false;
            return _cover;
        }

        IEnumerator FadeCover(float from, float to, float seconds)
        {
            var c = Cover();
            c.blocksRaycasts = to >= from && to > 0.01f;   // covering swallows taps; revealing never blocks the new screen
            float t = 0f;
            while (t < seconds)
            {
                c.alpha = Mathf.LerpUnclamped(from, to, Ease.Out(t / seconds));
                yield return null;
                t += Time.unscaledDeltaTime;
            }
            c.alpha = to;
            c.blocksRaycasts = to >= from && to > 0.01f;
        }

        IEnumerator LoadRoutine(string sceneName)
        {
            IsLoading = true;
            if (_reveal != null) { StopCoroutine(_reveal); _reveal = null; }   // a load during a reveal covers from where it is
            yield return FadeCover(Cover().alpha, 1f, Ease.Cover);
            // Unfreeze before loading so the new scene starts with normal time.
            Services.State.SetState(GameState.Menu);
            OrientationController.ApplyForScene(sceneName);

            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            while (!op.isDone) yield return null;

            Services.State.SetState(SceneIds.IsGameplay(sceneName) ? GameState.Playing : GameState.Menu);
            IsLoading = false;
            SceneLoaded?.Invoke(sceneName);
            _reveal = StartCoroutine(Reveal());
        }

        Coroutine _reveal;

        IEnumerator Reveal()
        {
            yield return null;   // let the new scene draw its first frame under the cover
            yield return FadeCover(1f, 0f, Ease.Reveal);
            _reveal = null;
        }
    }
}
