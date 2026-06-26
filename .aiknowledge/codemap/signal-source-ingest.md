---
status: active
created_at: 2026-06-26
created_from: change:2026-06-26-v2.3.0-polish-01-source-continuity-reconfiguration
last_verified_at: 2026-06-26
last_verified_by: opsx-codemap
verification_basis: archive
applies_to:
  - src/DesktopLoudnessMeter.App/Audio/LocalLoopbackSignalSourceService.cs
superseded_by:
---

# Signal Source Ingest

## Responsibility
Owns localhost loopback source registration, heartbeat/close handling, audio packet analysis, source timeout cleanup, analyzer settings, and snapshot creation for plugin or local loopback sources.

## Key Files

| File | Role |
|------|------|
| src/DesktopLoudnessMeter.App/Audio/LocalLoopbackSignalSourceService.cs | Thread-safe source dictionary, same-handle reconfiguration, analyzer lifecycle, and snapshot projection |
| tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackSignalSourceServiceTests.cs | Registration, reconfiguration, timeout, close, and analyzer preference coverage |

## Implicit Constraints
- `Ingest`, settings updates, snapshots, and disposal are protected by the service lock.
- Registering an existing handle with a new sample rate or channel count reconfigures the analyzer in place and preserves source key identity.
- Explicit close and timeout remove the source and dispose the analyzer; same-handle format restarts do not.
