# HostBridge Contract Matrix

## Purpose

This matrix audits the reference WebView branch at `D:\GitRepo\uMeter-webview-reference` commit `cb7065d`. It is not the clean migration contract yet. Rows marked `contract-failure` must be fixed or intentionally redesigned before the corresponding WebView behavior ships.

## Status Key

| Status | Meaning |
|--------|---------|
| `covered` | Frontend, mock, real bridge, host execution, and tests are aligned or have a clear owner. |
| `shell-owned` | Real bridge routes to shell callbacks rather than host controller state. |
| `contract-failure` | Frontend or mock behavior exists without an equivalent real bridge or host behavior. |
| `review-item` | Behavior exists but needs state ownership, tests, or retirement decision. |

## Commands

| Command | Payload Fields | Result Shape | Frontend Caller | Mock Implementation | C# Bridge Handler | Host Execution Path | Test Coverage | Status |
|---------|----------------|--------------|-----------------|---------------------|--------------------|---------------------|---------------|--------|
| `refresh` | none | `HostCommandResultDto` with snapshot | `AppShell.tsx` startup refresh | generic snapshot return | `HostBridgeController.cs` -> `RefreshHostCommand` | `DesktopMeterHostController.Execute` | planned contract tests | `review-item` |
| `openNativeUi` | none | command result | not primary WebView flow | none | `HostBridgeController.cs` -> `OpenNativeUiHostCommand` | `_openNativeUi` callback | planned shell tests | `review-item` |
| `openGlobalSettings` | none | command result | settings button | shallow snapshot clone | `HostBridgeController.cs` -> `OpenGlobalSettingsHostCommand` | shell callback | planned shell tests | `shell-owned` |
| `beginWindowDrag` | none | command result | window chrome | shallow snapshot clone | `HostBridgeController.cs` -> `BeginWindowDragHostCommand` | shell callback | planned shell tests | `shell-owned` |
| `toggleWindowState` | none | command result | window chrome | shallow snapshot clone | `HostBridgeController.cs` -> `ToggleWindowStateHostCommand` | shell callback | planned shell tests | `shell-owned` |
| `hideToTray` | none | command result | window chrome | shallow snapshot clone | `HostBridgeController.cs` -> `HideToTrayHostCommand` | shell callback | planned lifecycle tests | `shell-owned` |
| `closeWindow` | none | command result | window chrome | shallow snapshot clone | `HostBridgeController.cs` -> `CloseWindowHostCommand` | shell callback | planned lifecycle tests | `shell-owned` |
| `setAlwaysOnTop` | `isAlwaysOnTop` | command result | always-on-top toggle | shallow snapshot clone | `HostBridgeController.cs` -> `SetAlwaysOnTopHostCommand` | shell callback | planned settings tests | `review-item` |
| `setDisplayMode` | `displayMode` | command result | meter display segmented control | shallow snapshot clone | `HostBridgeController.cs` -> `SetDisplayModeHostCommand` | shell callback | planned settings tests | `review-item` |
| `setTruePeakEnabled` | `truePeakEnabled` | command result | true-peak toggle | shallow snapshot clone | `HostBridgeController.cs` -> `SetTruePeakEnabledHostCommand` | shell callback | planned settings tests | `review-item` |
| `setPerformanceMode` | `performanceMode` | command result | performance control | none | `HostBridgeController.cs` -> `SetPerformanceModeHostCommand` | `DesktopMeterHostController.Execute` | planned settings tests | `review-item` |
| `setTheme` | `themeId` | command result | theme picker | updates mock settings | `HostBridgeController.cs` -> `SetThemeHostCommand` | `DesktopMeterHostController.Execute` | planned shell/theme tests | `review-item` |
| `setShowHiddenSources` | `showHiddenSources` | command result | show-hidden toggle | updates mock settings | `HostBridgeController.cs` -> `SetShowHiddenSourcesHostCommand` | `DesktopMeterHostController.Execute` | planned source workflow tests | `review-item` |
| `resetAll` | none | command result | global reset | none | `HostBridgeController.cs` -> `ResetAllHostCommand` | `DesktopMeterHostController.Execute` | planned source workflow tests | `review-item` |
| `selectDetail` | `sourceKey` | command result | source row/detail navigation | updates selected detail | `HostBridgeController.cs` -> `SelectDetailHostCommand` | `DesktopMeterHostController.Execute` | planned source workflow tests | `review-item` |
| `openPopup` | `sourceKey` | command result | source row/detail actions | shallow snapshot clone | `HostBridgeController.cs` -> `OpenPopupHostCommand` | shell callback | planned shell tests | `shell-owned` |
| `resetSource` | `sourceKey`, `sourceKind`, `processId` | command result | source row/detail actions | none | `HostBridgeController.cs` -> `ResetSourceHostCommand` | `DesktopMeterHostController.Execute` | planned source workflow tests | `review-item` |
| `hideSource` | `sourceKey` | command result | source row action | updates mock source hidden state | `HostBridgeController.cs` -> `HideSourceHostCommand` | `DesktopMeterHostController.Execute` | planned source workflow tests | `review-item` |
| `restoreSource` | `sourceKey` | command result | source row action | updates mock source hidden state | `HostBridgeController.cs` -> `RestoreSourceHostCommand` | `DesktopMeterHostController.Execute` | planned source workflow tests | `review-item` |
| `moveSource` | `sourceKey`, `moveDirection` | command result | source row action | reorders mock sources | `HostBridgeController.cs` -> `MoveSourceHostCommand` | `DesktopMeterHostController.Execute` | planned source workflow tests | `review-item` |
| `forgetSource` | `sourceKey` | command result | detail/source action | removes mock settings entry | `HostBridgeController.cs` -> `ForgetSourceHostCommand` | `DesktopMeterHostController.Execute` | planned source workflow tests | `review-item` |
| `setSourceAlias` | `sourceKey`, `alias` | command result | `AppShell.tsx` alias editor | `mockHostBridge.ts` updates alias/title | missing in `HostBridgeController.cs` | none | planned failure test | `contract-failure` |
| `backToGlobal` | none | command result | detail back button | clears selected detail | `HostBridgeController.cs` -> `BackToGlobalHostCommand` | `DesktopMeterHostController.Execute` | planned source workflow tests | `review-item` |
| `saveDetailLayout` | none | command result | detail toolbar | shallow snapshot clone | `HostBridgeController.cs` -> `SaveDetailLayoutHostCommand` | shell callback | planned detail tests | `shell-owned` |
| `loadDetailLayout` | none | command result | detail toolbar | shallow snapshot clone | `HostBridgeController.cs` -> `LoadDetailLayoutHostCommand` | shell callback | planned detail tests | `shell-owned` |
| `openStereoSettings` | none | command result | stereo module action | shallow snapshot clone | `HostBridgeController.cs` -> `OpenStereoSettingsHostCommand` | shell callback | planned detail tests | `shell-owned` |
| `setTruePeakOversampling` | `sourceKey`, `sourceKind`, `processId`, `truePeakOversampling` | command result | detail/source controls | none | `HostBridgeController.cs` -> `SetTruePeakOversamplingHostCommand` | `DesktopMeterHostController.Execute` | planned detail tests | `review-item` |
| `setWaveformDisplayMode` | `sourceKey`, `sourceKind`, `processId`, `waveformDisplayMode` | command result | detail controls | updates mock source mode | `HostBridgeController.cs` -> `SetWaveformDisplayModeHostCommand` | `DesktopMeterHostController.Execute` | planned detail tests | `review-item` |
| `setSpectrumDisplayMode` | `sourceKey`, `sourceKind`, `processId`, `spectrumDisplayMode` | command result | detail controls | updates mock source mode | `HostBridgeController.cs` -> `SetSpectrumDisplayModeHostCommand` | `DesktopMeterHostController.Execute` | planned detail tests | `review-item` |
| `setDetailModuleVisibility` | `sourceKey`, `moduleKind`, `isVisible` | command result | module visibility toggle | updates selected mock module | `HostBridgeController.cs` -> `SetDetailModuleVisibilityHostCommand` | `DesktopMeterHostController.Execute` | planned detail tests | `review-item` |
| `setDetailModuleHeight` | `sourceKey`, `moduleKind`, `moduleHeight` | command result | module resize | updates selected mock module | `HostBridgeController.cs` -> `SetDetailModuleHeightHostCommand` | `DesktopMeterHostController.Execute` | planned detail tests | `review-item` |
| `moveDetailModule` | `sourceKey`, `moduleKind`, `targetModuleKind`, `insertAfter` | command result | module reorder | reorders selected mock modules | `HostBridgeController.cs` -> `MoveDetailModuleHostCommand` | `DesktopMeterHostController.Execute` | planned detail tests | `review-item` |
| `setSpectrumDisplayRange` | `sourceKey`, `spectrumDisplayFloorDb`, `spectrumDisplayCeilingDb` | command result | spectrum controls | updates mock settings source | `HostBridgeController.cs` -> `SetSpectrumDisplayRangeHostCommand` | `DesktopMeterHostController.Execute` | planned detail tests | `review-item` |
| `setLoudnessClockSettings` | `sourceKey`, `loudnessClockMinimumDb`, `loudnessClockMaximumDb`, `loudnessClockWarningDb`, `loudnessClockCycleSeconds` | command result | loudness-clock controls | updates mock settings source | `HostBridgeController.cs` -> `SetLoudnessClockSettingsHostCommand` | `DesktopMeterHostController.Execute` | planned detail tests | `review-item` |
| `setCollisionSettings` | `sourceKey`, `collisionLevelFloorDb`, `collisionProminenceThresholdDb`, `collisionRelativeThreshold`, `collisionFrequencyScale`, `inlineCollisionSourceKey` | command result | collision controls | updates mock settings/detail collision | `HostBridgeController.cs` -> `SetCollisionSettingsHostCommand` | `DesktopMeterHostController.Execute` | planned detail tests | `review-item` |
| `setRecentEventLimit` | `maxRecentEventEntries` | command result | global settings surface | updates mock settings | `HostBridgeController.cs` -> `SetRecentEventLimitHostCommand` | `DesktopMeterHostController.Execute` | planned settings tests | `review-item` |
| `setEventOutputFolder` | `eventOutputFolder` | command result | global settings surface | updates mock settings | `HostBridgeController.cs` -> `SetEventOutputFolderHostCommand` | `DesktopMeterHostController.Execute` | planned settings tests | `review-item` |

## Required Follow-Up

- `03-hostbridge-source-workflows` must turn source workflow `review-item` rows into `covered` rows.
- `04-detail-charts-and-settings` must turn detail and rendering rows into `covered` rows.
- `02-webview-shell-foundation` must decide shell-owned commands without creating a permanent WPF fallback surface.
- `setSourceAlias` remains a `contract-failure` until the real bridge and host execution path are added or the alias UX is intentionally redesigned.
