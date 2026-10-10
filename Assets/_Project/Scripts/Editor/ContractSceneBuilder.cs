using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace PKR.EditorTools
{
    /// <summary>
    /// PKR > Build Shadow Contracts: the six contract scenes (MS_*, from ContractLayouts) and the portrait board
    /// 05_ShadowContracts. Part of PKR > Build All Scenes.
    /// </summary>
    public static class ContractSceneBuilder
    {
        public const string BoardPath = "Assets/_Project/Scenes/" + SceneIds.MissionBoard + ".unity";

        [MenuItem("PKR/Build Shadow Contracts", priority = 26)]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildNoPrompt();
        }

        public static void BuildNoPrompt()
        {
            AsciiLevelBuilder.BuildContracts();
            BuildBoard();
        }

        static void BuildBoard()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(24, 20, 48, 255);
            cam.transform.position = new Vector3(0f, 0f, -10f);
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var ui = new GameObject("ShadowContracts").AddComponent<MissionBoardUI>();
            EditorUtil.SetField(ui, "board", DataAssets.GetOrCreateMissionBoard());
            EditorSceneManager.SaveScene(scene, BoardPath);
            EditorUtil.AddSceneToBuild(BoardPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[PKR] Shadow Contracts built: {BoardPath} and {DataAssets.ContractSpecs.Length} contracts.");
        }
    }
}
