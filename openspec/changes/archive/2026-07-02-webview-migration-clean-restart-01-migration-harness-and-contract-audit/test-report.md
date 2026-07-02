# Test Report: 2026-07-02-webview-migration-clean-restart/01-migration-harness-and-contract-audit

## Task 2: Build the HostBridge contract audit

### Red Phase
- **Timestamp:** 2026-07-02 18:51
- **Failing Tests:**
  - `HostBridgeContractMatrixTests.ContractMatrixContainsRequiredAuditColumns` - `Xunit.Sdk.TrueException`: `hostbridge-contract-matrix.md` missing (product document missing)
  - `HostBridgeContractMatrixTests.ContractMatrixRecordsKnownAliasMismatchAsFailure` - `Xunit.Sdk.TrueException`: `hostbridge-contract-matrix.md` missing (product document missing)
  - `HostBridgeContractMatrixTests.StateOwnershipMapDefinesAllowedOwnershipKinds` - `Xunit.Sdk.TrueException`: `state-ownership-map.md` missing (product document missing)
- **False Red Fixes:** none

### Green Phase
- **Timestamp:** 2026-07-02 18:51
- **Passing Tests:**
  - `HostBridgeContractMatrixTests.ContractMatrixContainsRequiredAuditColumns` - pass
  - `HostBridgeContractMatrixTests.ContractMatrixRecordsKnownAliasMismatchAsFailure` - pass
  - `HostBridgeContractMatrixTests.StateOwnershipMapDefinesAllowedOwnershipKinds` - pass
- **Change Summary:**
  - `tests/DesktopLoudnessMeter.Tests/WebViewMigration/HostBridgeContractMatrixTests.cs` - added - Verifies required contract matrix shape, known alias mismatch recording, and allowed state ownership categories.
  - `docs/webview-migration/hostbridge-contract-matrix.md` - added - Records reference-branch WebView commands, real bridge status, and contract failures.
  - `docs/webview-migration/state-ownership-map.md` - added - Assigns ownership categories for host snapshot, persisted settings, derived fields, and transient UI state.
- **Test Change Record:** No test modifications
- **Acceptance Criteria Coverage:**

| Acceptance Criterion | Test Case | Status |
|----------------------|-----------|--------|
| The contract matrix includes command, payload fields, result shape, frontend caller, mock implementation, C# bridge handler, host execution path, and test coverage columns. | `ContractMatrixContainsRequiredAuditColumns` | pass |
| The matrix marks mock-only and frontend-only commands as contract failures. | `ContractMatrixRecordsKnownAliasMismatchAsFailure` | pass |
| The matrix explicitly records the known `setSourceAlias` mismatch from the reference branch. | `ContractMatrixRecordsKnownAliasMismatchAsFailure` | pass |
| The state ownership map marks each field as host-owned, WebView-derived, persisted-workspace-owned, or transient-view-owned. | `StateOwnershipMapDefinesAllowedOwnershipKinds` | pass |
| Tests fail if required contract matrix columns are missing. | `ContractMatrixContainsRequiredAuditColumns` | pass |
| Tests fail if the known alias mismatch is omitted before it is intentionally resolved by a later slice. | `ContractMatrixRecordsKnownAliasMismatchAsFailure` | pass |

### Refactor Phase
- **Timestamp:** 2026-07-02 18:51
- **Passing Tests:**
  - `HostBridgeContractMatrixTests.ContractMatrixContainsRequiredAuditColumns` - pass
  - `HostBridgeContractMatrixTests.ContractMatrixRecordsKnownAliasMismatchAsFailure` - pass
  - `HostBridgeContractMatrixTests.StateOwnershipMapDefinesAllowedOwnershipKinds` - pass
- **Change Summary:**
  - `tests/DesktopLoudnessMeter.Tests/WebViewMigration/HostBridgeContractMatrixTests.cs` - added - No refactor needed beyond the initial focused helper and assertions.
  - `docs/webview-migration/hostbridge-contract-matrix.md` - added - No refactor needed.
  - `docs/webview-migration/state-ownership-map.md` - added - No refactor needed.
- **Coverage:** N/A

## Task 3: Create deterministic scenario traces

### Red Phase
- **Timestamp:** 2026-07-02 18:54
- **Failing Tests:**
  - `SourceWorkflowScenarioTraceTests.FakeSourceIdentitySurvivesDisappearReconnectAndRename` - `CS0103`: `FakeSourceScenarioHarness` missing (product test harness missing)
  - `SourceWorkflowScenarioTraceTests.FakeSourceHarnessModelsExpectedSourceKinds` - `CS0103`: `FakeSourceScenarioHarness` missing (product test harness missing)
- **False Red Fixes:** Fixed constructor argument casing in `FakeSourceScenarioHarness.cs` after the first green attempt produced `CS1739`; the original missing-harness red remained valid.

