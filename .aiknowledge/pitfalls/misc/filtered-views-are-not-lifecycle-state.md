---
status: active
created_at: 2026-06-29
created_from: change:2026-06-29-host-snapshot-command-contract
last_verified_at: 2026-06-29
last_verified_by: opsx-knowledge
verification_basis: archive-follow-up
source_refs:
  - change:2026-06-29-host-snapshot-command-contract
  - evidence:openspec/changes/archive/2026-06-29-host-snapshot-command-contract/evidence.md
applies_to:
  - src/DesktopLoudnessMeter.App/Host/DesktopMeterHostController.cs
  - src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs
superseded_by:
---

# Filtered Views Are Not Lifecycle State

## Problem

Using a filtered UI/source row collection as the source of lifecycle truth can produce false appearance or disappearance events when a user changes visibility filters.

## Signal

- A hidden source exists in the latest service snapshot.
- The visible row list excludes it because `ShowHiddenSources` is false.
- Toggling the filter to show hidden sources makes the source appear in the visible list.
- Lifecycle tracking based on the visible list records `Source appeared`, even though ingest state did not change.

## Prevention

Track live lifecycle state from the unfiltered service/source snapshot identity set. Apply visibility filters only after lifecycle keys have been recorded. UI rows and host snapshots may be filtered, but appearance/disappearance events should compare live source keys across refreshes.

## Verification

Add a regression where a hidden live source is refreshed while hidden, then `ShowHiddenSources` is enabled. The host snapshot should reveal the source without adding a new appearance event.
