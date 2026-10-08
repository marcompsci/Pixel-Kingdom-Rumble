#!/bin/bash
# Pixel Kingdom Rumble: one-click Unity check (macOS).
# Double-click this file in Finder. It runs Unity WITHOUT opening the Editor window:
#   1. opens/imports the project and compiles every script
#   2. if that compiled, runs the PKR menu steps (layers, iOS settings, data, all scenes)
#   3. runs the EditMode tests, then the PlayMode tests
# Results land in Logs/check/ (git ignores that folder). Claude reads SUMMARY.txt from there.
# Close this project in the Unity Editor first (another project being open is fine).
# The first run imports everything and can take 10-20 minutes.

cd "$(dirname "$0")/.." || exit 1
PROJ="$(pwd)"
OUT="$PROJ/Logs/check"
mkdir -p "$OUT"
rm -f "$OUT"/*

UNITY="$(ls -d /Applications/Unity/Hub/Editor/*/Unity.app/Contents/MacOS/Unity 2>/dev/null | sort -V | tail -1)"
if [ -z "$UNITY" ]; then
  echo "Unity Editor not found under /Applications/Unity/Hub/Editor. Install Unity 6 with Unity Hub." | tee "$OUT/SUMMARY.txt"
  read -r -p "Press Return to close." _; exit 1
fi

say() { echo "$@" | tee -a "$OUT/SUMMARY.txt"; }
say "PKR Unity check  $(date)"
say "Unity: $UNITY"
say "Project: $PROJ"
say ""

# 1. Import + compile
say "[1/4] Importing and compiling (first run is slow)..."
"$UNITY" -batchmode -nographics -projectPath "$PROJ" -logFile "$OUT/1_compile.log" -quit
grep -E "error CS[0-9]+" "$OUT/1_compile.log" | sort -u > "$OUT/compile_errors.txt"
ERRS=$(wc -l < "$OUT/compile_errors.txt" | tr -d ' ')
say "      compile errors: $ERRS"
if [ "$ERRS" != "0" ]; then
  say ""
  say "First errors:"
  head -20 "$OUT/compile_errors.txt" | tee -a "$OUT/SUMMARY.txt"
  say ""
  say "STOPPED: fix compile errors first. Tell Claude 'check done'."
  read -r -p "Press Return to close." _; exit 1
fi

# 2. PKR setup + scene builders
say "[2/4] Running PKR setup and building all scenes..."
"$UNITY" -batchmode -nographics -projectPath "$PROJ" -logFile "$OUT/2_build.log" \
  -executeMethod PKR.EditorTools.BatchCheck.SetupAndBuild
if grep -q "\[PKR BatchCheck\] OK" "$OUT/2_build.log"; then say "      scenes built: OK"
else say "      scene build FAILED (see Logs/check/2_build.log)"; grep -E "PKR BatchCheck|Exception|error" "$OUT/2_build.log" | head -15 | tee -a "$OUT/SUMMARY.txt"; fi

# 3 + 4. Tests
run_tests() {
  local platform="$1" step="$2"
  say "[$step/4] $platform tests..."
  local extra="-nographics"
  [ "$platform" = "PlayMode" ] && extra=""
  "$UNITY" -batchmode $extra -projectPath "$PROJ" -runTests -testPlatform "$platform" \
    -testResults "$OUT/${platform}.xml" -logFile "$OUT/${step}_${platform}.log"
  if [ -f "$OUT/${platform}.xml" ]; then
    local line
    line=$(grep -m1 "<test-run" "$OUT/${platform}.xml" | grep -oE '(total|passed|failed|skipped)="[0-9]+"' | tr '\n' ' ')
    say "      $line"
    grep -oE '<test-case [^>]*fullname="[^"]*"[^>]*result="Failed"' "$OUT/${platform}.xml" \
      | grep -oE 'fullname="[^"]*"' | head -20 | sed 's/^/      FAILED /' | tee -a "$OUT/SUMMARY.txt"
  else
    say "      no results file (see Logs/check/${step}_${platform}.log)"
  fi
}
run_tests EditMode 3
run_tests PlayMode 4

say ""
say "DONE. Tell Claude 'check done'."
read -r -p "Press Return to close." _
