#!/usr/bin/env python3
"""
Apple compliance gate for Pixel Kingdom Rumble (static checks; no Unity licence needed).

  python3 Tools/compliance_check.py                 # repository checks (CI and the Mac check)
  python3 Tools/compliance_check.py --xcode Builds/iOS   # + checks on an exported Xcode project (Mac, after a build)

Prints one PASS / WARN / FAIL / SKIP line per check and exits 1 if anything FAILs. A check that cannot run is SKIP,
never PASS. What each check enforces and why is listed in Docs/apple-compliance-report.md.
"""
import glob, os, plistlib, re, subprocess, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
S = os.path.join(ROOT, "Assets", "_Project", "Scripts")
results = []


def res(level, name, detail=""):
    results.append(level)
    print(f"{level:4}  {name}" + (f"  ·  {detail}" if detail else ""))


def read(p):
    try:
        return open(os.path.join(ROOT, p), encoding="utf-8", errors="replace").read()
    except FileNotFoundError:
        return None


def tracked_files():
    try:
        out = subprocess.run(["git", "-C", ROOT, "ls-files"], capture_output=True, text=True, check=True).stdout
        return [l for l in out.splitlines() if l]
    except Exception:
        return None


def cs_files():
    return glob.glob(os.path.join(ROOT, "Assets", "**", "*.cs"), recursive=True)


# ---------------------------------------------------------------------------------------------------------------
def check_secrets():
    files = tracked_files()
    if files is None:
        res("SKIP", "Secrets", "not a git checkout"); return
    bad_ext = [f for f in files if f.lower().endswith((".p8", ".p12", ".pem", ".mobileprovision", ".cer", ".keystore", ".jks"))]
    res("FAIL" if bad_ext else "PASS", "No signing keys or certificates in the repo", ", ".join(bad_ext[:5]))
    patterns = [
        (r"-----BEGIN (?:EC |RSA )?PRIVATE KEY-----", "private key"),
        (r"AKIA[0-9A-Z]{16}", "AWS key"),
        (r"sk_live_[0-9a-zA-Z]{16,}", "Stripe live key"),
        (r"AIza[0-9A-Za-z\-_]{35}", "Google API key"),
        (r"ghp_[0-9A-Za-z]{36}", "GitHub token"),
        (r"(?i)(api[_-]?key|secret|password|auth[_-]?token)\s*[:=]\s*[\"'][A-Za-z0-9_\-/+]{16,}[\"']", "hard-coded credential"),
    ]
    hits = []
    for f in files:
        if f.startswith(("Library/", "Logs/")) or f.lower().endswith((".png", ".wav", ".ttf", ".bundle", ".mov", ".mp4", ".unity", ".asset", ".meta", ".json")):
            continue
        if f == "Tools/compliance_check.py":
            continue
        txt = read(f)
        if not txt:
            continue
        for pat, what in patterns:
            if re.search(pat, txt):
                hits.append(f"{f} ({what})")
    res("FAIL" if hits else "PASS", "No secrets in tracked text files", "; ".join(hits[:5]))


def check_tester_code():
    bad = []
    for f in cs_files():
        if "/Editor/" in f.replace("\\", "/"):
            continue   # editor scripts never ship
        lines = open(f, encoding="utf-8", errors="replace").read().splitlines()
        depth_ok = []
        for i, line in enumerate(lines):
            st = line.strip()
            if st.startswith("#if"):
                depth_ok.append("PKR_TESTER" in st)
            elif st.startswith("#endif") and depth_ok:
                depth_ok.pop()
            elif st.startswith("#else") and depth_ok:
                depth_ok[-1] = False
            if "TESTER BUILD" in line.split("//")[0] and '"' in line.split("//")[0] and not any(depth_ok):
                bad.append(f"{os.path.relpath(f, ROOT)}:{i + 1}")
    res("FAIL" if bad else "PASS", "Tester-only unlocks compiled out of release builds (#if PKR_TESTER)", ", ".join(bad))
    tf = read("Tools/testflight_upload.command") or ""
    res("PASS" if "PKR_RELEASE=1" in tf else "FAIL", "TestFlight/App Store builds set PKR_RELEASE=1")
    bc = read("Assets/_Project/Scripts/Editor/BatchCheck.cs") or ""
    res("PASS" if re.search(r'PKR_RELEASE"\)\s*==\s*"1"\s*\?\s*new string\[0\]', bc) else "FAIL", "PKR_TESTER is only defined when PKR_RELEASE is not 1")


