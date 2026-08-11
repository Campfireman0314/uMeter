# Dancing Sheep Rig Plan

This document locks the Plan 2 `Long-Leg Groove` direction into an asset and runtime contract. The current WPF vector character is a functional rig prototype, not final painted art.

## Design Lock

The final model keeps the concept's tall, syncopated silhouette while simplifying it for repeated real-time motion:

- Shorten the concept limbs by roughly 15 percent so raised poses stay inside the game window.
- Build the torso from seven clean, overlapping wool masses. Texture belongs inside each mass and must not define its silhouette.
- Keep a graphite face and limbs, off-white wool, a pure white muzzle, and restrained cyan/coral details.
- Keep forelegs and hind legs visibly sheep-like. Every terminal shape is a split hoof; no hand or shoe shapes.
- Use two rigid-looking segments per limb with soft wool cuffs hiding rotational seams.
- Keep subtle asymmetry in wool and facial contours, while limb movement remains mirrored for the centered jump.

## Required Model Sheet

The production art pass must deliver one sheet at a consistent scale with these panels:

1. Neutral front pose with all pivots visible and limbs clear of the torso.
2. Three-quarter front pose matching the in-game camera.
3. Side silhouette showing muzzle projection, torso depth, tail base, and hoof contact.
4. Takeoff, airborne apex, descent, and centered landing references for one vertical jump.
5. Face row: neutral, blink, smile, open mouth, and wide beat accent.
6. Joint map with shoulder, elbow, wrist, hip, knee, ankle, ear-base, ear-tip, tail-base, and tail-tip pivots.
7. Fixed layer-stack example with both hind legs and both foreleg roots behind the torso wool.

Do not paint over joints on the sheet. Each overlap needs enough hidden artwork to survive at least 35 degrees beyond its expected motion range without exposing a gap.

## Layer Manifest

Artwork should be exported at 2x the intended display size with transparent padding retained around every pivot.

| Group | Required layers | Runtime motion |
| --- | --- | --- |
| Back | optional shadow | opacity and scale only |
| Tail | `tail_base`, `tail_tip` | two-stage rotation with delayed follow-through |
| Left hind leg | `lh_thigh`, `lh_knee_cuff`, `lh_shin`, `lh_ankle_cuff`, `lh_hoof` | hip, knee, ankle |
| Right hind leg | `rh_thigh`, `rh_knee_cuff`, `rh_shin`, `rh_ankle_cuff`, `rh_hoof` | hip, knee, ankle |
| Torso | `wool_core`, seven wool masses, `fringe_left`, `fringe_right` | translation, rotation, squash, twist, delayed fringe |
| Left foreleg | `lf_upper`, `lf_elbow_cuff`, `lf_lower`, `lf_wrist_cuff`, `lf_hoof` | shoulder, elbow, wrist |
| Right foreleg | `rf_upper`, `rf_elbow_cuff`, `rf_lower`, `rf_wrist_cuff`, `rf_hoof` | shoulder, elbow, wrist |
| Head | `head`, `muzzle`, `nose`, `mouth_closed`, `mouth_open`, cheeks | angle, bob, expression scale/state |
| Eyes | left/right white, iris, pupil, highlight, upper lid | blink and later gaze |
| Ears | left/right base and tip | authored base angle plus delayed tip |
| Head wool | three to five masses | follows head; optional later secondary motion |

Layer names are part of the import contract and should not contain spaces. Left and right refer to the character's own sides.

## Prototype Coordinates

`DancingSheepView` uses a 260 x 390 logical canvas. The current anchor points are:

| Pivot | X | Y |
| --- | ---: | ---: |
| Left shoulder | 105 | 174 |
| Right shoulder | 155 | 174 |
| Left hip | 103 | 233 |
| Right hip | 157 | 233 |
| Head group origin | 45 | 35 |
| Tail base | 187 | 190 |
| Ground line | - | 370 |

Final raster or vector parts may refine local contours, but moving these anchors requires updating the choreography poses and render framing together.

## Runtime Ownership

`AdaptiveBeatDetector` accepts multi-band spectral-flux onsets with correlation/periodicity support. `AudioDanceController` owns choreography and returns one bounded `SheepDanceFrame` per UI tick.

- Audio selects accent timing and scales intensity; it does not drive individual joints.
- One authored 460 ms jump curve defines takeoff, airborne apex, descent, and landing compression.
- Accepted onsets inside a 320 ms animation guard are ignored by choreography so the active jump is not restarted.
- The jump arc drives vertical travel, mirrored knee tuck, head bob, expression, squash, and a low-V-to-high-V cheer arm path; audio energy only scales intensity.
- Both arm chains remain nearly straight, with a small opposite elbow follow-through through takeoff and descent.
- Ear tips, tail sections, and lower wool fringe run through damped spring followers after jump evaluation.
- The WPF view keeps both hind legs and both complete foreleg chains behind the torso wool so the shoulder roots remain integrated.
- The WPF view applies normal dance transforms and draws layers; it contains no beat decisions.
- Round outcomes temporarily override the bound joint transforms: win runs three 2.18-second high-V cheer jumps, while loss runs a 1.42-second inward rag-doll fall and holds the collapsed pose.
- Restart, New Game, Cannon recovery, or Digger recovery clears outcome animation clocks and restores the live audio-bound pose.

## Cubism Decision Gate

Keep the WPF renderer for the next art pass unless at least one of these requirements survives hands-on review:

- The muzzle or face needs continuous non-affine deformation rather than discrete expression layers and scale transforms.
- Torso turns require convincing perspective deformation across wool masses, not rotation plus layer swapping.
- Limb seams remain visible after hidden overlap and wool cuffs are tuned.
- More than two deforming parameters per art layer are needed for acceptable motion.
- The final art cannot hold 60 UI frames per second on the project's minimum Windows target when rendered as WPF vectors or bitmaps.

If none applies, Cubism adds packaging, native-runtime, authoring, and licensing cost without improving the approved motion. If one applies, prototype only that failing shot in Cubism and compare visual quality, frame time, package size, and asset iteration time before migrating the whole character.

## Acceptance Checks

- Takeoff and landing hold a readable low V, while the airborne apex reaches a balanced high V at the in-game 0.68 opacity.
- Both shoulder roots stay behind the wool and both split hooves remain inside the logical canvas.
- Both knees tuck symmetrically without exposing a joint gap.
- Both planted hooves retain a clear ground-contact edge during squash.
- The muzzle stays white and the mouth remains visible in every application theme.
- Ear tips and tail settle without sustained oscillation after five seconds of silence.
- Normal dance poses remain inside the 260 x 390 logical canvas after the parent Viewbox is applied; the rag-doll fall may extend limbs into the surrounding game surface but remains inside the window boundary.
