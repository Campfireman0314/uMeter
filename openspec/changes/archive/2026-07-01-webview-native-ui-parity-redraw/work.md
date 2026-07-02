# Work Packet: 2026-07-01-webview-native-ui-parity-redraw

## Metadata

- profile: strict-change
- source_type: feature
- status: drafting
- created: 2026-07-01

## Split Decision

- Decision: KEEP_ONE_CHANGE
- Hard constraint: do not create extra OpenSpec changes for this UI redraw. Complete only this aggregate change unless a real blocker appears that cannot be verified safely inside the same change.
- Reason: the user-facing closure is "WebView core UI matches the native/WPF client information architecture and primary interactions." Header chrome, global controls, source list, detail readouts, chart modules, settings, responsive layout, tests, and visual smoke must land together to be reviewable. Splitting them into small UI-only changes would recreate the noisy change explosion the user explicitly rejected.
- Split guard: if a task appears to require macOS runner work, C++ Core work, platform packaging, or a new native host capability, stop and return to plan instead of silently expanding scope or opening another change.

## Intent

The current WebView UI already has HostBridge snapshots, settings commands, detail chart modules, and Canvas renderers, but its shell still reads like a three-column management dashboard. The native/WPF client uses a compact meter-oriented window: custom title chrome, status strip, dense global controls, recent event strip, source rows, selected detail readouts, and stacked detail modules.

This change redraws the WebView core UI to follow that native client shape while preserving the already implemented WebView + HostBridge contract. The result should feel like the real uMeter client, not a placeholder web dashboard.

## Scope

### In Scope

- Recompose `AppShell` into a native-client-style WebView shell:
  - compact title/chrome band
  - status/refresh strip
  - global controls area
  - recent events strip
  - source list
  - selected detail header/readouts
  - stacked chart modules and settings surfaces
- Redraw Web CSS to match the native client density, spacing, control hierarchy, and meter-first layout.
- Use existing HostBridge commands only unless a tiny web-side type adjustment is needed for an existing command.
- Preserve existing waveform, spectrum, stereo, loudness clock, and frequency collision chart behavior.
- Update Web component tests and mock HostBridge fixture data for the new UI structure and critical interactions.
- Run Web tests/build and browser visual smoke; run targeted .NET tests only if C# bridge files change.
- Maintain this single OpenSpec change and its evidence ledger.

### Out of Scope

- No new OpenSpec changes for submodules of this work.
- No `src/DesktopLoudnessMeter.Native/**` or C++ Core changes.
- No macOS runner, WKWebView runner, or platform packaging work.
- No installer/publish work.
- No WPF retirement/removal.
- No broad Host/native command expansion for alias, source hide/order/forget, tray commands, or window chrome commands unless the user explicitly re-scopes the change.
- No workspace schema migration.
- No unrelated cleanup of older archived or legacy OpenSpec directories.

## Success Criteria

- WebView first screen uses a compact native-client information architecture rather than the current three-column dashboard.
- Source selection, back-to-global, refresh, show-hidden, settings, chart mode, and chart visibility interactions still dispatch through HostBridge.
- Selected detail shows key readouts and stacked chart modules without layout overlap at desktop and mobile widths.
- Recent events and global settings remain reachable without becoming a separate page or marketing-style surface.
- Web tests and production build pass.
- Browser visual smoke confirms the UI renders nonblank charts and has no obvious overlap.
- This change remains the only active change created for the redraw.

## Requirements Summary

- [R1] WebView shell must mirror the native client information architecture.
- [R2] Global controls and source workflow must remain HostBridge-backed.
- [R3] Selected detail must present native-style readouts and stacked chart modules.
- [R4] Settings and recent events must remain usable in the compact shell.
- [R5] Layout must be responsive, stable, and visually verified.
- [R6] The change must not split into multiple OpenSpec changes or expand into platform/core work.

## Solution Summary

Keep the HostBridge/data layer stable and redraw the Web layer around it. `AppShell.tsx` will be reorganized into smaller view helpers only where it reduces local complexity. `app.css` will replace the current three-column panel treatment with a compact app chrome, global control band, source list, and selected detail stack. Tests will assert the new landmarks, command wiring, chart modules, and responsive-safe controls. Visual verification will use the Web dev server and browser screenshot/pixel checks where possible.

## Files / Ownership

### Allowed Files

