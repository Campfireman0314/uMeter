# Performance follow-up and spectrum cursor — verification

## Baseline, scope, and delivery

- Follow-up baseline: `35f713f1c4139be3f9b449a932da16f4192f6c17` on `optimization/performance-precision-accessibility`.
- Scope: analyzer window/gate storage, dependency-free benchmark configuration, shared spectrum history/cursor control, targeted tests, metering/rendering notes, and this OpenSpec change. No transport, public snapshot schema, plugin, installer, dependency, global configuration, or remote publication changes.
- Existing user untracked `.claude/`, `.codex/`, `output/`, and `tmp/` remain excluded. Prior delivery and its human acceptance gates remain intact.
- Implementation, review remediation, and final automated verification are complete. Local commit is the final delivery action; human visual/live-device acceptance remains open.

## Final observed verification

Environment: Windows `10.0.26300`, x64, .NET SDK `11.0.100`, `.NET 8.0.34` runtime, 24 logical CPUs. Build/test/format checks were serial. Parent shell pipelines used `set -euo pipefail`, preserving the tested command's exit.

| Check | Observed result |
| --- | --- |
| Worker baseline full test project before production edits | exit 0; 492 passed, 0 failed/skipped |
| Parent `dotnet build desktop-loudness-meter.sln --no-restore -v minimal` | exit 0; **0 warnings, 0 errors** |
| Parent `dotnet test desktop-loudness-meter.sln --no-restore -v minimal --blame-hang --blame-hang-timeout 30s --results-directory <run-artifacts>/parent-tests` | exit 0; **557 passed, 0 failed, 0 skipped**, about 4 s |
| Parent `dotnet format desktop-loudness-meter.sln --no-restore --verify-no-changes --include <all changed/new src and tests paths> --verbosity minimal` | exit 0; no formatting change required |
| Parent `git diff --check` | exit 0; no whitespace errors; Git LF/CRLF notices are informational |
| Installed OpenSpec entrypoint: `node <installed-openspec>/bin/openspec.js validate performance-followup-spectrum-level --strict --no-interactive --json` | exit 0; valid, zero issues |
| Parent copied final test output, removed only its copied native DLL, and ran `dotnet vstest <copy>/DesktopLoudnessMeter.Tests.dll --TestCaseFilter:FullyQualifiedName~Audio.Analysis` | exit 0; **98 passed**, 0 failed/skipped |
| Parent exact gate equality/regression class | exit 0; 10 passed |
| Parent rebuilt benchmark, then `--analyzer-only --seconds=0.001,0.002 --mode=normal --warmup=0 --repeats=1` | exit 0; empty spectrum reported as `n0/empty` rather than indexing it |

Existing optional-native tests return early when no native DLL is present. Therefore the fallback aggregate count does not prove native comparison code ran in that copy. The numerical analysis fixtures ran via managed fallback; the ordinary full suite had native acceleration available.

### Recovery and review remediation

The initial delegated model override was rejected before tool execution: workflow `fe236610`, child `bc17c5fa`, HTTP400 `model_not_found`, unknown provider for `gpt-5.5`. Parent verified no tracked/source/test changes, preserved the spec, and made one retry through the same subagent protocol using the configured worker model. Recovery workflow `02deeb7f` completed; no provider/global settings or execution protocol were changed.

The parent independently confirmed and addressed these review points:

| Axis | Finding | Disposition |
| --- | --- | --- |
| Standards, P2 | Retained-memory projection incorrectly treated measured bytes per completed block as bytes per distinct index node | Corrected to a runtime-dependent source-layout estimate of approximately 72 B/distinct node, with signal-dependent measured totals separate |
| Spec, P3 note | Benchmark signature formatter indexes empty non-null series | Reproduced at 0.002 s, observed `ArgumentOutOfRangeException` and exit 127; added empty-series guard, rebuilt, both 0.001/0.002 s complete with exit 0 |
| Spec, coverage note | No explicit exact relative-threshold equality fixture | Added `[0.1, 1.9]` fixture: independent preliminary mean 1, threshold exactly 0.1, one survivor, expected 2.096536009528289 LUFS; added duplicate-key equality/BitDecrement query fixture |

