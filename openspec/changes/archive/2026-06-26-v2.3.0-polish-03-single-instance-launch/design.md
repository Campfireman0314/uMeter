# Design

## U1: Named User-Session Coordinator

`SingleInstanceCoordinator` owns a `Local\...` named mutex scoped by current user identity and process session id. This allows independent users or sessions to run uMeter while preventing competing receivers/settings writers in the same interactive session.

The primary instance starts a per-user/session named pipe listener. A secondary instance that cannot acquire the mutex connects to the pipe, writes a one-byte activation signal, and exits.

## U2: Early Startup Gate

`App.OnStartup` creates and starts the coordinator before workspace settings, source services, UDP receivers, tray icon, timers, and windows are constructed. Secondary instances dispose the coordinator and call `Shutdown(0)`.

## U3: Activation Restore

The primary listens for activation requests. Requests before the main window is ready are latched and replayed after window creation. Requests after startup show the main window, restore minimized state, activate, and focus.

## Tests

The coordinator is built from injectable lock/server/client interfaces so tests can cover primary acquisition, secondary activation request, event forwarding, and disposal lifecycle without spawning real processes.
