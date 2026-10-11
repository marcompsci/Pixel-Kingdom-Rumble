#!/bin/bash
# Pixel Kingdom Rumble: build a RELEASE (no tester unlock button) and upload it to App Store Connect / TestFlight.
# Double-click (project closed in the Unity Editor), or let unity_check_watch.command run it when
# Logs/testflight_request exists. Uses the Apple ID signed in to Xcode (Settings > Accounts) for team X6LZQ3FS36.
#   1. Unity exports the Xcode project (PKR_RELEASE=1, new build number = timestamp)
#   2. xcodebuild archive (automatic signing, Apple Distribution)
#   3. xcodebuild -exportArchive with destination=upload sends it to App Store Connect
# Apple then processes the build (usually 5-30 minutes) before it shows in TestFlight.
# Results: Logs/testflight/SUMMARY.txt (+ unity.log, archive.log, upload.log).

cd "$(dirname "$0")/.." || exit 1
pause_end() { [ -z "$PKR_NO_PAUSE" ] && read -r -p "Press Return to close." _; }
PROJ="$(pwd)"
OUT="$PROJ/Logs/testflight"
mkdir -p "$OUT"; rm -f "$OUT"/*
say() { echo "$@" | tee -a "$OUT/SUMMARY.txt"; }
TEAM="X6LZQ3FS36"
export PKR_RELEASE=1
export PKR_BUILD_NUMBER="${PKR_BUILD_NUMBER:-$(date +%Y%m%d).$(date +%H%M)}"

UNITY="$(ls -d /Applications/Unity/Hub/Editor/*/Unity.app/Contents/MacOS/Unity 2>/dev/null | sort -V | tail -1)"
say "PKR TestFlight upload  $(date)"
say "Build number: $PKR_BUILD_NUMBER"

say "[1/3] Unity: exporting the release Xcode project..."
rm -rf Builds/iOS
"$UNITY" -batchmode -nographics -projectPath "$PROJ" -buildTarget iOS -logFile "$OUT/unity.log" \
  -executeMethod PKR.EditorTools.BatchCheck.BuildIOS
if ! grep -q "\[PKR BuildIOS\] OK" "$OUT/unity.log"; then
  say "      FAILED (Logs/testflight/unity.log)"
  grep -E "PKR BuildIOS|PKR iOS|error CS|Error building|BuildFailedException" "$OUT/unity.log" | head -20 | tee -a "$OUT/SUMMARY.txt"
  pause_end; exit 1
fi
grep -E "Release build|PKR iOS\] Post-build" "$OUT/unity.log" | head -3 | sed 's/^/      /' | tee -a "$OUT/SUMMARY.txt"
say "      OK"

say "[2/3] Xcode: archiving (team $TEAM)..."
ARCHIVE="Builds/PixelKingdomRumble.xcarchive"
rm -rf "$ARCHIVE"
xcodebuild -project Builds/iOS/Unity-iPhone.xcodeproj -scheme Unity-iPhone -configuration Release \
  -destination "generic/platform=iOS" -archivePath "$ARCHIVE" -allowProvisioningUpdates \
  DEVELOPMENT_TEAM="$TEAM" CODE_SIGN_STYLE=Automatic archive > "$OUT/archive.log" 2>&1
if ! grep -q "ARCHIVE SUCCEEDED" "$OUT/archive.log"; then
  say "      FAILED (Logs/testflight/archive.log)"
  grep -E "error:|ARCHIVE FAILED" "$OUT/archive.log" | sort -u | head -20 | tee -a "$OUT/SUMMARY.txt"
  pause_end; exit 1
fi
say "      OK: $ARCHIVE"

say "[3/3] Uploading to App Store Connect..."
cat > Builds/ExportOptions.plist <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>method</key><string>app-store-connect</string>
  <key>destination</key><string>upload</string>
  <key>teamID</key><string>$TEAM</string>
  <key>signingStyle</key><string>automatic</string>
  <key>uploadSymbols</key><true/>
  <key>manageAppVersionAndBuildNumber</key><false/>
  <key>testFlightInternalTestingOnly</key><false/>
</dict>
</plist>
PLIST
xcodebuild -exportArchive -archivePath "$ARCHIVE" -exportOptionsPlist Builds/ExportOptions.plist \
  -exportPath Builds/TestFlightExport -allowProvisioningUpdates > "$OUT/upload.log" 2>&1
if grep -qE "EXPORT SUCCEEDED|Upload succeeded|Uploaded " "$OUT/upload.log" && ! grep -q "EXPORT FAILED" "$OUT/upload.log"; then
  say "      UPLOADED build $PKR_BUILD_NUMBER. Apple is processing it; it appears in TestFlight in about 5-30 minutes."
else
  say "      FAILED (Logs/testflight/upload.log)"
  grep -E "error|Error|FAILED" "$OUT/upload.log" | sort -u | head -20 | tee -a "$OUT/SUMMARY.txt"
  pause_end; exit 1
fi
say ""
say "DONE."
pause_end
