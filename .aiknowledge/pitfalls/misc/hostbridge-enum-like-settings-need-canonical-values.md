---
status: active
created_at: 2026-06-30
created_from: change:2026-06-30-web-settings-workflow-parity
last_verified_at: 2026-06-30
last_verified_by: opsx-knowledge
applies_to:
  - src/DesktopLoudnessMeter.App/WebView/HostBridgeController.cs
  - src/DesktopLoudnessMeter.Web/src/AppShell.tsx
  - src/DesktopLoudnessMeter.App/Settings/UserWorkspaceSettings.cs
source_refs:
  - change:2026-06-30-web-settings-workflow-parity
  - evidence:openspec/changes/archive/2026-06-30-web-settings-workflow-parity/evidence.md
superseded_by:
deprecated_reason:
---

# HostBridge Enum-Like Settings Need Canonical Values

## Problem

Bridge commands often accept user-friendly or case-insensitive string values for enum-like settings. If the bridge only validates membership and then persists the raw input, downstream consumers that resolve exact canonical ids can reject or fallback from a setting that the host just accepted.

## Signal

- A `HostBridgeController` command accepts string ids for a persisted workspace setting.
- The bridge validation is case-insensitive or otherwise tolerant.
- The Web/CSS/renderer side uses exact ids, `data-*` attributes, switch statements, or dictionary lookup.
- A successful command result can return a snapshot containing an id shape not recognized by the consumer.

## Guardrail

- Validate and canonicalize string ids at the bridge boundary before creating the host command.
- Persist the canonical DTO value, not the request spelling or display label.
- Add a regression where a mixed-case or alias input succeeds but the post-command snapshot contains the exact canonical id consumed by the UI.
