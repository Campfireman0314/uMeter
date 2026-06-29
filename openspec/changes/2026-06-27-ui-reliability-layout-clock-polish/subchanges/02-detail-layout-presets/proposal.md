# 02-detail-layout-presets

## Goal
Let users save and load detail-panel layout/settings presets as JSON files, with safe validation and optional default-layout loading.

## In Scope
- Add save/load layout commands/icons for both inline global detail and pop-up detail surfaces.
- Persist the relevant detail visualization settings to a layout JSON file and apply them back to the active detail view.
- Add a global setting that can load a default detail layout from a chosen layout file without deleting temporary/user-saved layout files.
- Validate layout file shape/version and report errors while falling back to hard-wired defaults when a configured default layout is invalid.
- Cover serialization/validation/application behavior with tests.

## Out of Scope
- Cloud sync or layout library management.
- Bulk applying a preset to every stored source automatically.
- Changing source identity or alias persistence.

## Depends On
- Existing per-source detail preferences in `UserWorkspaceSettings`.
- Existing inline and pop-up detail view-model settings.

## Done Means
- Users can export/import detail layout JSON for inline and pop-up detail views.
- The global default-layout setting safely loads a valid layout file or reports/falls back on invalid input.
- Tests validate layout file compatibility and error handling.
