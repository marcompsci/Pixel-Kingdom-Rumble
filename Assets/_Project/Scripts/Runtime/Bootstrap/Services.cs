namespace PKR
{
    /// <summary>
    /// Global access to the persistent services created by GameBootstrap.
    /// Deliberately simple: a handful of long-lived singletons, set once at startup.
    /// </summary>
    public static class Services
    {
        public static SaveService Save { get; internal set; }
        public static SettingsService Settings { get; internal set; }
        public static SceneLoader Scenes { get; internal set; }
        public static GameStateManager State { get; internal set; }
        public static AudioService Audio { get; internal set; }
        public static HapticsService Haptics { get; internal set; }

        public static bool IsReady => Save != null && Settings != null && Scenes != null && State != null;

        internal static void Clear()
        {
            Save = null; Settings = null; Scenes = null; State = null; Audio = null; Haptics = null;
        }
    }
}
