# Spec: completion audit refresh

## Requirement: final-state coverage
**Trace**: R1

The audit refresh must cover every final-state requirement in the user goal and assign an evidence grade to each requirement.

### Scenario: refresh the completion matrix
- **Given** the user goal lists final-state requirements for WebView UI, Windows WebView2, macOS WKWebView boundary, C++ Core, Host glue, HostBridge v1, React/Vite, JUCE/Protocol, Windows-first delivery, and WPF fallback/oracle
- **When** the audit refresh writes evidence
- **Then** every requirement must have one of `proven`, `incomplete`, `weak_evidence`, `contradicted`, or `not_applicable_by_scope`
- **And** each grade must cite current source, codemap, archived evidence, or fresh command output

## Requirement: old gap map recheck
**Trace**: R2

The audit refresh must re-evaluate the old `2026-06-30-completion-audit` remaining gap map instead of carrying stale conclusions forward.

### Scenario: later archives close old gaps
- **Given** the old audit gap map listed `web-settings-workflow-parity`, `native-protocol-ingest-core`, `native-host-snapshot-projection-core`, `host-glue-boundary-reduction`, and `macos-runner-implementation`
- **When** current archives and source evidence prove that a gap has been covered
- **Then** the audit refresh must mark that old gap as `closed` or `superseded`
- **And** any still-unproven part must be restated as a current remaining gap

## Requirement: fresh current-state evidence
**Trace**: R3

The audit refresh must use current repository state and fresh verification rather than relying on memory.

### Scenario: current command verification
- **Given** the current worktree contains the Web frontend, .NET tests, native project, and OpenSpec artifacts
- **When** implementation performs the audit refresh
- **Then** it must run or record current Web build/test, .NET regression, and scope/check commands
- **And** unavailable platform or native checks must be recorded as weak/missing evidence rather than pass

## Requirement: current remaining gap map
**Trace**: R4

If the goal is not complete, the audit refresh must output a current remaining gap map and recommended next implementation change.

### Scenario: goal is still incomplete
- **Given** any final-state requirement is `incomplete`, `weak_evidence`, or `contradicted`
- **When** the audit refresh states its final verdict
- **Then** the verdict must be `not_complete`
- **And** evidence must list the gap, impact, current evidence, and suggested next change

## Requirement: lifecycle guard
**Trace**: R5

The audit refresh must complete the OpenSpec lifecycle and must not treat the audit itself as overall goal completion.

### Scenario: audit is not implementation completion
- **Given** this change is a read-only audit refresh
- **When** verify, review, and archive complete
- **Then** this change only proves that the audit refresh is complete
- **And** overall goal completion is allowed only if the audit matrix proves every final-state requirement
