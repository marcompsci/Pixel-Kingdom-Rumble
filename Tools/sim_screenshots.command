#!/bin/bash
# Pixel Kingdom Rumble: App Store screenshots from the iOS Simulator (macOS).
# Builds a release-style Simulator build, boots an iPhone 17 Pro Max simulator (6.9" display), opens a list of
# scenes with the -pkrScene launch argument and saves a screenshot of each to Logs/shots/.
# Run directly or via Logs/shots_request with the watcher. Results: Logs/shots/SUMMARY.txt.

cd "$(dirname "$0")/.." || exit 1
pause_end() { [ -z "$PKR_NO_PAUSE" ] && read -r -p "Press Return to close." _; }
PROJ="$(pwd)"
OUT="$PROJ/Logs/shots"
mkdir -p "$OUT"; rm -f "$OUT"/*
say() { echo "$@" | tee -a "$OUT/SUMMARY.txt"; }
BUNDLE="com.marcompsci.pixelkingdomrumble"
DEVICE_NAME="${PKR_SIM_DEVICE:-iPhone 17 Pro Max}"
SCENES="${PKR_SHOT_SCENES:-01_MainMenu 03_LevelSelect SQ_SunspireHeights SQ_RooftopRun SQ_FrostgearAscent MS_FirstLight 05_ShadowContracts VS_Dojo}"
export PKR_RELEASE=1 PKR_SIMULATOR=1

UNITY="$(ls -d /Applications/Unity/Hub/Editor/*/Unity.app/Contents/MacOS/Unity 2>/dev/null | sort -V | tail -1)"
say "PKR simulator screenshots  $(date)"
say "[1/4] Unity: exporting the Simulator Xcode project..."
"$UNITY" -batchmode -nographics -projectPath "$PROJ" -buildTarget iOS -logFile "$OUT/unity.log" \
  -executeMethod PKR.EditorTools.BatchCheck.BuildIOS
if ! grep -q "\[PKR BuildIOS\] OK" "$OUT/unity.log"; then
  say "      FAILED (Logs/shots/unity.log)"; grep -E "PKR BuildIOS|error CS" "$OUT/unity.log" | head -10 | tee -a "$OUT/SUMMARY.txt"; pause_end; exit 1
fi
say "      OK"

say "[2/4] Xcode: building for the Simulator..."
xcodebuild -project Builds/iOSSim/Unity-iPhone.xcodeproj -scheme Unity-iPhone -configuration Release \
  -sdk iphonesimulator -destination "generic/platform=iOS Simulator" -derivedDataPath Builds/SimDerived \
  CODE_SIGNING_ALLOWED=NO build > "$OUT/xcodebuild.log" 2>&1
if ! grep -q "BUILD SUCCEEDED" "$OUT/xcodebuild.log"; then
  say "      FAILED (Logs/shots/xcodebuild.log)"; grep -E "error:" "$OUT/xcodebuild.log" | sort -u | head -10 | tee -a "$OUT/SUMMARY.txt"; pause_end; exit 1
fi
APP="$(ls -d Builds/SimDerived/Build/Products/Release-iphonesimulator/*.app | head -1)"
say "      OK: $APP"

say "[3/4] Booting '$DEVICE_NAME'..."
UDID="$(xcrun simctl list devices available | grep -F "$DEVICE_NAME (" | head -1 | grep -oE '[0-9A-F-]{36}')"
if [ -z "$UDID" ]; then say "      No simulator named '$DEVICE_NAME'. Available:"; xcrun simctl list devices available | grep iPhone | tee -a "$OUT/SUMMARY.txt"; pause_end; exit 1; fi
xcrun simctl boot "$UDID" 2>/dev/null; xcrun simctl bootstatus "$UDID" -b > /dev/null 2>&1
xcrun simctl status_bar "$UDID" override --time "9:41" --batteryState charged --batteryLevel 100 --wifiBars 3 --cellularBars 4 2>/dev/null
xcrun simctl uninstall "$UDID" "$BUNDLE" 2>/dev/null
xcrun simctl install "$UDID" "$APP"
say "      OK: $UDID"

say "[4/4] Screenshots..."
n=0
for S in $SCENES; do
  n=$((n+1))
  xcrun simctl terminate "$UDID" "$BUNDLE" 2>/dev/null; sleep 1
  xcrun simctl launch "$UDID" "$BUNDLE" -pkrScene "$S" > /dev/null
  sleep "${PKR_SHOT_WAIT:-9}"
  xcrun simctl io "$UDID" screenshot "$OUT/$(printf %02d $n)_$S.png" > /dev/null 2>&1 && say "      $S" || say "      $S FAILED"
done
xcrun simctl terminate "$UDID" "$BUNDLE" 2>/dev/null
say ""
say "DONE."
pause_end
