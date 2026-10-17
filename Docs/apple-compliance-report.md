# Apple compliance report: Pixel Kingdom Rumble 1.0

_Audit date: 17 Oct 2026. Repo commit: see git log. App Store Connect app 6821419588, bundle `com.marcompsci.pixelkingdomrumble`._

These five documents live in `Docs/` (not `docs/`): the Mac's file system is case-insensitive and the repo already
has `Docs/`, so a second `docs/` folder would collide there.

## Release-readiness result: **CONDITIONAL**

- **External TestFlight:** CONDITIONAL. Build **20261016.2133** passed every automated gate, then uploaded and
  processed. It contains all the fixes below, is attached to version 1.0, and is in the external group "Public
  Testers" with What to Test, beta description and beta review notes set. Submitting it for TestFlight App Review was
  attempted on 17 Oct (Omari approved) and refused by Apple: `ENTITY_UNPROCESSABLE.BETA_CONTRACT_MISSING`. That
  means the account has an agreement waiting. Omari has to sign in to App Store Connect (Business > Agreements, or the
  banner on the home page), accept it as the Account Holder, and submit again.
- **App Store submission:** CONDITIONAL. The two former blockers (F1 IAP price, F3 iPad) are fixed. What's left is
  manual: the co-op test (F6), the App Privacy check, and Omari pressing Submit. Approval is never guaranteed.

## 1. Architecture summary (what is actually in the repo)

| Area | Finding (checked in the code) |
|---|---|
| Engine / language | Unity 6000.6.4f1, URP 2D, C# (IL2CPP). No Swift/SwiftUI app code, no React Native/Flutter/web views. |
| Native code | 4 Objective-C++ plugins in `Assets/Plugins/iOS`: `PKRGameCenter.mm` (sign-in, scores, achievements, leaderboards, friends rank), `PKRMatch.mm` (GameKit real-time matches for online co-op), `PKRShare.mm` (share sheet, iPad popover anchored), `PKRHaptics.mm`. |
| Packages | Unity IAP 5.0.0 (`com.unity.purchasing`), Input System, URP, Test Framework, uGUI. No analytics, ads, Firebase, Supabase or other SDKs. |
| Backend / network | None. No servers, no `UnityWebRequest`, no API keys. All network traffic goes through Apple frameworks (GameKit, StoreKit). |
| Accounts / login | None. Game Center is optional. |
| Data | One local JSON save (`JsonFileStore`, atomic write with a backup) plus PlayerPrefs settings. |
| Purchases | One non-consumable, `com.marcompsci.pixelkingdomrumble.shadowcontracts` ("Shadow Contracts", $3.99 per `Docs/SHADOW_CONTRACTS.md`), with restore. Star Shards are earned only, never sold. |
| Multiplayer | Game Center matchmaking for 2-4 players. Each player's Game Center nickname is shown; there is no text or voice chat. |
| Signing / build | Automatic signing, team X6LZQ3FS36. Release builds come from `Tools/testflight_upload.command` with `PKR_RELEASE=1`. Deployment target iOS 15.0; `TARGETED_DEVICE_FAMILY` 1,2 (iPhone and iPad). |
| CI | `.github/workflows/core-tests.yml` (engine-free tests) and the new `.github/workflows/compliance.yml` (Apple gate). Unity Edit/Play Mode tests run on the Mac (`Tools/unity_check.command`). |

## 2. Findings

Severity: BLOCKER / HIGH / MEDIUM / LOW. "Verified by" names the check that shows the current state.

