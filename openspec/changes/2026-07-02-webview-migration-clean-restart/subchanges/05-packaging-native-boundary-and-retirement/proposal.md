# 05-packaging-native-boundary-and-retirement

## Goal
Finalize build, publish, installer, and native-boundary decisions for the WebView migration, then retire obsolete compatibility surfaces and fallback code paths that no longer match the chosen product direction.

## In Scope
- Decide and document whether native DSP/runtime components are mandatory or whether managed fallback remains a supported product promise.
- Make solution build, publish, and installer output include the WebView assets and required native binaries consistently.
- Remove or quarantine WPF UI fallback surfaces once the harness confirms their behavior is either migrated or intentionally discarded.
- Verify startup failure messaging, missing native dependency handling, and development versus packaged asset loading.
- Update user-facing and developer documentation for the new WebView app surface, build requirements, and packaging expectations.

## Out of Scope
- Introducing new product capabilities unrelated to migration completion.
- Reworking source workflows or charts beyond packaging integration fixes.
- Supporting two permanent UI systems with divergent behavior.

## Depends On
- `01-migration-harness-and-contract-audit` for retirement criteria and native-boundary decision rules.
- `02-webview-shell-foundation` for asset loading and app hosting.
- `03-hostbridge-source-workflows` and `04-detail-charts-and-settings` before final WPF surface retirement.

## Done Means
- Restore, build, test, publish, and installer/package checks pass on this PC.
- Native dependency behavior is explicit, tested, and documented.
- Obsolete WPF/WebView compatibility bridges are removed or isolated with a documented reason.
- The migration has a clear release checklist instead of relying on the old branch's accidental working state.
