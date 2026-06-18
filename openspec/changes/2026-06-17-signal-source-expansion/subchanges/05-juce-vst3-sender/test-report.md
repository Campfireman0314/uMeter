# Test Report: 2026-06-17-signal-source-expansion/05-juce-vst3-sender

This report will be updated task by task during implementation.

## Task 1: Add JUCE VST3 project scaffold

### Direct Implementation
- **Timestamp:** 2026-06-18 22:22
- **Validation:**
  - Deferred full CMake configure/build until sender source and tests are added.
- **Change Summary:**
  - `src/DesktopLoudnessMeter.JucePlugin/CMakeLists.txt` - added - Defines the JUCE VST3 target with a configurable `JUCE_DIR`.
  - `src/DesktopLoudnessMeter.JucePlugin/Source/PluginProcessor.h` - added - Declares an editorless pass-through processor.
  - `src/DesktopLoudnessMeter.JucePlugin/Source/PluginProcessor.cpp` - added - Implements mono/stereo layout checks, pass-through processing, and plugin metadata plumbing.

## Task 2: Implement native loopback protocol and queue

### Red Phase
- **Timestamp:** 2026-06-18 22:24
- **Failing Tests:**
  - `DesktopLoudnessMeterJucePluginTests` - `C1083`: `LoopbackProtocol.h` and `RealtimeAudioQueue.h` are missing (product code missing).
- **False Red Fixes:** none

### Green Phase
- **Timestamp:** 2026-06-18 22:30
- **Passing Tests:**
  - `DesktopLoudnessMeterJucePluginTests` - pass through CTest
- **Change Summary:**
  - `src/DesktopLoudnessMeter.JucePlugin/Source/LoopbackProtocol.h` - added - Declares the native v1 protocol helper.
  - `src/DesktopLoudnessMeter.JucePlugin/Source/LoopbackProtocol.cpp` - added - Encodes plugin register, heartbeat, rename, audio, and close packets.
  - `src/DesktopLoudnessMeter.JucePlugin/Source/RealtimeAudioQueue.h` - added - Declares the bounded preallocated audio packet queue.
  - `src/DesktopLoudnessMeter.JucePlugin/Source/RealtimeAudioQueue.cpp` - added - Copies host buffers into interleaved packet slots and tracks drops.
  - `src/DesktopLoudnessMeter.JucePlugin/Tests/CMakeLists.txt` - added - Builds the native CTest executable.
  - `src/DesktopLoudnessMeter.JucePlugin/Tests/TestMain.cpp` - added - Runs the native test groups.
  - `src/DesktopLoudnessMeter.JucePlugin/Tests/LoopbackProtocolTests.cpp` - added - Covers handle generation and v1 packet shapes.
  - `src/DesktopLoudnessMeter.JucePlugin/Tests/RealtimeAudioQueueTests.cpp` - added - Covers interleaving, bounded capacity, and oversize rejection.
  - `src/DesktopLoudnessMeter.JucePlugin/CMakeLists.txt` - modified - Adds native tests and protocol source to the JUCE project.
- **Test Change Record:** No test modifications.
- **Acceptance Criteria Coverage:**

| Acceptance Criterion | Test Case | Status |
|----------------------|-----------|--------|
| New handles are valid for the v1 loopback protocol. | `generatedHandlesAreProtocolSafe` | pass |
| Register, heartbeat, rename, audio, and close packets match the v1 JSON shape. | `registerHeartbeatRenameAndCloseUseV1Shape` | pass |
| Audio packets encode little-endian float32 samples as base64. | `audioUsesLittleEndianFloatBase64` | pass |
| Queue storage is bounded and preallocated. | `audioBuffersAreCopiedAsInterleavedPackets` | pass |
| Full or oversized queue submissions fail without blocking and update dropped-packet count. | `fullQueuesDropInsteadOfBlocking`, `oversizedBlocksAreRejected` | pass |

### Refactor Phase
- **Timestamp:** 2026-06-18 22:31
- **Passing Tests:**
  - `DesktopLoudnessMeterJucePluginTests` - pass through CTest
- **Change Summary:**
  - `src/DesktopLoudnessMeter.JucePlugin/CMakeLists.txt` - modified - Adds an explicit bundle id to avoid JUCE's fallback id with spaces.
- **Coverage:** N/A

## Task 3: Wire plugin lifecycle to the sender worker

