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
        public const string StoryTest = "SQ_SunspireMeadows_Test";
        public const string ArenaTest = "AC_Skyforge_Test";

        /// <summary>Gameplay scenes run in landscape; everything else is portrait.</summary>
        public static bool IsGameplay(string sceneName) =>
            sceneName == StoryTest || sceneName == ArenaTest ||
            sceneName.StartsWith("SQ_") || sceneName.StartsWith("AC_");
    }
}