The predicted 0.001 s native crash did **not** reproduce on this machine because native reconstruction startup left only 60 display samples. At 0.002 s, 96 raw frames produced 252 display samples, exposing an empty FFT view. This refined trigger, not the original prediction, is the observed reproduction.

### Standards

Initial full follow-up review accepted fixed storage/arithmetic order, exact-value AVL query structure, cache/reset semantics, immutable publication, and raw historical cursor wiring. The retained-memory finding is explicitly closed by the read-only follow-up review. No outstanding material finding in the reviewed remediation seam; earlier accepted conclusions remain unchanged.

### Spec

Initial full follow-up review found no blocker for representative workloads and noted the short-run reporting/threshold fixture gaps. The follow-up explicitly closes both notes, verifies the corrected memory qualification, and identifies no new material issue in that seam. Reviewer conclusions are source inspection, not independent executed tests; the parent ran final commands and measurements below. Human acceptance is not inferred from review.

## What is implemented and exercised

### Analyzer performance and fidelity

- Sliding power/peak windows retain fixed arrays rather than growing queues past capacity. Power arithmetic remains add-new then subtract-evicted, preserving prior chronological rounding. Peak queries avoid enumerator allocations.
- Integrated loudness retains exact measured powers in a deterministic AVL aggregate index with duplicate counts and subtree sums. Queries are O(log distinct powers), rather than a full historical scan after each completed block. Chronological absolute-gated sum/count preserves relative-threshold calculation; returned gated sums may reorder within the stated 1e-10 LU budget.
- Tests use a separate naive two-pass oracle, varied/ordered/duplicate histories, absolute equality/adjacent powers, explicit relative equality, reset/cache, allocation behavior, and public analyzer streaming/Mid/Side cases. No quantization, history truncation, resolution/cadence reduction, or mutable published-buffer reuse was introduced.

### Spectrum cursor

- The shared `SpectrumRollView` now draws frequency plus one-decimal FFT-derived dBFS from the nearest retained source bucket at the hovered history column, for example `1 kHz · -24.3 dBFS`.
- Tests distinguish a bucket's own level from a stronger neighbor combined into its rendered row. Numeric lookup is independent of color, theme and display floor/ceiling.
- Empty/non-finite data is unavailable (`—`); stored normalized 0 means at/below -200 dBFS (`≤ -200 dBFS`), and 1 means at/above 0 dBFS (`≥ 0 dBFS`), not fabricated unclamped values.
- Partial right alignment, full/wrapped history, oldest/newest columns, the shared DPI-snapped seam at 96/144/192 DPI, effective viewport mapping, clear/source-size changes, color invariance and real STA-control deferred-update/render paths are exercised.
- The same control remains used in MainWindow and DetailWindow overall/L/R plots. Pointer movement stores position and invalidates drawing; lookup reads one retained column/bucket without FFT or audio processing. Narrow labels can stack and are clipped/placed inside the plot.
- It is an FFT bucket estimate, **not an exact arbitrary-frequency calibration, true-peak, RMS, or compliance reading**. The existing displayed 20 Hz–22 kHz domain is unchanged; the analyzer's original `min(22000 Hz, 0.49 * sample rate)` bucket endpoint policy also remains unchanged. Do not interpret this change as correcting that pre-existing low-sample-rate display-domain approximation.

## Parent matched benchmarks

### Method

Parent created a task-owned temporary detached worktree at `35f713f`, copied only the final benchmark harness and the existing native DLL into it, restored exclusively from the existing local NuGet cache, and built Release. No main-checkout source was temporarily reverted. Baseline/final use the same native DLL SHA-256:
`591f5993629002db1f893137440bbcb2eaba567532923194b9ffc773f5bd9a3c`.

Each analyzer scenario ran in a **fresh process**, baseline then final, with three warmups and three measured repeats. Same 48 kHz stereo signal, 4x true peak, 480-frame blocks, native backend, selected mode/source/channel flags. Channel-enabled scenarios use three independent analyzers fed identical PCM in one sequential loop; this is not a live concurrent capture benchmark. Time and allocations include analyzer construction but exclude signal generation and signature formatting. Allocations are cumulative managed bytes, not peak/retained RAM. Throughput counts frames across all analyzers.

