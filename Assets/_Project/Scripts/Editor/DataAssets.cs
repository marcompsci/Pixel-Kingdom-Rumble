using UnityEditor;
using UnityEngine;

namespace PKR.EditorTools
{
    /// <summary>
    /// Creates default data assets if missing. Never overwrites tuning you've changed in the Inspector;
    /// only fills empty art references.
    /// </summary>
    public static class DataAssets
    {
        public const string CharactersFolder = "Assets/_Project/Data/Characters";

        [MenuItem("PKR/Create Default Data Assets", priority = 3)]
        public static void CreateAll()
        {
            GetOrCreateNova();
            AssetDatabase.SaveAssets();
        }

        public static CharacterDefinition GetOrCreateNova()
        {
            EditorUtil.EnsureFolder(CharactersFolder);
            var kit = GetOrCreate<NovaKitDefinition>($"{CharactersFolder}/NovaKit.asset", _ => { });
            var nova = GetOrCreate<CharacterDefinition>($"{CharactersFolder}/Nova.asset", d =>
            {
                d.id = "nova";
                d.displayName = "Nova";
                d.tagline = "Sky courier. Never late, rarely on the ground.";
                d.lore = "Nova carries messages between the drifting Sunspire Isles, vaulting gaps no bridge reaches. " +
                         "Her comet staff was cut from a fallen star-rail of the Tickworks, and it still hums when the " +
                         "great gears turn backward. She took up the Star Shard hunt because a courier who stops moving " +
                         "is a courier whose island has already fallen.";
                d.unlockedByDefault = true;
                d.playableInThisBuild = true;
                d.placeholderColor = new Color32(46, 168, 178, 255);
                d.maxHealth = 5;
                d.weight = 0.9f;   // light: travels far when launched
                d.armorPercent = 0f;
                d.guardPips = 3;
            });
            if (nova.kit == null) nova.kit = kit;
            if (nova.bodySprite == null) nova.bodySprite = PlaceholderArt.Nova();
            EditorUtility.SetDirty(nova);
            return nova;
        }

        static T GetOrCreate<T>(string path, System.Action<T> init) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            init(asset);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
