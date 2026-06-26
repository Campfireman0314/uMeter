# Design

## U1: Metadata Sweep

Update .NET project versions, setup constants, JUCE CMake project versions, and `build-installer.ps1` default version.

## U2: Release Notes And Status

Add `docs/v2.3.0-release.md`, prepend a changelog entry, and update README/current-status active-release references while leaving historical release notes unchanged.

## U3: Verification

Run:

- managed unit tests;
- solution build;
- JUCE VST3 Debug tests;
- installer packaging if toolchain inputs are present.

Record exact results in `test-report.md`.
