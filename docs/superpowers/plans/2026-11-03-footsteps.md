# Walking audio and fading footprints

**Goal:** Play footsteps and leave faint alternating shoe prints only while the character actually walks. Existing prints fade out over time.

**Architecture:** `PlayerController` automatically adds or reuses `PlayerFootsteps` and supplies movement intent. That component measures actual horizontal travel and checks the floor, then plays one short sound and stamps one footprint per step. A separate `FootprintTrail` owns a bounded pool of world-space prints and fades them even when the player stops.

**Tech stack:** Existing Unity 6000.5, Rigidbody, AudioSource, Physics, shared URP Unlit material, original locally generated audio/texture assets. No new packages.

**Current defaults after user tuning:** Small black shoe prints in left/right pairs with a soft gray edge, size 0.14 x 0.27 m, opacity 0.45; lifetime 2 seconds, fading throughout those 2 seconds; footstep volume 0.65; 64 marks maximum. Walking and running share distance-based cadence with a minimum interval and emit one sound per pair.

**Workspace:** Apply directly to the current checkout; preserve its existing scene, lighting, package and gameplay changes. Do not save or rewrite scenes. New behavior applies to all scenes using PlayerController.

## Tasks

- [x] Write a Unity behavioral runner and demonstrate that the requested footstep component is missing before implementation.
- [x] Add the distance/ground/movement gates, alternating stamps, shared-material fading pool, and movement-intent integration.
- [x] Generate four original short shoe-on-floor audio variations and a shoe-tread alpha texture/material under Resources/PlayerFootsteps.
- [x] Run Unity checks for actual movement, standing, blocked movement, no intent, airborne movement, pause, teleport, alternating world-space placement, fade-out and pool bounds; check audio/texture imports.
- [x] Inspect a rendered footprint trail, review the implementation, rerun relevant checks, and confirm existing scene files are unchanged.

## Review focus

- Input held against a wall must not produce footprints or repeated step sounds.
- Pausing must not emit or age marks; previously emitted marks must still fade after stopping movement.
- Airborne movement, triggers, self-colliders, walls, and teleporting must not create stray prints.
- Prints stay on the floor in world space and alternate sides; allocation remains bounded and cleanup follows the player scene.
- Existing interaction/game-over gates prevent steps; audio must not loop or play on awakening.

## Verification

Use a disposable Unity project under `.superpowers/footsteps/UnityCheck` with production effect scripts and a retained runner at `Tests/Unity/FootstepEffectsChecks.cs`. Execute `FootstepEffectsChecks.Run` through Unity batch mode; success is a report containing only PASS lines and exit code 0. Use actual Rigidbody, Collider, Physics, Renderer and AudioClip objects. The runner is independent of the gameplay scene and its unsaved state.

Run again with `Tests/Unity/RunFootstepChecks.ps1`. Final result: 27 PASS, exit 0. Full Assembly-CSharp source compilation with the project's own generated compiler references also exits 0 (existing obsolete-API/unused-field warnings remain). The original source encoding in PlayerController.cs is preserved; its integration adds 12 lines.

Independent read-only review found that a loose ground probe could stamp during a short fall. A regression with the feet 0.2 m above the floor failed before the fix and passed after restricting contact tolerance to 0.06 m; the full 27-check suite passed again. No other important findings were reported.

Rendered the actual footprint mesh, shoe texture and fading at fresh, half-faded and expired states in the disposable project, using a built-in transparent preview material. The preview is a visual check of the effect, not a gameplay-scene render. Existing scene and room-light files remain unchanged.

Latest feedback repair: verified paired marks on the actual Level1 floor and captured a close-up through the real URP/post-processing camera. Updated pair-count regressions failed before implementation and the complete 27-check suite passed after it. The user-set ground tolerance 0.03 is preserved. Independent review found no important issues.

The Level1 house-creak source was missing although its original clip remained. Restored Distant House Creaks (volume 0.18), raised Quiet Room Tone to 0.12, and added an original 40-second HauntedHouseDrone loop as Low House Rumble (volume 0.1, 2D). Live Play-mode inspection confirmed all three clips loaded and playing, listener volume 1, and no mute. Scene saved, camera/player test movement reverted, and temporary Editor authoring removed.
