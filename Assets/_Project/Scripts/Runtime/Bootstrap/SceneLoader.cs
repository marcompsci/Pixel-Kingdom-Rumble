using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PKR
{
    /// <summary>
    /// Async scene transitions. Applies the right orientation and game state for the target scene.
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

        IEnumerator LoadRoutine(string sceneName)
        {
            IsLoading = true;
            // Unfreeze before loading so the new scene starts with normal time.
            Services.State.SetState(GameState.Menu);
            OrientationController.ApplyForScene(sceneName);

            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            while (!op.isDone) yield return null;

            Services.State.SetState(SceneIds.IsGameplay(sceneName) ? GameState.Playing : GameState.Menu);
            IsLoading = false;
            SceneLoaded?.Invoke(sceneName);
        }
    }
}
