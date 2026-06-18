# Test Report: 2026-06-17-signal-source-expansion/01-signal-source-foundation

## Task 1: Introduce source contracts

### Red Phase
- **Timestamp:** 2026-06-17 20:02
- **Failing Tests:**
  - `SignalSourceSnapshotTests` - `CS0246`/`CS0103`: `SignalSourceSnapshot` and `SignalSourceKind` are missing product types.
- **False Red Fixes:** none

### Green Phase
- **Timestamp:** 2026-06-17 20:03
- **Passing Tests:**
  - `SignalSourceSnapshotTests` - pass
- **Change Summary:**
  - `src/DesktopLoudnessMeter.App/Audio/SignalSourceKind.cs` - added - Defines generic source kinds and compact labels.
  - `src/DesktopLoudnessMeter.App/Audio/SignalSourceSnapshot.cs` - added - Carries source identity plus the existing analyzer metric payload.
  - `src/DesktopLoudnessMeter.App/Audio/ISignalSourceMeterService.cs` - added - Defines source-oriented meter, reset, and settings interfaces.
  - `tests/DesktopLoudnessMeter.Tests/Audio/SignalSourceSnapshotTests.cs` - added - Covers source identity and label formatting.
- **Test Change Record:** No test modifications
- **Acceptance Criteria Coverage:**

| Acceptance Criterion | Test Case | Status |
|----------------------|-----------|--------|
| Source snapshots expose identity, process id, display name, state, and existing metric fields. | `SnapshotExposesSourceIdentityAndExistingAnalyzerMetrics` | pass |
| WASAPI/source kinds format as compact user-facing labels. | `SourceKindFormatsCompactLabels` | pass |
| No listener or plugin dependency is introduced. | Source contract files only; no runtime transport added | pass |

### Refactor Phase
- **Timestamp:** 2026-06-17 20:03
- **Passing Tests:**
  - `SignalSourceSnapshotTests` - pass
- **Change Summary:**
  - No refactor changes after green.
- **Coverage:** N/A

## Task 2: Adapt WASAPI provider to source contracts

### Red Phase
- **Timestamp:** 2026-06-17 20:04
- **Failing Tests:**
  - `WasapiSessionMeterServiceTests.ExposesWasapiRowsThroughSignalSourceService` - `CS0029`: `WasapiSessionMeterService` does not implement `ISignalSourceMeterService`.
  - `WasapiSessionMeterServiceTests.ExposesWasapiRowsThroughSignalSourceService` - `CS0030`: legacy `AudioSessionSnapshot` is not yet connected to `SignalSourceSnapshot`.
- **False Red Fixes:** none

### Green Phase
- **Timestamp:** 2026-06-17 20:07
- **Passing Tests:**
  - `WasapiSessionMeterServiceTests` - pass
  - `SignalSourceSnapshotTests` - pass
- **Change Summary:**
  - `src/DesktopLoudnessMeter.App/Audio/AudioSessionSnapshot.cs` - modified - Makes legacy session snapshots inherit the generic source snapshot contract while preserving the old constructor and `SessionKey` getter.
  - `src/DesktopLoudnessMeter.App/Audio/IAudioSessionMeterService.cs` - modified - Bridges old session service/reset/settings interfaces to source-oriented interfaces with default interface implementations.
  - `src/DesktopLoudnessMeter.App/Audio/WasapiSessionMeterService.cs` - modified - Updates duplicate-process collapse to write the generic source key.
  - `tests/DesktopLoudnessMeter.Tests/Audio/WasapiSessionMeterServiceTests.cs` - modified - Covers WASAPI source exposure and source-interface reset/settings delegation.
- **Test Change Record:** No red assertions were modified; added source reset/settings coverage after the compatibility bridge was in place.
- **Acceptance Criteria Coverage:**

| Acceptance Criterion | Test Case | Status |
|----------------------|-----------|--------|
| Existing duplicate collapse, self-filter, silent-filter, and metric merge tests still pass through source snapshots. | `WasapiSessionMeterServiceTests` existing cases plus `ExposesWasapiRowsThroughSignalSourceService` | pass |
| Reset and analysis-settings delegation use source keys while preserving process-loopback routing. | `ResetDelegatesThroughSignalSourceInterface`, `AnalysisSettingsDelegateThroughSignalSourceInterface` | pass |
| WASAPI snapshots are labeled as `WasapiSession`. | `ExposesWasapiRowsThroughSignalSourceService` | pass |

### Refactor Phase
- **Timestamp:** 2026-06-17 20:07
- **Passing Tests:**
  - `WasapiSessionMeterServiceTests` - pass
  - `SignalSourceSnapshotTests` - pass
- **Change Summary:**
  - No refactor changes after green.
- **Coverage:** N/A

## Task 3: Move main view model reconciliation to source identity

### Red Phase
- **Timestamp:** 2026-06-17 20:10
- **Failing Tests:**
  - `MainViewModelTests.RefreshNowAddsGenericSourcesBySourceKey` - `CS1503`: `MainViewModel` still requires `IAudioSessionMeterService` instead of `ISignalSourceMeterService`.
  - `MainViewModelTests.RefreshNowAddsGenericSourcesBySourceKey` - `CS1061`: row view model does not expose `SourceKey`, `SourceKind`, or `SourceKindLabel`.
  - `MainViewModelAdvancedControlsTests.PerRowAnalysisSettingsUseSourceIdentity` - `CS1503`: source-native settings service cannot be used by `MainViewModel`.
- **False Red Fixes:** none

