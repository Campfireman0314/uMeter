# 05-v2.3.0-release-convergence

## Goal
Converge the completed polish slices into a coherent `v2.3.0` release with version numbers, docs, packaging, and regression checks aligned.

## In Scope
- Update product/project/installer version metadata from `2.2.8` to `2.3.0`.
- Add `v2.3.0` release notes and update README, changelog, current status, and packaging references.
- Re-run managed, native, JUCE, and installer verification after all preceding slices land.
- Confirm generated installer contains the updated desktop app, native DLL, and VST3 sender.
- Capture remaining known limits and follow-up candidates.

## Out of Scope
- Implementing any bug fix or feature behavior from earlier slices.
- Signing/notarization unless separately requested.

## Depends On
- `01-source-continuity-reconfiguration`
- `02-spectrograph-fidelity`
- `03-single-instance-launch`
- `04-source-list-workspace-controls`

## Done Means
- All version references and release artifacts agree on `2.3.0`.
- Verification commands pass or any residual risks are explicitly documented.
- Release notes describe the source continuity, spectrograph, single-instance, alias, hide, and reorder changes in user-facing terms.
