# Design: 2026-07-01-webview-native-ui-parity-redraw

## Users

- [U1] Desktop uMeter user who expects the WebView client to feel like the existing native/WPF meter window.
- [U2] WebView migration maintainer who needs the redraw to stay on the existing HostBridge contract.
- [U3] Reviewer who needs a bounded, single-change diff with clear visual and automated evidence.

## Current State

- WPF `MainWindow.xaml` uses a narrow, dense client layout: custom title bar, status strip, global controls, recent events, source rows, and inline detail modules.
- WPF `DetailWindow.xaml` uses a compact selected-source header, readouts, module controls, and stacked chart panels.
- Web `AppShell.tsx` currently exposes the right data and basic controls, but arranges them as source/detail/side panes with a dashboard flavor.
- Web chart parity is already implemented through Canvas modules and HostBridge DTOs.
- HostBridge does not currently expose native source-management commands for alias edit, hide/restore, reorder, or forget. This redraw will not fake those as persisted actions.

## Proposed UI Shape

The WebView shell will become a compact app surface:

1. App chrome:
   - title and subtitle
   - refresh/settings icon buttons
   - compact show-hidden toggle
2. Status and global controls:
   - host status text
   - source count / selected state
   - theme and recent settings summary
3. Main meter body:
   - source list, optimized for scanning active/missing/hidden state and loudness readout
   - selected detail header with source identity and back command
   - native-style readout meters/cards for peak, momentary, short term, integrated, true peak, RMS
   - stacked detail chart modules
4. Secondary surfaces:
   - recent events strip/list
   - settings drawer/panel that keeps the user in the same screen

## Interaction Model

- `refresh`: existing command, top chrome.
- `setShowHiddenSources`: existing command, global control.
- `selectDetail`: existing command, source rows.
- `backToGlobal`: existing command, selected detail header.
- `setTheme`, `setRecentEventLimit`, `setEventOutputFolder`: existing settings commands.
- `setWaveformDisplayMode`, `setSpectrumDisplayMode`, `setDetailModuleVisibility`: existing chart controls.

Native-only WPF actions without HostBridge support remain absent or read-only. The UI may show state such as hidden/missing, but it must not present a fake persistent action.

## Layout Strategy

- Prefer a single app shell with constrained max width for large browsers and natural fill inside WebView.
- Use full-width bands and functional panels; avoid nested decorative cards.
- Use fixed/min dimensions for toolbar buttons, source rows, readout tiles, chart modules, and compact controls.
- Stack modules vertically to match native detail behavior and avoid dashboard grid crowding.
- Use responsive CSS breakpoints for narrow widths:
  - controls wrap in stable rows
  - source/detail regions stack
  - chart controls wrap without changing canvas minimum height

## Testing Strategy

- Update React tests to assert:
  - native-style shell regions render
  - existing command wiring is preserved
  - selected detail readouts and chart modules render
  - settings and recent events remain reachable
  - empty states remain stable
- Preserve existing chart tests unless a small ARIA/layout adjustment is required.
- Use production build as a compile/bundling gate.
- Use browser visual smoke to inspect desktop and narrow widths, plus chart canvas nonblank checks where feasible.

## Risks And Mitigations

- Risk: visual parity becomes too broad.
  - Mitigation: limit to core shell and existing interactions; do not add native-only command capabilities.
- Risk: CSS redraw breaks chart canvas sizing.
  - Mitigation: keep chart module min heights and run browser smoke.
- Risk: text overlap in compact controls.
  - Mitigation: use explicit min/max dimensions, wrapping/truncation, and narrow viewport smoke.
- Risk: change fragmentation returns.
  - Mitigation: tasks and scope explicitly forbid additional OpenSpec changes.

## Non-Goals

- macOS runner/WKWebView implementation.
- C++ Core or DSP changes.
- Platform packaging.
- WPF retirement.
- New persistent source-management command families.