### Green Phase
- **Timestamp:** 2026-06-17 20:14
- **Passing Tests:**
  - `MainViewModelTests` - pass
  - `MainViewModelAdvancedControlsTests` - pass
  - `AudioSessionMeterViewModelTests` - pass
  - `DetailWindowViewModelTests` - pass
- **Change Summary:**
  - `src/DesktopLoudnessMeter.App/ViewModels/AudioSessionMeterViewModel.cs` - modified - Adds source key/kind properties and accepts generic source snapshots while retaining `SessionKey` as a compatibility alias.
  - `src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs` - modified - Reconciles generic source snapshots and routes reset/settings through source-oriented interfaces.
  - `tests/DesktopLoudnessMeter.Tests/ViewModels/MainViewModelTests.cs` - modified - Covers generic source reconciliation by source key.
  - `tests/DesktopLoudnessMeter.Tests/ViewModels/MainViewModelAdvancedControlsTests.cs` - modified - Covers source identity for per-row reset and analysis settings.
- **Test Change Record:** No red assertions were modified; added reset routing coverage after the source service path was green.
- **Acceptance Criteria Coverage:**

| Acceptance Criterion | Test Case | Status |
|----------------------|-----------|--------|
| Rows update in place by `SourceKey`. | `RefreshNowAddsGenericSourcesBySourceKey` | pass |
| Detail tracking survives transient WASAPI source-key changes by display-name recovery. | `RefreshNowReattachesTransientAppRowsByDisplayName` | pass |
| Missing process-backed sources remain until process exit. | `RefreshNowRetainsMissingAudioHandleUntilProcessExits` | pass |
| Settings and reset requests carry source keys to the provider. | `PerRowAnalysisSettingsUseSourceIdentity`, `PerRowResetUsesSourceIdentity` | pass |

### Refactor Phase
- **Timestamp:** 2026-06-17 20:14
- **Passing Tests:**
  - `MainViewModelTests` - pass
  - `MainViewModelAdvancedControlsTests` - pass
  - `AudioSessionMeterViewModelTests` - pass
  - `DetailWindowViewModelTests` - pass
- **Change Summary:**
  - No refactor changes after green.
- **Coverage:** N/A

### Additional Red Phase
- **Timestamp:** 2026-06-17 20:21
- **Failing Tests:**
  - `MainViewModelTests.RefreshNowKeepsLoopAliveAfterServiceFailure` - `Assert.Contains` failure: status text still said `1 session tracked` instead of source-facing language.
- **False Red Fixes:** First run used `--no-build` and executed stale binaries; reran without `--no-build` to confirm the valid assertion failure.

### Additional Green Phase
- **Timestamp:** 2026-06-17 20:11
- **Passing Tests:**
  - `MainViewModelTests.RefreshNowKeepsLoopAliveAfterServiceFailure` - pass
- **Change Summary:**
  - `src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs` - modified - Changes status text from session terminology to source terminology.
  - `tests/DesktopLoudnessMeter.Tests/ViewModels/MainViewModelTests.cs` - modified - Expects source-facing status text.
- **Test Change Record:** Updated status-text assertion from `1 session` to `1 source` to reflect R4 source-facing identity language.
- **Acceptance Criteria Coverage:**

| Acceptance Criterion | Test Case | Status |
|----------------------|-----------|--------|
| Rows and status use source-facing identity language. | `RefreshNowKeepsLoopAliveAfterServiceFailure` | pass |

### Review Red Phase
- **Timestamp:** 2026-06-17 20:12
- **Failing Tests:**
  - `MainViewModelTests.RefreshNowDoesNotRecoverRowsAcrossSourceKinds` - `Assert.Equal` failure: display-name recovery allowed a plugin row to become a local-loopback row with the same name.
- **False Red Fixes:** none

### Review Green Phase
- **Timestamp:** 2026-06-17 20:13
- **Passing Tests:**
  - `MainViewModelTests.RefreshNowDoesNotRecoverRowsAcrossSourceKinds` - pass
- **Change Summary:**
  - `src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs` - modified - Restricts display-name recovery to rows with the same source kind.
  - `tests/DesktopLoudnessMeter.Tests/ViewModels/MainViewModelTests.cs` - modified - Covers same-name sources from different source kinds.
- **Test Change Record:** No test modifications
- **Acceptance Criteria Coverage:**

| Acceptance Criterion | Test Case | Status |
|----------------------|-----------|--------|
| Detail tracking recovery must preserve source identity boundaries. | `RefreshNowDoesNotRecoverRowsAcrossSourceKinds` | pass |

## Task 4: Add minimal source markers to UI

### Direct Implementation
- **Timestamp:** 2026-06-17 20:11
- **Validation:**
  - `dotnet build .\desktop-loudness-meter.sln` - pass
  - `AudioSessionMeterViewModelTests.SourceIdentityTextUsesCompactSourceKindLabel` - pass
- **Change Summary:**
  - `src/DesktopLoudnessMeter.App/MainWindow.xaml` - modified - Adds compact source-kind badges to global rows and source text to inline detail headers.
  - `src/DesktopLoudnessMeter.App/DetailWindow.xaml` - modified - Adds source text to pop-up detail headers.
  - `src/DesktopLoudnessMeter.App/App.xaml.cs` - modified - Includes source kind labels in tray pop-up menu items.
  - `tests/DesktopLoudnessMeter.Tests/ViewModels/AudioSessionMeterViewModelTests.cs` - modified - Covers source identity text used by the UI.

### Pending Manual Validation
- [ ] [manual] Visual check confirms source badges fit in the compact window without overlapping row meter text - validation method: launch app with at least one active WASAPI source and inspect the compact global row at normal and narrow widths.
