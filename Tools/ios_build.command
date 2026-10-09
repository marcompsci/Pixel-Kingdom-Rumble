#!/bin/bash
# Pixel Kingdom Rumble: build for iPhone and install on a connected iPhone (macOS).
# Double-click (project closed in the Unity Editor), or let unity_check_watch.command run it when
# Logs/ios_request exists. Steps:
#   1. Unity exports the Xcode project to Builds/iOS (batch mode, iOS target)
#   2. xcodebuild compiles it with automatic signing (team X6LZQ3FS36; Xcode must be signed in to that account)
#   3. if an iPhone is connected (USB, or Wi-Fi paired in Xcode) it is installed and launched
# Results: Logs/ios/SUMMARY.txt (+ unity.log, xcodebuild.log, install.log).

cd "$(dirname "$0")/.." || exit 1
pause_end() { [ -z "$PKR_NO_PAUSE" ] && read -r -p "Press Return to close." _; }
PROJ="$(pwd)"
OUT="$PROJ/Logs/ios"
mkdir -p "$OUT"; rm -f "$OUT"/*
say() { echo "$@" | tee -a "$OUT/SUMMARY.txt"; }
TEAM="X6LZQ3FS36"
BUNDLE="com.marcompsci.pixelkingdomrumble"

UNITY="$(ls -d /Applications/Unity/Hub/Editor/*/Unity.app/Contents/MacOS/Unity 2>/dev/null | sort -V | tail -1)"
say "PKR iOS build  $(date)"
say "Unity: $UNITY"

say "[1/3] Unity: exporting the Xcode project..."
"$UNITY" -batchmode -nographics -projectPath "$PROJ" -buildTarget iOS -logFile "$OUT/unity.log" \
  -executeMethod PKR.EditorTools.BatchCheck.BuildIOS
if ! grep -q "\[PKR BuildIOS\] OK" "$OUT/unity.log"; then
  say "      FAILED (Logs/ios/unity.log)"
  grep -E "PKR BuildIOS|error CS|Error building|BuildFailedException" "$OUT/unity.log" | head -20 | tee -a "$OUT/SUMMARY.txt"
  pause_end; exit 1
fi
say "      OK"

say "[2/3] Xcode: compiling and signing (team $TEAM)..."
xcodebuild -project Builds/iOS/Unity-iPhone.xcodeproj -scheme Unity-iPhone -configuration Release \
  -destination "generic/platform=iOS" -derivedDataPath Builds/DerivedData -allowProvisioningUpdates \
  DEVELOPMENT_TEAM="$TEAM" CODE_SIGN_STYLE=Automatic build > "$OUT/xcodebuild.log" 2>&1
if ! grep -q "BUILD SUCCEEDED" "$OUT/xcodebuild.log"; then
  say "      FAILED (Logs/ios/xcodebuild.log)"
  grep -E "error:|BUILD FAILED" "$OUT/xcodebuild.log" | sort -u | head -20 | tee -a "$OUT/SUMMARY.txt"
  pause_end; exit 1
fi
APP="$(ls -d Builds/DerivedData/Build/Products/Release-iphoneos/*.app 2>/dev/null | head -1)"
say "      OK: $APP"

say "[3/3] Installing on a connected iPhone..."
xcrun devicectl list devices > "$OUT/devices.txt" 2>&1
# Column 4 is the state; "unavailable" means asleep, locked, or not connected.
DEVICE="$(grep -i "iphone" "$OUT/devices.txt" | grep -viE "unavailable" | grep -iE "available|connected" \
  | grep -oE "[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12}" | head -1)"
if [ -z "$DEVICE" ]; then
  say "      No reachable iPhone (Logs/ios/devices.txt lists them; 'unavailable' = not connected or asleep)."
  say "      Plug the iPhone into the Mac with a cable, unlock it, tap Trust if asked, and make sure Developer Mode"
  say "      is on (Settings > Privacy & Security). Then run this again; the app is already built."
  pause_end; exit 2
fi
if xcrun devicectl device install app --device "$DEVICE" "$APP" > "$OUT/install.log" 2>&1; then
  say "      Installed on $DEVICE. Launching; recording the game's log for 90 s (Logs/ios/device_console.log)..."
  xcrun devicectl device process launch --console --terminate-existing --device "$DEVICE" "$BUNDLE" \
    > "$OUT/device_console.log" 2>&1 &
  PID=$!; sleep 90; kill $PID 2>/dev/null
  # Stopping the log stream also closes the game, so start it again (no log) for the player.
  sleep 2; xcrun devicectl device process launch --device "$DEVICE" "$BUNDLE" > /dev/null 2>&1
  say "      Launched."
else say "      Install FAILED (Logs/ios/install.log)"; tail -5 "$OUT/install.log" | tee -a "$OUT/SUMMARY.txt"; fi

say ""
say "DONE."
pause_end
