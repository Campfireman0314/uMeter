# Evidence: 2026-07-01-native-host-projection-core

## Final Gate Summary

- final_readiness: pass_with_warnings
- final_verify: pass_with_warnings
- final_review: pass_with_warnings
- final_archive: pass

The consolidated gate state is `pass_with_warnings` because two source changes carried non-blocking warnings.

## Consolidated Archives

| Change | Readiness | Verify | Review | Notes |
|--------|-----------|--------|--------|-------|
| `2026-06-30-native-host-source-list-projection-core` | pass_with_warnings | pass_with_warnings | pass_with_warnings | Native source-list projection; retained ordering and local environment warnings. |
| `2026-07-01-native-host-recent-events-core` | pass_with_warnings | pass_with_warnings | pass | Native Host/Web recent-event lifecycle/history; retained warning state from original gate. |
| `2026-07-01-native-host-detail-module-projection-core` | pass | pass | pass | Native detail module row projection. |
| `2026-07-01-native-host-snapshot-chrome-projection-core` | pass | pass | pass | Native settings/chrome and command availability projection. |
| `2026-07-01-native-host-collision-projection-core` | pass | pass | pass | Native selected-detail collision band projection. |
| `2026-07-01-native-host-selected-detail-shell-core` | pass | pass | pass | Native selected-detail shell metadata/default planning. |
| `2026-07-01-native-host-aggregate-snapshot-core` | pass | pass | pass | Native aggregate snapshot metadata planning. |
| `2026-07-01-native-host-selected-detail-snapshot-core` | pass | pass | pass | Unified native selected-detail snapshot planning. |

## Retained Warnings

- CMake/JUCE binary readiness was unavailable in the local environment for the source-list projection change. Windows MSBuild/native, managed, app and web paths were verified there; macOS/JUCE runtime parity was not claimed by that change.
- Source-list fallback display-name ordering uses deterministic ASCII/ordinal case-folding in native projection rather than .NET `CurrentCultureIgnoreCase`. Explicit workspace order still has priority; revisit if localized non-ASCII source-name ordering becomes a tested UX requirement.
- The recent-events source change carried `pass_with_warnings`; this consolidation preserves that warning level rather than recasting it as a clean pass.

## Consolidation Evidence

- The fine-grained projection archives were replaced by this consolidated archive to reduce OpenSpec noise.
- Knowledge references now point to `openspec/changes/archive/2026-07-01-native-host-projection-core/evidence.md`.
- Historical fine-grained evidence remains recoverable from Git history before the consolidation.
