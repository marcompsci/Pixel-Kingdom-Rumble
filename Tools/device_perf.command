#!/bin/bash
# Pixel Kingdom Rumble: measure frame rate on the connected iPhone (no rebuild; run Tools/ios_build.command first).
# Launches the installed game straight into a Last Stand round that plays itself (EV_LastStand@demo, an AI squad),
# records the game's log for PKR_PERF_SECONDS (default 150 s) and keeps the "[PKR Perf]" lines PerfMonitor writes
# every 30 s (average fps, 95th percentile frame, missed frames, graphics tier, thermal state).
# Results: Logs/ios/perf_summary.txt (and the full log in Logs/ios/perf_console.log).
cd "$(dirname "$0")/.." || exit 1
OUT="Logs/ios"; mkdir -p "$OUT"; S="$OUT/perf_summary.txt"; : > "$S"
say() { echo "$@" | tee -a "$S"; }
BUNDLE="com.marcompsci.pixelkingdomrumble"
SECS="${PKR_PERF_SECONDS:-150}"
SCENE="${PKR_PERF_SCENE:-EV_LastStand@demo}"
say "PKR device performance run  $(date)  scene $SCENE  ${SECS}s"
xcrun devicectl list devices > "$OUT/devices.txt" 2>&1
DEVICE="$(grep -i "iphone" "$OUT/devices.txt" | grep -viE "unavailable" | grep -iE "available|connected" \
  | grep -oE "[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12}" | head -1)"
if [ -z "$DEVICE" ]; then
  say "No reachable iPhone (unlock it; Developer Mode on). Nothing measured."
  exit 2
fi
grep -i "$DEVICE" "$OUT/devices.txt" | tee -a "$S"
xcrun devicectl device process launch --console --terminate-existing --device "$DEVICE" \
  --environment-variables "{\"PKR_SCENE\": \"$SCENE\"}" "$BUNDLE" > "$OUT/perf_console.log" 2>&1 &
PID=$!
sleep "$SECS"
kill $PID 2>/dev/null
sleep 2; xcrun devicectl device process launch --device "$DEVICE" "$BUNDLE" > /dev/null 2>&1
say "Log lines: $(wc -l < "$OUT/perf_console.log" | tr -d ' ')"
say "--- [PKR Perf] lines ---"
grep "\[PKR Perf\]" "$OUT/perf_console.log" | tee -a "$S"
N=$(grep -c "\[PKR Perf\] scene" "$OUT/perf_console.log")
[ "$N" = "0" ] && say "NO MEASUREMENT: the game didn't log a [PKR Perf] line (did it launch into the scene?)."
say "--- errors / exceptions ---"
grep -iE "exception|error" "$OUT/perf_console.log" | grep -v "PKR Perf" | head -20 | tee -a "$S"
say "DONE."
