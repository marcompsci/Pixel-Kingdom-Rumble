using System;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Tiny typed event bus for decoupled game events (pause, checkpoint, pickup, KO...).
    /// Subscribe in OnEnable, unsubscribe in OnDisable.
    /// </summary>
    public static class EventBus<T> where T : struct
    {
        static event Action<T> Handlers;

        public static void Subscribe(Action<T> handler) => Handlers += handler;
        public static void Unsubscribe(Action<T> handler) => Handlers -= handler;

        public static void Raise(T evt)
        {
            var h = Handlers;
            if (h == null) return;
            foreach (Action<T> d in h.GetInvocationList())
            {
                try { d(evt); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }
    }

    // ---- Common events -------------------------------------------------------------------------

    public struct GameStateChanged { public GameState previous; public GameState current; }
    public struct SettingsChanged { public PKR.Core.SettingsData settings; }
    public struct ShardsChanged { public int total; public int delta; }
}