def check_post_build():
    pb = read("Assets/_Project/Scripts/Editor/IOSPostBuild.cs") or ""
    code = "\n".join(open(f, encoding="utf-8", errors="replace").read() for f in cs_files())
    mm = "\n".join(open(f, encoding="utf-8", errors="replace").read() for f in glob.glob(os.path.join(ROOT, "Assets", "Plugins", "iOS", "*.mm")))
    res("PASS" if 'SetBoolean("ITSAppUsesNonExemptEncryption", false)' in pb else "FAIL", "Export compliance key set (ITSAppUsesNonExemptEncryption = NO)")
    need = {
        "NSPhotoLibraryAddUsageDescription": "UIActivityViewController" in mm,
        "NSGKFriendListUsageDescription": "GKLeaderboardPlayerScopeFriendsOnly" in mm or "loadFriends" in mm,
        "NSCameraUsageDescription": "AVCaptureDevice" in mm or "WebCamTexture" in code,
        "NSMicrophoneUsageDescription": "Microphone." in code or "AVAudioRecorder" in mm,
        "NSLocationWhenInUseUsageDescription": "Input.location" in code or "CLLocationManager" in mm,
        "NSUserTrackingUsageDescription": "ATTrackingManager" in mm,
    }
    for key, used in need.items():
        if used:
            res("PASS" if key in pb else "FAIL", f"Purpose string for a used API: {key}")
    res("FAIL" if "ATTrackingManager" in mm else "PASS", "No App Tracking Transparency / tracking code")
    everything = pb + mm + code
    res("FAIL" if "NSAllowsArbitraryLoads" in everything else "PASS", "App Transport Security not weakened (no NSAllowsArbitraryLoads)")
    res("PASS" if "NSPrivacyTracking" in pb and "NSPrivacyCollectedDataTypes" in pb else "FAIL", "App privacy manifest written by the post-build step")
    res("PASS" if "NSPrivacyAccessedAPICategorySystemBootTime" in pb else "WARN", "Required-reason API: system boot time declared for UnityFramework")


def check_privacy_policy():
    p = read("PRIVACY.md")
    if not p:
        res("FAIL", "PRIVACY.md exists"); return
    mm = "\n".join(open(f, encoding="utf-8", errors="replace").read() for f in glob.glob(os.path.join(ROOT, "Assets", "Plugins", "iOS", "*.mm")))
    res("PASS" if "Last updated" in p else "WARN", "Privacy policy has a 'Last updated' date")
    if "GKMatch" in mm:
        res("PASS" if re.search(r"(?i)online co-?op|multiplayer", p) else "FAIL", "Privacy policy covers online multiplayer (GameKit matches)")
    if "FriendsOnly" in mm:
        res("PASS" if re.search(r"(?i)friends", p) else "FAIL", "Privacy policy covers Game Center friends access")
    if "UIActivityViewController" in mm:
        res("PASS" if re.search(r"(?i)photo", p) else "FAIL", "Privacy policy covers saving to Photos")
    sdk = ("Firebase", "UnityEngine.Analytics", "Unity.Services.Analytics", "AppsFlyer", "Adjust.start", "FB.Init",
           "GoogleMobileAds", "Amplitude.Instance", "Mixpanel", "UnityEngine.Advertisements", "IronSource", "AppLovin")
    allcode = "\n".join(open(f, encoding="utf-8", errors="replace").read() for f in cs_files())
    manifest = read("Packages/manifest.json") or ""
    analytics = [x for x in sdk if x in allcode] + [x for x in ("com.unity.services.analytics", "com.unity.ads", "com.google.firebase") if x in manifest]
    res("FAIL" if analytics else "PASS", "No analytics/ads/tracking SDKs (the policy and App Privacy label say none)", ", ".join(analytics))
    res("PASS" if read("SUPPORT.md") else "FAIL", "SUPPORT.md (support URL target) exists")


