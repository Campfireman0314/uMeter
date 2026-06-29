---
status: active
created_at: 2026-06-26
created_from: change:2026-06-26-v2.3.0-polish-03-single-instance-launch
last_verified_at: 2026-06-26
last_verified_by: opsx-codemap
verification_basis: archive
applies_to:
  - src/DesktopLoudnessMeter.App/App.xaml.cs
  - src/DesktopLoudnessMeter.App/Infrastructure
superseded_by:
---

# App Startup

## Responsibility
Owns desktop application startup order, single-instance coordination, tray icon lifecycle, and activation requests from secondary launches. It runs before source receivers, settings-backed view models, tray icons, or windows are created.

## Key Files

| File | Role |
|------|------|
| src/DesktopLoudnessMeter.App/App.xaml.cs | Startup and exit orchestration, primary/secondary instance branching, tray menu actions, and activation replay |
| src/DesktopLoudnessMeter.App/Infrastructure/SingleInstanceCoordinator.cs | Per-user/session mutex and named-pipe activation client/server |
| src/DesktopLoudnessMeter.App/Infrastructure/TrayExitCoordinator.cs | Defers tray shutdown out of the context-menu callback and makes exit idempotent |
| tests/DesktopLoudnessMeter.Tests/Infrastructure/SingleInstanceCoordinatorTests.cs | Lock, activation, and disposal coverage |
| tests/DesktopLoudnessMeter.Tests/Infrastructure/TrayExitCoordinatorTests.cs | Deferred and idempotent tray exit coverage |

## Implicit Constraints
- The single-instance lock must be acquired before creating receivers, settings writers, tray icons, or windows.
- Secondary launches must send activation and exit without running the normal app graph.
- Activation can arrive before the main window exists, so startup keeps a replay flag and shows the window after creation.
