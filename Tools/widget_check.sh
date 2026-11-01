#!/bin/bash
# Quick Game widget: type-check every widget Swift file against the iOS SDK, then build and run the engine tests.
# Writes Logs/widget/SUMMARY.txt. Run by unity_check.command (full check) or alone via Logs/widget_request.
cd "$(dirname "$0")/.." || exit 1
OUT="Logs/widget"; mkdir -p "$OUT"; S="$OUT/SUMMARY.txt"; : > "$S"
say() { echo "$@" | tee -a "$S"; }
say "Quick Game widget check  $(date)"
SDK="$(xcrun --sdk iphoneos --show-sdk-path 2>/dev/null)"
if xcrun swiftc -typecheck -sdk "$SDK" -target arm64-apple-ios17.0 -module-name QuickGameWidget -parse-as-library \
     Native/QuickGameWidget/*.swift > "$OUT/typecheck.log" 2>&1; then
  say "      widget Swift type-check (iOS 17 SDK): OK"
else
  say "      widget Swift type-check: FAILED"; grep -E "error:" "$OUT/typecheck.log" | head -12 | sed 's/^/        /' | tee -a "$S"
fi
if xcrun swiftc -O Native/QuickGameWidget/QuickGameEngine.swift Tools/QuickGameTests/main.swift -o "$OUT/qg_tests" > "$OUT/qg_build.log" 2>&1; then
  "$OUT/qg_tests" > "$OUT/qg_tests.log" 2>&1
  grep -E "^FAIL|fewest|QUICKGAME TESTS" "$OUT/qg_tests.log" | sed 's/^/      /' | tee -a "$S"
else
  say "      engine tests: SWIFT BUILD FAILED"; grep -E "error:" "$OUT/qg_build.log" | head -8 | sed 's/^/        /' | tee -a "$S"
fi
say "DONE."
