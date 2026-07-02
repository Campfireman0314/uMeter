# Tasks

## Task 1: Define parity rules and seed the salvage ledger

**Requirement Trace:** [R1, R2, R3, R4] -> [U1]
**Execution Mode:** [direct]
**Files:**
- `docs/webview-migration/parity-decision-rules.md` - decision taxonomy and compatibility bridge policy
- `docs/webview-migration/salvage-ledger.md` - reference-branch salvage decisions

**Acceptance Criteria:**
- [x] The decision rules define `fatal`, `blocking-fixable`, `acceptable-intentional`, and `discardable-legacy`.
- [x] The rules name product-promise categories for startup, build, publish, source identity, persistence, metering, command/UI alignment, detail settings, and performance gates.
- [x] The compatibility policy requires an owner, retirement condition, and later slice for any staged bridge.
- [x] The salvage ledger records the reference worktree path and commit.
- [x] The salvage ledger seeds known findings for React/Vite setup, WebView hosting, HostBridge DTOs, mock-only bridge behavior, native-host work, tests, and packaging changes.
- [x] [manual] Review the ledger against `D:\GitRepo\uMeter-webview-reference` and confirm no copied code is marked reusable without a stated reason.

**Dependencies:** None

## Task 2: Build the HostBridge contract audit

**Requirement Trace:** [R4, R5, R6, R7, R8] -> [U2]
**Execution Mode:** [test-first]
**Files:**
- `docs/webview-migration/hostbridge-contract-matrix.md` - command inventory and parity status
- `docs/webview-migration/state-ownership-map.md` - snapshot and state ownership table
- `tests/DesktopLoudnessMeter.Tests/WebViewMigration/HostBridgeContractMatrixTests.cs` - matrix shape and known-mismatch checks

**Acceptance Criteria:**
- [x] The contract matrix includes command, payload fields, result shape, frontend caller, mock implementation, C# bridge handler, host execution path, and test coverage columns.
- [x] The matrix marks mock-only and frontend-only commands as contract failures.
- [x] The matrix explicitly records the known `setSourceAlias` mismatch from the reference branch.
- [x] The state ownership map marks each field as host-owned, WebView-derived, persisted-workspace-owned, or transient-view-owned.
- [x] Tests fail if required contract matrix columns are missing.
- [x] Tests fail if the known alias mismatch is omitted before it is intentionally resolved by a later slice.

**Dependencies:** Task 1

## Task 3: Create deterministic scenario traces

**Requirement Trace:** [R2, R9, R10, R11, R12] -> [U3]
**Execution Mode:** [test-first]
**Files:**
- `docs/webview-migration/scenario-traces.md` - source workflow, detail settings, and rendering work gate traces
- `tests/DesktopLoudnessMeter.Tests/WebViewMigration/FakeSourceScenarioHarness.cs` - deterministic fake source fixtures
- `tests/DesktopLoudnessMeter.Tests/WebViewMigration/SourceWorkflowScenarioTraceTests.cs` - source workflow trace coverage
- `tests/DesktopLoudnessMeter.Tests/WebViewMigration/DetailSettingsScenarioTraceTests.cs` - detail settings and render gate trace coverage

**Acceptance Criteria:**
- [x] Fake source fixtures can model WASAPI, local-loopback, and plugin-style source identities.
- [x] Fake source fixtures can disappear and reconnect with the same stable source key.
- [x] Source traces cover refresh, selection, alias edit, hide, restore, reorder, reset, forget, show hidden, and reconnect.
- [x] Detail traces cover true peak, waveform mode, spectrum mode, collision visibility, detail module order, and loudness-clock settings.
- [x] Rendering gate traces cover hidden waveform, spectrum, and loudness-clock work.
- [x] Tests assert that source-key identity remains separate from display title.
- [x] Tests assert that every documented trace names initial state, action, expected host state, expected persisted state, and expected WebView projection.

**Dependencies:** Task 1

## Task 4: Characterize native and build packaging baselines

**Requirement Trace:** [R2, R4, R13, R14] -> [U4]
**Execution Mode:** [characterization-first]
**Files:**
- `docs/webview-migration/native-boundary-baseline.md` - native analyzer and fallback baseline
- `docs/webview-migration/scenario-traces.md` - build and packaging baseline traces
- `tests/DesktopLoudnessMeter.Tests/WebViewMigration/NativeBoundaryBaselineTests.cs` - native boundary characterization
- `tests/DesktopLoudnessMeter.Tests/WebViewMigration/BuildPackagingBaselineTests.cs` - restore/build/publish expectation characterization

**Acceptance Criteria:**
- [x] The native baseline records the current `main` promise for native DSP availability.
- [x] The native baseline records the reference branch native-mandatory behavior.
- [x] Native boundary tests distinguish preserved behavior, intentional change, and fatal regression classifications.
- [x] Build baseline traces record restore, build, test, publish, development asset, and packaged asset expectations.
- [x] Packaging baseline tests cover the expected shape of WebView development and packaged asset paths without requiring the WebView project to exist yet.
- [x] [manual] Run the documented restore/build/test commands on this PC and record any environmental blockers in the baseline document.

**Dependencies:** Task 1, Task 3
