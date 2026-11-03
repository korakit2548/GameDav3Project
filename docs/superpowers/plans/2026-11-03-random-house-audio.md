# Random house audio effects

**Goal:** Add occasional random door knocks and falling-object sounds to the active Level1 house, and raise the existing background hiss slightly.

**Approach:** Add one RandomHouseAudioEffects component to the existing Atmosphere root. It owns a separate spatial AudioSource, chooses from three original knock and three original drop recordings, and uses assigned door/room points. A local System.Random preserves gameplay randomness. Scaled-time scheduling pauses with the game; a playing event finishes before another starts.

**Defaults after the user's spacing request:** Random 20–40 second quiet intervals after the previous effect finishes, varied pitch and volume, no immediate repeat of the same clip. Background Quiet Room Tone volume 0.12 -> 0.15. Other ambience, footsteps, footprints, lighting, and gameplay remain unchanged.

**Workspace:** Current checkout contains the user's open scene and other edits. Back up the live scene before authoring, change only the requested audio, and remove temporary Editor tools afterward. No package changes.

- [x] Add a Unity behavioral runner and observe failure for the missing component.
- [x] Generate six original short audio assets and implement the scheduler/emitter.
- [x] Verify timing, pause, spatial anchors, no overlap, asset data, repeat avoidance, and preservation of Unity's random state.
- [x] Attach/configure in the live Level1 scene, increase hiss, verify actual playback, save, and remove temporary tooling.
- [x] Run relevant regression checks and review the final change.

**Final tuning:** Door knocks use low hollow impacts and room echoes; falling objects use heavier thuds, bounces and metallic resonance. Six recordings last 2.8–2.9 seconds including their tails. Quiet time starts after playback completes, so the echo tail does not consume the next 20–40 second interval.

**Validation:** Missing-component and quiet-gap regressions were observed failing before implementation/fix. All 20 random-audio checks and 27 existing footstep checks pass. Live Level1 playback confirmed both categories at their corresponding door/room positions, background hiss volume 0.15 and a scheduled delay within 20–40 seconds. Review flagged old serialized scene delays, which were updated and confirmed as 20/40 on disk. The final echoed recordings were reimported and passed the Unity audio-data checks. A longer live gap test was interrupted by normal game-over pausing; timing behavior is verified by the regression suite. Temporary authoring was removed and Unity returned to edit mode.
