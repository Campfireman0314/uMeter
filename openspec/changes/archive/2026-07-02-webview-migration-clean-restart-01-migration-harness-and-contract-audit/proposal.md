# 01-migration-harness-and-contract-audit

## Problem Statement
The prior WebView refactor compiles in places but mixes product behavior, compatibility fallback, mock-only bridge behavior, and native-host experiments without a shared way to judge differences from `main`. Continuing directly would make every mismatch feel negotiable, and starting feature work without a harness would hide fatal parity breaks until late in the migration.

## Goal
Establish a migration judgment harness before rebuilding the WebView app surface, so branch differences are classified as fatal parity breaks, blocking clean-design gaps, acceptable intentional changes, or discardable legacy behavior.

## In Scope
- Use `main` as the behavior oracle and `D:\GitRepo\uMeter-webview-reference` as a salvage reference, not as a runtime fallback design.
- Define the parity decision rules for product promises: startup/build/package, source identity, alias/order/visibility persistence, metering correctness, command/UI alignment, detail settings, and performance gates.
- Draft a contract matrix that ties WebView commands, mock bridge behavior, C# bridge parsing, host execution, and tests together.
- Draft deterministic scenario traces with fake sources for refresh, select, alias edit, hide/restore, reorder, forget/reset, detail settings, persistence, and reconnect paths.
- Create a state ownership map for runtime state, persisted workspace settings, analysis settings, theme/window settings, and recent events.
- Produce a salvage ledger for the existing WebView branch, including useful pieces to copy, areas to redesign, and compatibility code to delete.

## Out of Scope
- Migrating the UI shell itself.
- Rewriting source workflows or chart rendering.
- Deciding final installer or native packaging behavior.
- Keeping WPF and WebView as permanent peer product surfaces.

## Depends On
- `origin/main` and the current clean restart branch.
- Detached reference worktree `D:\GitRepo\uMeter-webview-reference` at the prior WebView refactor branch.
- Existing codemap entries for startup, source workspace, ingest, and analysis rendering.

## Done Means
- Differences can be judged with explicit fatal/blocking/acceptable/discardable criteria.
- The command matrix exposes missing real bridge handlers, mock-only commands, and untested host actions.
- The scenario trace list is ready to become characterization tests in planning.
- The state ownership map names exactly one owner for each migrated state field.
- The salvage ledger identifies which files and concepts can be reused without carrying compatibility debt.

## Success Criteria
- Planning for later WebView slices can cite a concrete parity rule instead of relying on taste.
- Known WebView-branch issues, including mock-only commands and native fallback drift, are visible in the audit outputs.
- Compatibility bridges are permitted only as staged implementation aids with an owner and retirement condition.
