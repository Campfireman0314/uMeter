# Spec: 2026-07-01-webview-native-ui-parity-redraw

## New Requirements

### Requirement: Native-Style WebView Shell
**Trace**: R1
**Slice**: webview-shell-redraw
The WebView main shell must follow the existing native/WPF client information architecture instead of the current three-column dashboard layout.

#### Scenario: Shell presents native client hierarchy
- **Given** a HostBridge snapshot is available
- **When** the WebView app renders its first screen
- **Then** the UI shows compact app chrome, status, global controls, recent events, sources, and selected detail in a meter-first hierarchy
- **And** the shell does not use a landing-page, hero, or card-heavy dashboard composition

#### Scenario: Shell works before native snapshot loads
- **Given** the HostBridge snapshot is still loading or unavailable
- **When** the WebView app renders
- **Then** the shell keeps stable native-style regions
- **And** empty/loading states do not resize surrounding controls unexpectedly

### Requirement: HostBridge-Backed Global And Source Workflow
**Trace**: R2
**Slice**: webview-hostbridge-workflow
The redesigned UI must preserve existing HostBridge-backed workflow controls.

#### Scenario: Existing commands remain wired
- **Given** a source is visible in the snapshot
- **When** the user triggers refresh, source selection, back-to-global, show-hidden, settings, chart mode, or chart visibility controls
- **Then** the Web UI sends the existing HostBridge command with the expected payload
- **And** the returned snapshot updates the UI

#### Scenario: Missing native-only commands are not faked
- **Given** a native/WPF interaction has no current HostBridge command
- **When** the WebView UI is redrawn
- **Then** the UI does not invent a local-only stateful command that appears persisted
- **And** the missing capability is treated as out of scope for this redraw

### Requirement: Native-Style Selected Detail
**Trace**: R3
**Slice**: webview-detail-redraw
Selected detail must use a native-client-style meter layout with compact readouts and stacked chart modules.

#### Scenario: Selected source shows readouts and charts
- **Given** a selected source has detail modules and chart payloads
- **When** the detail area renders
- **Then** key loudness/peak readouts are visible near the selected source identity
- **And** waveform, spectrum, stereo, loudness clock, and frequency collision modules render as stable stacked modules

#### Scenario: Chart empty states remain stable
- **Given** a visible chart module has no data
- **When** the module renders its empty state
- **Then** the module keeps a stable minimum size
- **And** neighboring controls and labels do not overlap

### Requirement: Compact Settings And Recent Events
**Trace**: R4
**Slice**: webview-settings-events
Settings and recent events must remain usable inside the compact shell.

#### Scenario: Recent events are scannable
- **Given** recent events exist
- **When** the shell renders
- **Then** recent event time, title, detail, and severity are available in a compact strip or list
- **And** an empty recent state remains stable when no events are present

#### Scenario: Settings stay reachable
- **Given** global settings are available in the snapshot
- **When** the user opens the settings surface
- **Then** theme, recent limit, event output folder, and show-hidden controls remain editable
- **And** closing the settings surface restores the compact shell without losing state

### Requirement: Responsive And Visually Stable Layout
**Trace**: R5
**Slice**: webview-layout-stability
The redesigned WebView UI must fit desktop and mobile-width WebView surfaces without incoherent overlap.

#### Scenario: Desktop viewport is dense but readable
- **Given** a desktop WebView-sized viewport
- **When** the app renders with a selected source
- **Then** controls, source rows, readouts, and chart modules are visible without obvious overlap
- **And** chart canvases are nonblank when data exists

#### Scenario: Narrow viewport stacks predictably
- **Given** a narrow viewport
- **When** the app renders
- **Then** fixed-format controls preserve stable dimensions
- **And** text wraps or truncates without escaping its parent controls

### Requirement: Single-Change Boundary
**Trace**: R6
**Slice**: openspec-boundary
This work must remain a single aggregate OpenSpec change and must not expand into platform/core work.

#### Scenario: No extra change is created
- **Given** the WebView UI redraw is in progress
- **When** additional UI subareas are discovered
- **Then** they are handled inside `2026-07-01-webview-native-ui-parity-redraw`
- **And** no extra OpenSpec change is created for chrome, source list, detail, settings, or visual polish

#### Scenario: Out-of-scope platform/core need is discovered
- **Given** the redraw appears to require macOS runner, C++ Core, packaging, or broad HostBridge command expansion work
- **When** the need is confirmed
- **Then** implementation pauses and returns to planning
- **And** the out-of-scope work is not silently added to this change

## Modified Requirements

None.

## Removed Requirements

None.
