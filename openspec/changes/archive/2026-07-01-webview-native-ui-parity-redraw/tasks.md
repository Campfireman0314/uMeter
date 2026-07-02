# Tasks: 2026-07-01-webview-native-ui-parity-redraw

## Default Constraints

- Do not create additional OpenSpec changes for this work.
- Do not modify macOS runner, C++ Core, packaging, installer, JUCE, SenderKit, or protocol files.
- Keep HostBridge command surface unchanged unless a type-only alignment is required for an already-supported command.
- Shared Artifacts:
  - `.openspec.yaml` - controller updates only
  - `tasks.md` - controller updates only
  - `evidence.md` - controller updates only
- Worker Return Format: `DONE | DONE_WITH_CONCERNS | NEEDS_CONTEXT | BLOCKED`

## 1. Web Shell Parity

- [x] 1.1 [characterization-first] Capture native-shell parity expectations in Web tests

  **Requirement Trace**: [R1, R2, R4, R6] -> [U1, U2, U3]
  **Execution Method**: [characterization-first]
  **Allowed Files**:
  - `src/DesktopLoudnessMeter.Web/src/AppShell.test.tsx` - characterize expected shell regions and existing command wiring
  - `src/DesktopLoudnessMeter.Web/src/mockHostBridge.ts` - fixture tuning only if needed

  **Acceptance Criteria**:
  - [x] Tests describe compact app chrome, status/global controls, sources, recent events, settings, and selected detail landmarks.
  - [x] Tests preserve existing HostBridge command expectations for refresh, select detail, back, show-hidden, settings, chart mode, and visibility controls.
  - [x] Tests do not assert fake native-only source-management commands.

  **Verification Commands / Methods**:
  - `npm test` in `src/DesktopLoudnessMeter.Web`, expected: the new/updated characterization fails before implementation for layout-specific assertions, then passes after implementation.

  **Evidence Required**:
  - red/green component-test evidence
  - command wiring summary

  **Done When**:
  - Web test expectations lock the intended single-change UI redraw boundary.

  **Dependencies**: none

- [x] 1.2 [test-first] Redraw AppShell into native-style core UI

  **Requirement Trace**: [R1, R2, R3, R4] -> [U1, U2]
  **Execution Method**: [test-first]
  **Allowed Files**:
  - `src/DesktopLoudnessMeter.Web/src/AppShell.tsx` - shell composition and interaction wiring
  - `src/DesktopLoudnessMeter.Web/src/AppShell.test.tsx` - green/refactor coverage
  - `src/DesktopLoudnessMeter.Web/src/mockHostBridge.ts` - fixture tuning only if needed

  **Acceptance Criteria**:
  - [x] App chrome, status/global controls, source list, selected detail, recent events, and settings render in one native-client-style shell.
  - [x] Existing HostBridge commands still dispatch with the same payloads.
  - [x] Selected detail shows compact source identity, readouts, and stacked visible chart modules.
  - [x] No additional OpenSpec change is created.

  **Verification Commands / Methods**:
  - `npm test` in `src/DesktopLoudnessMeter.Web`, expected: pass.

  **Evidence Required**:
  - React component test pass evidence
  - changed interaction summary

  **Done When**:
  - AppShell behavior matches the new tests without expanding HostBridge command scope.

  **Dependencies**: Task 1.1

## 2. Visual Redraw And Responsiveness

