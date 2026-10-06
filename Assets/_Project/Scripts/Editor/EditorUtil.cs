using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PKR.EditorTools
{
    /// <summary>Shared helpers for PKR editor tools: folders, layers, build settings, serialized fields.</summary>
    public static class EditorUtil
    {
        /// <summary>Creates "Assets/a/b/c" one level at a time (so Unity generates proper .meta files).</summary>
        public static void EnsureFolder(string path)
        {
            path = path.TrimEnd('/');
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }

        [MenuItem("PKR/Setup Layers", priority = 2)]
        public static void EnsureLayers()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0) { Debug.LogError("[PKR] TagManager.asset not found."); return; }
            var tagManager = new SerializedObject(assets[0]);
            var layers = tagManager.FindProperty("layers");
            bool changed = false;
            foreach (var (index, name) in PKRLayers.All)
            {
                var sp = layers.GetArrayElementAtIndex(index);
                if (sp.stringValue == name) continue;
                if (!string.IsNullOrEmpty(sp.stringValue))
                {
                    Debug.LogWarning($"[PKR] Layer {index} is already named '{sp.stringValue}', expected '{name}'. Leaving it; physics masks use the index.");
                    continue;
                }
                sp.stringValue = name;
                changed = true;
            }
            if (changed)
            {
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log("[PKR] Physics layers configured.");
            }
        }

        public static void AddSceneToBuild(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == scenePath)) return;
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        /// <summary>Make this scene index 0 in Build Settings (the scene the app launches into).</summary>
        public static void MakeFirstInBuild(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != scenePath).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        /// <summary>Assign a private [SerializeField] on a component without making it public.</summary>
        public static void SetField(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[PKR] Field '{field}' not found on {target.GetType().Name}"); return; }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetLayerMask(Object target, string field, int mask)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[PKR] Field '{field}' not found on {target.GetType().Name}"); return; }
            p.intValue = mask;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetInt(Object target, string field, int value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[PKR] Field '{field}' not found on {target.GetType().Name}"); return; }
            p.intValue = value; // also works for enums (by underlying value)
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetFloat(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[PKR] Field '{field}' not found on {target.GetType().Name}"); return; }
            p.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static Material UnlitSpriteMaterial()
        {
            // URP's unlit sprite material: renders correctly with no 2D lights in the scene.
            const string path = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) Debug.LogWarning("[PKR] URP Sprite-Unlit-Default material not found; sprites keep their default material. " +
                                              "If they render black, add a Global Light 2D to the scene.");
            return mat;
        }
    }
}
