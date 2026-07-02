# Tasks: 2026-06-29-juce-sender-protocol-boundary

## Default Constraints

**Forbidden Files**:
- `.openspec.yaml`
- `tasks.md`
- `evidence.md`

**Shared Artifacts**:
- `.openspec.yaml` - controller 串行更新
- `tasks.md` - controller 串行更新
- `evidence.md` - controller 串行更新

**Worker Return Format**: `DONE | DONE_WITH_CONCERNS | NEEDS_CONTEXT | BLOCKED`

## Task 1: Characterize and centralize C# Protocol v1 vocabulary [x]

**需求追踪**: [R1] -> [U1], [U2]
**执行方式**: [characterization-first]
**Allowed Files**:
- `src/DesktopLoudnessMeter.Protocol/LocalLoopbackProtocol.cs`
- `src/DesktopLoudnessMeter.Protocol/LocalLoopbackPacket.cs`
- `src/DesktopLoudnessMeter.Protocol/LocalLoopbackSourceKind.cs`
- `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackProtocolTests.cs`

**验收标准**:
- [x] Tests lock canonical v1 JSON shape for register, heartbeat, rename, audio, close and ack.
- [x] C# Protocol uses a centralized vocabulary/DTO boundary for packet type, field name, source kind and version constants.
- [x] Public Protocol API signatures remain compatible with existing callers.
- [x] Invalid protocol/type/handle/sample payload tests still reject bad packets.

**验证命令 / 方法**:
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~LocalLoopbackProtocolTests`，预期通过。

**Evidence Required**:
- Focused test output.
- Summary of public API compatibility and wire-shape characterization.

**Done When**:
- C# Protocol tests pass and implementation no longer scatters v1 vocabulary strings across encode/decode logic.

## Task 2: Align JUCE protocol contract with C# v1 vocabulary [x]

**需求追踪**: [R1], [R2], [R5] -> [U2], [U3]
**执行方式**: [characterization-first]
**Allowed Files**:
- `src/DesktopLoudnessMeter.JucePlugin/Source/LoopbackProtocol.h`
- `src/DesktopLoudnessMeter.JucePlugin/Source/LoopbackProtocol.cpp`
- `src/DesktopLoudnessMeter.JucePlugin/Tests/LoopbackProtocolTests.cpp`
- `src/DesktopLoudnessMeter.JucePlugin/Tests/LoopbackSenderTests.cpp`
- `src/DesktopLoudnessMeter.JucePlugin/Tests/CMakeLists.txt`

**验收标准**:
- [x] JUCE protocol tests assert same v1 field names and values as C# Protocol tests.
- [x] JUCE ack matching rejects wrong protocol, wrong type, wrong handle, invalid JSON and null/empty data.
- [x] Same-handle restart tests still prove no close before replacement register.
- [x] Explicit stop still emits close.

**验证命令 / 方法**:
- Existing JUCE/CMake test command if available; record exact command discovered during implement.
- If CMake/JUCE runner is unavailable, record toolchain probe output and run source-level checks for test coverage.

**Evidence Required**:
- JUCE test runner output or documented toolchain blocker plus source-level contract evidence.
- Focused grep/source evidence showing tests cover ack rejection and same-handle restart.

**Done When**:
- JUCE protocol/sender behavior is either freshly tested or the missing runner is explicitly documented with deterministic source-level coverage.

## Task 3: Preserve SenderKit as sender-side protocol companion [x]

**需求追踪**: [R3] -> [U4]
**执行方式**: [characterization-first]
**Allowed Files**:
- `src/DesktopLoudnessMeter.SenderKit/LocalLoopbackSender.cs`
- `src/DesktopLoudnessMeter.SenderKit/LoopbackSenderOptions.cs`
- `tests/DesktopLoudnessMeter.Tests/SenderKit/LocalLoopbackSenderTests.cs`

**验收标准**:
- [x] SenderKit continues to encode outbound packets through `DesktopLoudnessMeter.Protocol`.
- [x] Backpressure/drop tests remain non-blocking and deterministic.
- [x] Oversized v1 UDP JSON packet guard remains covered.
- [x] SenderKit does not reference host receiver or native ingest types.

**验证命令 / 方法**:
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~LocalLoopbackSenderTests`，预期 non-skipped deterministic tests pass; skipped UDP smoke recorded separately.

