# Test Report: 2026-06-17-signal-source-expansion/04-source-management-polish

## Task 1: Polish row identity and duplicate-name presentation

### Red Phase
- **Timestamp:** 2026-06-18 15:10
- **Failing Tests:**
  - `AudioSessionMeterViewModelTests.DuplicateDisplayNamesUseQualifiedRowTitleWithoutChangingProviderName` - `CS1061`: `DisplayTitle` and `SetDisplayNameCollision` are missing.
  - `AudioSessionMeterViewModelTests.MissingAudioHandleUsesSourceFacingStatusText` - `CS1061`: `SourceStatusText` is missing.
  - `MainViewModelTests.RefreshNowMarksOnlyCurrentDuplicateDisplayNames` - `CS1061`: `DisplayTitle` is missing.
- **False Red Fixes:** none

### Green Phase
- **Timestamp:** 2026-06-18 15:10
- **Passing Tests:**
  - `AudioSessionMeterViewModelTests` - pass
  - `MainViewModelTests` - pass
- **Change Summary:**
  - `src/DesktopLoudnessMeter.App/ViewModels/AudioSessionMeterViewModel.cs` - modified - Adds display-only titles, source identity text, source status text, and duplicate-name qualification.
  - `src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs` - modified - Marks current display-name collisions after reconciliation.
  - `src/DesktopLoudnessMeter.App/MainWindow.xaml` - modified - Uses disambiguated row titles and shows source status in inline detail.
  - `src/DesktopLoudnessMeter.App/DetailWindow.xaml` - modified - Uses disambiguated titles and source status in pop-up detail.
  - `src/DesktopLoudnessMeter.App/App.xaml.cs` - modified - Uses disambiguated title and source identity in tray pop-up labels.
  - `tests/DesktopLoudnessMeter.Tests/ViewModels/AudioSessionMeterViewModelTests.cs` - modified - Covers identity text, duplicate display titles, and source-facing missing status.
  - `tests/DesktopLoudnessMeter.Tests/ViewModels/MainViewModelTests.cs` - modified - Covers current duplicate-name collision marking.
- **Test Change Record:** Updated older source identity and missing-handle assertions from generic/plugin-only and audio-session language to the new source-facing wording.
- **Acceptance Criteria Coverage:**

| Acceptance Criterion | Test Case | Status |
|----------------------|-----------|--------|
| `DisplayName` remains the provider-owned display name. | `DuplicateDisplayNamesUseQualifiedRowTitleWithoutChangingProviderName` | pass |
| Duplicate display-name rows expose a row title with compact source qualifier. | `DuplicateDisplayNamesUseQualifiedRowTitleWithoutChangingProviderName`, `RefreshNowMarksOnlyCurrentDuplicateDisplayNames` | pass |
| Unique display-name rows expose the clean display name. | `DuplicateDisplayNamesUseQualifiedRowTitleWithoutChangingProviderName`, `RefreshNowMarksOnlyCurrentDuplicateDisplayNames` | pass |
| Source identity text identifies WASAPI process ids and plugin/loopback handles. | `SourceIdentityTextIncludesProcessOrStableHandle` | pass |
| Missing/reconnecting rows use source-facing status text. | `MissingAudioHandleUsesSourceFacingStatusText`, `RefreshNowRetainsMissingAudioHandleUntilProcessExits` | pass |

### Refactor Phase
- **Timestamp:** 2026-06-18 15:10
- **Passing Tests:**
  - `AudioSessionMeterViewModelTests`, `MainViewModelTests` - pass, 24 tests
- **Change Summary:**
  - `src/DesktopLoudnessMeter.App/ViewModels/AudioSessionMeterViewModel.cs` - modified - Simplifies duplicate-name collision notification to raise `DisplayTitle` once.
- **Coverage:** N/A

## Task 2: Add deterministic source ordering and reconnect preference memory

### Red Phase
- **Timestamp:** 2026-06-18 15:11
- **Failing Tests:**
  - `MainViewModelTests.RefreshNowSortsMixedSourcesByKindDisplayNameAndKey` - assertion failure: rows remained in provider order.
  - `MainViewModelAdvancedControlsTests.PerSourceAnalysisSettingsSurviveReconnectBySourceKey` - assertion failure: reconnected source reverted to default 4x true-peak oversampling.
- **False Red Fixes:** none

### Green Phase
- **Timestamp:** 2026-06-18 15:11
- **Passing Tests:**
  - `MainViewModelTests.RefreshNowSortsMixedSourcesByKindDisplayNameAndKey` - pass
  - `MainViewModelAdvancedControlsTests.PerSourceAnalysisSettingsSurviveReconnectBySourceKey` - pass
  - `MainViewModelAdvancedControlsTests.GlobalTruePeakOversamplingRemainsDefaultForNewSourcesWithoutPreferences` - pass
- **Change Summary:**
  - `src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs` - modified - Sorts rows by source kind/display/source key and caches analysis preferences by source key.
  - `tests/DesktopLoudnessMeter.Tests/ViewModels/MainViewModelTests.cs` - modified - Covers mixed source sorting.
  - `tests/DesktopLoudnessMeter.Tests/ViewModels/MainViewModelAdvancedControlsTests.cs` - modified - Covers source-key preference memory and global defaults.
- **Test Change Record:** No red assertions were modified.
- **Acceptance Criteria Coverage:**

| Acceptance Criterion | Test Case | Status |
|----------------------|-----------|--------|
| Plugin sources sort before local-loopback sources. | `RefreshNowSortsMixedSourcesByKindDisplayNameAndKey` | pass |
| Local-loopback sources sort before WASAPI sources. | `RefreshNowSortsMixedSourcesByKindDisplayNameAndKey` | pass |
| Same-kind sources sort by display name, then source key. | `RefreshNowSortsMixedSourcesByKindDisplayNameAndKey` | pass |
| Per-source true-peak oversampling survives disappearance and reconnect. | `PerSourceAnalysisSettingsSurviveReconnectBySourceKey` | pass |
| Per-source waveform and spectrum channel modes survive disappearance and reconnect. | `PerSourceAnalysisSettingsSurviveReconnectBySourceKey` | pass |
| Global true-peak oversampling remains the default for newly seen sources. | `GlobalTruePeakOversamplingRemainsDefaultForNewSourcesWithoutPreferences` | pass |

### Refactor Phase
- **Timestamp:** 2026-06-18 15:11
- **Passing Tests:**
  - `MainViewModelTests`, `MainViewModelAdvancedControlsTests` - pass, 27 tests
- **Change Summary:**
  - No refactor changes after green.
- **Coverage:** N/A

## Task 3: Document multi-source source management behavior

### Direct Implementation
- **Timestamp:** 2026-06-18 15:12
- **Validation:**
  - Documentation reviewed against implemented source ordering, duplicate-name, and reconnect preference behavior.
- **Change Summary:**
  - `docs/source-management.md` - added - Explains source types, ordering, duplicate names, stable handles, reconnect preference memory, and deferred alias/visibility UI.
  - `README.md` - modified - Mentions mixed source rows and links source-management docs.
  - `docs/current-status.md` - modified - Updates current status and known limits for mixed sources and in-memory source preferences.
