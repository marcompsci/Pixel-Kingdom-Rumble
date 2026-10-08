#!/bin/bash
# Pixel Kingdom Rumble: install the last iPhone build on a connected iPhone and launch it (no rebuild).
# Build first with Tools/ios_build.command. Results: Logs/ios/install_summary.txt.
cd "$(dirname "$0")/.." || exit 1
OUT="Logs/ios"; mkdir -p "$OUT"; S="$OUT/install_summary.txt"; : > "$S"
say() { echo "$@" | tee -a "$S"; }
BUNDLE="com.marcompsci.pixelkingdomrumble"
APP="$(ls -d Builds/DerivedData/Build/Products/Release-iphoneos/*.app 2>/dev/null | head -1)"
say "PKR iPhone install  $(date)"
[ -z "$APP" ] && { say "No build found. Run Tools/ios_build.command first."; read -r -p "Press Return to close." _; exit 1; }
xcrun devicectl list devices > "$OUT/devices.txt" 2>&1
cat "$OUT/devices.txt" | tee -a "$S"
DEVICE="$(grep -i "iphone" "$OUT/devices.txt" | grep -viE "unavailable" | grep -iE "available|connected" \
  | grep -oE "[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12}" | head -1)"
if [ -z "$DEVICE" ]; then
  say "No reachable iPhone. Unlock it, tap Trust if asked, and turn on Developer Mode (Settings > Privacy & Security)."
  read -r -p "Press Return to close." _; exit 2
fi
say "Installing $APP on $DEVICE..."
if xcrun devicectl device install app --device "$DEVICE" "$APP" >> "$S" 2>&1 && \
   xcrun devicectl device process launch --device "$DEVICE" "$BUNDLE" >> "$S" 2>&1; then
  say "INSTALLED AND LAUNCHED."
else
  say "FAILED (see above)."
fi
read -r -p "Press Return to close." _
