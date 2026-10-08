using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PKR.EditorTools
{
    /// <summary>
    /// Makes sure the project renders with URP and the 2D Renderer (what the Universal 2D template sets up).
    /// Without it a project opened from this repo falls back to the Built-in pipeline and the URP sprite material
    /// renders pink. Safe to run repeatedly: an existing pipeline asset is left alone.
    /// Menu: PKR > Setup URP 2D Renderer
    /// </summary>
    public static class RenderPipelineSetup
    {
        public const string Folder = "Assets/_Project/Settings/Rendering";
        const string RendererPath = Folder + "/PKR_Renderer2D.asset";
        const string PipelinePath = Folder + "/PKR_URP2D.asset";

        [MenuItem("PKR/Setup URP 2D Renderer", priority = 4)]
        public static void EnsureURP2D()
        {
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset existing)
            {
                AssignToAllQualityLevels(existing);
                Debug.Log($"[PKR] URP already set up ({AssetDatabase.GetAssetPath(existing)}).");
                return;
            }

            EditorUtil.EnsureFolder(Folder);
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                var renderer = AssetDatabase.LoadAssetAtPath<Renderer2DData>(RendererPath);
                if (renderer == null)
                {
                    renderer = ScriptableObject.CreateInstance<Renderer2DData>();
                    AssetDatabase.CreateAsset(renderer, RendererPath);
                }
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;
            AssignToAllQualityLevels(pipeline);
            AssetDatabase.SaveAssets();
            Debug.Log($"[PKR] URP 2D Renderer set up: {PipelinePath}.");
        }

        static void AssignToAllQualityLevels(RenderPipelineAsset pipeline)
        {
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                if (QualitySettings.renderPipeline != pipeline) QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(current, false);
        }
    }
}
