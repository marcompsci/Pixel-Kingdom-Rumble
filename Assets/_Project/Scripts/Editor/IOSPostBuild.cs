#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace PKR.EditorTools
{
    /// <summary>
    /// Runs after Unity exports the Xcode project: adds the Game Center and In-App Purchase capabilities (entitlement +
    /// GameKit/StoreKit frameworks) and declares that the app uses no non-exempt encryption, so TestFlight uploads
    /// don't stop at the export-compliance question.
    /// </summary>
    public static class IOSPostBuild
    {
        const string EntitlementsName = "PixelKingdomRumble.entitlements";

        [PostProcessBuild(100)]
        public static void OnPostprocessBuild(BuildTarget target, string path)
        {
            if (target != BuildTarget.iOS) return;

            string projPath = PBXProject.GetPBXProjectPath(path);
            var proj = new PBXProject();
            proj.ReadFromFile(projPath);
            string appTarget = proj.GetUnityMainTargetGuid();
            string frameworkTarget = proj.GetUnityFrameworkTargetGuid();
            proj.AddFrameworkToProject(frameworkTarget, "GameKit.framework", false);
            proj.AddFrameworkToProject(frameworkTarget, "StoreKit.framework", false);
            proj.WriteToFile(projPath);

            var caps = new ProjectCapabilityManager(projPath, "Unity-iPhone/" + EntitlementsName, null, appTarget);
            caps.AddGameCenter();
            caps.AddInAppPurchase();
            caps.WriteToFile();

            // Make sure the Game Center entitlement itself is present (some Unity versions only add the framework).
            string entPath = Path.Combine(path, "Unity-iPhone", EntitlementsName);
            var ent = new PlistDocument();
            if (File.Exists(entPath)) ent.ReadFromFile(entPath);
            ent.root.SetBoolean("com.apple.developer.game-center", true);
            ent.WriteToFile(entPath);

            string plistPath = Path.Combine(path, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            plist.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
            plist.WriteToFile(plistPath);

            Debug.Log("[PKR iOS] Post-build: Game Center + In-App Purchase capabilities, ITSAppUsesNonExemptEncryption=NO.");
        }
    }
}
#endif
