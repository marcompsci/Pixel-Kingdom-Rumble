using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace PKR.EditorTools
{
    /// <summary>
    /// PKR > Build Menu Scenes: 00_Boot (launch scene, index 0 in Build Settings), 01_MainMenu and
    /// 02_CharacterSelect. All UI is built at runtime by MainMenuUI / CharacterSelectUI, so these scenes are tiny.
    /// PKR > Build All Scenes runs every builder in the right order.
    /// </summary>
    public static class MenuSceneBuilder
    {
        const string Folder = "Assets/_Project/Scenes";
        static readonly Color MenuSky = new Color32(36, 40, 74, 255);

        [MenuItem("PKR/Build Menu Scenes", priority = 22)]
        public static void BuildMenus()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildMenusNoPrompt();
        }

        [MenuItem("PKR/Build All Scenes", priority = 40)]
        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorUtil.EnsureLayers();
            DataAssets.CreateAll();
            SandboxBuilder.BuildNoPrompt();
            StoryLevelBuilder.BuildNoPrompt();
            BossSceneBuilder.BuildNoPrompt();
            BuildMenusNoPrompt();
            ArenaHook?.Invoke(); // set by the arena builder (increment 7) so this file doesn't depend on it
            Debug.Log("[PKR] All scenes built. Press Play in 00_Boot to run the game from the start.");
        }

        /// <summary>Lets later builders join PKR > Build All Scenes without editing this file.</summary>
        public static System.Action ArenaHook;

        static void BuildMenusNoPrompt()
        {
            EditorUtil.EnsureLayers();
            var roster = DataAssets.GetOrCreateRoster();
            var level = DataAssets.GetOrCreateSunspireTestLevel();
            EditorUtil.EnsureFolder(Folder);

            // 00_Boot
            string bootPath = $"{Folder}/{SceneIds.Boot}.unity";
            var boot = NewUiScene();
            new GameObject("Boot").AddComponent<BootLoader>();
            EditorSceneManager.SaveScene(boot, bootPath);
            EditorUtil.AddSceneToBuild(bootPath);
            EditorUtil.MakeFirstInBuild(bootPath);

            // 01_MainMenu
            string menuPath = $"{Folder}/{SceneIds.MainMenu}.unity";
            var menu = NewUiScene();
            var menuUi = new GameObject("MainMenu").AddComponent<MainMenuUI>();
            EditorUtil.SetField(menuUi, "featuredLevel", level);
            EditorUtil.SetField(menuUi, "roster", roster);
            EditorUtil.SetField(menuUi, "codex", DataAssets.GetOrCreateCodex());
            EditorSceneManager.SaveScene(menu, menuPath);
            EditorUtil.AddSceneToBuild(menuPath);

            // 02_CharacterSelect
            string selectPath = $"{Folder}/{SceneIds.CharacterSelect}.unity";
            var select = NewUiScene();
            var selectUi = new GameObject("CharacterSelect").AddComponent<CharacterSelectUI>();
            EditorUtil.SetField(selectUi, "roster", roster);
            EditorSceneManager.SaveScene(select, selectPath);
            EditorUtil.AddSceneToBuild(selectPath);

            // 03_LevelSelect
            string levelsPath = $"{Folder}/{SceneIds.LevelSelect}.unity";
            var levels = NewUiScene();
            var levelsUi = new GameObject("LevelSelect").AddComponent<LevelSelectUI>();
            EditorUtil.SetField(levelsUi, "world", DataAssets.GetOrCreateStoryWorld());
            EditorSceneManager.SaveScene(levels, levelsPath);
            EditorUtil.AddSceneToBuild(levelsPath);

            AssetDatabase.SaveAssets();
            Debug.Log("[PKR] Menu scenes built: 00_Boot (first in Build Settings), 01_MainMenu, 02_CharacterSelect, 03_LevelSelect.");
        }

        static UnityEngine.SceneManagement.Scene NewUiScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = MenuSky;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            return scene;
        }
    }
}
