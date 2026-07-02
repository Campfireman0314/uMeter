# Parity Decision Harness Spec

## Requirements

### Requirement: Difference Severity Taxonomy
**Trace**: R1

The migration harness must classify observed differences from `main` as `fatal`, `blocking-fixable`, `acceptable-intentional`, or `discardable-legacy`.

#### Acceptance Criteria
- A `fatal` classification is required for differences that break startup, build, publish, source identity, persisted user data, metering correctness, or command/UI alignment.
- A `blocking-fixable` classification is required for intended WebView behavior that lacks a clean host contract, snapshot field, or testable owner.
- An `acceptable-intentional` classification requires the same user outcome as `main`, an explicit rationale, and an owner.
- A `discardable-legacy` classification requires proof that the behavior is a WPF-only artifact rather than a product promise.

### Requirement: Product Promise Baseline
**Trace**: R2

The harness must record the `main` behavior baseline for every migrated product promise before later slices change product code.

#### Acceptance Criteria
- The baseline covers startup and shutdown behavior.
- The baseline covers source identity, alias, visibility, ordering, selection, reset, and reconnect behavior.
- The baseline covers detail settings and metering outputs that are relevant to the WebView migration.
- The baseline covers restore, build, publish, and package expectations that affect running on this PC.

### Requirement: Compatibility Debt Policy
**Trace**: R3

The migration plan must reject permanent compatibility bridges unless a bridge is explicitly recorded as a staged implementation aid.

#### Acceptance Criteria
- Each staged bridge has an owner.
- Each staged bridge has a retirement condition.
- Each staged bridge names the later slice that must remove or absorb it.
- Unowned WPF/WebView fallback behavior is classified as `blocking-fixable` or `discardable-legacy`.

### Requirement: Salvage Ledger
**Trace**: R4

The harness must maintain a salvage ledger for the reference WebView branch.

#### Acceptance Criteria
- The ledger marks each inspected WebView-branch component as `copy`, `adapt`, `redesign`, `delete`, or `defer`.
- Each `copy` or `adapt` decision cites the product promise or harness requirement it serves.
- Each `redesign` or `delete` decision names the debt or mismatch that prevents reuse.
- The ledger includes React/Vite setup, WebView hosting, HostBridge DTOs, native host work, mock bridge behavior, tests, and packaging changes.