### Red Phase
- **Timestamp:** 2026-06-18 22:34
- **Failing Tests:**
  - `DesktopLoudnessMeterJucePluginTests` - `C1083`: `LoopbackSender.h` and `PluginState.h` are missing (product code missing).
- **False Red Fixes:** none

### Green Phase
- **Timestamp:** 2026-06-18 22:33
- **Passing Tests:**
  - `DesktopLoudnessMeterJucePluginTests` - pass through CTest
  - `DesktopLoudnessMeterSender_VST3` - Debug build succeeds
- **Change Summary:**
  - `src/DesktopLoudnessMeter.JucePlugin/Source/LoopbackSender.h` - added - Declares sender configuration, worker lifecycle, and non-blocking enqueue.
  - `src/DesktopLoudnessMeter.JucePlugin/Source/LoopbackSender.cpp` - added - Sends register, heartbeat, audio, and close packets from a JUCE worker thread.
  - `src/DesktopLoudnessMeter.JucePlugin/Source/PluginState.h` - added - Declares plugin state persistence helpers.
  - `src/DesktopLoudnessMeter.JucePlugin/Source/PluginState.cpp` - added - Saves and restores stable handle/display-name XML state.
  - `src/DesktopLoudnessMeter.JucePlugin/Source/PluginProcessor.h` - modified - Adds stable source identity and sender lifetime members.
  - `src/DesktopLoudnessMeter.JucePlugin/Source/PluginProcessor.cpp` - modified - Starts the sender during prepare, enqueues audio from processBlock, stops on release, and persists state.
  - `src/DesktopLoudnessMeter.JucePlugin/Tests/LoopbackSenderTests.cpp` - added - Covers UDP lifecycle packets from the sender worker.
  - `src/DesktopLoudnessMeter.JucePlugin/Tests/PluginStateTests.cpp` - added - Covers state round-trip and invalid-handle rejection.
  - `src/DesktopLoudnessMeter.JucePlugin/CMakeLists.txt` - modified - Builds the sender, state, and queue sources into the VST3 target.
  - `src/DesktopLoudnessMeter.JucePlugin/Tests/CMakeLists.txt` - modified - Adds sender and state tests to the native CTest executable.
- **Test Change Record:** Fixed the invalid-state test setup to write plain XML state bytes matching the implemented state serializer; no assertions changed.
- **Acceptance Criteria Coverage:**

| Acceptance Criterion | Test Case | Status |
|----------------------|-----------|--------|
| Plugin state persists and restores the stable handle. | `validStateRoundTripsHandleAndDisplayName`, `invalidSavedHandlesAreRejected` | pass |
| `prepareToPlay` starts or refreshes the sender with sample rate and channel count. | `DesktopLoudnessMeterSender_VST3` build plus `PluginProcessor.prepareToPlay` implementation review | pass |
| `processBlock` only performs pass-through copying and bounded enqueue. | `DesktopLoudnessMeterSender_VST3` build plus `RealtimeAudioQueue` tests | pass |
| `releaseResources` and destruction close the source. | `senderEmitsLifecyclePacketsFromWorker` | pass |
| Heartbeat and audio sending run on a non-audio worker thread. | `senderEmitsLifecyclePacketsFromWorker` | pass |

### Refactor Phase
- **Timestamp:** 2026-06-18 22:38
- **Passing Tests:**
  - `DesktopLoudnessMeterJucePluginTests` - pass through CTest
  - `DesktopLoudnessMeterSender_VST3` - Release build succeeds
- **Change Summary:**
  - `src/DesktopLoudnessMeter.JucePlugin/Source/LoopbackSender.cpp` - modified - Marks the sender active only after the worker thread starts successfully.
- **Coverage:** N/A

## Task 4: Document VST3 build and smoke flow

### Direct Implementation
- **Timestamp:** 2026-06-18 22:35
- **Validation:**
  - Documentation checked against the Debug VST3 artifact path produced by the local build.
- **Change Summary:**
  - `docs/juce-vst3-sender.md` - added - Documents VST3 build, test, artifact, install, and smoke-test flow.
  - `docs/plugin-sender-prototype.md` - modified - Updates sender prototype status now that the JUCE VST3 plugin exists.
  - `README.md` - modified - Adds the JUCE plugin project and docs to the project layout.
  - `.gitignore` - modified - Ignores generated CMake build outputs.

### Follow-up Polish
- **Timestamp:** 2026-06-18 22:46
- **Validation:**
  - `C:\Program Files\VSTPlugins` write probe passed.