**Evidence Required**:
- Focused test output including passed/skip counts.
- Source evidence that SenderKit only depends on Protocol for packet encoding.

**Done When**:
- SenderKit boundary remains sender-only and tests prove queue/backpressure behavior.

## Task 4: Add deterministic receiver-to-native ingest seam coverage [x]

**需求追踪**: [R4], [R5] -> [U5]
**执行方式**: [test-first]
**Allowed Files**:
- `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackUdpReceiver.cs`
- `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackSignalSourceService.cs`
- `src/DesktopLoudnessMeter.App/Audio/Analysis/NativeSourceIngestCore.cs`
- `src/DesktopLoudnessMeter.Native/DlmSourceIngestCore.h`
- `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackUdpReceiverTests.cs`
- `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackSignalSourceServiceTests.cs`

**验收标准**:
- [x] Non-socket tests prove inbound ack is ignored and does not route into source ingest.
- [x] Non-socket tests prove valid non-ack packets route to source ingest.
- [x] Non-loopback remote packets are ignored without source state changes.
- [x] Native source ingest public boundary remains typed and does not expose UDP JSON parser API.

**验证命令 / 方法**:
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~LocalLoopbackUdpReceiverTests`，预期 deterministic tests pass; environment-skipped UDP smoke remains skipped with reason.
- `rg -n "ProcessJson|Json|Udp|Datagram" src\DesktopLoudnessMeter.Native\DlmSourceIngestCore.h src\DesktopLoudnessMeter.Native\DlmSourceIngestCore.cpp`，预期不出现 native UDP/JSON parser API。

**Evidence Required**:
- Focused receiver test output.
- Native boundary grep output.

**Done When**:
- Receiver routing is covered without relying on real UDP datagrams and native ingest remains typed.

## Task 5: Refresh codemap and verification evidence [x]

**需求追踪**: [R5] -> [U6]
**执行方式**: [direct]
**Allowed Files**:
- `.aiknowledge/codemap/juce-sender.md`
- `.aiknowledge/codemap/signal-source-ingest.md`
- `.aiknowledge/codemap/chains/pcm-metric-flow.md`
- `.aiknowledge/codemap/chains/same-handle-reconfiguration.md`
- `.aiknowledge/codemap/index.md`
- `.aiknowledge/logs/2026-06.md`

**验收标准**:
- [x] Codemap records Protocol v1 vocabulary authority and JUCE/SenderKit sender boundary.
- [x] Codemap records host receiver to native typed ingest seam.
- [x] No codemap entry claims native core owns UDP JSON parser after this change.

**验证命令 / 方法**:
- `rg -n "Protocol v1|typed ingest|LoopbackProtocol|SenderKit|DlmSourceIngestCore|UDP JSON" .aiknowledge\codemap`，预期命中更新后的边界描述。

**Evidence Required**:
- Codemap grep output.

**Done When**:
- Knowledge artifacts reflect the implemented protocol/sender/native boundary.

## Final Verification [x]

**需求追踪**: [R1], [R2], [R3], [R4], [R5]
**执行方式**: [direct]
**Allowed Files**:
- 无产品代码修改；controller 只更新 `evidence.md` 和 gate metadata。

**验收标准**:
- [x] All task-level focused checks are recorded.
- [x] Native Release build passes.
- [x] Full managed test project passes or failures are triaged as unrelated with evidence.
- [x] App build passes.
- [x] Web tests/build pass.
- [x] JUCE test route is either passed or toolchain limitation is explicitly recorded without claiming pass.

**验证命令 / 方法**:
- `powershell -ExecutionPolicy Bypass -File src\DesktopLoudnessMeter.Native\build-native.ps1 -Configuration Release`
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj`
- `dotnet build src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj`
- `npm --prefix src\DesktopLoudnessMeter.Web test -- --run`
- `npm --prefix src\DesktopLoudnessMeter.Web run build`
- JUCE test runner/toolchain probe.

**Evidence Required**:
- Final command output summary.
- Manual Checks section updated.

**Done When**:
- Evidence supports route to `opsx-verify`.

## Manual Checks

- [~] [manual] Real UDP loopback smoke for SenderKit/JUCE packets | 原因：当前测试环境已记录 127.0.0.1 UDP datagrams 不可靠；本 change 使用 deterministic contract/unit tests 覆盖主 gate，real UDP smoke 保持非阻塞。
