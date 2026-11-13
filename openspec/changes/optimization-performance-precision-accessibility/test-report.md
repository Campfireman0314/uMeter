# Verification report — performance, precision, and accessibility

## Delivery boundary

- Integration branch: `optimization/performance-precision-accessibility`.
- Original baseline: `396774a56f086a169059035cada64f1dd2d52478` (`main`).
- Scope: the existing WPF application/shared DSP, existing managed tests and benchmark executable, metering notes, and this OpenSpec change. No transport, plugin-editor, installer, release-version, dependency, or global-configuration changes were authored.
- User-owned untracked `.claude/`, `.codex/`, `output/`, and `tmp/` are excluded from delivery. Existing other worktrees are untouched.
- Engineering implementation and automated verification are complete; local commit recording is the final delivery step. Human and real-device acceptance remains open below. No push, PR, merge, deployment, or external tracker publication.

## Observed final commands

Windows `10.0.26300`, x64, .NET SDK `11.0.100`, target/runtime `.NET 8.0.34`, 24 logical CPUs. All build/test/format commands were run serially. Shell pipelines used `set -euo pipefail`; the recorded result is the tested command's exit, not a filtering command's exit.

| Check | Observed result |
| --- | --- |
| Baseline `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --no-restore -v minimal` | exit 0; 396 passed, 0 failed, 0 skipped |
| Final `dotnet build desktop-loudness-meter.sln --no-restore -v minimal` | exit 0; 0 warnings, 0 errors |
| Final `dotnet test desktop-loudness-meter.sln --no-restore -v minimal --blame-hang --blame-hang-timeout 30s --results-directory <task-artifacts>/parent-tests` | exit 0; **492 passed, 0 failed, 0 skipped**, approximately 3 s |
| `dotnet format desktop-loudness-meter.sln --no-restore --verify-no-changes --include <all changed/new src and tests paths> --verbosity minimal` | exit 0; no formatting changes required |
| `git diff --check` | exit 0; no whitespace errors (Git's LF/CRLF conversion notices are not test failures) |
| Installed OpenSpec CLI: `node <installed-openspec>/bin/openspec.js validate optimization-performance-precision-accessibility --strict --no-interactive --json` | exit 0; change valid, zero issues |
| Managed-fallback analysis suite from a copy of final test output with the native DLL removed: `dotnet vstest <copy>/DesktopLoudnessMeter.Tests.dll --TestCaseFilter:FullyQualifiedName~Audio.Analysis` | exit 0; **81 passed**; no source or installed DLL was changed |
| Final true-peak benchmark, `--true-peak-only` | exit 0; managed/native printed peak deltas `0.000000` at 2x/4x/8x |

The fallback suite includes existing optional-native tests that return early when the DLL is absent. Its aggregate pass count is therefore **not** evidence that native comparison tests exercised a native backend in that run. The rate/reference/analytical analyzer fixtures ran through the managed fallback; the ordinary full run had native acceleration available.

The OpenSpec strict check initially rejected the prose-only carrier because it had no delta files. The already-agreed acceptance criteria were formalized under `specs/metering-precision/`, `specs/analysis-efficiency/`, and `specs/desktop-accessibility/`; the final strict check passes. The installed Git Bash launcher selected a broken npm-local Node shim, so validation invoked the same installed OpenSpec entrypoint with the existing `C:/Program Files/nodejs/node.exe`. No CLI installation or global configuration was changed.

### Precision coverage

- K-weighting now takes the configured input sample rate and reconstructs the weighting curve while reproducing the established 48 kHz coefficient set to within double rounding.
- Reference tests cover 44.1/48/96 kHz at 100 Hz, 1 kHz, 2 kHz, and 10 kHz, plus the stereo 1 kHz -23 LUFS reference level. Expected values are literal offline frequency-response results, not calculated with the production filter in the test. Their provenance, formula, and 0.1 LU tolerance are documented in the tests and metering notes.
- The offline non-48-kHz reference values use one separate derivation of the same published weighting design; they are not a second certified meter implementation. The 48 kHz published coefficients, reference level, and closed-form true-peak signal provide additional anchors.
- The analytical true-peak fixture uses an `fs/4`, phase `pi/4` sine: continuous peak `A`, discrete peak `A/sqrt(2)`. For `A=0.9`, the expected continuous peak is approximately `-0.9151 dBFS`.
- Streaming partitioning, reset, startup windows, silence, Overall/Mid/Side semantics, exact integrated gating, and native/managed agreement remain covered. This is **not ITU/EBU certification** or validation against the full official programme material.

### Accessibility coverage

Actual STA WPF controls and real UI Automation peers/providers are exercised, not only source-text checks. Range endpoints support arrows, Home/End, Space endpoint selection, Enter/F2 numeric entry, Escape cancellation, and normal unhandled Tab navigation. Single-value controls receive the same editing/focus treatment. Provider tests cover values, bounds, enabled state, binding preservation, endpoint automation focus, and notification-raising paths. Both detail XAML surfaces expose localized accessible names; themed focus contracts include live control/pixel tests and a simulated high-contrast brush path.

The parent reproduced the final fractional-bound defect with six failing tests (exit 1), then verified 41 slider tests passing after centralized coercion preserved exact endpoints and clamped rounded interior values. Final full-suite evidence above follows that last source edit.

Notification-raising assertions do not prove client-side delivery to Narrator. Unhandled Tab assertions do not prove complete real-window focus traversal. Those limitations remain explicit.

### Recovered test-hang incident

An early accessibility full-suite attempt hung after targeted provider tests had passed. The parent verified the exact test-runner/testhost ownership and stopped only that task-owned testhost. That attempt is **failed**, not green. The worker changed the new STA helper to serialize WPF scenarios, run a real dispatcher loop, use per-event presentation sources, capture dispatcher exceptions, and bound joins to 30 seconds on background threads. Subsequent worker suites and the final parent full-suite run completed; no tests were removed or skipped to hide the incident.

## Independent reviews and disposition

### Standards

Initial independent findings: endpoint UIA focus delegation and missing backend identity in analyzer-only benchmarks. A later review found fractional-bound quantization still invalidated provider ranges. All are resolved; the final retained review reports **no remaining material finding** in the changed seam, retaining its earlier accepted conclusions.

### Spec

Initial independent findings additionally covered dynamic endpoint-bound reconciliation and mutation of retained published waveform/spectrum collections. These were fixed with regression tests. Both reviewers independently identified the remaining fractional-bound issue; the parent fixed it, added rounding-in-both-directions regressions, and obtained explicit closure. Final spec follow-up reports **no remaining blocker** in that follow-up and does not close human gates.

| Distinct issue | Resolution/evidence |
| --- | --- |
| Endpoint peers advertise focus but cannot take it | Delegate to owner and activate requested endpoint; HWND-hosted real-peer focus/arrow test |
| Analyzer-only benchmark omits backend availability | Probe/report native availability before selecting benchmark sections |
| Shrinking/raising bounds leaves crossing endpoints | Dependency-ordered coercion and integer-bound provider regressions |
| Published waveform/spectrum views can change after later refresh/reset | Independent read-only views; retained-view, repeated-refresh, and reset regressions |
| Fractional bounds round values out of range or become unreachable | Central coercion retains exact endpoints and clamps after rounding; six parent RED/GREEN regressions |

Reviewer source inspection is separate from executed verification. The parent ran the final checks rather than accepting child completion statements alone.

## Final matched analyzer benchmark

### Method and environment

The parent created a temporary **detached** worktree at the original commit, copied only the final benchmark harness into it, and copied the same existing native DLL to both sides. The original app/DSP sources remained at the baseline commit in that worktree. Both Release builds succeeded. Restoration used **only the existing local NuGet cache** as a package source; no new dependency was added or installed. The main checkout was never temporarily reverted for measurement.

Native DLL SHA-256 on both sides:
`591f5993629002db1f893137440bbcb2eaba567532923194b9ffc773f5bd9a3c`.

Parameters: 48 kHz stereo, 4x true peak, native backend available, channel waveform/spectrum disabled, one warmup, three measured repeats. The harness constructs an analyzer and reads its snapshot after every PCM block. It reports elapsed processing time, throughput, and cumulative allocated managed bytes, including analyzer construction; allocated bytes are **not retained memory or peak RAM**. Signals are pre-generated outside the measurement. Reported deltas are computed from medians.

```powershell
dotnet build tests/DesktopLoudnessMeter.Benchmarks/DesktopLoudnessMeter.Benchmarks.csproj -c Release --no-restore
# Run the identical harness against baseline and final Release binaries:
dotnet <benchmark.dll> --analyzer-only --seconds=10,60,300 --block=480 --mode=normal,eco --warmup=1 --repeats=3
dotnet <benchmark.dll> --analyzer-only --seconds=60 --block=96 --mode=normal --warmup=1 --repeats=3
```

These measurements **supersede** the earlier worker's allocation improvement figures, which preceded the immutable waveform/spectrum publication fix.

| Audio / mode / block frames | Baseline median ms | Final median ms | Time delta | Baseline allocated B | Final allocated B | Allocation delta |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 10 s / Normal / 480 | 176.7 | 166.5 | -5.8% | 18,447,816 | 18,458,376 | +0.06% |
| 10 s / Eco / 480 | 122.1 | 114.1 | -6.6% | 18,386,216 | 16,131,208 | -12.3% |
| 60 s / Normal / 480 | 1,070.0 | 1,027.0 | -4.0% | 33,971,040 | 37,319,456 | **+9.9%** |
| 60 s / Eco / 480 | 739.1 | 675.4 | -8.6% | 33,909,440 | 23,617,696 | -30.4% |
| 300 s / Normal / 480 | 5,444.2 | 5,129.5 | -5.8% | 108,422,640 | 127,809,456 | **+17.9%** |
| 300 s / Eco / 480 | 3,775.9 | 3,419.5 | -9.4% | 108,361,040 | 59,497,792 | -45.1% |
| 60 s / Normal / 96 | 1,789.2 | 1,461.3 | -18.3% | 108,117,584 | 53,837,792 | -50.2% |

Raw elapsed repeats, before / after (ms):

| Scenario | Baseline repeats | Final repeats |
| --- | --- | --- |
| 10 s Normal 480 | 176.7, 179.2, 176.7 | 167.0, 165.1, 166.5 |
| 10 s Eco 480 | 121.4, 122.3, 122.1 | 114.1, 115.9, 111.7 |
| 60 s Normal 480 | 1049.9, 1082.4, 1070.0 | 1027.0, 1029.2, 1001.0 |
| 60 s Eco 480 | 739.1, 731.4, 740.0 | 684.5, 673.5, 675.4 |
| 300 s Normal 480 | 5405.3, 5444.2, 5595.3 | 4981.6, 5155.6, 5129.5 |
| 300 s Eco 480 | 3873.0, 3770.0, 3775.9 | 3522.0, 3376.4, 3419.5 |
| 60 s Normal 96 | 1789.2, 1761.4, 1853.0 | 1461.3, 1478.4, 1458.2 |

All scenarios reported `numeric-stable=True` **within their repeats**. Before/after printed numeric summaries agreed to four decimals (`M=-3.2554 S=-3.2550 I=-3.2550 TP=-0.8214 rms100=-7.1965 corr=0.1221`, 160 points). This is not a claim of bit-identical baseline-to-final output for every metric; the regression suite supplies the stronger behavioral checks.

Final true-peak microbenchmark: managed/native peaks printed identically at 2x `0.909116`, 4x `0.909763`, and 8x `0.909780`; 8x elapsed was `300.84 / 150.42 ms` for 30 s of stereo PCM. This microbenchmark is separate from the full-analyzer comparison above.

### Interpretation and remaining limits

- Exact gate caching eliminates repeated historical scans **between completed 100 ms gate blocks**. The block set still grows over the session and a new completed gate block still triggers the exact historical calculation. This is not bounded-memory or constant-time integrated loudness.
- Stereo point sampling reads ring slots directly and follows the display cadence. Numeric metrics remain current after each PCM block; graph data has its documented cadence.
- Immutable publication fixes a real cross-dispatcher retained-view hazard but has a cost: **Normal-mode cumulative allocations increase in longer 480-frame runs**, despite lower processing time. This is a deliberate within-scope correctness/performance tradeoff, not an across-the-board memory win. Eco and the smaller-block case still reduce allocations substantially. No old percentage is reused to conceal this cost.
- Single-machine synthetic throughput, not live end-to-end app CPU/latency profiling. No CPU affinity, randomized A/B order, multi-machine replication, multi-source run, channel-graph benchmark, or long-duration production capture was performed. Timing differences are measurements, not unit-test thresholds or portable guarantees.
- Very low positive sample rates retain a stable clamped filter design but are not claimed calibrated. Real sources must report the correct rate; no resampling is added.

## Evidence locations

Durable commands, final counts, raw elapsed repeats, numeric summaries, and tradeoffs are recorded above. Detailed run artifacts are outside the repository:
`C:/Users/linyudong/.pi/agent/sessions/--D--GitRepo-uMeter-main--/subagent-artifacts/outputs/9248d805-5cd0-493b-8b3a-bfdf1df777c8/`.

Relevant final files: `parent-final-build.txt`, `parent-final-tests.txt`, `parent-final-format.txt`, `parent-managed-analysis-tests.txt`, `parent-final-truepeak.txt`, `parent-baseline-480.txt`, `parent-final-480.txt`, `parent-baseline-96.txt`, `parent-final-96.txt`, `parent-openspec-validation.json`, `standards-final.ae50684a.md`, and `spec-final.7f312ac0.md`. The temporary benchmark worktree was removed after evidence collection; the original three worktrees are preserved.

## Open human / real-device gates

1. End-to-end Narrator navigation, endpoint announcements, editing, and client-side value-change delivery in real inline/popup workflows.
2. Visual focus, clipping, and legibility at 100%, 150%, and 200% display scaling and across mixed-DPI monitors.
3. Real Windows high-contrast appearance, theme refresh, and language switching.
4. Live WASAPI and DAW sender smoke checks at 44.1/48/96 kHz, with multiple sources and collision/Signal Shuffle consumers.

**Engineering complete, awaiting human/real-device acceptance.** These gates are not checked off by source review, headless rendering, provider calls, or synthetic benchmarks.