```powershell
dotnet <benchmark.dll> --analyzer-only --no-gate --seconds=<10|60|300> --mode=<normal|eco> --block=480 --warmup=3 --repeats=3
dotnet <benchmark.dll> --analyzer-only --no-gate --seconds=10 --mode=<normal|eco> --block=480 --sources=3 --channel-waveform=true --channel-spectrum=true --warmup=3 --repeats=3
dotnet <benchmark.dll> --gate-only --gate-blocks=1000,5000,30000 --warmup=3 --repeats=3
```

### Full-analyzer medians

| Scenario | Baseline ms | Final ms | Time delta | Baseline allocated B | Final allocated B | Allocation delta |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 10 s Normal | 171.7 | 155.5 | -9.4% | 18,458,440 | 9,329,928 | -49.5% |
| 10 s Eco | 117.9 | 107.8 | -8.6% | 16,131,272 | 7,002,760 | -56.6% |
| 60 s Normal | 1,043.4 | 923.8 | -11.5% | 37,319,520 | 27,855,784 | -25.4% |
| 60 s Eco | 688.8 | 599.5 | -13.0% | 23,617,760 | 14,154,024 | -40.1% |
| 300 s Normal | 5,043.3 | 4,839.4 | -4.0% | 127,809,520 | 116,796,512 | -8.6% |
| 300 s Eco | 3,456.7 | 3,029.4 | -12.4% | 59,497,856 | 48,484,848 | -18.5% |
| 10 s Normal, 3 sources + channel graphs | 977.4 | 945.8 | -3.2% | 69,984,736 | 42,594,032 | -39.1% |
| 10 s Eco, 3 sources + channel graphs | 501.2 | 459.7 | -8.3% | 52,776,800 | 25,391,408 | -51.9% |

Elapsed repeats (ms), retained to show scatter:

| Scenario | Baseline repeats | Final repeats |
| --- | --- | --- |
| 10 s Normal | 171.7, 171.7, 167.8 | 155.3, 155.8, 155.5 |
| 10 s Eco | 117.9, 115.9, 118.8 | 106.9, 107.8, 108.2 |
| 60 s Normal | 1043.4, 1022.5, 1049.4 | 923.0, 933.3, 923.8 |
| 60 s Eco | 681.3, 743.1, 688.8 | 596.9, 600.4, 599.5 |
| 300 s Normal | 5072.7, 5006.6, 5043.3 | 4656.5, 4839.4, 4961.2 |
| 300 s Eco | 3491.8, 3456.7, 3453.5 | 3029.4, 3004.7, 3080.4 |
| Channel-3 Normal | 977.4, 981.2, 976.4 | 949.0, 945.8, 945.4 |
| Channel-3 Eco | 501.2, 498.2, 502.1 | 460.3, 459.7, 456.0 |

Every scenario printed numeric-stable=True within its repeats. Parent compared all printed per-source signatures after excluding the intentionally budgeted integrated field: other numeric fields and the count/sum/first/last display fingerprints match exactly. This is **not a full-array byte comparison**. Maximum integrated difference in these printed signatures was `3.9968028886505635e-15 LU`, well inside 1e-10 LU.

### Isolated long-history gate

This drives four frames per block/hop and queries every completed block, avoiding hours of PCM generation. Its deterministic workload deliberately rounds input powers to six decimals and repeats values to exercise duplicates; production measured powers are not quantized.

| Completed blocks | Baseline us/block | Final us/block | Baseline retained B | Final retained B |
| ---: | ---: | ---: | ---: | ---: |
| 1,000 | 3.168 | 0.477 | 8,504 | 31,208 |
| 5,000 | 2.669 | 0.571 | 65,848 | 183,896 |
| 30,000 | 18.897 | 0.735 | 262,456 | 1,216,592 |

