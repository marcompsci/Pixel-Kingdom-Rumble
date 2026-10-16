# Release checklist: Pixel Kingdom Rumble 1.0 (zombies lead)

Nothing here submits the app. Omari presses **Submit for Review** himself when every box he cares about is ticked.

## 1. Build
- [ ] Mac Unity check green (compile, EditMode, PlayMode) on the commit being shipped.
- [ ] TestFlight build uploaded and VALID in App Store Connect (`Logs/testflight_request` + `Logs/check_request`).
- [ ] Installed on Omari's iPhone from TestFlight; launches, no crash in the first 5 minutes.
- [ ] Version / build number in App Store Connect matches the TestFlight build picked for review.

## 2. Play it by hand (nobody has yet)
- [ ] Last Stand, Hollow Block, solo: reach round 10 (the Butcher). Note anything unfair or boring.
- [ ] Last Stand, Salt Pier, AI squad: do teammates help, revive, and not block doorways?
- [ ] Weekly challenge: the mutators feel different; the reward pays once.
- [ ] Zombie Land chapter 1 to the First Turned (or use a save that is close), then the causeway, the 4 sigils and Mother Brine.
- [ ] Riot zombies (flank them) and leapers (watch the red crouch) read clearly on a phone screen.
- [ ] Rime Cannon and Storm Coil both come out of the box.
- [ ] Online co-op with **two phones on two Game Center accounts**: invite, lobby, a few rounds, one player leaves (their hero turns AI), host leaves (guests see the end). Not tested yet: the store text promises it, so test before release or soften the text.
- [ ] Touch controls: FIRE / USE reachable, revive by holding USE.

## 3. Game Center (App Store Connect > Game Center)
- [x] `pkr.lb.laststand`, `pkr.lb.manor`, `pkr.lb.survivor_xp` created with en-US names (16 Oct).
- [x] `pkr.lb.pier` "Last Stand: Salt Pier" created (16 Oct).
- [ ] Add the new leaderboards to the version's Game Center section before submitting (they ship with the version).
- [ ] Multiplayer compatibility: the app's own bundle only (Game Center > Multiplayer Compatibility, nothing to add).

## 4. Store page (App Store Connect > 1.0)
- [x] Subtitle, promotional text, keywords and description from Docs/AppStore/LISTING.md (saved in ASC 16 Oct).
- [ ] Screenshots (6.9"): lead with the zombie shots from Tools/sim_trailer.command (Logs/trailer/shot_*.png) in the order in LISTING.md.
- [ ] App preview: the 15-30 s zombie clip (landscape 1920x886 or the size ASC asks for), poster frame from Last Stand.
- [ ] Age rating: re-answer the questionnaire for the zombie modes (cartoon / fantasy violence, frequent). It was set to 9+ before zombies led the game.
- [ ] App Privacy: still "Data Not Collected" (Game Center is Apple's; no analytics, no ads).
- [ ] Review notes: add (couldn't save on 16 Oct: ASC requires the App Review contact name/email/phone first) "ZOMBIES is the first main-menu button; Last Stand > SOLO starts a run; online co-op needs a second Game Center account".
- [ ] Phone number for App Review contact (Omari).
- [ ] Shadow Contracts IAP review screenshot attached.

## 5. Before pressing submit
- [ ] Everything in sections 1-4 ticked, or knowingly skipped.
- [ ] GitHub main matches the build that was uploaded.
- [ ] Release type: manual (so the release date is Omari's call).
