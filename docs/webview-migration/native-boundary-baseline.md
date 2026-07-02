# Native Boundary Baseline

## Purpose

This baseline records the current native DSP promise before the WebView migration changes packaging or host boundaries. Later slices may intentionally change the promise, but only after classifying the change as preserved, intentional, or fatal.

## Decision States

| State | Meaning |
|-------|---------|
| `preserved` | The WebView migration keeps the `main` behavior and verifies it. |
| `intentional` | The WebView migration deliberately changes the behavior and documents the new product promise. |
| `fatal` | The WebView migration accidentally breaks the current product promise. |

## Current `main` Promise

| Area | Baseline |
|------|----------|
| Native FFT | `NativeRfRapidSpectrumFft` attempts `DesktopLoudnessMeter.Native.dll`, then falls back to `ManagedSpectrumFft` when the DLL, exports, image format, or native execution fail. |
| Native true peak | `PcmMetricAnalyzer` attempts `NativeRfRapidTruePeakEstimator`, catches native load/execution failures, disables the native path, and continues with managed `TruePeakEstimator`. |
| Documentation | `README.md` describes optional native RfRapid C++ FFT/true-peak acceleration with managed fallback. |
| Build command | `src\DesktopLoudnessMeter.Native\build-native.ps1` enables native acceleration but is not required for managed analyzer behavior. |
| Product promise | Missing `DesktopLoudnessMeter.Native.dll` must not prevent the app from running metering with managed fallback. |

## Reference WebView Branch Drift

| Reference Area | Observed Behavior | Classification | Owner |
|----------------|-------------------|----------------|-------|
| `PcmMetricAnalyzer.cs` | Constructs `NativePcmAnalyzer` directly in the reference branch. | `mandatory-native-drift` | `05-packaging-native-boundary-and-retirement` |
| `NativePcmAnalyzer` | Wraps native analyzer state and exports as a required runtime dependency for that path. | `mandatory-native-drift` | `05-packaging-native-boundary-and-retirement` |
| Native host startup | Reference branch creates native host/controller objects earlier in app startup. | `mandatory-native-drift` | `05-packaging-native-boundary-and-retirement` |

The drift is `fatal` if it is accidental. It can become `intentional` only if the packaging slice changes docs, tests, installer payload, startup errors, and user-facing requirements together.

## Required Checks For Packaging Slice

| Check | Required Result |
|-------|-----------------|
| Managed fallback smoke | App analyzer works when `DesktopLoudnessMeter.Native.dll` is absent, unless the product promise is intentionally changed. |
| Native DLL present | Native acceleration can be loaded when the DLL and exports are present. |
| Missing native dependency | Failure message is explicit if native becomes mandatory. |
| Publish output | Required native binaries are included when native is mandatory or acceleration is packaged. |
| Installer output | Installer includes required native binaries and WebView assets. |
| Documentation | README/current-status/release notes state whether native is optional or mandatory. |

## Current Classification

| Behavior | State | Reason |
|----------|-------|--------|
| Optional native acceleration with managed fallback | `preserved` | This is the documented `main` product promise. |
| Mandatory native analyzer behavior from the reference branch | `fatal` unless changed intentionally | It contradicts the current README and analyzer fallback code. |
| WebView asset loading in development mode | `preserved` until shell slice changes it | Development asset requirements must be explicit before WebView shell work. |
| WebView asset loading in packaged mode | `preserved` until packaging slice changes it | Packaged assets must be present or fail with a clear message. |

## Commands To Record On This PC

| Command | Purpose | Latest Local Result |
|---------|---------|---------------------|
| `dotnet restore .\desktop-loudness-meter.sln` | Restore managed projects. | Passed on 2026-07-02; all projects were already current. |
| `dotnet build .\desktop-loudness-meter.sln --no-restore` | Build managed app/tests and project references. | Passed on 2026-07-02 with 0 warnings and 0 errors. |
| `dotnet test .\desktop-loudness-meter.sln --no-build` | Run managed test suite. | Passed on 2026-07-02 with 288 tests passing. |
| `.\src\DesktopLoudnessMeter.Native\build-native.ps1 -Configuration Release` | Build optional native acceleration. | Deferred to packaging/native slice unless native behavior is under test. |

## Follow-Up

`05-packaging-native-boundary-and-retirement` must decide whether the migration keeps the managed fallback or makes native mandatory. Until then, clean WebView slices should preserve the optional-native fallback contract.
