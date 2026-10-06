using PKR.Core;
using UnityEngine;

namespace PKR
{
    public enum SessionMode { StoryQuest, ArenaClash }

    /// <summary>
    /// What the player chose in the menus (mode, hero), carried into the next scene.
    /// The hero choice is also saved so it is remembered between launches.
    /// </summary>
    public static class GameSession
    {
        public static SessionMode Mode { get; set; } = SessionMode.StoryQuest;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Mode = SessionMode.StoryQuest;

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

        /// <summary>Scene for the current mode.</summary>
        public static string TargetScene => Mode == SessionMode.ArenaClash ? SceneIds.ArenaTest : SceneIds.StoryTest;
    }
}
