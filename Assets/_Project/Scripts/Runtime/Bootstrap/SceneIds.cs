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
        public const string VersusSelect = "04_VersusSelect";
        public const string VersusStage = "VS_Dojo";
        // Phase 3.2/3.3 Story Quest levels, built from text maps (StoryLayouts).
        public const string SunspireHeights = "SQ_SunspireHeights";
        public const string GearfallCaverns = "SQ_GearfallCaverns";
        public const string RooftopRun = "SQ_RooftopRun";
        public const string NightMarketHeist = "SQ_NightMarketHeist";
        public const string FrostgearAscent = "SQ_FrostgearAscent";
        public const string ClockworkGlacier = "SQ_ClockworkGlacier";
        // Story world 3: The Shattered Realms
        public const string EmberWastes = "SQ_EmberWastes";
        public const string SkyGardens = "SQ_SkyGardens";
        public const string SunkenClockworks = "SQ_SunkenClockworks";
        public const string ToyFortress = "SQ_ToyFortress";
        public const string ShatteredCrossing = "SQ_ShatteredCrossing";
        public const string FourfoldSovereign = "SQ_FourfoldSovereign";
        // Shadow Contracts (mission mode): the board and the contract scenes (MS_ prefix).
        // Story world 4: the Starfall Observatory
        public const string StarfallSteps = "SQ_StarfallSteps";
        public const string OrreryGardens = "SQ_OrreryGardens";
        public const string CelestialMaze = "SQ_CelestialMaze";
        public const string CometRun = "SQ_CometRun";
        public const string OrrerySentinel = "SQ_OrrerySentinel";

        // Mazes (one per Story world)
        public const string GearworkMaze = "SQ_GearworkMaze";
        public const string FrozenVault = "SQ_FrozenVault";
        public const string ShatteredLabyrinth = "SQ_ShatteredLabyrinth";

        // Holiday Mode and Halloween Mode (EV_ = event levels, landscape gameplay)
        public const string EventPrefix = "EV_";
        public const string WinterGiftRun = "EV_WinterGiftRun";
        public const string SweetheartSprint = "EV_SweetheartSprint";
        public const string HarvestHustle = "EV_HarvestHustle";
        public const string CountdownClimb = "EV_CountdownClimb";
        public const string HauntedEscape = "EV_HauntedEscape";
        public const string ZombieSurvival = "EV_ZombieSurvival";

        public const string MissionBoard = "05_ShadowContracts";
        public const string ContractPrefix = "MS_";

        /// <summary>Gameplay scenes run in landscape; everything else is portrait.</summary>
        public static bool IsGameplay(string sceneName) =>
            sceneName == StoryTest || sceneName == ArenaTest ||
            sceneName.StartsWith("SQ_") || sceneName.StartsWith("AC_") || sceneName.StartsWith("VS_") ||
            sceneName.StartsWith(ContractPrefix) || sceneName.StartsWith(EventPrefix);
    }
}