def check_assets():
    credits = read("Docs/CREDITS_AND_LICENSES.md") or ""
    odd = []
    for f in glob.glob(os.path.join(ROOT, "Assets", "**", "*"), recursive=True):
        ext = os.path.splitext(f)[1].lower()
        rel = os.path.relpath(f, ROOT)
        if ext in (".ttf", ".otf"):
            if os.path.basename(f) not in credits:
                odd.append(rel)
        elif ext in (".mp3", ".ogg", ".aiff", ".aif", ".m4a", ".fbx", ".obj", ".blend", ".psd"):
            odd.append(rel)
        elif ext == ".wav" and "Resources/Audio" not in rel.replace("\\", "/"):
            odd.append(rel)
    res("FAIL" if odd else "PASS", "Every font/audio/model file is accounted for in Docs/CREDITS_AND_LICENSES.md", ", ".join(odd[:5]))
    res("PASS" if os.path.exists(os.path.join(ROOT, "Tools", "make_audio.py")) else "WARN", "Audio is generated by Tools/make_audio.py")


BLOCK = ["Mario", "Nintendo", "Smash Bros", "Pokémon", "Pokemon", "Zelda", "Donkey Kong", "Call of Duty", "CODM", "Fortnite",
         "Minecraft", "Sonic the", "Mega Man", "Metroid", "Kirby", "Pac-Man", "Tetris", "Street Fighter", "Mortal Kombat"]


def check_ip():
    hits = []
    strre = re.compile(r'"((?:[^"\\]|\\.)*)"')
    for f in cs_files():
        if "/Tests/" in f.replace("\\", "/"):
            continue
        for i, line in enumerate(open(f, encoding="utf-8", errors="replace")):
            code = line.split("//")[0]
            for m in strre.finditer(code):
                for w in BLOCK:
                    if w.lower() in m.group(1).lower():
                        hits.append(f"{os.path.relpath(f, ROOT)}:{i + 1} '{w}'")
    for f in glob.glob(os.path.join(ROOT, "Assets", "_Project", "Resources", "**", "*.json"), recursive=True) + \
             [os.path.join(ROOT, "Docs", "AppStore", n) for n in ("LISTING.md", "whats-to-test.txt")] + \
             [os.path.join(ROOT, "Docs", n) for n in ("app-review-notes.md", "external-tester-guide.md")]:
        if not os.path.exists(f):
            continue
        txt = open(f, encoding="utf-8", errors="replace").read().lower()
        for w in BLOCK:
            if re.search(r"\b" + re.escape(w.lower()) + r"\b", txt):
                hits.append(f"{os.path.relpath(f, ROOT)} '{w}'")
    res("FAIL" if hits else "PASS", "No third-party game names in player-facing text or store metadata", "; ".join(hits[:6]))


def check_store():
    ps = read("Assets/_Project/Scripts/Runtime/Store/PurchaseService.cs") or ""
    ui = read("Assets/_Project/Scripts/Runtime/UI/MissionBoardUI.cs") or ""
    res("PASS" if "RESTORE PURCHASE" in ui and "Restore(" in ps else "FAIL", "Non-consumable purchase can be restored in the app")
    iap = read("Assets/_Project/IAP/UnityIapBackend.cs") or ""
    res("PASS" if "OnPurchaseDeferred" in iap and "OnPurchaseFailed" in iap else "FAIL", "Failed and Ask-to-Buy (deferred) purchases are handled")
    res("FAIL" if re.search(r"(?i)https?://[^\"]*(checkout|paypal|stripe|buy)", ps + ui + iap) else "PASS", "No external checkout links for digital goods")


