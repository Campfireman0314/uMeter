---
status: active
created_at: 2026-06-30
created_from: change:2026-06-30-webview-chart-parity
last_verified_at: 2026-06-30
last_verified_by: opsx-knowledge
applies_to:
  - src/DesktopLoudnessMeter.App/WebView/HostBridgeController.cs
  - src/DesktopLoudnessMeter.App/Settings/UserWorkspaceSettings.cs
source_refs:
  - change:2026-06-30-webview-chart-parity
  - evidence:openspec/changes/archive/2026-06-30-webview-chart-parity/evidence.md
superseded_by:
deprecated_reason:
---

# HostBridge Numeric Settings Need Domain Validation

## Problem

HostBridge command payloads that write numeric settings can be valid JSON and finite numbers while still violating the domain range used by WPF/settings. If the bridge only rejects `NaN`/`Infinity`, a WebView message can persist inverted or out-of-range settings that later projections and renderers do not expect.

## Signal

- A new `HostBridgeController` command writes `UserWorkspaceSettings` numeric fields directly.
- Existing WPF/UI setters clamp or coerce the same setting.
- `UserWorkspaceSettings.Save()` does not normalize every numeric domain field being written.

## Guardrail

- Validate numeric command payloads at the HostBridge creation boundary using the same usable range as the settings/UI domain.
- Add a typed-error test that confirms invalid numeric payloads return `invalidRequest` and do not create or mutate source settings.
- Keep renderer-side defaults as a fallback, but do not rely on renderer clamping to protect persisted workspace state.
