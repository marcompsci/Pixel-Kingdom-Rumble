using System;
using UnityEditor;
using UnityEngine;

namespace PKR.EditorTools
{
    /// <summary>
    /// Entry point for Tools/unity_check.command (Unity -batchmode -executeMethod PKR.EditorTools.BatchCheck.SetupAndBuild):
    /// runs the same steps as the PKR menu (layers, iOS settings, data assets, all scenes) without opening the Editor UI,
    /// and exits with 0 on success or 1 on an exception so the script can report it.
    /// </summary>
    public static class BatchCheck
    {
        public static void SetupAndBuild()
        {
            int code = 0;
            try
            {
                Debug.Log("[PKR BatchCheck] Setup Layers");
                EditorUtil.EnsureLayers();
                Debug.Log("[PKR BatchCheck] Configure iOS Player Settings");
                ProjectConfigurator.ConfigureIOS();
                Debug.Log("[PKR BatchCheck] Build All Scenes");
                MenuSceneBuilder.BuildAll();
                AssetDatabase.SaveAssets();
                Debug.Log("[PKR BatchCheck] OK");
            }
            catch (Exception e)
            {
                Debug.LogError("[PKR BatchCheck] FAILED: " + e);
                code = 1;
            }
            EditorApplication.Exit(code);
        }
    }
}
