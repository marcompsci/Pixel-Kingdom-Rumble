# Privacy and security audit (17 Oct 2026, re-checked 12 Nov 2026)

## Data inventory

| Data | Where it lives | Leaves the device? | Who can access it |
|---|---|---|---|
| Save (progress, unlocks, scores, settings) | App sandbox: JSON save (atomic write + backup) and PlayerPrefs | No | Only the user |
| Game Center scores and achievements | Sent by GameKit | Yes, to Apple | Apple; other players see leaderboard entries |
| Online co-op gameplay (positions, actions, round, score) and Game Center nicknames | GameKit match | Yes, between the players' devices through Apple | The players in that match. The developer can't access it |
| Game Center friends rank | Read on device by GameKit after iOS asks | No (only Apple's service is queried) | The user |
| Photos the user saves | Photo library (add only, through the share sheet) | Only if the user shares | The user |
| Purchases | StoreKit | Yes, to Apple | Apple |
| Quick Game widget run and stats (12 Nov) | App Group `group.com.marcompsci.pixelkingdomrumble` (UserDefaults), shared only by the game and its widget | No | Only the user |
| Live player counts (12 Nov) | GameKit: a hosted matchmaking request for the place's player group (cancelled after 6 s) and `queryPlayerGroupActivity` | Yes, to Apple Game Center only, and only when signed in | Apple. Other players see an aggregate number, never an identity. The developer receives nothing |

Result: the developer collects no data. That matches the App Privacy label ("Data Not Collected"; recheck in App Store
Connect), `PRIVACY.md`, and the app privacy manifest (`NSPrivacyCollectedDataTypes` empty, `NSPrivacyTracking`
false).

## Controls checked

| Control | Status | Evidence |
|---|---|---|
| HTTPS/TLS | The app makes no network calls of its own; GameKit and StoreKit use Apple TLS | No `UnityWebRequest`, `HttpClient` or `NSURLSession` in game code |
| ATS not weakened | OK | No `NSAppTransportSecurity` exceptions (compliance gate) |
| Secrets in client or repo | None | Gate: no keys, certificates or credential patterns in tracked files. The App Store Connect API key lives outside the repo (`../.appstoreconnect/`) and is used only by Mac scripts |
| Token storage | Not applicable | No tokens, no accounts |
| Server-side authorization, RLS, Firebase rules | Not applicable | No backend |
| Input validation | Not applicable | No text input; save data is sanitized on load (`*.Sanitize()`) |
| Purpose strings | OK | `NSPhotoLibraryAddUsageDescription`, `NSGKFriendListUsageDescription`; no camera, microphone, location or tracking use |
| Privacy policy access | OK | `PRIVACY.md` on GitHub (HTTP 200); set as the privacy URL for the App Store and TestFlight |
| Account deletion | Not applicable | No accounts. Deleting the app deletes all local data |
| Widget privacy manifest (12 Nov) | OK | `Native/QuickGameWidget/PrivacyInfo.xcprivacy`: no tracking, no data collected, UserDefaults 1C8F.1. UnityFramework also declares 1C8F.1 for the game's App Group read (`PKRWidget.mm`). Gate-checked |
| Widget network (12 Nov) | OK | No `URLSession` or URLs in the widget's Swift. Gate-checked |
| Live counts lifecycle (12 Nov) | **Fixed** | Pings had kept running after the player left the plaza, and each one ended with `[GKMatchmaker cancel]`, which could stop a real Online Co-op search. Now they are owner-scoped (`LivePresence.Enter/Leave(owner)`), stop when the scene goes, and are skipped while co-op is busy (`PKR_MP_State`). PlayMode test `Plaza_StopsAnnouncing_WhenThePlayerLeaves`; gate checks for both |
| Privacy manifests | Fixed | App target manifest added; UnityFramework manifest declares FileTimestamp (C617.1/0A2A.1), UserDefaults (CA92.1), SystemBootTime (35F9.1); UnityRuntime.framework declares its own |
| Third-party SDKs | None besides Unity IAP (StoreKit) | `Packages/manifest.json` |
| Export compliance | Exempt | `ITSAppUsesNonExemptEncryption = NO`; App Store Connect shows `usesNonExemptEncryption=False` |
| Tracking / analytics / ads | None | Gate fails if a known SDK appears |

## Rules for future changes

Any of these changes makes this audit out of date. When one happens, update the policy, the label, the manifest and
this file together:

- adding a server
- adding analytics or ads
- adding chat or user-generated content (guideline 1.2: report, block, filter, contact)
- adding sign-in (4.8, and in-app account deletion under 5.1.1(v))
- sending anything new through Game Center (the gate checks that PRIVACY.md covers the live counts and the widget)