- **Change Summary:**
  - `src/DesktopLoudnessMeter.JucePlugin/CMakeLists.txt` - modified - Enables JUCE's post-build VST3 copy step to `C:\Program Files\VSTPlugins` by default.
  - `docs/juce-vst3-sender.md` - modified - Documents the default Cubase smoke-test copy directory and CMake override switches.

### Cubase Scan Path Fix
- **Timestamp:** 2026-06-18 23:04
- **Validation:**
  - `C:\Program Files\Common Files\VST3` write probe passed.
- **Change Summary:**
  - `src/DesktopLoudnessMeter.JucePlugin/CMakeLists.txt` - modified - Uses the standard VST3 folder as the primary JUCE copy target and mirrors an extra copy to `C:\Program Files\VSTPlugins`.
  - `docs/juce-vst3-sender.md` - modified - Clarifies that VST2 activation does not affect this VST3 plugin and documents both copy paths.

### Cubase Scan Path Fix Refactor
- **Timestamp:** 2026-06-18 23:07
- **Validation:**
  - Initial rebuild exposed stale CMake cache and nested generator-expression handling in the extra copy command.
- **Change Summary:**
  - `src/DesktopLoudnessMeter.JucePlugin/CMakeLists.txt` - modified - Migrates the old primary copy path cache value and copies the extra bundle from the built VST3 binary's bundle root.

### Minimal Plugin UI
- **Timestamp:** 2026-06-19 00:02
- **Validation:**
  - `DesktopLoudnessMeterJucePluginTests` passed through CTest.
  - `DesktopLoudnessMeterSender_VST3` Release build passed and copied to both VST3 smoke-test folders.
  - `dotnet test --no-build` passed 142/142.
- **Change Summary:**
  - `src/DesktopLoudnessMeter.JucePlugin/Source/PluginEditor.h` - added - Declares the minimal source-name and mute editor.
  - `src/DesktopLoudnessMeter.JucePlugin/Source/PluginEditor.cpp` - added - Implements the small JUCE editor UI and commits source-name/mute changes to the processor.
  - `src/DesktopLoudnessMeter.JucePlugin/Source/PluginProcessor.h` - modified - Exposes source-name and temporary mute control state to the editor.
  - `src/DesktopLoudnessMeter.JucePlugin/Source/PluginProcessor.cpp` - modified - Creates the editor, persists the source name, sends rename requests, and clears output while muted.
  - `src/DesktopLoudnessMeter.JucePlugin/Source/LoopbackSender.h` - modified - Adds a worker-thread rename request API.
  - `src/DesktopLoudnessMeter.JucePlugin/Source/LoopbackSender.cpp` - modified - Emits rename packets without changing the stable handle.
  - `src/DesktopLoudnessMeter.JucePlugin/Tests/LoopbackSenderTests.cpp` - modified - Covers rename packet emission.
  - `src/DesktopLoudnessMeter.JucePlugin/CMakeLists.txt` - modified - Builds the plugin editor and enables keyboard focus for the editor.
  - `docs/juce-vst3-sender.md` - modified - Documents the small editor and temporary mute behavior.

### Late Receiver Startup And Port Collision Fix
- **Timestamp:** 2026-06-19 00:28
- **Validation:**
  - `DesktopLoudnessMeterJucePluginTests` passed through CTest.
  - Targeted .NET tests for main view-model and loopback receiver passed after build.
- **Change Summary:**
  - `src/DesktopLoudnessMeter.JucePlugin/Source/LoopbackSender.h` - modified - Adds a register refresh interval to sender configuration.
  - `src/DesktopLoudnessMeter.JucePlugin/Source/LoopbackSender.cpp` - modified - Repeats register packets while active and uses the latest renamed display name.
  - `src/DesktopLoudnessMeter.JucePlugin/Tests/LoopbackSenderTests.cpp` - modified - Covers repeated register emission after rename.
  - `src/DesktopLoudnessMeter.App/App.xaml.cs` - modified - Passes loopback receiver bind failures into the main view model.
  - `src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs` - modified - Keeps a visible status warning when plugin UDP receive cannot start.
  - `tests/DesktopLoudnessMeter.Tests/ViewModels/MainViewModelTests.cs` - modified - Covers persistent source-warning status text.
  - `docs/juce-vst3-sender.md` - modified - Documents flexible startup order and the port-in-use warning.
  - `docs/local-loopback-protocol.md` - modified - Documents repeated register packets as receiver-refresh behavior.
