# 04-source-list-workspace-controls

## Goal
Give users direct control over source names and global source ordering/visibility while preserving stable identity across live and hibernated sources.

## In Scope
- Merge source-name display and alias editing so double-clicking the title edits the user alias in place.
- Preserve provider-owned display names separately from user aliases and expose a clear way to clear/revert an alias.
- Add a per-source hide toggle and a global `Show hidden` toggle.
- Add global-view drag reordering for sources.
- Persist aliases, hidden state, and manual order in `UserWorkspaceSettings` keyed by stable source identity.
- Keep manual order stable for alive and hibernated sources, and define fallback ordering for new/unordered sources.
- Update source-management docs for aliases, hidden sources, and ordering.

## Out of Scope
- Source lifecycle semantics already owned by `01-source-continuity-reconfiguration`.
- Spectrograph fidelity changes.
- Full settings import/export.

## Depends On
- `01-source-continuity-reconfiguration` for stable alive/hibernated/closed source semantics.
- Existing workspace source settings and source-management polish foundation.

## Done Means
- Users can rename a source alias directly from the title without a separate text editor control.
- Hidden sources disappear from normal global view, reappear when `Show hidden` is enabled, and can be unhidden.
- Dragged source order survives refreshes, temporary handle loss, and restart when the same stable source identity returns.
- New sources get deterministic placement that does not disturb existing manual order.
