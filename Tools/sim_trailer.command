#!/bin/bash
# Pixel Kingdom Rumble: zombie trailer footage + App Store screenshots from the iOS Simulator (macOS).
# Builds a Simulator build, boots an iPhone 17 Pro Max, and for each segment opens a scene with -pkrScene, lets it
# settle, records the screen (xcrun simctl io recordVideo) and takes two screenshots. Segments where the game plays
# itself: EV_LastStand@demo, EV_LastStand@pierdemo, EV_ZombieLand@isle. Results: Logs/trailer/ (seg_*.mov, shot_*.png).
# Run via Logs/trailer_request + Logs/check_request with the watcher. Claude cuts the clip together afterwards.

cd "$(dirname "$0")/.." || exit 1
pause_end() { [ -z "$PKR_NO_PAUSE" ] && read -r -p "Press Return to close." _; }
PROJ="$(pwd)"
OUT="$PROJ/Logs/trailer"
mkdir -p "$OUT"; rm -f "$OUT"/*
say() { echo "$@" | tee -a "$OUT/SUMMARY.txt"; }
BUNDLE="com.marcompsci.pixelkingdomrumble"
DEVICE_NAME="${PKR_SIM_DEVICE:-iPhone 17 Pro Max}"
SEGMENTS="${PKR_TRAILER_SEGMENTS:-01_MainMenu@zombies:6 EV_LastStand@demo:16 EV_LastStand@pierdemo:16 EV_ZombieLand@isle:12}"
[ -f "$PROJ/Logs/trailer_segments" ] && SEGMENTS="$(cat "$PROJ/Logs/trailer_segments")" && rm -f "$PROJ/Logs/trailer_segments"
export PKR_RELEASE=1 PKR_SIMULATOR=1

UNITY="$(ls -d /Applications/Unity/Hub/Editor/*/Unity.app/Contents/MacOS/Unity 2>/dev/null | sort -V | tail -1)"
say "PKR simulator trailer footage  $(date)"
say "[1/4] Unity: exporting the Simulator Xcode project..."
"$UNITY" -batchmode -nographics -projectPath "$PROJ" -buildTarget iOS -logFile "$OUT/unity.log" \
  -executeMethod PKR.EditorTools.BatchCheck.BuildIOS
if ! grep -q "\[PKR BuildIOS\] OK" "$OUT/unity.log"; then
  say "      FAILED (Logs/trailer/unity.log)"; grep -E "PKR BuildIOS|error CS" "$OUT/unity.log" | head -10 | tee -a "$OUT/SUMMARY.txt"; pause_end; exit 1
fi
say "      OK"
say "[2/4] Xcode: building for the Simulator..."
xcodebuild -project Builds/iOSSim/Unity-iPhone.xcodeproj -scheme Unity-iPhone -configuration Release \
  -sdk iphonesimulator -destination "generic/platform=iOS Simulator" -derivedDataPath Builds/SimDerived \
  CODE_SIGNING_ALLOWED=NO build > "$OUT/xcodebuild.log" 2>&1
if ! grep -q "BUILD SUCCEEDED" "$OUT/xcodebuild.log"; then
  say "      FAILED (Logs/trailer/xcodebuild.log)"; grep -E "error:" "$OUT/xcodebuild.log" | sort -u | head -10 | tee -a "$OUT/SUMMARY.txt"; pause_end; exit 1
fi
APP="$(ls -d Builds/SimDerived/Build/Products/Release-iphonesimulator/*.app | head -1)"
say "      OK: $APP"
say "[3/4] Booting '$DEVICE_NAME'..."
UDID="$(xcrun simctl list devices available | grep -F "$DEVICE_NAME (" | head -1 | grep -oE '[0-9A-F-]{36}')"
if [ -z "$UDID" ]; then say "      No simulator named '$DEVICE_NAME'."; pause_end; exit 1; fi
xcrun simctl boot "$UDID" 2>/dev/null; xcrun simctl bootstatus "$UDID" -b > /dev/null 2>&1
xcrun simctl status_bar "$UDID" override --time "9:41" --batteryState charged --batteryLevel 100 --wifiBars 3 --cellularBars 4 2>/dev/null
xcrun simctl uninstall "$UDID" "$BUNDLE" 2>/dev/null
xcrun simctl install "$UDID" "$APP"
say "      OK: $UDID"
say "[4/4] Recording..."
xcrun simctl launch "$UDID" "$BUNDLE" > /dev/null; sleep 25
n=0
for SEG in $SEGMENTS; do
  n=$((n+1))
  S="${SEG%%:*}"; D="${SEG##*:}"
  NAME="$(printf %02d $n)_$(echo "$S" | tr '@' '_')"
  xcrun simctl terminate "$UDID" "$BUNDLE" 2>/dev/null; sleep 1
  SIMCTL_CHILD_PKR_SCENE="$S" xcrun simctl launch "$UDID" "$BUNDLE" -pkrScene "$S" > /dev/null
  sleep "${PKR_TRAILER_SETTLE:-14}"
  xcrun simctl io "$UDID" recordVideo --codec=h264 --force "$OUT/seg_$NAME.mov" > /dev/null 2>&1 &
  REC=$!
  sleep $((D / 2)); xcrun simctl io "$UDID" screenshot "$OUT/shot_${NAME}_a.png" > /dev/null 2>&1
  sleep $((D - D / 2)); xcrun simctl io "$UDID" screenshot "$OUT/shot_${NAME}_b.png" > /dev/null 2>&1
  kill -INT $REC 2>/dev/null; wait $REC 2>/dev/null
  if [ -s "$OUT/seg_$NAME.mov" ]; then say "      $S  ${D}s  OK"; else say "      $S  FAILED"; fi
  if xcrun simctl spawn "$UDID" launchctl list 2>/dev/null | grep -q "$BUNDLE"; then say "        (running)"; else say "        (NOT RUNNING)"; fi
done
xcrun simctl terminate "$UDID" "$BUNDLE" 2>/dev/null
say ""
say "DONE."
pause_end
