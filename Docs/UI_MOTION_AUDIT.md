# UI motion audit against Emil Kowalski's design-engineering skill (28 Oct 2026)

Source: Emil Kowalski's public skill repository (github.com/emilkowalski/skill): `emil-design-eng/SKILL.md`,
`review-animations/STANDARDS.md` and `ui-cheatsheet.md`. The skill is written for CSS on the web. Here it is applied
to Unity uGUI. CSS easing curves become `PKR.Core.Ease` (a cubic-bezier solver with the skill's three curves), CSS
transitions become `UiMotion` (unscaled-time, interruptible tweens), and `:active` becomes `PressScale` or the pixel
key press. Tabular numbers come from the pixel font, whose digits are all one width.

| Before | After | Why |
| --- | --- | --- |
| Scene changes cut hard from one screen to the next | Cover fade 160 ms, reveal 220 ms, both strong ease-out; the reveal never blocks taps; a load during a reveal covers from where it is | Elements appearing with no transition feel broken. Screen changes are occasional, so a short transition is right. |
| Pause menu, results screens, Last Stand and Survival game over, Arena setup and results, Versus results, contract briefing, cutscenes, Zombie Land map and shop: appeared in one frame | Scrim fades in; the panel fades in from 97% scale, centred, over 200 ms on cubic-bezier(0.23, 1, 0.32, 1) | Never appear from nothing. Pair scale with opacity. Use a strong custom ease-out. Modals stay centred (they aren't anchored to a trigger). |
| Pause menu, Zombie Land map and shop vanished in one frame on close | 130 ms fade; touches stop at once | Exits are faster than entrances, but shouldn't vanish in a frame. The game resumes immediately; only the fade lingers. |
| Pause menu to Settings and back, map and shop tabs, Zombies menu pages | No entrance; they just change | Moving between pages of an open menu happens tens of times a session. Replaying an entrance each step makes it feel slow. |
| Progress toasts: linear slide, the same 250 ms in and out, no fade | In by its own height from the top edge with a fade, 220 ms ease-out; back out the same way in 150 ms | Spatial consistency (it leaves the way it came). Asymmetric timing. Linear isn't for UI. |
| Zombie HUD messages snapped on, then faded out over 0.5 s linear | Rise 12 px and fade in (180 ms ease-out); drift up 8 px and fade out (140 ms) | Prevents jarring changes. Exit faster than entrance. |
| First-time tip banner: background popped on and off while only the text faded | The whole banner drops 14 px into place and fades in (220 ms); it leaves the same way (140 ms) | Pair movement with opacity, and fade the whole element, not part of it. |
| Comic page turn: art jumped to 108% and shrank back (linear) | The next page slides 28 px from the right and fades up (160 ms ease-out); a fast tap continues from its real resting place | Spatial consistency: pages move forward in reading direction. Interruptible. |
| Results reward counted up linearly over 1.2 s in a proportional font (digits wobbled) | 0.9 s ease-out count in the pixel font (equal-width digits) | Tabular numbers for values that change. A count that decelerates onto its total feels earned, not mechanical. |
| Squad readout (HP and points change every hit) in a proportional font | Pixel font with equal-width digits | Numbers jitter when they change. |
| World map marker: 350 ms SmoothStep hop, 60 px arc, cartoon squash (15%); bounced 10 px fast while resting; node pulsed 6% fast | 300 ms strong ease-in-out, 36 px arc, 5% stretch; rests with a slow 4 px breath; node pulse 3%, slower; all still with Reduced motion | On-screen movement uses ease-in-out. Constant decorative motion on something always visible is noise. |
| Character cards snapped between 110% and 92% | Ease to 106% and 95% over 140 ms, interruptible | Prevents a jarring jump; frequent navigation, so short and small. |
| Lost heart jumped to 160% for a frame | 130% kick, settles; none with Reduced motion | It happens many times a level: smaller. |
| Stuck jetpack button pulsed ±15% fast, even with Reduced motion | An even 8% breath, slower; still with Reduced motion | Gentle decorative motion; reduced motion removes movement. |
| Pause and sound icon buttons: colour tint only | Press to 96%, ease back in 160 ms (`PressScale`) | Pressable elements must answer the press (0.95-0.98). |
| World-unlock doors: 1.4 s SmoothStep | 1.2 s strong ease-in-out | Rare, celebratory: it can be long, but on the strong curve. |
| "1 kills", "1 revives", "(1 zombies down)", "+1 Star Shards" | "1 kill", "1 revive", "1 zombie", "+1 Star Shard" (`Plural.Of`) | Counts must read right. |
| Easing written ad hoc (linear, SmoothStep, Lerp) in each script | One `Ease` set: Out (0.23, 1, 0.32, 1), InOut (0.77, 0, 0.175, 1), Drawer (0.32, 0.72, 0, 1), with shared timings; unit-tested against an independent solver | Built-in curves are too weak; cohesion comes from one vocabulary. |

Reduced motion everywhere: `UiMotion` keeps fades and drops slides and scales. Toasts and tips fade without moving.
The map marker and the jetpack hint hold still. Lost hearts don't kick.

Kept as they were:
- The pixel key buttons. The key sinks on press and pops back on release, instantly. A physical key doesn't animate.
- The touch controls (FIRE, USE, the stick). Gameplay input must be instant.
- Settings toggles flip at once.
- Gameplay motion (zombies, bullets, camera, power-up bobs) is game feel, not UI chrome.

Not checked by eye yet: the new motion is covered by tests (timings, curves, the pause menu fade and its touch
blocking), not by watching it on a phone.
