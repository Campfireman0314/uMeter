# Tasks

## Task 1: Add single-instance coordinator

**Requirement Trace:** [R1, R3] -> [U1]  
**Execution Mode:** [test-first]
**Files:**
- `src/DesktopLoudnessMeter.App/Infrastructure/SingleInstanceCoordinator.cs`
- `tests/DesktopLoudnessMeter.Tests/Infrastructure/SingleInstanceCoordinatorTests.cs`

**Acceptance Criteria:**
- [x] Primary startup acquires the lock and starts an activation listener.
- [x] Secondary startup fails the lock and attempts an activation signal.
- [x] Coordinator disposal releases lock/listener and stops forwarding activation events.

**Dependencies:** none

## Task 2: Gate WPF startup before duplicate resources are created

**Requirement Trace:** [R2, R5] -> [U2]  
**Execution Mode:** [direct]
**Files:**
- `src/DesktopLoudnessMeter.App/App.xaml.cs`

**Acceptance Criteria:**
- [x] Single-instance coordination starts before settings, source services, receivers, tray icon, or windows.
- [x] Secondary launch exits with code 0.
- [x] Secondary launch log messages are informational.

**Dependencies:** Task 1

## Task 3: Restore/activate primary window on secondary launch

**Requirement Trace:** [R4] -> [U3]  
**Execution Mode:** [direct]
**Files:**
- `src/DesktopLoudnessMeter.App/App.xaml.cs`

**Acceptance Criteria:**
- [x] Activation requests show the main window.
- [x] Minimized windows are restored.
- [x] Requests received before the window is ready are replayed after startup.

**Dependencies:** Task 2
