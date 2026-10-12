using PKR.Core;
using UnityEngine;

namespace PKR
{
    public enum SessionMode { StoryQuest, ArenaClash, Versus, Contracts }

    /// <summary>
    /// What the player chose in the menus (mode, hero), carried into the next scene.
    /// The hero choice is also saved so it is remembered between launches.
    /// </summary>
    public static class GameSession
    {
        public static SessionMode Mode { get; set; } = SessionMode.StoryQuest;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Mode = SessionMode.StoryQuest;
            VersusOpponentId = null;
            VersusBotLevel = BotLevel.Normal;
            ContractScene = null;
            ContractWorldPage = -1;
            PendingWorldDoor = -1;
        }

        /// <summary>Shadow Contracts: the world page the board was last showing (-1 = pick automatically).</summary>
        public static int ContractWorldPage { get; set; } = -1;
        /// <summary>Set by the results screen when the player walks through a door into a new world; the board plays the
        /// door opening for that world and then opens the world's first contract.</summary>
        public static int PendingWorldDoor { get; set; } = -1;

        /// <summary>Shadow Contracts: the contract scene picked on the board (loaded after Character Select).</summary>
        public static string ContractScene { get; set; }

        /// <summary>Versus: the CPU's fighter (null = random) and difficulty, chosen on the Versus select screen.</summary>
        public static string VersusOpponentId { get; set; }
        public static BotLevel VersusBotLevel { get; set; } = BotLevel.Normal;

        public static string SelectedCharacterId
        {
            get
            {
                var save = Services.Save;
                var id = save != null ? save.Data.lastSelectedCharacter : null;
                return string.IsNullOrEmpty(id) ? SaveData.DefaultCharacterId : id;
            }
        }

        public static void SelectCharacter(string id)
        {
            var save = Services.Save;
            if (save == null || string.IsNullOrEmpty(id)) return;
            save.Data.UnlockCharacter(id); // keeps the choice valid when the save is repaired on load
            save.Data.lastSelectedCharacter = id;
            save.MarkDirty();
        }

        /// <summary>Scene after Character Select: the arena, or the Story Quest level select (straight to the first
        /// level if the level select scene isn't built).</summary>
        public static string TargetScene
        {
            get
            {
                if (Mode == SessionMode.ArenaClash) return SceneIds.ArenaTest;
                if (Mode == SessionMode.Versus) return SceneIds.VersusStage;
                if (Mode == SessionMode.Contracts)
                    return !string.IsNullOrEmpty(ContractScene) ? ContractScene : SceneIds.MissionBoard;
                return UnityEngine.Application.CanStreamedLevelBeLoaded(SceneIds.LevelSelect) ? SceneIds.LevelSelect : SceneIds.StoryTest;
            }
        }
    }
}
