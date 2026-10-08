#!/bin/bash
# Pixel Kingdom Rumble: Unity check in WATCH mode (macOS).
# Double-click once and leave the Terminal window open. Every time the file Logs/check_request appears
# (Claude creates it after pulling a fix), this runs Tools/unity_check.command again, so fixes can be
# checked without anyone clicking. Close the window (or press Ctrl+C) to stop.

cd "$(dirname "$0")/.." || exit 1
mkdir -p Logs
REQ="Logs/check_request"
echo "Watching for $PWD/$REQ  (started $(date))"
echo "Running the first check now..."
touch "$REQ"
while true; do
  if [ -f "$REQ" ]; then
    rm -f "$REQ"
    echo ""
    echo "=== Check requested $(date) ==="
    PKR_NO_PAUSE=1 bash Tools/unity_check.command
    date > Logs/check_finished
    echo "=== Check finished $(date). Waiting for the next request... ==="
  fi
  sleep 5
done
