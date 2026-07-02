---
status: active
created_at: 2026-06-29
last_verified_at: 2026-06-30
last_verified_by: opsx-knowledge
verification_basis: archive-follow-up
source_refs:
  - change:2026-06-23-native-core-dsp
  - evidence:openspec/changes/archive/2026-06-23-native-core-dsp/evidence.md
  - change:2026-06-29-native-core-build-boundary
  - evidence:openspec/changes/archive/2026-06-29-native-core-build-boundary/evidence.md
  - change:2026-06-29-webview-packaging-startup-tray
  - evidence:openspec/changes/archive/2026-06-29-webview-packaging-startup-tray/evidence.md
applies_to:
  - src/DesktopLoudnessMeter.App/DesktopLoudnessMeter.App.csproj
  - tests/DesktopLoudnessMeter.Benchmarks/DesktopLoudnessMeter.Benchmarks.csproj
  - src/DesktopLoudnessMeter.Native/DesktopLoudnessMeter.Native.targets
---

# Native Consumers Must Import Copy Targets

## Problem

A managed executable can reference a project that itself imports a native DLL copy target, yet still fail at runtime with `DllNotFoundException` because the native binary was copied to the referenced project's output, not the consuming executable's output.

## Signal

- `dotnet run` or `dotnet test` builds successfully.
- The app project output contains the native DLL.
- The consuming benchmark/test output does not contain the native DLL.
- Runtime fails when a `DllImport("...Native...")` path is first executed.

## Prevention

Every managed executable, test project, benchmark, or publish/installer route that directly or indirectly exercises P/Invoke must import the native copy target or have an equivalent explicit deployment rule. Do not assume `ProjectReference` transitivity or build-output copy targets place native runtime assets into the final process or publish directory.

When adding a new native build entry such as CMake, either keep its Windows output compatible with the existing copy target (`x64/<Configuration>/DesktopLoudnessMeter.Native.dll`) or update the copy target and verify every final process output again.

## Verification

Run the consuming project or inspect the final payload, not only the app build. For uMeter, `tests/DesktopLoudnessMeter.Benchmarks/DesktopLoudnessMeter.Benchmarks.csproj` must import `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeter.Native.targets` so `dotnet run --project ... -c Release` can load `DesktopLoudnessMeter.Native.dll`; app publish and installer payload checks must also assert `DesktopLoudnessMeter.Native.dll` is present next to `uMeter.exe`.
