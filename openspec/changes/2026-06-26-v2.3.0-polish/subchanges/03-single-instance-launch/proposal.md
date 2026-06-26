# 03-single-instance-launch

## Goal
Allow only one uMeter desktop executable instance per user session and make repeated launches activate the existing instance instead of creating competing receivers and settings writers.

## In Scope
- Add a named single-instance guard at app startup.
- Decide per-user versus machine-wide scope and document the choice.
- Bring the existing instance to the foreground or restore it from tray when a second launch occurs.
- Avoid starting duplicate UDP receivers, timers, tray icons, or settings writes from rejected secondary instances.
- Add tests or an injectable coordination abstraction where feasible.

## Out of Scope
- Cross-machine coordination.
- Installer enforcement beyond launching the normal executable.
- Solving UDP port conflict diagnostics for unrelated third-party processes.

## Depends On
- Existing app startup, tray restore behavior, and plugin receiver initialization.

## Done Means
- A second launch exits quickly and leaves exactly one running uMeter instance.
- If the first instance is hidden to tray or behind other windows, the second launch requests restore/activation.
- Existing startup logging records useful information without treating normal second launches as crashes.
