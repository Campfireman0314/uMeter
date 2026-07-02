# Misc

| Entry | Status | Last Verified | Summary |
|-------|--------|---------------|---------|
| [Bridge Circular Plot Cycle Origins](bridge-circular-plot-cycle-origins.md) | active | 2026-06-27 | Circular history plots should synthesize boundary segments at phase zero instead of assuming timer samples land exactly on the origin. |
| [Filtered Views Are Not Lifecycle State](filtered-views-are-not-lifecycle-state.md) | active | 2026-06-29 | Lifecycle events should compare unfiltered live source identities, not UI-filtered row collections. |
| [HostBridge Enum-Like Settings Need Canonical Values](hostbridge-enum-like-settings-need-canonical-values.md) | active | 2026-06-30 | HostBridge string settings that are accepted case-insensitively must persist canonical ids consumed by Web/UI resolvers. |
| [HostBridge Numeric Settings Need Domain Validation](hostbridge-numeric-settings-need-domain-validation.md) | active | 2026-06-30 | HostBridge numeric settings commands must validate domain ranges, not just finite JSON numbers, before persisting workspace state. |
| [Native Consumers Must Import Copy Targets](native-consumers-must-import-copy-targets.md) | active | 2026-06-30 | Managed executables/tests/benchmarks and publish/installer payloads that P/Invoke a native DLL must import or run the native copy target, not rely on a referenced app project or build-output copy alone. |
| [Visible-Only Layout Bindings Can Hide Controls](visible-only-layout-bindings-can-hide-controls.md) | active | 2026-06-27 | Binding container constraints to hidden-state layout values can collapse headers or controls that should stay reachable. |