### Green Phase
- **Timestamp:** 2026-07-02 18:54
- **Passing Tests:**
  - `SourceWorkflowScenarioTraceTests.FakeSourceIdentitySurvivesDisappearReconnectAndRename` - pass
  - `SourceWorkflowScenarioTraceTests.FakeSourceHarnessModelsExpectedSourceKinds` - pass
  - `SourceWorkflowScenarioTraceTests.ScenarioTraceDocumentDefinesSourceWorkflowCases` - pass
  - `DetailSettingsScenarioTraceTests.ScenarioTraceDocumentDefinesDetailSettingsCases` - pass
  - `DetailSettingsScenarioTraceTests.ScenarioTraceDocumentDefinesRenderingGateCases` - pass
- **Change Summary:**
  - `tests/DesktopLoudnessMeter.Tests/WebViewMigration/FakeSourceScenarioHarness.cs` - added - Provides deterministic source fixtures for WASAPI, local-loopback, and plugin-style sources.
  - `tests/DesktopLoudnessMeter.Tests/WebViewMigration/SourceWorkflowScenarioTraceTests.cs` - added - Verifies source identity fixtures and required source workflow trace cases.
  - `tests/DesktopLoudnessMeter.Tests/WebViewMigration/DetailSettingsScenarioTraceTests.cs` - added - Verifies detail settings and rendering work gate trace coverage.
  - `docs/webview-migration/scenario-traces.md` - added - Records deterministic source workflow, detail settings, and render gate traces.
- **Test Change Record:** No test modifications; helper constructor argument casing was corrected before green.
- **Acceptance Criteria Coverage:**

| Acceptance Criterion | Test Case | Status |
|----------------------|-----------|--------|
| Fake source fixtures can model WASAPI, local-loopback, and plugin-style source identities. | `FakeSourceHarnessModelsExpectedSourceKinds` | pass |
| Fake source fixtures can disappear and reconnect with the same stable source key. | `FakeSourceIdentitySurvivesDisappearReconnectAndRename` | pass |
| Source traces cover refresh, selection, alias edit, hide, restore, reorder, reset, forget, show hidden, and reconnect. | `ScenarioTraceDocumentDefinesSourceWorkflowCases` | pass |
| Detail traces cover true peak, waveform mode, spectrum mode, collision visibility, detail module order, and loudness-clock settings. | `ScenarioTraceDocumentDefinesDetailSettingsCases` | pass |
| Rendering gate traces cover hidden waveform, spectrum, and loudness-clock work. | `ScenarioTraceDocumentDefinesRenderingGateCases` | pass |
| Tests assert that source-key identity remains separate from display title. | `FakeSourceIdentitySurvivesDisappearReconnectAndRename` | pass |
| Tests assert that every documented trace names initial state, action, expected host state, expected persisted state, and expected WebView projection. | `ScenarioTraceDocumentDefinesSourceWorkflowCases` | pass |

### Refactor Phase
- **Timestamp:** 2026-07-02 18:54
- **Passing Tests:**
  - `HostBridgeContractMatrixTests.ContractMatrixContainsRequiredAuditColumns` - pass
  - `HostBridgeContractMatrixTests.ContractMatrixRecordsKnownAliasMismatchAsFailure` - pass
  - `HostBridgeContractMatrixTests.StateOwnershipMapDefinesAllowedOwnershipKinds` - pass
  - `SourceWorkflowScenarioTraceTests.FakeSourceIdentitySurvivesDisappearReconnectAndRename` - pass
  - `SourceWorkflowScenarioTraceTests.FakeSourceHarnessModelsExpectedSourceKinds` - pass
  - `SourceWorkflowScenarioTraceTests.ScenarioTraceDocumentDefinesSourceWorkflowCases` - pass
  - `DetailSettingsScenarioTraceTests.ScenarioTraceDocumentDefinesDetailSettingsCases` - pass
  - `DetailSettingsScenarioTraceTests.ScenarioTraceDocumentDefinesRenderingGateCases` - pass
- **Change Summary:**
  - `tests/DesktopLoudnessMeter.Tests/WebViewMigration/MigrationDocumentReader.cs` - added - Centralizes migration document lookup for WebView migration tests.
  - `tests/DesktopLoudnessMeter.Tests/WebViewMigration/HostBridgeContractMatrixTests.cs` - modified - Reuses the shared migration document reader.
  - `tests/DesktopLoudnessMeter.Tests/WebViewMigration/SourceWorkflowScenarioTraceTests.cs` - modified - Reuses the shared migration document reader.
  - `tests/DesktopLoudnessMeter.Tests/WebViewMigration/DetailSettingsScenarioTraceTests.cs` - modified - Reuses the shared migration document reader.
- **Coverage:** N/A

## Task 4: Characterize native and build packaging baselines

