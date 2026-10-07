namespace PKR
{
    /// <summary>
    /// Scene names in one place. These must match the scenes in Build Settings
    /// (the PKR > Build Test Scenes editor command adds them automatically).
    /// </summary>
    public static class SceneIds
    {
        public const string Boot = "00_Boot";
        public const string MainMenu = "01_MainMenu";
        public const string CharacterSelect = "02_CharacterSelect";
        public const string LevelSelect = "03_LevelSelect";
        public const string StoryTest = "SQ_SunspireMeadows_Test";
        public const string BossTest = "SQ_ClockworkWarden_Test";
        public const string ArenaTest = "AC_Skyforge_Test";
        public const string MovementSandbox = "SQ_MovementSandbox";

        /// <summary>Gameplay scenes run in landscape; everything else is portrait.</summary>
        public static bool IsGameplay(string sceneName) =>
            sceneName == StoryTest || sceneName == ArenaTest ||
            sceneName.StartsWith("SQ_") || sceneName.StartsWith("AC_");
    }
}
