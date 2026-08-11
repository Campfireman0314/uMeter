# Signal Shuffle

`Signal Shuffle` is an unreleased embedded uMeter game inspired by the layered triple-tile mechanic popularized by 羊了个羊, commonly rendered in English as `Sheep a Sheep`. It uses original naming, symbols, layout, and vector artwork rather than copied game assets or trade dress.

## Play Contract

- The board contains 222 tiles across thirteen layers with counts `4, 8, 13, 18, 23, 28, 34, 28, 23, 18, 13, 8, 4`, creating a narrow opening and dense hidden middle.
- A tile can be selected only when no higher-layer tile overlaps it.
- Selected tiles enter a seven-slot base tray and are grouped beside tiles of the same kind; Digger can expand it to eight once per round.
- Three identical tray tiles disappear immediately.
- Clearing the board wins; filling the current seven- or eight-slot capacity without a match loses.
- Sixteen original signal symbols increase tray pressure and visual scanning cost.
- Every generated seed has a constructive solution ordered from exposed upper layers downward. Several triples intentionally cross layer boundaries, requiring partial groups to remain in the tray and increasing pressure without creating unwinnable boards.
- Group kinds are dealt from spaced sixteen-kind decks, preventing nearby canonical groups from repeating the same symbol.
- Generation evaluates 48 deterministic position/kind candidates and selects the lowest local branch score. Tested seeds begin with exactly one immediately completable symbol family.
- Generated boards remain continuously playable with no midpoint rule interruption. Cannon, Fan, and Digger are optional strategic aids rather than required progress gates.
- Cards covered by one active layer remain dimly readable. Cards covered by two or more distinct active layers are rendered behind an opaque neutral veil, hiding both symbol and family color until enough upper layers are removed.

## Audio Contract

The game reads the UI-safe spectrum buckets from one selected uMeter source. It does not create another capture stream and does not execute work in an audio callback.

At 30 Hz, `AdaptiveBeatDetector` combines low-, mid-, and high-band energy, positive spectral flux, synchronized cross-band activity, zero-lag band correlation, and autocorrelation of recent onset strength. A periodicity estimate can lower the adaptive threshold by at most 18 percent near an expected pulse; periodicity never creates a beat without fresh onset evidence. The detector applies a 180 ms refractory period. `AudioDanceController` turns each accepted onset into a timed jump while retaining bounded background motion:

- bounce: 0 to 24 display units;
- jump arc: 460 ms from takeoff through apex to landing;
- animation onset guard: 320 ms; closer detected beats do not restart the jump;
- lean: -14 to +14 degrees;
- vertical squash/stretch: 0.93 to 1.02 scale;
- torso twist: -12 to +12 degrees;
- head rotation: -18 to +18 degrees, with up to 8 display units of vertical bob;
- shoulder rotation: -145 to +145 degrees and elbow rotation: -110 to +110 degrees;
- wrist/forehoof rotation: -45 to +45 degrees;
- hip rotation: -70 to +70 degrees and knee rotation: -115 to +115 degrees;
- ankle/hind-hoof rotation: -45 to +45 degrees;
- ear base rotation: -24 to +24 degrees, with delayed ear-tip rotation up to 30 degrees;
- tail and wool-fringe secondary motion: bounded to 32 and 16 degrees respectively.

Each accepted onset starts one centered 460 ms jump arc with explicit takeoff, apex, descent, and landing phases. Detected onsets arriving within the next 320 ms remain visible in diagnostics but do not restart the animation. The jump curve drives height, symmetric knee tuck, head bob, takeoff/landing squash, and a classic cheer arm path from low V to high V and back. A small opposite elbow follow-through keeps the otherwise straight arm chains from looking mechanical. Broadband signal energy adjusts jump strength and low-level idle bob. Ear tips, the two-stage tail, and two lower wool-fringe masses add damped follow-through. Silence settles at the centered low-V stance. Invalid and out-of-range input is clamped before animation. An opt-in overlay exposes bass energy, broadband signal energy, spectral flux, cross-band agreement, periodicity, adaptive baseline, threshold, onset strength, beat pulse, and sensitivity for tuning against real material.

## Rhythm Interaction

Rhythm feedback is cosmetic and never changes board generation, tile selectability, tray capacity, matching, win conditions, or loss conditions.

- One action can match each accepted onset within a symmetric 140 ms window. Actions up to 140 ms before an onset are retained briefly and scored when that onset arrives; gameplay itself is never delayed.
- Actions within 65 ms receive `Perfect`; other matched actions receive `On beat`. Off-beat actions have no penalty.
- Matched actions add to a `Groove` meter, which decays slowly at 2.5 percent per second rather than resetting.
- Four phrase pips advance on accepted onsets. They describe detector pulses, not claimed musical bars.
- Tempo uses the median of recent 250-1500 ms onset intervals. BPM and a beat-phase playhead appear only when consistency confidence reaches 0.72.
- Spectrum affinity is fixed and cosmetic: `Cloud`, `Moon`, `Bloom`, `Disc`, and `Meter` use low-band affinity; `Star`, `Sun`, `Diamond`, `Wave`, and `Bars` use mid-band affinity; `Note`, `Bolt`, `Pulse`, `Speaker`, `Orbit`, and `Arrows` use high-band affinity.
- Each family receives a continuous 0-1 envelope from its share of current spectral energy. A fast 80 ms attack and slower 220 ms release produce smooth fades and crossfades instead of gated/latching outlines.
- Completing a triple queues a board-centered rhythm echo for the next accepted onset while retaining the immediate match burst.
- Power-ups are round-limited manual actions and are never charged or strengthened by rhythm timing.

