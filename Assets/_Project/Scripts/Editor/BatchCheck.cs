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
                Debug.Log("[PKR BatchCheck] Setup URP 2D Renderer");
                RenderPipelineSetup.EnsureURP2D();
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
    
        /// <summary>
        /// Batch iOS build for Tools/ios_build.command: exports the Xcode project to Builds/iOS
        /// (run Unity with -buildTarget iOS). Exits 0 on success, 1 on failure.
        /// </summary>
        public static void BuildIOS()
        {
            int code = 0;
            try
            {
                RenderPipelineSetup.EnsureURP2D();
                ProjectConfigurator.ConfigureIOS();
                var scenes = new System.Collections.Generic.List<string>();
                foreach (var s in EditorBuildSettings.scenes) if (s.enabled) scenes.Add(s.path);
                if (scenes.Count == 0) throw new Exception("No scenes in Build Settings. Run PKR > Build All Scenes first.");
                var options = new BuildPlayerOptions
                {
                    scenes = scenes.ToArray(),
                    locationPathName = "Builds/iOS",
                    target = BuildTarget.iOS,
                    options = BuildOptions.None,
                    // Tester builds (the default from Tools/ios_build.command) get a "TESTER BUILD: UNLOCK ALL" button on the
                    // Shadow Contracts board. App Store builds must set PKR_RELEASE=1 so it is compiled out.
                    extraScriptingDefines = Environment.GetEnvironmentVariable("PKR_RELEASE") == "1" ? new string[0] : new[] { "PKR_TESTER" }
                };
                Debug.Log("[PKR BuildIOS] " + (options.extraScriptingDefines.Length > 0 ? "Tester build (PKR_TESTER)." : "Release build."));
                var report = UnityEditor.BuildPipeline.BuildPlayer(options);
                Debug.Log($"[PKR BuildIOS] {report.summary.result}: {report.summary.totalErrors} errors, " +
                          $"{report.summary.totalSize / (1024 * 1024)} MB, {report.summary.totalTime}");
                if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) code = 1;
                else Debug.Log("[PKR BuildIOS] OK");
            }
            catch (Exception e)
            {
                Debug.LogError("[PKR BuildIOS] FAILED: " + e);
                code = 1;
            }
            EditorApplication.Exit(code);
        }
    }
}