def check_versions():
    ps = read("ProjectSettings/ProjectSettings.asset") or ""
    m = re.search(r"bundleVersion: (\S+)", ps)
    res("PASS" if m else "FAIL", "Marketing version set in ProjectSettings", m.group(1) if m else "")
    td = re.search(r"targetDevice: (\d)", ps)
    fam = {"0": "iPhone only", "1": "iPad only", "2": "iPhone + iPad"}.get(td.group(1) if td else "", "unknown")
    if td and td.group(1) == "2":
        oc = read("Assets/_Project/Scripts/Runtime/Bootstrap/OrientationController.cs") or ""
        pb = read("Assets/_Project/Scripts/Editor/IOSPostBuild.cs") or ""
        handled = "DeviceIdiom.IsPad" in oc and "UISupportedInterfaceOrientations~ipad" in pb
        shots = glob.glob(os.path.join(ROOT, "Docs", "AppStore", "screenshots", "ipad13", "*.png"))
        res("PASS" if handled and shots else "FAIL", "Device family", fam + f": iPad orientation handled={handled}, 13\" iPad screenshots in repo={len(shots)} (keep them current)")
    else:
        res("PASS", "Device family", fam)
    res("PASS" if "appleDeveloperTeamID: X6LZQ3FS36" in ps else "WARN", "Apple team set (automatic signing)")


def check_xcode(path):
    path = os.path.join(ROOT, path) if not os.path.isabs(path) else path
    if not os.path.isdir(path):
        res("SKIP", "Exported Xcode project", f"{path} not found (run a build on the Mac first)"); return
    info = plistlib.load(open(os.path.join(path, "Info.plist"), "rb"))
    res("PASS" if info.get("ITSAppUsesNonExemptEncryption") is False else "FAIL", "Info.plist: ITSAppUsesNonExemptEncryption = NO")
    for k in ("NSPhotoLibraryAddUsageDescription", "NSGKFriendListUsageDescription"):
        res("PASS" if info.get(k) else "FAIL", f"Info.plist: {k}")
    ats = info.get("NSAppTransportSecurity", {})
    res("FAIL" if ats.get("NSAllowsArbitraryLoads") else "PASS", "Info.plist: ATS not disabled")
    res("PASS" if info.get("CFBundleShortVersionString") else "FAIL", "Info.plist: version", f"{info.get('CFBundleShortVersionString')} ({info.get('CFBundleVersion')})")
    for rel in ("Unity-iPhone/PrivacyInfo.xcprivacy", "UnityFramework/PrivacyInfo.xcprivacy"):
        p = os.path.join(path, rel)
        if not os.path.exists(p):
            res("FAIL", f"Privacy manifest {rel}"); continue
        d = plistlib.load(open(p, "rb"))
        res("PASS" if d.get("NSPrivacyTracking") is False else "FAIL", f"Privacy manifest {rel}: tracking = false")
    pbx = open(os.path.join(path, "Unity-iPhone.xcodeproj", "project.pbxproj"), encoding="utf-8", errors="replace").read()
    res("PASS" if "PrivacyInfo.xcprivacy in Resources" in pbx else "FAIL", "App privacy manifest is in the app target's resources")
    ent = glob.glob(os.path.join(path, "Unity-iPhone", "*.entitlements"))
    gc = ent and plistlib.load(open(ent[0], "rb")).get("com.apple.developer.game-center")
    res("PASS" if gc else "FAIL", "Entitlements: Game Center")
    fam = re.findall(r'TARGETED_DEVICE_FAMILY = "?([0-9,]+)"?', pbx)
    res("INFO", "TARGETED_DEVICE_FAMILY", ", ".join(sorted(set(fam))))
    dt = sorted(set(re.findall(r"IPHONEOS_DEPLOYMENT_TARGET = ([0-9.]+)", pbx)))
    res("INFO", "Deployment target", ", ".join(dt))


def main():
    print("PKR Apple compliance gate")
    check_secrets(); check_tester_code(); check_post_build(); check_privacy_policy(); check_assets(); check_ip()
    check_store(); check_versions()
    if "--xcode" in sys.argv:
        check_xcode(sys.argv[sys.argv.index("--xcode") + 1])
    f, w = results.count("FAIL"), results.count("WARN")
    print(f"RESULT: {'FAIL' if f else 'PASS'} ({f} FAIL, {w} WARN, {results.count('SKIP')} SKIP)")
    return 1 if f else 0


if __name__ == "__main__":
    sys.exit(main())
