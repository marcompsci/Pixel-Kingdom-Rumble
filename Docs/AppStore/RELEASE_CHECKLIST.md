# Release checklist: Pixel Kingdom Rumble 1.0 (zombies lead)

Nothing here submits the app. Omari presses **Submit for Review** himself when every box he cares about is ticked.

## 1. Build
- [x] Mac Unity check green on a527cff (16 Oct): compile 0 errors, EditMode 307/307, PlayMode 131 passed / 0 failed / 2 skipped (balance labs).
- [x] Mac Unity check green on f59e47a (16 Oct, after onboarding, Rail Yard, chapter 3, daily orders, the LOCKER): EditMode 321/321, PlayMode 140 passed / 0 failed / 2 skipped.
- [x] TestFlight: 20261016.1842 (f59e47a + docs: Rail Yard, chapter 3, daily orders, LOCKER, friends boards) uploaded 16 Oct through Xcode Organizer ("Uploaded to Apple"; usual missing-dSYM warning for UnityRuntime). Apple processes it before it shows in TestFlight. Older: 1338 uploaded, 1521 never uploaded (superseded).
- [ ] Installed on Omari's iPhone from TestFlight; launches, no crash in the first 5 minutes.
- [x] Version 1.0 uses build 20261016.2133 (17 Oct: privacy manifests, iPad landscape fix, compliance gate passed, uploaded with the API key).

## 2. Play it by hand (nobody has yet)
- [ ] Last Stand, Hollow Block, solo: reach round 10 (the Butcher). Note anything unfair or boring.
- [ ] Last Stand, Salt Pier, AI squad: do teammates help, revive, and not block doorways?
- [ ] Weekly challenge: the mutators feel different; the reward pays once.
- [ ] Last Stand, Rail Yard: throw the three signal levers, beat the Iron Widow, get the Singularity. Is it fun or just big?
- [ ] Daily orders: finish all three on a real day; the streak shows 1 the next morning and 0 after a missed day.
- [ ] The LOCKER: a tracer changes your bullet colour; a title shows on the ZOMBIES menu and the end-of-run card.
- [ ] BOARDS page on iPhone: FRIENDS / EVERYONE open Game Center; the friends rank line appears (needs a Game Center friend with a score; iOS asks for friends access the first time).
- [ ] First-time tips and the rookie warm-up read well on a phone; the HUD size slider and strong aim assist work.
- [ ] Zombie Land chapter 1 to the First Turned (or use a save that is close), then the causeway, the 4 sigils and Mother Brine.
- [ ] Chapter 3: the freight lift under the Graystone lab, the 3 keycards, the Curator (riot guards at half, leapers at a quarter), the lift back up.
- [ ] Riot zombies (flank them) and leapers (watch the red crouch) read clearly on a phone screen.
- [ ] Rime Cannon and Storm Coil both come out of the box.
- [ ] Gravemoor Park (lanterns, the Briar Queen drops the Kindler), Zombie Land chapter 4 (the service road, 3 clearances, the Marshal).
- [ ] Splitters and burrowers read clearly; the Kindler; overcharge at the forge; LOCKER > ARMORY; prestige at rank 30.
- [ ] Settings > DISPLAY & ACCESS: every option does what it says (left-handed controls, bigger touch targets, colour vision, reduced motion, sound cues, graphics modes, Show FPS).
- [x] Frame rate on iPhone 17 Pro (17 Oct, Tools/device_perf.command): 60 fps, p95 17.2 ms, 0-1% missed frames in a self-playing Last Stand round.
- [ ] A long session by hand, and an older iPhone if one is around: no heat warnings or stutter.
- [ ] Online co-op with **two phones on two Game Center accounts**: invite, lobby, a few rounds, one player leaves (their hero turns AI), host leaves (guests see the end). Not tested yet: the store text promises it, so test before release or soften the text.
- [ ] Touch controls: FIRE / USE reachable, revive by holding USE.

## 3. Game Center (App Store Connect > Game Center)
- [x] `pkr.lb.laststand`, `pkr.lb.manor`, `pkr.lb.survivor_xp` created with en-US names (16 Oct).
- [x] `pkr.lb.pier` "Last Stand: Salt Pier" created (16 Oct).
- [x] `pkr.lb.rail` "Last Stand: Rail Yard" and the 11 zombie achievements (pkr.ach.z_*, 350 points, badges uploaded) created (17 Oct). 21 achievements, 630 points in all.
- [x] Game Center turned on for version 1.0 (16 Oct). All 7 leaderboards and 21 achievements are in Prepare for Submission, so they go to review with the version.
- [x] Zombie update 2 (17 Oct, through the API with Tools/asc_fix.py): `pkr.lb.gravemoor` "Last Stand: Gravemoor Park" and 4 achievements (Thornless 40, Wheels Up 40, Born Again 30, Golden Gun 20; en-US text and badges). Now 8 leaderboards, 25 achievements, 760 points. Check they show in version 1.0's Game Center section before submitting.
- [ ] Multiplayer compatibility: the app's own bundle only (Game Center > Multiplayer Compatibility, nothing to add).

## 4. Store page (App Store Connect > 1.0)
- [x] Subtitle, promotional text, keywords and description from Docs/AppStore/LISTING.md (saved in ASC 16 Oct).
- [x] Screenshots (16 Oct, from the iPhone 17 Pro Max simulator): 10 for 6.9" and the same 10 resized for 6.3": Rail Yard, Salt Pier, Hollow Block, the Sublevel, Blackwater, ZOMBIES menu, Sunspire Heights, Rooftop Run, First Light contract, Shadow Contracts board. Files in Docs/AppStore/screenshots/; re-upload with Tools/asc_screenshots.py (Logs/asc_request).
- [ ] App preview: the 15-30 s zombie clip (landscape 1920x886 or the size ASC asks for), poster frame from Last Stand.
- [x] Age rating re-answered (16 Oct): cartoon or fantasy violence FREQUENT/INTENSE, guns or other weapons FREQUENT/INTENSE, horror or fear themes INFREQUENT/MILD, everything else unchanged (none). App Store rating now 12+ (was 9+).
- [ ] App Privacy: still "Data Not Collected" (Game Center is Apple's; no analytics, no ads).
- [x] Review notes updated for ZOMBIES (Last Stand, Zombie Land, daily orders, LOCKER, BOARDS; online co-op optional) and the zombie leaderboards (16 Oct).
- [x] App Review contact: Omari Bell, rodlonbell16@gmail.com, +1 559 408 9014 (same as Crossfall and Retro Hoops).
- [x] Shadow Contracts IAP: review screenshot attached, $3.99 base price, all territories; state Ready to Submit (17 Oct). Add it to the version when submitting.

## 4b. Compliance gate
- See `Docs/apple-compliance-report.md` (result CONDITIONAL) and `Docs/testflight-release-checklist.md` (run before every TestFlight build).
- [ ] Accept the pending App Store Connect agreement (Apple refused beta review with BETA_CONTRACT_MISSING), then submit 20261016.2133 for TestFlight review.

## 5. Before pressing submit
- [ ] Everything in sections 1-4 ticked, or knowingly skipped.
- [ ] GitHub main matches the build that was uploaded.
- [ ] Release type: manual (so the release date is Omari's call).
