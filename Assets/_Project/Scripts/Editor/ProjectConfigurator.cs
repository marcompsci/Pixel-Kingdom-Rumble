using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace PKR.EditorTools
{
    /// <summary>
    /// One-click Player Settings for iOS. Safe to run repeatedly.
    /// Menu: PKR > Configure iOS Player Settings
    /// </summary>
    public static class ProjectConfigurator
    {
        // Change before App Store submission. Must match the App ID in your Apple Developer account.
        public const string DefaultBundleId = "com.marcompsci.pixelkingdomrumble";
        public const string ProductName = "Pixel Kingdom Rumble";
        public const string AppleTeamId = "X6LZQ3FS36";
        public const string MinIOSVersion = "15.0";

        [MenuItem("PKR/Configure iOS Player Settings", priority = 1)]
        public static void ConfigureIOS()
        {
            PlayerSettings.productName = ProductName;
            if (string.IsNullOrEmpty(PlayerSettings.companyName) || PlayerSettings.companyName == "DefaultCompany")
                PlayerSettings.companyName = "marcompsci";

            var ios = NamedBuildTarget.iOS;
            string currentId = PlayerSettings.GetApplicationIdentifier(ios);
            if (string.IsNullOrEmpty(currentId) || currentId.StartsWith("com.DefaultCompany") || currentId.StartsWith("com.Unity") ||
                currentId.Contains("Pixel-Kingdom-Rumble")) // Unity's auto id from the folder name
                PlayerSettings.SetApplicationIdentifier(ios, DefaultBundleId);
            // Automatic signing with Omari's Apple Developer team (Team ID is public, not a secret).
            if (string.IsNullOrEmpty(PlayerSettings.iOS.appleDeveloperTeamID)) PlayerSettings.iOS.appleDeveloperTeamID = AppleTeamId;
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;

            PlayerSettings.SetScriptingBackend(ios, ScriptingImplementation.IL2CPP);
            PlayerSettings.iOS.targetOSVersionString = MinIOSVersion;
            PlayerSettings.iOS.requiresFullScreen = true;
            PlayerSettings.statusBarHidden = true;

            // Portrait menus + landscape gameplay: allow all three, code restricts per scene.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.useAnimatedAutorotation = true;

            AssetDatabase.SaveAssets();
            Debug.Log($"[PKR] iOS Player Settings configured. Bundle ID: {PlayerSettings.GetApplicationIdentifier(ios)}, " +
                      $"min iOS {MinIOSVersion}, IL2CPP, auto-rotation (portrait + landscape).");
        }
    }
}
