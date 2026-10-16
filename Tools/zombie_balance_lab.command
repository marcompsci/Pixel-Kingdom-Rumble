#!/bin/bash
# Pixel Kingdom Rumble: the zombie balance lab (macOS). A bot plays Last Stand on every map, solo and with the AI
# squad, sped up, and writes Logs/zombie_balance/rounds.csv and summary.csv. Takes about 20-25 minutes.
# Run it by creating Logs/zombie_balance_request and then Logs/check_request (the check watcher starts it).
cd "$(dirname "$0")/.." || exit 1
PROJ="$(pwd)"
OUT="$PROJ/Logs/zombie_balance"
mkdir -p "$OUT"
rm -f "$OUT"/SUMMARY.txt "$OUT"/results.xml
UNITY="$(ls -d /Applications/Unity/Hub/Editor/*/Unity.app/Contents/MacOS/Unity 2>/dev/null | sort -V | tail -1)"
echo "PKR zombie balance lab  $(date)" | tee "$OUT/SUMMARY.txt"
touch Logs/zombie_balance_request
"$UNITY" -batchmode -projectPath "$PROJ" -runTests -testPlatform PlayMode -testFilter "PKR.Tests.ZombieBalanceLab" \
  -testResults "$OUT/results.xml" -logFile "$OUT/unity.log"
rm -f Logs/zombie_balance_request
if [ -f "$OUT/summary.csv" ]; then
  cat "$OUT/summary.csv" | tee -a "$OUT/SUMMARY.txt"
else
  echo "No summary (see Logs/zombie_balance/unity.log)" | tee -a "$OUT/SUMMARY.txt"
fi
echo "DONE." | tee -a "$OUT/SUMMARY.txt"
