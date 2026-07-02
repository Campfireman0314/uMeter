# 02-webview-shell-foundation

## Goal
Introduce the clean WebView2 and React/Vite application foundation on top of `main`, with one primary app surface and no long-term WPF compatibility shell.

## In Scope
- Add the WebView frontend project and build wiring needed for local development and solution compilation.
- Host the WebView surface from the desktop app startup path while preserving single-instance, tray, activation, theme, and shutdown behavior.
- Establish the host bridge bootstrap, initial host snapshot delivery, error reporting, and development/production asset loading rules.
- Salvage useful React/Vite setup and WebView2 hosting ideas from the reference branch only after the harness classifies them.
- Keep WPF only where it remains necessary for platform services, native dialogs, tray integration, or staged migration internals.

## Out of Scope
- Full source-management command parity.
- Detail chart migration.
- Installer or publish packaging.
- A permanent runtime fallback that lets WPF and WebView drift as separate products.

## Depends On
- `01-migration-harness-and-contract-audit` for acceptance criteria, state ownership, and salvage decisions.
- Current `main` startup and lifecycle behavior.

## Done Means
- The solution restores and compiles with the WebView project included.
- The desktop app launches into the WebView surface in development and production asset modes.
- Startup, activation, tray visibility, theme application, and shutdown paths remain covered by tests or characterization checks.
- The foundation has a documented bridge and snapshot boundary that later slices can extend without mock-only behavior.