- [x] 2.1 [test-first] Replace dashboard styling with compact native-meter styling

  **Requirement Trace**: [R1, R3, R4, R5] -> [U1]
  **Execution Method**: [test-first]
  **Allowed Files**:
  - `src/DesktopLoudnessMeter.Web/src/styles/app.css` - visual redraw, spacing, responsive layout
  - `src/DesktopLoudnessMeter.Web/src/AppShell.tsx` - class/name adjustments required by CSS
  - `src/DesktopLoudnessMeter.Web/src/AppShell.test.tsx` - class-independent behavior assertions only
  - `src/DesktopLoudnessMeter.Web/src/charts/**` - only small ARIA/layout adjustments if needed

  **Acceptance Criteria**:
  - [x] The UI uses compact meter-oriented bands/sections instead of a three-column dashboard.
  - [x] Source rows, readout tiles, toolbar buttons, and chart modules have stable dimensions.
  - [x] Chart modules stack predictably and canvases remain nonblank when data exists.
  - [x] Narrow viewport layout stacks without incoherent text/control overlap.

  **Verification Commands / Methods**:
  - `npm test` in `src/DesktopLoudnessMeter.Web`, expected: pass.
  - `npm run build` in `src/DesktopLoudnessMeter.Web`, expected: pass.

  **Evidence Required**:
  - Web test/build evidence
  - CSS/layout change summary

  **Done When**:
  - The Web app builds with native-style layout and stable chart surfaces.

  **Dependencies**: Task 1.2

## 3. Stabilization And Final Verification

- [x] 3.1 [stabilization-first] Run browser visual smoke and final verification

  **Requirement Trace**: [R5, R6] -> [U3]
  **Execution Method**: [stabilization-first]
  **Allowed Files**:
  - `src/DesktopLoudnessMeter.Web/src/AppShell.tsx` - within-scope visual/layout fixes
  - `src/DesktopLoudnessMeter.Web/src/styles/app.css` - within-scope responsive fixes
  - `src/DesktopLoudnessMeter.Web/src/charts/**` - within-scope canvas/layout fixes
  - `openspec/changes/2026-07-01-webview-native-ui-parity-redraw/**` - evidence and task status

  **Acceptance Criteria**:
  - [x] Browser smoke confirms desktop and narrow layouts have no obvious overlap.
  - [x] Browser smoke confirms visible chart canvases are nonblank when data exists.
  - [x] `npm test` passes.
  - [x] `npm run build` passes.
  - [x] `opsx changes list` or equivalent confirms no extra active change was created for the redraw.

  **Verification Commands / Methods**:
  - `npm test` in `src/DesktopLoudnessMeter.Web`, expected: pass.
  - `npm run build` in `src/DesktopLoudnessMeter.Web`, expected: pass.
  - Browser visual smoke at desktop and narrow viewport, expected: pass or documented blocker.
  - `opsx changes list`, expected: only this active change relevant to current work.

  **Evidence Required**:
  - Stabilization Rationale
  - Adjustment Ledger if visual fixes are made
  - Manual / Platform Matrix
  - Final Hardening
  - command outputs and screenshot/DOM/canvas smoke notes

  **Stabilization Rationale**:
  - Uncertainty sources: browser/WebView viewport size, CSS wrap behavior, canvas visibility, and text fit cannot be fully proven by jsdom.
  - Why not only target behavior tests: component tests validate wiring, but real browser layout and canvas pixels need visual smoke.
  - Exit conditions: tests/build pass, browser smoke passes or has a scoped non-blocking waiver, and no extra change has been created.

  **Manual / Platform Matrix**:
  | Coverage Item | Method | Blocking? | Expected |
  |---|---|---|---|
  | Browser desktop layout | dev server + browser screenshot/DOM/canvas checks | yes | compact native-style shell, nonblank charts, no obvious overlap |
  | Browser narrow layout | browser viewport check | yes | stacked stable controls, no incoherent text overflow |
  | Windows WebView host | launch if practical in current environment | no | same shell loads in host; if unavailable, document browser smoke as scoped evidence |

  **Final Hardening Plan**:
  - Re-run Web tests and production build after visual fixes.
  - Record final single-change boundary check.
  - doc impact classification: TASK_EVIDENCE_ONLY unless implementation reveals spec/design drift.

  **Done When**:
  - The redraw is visually stabilized and ready for verify/review without known blockers.

  **Dependencies**: Task 2.1
