# Review Report

## code-review | 2026-06-17T20:14:06.0313339+08:00 | pass_with_warnings
findings:
- [WARNING] business logic: display-name recovery originally allowed same-name sources from different source kinds to reuse the same row; fixed by requiring source-kind equality in `MainViewModel.TryFindRecoverableRow` and covered by `RefreshNowDoesNotRecoverRowsAcrossSourceKinds`.
- [WARNING] manual validation: compact global-row source badge overlap still needs visual inspection with live audio sources at narrow widths.
