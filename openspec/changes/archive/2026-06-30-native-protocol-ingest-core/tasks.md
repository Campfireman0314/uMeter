# Tasks

## Default Constraints

**Forbidden Files**（默认）：
- `.openspec.yaml`
- `tasks.md`
- `evidence.md`

**Shared Artifacts**（默认）：
- `.openspec.yaml` - controller 串行更新
- `tasks.md` - controller 串行更新
- `evidence.md` - controller 串行更新

**Worker Return Format**（默认）：`DONE | DONE_WITH_CONCERNS | NEEDS_CONTEXT | BLOCKED`

## Task 1: Characterize native protocol contract [test-first]

**需求追踪**：R1, R2, R3, R5 -> U4
**执行方式**：test-first
**Allowed Files**：
- `tests/DesktopLoudnessMeter.Tests/Audio/NativeLoopbackProtocolTests.cs` - 新增 native protocol focused tests
- `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackProtocolTests.cs` - 保持/扩展 sender-side wire oracle

**验收标准**：
- [x] register/audio/heartbeat/rename/close/ack 的 native decode 目标行为有 focused tests。
- [x] invalid protocol/version/type/handle/sampleRate/channels/displayName/base64/audio alignment 有 rejection tests。
- [x] native ack encoding wire shape 有 focused test。
- [x] C# sender-side `LocalLoopbackProtocolTests` 继续验证 canonical Protocol v1 JSON shape。

**验证命令 / 方法**：
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~NativeLoopbackProtocolTests`，初始可失败，最终通过。
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~LocalLoopbackProtocolTests`，最终通过。

**Evidence Required**：
- 红/绿阶段测试结果。
- 若初始红因类型/文件不存在失败，记录为预期 test-first 红。

**Done When**：
- Native protocol 目标行为被 deterministic tests 描述，且 sender-side wire oracle 仍通过。

## Task 2: Implement native protocol ingest ABI [test-first]

**需求追踪**：R1, R2, R3, R5 -> U1
**执行方式**：test-first
**Allowed Files**：
- `src/DesktopLoudnessMeter.Native/DlmLoopbackProtocol.h` - native C ABI declarations
- `src/DesktopLoudnessMeter.Native/DlmLoopbackProtocol.cpp` - parser/base64/ack implementation
- `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeterNative.cpp` - exported forwarding functions
- `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeter.Native.vcxproj` - MSBuild source/header list
- `src/DesktopLoudnessMeter.Native/CMakeLists.txt` - CMake source list
- `tests/DesktopLoudnessMeter.Tests/Audio/NativeLoopbackProtocolTests.cs` - adjust tests only to match final public wrapper names

**验收标准**：
- [x] Native decode accepts all valid Protocol v1 packet types and exposes required metadata/string/sample copy functions.
- [x] Native decode rejects invalid packets without partial packet output.
- [x] Native ack encode validates handle and returns caller-owned bytes.
- [x] New native files are included in `.vcxproj` and `CMakeLists.txt`.
- [x] Native Release build succeeds before managed DllImport tests are treated as meaningful.

**验证命令 / 方法**：
- `powershell -ExecutionPolicy Bypass -File src\DesktopLoudnessMeter.Native\build-native.ps1 -Configuration Release`，预期通过。
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~NativeLoopbackProtocolTests`，预期通过。
- `rg -n "DlmLoopbackProtocol" src\DesktopLoudnessMeter.Native\DesktopLoudnessMeter.Native.vcxproj src\DesktopLoudnessMeter.Native\CMakeLists.txt src\DesktopLoudnessMeter.Native\DesktopLoudnessMeterNative.cpp`，预期命中新文件和 exports。

**Evidence Required**：
- Native build 输出摘要。
- Focused native protocol tests 通过摘要。
- file list/export grep 摘要。

**Done When**：
- Native protocol ABI、exports、Windows build path 和 managed focused tests 全部闭环。

**依赖**：Task 1

## Task 3: Migrate receiver route to native protocol wrapper [characterization-first]

**需求追踪**：R2, R4 -> U2, U3
**执行方式**：characterization-first
**Allowed Files**：
- `src/DesktopLoudnessMeter.App/Audio/Analysis/NativeLoopbackProtocol.cs` - managed P/Invoke wrapper
- `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackUdpReceiver.cs` - route/decode/ack migration
- `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackUdpReceiverTests.cs` - receiver route regression
- `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackSignalSourceServiceTests.cs` - source ingest regression only if behavior needs characterization

**验收标准**：
- [x] `RouteReceivedPacket` keeps non-loopback remote filtering in Host glue.
- [x] Valid non-ack loopback packets route through native decode to source ingest and return ack handle.
- [x] Inbound ack and invalid packets do not ingest and do not ack.
- [x] `SendAckAsync` uses native ack encoding.
- [x] `LocalLoopbackUdpReceiver.cs` no longer directly references `LocalLoopbackProtocol.TryDecode` or `LocalLoopbackProtocol.EncodeAck`.

**验证命令 / 方法**：
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~LocalLoopbackUdpReceiverTests`，预期通过。
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~LocalLoopbackSignalSourceServiceTests`，预期通过。
- `rg -n "LocalLoopbackProtocol\.TryDecode|LocalLoopbackProtocol\.EncodeAck" src\DesktopLoudnessMeter.App\Audio\LocalLoopbackUdpReceiver.cs`，预期无命中。

**Evidence Required**：
- Receiver/source-service focused tests 通过摘要。
- Direct C# protocol decode/ack grep no-match 记录。

**Done When**：
- Host receiver 只保留 socket/IP/routing glue，protocol decode/ack 已通过 native wrapper。

**依赖**：Task 2

## Task 4: Run regression, update codemap and prepare verification [direct]

**需求追踪**：R5 -> U5
**执行方式**：direct
**Allowed Files**：
- `.aiknowledge/codemap/signal-source-ingest.md` - protocol ingest boundary update
- `.aiknowledge/codemap/native-dsp.md` - native protocol key files/update
- `.aiknowledge/codemap/juce-sender.md` - sender remains sender-side protocol owner
- `.aiknowledge/codemap/chains/pcm-metric-flow.md` - local/plugin flow update
- `.aiknowledge/logs/2026-06.md` - monthly knowledge log entry
- `openspec/changes/2026-06-30-native-protocol-ingest-core/evidence.md` - controller evidence updates

**验收标准**：
- [x] Codemap records Host UDP glue -> native protocol decode/ack -> managed packet -> native source ingest flow.
- [x] Codemap does not claim UDP socket or HostSnapshot projection moved to native.
- [x] Full managed tests, app build, web tests/build and native build have fresh evidence.
- [x] JUCE/CMake availability is probed and recorded without false pass if unavailable.

**验证命令 / 方法**：
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj`，预期通过。
- `dotnet build src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj`，预期通过。
- `npm --prefix src\DesktopLoudnessMeter.Web test -- --run`，预期通过。
- `npm --prefix src\DesktopLoudnessMeter.Web run build`，预期通过。
- `where.exe cmake` and `Test-Path C:\JUCE\CMakeLists.txt`，记录可用性。
- `rg -n "native protocol|DlmLoopbackProtocol|NativeLoopbackProtocol|UDP socket|Host glue" .aiknowledge\codemap openspec\changes\2026-06-30-native-protocol-ingest-core\evidence.md`，预期命中更新内容。

**Evidence Required**：
- Regression/build command summaries。
- Codemap grep 摘要。
- JUCE/CMake probe 摘要。

**Done When**：
- 实现、测试、build、knowledge 和 evidence 已准备进入 `opsx-verify`。
