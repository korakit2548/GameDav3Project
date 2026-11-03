# Film atmosphere implementation plan

**Goal:** Add a restrained old-film horror atmosphere to the open Level1 scene.
**Approved scope:** The user approved implementation, then narrowed it to supplementary atmosphere only.
**Architecture:** A separate Global Volume, sparse dust Particle Systems, and quiet ambient AudioSources. Author assets through the live Editor and preserve gameplay, existing lights, warning colors, camera transforms, and scene layout.
**Tech stack:** Unity 6000.5.1f1, URP 17.5, native Volume/ParticleSystem/AudioSource components.

- [x] Reconnect the live Editor and back up the current unsaved scene as a copy.
- [x] Inspect existing volumes and tune a separate film profile without darkening navigation excessively.
- [x] Add sparse dust and subtle ambient sound under a single removable atmosphere root.
- [x] Verify rendered before/after views, advancing Play-mode frames, configured assets, and console errors.
- [x] Save the final scene and remove temporary connection tooling.

Verification uses live rendering and component inspection; no gameplay code is introduced. Use the current checkout because it contains the user's open unsaved scene. Do not commit pre-existing user changes.

Scope ruling after the user's clarification: add supplemental effects only; do not change existing light settings, warning colors, geometry, or gameplay. Keep existing volume assets intact and layer a separately removable Volume above them. Scratch setup scripts are Editor authoring tools, not runtime project code.

Verification: Play-mode frames advanced (633 to 656 during a 121ms wait); all four dust systems running at about 45 particles each; both 40/48-second audio clips loaded and playing; generated clip samples are nonzero, listener active/unmuted; zero console errors. The original red room warning remains visible in Play mode. Before/after renders inspected.

Independent read-only review: no Critical or Important findings. All 309 original scene documents are identical to the unsaved-scene backup except SceneRoots gaining the atmosphere root; only 25 new documents added. Assets and references resolve. Dust has no collisions, triggers, shadow casting or subemitters. No runtime code retained.

Final state: Level1 saved with the supplement. Temporary Pipeline dependency, helper script, settings and asset-folder screenshots removed; package manifests match their baseline. Preview PNGs moved to the conversation visualization directory. Post-cleanup log contains no C# compilation errors or missing/null reference exceptions.

User correction: floating dust was too conspicuous indoors. Disabled and cleared all four Dust GameObjects through the live Editor and saved Level1. Film profile and ambient audio remain enabled. Temporary authoring helper removed.

## Approved lighting revision

User explicitly approved changing room lights and shadows after the supplement alone did not achieve the intended horror atmosphere. Saved backup: `.superpowers/film-atmosphere/Level1-before-light-retune.unity`.

- Four room lights now cast soft shadows, with shorter ranges and warm/cool normal colors. Room normal-light colors match their lights; warning/cursed colors are unchanged.
- Living-room key light now shines diagonally from (-11.68, 4.58, -6.29), intensity 18, producing a visible long character shadow. Kitchen/bathroom/bedroom intensities are 19/16/18. Bathroom light lowered to Y=4.1 below the ceiling.
- Flat ambient color (0.2,0.235,0.29), reflection intensity 0.1. Supplement profile: ACES, exposure +0.5, contrast 15, saturation -20, White Balance -10/-2, Thin1 grain 0.12 and Bloom 0.08.
- All four dust systems remain inactive. Geometry, camera transforms, gameplay scripts and warning colors preserved.
- A temporary experimental cookie light triggered a DX12 GPU device crash during offscreen verification. The experimental object and cookie were removed; Unity was reopened with the DX11 launch flag. No persistent project graphics-API change was made.
- Independent read-only review found no Critical/Important issues. Final live Play-mode verification advanced frames 1 to 31 and 1 to 61, confirmed inactive dust and retained room colors, and captured `haunted-film/horror-final-play.png` in the conversation visualization directory. The existing red warning light was also observed working. Fresh reopened-Editor log had no C# errors, null/missing-reference exceptions, or further GPU crash.
- Temporary Editor authoring and verification scripts removed after testing; Level1 remains saved.
