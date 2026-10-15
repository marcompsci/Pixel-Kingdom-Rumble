#!/bin/bash
# Pixel Kingdom Rumble: the balance lab (macOS). The CPU plays every hero against four sparring partners in the
# Versus dojo, sped up, and writes Logs/balance/results.csv and summary.csv. Takes about 20-40 minutes.
# Run it by creating Logs/balance_request and then Logs/check_request (the check watcher starts it).
cd "$(dirname "$0")/.." || exit 1
PROJ="$(pwd)"
OUT="$PROJ/Logs/balance"
mkdir -p "$OUT"
rm -f "$OUT"/SUMMARY.txt "$OUT"/results.xml
UNITY="$(ls -d /Applications/Unity/Hub/Editor/*/Unity.app/Contents/MacOS/Unity 2>/dev/null | sort -V | tail -1)"
echo "PKR balance lab  $(date)" | tee "$OUT/SUMMARY.txt"
touch Logs/balance_request
"$UNITY" -batchmode -projectPath "$PROJ" -runTests -testPlatform PlayMode -testFilter "PKR.Tests.BalanceLab" \
  -testResults "$OUT/results.xml" -logFile "$OUT/unity.log"
rm -f Logs/balance_request
if [ -f "$OUT/summary.csv" ]; then
  echo "Win rates (vs 4 sparring partners, CPU Hard both sides):" | tee -a "$OUT/SUMMARY.txt"
  cat "$OUT/summary.csv" | tee -a "$OUT/SUMMARY.txt"
else
  echo "No summary (see Logs/balance/unity.log)" | tee -a "$OUT/SUMMARY.txt"
fi
echo "DONE." | tee -a "$OUT/SUMMARY.txt"
