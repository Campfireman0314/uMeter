# JUCE VST3 Sender Proposal

## Goal

Add the first real DAW plugin sender: a native JUCE VST3 audio-effect plugin that passes audio through unchanged while streaming bounded PCM packets to the desktop app through the existing localhost loopback protocol.

## In Scope

- A CMake-based JUCE plugin project under `src/DesktopLoudnessMeter.JucePlugin`.
- VST3 output using the local JUCE install at `C:\JUCE`.
- A native C++ sender core that mirrors the existing sender-kit boundary: audio callback -> bounded queue -> worker thread -> localhost UDP protocol.
- Stable plugin instance handles persisted in plugin state separately from display names.
- Minimal docs and smoke steps for building, installing, and validating the plugin in a VST3 host.

## Out of Scope

- AAX output, signing, validation, or installer packaging.
- Custom plugin UI or source renaming controls.
- Binary loopback transport optimization.
- Changes to the WPF app's analysis pipeline beyond consuming the existing plugin-kind protocol packets.

## Depends On

- `03-plugin-sender-prototypes`, which established the v1 loopback protocol and receiver-compatible sender behavior.
- Local JUCE at `C:\JUCE`.
- A Windows C++ toolchain discoverable by CMake.

## Done Means

- The VST3 project configures with CMake and builds when the local C++ toolchain is available.
- The plugin registers as a `plugin` source, sends heartbeat/audio/close packets, and leaves audio samples unchanged.
- Queue/protocol behavior is covered by native tests or a documented toolchain blocker.
- Docs explain how to build, where to find/copy the VST3, and how to smoke-test it with the desktop app.