At 30,000 blocks, median total time was **566.9 ms → 22.0 ms**, approximately **25.8x faster for this isolated workload**, not for the entire application. Baseline repeats 567.5/566.3/566.9 ms; final 22.1/22.0/17.7 ms. At 5,000 blocks an outlying baseline repeat was 27.7 ms; the other two were 13.3/13.2 ms. All repeats are preserved in raw logs.

Checksums are not bit-identical across implementations: 30k baseline `-172566.76724169723`, final `-172566.767241697`. This is a cumulative sum over 30,000 readings, distinct from the per-result 1e-10 LU comparison. Earlier worker statements of identical checksums are superseded by these full-precision observations.

## Memory and measurement limitations

1. **Index retention increases.** One node per distinct measured power; O(distinct powers), not bounded session memory. Source layout implies approximately 72 B/node on x64 .NET (runtime-dependent object overhead/alignment). Ten mostly-distinct blocks/s imply approximately 43.2 KB/minute or 2.59 MB/hour **per gate for nodes alone**. Overall/Mid/Side are separate gates; repeated powers share nodes. The mixed rounded benchmark totals cannot establish cost per distinct power. The understated earlier 40 B/node/1.4 MB/hour projection was corrected and reviewed.
2. **Cursor history is bounded but can be larger at typical sizes.** It replaces the old row-level array, not adds a second history. Maximum raw level storage is 480 x 320 x 8 = 1,228,800 B (1.17 MiB), matching the former worst-case level-array ceiling. At the representative 208-column/144-row, 320-bucket geometry, level data rises from 239,616 B to 532,480 B: +292,864 B (286 KiB) per control. This is preallocated retention, not a per-pointer allocation. Pixel/other control storage is additional. Analyzer benchmark savings do not include this WPF retention cost.
3. **Timing is not a portable guarantee.** The worker reported approximately 2x timing changes after sustained in-process workload and could not establish a cause. Parent used separate processes per scenario and does not reproduce or attribute that claimed transition as a confirmed thermal/JIT defect. Worker slow runs are retained in artifacts, not silently discarded; the final table is the explicitly stated new protocol. No affinity, randomized A/B order, multi-machine replication, live capture or whole-UI CPU profiling was performed. Small timing differences should be read with the repeats/noise, not as test thresholds.
4. **Numerical budget is explicit.** Indexed aggregation reorders additions; membership uses exact stored keys and strict comparisons. No claim of bit-identical integrated output for every possible history or ITU/EBU certification.
5. **Automated cursor tests are not visual acceptance.** Mapper and real STA-control tests establish computation/wiring. Actual mouse-wheel/pan feel, narrow-label readability, mixed-DPI appearance and live Narrator/DAW behavior remain unobserved.

## Evidence and cleanup

Detailed final logs are outside the repo at:
`C:/Users/linyudong/.pi/agent/sessions/--D--GitRepo-uMeter-main--/subagent-artifacts/outputs/02deeb7f-b163-4cfd-a91f-b8478e0c9bef/`.

Files: `parent-final-build.txt`, `parent-final-tests.txt`, `parent-final-format.txt`, `parent-openspec-validation.json`, `parent-managed-analysis-tests.txt`, `parent-gate-regressions.txt`, `parent-short-benchmark.txt`, `parent-before/after-{10,60,300}-{normal,eco}.txt`, `parent-before/after-channel3-{normal,eco}.txt`, `parent-before/after-gate.txt`, `standards-review.a87c5502.md`, and `spec-review.3438c98f.md`. Worker raw logs remain in the reported sibling `scratch/` artifact directory; they are not the final timing table. Durable summary, raw elapsed repeats, parameters and limitations are in this report so a temporary path alone is not the evidence.

The task-owned temporary baseline worktree was removed after capture. The original three worktrees and user files are preserved, as confirmed by the final worktree/status inspection.

## Acceptance still open

- Human visual check of the frequency+dB label at narrow sizes, pan/zoom, and mixed-DPI monitors.
- Prior Narrator/high-contrast/live WASAPI/DAW acceptance gates remain open in the preceding change; this follow-up does not close them.

Engineering implementation/verification is complete, awaiting local commit recording and human/real-device acceptance. No push, PR, merge, installer build, or release was performed.
