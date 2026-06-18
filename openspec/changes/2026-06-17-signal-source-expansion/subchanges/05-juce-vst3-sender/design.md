# JUCE VST3 Sender Design

## Overview

This slice turns the sender prototype into a real VST3 plugin. The plugin stays intentionally thin: JUCE owns plugin format glue, `PluginProcessor` owns host lifecycle and audio pass-through behavior, and a small native sender core owns queueing, protocol encoding, heartbeat, and UDP I/O.

The existing .NET `DesktopLoudnessMeter.SenderKit` cannot be loaded directly inside a native VST3 plugin without adding a runtime bridge, so this pass mirrors its public behavior in C++ rather than sharing binaries.

## Key Decisions

- Build only `VST3` in this pass. AAX remains a separate SDK/licensing/validation slice.
- Use CMake with `JUCE_DIR` defaulting to `C:/JUCE`, matching the user's local JUCE install.
- Keep the plugin editorless for the first pass so DAW compatibility and signal routing are the focus.
- Keep `processBlock` pass-through and bounded: copy interleaved samples into preallocated packet slots, then return.
- Encode v1 JSON/base64 packets on the worker thread for app compatibility.
- Persist the source handle in XML plugin state; generate a fresh handle when no valid saved handle exists.

## Implementation Units

### U1: CMake VST3 Project
Module Boundary:
- `src/DesktopLoudnessMeter.JucePlugin/CMakeLists.txt`
- `src/DesktopLoudnessMeter.JucePlugin/Source/PluginProcessor.h`
- `src/DesktopLoudnessMeter.JucePlugin/Source/PluginProcessor.cpp`

Responsibilities:
- Define a JUCE plugin target that builds VST3.
- Configure plugin metadata, bus support, editorless behavior, and pass-through processing.
- Bridge host lifecycle calls to the native loopback sender.

### U2: Native Loopback Sender Core
Module Boundary:
- `src/DesktopLoudnessMeter.JucePlugin/Source/LoopbackProtocol.h`
- `src/DesktopLoudnessMeter.JucePlugin/Source/LoopbackProtocol.cpp`
- `src/DesktopLoudnessMeter.JucePlugin/Source/RealtimeAudioQueue.h`
- `src/DesktopLoudnessMeter.JucePlugin/Source/RealtimeAudioQueue.cpp`
- `src/DesktopLoudnessMeter.JucePlugin/Source/LoopbackSender.h`
- `src/DesktopLoudnessMeter.JucePlugin/Source/LoopbackSender.cpp`
- `src/DesktopLoudnessMeter.JucePlugin/Source/PluginState.h`
- `src/DesktopLoudnessMeter.JucePlugin/Source/PluginState.cpp`

Responsibilities:
- Validate handles and encode register, heartbeat, rename, audio, and close packets compatible with `DesktopLoudnessMeter.Protocol`.
- Provide a bounded single-producer/single-consumer queue with preallocated packet storage.
- Run a JUCE worker thread that owns protocol encoding and UDP sends.
- Persist and restore plugin state XML without changing the protocol handle.

### U3: Native Tests
Module Boundary:
- `src/DesktopLoudnessMeter.JucePlugin/Tests/CMakeLists.txt`
- `src/DesktopLoudnessMeter.JucePlugin/Tests/LoopbackProtocolTests.cpp`
- `src/DesktopLoudnessMeter.JucePlugin/Tests/RealtimeAudioQueueTests.cpp`

Responsibilities:
- Cover protocol packet shapes and sample byte ordering.
- Cover queue capacity, oversize rejection, interleaving, and drop counting.
- Run through CTest when a local C++ toolchain is available.

### U4: VST3 User Documentation
Module Boundary:
- `docs/juce-vst3-sender.md`
- `docs/plugin-sender-prototype.md`

Responsibilities:
- Document build/install/smoke commands.
- Explain the relationship between the VST3 sender and the earlier sender probe.
- Make the AAX boundary explicit.

## Traceability

- R1: U1
- R2: U1
- R3: U1, U2
- R4: U2, U3
- R5: U2, U3
- R6: U4

## Risks

- JSON/base64 UDP is intentionally compatible but not ideal for high channel counts or large blocks. This plugin limits packet size and drops rather than blocking.
- JUCE VST3 builds depend on the local Visual Studio/CMake setup. If the toolchain is unavailable, this slice can still land source and docs but must record the build blocker.
- Some hosts may call `prepareToPlay` and `releaseResources` frequently. Sender lifecycle methods must be idempotent and cheap.