## Round Power-Ups

Each restart or new game grants one of each item:

- `Cannon`: clears every tray card and removes the complementary table cards required to complete those groups. The remaining table is reconstructed into a valid low-repeat solution order, so cannon use cannot make the round mathematically impossible. It can recover a full tray.
- `Fan`: preserves the current table-card multiset, moves kinds among existing positions, and reconstructs at least one completion that first resolves partial tray groups. It must be used before the tray reaches an unrecoverable timing point.
- `Digger`: permanently increases tray capacity from seven to eight for the current round and can immediately recover a seven-card loss.
- Each item is single-use. Restart and New Game restore all three.
- No item is mandatory. Cannon rewards tray-aware timing, Fan can repair a difficult partial-group state, and Digger provides one additional margin slot.

## Feedback Effects

- Every accepted tile selection emits a 300 ms tile-local ring and six-particle splash.
- Restart rebuilds the current seed; New Game selects a different seed. Both actions restore all power-ups, clear stale effects, and animate the rebuilt board into place.
- Cannon emits a tray-centered blast, launches every surviving table card upward, then applies a gravity fall beyond the window boundary before the surviving stack returns.
- Fan blows every table card above the window, then flies the shuffled cards from all four window corners and restacks them from lower to upper layers.
- Digger pulses the resized tray.
- Completing a triple emits a 620 ms tray-centered glyph, two expanding rings, a sixteen-particle upward fan, and a short tray pulse.
- The next accepted onset after a triple emits a 440 ms board-centered echo with two rings and ten particles.
- Winning presents a non-blocking 2.7-second full-width celebration banner with localized clear and match text, while the sheep performs three airborne high-V cheer jumps.
- Losing triggers a 1.42-second articulated rag-doll fall with delayed head/ear motion, independently swinging limbs, closing eyes, and a held collapsed pose.
- The view model emits semantic selection and match events; the WPF window owns animation objects and removes every effect layer when its animation completes.
- Effect canvases never participate in hit testing, so feedback cannot block or alter gameplay input.

## UI Ownership

- The game icon sits immediately beside the theme selector in the main title bar.
- One modeless `GameWindow` can be open at a time.
- The game window is owned by the main window and closes with it.
- Theme resources and the existing four-language localization traversal are reused.
- The board coordinates are rotated into a 5-by-7 portrait footprint while tile symbols remain upright.
- A compact strip above the board displays four phrase pips, groove, confidence-gated BPM/phase, and transient timing feedback.
- The portrait board backing is 50% opaque so the dancer remains visible through uncovered plate areas; tiles, borders, symbols, and input targets remain fully opaque.
- Tiles covered by one layer receive a dark occlusion veil. Tiles covered by two or more active layers receive a fully opaque neutral veil, so their glyphs and family colors cannot be identified. All covered tiles suppress their affinity rim; selectable tiles remain saturated and visually dominant.
- The power rail uses dedicated vector silhouettes for a wheeled cannon, folding fan, and shovel rather than generic target/shuffle/plus glyphs.
- `DancingSheepView` is a replaceable 260 x 390 WPF rig prototype based on the Plan 2 direction. It uses seven torso wool masses, independent expression parts, two segments plus a hoof per limb, two-stage ears and tail, and a fixed rear stack for legs and both arm roots beneath the torso wool.
- The production-art handoff, pivot coordinates, layer names, acceptance checks, and Cubism decision gate are defined in `docs/dancing-sheep-rig.md`.
- The face uses a graphite head with a pure white muzzle so the mouth remains distinct across themes.

## Current Limits

- No persistent score or saved in-progress board.
- No undo or unlimited assistance. Cannon, Fan, and Digger are each available once per round and remain optional.
- Final painted sheep, tile, background, motion, and sound art remain open; the WPF rig and layer contract are now fixed for the next asset pass.
- Tempo confidence and phase are visual only; jump takeoff still follows accepted onset timing rather than rescheduling gameplay or animation to a predicted future beat.

## Research Basis

- A 2022 numerical-design analysis describes the viral mini-program's second-stage pacing as many symbol types across a small-start, dense-middle, small-end layer distribution. Signal Shuffle adopts that general information-pressure curve at smaller scale, not the original map: https://maimai.cn/article/detail?fid=1756056437&efid=uAe2sNMx5ilsv0k8NxruWw
- The public SheepSolver project characterizes the game as a backtracking/topology problem whose draw strategy materially affects solvability. Signal Shuffle retains a guaranteed constructive solution sequence instead of copying captured map data: https://github.com/NB-Dragon/SheepSolver
- The detector direction follows established beat-tracking practice that uses spectral event flux and autocorrelation to estimate periodicities, including `Template-Based Estimation of Time-Varying Tempo`: https://doi.org/10.1155/2007/67215

## Verification

- Managed solution build: passed with zero warnings and zero errors.
- Managed suite: 396 passing tests.
- Articulated-rig controller coverage checks the timed rise/apex/landing curve, close-onset rejection, low-V/high-V cheer transition, bounded joint output, secondary follow-through, and jump settling.
- Off-screen WPF renders of the low-V stance and high-V jumping apex: 32,992 to 33,881 nontransparent pixels per 260 x 390 frame; all alpha bounds remain inside the logical canvas.
- Pose inspection: the white muzzle, wool-occluded shoulder roots, straight split-hoof arm lines, symmetric knee tuck, and airborne high V remain legible.
- Localization catalog coverage includes the game window and computed state text.
