# TestFlight release checklist (run before every TestFlight build)

Each step says how it is checked. A step that wasn't run is left unticked. Never tick a step because the build compiles.

## A. Automated gates (no Apple account actions)

| Step | How (Mac watcher: `touch Logs/<name>_request Logs/check_request` in the repo) | Must show |
|---|---|---|
| 1. Engine-free tests | CI `Core tests`, or `cd DotnetTests && dotnet run -c Release` | `N passed, 0 failed` |
| 2. Compliance gate (repo) | CI `Apple compliance gate`, or `python3 Tools/compliance_check.py` | `RESULT: PASS` (WARNs are listed and read, not hidden) |
| 3. Unity check: compile, EditMode, PlayMode, compliance | `check_request` → `Logs/check/SUMMARY.txt` | 0 compile errors, 0 failed tests; skipped = only the 2 balance labs |
| 4. Release export + compliance on the Xcode project + archive (no upload) | `release_gate_request` → `Logs/testflight/SUMMARY.txt` | `[gate]` passes; `ARCHIVE SUCCEEDED`; both PrivacyInfo files are listed in the archived app; `DONE (archive only, nothing uploaded)` |
| 5. App Store Connect audit (read-only) | `asc_audit_request` → `Logs/asc_audit/SUMMARY.txt` | No FAIL that applies to this release (IAP price, screenshots, URLs, contacts, export compliance) |

## B. Upload (only when Omari says so)

6. `testflight_request` builds, re-runs the gate (it stops on FAIL) and uploads. If it ends with "Failed to Use Accounts", upload from Xcode Organizer (Distribute App > App Store Connect).
7. Wait for processing (5-30 min). `asc_audit` shows `Build processed … VALID`.
8. Set "What to Test" for the new build: put the text in `Docs/AppStore/whats-to-test.txt`, then run `asc_fix` with `{"do":"whats_new","build":"<number>","file":"Docs/AppStore/whats-to-test.txt"}`.

## C. Manual (an actual person with a device)

9. Install from TestFlight on an iPhone. Cold start; play 5 minutes; background and come back; lock and unlock the phone.
10. ZOMBIES: Last Stand solo on each map; AI squad; Zombie Land for one mission; daily orders, Locker and Boards pages.
11. Story Quest one level; Arena Clash one match; Versus one match.
12. Shadow Contracts: buy with a sandbox account; delete and reinstall; RESTORE PURCHASE brings it back; cancelling shows "Purchase cancelled."
13. Online co-op on two iPhones with two Game Center accounts (the store text promises it).
14. iPad (while the app supports iPad): menus, one Last Stand round, share sheet.
15. Airplane mode: the game starts and plays; Game Center and the store fail politely.

## D. External TestFlight (only when Omari says so)

16. Add the build to the external group "Public Testers" (or "Friends & Family").
17. Submit for TestFlight App Review (App Store Connect > TestFlight > the build > Submit for Review). Wait for "Approved". This is **beta review only**; it is not App Store approval.
18. Public link: the group "Public Testers" has it on. Open it from an Apple Account that isn't on the team and check that the invite installs.

## E. Before the App Store (not part of TestFlight)

- `Docs/apple-compliance-report.md` §4 (the IAP price, iPad decision, co-op test, App Privacy check).
- Add the IAP to the version, then press Submit for Review (Omari only).