### Red Phase
- **Timestamp:** 2026-07-02 18:59
- **Failing Tests:**
  - `BuildPackagingBaselineTests.ScenarioTracesRecordBuildAndAssetBaselines` - `Xunit.Sdk.ContainsException`: build/asset baseline terms missing from `scenario-traces.md` (product document incomplete)
  - `NativeBoundaryBaselineTests.NativeBoundaryBaselineRecordsMainAndReferenceBehavior` - `Xunit.Sdk.TrueException`: `native-boundary-baseline.md` missing (product document missing)
  - `BuildPackagingBaselineTests.NativeBoundaryBaselineRecordsPackagingDecisionStates` - `Xunit.Sdk.TrueException`: `native-boundary-baseline.md` missing (product document missing)
- **False Red Fixes:** none. `CurrentReadmeDocumentsOptionalNativeManagedFallback` and `CurrentAnalyzerFallsBackWhenNativeTruePeakCannotLoad` passed during the characterization run, confirming the current `main` fallback baseline.

### Green Phase
- **Timestamp:** 2026-07-02 18:59
- **Passing Tests:**
  - `NativeBoundaryBaselineTests.CurrentReadmeDocumentsOptionalNativeManagedFallback` - pass
  - `NativeBoundaryBaselineTests.CurrentAnalyzerFallsBackWhenNativeTruePeakCannotLoad` - pass
  - `NativeBoundaryBaselineTests.NativeBoundaryBaselineRecordsMainAndReferenceBehavior` - pass
  - `BuildPackagingBaselineTests.ScenarioTracesRecordBuildAndAssetBaselines` - pass
  - `BuildPackagingBaselineTests.NativeBoundaryBaselineRecordsPackagingDecisionStates` - pass
- **Change Summary:**
  - `tests/DesktopLoudnessMeter.Tests/WebViewMigration/NativeBoundaryBaselineTests.cs` - added - Characterizes current optional-native fallback and required native baseline documentation.
  - `tests/DesktopLoudnessMeter.Tests/WebViewMigration/BuildPackagingBaselineTests.cs` - added - Verifies build, publish, and WebView asset baseline trace terms.
  - `docs/webview-migration/native-boundary-baseline.md` - added - Records current managed fallback promise and reference branch mandatory-native drift.
  - `docs/webview-migration/scenario-traces.md` - modified - Adds restore/build/test/publish and development/packaged asset baseline traces.
- **Test Change Record:** No test modifications
- **Acceptance Criteria Coverage:**

| Acceptance Criterion | Test Case | Status |
|----------------------|-----------|--------|
| The native baseline records the current `main` promise for native DSP availability. | `CurrentReadmeDocumentsOptionalNativeManagedFallback`, `CurrentAnalyzerFallsBackWhenNativeTruePeakCannotLoad`, `NativeBoundaryBaselineRecordsMainAndReferenceBehavior` | pass |
| The native baseline records the reference branch native-mandatory behavior. | `NativeBoundaryBaselineRecordsMainAndReferenceBehavior` | pass |
| Native boundary tests distinguish preserved behavior, intentional change, and fatal regression classifications. | `NativeBoundaryBaselineRecordsPackagingDecisionStates` | pass |
| Build baseline traces record restore, build, test, publish, development asset, and packaged asset expectations. | `ScenarioTracesRecordBuildAndAssetBaselines` | pass |
| Packaging baseline tests cover the expected shape of WebView development and packaged asset paths without requiring the WebView project to exist yet. | `ScenarioTracesRecordBuildAndAssetBaselines` | pass |
| [manual] Run the documented restore/build/test commands on this PC and record any environmental blockers in the baseline document. | Manual validation completed: restore passed, build passed with 0 warnings/errors, test passed with 288 tests. | pass |

### Refactor Phase
- **Timestamp:** 2026-07-02 18:59
- **Passing Tests:**
  - `WebViewMigration` test slice - 13 tests passed
  - `dotnet restore .\desktop-loudness-meter.sln` - pass
  - `dotnet build .\desktop-loudness-meter.sln --no-restore` - pass
  - `dotnet test .\desktop-loudness-meter.sln --no-build` - pass
- **Change Summary:**
  - `docs/webview-migration/native-boundary-baseline.md` - modified - Records local restore/build/test results from this PC.
- **Coverage:** N/A

## Pending Manual Validation

- [x] [manual] Review the ledger against `D:\GitRepo\uMeter-webview-reference` and confirm no copied code is marked reusable without a stated reason. - validation method: inspected the reference worktree path/commit and seeded only `copy`/`adapt` decisions with reasons; no code was copied in this slice.
- [x] [manual] Run the documented restore/build/test commands on this PC and record any environmental blockers in the baseline document. - validation method: `dotnet restore .\desktop-loudness-meter.sln`, `dotnet build .\desktop-loudness-meter.sln --no-restore`, and `dotnet test .\desktop-loudness-meter.sln --no-build` all passed on 2026-07-02.
