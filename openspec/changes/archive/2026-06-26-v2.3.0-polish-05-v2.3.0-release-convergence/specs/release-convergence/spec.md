# Release Convergence Spec

## Requirements

### R1: Version Metadata Must Report 2.3.0
All product, project, installer, and JUCE sender version metadata SHALL report `2.3.0`.

### R2: Release Documentation Must Describe The Completed Slices
README, changelog, current status, and release notes SHALL describe source continuity, spectrograph fidelity, single-instance launch, source alias editing, hide/show hidden, and source ordering changes.

### R3: Verification Must Cover Managed And JUCE Paths
The release verification SHALL include the managed test suite and the JUCE sender tests.

### R4: Packaging Defaults Must Produce 2.3.0 Artifacts
The installer build defaults and setup metadata SHALL target `uMeter-Setup-2.3.0.exe`.

### R5: Residual Risks Must Be Recorded
Any verification that is not run or any known release limitation SHALL be documented before completion.
