#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;
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
        /// <summary>Shared by the game and the Quick Game widget (the run in progress, its stats, the hero to draw).</summary>
        public const string AppGroup = "group.com.marcompsci.pixelkingdomrumble";
        const string WidgetName = "QuickGameWidget";

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
            AddQuickGameWidget(proj, appTarget, path);
            proj.WriteToFile(projPath);
            DeclareFrameworkReasons(path);

            var caps = new ProjectCapabilityManager(projPath, "Unity-iPhone/" + EntitlementsName, null, appTarget);
            caps.AddGameCenter();
            caps.AddInAppPurchase();
            caps.AddAppGroups(new[] { AppGroup });
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
            // iPad: the whole game runs in landscape (OrientationController), so iPad declares only the landscape sides.
            var ipad = plist.root.CreateArray("UISupportedInterfaceOrientations~ipad");
            foreach (var o in new[] { "UIInterfaceOrientationLandscapeLeft", "UIInterfaceOrientationLandscapeRight" })
                ipad.AddString(o);
            plist.WriteToFile(plistPath);

            Debug.Log("[PKR iOS] Post-build: Quick Game widget extension + App Group, Game Center + In-App Purchase capabilities, ITSAppUsesNonExemptEncryption=NO, photo library add usage, privacy manifests.");
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
        /// The Quick Game widget (Native/QuickGameWidget): a WidgetKit extension (iOS 17+, interactive buttons) embedded
        /// in the app. Its Swift files, art catalog, privacy manifest and App Group entitlement are copied next to the
        /// Xcode project and get their own target; the app keeps its own deployment target.
        /// </summary>
        static void AddQuickGameWidget(PBXProject proj, string appTarget, string path)
        {
            string src = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Native", WidgetName));
            if (!Directory.Exists(src)) { Debug.LogWarning("[PKR iOS] Native/QuickGameWidget is missing: no widget in this build."); return; }
            string dst = Path.Combine(path, WidgetName);
            if (Directory.Exists(dst)) Directory.Delete(dst, true);
            CopyDir(src, dst);

            string version = PlayerSettings.bundleVersion;
            string build = string.IsNullOrEmpty(PlayerSettings.iOS.buildNumber) ? "1" : PlayerSettings.iOS.buildNumber;
            var info = new PlistDocument();
            info.ReadFromFile(Path.Combine(dst, "Info.plist"));
            info.root.SetString("CFBundleShortVersionString", version); // must match the app's for App Store validation
            info.root.SetString("CFBundleVersion", build);
            info.WriteToFile(Path.Combine(dst, "Info.plist"));

            string pbxText = File.ReadAllText(PBXProject.GetPBXProjectPath(path));
            if (pbxText.Contains("/* " + WidgetName + " */ = {\n\t\t\tisa = PBXNativeTarget") || pbxText.Contains(WidgetName + ".appex"))
            {
                Debug.Log("[PKR iOS] Quick Game widget target already in the project (append build).");
                return;
            }
            string bundleId = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS) + ".quickgame";
            string ext = proj.AddAppExtension(appTarget, WidgetName, bundleId, WidgetName + "/Info.plist");
            proj.AddSourcesBuildPhase(ext);
            proj.AddResourcesBuildPhase(ext);
            proj.AddFrameworksBuildPhase(ext);
            foreach (var f in Directory.GetFiles(dst, "*.swift"))
            {
                string rel = WidgetName + "/" + Path.GetFileName(f);
                proj.AddFileToBuild(ext, proj.AddFile(rel, rel));
            }
            foreach (var name in new[] { "Assets.xcassets", "PrivacyInfo.xcprivacy" })
            {
                string rel = WidgetName + "/" + name;
                proj.AddFileToBuild(ext, proj.AddFile(rel, rel));
            }
            proj.AddFile(WidgetName + "/" + WidgetName + ".entitlements", WidgetName + "/" + WidgetName + ".entitlements");
            proj.AddFile(WidgetName + "/Info.plist", WidgetName + "/Info.plist");
            foreach (var fw in new[] { "WidgetKit.framework", "SwiftUI.framework", "AppIntents.framework" })
                proj.AddFrameworkToProject(ext, fw, false);

            void Set(string key, string value) => proj.SetBuildProperty(ext, key, value);
            Set("PRODUCT_NAME", "$(TARGET_NAME)");
            Set("PRODUCT_BUNDLE_IDENTIFIER", bundleId);
            Set("INFOPLIST_FILE", WidgetName + "/Info.plist");
            Set("GENERATE_INFOPLIST_FILE", "NO");
            Set("CODE_SIGN_ENTITLEMENTS", WidgetName + "/" + WidgetName + ".entitlements");
            Set("CODE_SIGN_STYLE", "Automatic");
            Set("DEVELOPMENT_TEAM", PlayerSettings.iOS.appleDeveloperTeamID);
            Set("SWIFT_VERSION", "5.0");
            Set("IPHONEOS_DEPLOYMENT_TARGET", "17.0"); // interactive widgets (Button with an App Intent)
            Set("TARGETED_DEVICE_FAMILY", "1,2");
            Set("SDKROOT", "iphoneos");
            Set("SKIP_INSTALL", "YES");
            Set("APPLICATION_EXTENSION_API_ONLY", "YES");
            Set("ENABLE_BITCODE", "NO");
            Set("CLANG_ENABLE_MODULES", "YES");
            Set("SWIFT_EMIT_LOC_STRINGS", "NO");
            Set("ASSETCATALOG_COMPILER_APPICON_NAME", "");
            Set("LD_RUNPATH_SEARCH_PATHS", "$(inherited) @executable_path/Frameworks @executable_path/../../Frameworks");
            Set("MARKETING_VERSION", version);
            Set("CURRENT_PROJECT_VERSION", build);
            Debug.Log($"[PKR iOS] Quick Game widget extension added ({bundleId}, iOS 17+).");
        }

        static void CopyDir(string from, string to)
        {
            Directory.CreateDirectory(to);
            foreach (var f in Directory.GetFiles(from))
            {
                if (f.EndsWith(".meta") || Path.GetFileName(f) == ".DS_Store") continue;
                File.Copy(f, Path.Combine(to, Path.GetFileName(f)), true);
            }
            foreach (var d in Directory.GetDirectories(from)) CopyDir(d, Path.Combine(to, Path.GetFileName(d)));
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
            // The game reads the Quick Game widget's stats from the shared App Group (PKRWidget.mm): UserDefaults 1C8F.1.
            PlistElementDict ud = null;
            foreach (var e in types.values)
                if (e is PlistElementDict ed && ed["NSPrivacyAccessedAPIType"] != null &&
                    ed["NSPrivacyAccessedAPIType"].AsString() == "NSPrivacyAccessedAPICategoryUserDefaults") ud = ed;
            if (ud == null)
            {
                ud = types.AddDict();
                ud.SetString("NSPrivacyAccessedAPIType", "NSPrivacyAccessedAPICategoryUserDefaults");
            }
            var reasons = ud["NSPrivacyAccessedAPITypeReasons"] as PlistElementArray ?? ud.CreateArray("NSPrivacyAccessedAPITypeReasons");
            bool hasGroup = false;
            foreach (var r in reasons.values) if (r.AsString() == "1C8F.1") hasGroup = true;
            if (!hasGroup) reasons.AddString("1C8F.1");
            doc.WriteToFile(file);
        }
    }
}
#endif
