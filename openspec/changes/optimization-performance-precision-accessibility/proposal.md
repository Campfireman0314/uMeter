# uMeter performance, precision, and accessibility pass

## Problem Statement

uMeter should remain responsive during long monitoring sessions, provide consistent loudness readings for the sample rates used by desktop and plugin sources, and let keyboard and assistive-technology users operate its measurement controls. The current implementation uses fixed 48 kHz K-weighting coefficients for every input rate, repeats integrated-gate calculations even without a new gate block, and has incomplete keyboard, focus, and UI Automation support in its custom sliders.

## Solution

Improve the existing WPF desktop application and shared PCM analyzer without redesigning the product, changing its transport, or trading measurement accuracy for speed. Deliver rate-correct loudness filtering, measured reductions in redundant analysis/display work, and accessible controls in both inline and popup detail views. Preserve the current native acceleration with managed fallback.

## User Stories

1. As a plugin-source user, I want 44.1 kHz audio measured with the correct K-weighting so that changing my DAW sample rate does not silently change the meter's calibration.
2. As a high-sample-rate user, I want 96 kHz audio measured correctly so that the input format is respected.
3. As a desktop-source user, I want the established 48 kHz behavior preserved so that this pass does not shift existing readings unexpectedly.
4. As a meter user, I want independent numerical reference checks so that two implementations agreeing is not mistaken for proof of accuracy.
5. As a meter user, I want silence, startup windows, reset, gating, and Mid/Side behavior preserved so that unavailable and silent values retain their meaning.
6. As a long-session user, I want the analyzer to avoid recalculating unchanged integrated loudness so that repeated queries do not waste CPU.
7. As a user with several sources, I want unnecessary display-data scans and allocations reduced so that metering remains responsive.
8. As an Eco-mode user, I want display-cache work to respect the existing performance cadence without reducing numeric measurement fidelity.
9. As a collision-analyzer user, I want source spectra to remain available even when a spectrograph is hidden.
10. As a Signal Shuffle user, I want spectrum-consuming features to remain functional after optimization.
11. As a keyboard user, I want to select and adjust both endpoints of a range slider, including direct numeric entry, without using a mouse.
12. As a keyboard user, I want clearly visible focus on custom sliders and themed interactive controls so that I know where input will go.
13. As a screen-reader user, I want sliders to expose meaningful names, values, bounds, enabled state, and value-change notifications through Windows UI Automation.
14. As a screen-reader user, I want icon-only commands and compact metric selectors to have descriptive accessible names rather than glyphs.
15. As an inline or popup detail user, I want the same accessible behavior and labels in both surfaces.
16. As a user who changes language or theme, I want accessible names and focus visuals to remain consistent with the displayed interface.
17. As a maintainer, I want reproducible full-analyzer throughput and allocation measurements so that optimization claims can be checked.
18. As a maintainer, I want explicit remaining real-device checks so that automated tests are not mistaken for complete Narrator or visual acceptance.

## Implementation Decisions

- Retain WPF/.NET 8, public snapshot field semantics, source identity, preference storage, and native/managed fallback behavior.
- Derive K-weighting coefficients from the input rate using a documented standards-based design; retain the established 48 kHz coefficient set where needed for compatibility. Common 44.1/48/96 kHz rates are the acceptance targets. Do not narrow the existing positive-sample-rate API without evidence and approval.
- Use an independent published reference, precomputed literals, or analytical reference for numerical expected values. Never calculate expected values using the production filter under test. This work does not claim ITU/EBU certification.
- Keep integrated loudness exact. Cache only when the retained block set is unchanged; invalidate on a new block and reset. Do not replace exact gating with a quantized histogram or bounded rolling approximation.
- Reduce redundant display-data rebuilds and allocation at existing analyzer/performance seams. Preserve spectrum availability for collision analysis and Signal Shuffle. Broad visibility-demand plumbing is not required; hidden WPF render gates remain in place.
- Keep numeric measurements current after PCM processing; display caching must not freeze subsequent updates, mutate a consumer's published view unexpectedly, or retain stale data after reset/performance-mode changes.
- Extend the existing benchmark executable without adding dependencies; identify native availability, sample rate, duration, block size, performance mode, warmup, repeats, elapsed time, and allocations. Compare like-for-like runs before and after the optimization; timing is evidence rather than a flaky unit-test threshold.
- Range-slider keyboard interaction uses an identifiable active endpoint, arrows, Home/End, endpoint switching, and Enter/F2 numeric entry; Escape cancels editing and Tab navigation can leave the control. Preserve mouse behavior and the minimum endpoint separation.
- Expose one range-value UI Automation provider per logical slider endpoint. The editable single-value slider exposes the range-value pattern. Providers enforce disabled/read-only/bounds behavior and publish value changes.
- Add explicit focus indication through existing themed resources, including a system high-contrast focus fallback where applicable. Improve accessible labels for core chrome actions, diagram settings, and compact selectors symmetrically in inline and popup views. No whole-app visual redesign.
- Use one integration branch and one implementation writer at a time because the checkout contains user-owned untracked material. Parent owns this carrier; children must not modify its status. Read-only standards and spec reviews are independent.

## Testing Decisions

- Agreed numerical seam: public PCM analyzer input and snapshot outputs, preserving existing loudness, true-peak, channel, and reset contracts. Reference tests cover common rates, multiple frequencies, block partitioning, and independent expected levels with a stated tolerance.
- Agreed cache seam: observable analysis outputs and published display-data behavior across unchanged input, new samples, performance-mode changes, and reset. Gate-specific exact worked examples can use the existing internal-test visibility where the public analyzer cannot isolate a gate boundary.
- Agreed accessibility seam: actual WPF controls on an STA thread, routed keyboard input, and real UI Automation peers/provider interfaces. Exercise value changes, bounds, endpoint selection, disabled controls, edit commit/cancel, and binding preservation. Use existing WPF test conventions rather than a new test dependency.
- Test accessible naming and themed focus contracts in both real XAML surfaces; distinguish static resource checks from live control/provider tests.
- Run targeted tests after each vertical slice, then the full managed solution build/test and formatting verification serially. Do not run WPF builds, tests, and formatting concurrently against shared obj folders.
- Run full-analyzer benchmarks with matched parameters before and after performance changes and record the actual numbers, environment, and limitations.
- Independent standards and spec reviews cover the baseline-to-final diff. Resolve material findings within scope and rerun affected checks.
- Leave real Narrator end-to-end usage, mixed-DPI/zoom visual inspection, Windows high-contrast appearance, and live DAW/WASAPI smoke tests open until actually observed. Browser automation is not a substitute for native WPF accessibility checks.

## Out of Scope

WebView migration, binary transport, UDP/schema changes, plugin-editor redesign, installer or release-version changes, dependency installation, global configuration, certification claims, signing, remote tracker publication, push, PR, merge to main, deployment, and unrelated refactors. Do not modify or include the pre-existing untracked .claude, .codex, output, or tmp contents.

## Further Notes

- Authorization: user requested an all-round spec plan and completion after selecting pi-subagents. This covers this local spec, scoped implementation, tests, reviews, reversible task decomposition, and local delivery. No external write is authorized.
- Baseline: main at 396774a56f086a169059035cada64f1dd2d52478. No tracked changes; pre-existing untracked directories are preserved. Integration branch: optimization/performance-precision-accessibility.
- Observed baseline: dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --no-restore -v minimal passed 396/396, zero skipped, on Windows with .NET SDK 11.0.100 targeting .NET 8.
- Engineering acceptance and human/real-device acceptance are separate. Current delivery state lives in tasks.md and test-report.md in this change, not in a second progress ledger.
