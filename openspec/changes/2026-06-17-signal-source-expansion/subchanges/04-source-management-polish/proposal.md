# 04-source-management-polish

## Goal
Make mixed WASAPI, loopback, and plugin sources understandable and comfortable for daily use.

## In Scope
- Polish source-type badges, source status text, source detail headers, tray pop-up source selection, and grouping/sorting rules.
- Resolve display-name collisions while preserving provider-owned unique handles.
- Decide and implement which source preferences should persist, such as display aliases, source visibility, window placement, topmost state, and per-source analysis settings.
- Improve diagnostics for source activation, heartbeat timeout, reconnect, rename, protocol mismatch, and analyzer availability.
- Update README, release notes, and troubleshooting docs for multi-source behavior.

## Out of Scope
- Adding new transport protocols.
- Implementing plugin binary packaging.
- Official loudness/vector certification.

## Depends On
- `01-signal-source-foundation` for source metadata.
- `02-local-loopback-ingest` for non-WASAPI runtime states.
- `03-plugin-sender-prototypes` for final plugin-facing terminology, if plugin work has landed.
- Existing `04-polish-persistence` notes should be reconciled so settings persistence has one owner.

## Done Means
- Users can quickly tell what kind of source each row is, where it came from, whether it is active/stale/reconnecting, and which name is user-facing.
- Source settings survive common reconnect and restart paths where persistence is in scope.
- Documentation explains source types and expected routing behavior.
