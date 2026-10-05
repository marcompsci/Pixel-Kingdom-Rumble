using UnityEngine;

namespace PKR
{
    public enum GameState { Menu, Playing, Paused, Results }

    /// <summary>
    /// Owns pause state. Pausing freezes Time.timeScale and audio; gameplay code that must keep running
    /// while paused (UI animations) should use unscaled time.
    /// Auto-pauses when iOS backgrounds the app mid-run.
    /// </summary>
    public class GameStateManager : MonoBehaviour
    {
        public GameState State { get; private set; } = GameState.Menu;
        public bool IsPaused => State == GameState.Paused;

        public void SetState(GameState next)
        {
            if (next == State) return;
            var prev = State;
            State = next;

            bool frozen = next == GameState.Paused;
            Time.timeScale = frozen ? 0f : 1f;
            AudioListener.pause = frozen;

            EventBus<GameStateChanged>.Raise(new GameStateChanged { previous = prev, current = next });
        }

        public void Pause()
        {
            if (State == GameState.Playing) SetState(GameState.Paused);
        }

        public void Resume()
        {
            if (State == GameState.Paused) SetState(GameState.Playing);
        }

        public void TogglePause()
        {
            if (State == GameState.Playing) Pause();
            else if (State == GameState.Paused) Resume();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                Pause();
                Services.Save?.SaveNow();
            }
        }

#if !UNITY_EDITOR
        // Not in the Editor: clicking another window would pause the game constantly.
        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) Pause();
        }
#endif
    }
}
