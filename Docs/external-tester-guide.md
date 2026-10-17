# External TestFlight guide

External TestFlight approval lets people outside the team install the beta. It is **not** App Store approval: the App
Store submission is reviewed separately, later, against the full guidelines.

## 1. Beta app description (App Store Connect > TestFlight > Test Information; saved 17 Oct from `Docs/AppStore/beta-description.txt`)

Keep it short, saying what the game is and what to focus on. Suggested text:

> Pixel Kingdom Rumble: original pixel-art zombie survival (Last Stand on three maps, the Zombie Land campaign, online
> co-op through Game Center), plus a platformer, stealth contracts and an arena brawler. Free to play; one optional
> unlock. No account, no ads, no data collected.

## 2. What to Test

Saved for builds 20261016.1842 and 20261016.2133 from `Docs/AppStore/whats-to-test.txt`. Update it for every build (see the release
checklist, step 8).

## 3. Contacts and links

- Feedback email: rodlonbell16@gmail.com (set in Test Information)
- Beta App Review contact: complete (set)
- Privacy policy: https://github.com/marcompsci/Pixel-Kingdom-Rumble/blob/main/PRIVACY.md
- Support: https://github.com/marcompsci/Pixel-Kingdom-Rumble/blob/main/SUPPORT.md
- Demo account: none needed (no sign-in)

## 4. Export compliance

Answered by the build itself (`ITSAppUsesNonExemptEncryption = NO`), so no question appears on upload.

## 5. Tester requirements

- iPhone (or iPad, while the build supports iPad) on iOS 15 or later, with Metal and GameKit (every supported device)
- The TestFlight app from the App Store
- For online co-op: two devices, each signed in to Game Center

## 6. Groups and the public link (App Store Connect > TestFlight)

| Group | Type | Public link | Status (17 Oct) |
|---|---|---|---|
| PKR Team | internal | — | internal testing |
| Public Testers | external | **on** | build 20261016.2133 added; beta review refused until the pending account agreement is accepted (`BETA_CONTRACT_MISSING`) |
| Friends & Family | external | off | — |

Steps:

1. **Internal group:** add App Store Connect users (up to 100). Builds are available without beta review.
2. **External group:** TestFlight > + next to External Testing > name it. Add testers by email, or turn on the public link.
3. **Assign a build:** open the group > Builds > + > pick a VALID build.
4. **Submit for TestFlight App Review:** the first build of a version goes to beta review automatically when added to
   an external group (Submit for Review). Later builds of the same version are often approved without full review.
   **Only Omari submits.**
5. **Confirm approval:** the build's external state changes to "Ready to test" or "Testing"; Apple emails the account holder.
6. **Public link:** group > Public Link > Enable. Optionally set a tester limit and criteria (device, OS).
7. **Verify:** open the link on a device signed in to an Apple Account that isn't on the team, install TestFlight,
   accept, install the build, launch it.

## 7. Tester instructions (paste into the invitation)

1. Install TestFlight, open the invite link, and tap Accept, then Install.
2. Play! ZOMBIES is the first button.
3. Found a bug? Take a screenshot in the game; TestFlight offers "Share Beta Feedback". Or email the address above.
4. Purchases in TestFlight use a sandbox and are never charged.
