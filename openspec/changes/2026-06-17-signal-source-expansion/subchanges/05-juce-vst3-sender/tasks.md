# Tasks

## Task 1: Add JUCE VST3 project scaffold

**Requirement Trace:** [R1, R2] -> [U1]
**Execution Mode:** [direct]
**Files:**
- `src/DesktopLoudnessMeter.JucePlugin/CMakeLists.txt` - implementation
- `src/DesktopLoudnessMeter.JucePlugin/Source/PluginProcessor.h` - implementation
- `src/DesktopLoudnessMeter.JucePlugin/Source/PluginProcessor.cpp` - implementation

**Acceptance Criteria:**
- [x] CMake defaults `JUCE_DIR` to `C:/JUCE` and builds the `VST3` format.
- [x] Plugin metadata identifies `Desktop Loudness Meter Sender`.
- [x] Processor supports mono/stereo matching bus layouts and reports no editor.
- [x] Processor passes audio through unchanged and clears extra outputs.

**Dependencies:** None

## Task 2: Implement native loopback protocol and queue

**Requirement Trace:** [R3, R4, R5] -> [U2, U3]
**Execution Mode:** [test-first]
**Files:**
- `src/DesktopLoudnessMeter.JucePlugin/Source/LoopbackProtocol.h` - implementation
- `src/DesktopLoudnessMeter.JucePlugin/Source/LoopbackProtocol.cpp` - implementation
- `src/DesktopLoudnessMeter.JucePlugin/Source/RealtimeAudioQueue.h` - implementation
- `src/DesktopLoudnessMeter.JucePlugin/Source/RealtimeAudioQueue.cpp` - implementation
- `src/DesktopLoudnessMeter.JucePlugin/Tests/CMakeLists.txt` - tests
- `src/DesktopLoudnessMeter.JucePlugin/Tests/LoopbackProtocolTests.cpp` - tests
- `src/DesktopLoudnessMeter.JucePlugin/Tests/RealtimeAudioQueueTests.cpp` - tests

**Acceptance Criteria:**
- [x] New handles are valid for the v1 loopback protocol.
- [x] Register, heartbeat, rename, audio, and close packets match the v1 JSON shape.
- [x] Audio packets encode little-endian float32 samples as base64.
- [x] Queue storage is bounded and preallocated.
- [x] Full or oversized queue submissions fail without blocking and update dropped-packet count.

**Dependencies:** Task 1

## Task 3: Wire plugin lifecycle to the sender worker

**Requirement Trace:** [R3, R4, R5] -> [U1, U2]
**Execution Mode:** [test-first]
**Files:**
- `src/DesktopLoudnessMeter.JucePlugin/Source/LoopbackSender.h` - implementation
- `src/DesktopLoudnessMeter.JucePlugin/Source/LoopbackSender.cpp` - implementation
- `src/DesktopLoudnessMeter.JucePlugin/Source/PluginState.h` - implementation
- `src/DesktopLoudnessMeter.JucePlugin/Source/PluginState.cpp` - implementation
- `src/DesktopLoudnessMeter.JucePlugin/Source/PluginProcessor.h` - implementation
- `src/DesktopLoudnessMeter.JucePlugin/Source/PluginProcessor.cpp` - implementation
- `src/DesktopLoudnessMeter.JucePlugin/Tests/LoopbackProtocolTests.cpp` - tests
- `src/DesktopLoudnessMeter.JucePlugin/Tests/LoopbackSenderTests.cpp` - tests
- `src/DesktopLoudnessMeter.JucePlugin/Tests/PluginStateTests.cpp` - tests

**Acceptance Criteria:**
- [x] Plugin state persists and restores the stable handle.
- [x] `prepareToPlay` starts or refreshes the sender with sample rate and channel count.
- [x] `processBlock` only performs pass-through copying and bounded enqueue.
- [x] `releaseResources` and destruction close the source.
- [x] Heartbeat and audio sending run on a non-audio worker thread.

**Dependencies:** Task 2

## Task 4: Document VST3 build and smoke flow

**Requirement Trace:** [R6] -> [U4]
**Execution Mode:** [direct]
**Files:**
- `docs/juce-vst3-sender.md` - documentation
- `docs/plugin-sender-prototype.md` - documentation
- `README.md` - documentation

**Acceptance Criteria:**
- [x] Docs include CMake configure/build commands.
- [x] Docs explain how to locate or copy the `.vst3` bundle.
- [x] Docs include app-plus-host smoke-test expectations.
- [x] Docs state that AAX remains future work.

**Dependencies:** Task 3