| # | Guideline / requirement | Where | Severity | Concern | Remediation | Done? | Verified by |
|---|---|---|---|---|---|---|---|
| F1 | 2.1 App Completeness (in-app purchases must be complete and reviewable); 3.1.1 | App Store Connect, IAP `…shadowcontracts` | was **BLOCKER** | The IAP was in **Missing Metadata**: no price schedule and no availability. | Omari approved $3.99 in all territories. Set the US base price to $3.99 and made it available in all 175 territories plus new ones. | **Yes** (17 Oct) | `asc_fix`: state now **READY_TO_SUBMIT**; localization ✓, review screenshot ✓. It still has to be added to the version when submitting to the App Store |
| F2 | Hygiene | App Store Connect | LOW | A second IAP draft with a doubled product id. The code doesn't use it. | Deleted (Omari approved). | **Yes** (17 Oct) | `asc_fix`: DELETE 204 |
| F3 | 2.1 / 2.3.3 (screenshots for each supported device); App Review tests iPad builds | `OrientationController.cs`, `IOSPostBuild.cs`, `DeviceIdiom.cs` + `PKRDevice.mm` | was **BLOCKER** | The build supports iPad, but there were no iPad screenshots. On the iPad simulator, every forced rotation (portrait menus, landscape play) made iPadOS show the game in a small window over the home screen. | Omari chose to keep iPad support. On iPad the whole game now runs in landscape, either side, with no per-screen forced rotation; `Info.plist` declares landscape only for iPad (menus fit by aspect). I took 9 landscape 13" iPad screenshots in the simulator and uploaded them. iPhone behaviour is unchanged. | **Yes** (17 Oct) | iPad Pro 13" simulator: all 9 screens fill the display; App Store Connect `APP_IPAD_PRO_3GEN_129`: 9 COMPLETE |
| F4 | 5.1.1(i) Privacy policy must accurately describe data use | `PRIVACY.md` (the App Store privacy URL) | MEDIUM | The policy called it "a single-player game". It didn't cover online co-op (Game Center nicknames and gameplay data shared with other players), Game Center friends access, or saving to Photos. | Rewrote the policy for all three. Still no data collected by the developer. | **Yes** | compliance_check: policy covers multiplayer / friends / photos; asc_audit: URL returns HTTP 200 |
| F5 | Privacy manifests and required-reason APIs (Apple Developer documentation) | `Assets/_Project/Scripts/Editor/IOSPostBuild.cs` | MEDIUM | The app target had no `PrivacyInfo.xcprivacy`. The game's C# uses `Environment.TickCount` (system boot time), which UnityFramework's manifest didn't declare. | Post-build now writes the app manifest (no tracking, no data collected) into the app target, and adds system boot time (reason 35F9.1) to UnityFramework's manifest. | **Yes** | compliance_check (repo); `--xcode` check plus the archive listing in the release gate run (section 3) |
| F6 | 2.3 Accurate Metadata | `Docs/AppStore/LISTING.md`, App Store description | HIGH | The store text promises online co-op for up to 4 players. It has never been tested on two real devices. | Test it on two iPhones with two Game Center accounts before release, or soften the text. | **No: manual** | `Docs/AppStore/RELEASE_CHECKLIST.md` §2 |
| F7 | 2.2 Beta Testing (external testers need test information) | App Store Connect, build 20261016.1842 | MEDIUM | "What to Test" was empty. | Set from `Docs/AppStore/whats-to-test.txt`. | **Yes** | asc_fix: saved (841 characters) |
| F8 | App Store Connect submission requirement | App Information → content rights | LOW | The content rights declaration wasn't answered. | Set to "does not use third-party content". All art, audio, maps and the font are generated by this repo's tools (`Docs/CREDITS_AND_LICENSES.md`). | **Yes** | asc_fix: saved |
| F9 | 3.1.1 / StoreKit good practice | `Assets/_Project/IAP/UnityIapBackend.cs` | LOW | A refunded purchase stays unlocked: the unlock is kept in the save and never revoked. Apple doesn't require revocation for non-consumables. | Optional later: re-check the receipt on restore and revoke if the store reports it was refunded. Not changed, to avoid wrongly locking paid players while offline. | No (by choice) | Code review |
| F10 | 5.2 Intellectual Property | `Docs/CREDITS_AND_LICENSES.md` | LOW | The credits doc said "audio: none" and was out of date. | Updated to list every asset type and how it's generated. | **Yes** | compliance_check: every font/audio/model file accounted for; no third-party game names in player text or metadata |
| F11 | 2.3.1 (no hidden or undocumented features) | `MissionBoardUI.cs`, `BatchCheck.cs` | INFO (OK) | The tester "UNLOCK ALL" button exists only in tester builds. | Kept. The compliance gate now fails if tester-only strings escape `#if PKR_TESTER`. | Yes | compliance_check |
| F12 | Export compliance | `IOSPostBuild.cs` | INFO (OK) | `ITSAppUsesNonExemptEncryption = NO`. The app uses only Apple system networking (GameKit/StoreKit over HTTPS), which is exempt. | — | — | compliance_check; asc_audit `usesNonExemptEncryption=False` |
| F13 | 1.2 User-Generated Content | `PKRMatch.mm` | LOW / not applicable | The only player-to-player content is Game Center nicknames inside matches made by Game Center. There is no chat, no uploads and no profiles. Apple's Game Center handles reporting and blocking of players. | No in-game moderation system is needed; none is claimed. If chat is ever added, 1.2 applies in full (report, block, filter, contact). | — | Code review: no text input fields, no `GKVoiceChat` |
| F14 | Age rating (App Store Connect questionnaire) | App Store Connect | INFO (done) | Re-answered on 16 Oct: cartoon violence frequent, guns frequent, horror mild. App Store rating is now 12+. | — | Yes | asc_audit `TWELVE_PLUS` |
| F15 | 4.8 Login Services; 5.1.1(v) account deletion | — | Not applicable | The game has no accounts or third-party sign-in. | — | — | Code review |
| F16 | App Transport Security | Info.plist | INFO (OK) | No ATS exceptions. | The gate fails on `NSAllowsArbitraryLoads`. | — | compliance_check |
| F17 | 2.5.x software requirements | Plugins | INFO (OK) | Public APIs only (GameKit, StoreKit, UIKit). The iPad share sheet is anchored, so it doesn't crash on iPad. | — | — | Code review |
| F19 | Account agreements (App Store Connect) | App Store Connect account | **BLOCKER** for external TestFlight | Apple refuses beta review: `BETA_CONTRACT_MISSING`. | The Account Holder accepts the pending agreement in App Store Connect; only Omari can do this. Then run `asc_fix` `beta_submit` (or press Submit for Review on the build's TestFlight page). | **No** (needs Omari) | `asc_fix` 17 Oct 21:53 |
| F18 | Current guidelines | — | Note | A public change tracker shows Apple revised the App Review Guidelines on 17 Oct 2026. I couldn't load Apple's own page from this session, so the newest wording wasn't read directly. | Omari: skim "What's new" on the guidelines page before submitting. | — | — |

Not applicable here: 1.3 Kids Category (not in it), 4.7 hosted mini-apps or streaming, 5.4 VPN, Supabase/Firebase rules, backend authorization.

## 3. Functionality and crash checks actually run

| Check | Result |
|---|---|
| Mac Unity check (f59e47a): compile, EditMode, PlayMode | 0 errors; EditMode 321/321; PlayMode 140 passed, 0 failed, 2 skipped (balance labs, run only on request) |
| Engine-free core tests (cloud) | 307 passed, 0 failed |
| Device: install + cold launch on iPhone (1541c14 tester build) | Launched; 90 s of the game's log had no exceptions or crashes |
| Simulator (iPhone 17 Pro Max): menu, 3 Last Stand maps, Zombie Land city/isle/sublevel | All scenes running after 30 s (the script checks the process each time) |
| iPad 13" simulator (landscape, 646e163) | Menus, all 3 Last Stand maps, Zombie Land city and sublevel, Story Quest, Shadow Contracts board: all running, all full-screen |
| Background / foreground | Code review: `GameStateManager` pauses and saves on `OnApplicationPause(true)`; the save is also written on a timer and on quit |
| Offline / no Game Center / store unavailable | Code review: Game Center calls are no-ops when not signed in; purchase/restore show "The App Store isn't ready" instead of failing silently |

Not run (manual): online co-op on two devices, real sandbox purchase/restore/Ask to Buy, full play-throughs.

## 4. Remaining manual Apple actions

1. ~~IAP price and availability (F1); typo IAP (F2); iPad (F3)~~: done 17 Oct with Omari's approval.
2. Play the new build on a real iPad in landscape (the simulator looked right; a real device hasn't been tried).
3. Test online co-op on two iPhones (F6).
4. Accept the pending App Store Connect agreement (F19), then submit build 20261016.2133 for **TestFlight App Review**. It's already in "Public Testers", whose public link is on.
5. Check App Privacy in App Store Connect still says "Data Not Collected" (the API can't read it).
6. Before the App Store: add the IAP to the version submission, then press Submit for Review.
