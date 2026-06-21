# Platform Strategy

This note separates reusable product concepts from Windows-specific implementation before uMeter expands beyond the current Windows desktop app and VST3 sender.

## Reusable Product Contracts

### Source Identity

- Every source needs a stable machine-readable key and a mutable display name.
- User aliases and per-source workspace settings should attach to the stable key, not the display name.
- Sender-style sources should own their handle and keep it stable across host reloads when the host state permits.
- Same-name sources must remain distinguishable through source kind, host/process context, or short identity suffixes.

Current Windows/VST3 examples:

- WASAPI: source keys are derived from session/process identity where Windows exposes enough metadata.
- Plugin/loopback sender: source key is `loopback:<sender-handle>`.

### Metering Semantics

- Peak, true peak, loudness, waveform, spectrum, stereo image, and collision analysis should keep the same user meaning across platforms.
- Analysis values should be transport-neutral. Platform clients may use different capture APIs, but the app should not rename measurements to match API quirks.
- Display transforms are separate from analysis data. For example, spectrograph soft floor and smoothing are rendering decisions, not stored spectrum data.

### Sender Protocol

- The current v1 loopback protocol is inspectable JSON over UDP with base64 float32 audio and receiver `ack` feedback.
- `register`, `heartbeat`, `rename`, `audio`, `close`, and `ack` are the minimum sender lifecycle vocabulary.
- Future binary transport should preserve the same lifecycle semantics before optimizing packet shape.
- Protocol compatibility should be versioned explicitly rather than inferred from app/plugin version numbers.

### Theme And Graph Roles

- Future clients should use semantic roles, not copied color literals.
- Current shared graph roles are track, outline, subtle, signal, spectrum warm, text, and control pressed.
- New platform UIs may change rendering technology, but waveform, spectrum, stereo image, and collision views should preserve the same visual hierarchy: calm track, subtle scale, clear signal, and restrained warning/accent use.

### Events

- Events should remain neutral hints. Prefer names such as `Possible overlap` over judgmental wording.
- Event retention and export behavior should be bounded and user-configurable.
- Detectors should be conservative until backed by fixture tests or reference material.

## Platform-Specific Boundaries

### Windows Desktop App

- Owns WASAPI session enumeration, process-loopback capture, WPF windows, tray behavior, installer, and VST3 installation workflow.
- Remains the reference receiver while sender protocol and graph semantics stabilize.
- Should keep native FFT optional and preserve managed fallback.

### VST3 Sender

- Current primary plugin sender path.
- Shares the sender lifecycle model with future plugin formats.
- Should stay small: source naming, mute/pass-through, receiver status, and diagnostics.
- More advanced sender UI should be justified by DAW workflow value, not by available space in the editor.

### AAX Sender

- The repo has CMake groundwork, but distributable AAX remains blocked on Avid SDK access, PACE/signing, validation, and Pro Tools smoke testing.
- Treat AAX as an integration boundary until those external requirements are present.
- The first AAX success criterion is parity with VST3 sender lifecycle packets, not new metering features.

### macOS And AU

- AU should reuse the sender identity and lifecycle contract if a macOS receiver exists.
- macOS system-audio capture has different permission and routing constraints than Windows WASAPI, so it needs a separate feasibility pass.
- A macOS app should not be started as a direct WPF port. The reusable pieces are protocol, source model, metering semantics, and theme roles.

### iOS

- iOS should be treated as a separate product shape until its audio-routing role is decided.
- Likely viable roles are companion display, remote session monitor, or controlled sender endpoint.
- A full iOS system-audio meter should not be assumed feasible without platform API research.

## Recommended Expansion Order

1. Stabilize Windows receiver, VST3 sender, source identity, event language, and rendering contracts.
2. Add deterministic metering fixtures so platform ports can prove measurement parity.
3. Improve sender diagnostics and reconnect behavior in VST3.
4. Revisit binary protocol only after the JSON lifecycle semantics stop changing.
5. Pursue AAX when SDK/signing/validation prerequisites are available.
6. Explore macOS/AU as the first broader platform direction if there is a clear receiver story.
7. Define iOS as a companion/display product before attempting capture or sender work.

## Open Questions

- Should the first non-Windows app be a receiver, a sender, or a companion display?
- Do source workspace settings need a cross-platform file schema before macOS/iOS work begins?
- Which measurements must be fixture-proven before claiming platform parity?
- Should event export use JSON Lines, plain JSON, or a richer session bundle format?
