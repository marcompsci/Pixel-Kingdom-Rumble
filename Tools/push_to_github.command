#!/bin/bash
# Pixel Kingdom Rumble: push main to GitHub using this Mac's saved GitHub login (Keychain / GitHub Desktop).
cd "$(dirname "$0")/.." || exit 1
echo "=== $(date)  Push to GitHub"
git fetch -q origin main 2>/dev/null
: > Logs/push.log
# One commit at a time: big pushes over a slow connection can time out (HTTP 408).
for c in $(git rev-list --reverse origin/main..main); do
  git -c http.postBuffer=524288000 push origin "$c:refs/heads/main" 2>&1 | tee -a Logs/push.log
done
git push origin main 2>&1 | tee -a Logs/push.log
echo "Local:  $(git rev-parse --short HEAD)"
echo "GitHub: $(git ls-remote origin refs/heads/main | cut -c1-7)"
echo "=== finished. You can close this window."
