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
            AddAppPrivacyManifest(proj, appTarget, path);
            proj.WriteToFile(projPath);
            DeclareFrameworkReasons(path);

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
            // The share sheet's "Save Image" (photo mode, result cards) adds to the photo library.
            plist.root.SetString("NSPhotoLibraryAddUsageDescription", "Save your Pixel Kingdom Rumble photos and result cards.");
            // Friends-only leaderboards (the LOCKER's "you vs your friends") ask for Game Center friends access.
            plist.root.SetString("NSGKFriendListUsageDescription", "See where you rank against your Game Center friends on the zombie leaderboards.");
            // iPad: the game rotates freely (OrientationController), so declare all four orientations for iPad.
            var ipad = plist.root.CreateArray("UISupportedInterfaceOrientations~ipad");
            foreach (var o in new[] { "UIInterfaceOrientationPortrait", "UIInterfaceOrientationPortraitUpsideDown",
                                      "UIInterfaceOrientationLandscapeLeft", "UIInterfaceOrientationLandscapeRight" })
                ipad.AddString(o);
            plist.WriteToFile(plistPath);

            Debug.Log("[PKR iOS] Post-build: Game Center + In-App Purchase capabilities, ITSAppUsesNonExemptEncryption=NO, photo library add usage, privacy manifests.");
        }

        /// <summary>
        /// The app's own privacy manifest (Unity-iPhone target): no tracking, no tracking domains, no data collected.
        /// This matches the App Privacy label "Data Not Collected" and PRIVACY.md. Game Center and StoreKit data is
        /// handled by Apple. If the game ever adds analytics, ads or a server, this and the label must change together.
        /// </summary>
        static void AddAppPrivacyManifest(PBXProject proj, string appTarget, string path)
        {
            const string rel = "Unity-iPhone/PrivacyInfo.xcprivacy";
            var doc = new PlistDocument();
            doc.root.SetBoolean("NSPrivacyTracking", false);
            doc.root.CreateArray("NSPrivacyTrackingDomains");
            doc.root.CreateArray("NSPrivacyCollectedDataTypes");
            doc.root.CreateArray("NSPrivacyAccessedAPITypes");   // the app target's own code (main.mm) uses none
            doc.WriteToFile(Path.Combine(path, rel));
            string guid = proj.FindFileGuidByProjectPath(rel) ?? proj.AddFile(rel, rel);
            proj.AddFileToBuild(appTarget, guid);
        }

        /// <summary>
        /// UnityFramework holds all of the game's compiled C#. Besides what Unity declares, the game measures elapsed
        /// time with Environment.TickCount / Stopwatch (system boot time, reason 35F9.1: elapsed time inside the app),
        /// so make sure that category is declared too.
        /// </summary>
        static void DeclareFrameworkReasons(string path)
        {
            string file = Path.Combine(path, "UnityFramework", "PrivacyInfo.xcprivacy");
            var doc = new PlistDocument();
            if (File.Exists(file)) doc.ReadFromFile(file);
            else doc.root.SetBoolean("NSPrivacyTracking", false);
            var types = doc.root["NSPrivacyAccessedAPITypes"] as PlistElementArray ?? doc.root.CreateArray("NSPrivacyAccessedAPITypes");
            bool has = false;
            foreach (var e in types.values)
                if (e is PlistElementDict ed && ed["NSPrivacyAccessedAPIType"] != null &&
                    ed["NSPrivacyAccessedAPIType"].AsString() == "NSPrivacyAccessedAPICategorySystemBootTime") has = true;
            if (!has)
            {
                var d = types.AddDict();
                d.SetString("NSPrivacyAccessedAPIType", "NSPrivacyAccessedAPICategorySystemBootTime");
                d.CreateArray("NSPrivacyAccessedAPITypeReasons").AddString("35F9.1");
            }
            doc.WriteToFile(file);
        }
    }
}
#endif
