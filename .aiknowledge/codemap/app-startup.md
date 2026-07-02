---
status: active
created_at: 2026-06-26
created_from: change:2026-06-26-v2.3.0-polish-03-single-instance-launch
last_verified_at: 2026-06-30
last_verified_by: opsx-knowledge
verification_basis: archive-follow-up
applies_to:
  - src/DesktopLoudnessMeter.App/App.xaml.cs
  - src/DesktopLoudnessMeter.App/Infrastructure
source_refs:
  - change:2026-06-29-webview-packaging-startup-tray
  - evidence:openspec/changes/archive/2026-06-29-webview-packaging-startup-tray/evidence.md
  - change:2026-06-30-wpf-retirement
  - evidence:openspec/changes/archive/2026-06-30-wpf-retirement/evidence.md
superseded_by:
---

# App Startup

## Responsibility
Owns desktop application startup order, single-instance coordination, tray icon lifecycle, and activation requests from secondary launches. On Windows the primary startup and activation target is the WebView2 shell; WPF is retained as an internal fallback/regression oracle seam, not a normal user-visible tray entry. The single-instance gate still runs before source receivers, settings-backed view models, tray icons, or windows are created.

## Key Files

| File | Role |
|------|------|
| src/DesktopLoudnessMeter.App/App.xaml.cs | Startup and exit orchestration, primary/secondary instance branching, tray menu actions, and activation replay |
| src/DesktopLoudnessMeter.App/Infrastructure/AppWindowRouter.cs | Testable startup/tray/secondary-activation routing seam for WebView-primary behavior and explicit WPF fallback |
| src/DesktopLoudnessMeter.App/Infrastructure/SingleInstanceCoordinator.cs | Per-user/session mutex and named-pipe activation client/server |
| src/DesktopLoudnessMeter.App/Infrastructure/TrayExitCoordinator.cs | Defers tray shutdown out of the context-menu callback and makes exit idempotent |
| tests/DesktopLoudnessMeter.Tests/Infrastructure/AppWindowRouterTests.cs | WebView-primary routing and explicit fallback regression coverage |
| tests/DesktopLoudnessMeter.Tests/Infrastructure/AppTrayMenuTests.cs | Tray normal-menu characterization that keeps Show/Pop-up/Exit while excluding WPF fallback |
| tests/DesktopLoudnessMeter.Tests/Infrastructure/SingleInstanceCoordinatorTests.cs | Lock, activation, and disposal coverage |
| tests/DesktopLoudnessMeter.Tests/Infrastructure/TrayExitCoordinatorTests.cs | Deferred and idempotent tray exit coverage |

## Implicit Constraints
- The single-instance lock must be acquired before creating receivers, settings writers, tray icons, or windows.
- Secondary launches must send activation and exit without running the normal app graph; the primary instance restores the WebView shell.
- Tray `Show uMeter` and double-click route to the WebView shell. The normal tray menu must not expose WPF fallback; WPF remains reachable only through internal/test fallback seams.
- Activation can arrive before the WebView shell exists, so startup keeps a replay flag and shows the shell after creation.
