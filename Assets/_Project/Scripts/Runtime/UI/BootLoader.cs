using System.Collections;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// 00_Boot: shows the title for a moment while services start, then opens the main menu.
    /// (Services are created before any scene loads, so any scene can still be played directly in the Editor.)
    /// </summary>
    public class BootLoader : MonoBehaviour
    {
        public float minimumSeconds = 0.8f;

        void Awake()
        {
            var safe = UIFactory.CreateCanvas(transform, "BootCanvas", 10, landscape: false, out _);
            var title = UIFactory.Label(safe, "PIXEL KINGDOM\nRUMBLE", 110, TextAnchor.MiddleCenter, 400f, title: true);
            var rt = title.rectTransform;
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(0f, 400f);
        }

        IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(minimumSeconds);
            if (Services.Scenes != null) Services.Scenes.Load(SceneIds.MainMenu);
            else Debug.LogError("[BootLoader] Services missing; GameBootstrap did not run.");
        }
    }
}