- `src/DesktopLoudnessMeter.Web/src/AppShell.tsx` - core shell composition and UI wiring.
- `src/DesktopLoudnessMeter.Web/src/AppShell.test.tsx` - component tests for new layout and interactions.
- `src/DesktopLoudnessMeter.Web/src/styles/app.css` - visual redraw and responsive rules.
- `src/DesktopLoudnessMeter.Web/src/mockHostBridge.ts` - mock fixture tuning only if needed for smoke clarity.
- `src/DesktopLoudnessMeter.Web/src/charts/**` - only small layout/ARIA adjustments required by the redraw.
- `src/DesktopLoudnessMeter.Web/src/bridge/hostBridge.ts` - only type alignment for already-supported commands if needed.
- `openspec/changes/2026-07-01-webview-native-ui-parity-redraw/**` - this change's docs and evidence.

### Forbidden Files

- `src/DesktopLoudnessMeter.Native/**`
- `src/DesktopLoudnessMeter.JucePlugin/**`
- `src/DesktopLoudnessMeter.JuceAaxPlugin/**`
- `src/DesktopLoudnessMeter.Protocol/**`
- `src/DesktopLoudnessMeter.SenderKit/**`
- `src/DesktopLoudnessMeter.Setup/**`
- macOS runner/WKWebView runner files
- platform packaging and installer files
- unrelated OpenSpec changes or archives

## Git / Workspace

- Workspace Strategy: current-worktree
- Workspace Path: `E:\gitlab\uMeter`
- Workspace Branch: `codex/2026-07-01-host-source-reconcile-boundary`
- Current HEAD: `e0498f9770eb1f4e9f7ef4cad12bc0bae960399d`
- Base Branch: `main`
- Base HEAD: `4643c9626bd38f02570d6b9565b3bd52c331433e`
- Merge Target: current feature branch / eventual main integration
- Workspace Rationale:
  - dirty classification: before this change, the product working tree was clean; after init, dirty files are only `openspec/changes/2026-07-01-webview-native-ui-parity-redraw/**`, which belong to this change.
  - chain judgment: current branch is already a feature branch one commit ahead of origin with the prior native-host OpenSpec consolidation commit. This UI redraw is a continuation of the same WebView architecture track, so current-worktree is acceptable.
  - isolated trigger: no unrelated dirty files, no parallel worker, and no need to compare two product implementations side-by-side in separate worktrees. If unrelated dirty files appear, or if scope expands into platform/core work, pause and re-plan.
  - conclusion: use current-worktree and keep a single aggregate change.

## Verification Plan

- `opsx changes resolve 2026-07-01-webview-native-ui-parity-redraw`
- `npm test` in `src/DesktopLoudnessMeter.Web`
- `npm run build` in `src/DesktopLoudnessMeter.Web`
- Browser/dev-server visual smoke for desktop and mobile widths.
- If C# bridge files change: `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "HostBridge|DesktopMeterHostController"`

## Stabilization / Manual Uncertainty

- Need `[stabilization-first]`: yes.
- Uncertainty sources: real browser layout, text fit, chart canvas visibility, viewport differences, and WebView shell constraints.
- Manual/platform coverage: browser visual smoke is blocking; Windows WebView host smoke is desirable if the app can be launched locally without blocking, otherwise browser smoke plus Web tests/build is recorded as the scoped evidence.
- Final hardening strategy: component tests for command wiring and layout landmarks, production build, screenshot/DOM/canvas smoke, and evidence ledger updates.

## Rollback / Escalation

- Rollback: revert the Web AppShell/CSS/test fixture changes and return to the prior three-column UI.
- Escalation: if native-client parity requires missing source-management commands or platform window APIs, do not add them under this change without re-planning with the user.

## Bugfix Root Cause

none (feature)

## Rejected Attempts

- Rejected: split the redraw into separate chrome/source/detail/settings changes. Reason: user explicitly prohibited multi-change fragmentation and these pieces cannot be accepted independently as a coherent client UI.
- Rejected: extend the HostBridge command surface for all native source-management actions. Reason: that would turn this redraw into host capability work and risk another broad change set.

## Readiness Notes

- Readiness must confirm R1-R6 trace through spec/design/tasks.
- Readiness must confirm the no-extra-change constraint is explicit.
- Readiness must confirm no C++ Core, macOS runner, packaging, or unrelated OpenSpec files are in scope.
