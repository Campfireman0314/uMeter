# Evidence: 2026-06-30-native-protocol-ingest-core

正文默认使用中文；路径、命令、配置键、代码标识和状态枚举保留原文。

## Explore Baseline

### Required round intake

- `opsx changes list`：无活动变更。
- 已读取归档规划：
  - `openspec/changes/archive/2026-06-29-cross-platform-architecture-plan/work.md`
  - `openspec/changes/archive/2026-06-29-cross-platform-architecture-plan/design.md`
- 已读取 codemap：
  - `.aiknowledge/codemap/index.md`
  - `.aiknowledge/codemap/signal-source-ingest.md`
  - `.aiknowledge/codemap/host-contract.md`
  - `.aiknowledge/codemap/native-dsp.md`
  - `.aiknowledge/codemap/juce-sender.md`
  - `.aiknowledge/codemap/audio-capture-sources.md`
  - `.aiknowledge/codemap/chains/pcm-metric-flow.md`
  - `.aiknowledge/codemap/chains/same-handle-reconfiguration.md`
- 已读取 completion audit：
  - `openspec/changes/archive/2026-06-30-completion-audit/evidence.md`
- 已读取相关归档 evidence：
  - `openspec/changes/archive/2026-06-29-native-source-ingest-core/evidence.md`
  - `openspec/changes/archive/2026-06-29-juce-sender-protocol-boundary/evidence.md`
- 已读取 pitfalls：
  - `.aiknowledge/pitfalls/index.md`
  - `.aiknowledge/pitfalls/misc/index.md`
  - `.aiknowledge/pitfalls/lifecycle/index.md`

### Source facts

- `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackUdpReceiver.cs`
  - `RouteReceivedPacket` 当前直接调用 `LocalLoopbackProtocol.TryDecode`。
  - `SendAckAsync` 当前调用 `LocalLoopbackProtocol.EncodeAck`。
  - UDP socket、receive loop 和 loopback remote 过滤在 Host C#。
- `src/DesktopLoudnessMeter.Protocol/LocalLoopbackProtocol.cs`
  - 当前 Protocol v1 JSON encode/decode、handle validation、source kind parsing、sample base64 encode/decode 均在 C#。
- `src/DesktopLoudnessMeter.Native/DlmSourceIngestCore.h`
  - native source ingest 当前只接收 typed source lifecycle/audio fields，不接收 UDP JSON bytes。
- `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeter.Native.vcxproj` 和 `src/DesktopLoudnessMeter.Native/CMakeLists.txt`
  - native source files 显式列出，新 native protocol 文件必须加入两个 build entry。
- `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackProtocolTests.cs`
  - 当前 C# Protocol tests 已锁定 register/audio/simple/rename wire shape 和 invalid packet rejection。
- `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackUdpReceiverTests.cs`
  - 当前 deterministic receiver tests 覆盖 inbound ack ignore、valid non-ack route、non-loopback ignore；real UDP smoke 被 skip，原因是当前环境 UDP loopback 不可靠。

### Explore self-check

- 候选推荐：创建 `2026-06-30-native-protocol-ingest-core`。
- 第 1 轮扫描：blocking=none；non-blocking=JSON/base64 parser correctness、native DLL export freshness、real UDP smoke environment limitation、JUCE runner availability。
- 未验证前提：native C++ parser 可以不引入第三方依赖覆盖当前 Protocol v1 JSON 子集；Windows native build toolchain 仍可用。
- 最可能错处：把 protocol ingest 过度扩大为 UDP transport 迁移，或让 native packet DTO 过早进入 HostBridge/Web UI。
- 失败路径 / 边界：invalid JSON、malformed base64、misaligned float bytes、case-insensitive type handling、ack packet routing、same-handle register path。
- 更小替代：只新增 native decode/ack wrapper 但 receiver route 暂不切换；该替代无法关闭 audit 中 C# protocol glue 缺口，因此不采用。
- 证据 vs 推断：代码/codemap 证明当前 C# decode/ack 状态；独立 native protocol 单元是设计推断，已固化到 work/design/tasks。
- 处理方式：把 non-blocking 风险写入 spec/design/tasks 验证项。
- 最终扫描结论：无新的关键事实性漏洞；剩余不确定性不改变 change 范围和 profile。

## Plan Evidence

### Branch / change creation

- `git switch -c codex/2026-06-30-native-protocol-ingest-core`
  - result: created and switched to dedicated branch.
- `opsx changes init 2026-06-30-native-protocol-ingest-core spec-driven --profile strict-change --source-type feature`
  - result: initialized `openspec/changes/2026-06-30-native-protocol-ingest-core`.

### Scope decision

- result: `KEEP_ONE_CHANGE`
- profile: `strict-change`
- rationale: touches native ABI, managed P/Invoke wrapper, Host receiver route, Protocol v1 contract tests and native build files; still one complete module closure around receiver-side Protocol v1 ingest entering C++ Core.

### Test framework probe

- Repository has existing `tests/DesktopLoudnessMeter.Tests` xUnit project, native MSBuild script, Web npm tests/build, and JUCE source tests with known local runner limitations.

## Readiness Evidence

### 2026-06-30T18:36:35+08:00 - pass

StageResult:
- stage: `opsx-readiness`
- status: `pass`
- change: `2026-06-30-native-protocol-ingest-core`
- branch: `codex/2026-06-30-native-protocol-ingest-core`
- profile: `strict-change`

Checks:
- `work.md` 包含 intent、scope、non-goals、dependencies、success criteria、verification plan、rollback/escalation、bugfix/root-cause context、readiness notes、explore self-check、scope/profile/split decision 和 workspace strategy。
- `specs/change/spec.md` 定义 R1-R5，`**Trace**: R1` 到 `R5` 全局唯一，且每条 requirement 描述可观察行为。
- `design.md` 覆盖 R1-R5，并定义 U1-U5；每个 U section 均包含 `模块边界` 和具体文件范围。
- `tasks.md` 包含 Default Constraints；Task 1-4 均有需求追踪、执行方式、Allowed Files、验收标准、验证命令 / 方法、Evidence Required、Done When；无 `[stabilization-first]` task。
- Manual Checks 仅包含 `[~]` real UDP smoke waiver，且有原因；无未处理 `[ ] [manual]` 项。
- Workspace strategy 已解析为 `current-worktree`，专用 branch `codex/2026-06-30-native-protocol-ingest-core`，dirty tree 仅包含本 change root。
- Profile `strict-change` 匹配 native ABI / managed P/Invoke / Host receiver route / build file 的跨模块风险。

Commands / evidence:
- `rg -n "Trace\\*\\*: R|R[1-5]|\\[U[1-5]\\]|需求追踪|Allowed Files|Evidence Required|Done When|Workspace Strategy|Manual Checks|\\[ \\]|\\[~\\]" openspec\changes\2026-06-30-native-protocol-ingest-core`：命中 R/U/task/manual/workspace 结构；无 readiness-blocking 缺口。
- `Get-ChildItem -Recurse openspec\changes\2026-06-30-native-protocol-ingest-core`：确认 `.openspec.yaml`、`work.md`、`design.md`、`tasks.md`、`evidence.md`、`specs/change/spec.md` 存在。
- `opsx changes list`：`2026-06-30-native-protocol-ingest-core [tasks profile=strict-change next=opsx-readiness]`。
- `git status --short --branch`：branch=`codex/2026-06-30-native-protocol-ingest-core`，dirty only `?? openspec/changes/2026-06-30-native-protocol-ingest-core/`。
- `git rev-parse HEAD`：`845416989cf6c4393d86b297a0e44eaec93d6a74`。

Decision:
- readiness passed; route to `opsx-implement`.

## Implementation Evidence

## test-cycle | 2026-06-30T18:42:30+08:00 | pass
任务：Task 1 - Characterize native protocol contract
执行方式：test-first

### 红阶段
- 失败测试列表：
  - `NativeLoopbackProtocolTests.*` - `CS0103`: `NativeLoopbackProtocol` does not exist in the current context。
- 假红修正：无。失败原因是目标 managed wrapper 尚未实现，符合 test-first 红阶段。
- 命令：
  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~NativeLoopbackProtocolTests`
  - 结果：exit 1，编译失败，命中 6 个 `CS0103`。

### 绿阶段
- 通过测试列表：
  - `NativeLoopbackProtocolTests.RegisterPacketDecodesThroughNativeProtocol`
  - `NativeLoopbackProtocolTests.AudioPacketDecodesLittleEndianFloatSamplesThroughNativeProtocol`
  - `NativeLoopbackProtocolTests.SimpleControlPacketsDecodeThroughNativeProtocol`
  - `NativeLoopbackProtocolTests.RenamePacketDecodesThroughNativeProtocol`
  - `NativeLoopbackProtocolTests.InvalidPacketsAreRejectedByNativeProtocol`
  - `NativeLoopbackProtocolTests.AckEncodingUsesProtocolV1WireShape`
- 变更摘要：
  - `tests/DesktopLoudnessMeter.Tests/Audio/NativeLoopbackProtocolTests.cs` - 新增 - 描述 native receiver-side Protocol v1 decode/ack contract。
  - `src/DesktopLoudnessMeter.App/Audio/Analysis/NativeLoopbackProtocol.cs` - 新增 - managed native protocol wrapper。
  - `src/DesktopLoudnessMeter.Native/DlmLoopbackProtocol.h/.cpp` - 新增 - native parser/base64/ack implementation。
  - `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeterNative.cpp` - 修改 - 新增 exported forwarding functions。
  - `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeter.Native.vcxproj` / `CMakeLists.txt` - 修改 - 新增 native protocol source list。
- 命令：
  - `powershell -ExecutionPolicy Bypass -File src\DesktopLoudnessMeter.Native\build-native.ps1 -Configuration Release`
  - 结果：pass，MSBuild 17.14，0 warnings，0 errors。
  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~NativeLoopbackProtocolTests`
  - 结果：pass，14 passed，0 failed，0 skipped。

| 验收标准 (AC) | 测试/证据 | 状态 |
|--------------|-----------|------|
| register/audio/heartbeat/rename/close/ack native decode focused tests | `NativeLoopbackProtocolTests` | pass |
| invalid protocol/version/type/handle/sampleRate/channels/displayName/base64/audio alignment rejection tests | `NativeLoopbackProtocolTests.InvalidPacketsAreRejectedByNativeProtocol` | pass |
| native ack encoding wire shape focused test | `NativeLoopbackProtocolTests.AckEncodingUsesProtocolV1WireShape` | pass |
| C# sender-side wire oracle remains available | Existing `LocalLoopbackProtocolTests` preserved; focused run scheduled in final regression | pass |

### 重构阶段
- 通过测试列表：
  - `NativeLoopbackProtocolTests` focused suite, 14 passed。
- 覆盖率：N/A（不可用原因：focused `dotnet test` 未启用 coverage collection；补救：后续 full managed regression 和 focused protocol suites 覆盖）。

## test-cycle | 2026-06-30T18:42:30+08:00 | pass
任务：Task 2 - Implement native protocol ingest ABI
执行方式：test-first

### 红阶段
- 失败测试列表：
  - 与 Task 1 共享红阶段：native wrapper/export/parser 尚未实现导致 `NativeLoopbackProtocolTests` 编译失败。
- 假红修正：无。

### 绿阶段
- 通过测试列表：
  - `NativeLoopbackProtocolTests` 14 passed。
- 变更摘要：
  - `DlmLoopbackProtocol` 提供 raw UTF-8 JSON decode、string/sample copy、ack encode。
  - `DesktopLoudnessMeterNative.cpp` 新增 `DlmLoopbackProtocol*Export` functions。
  - `.vcxproj` 和 `CMakeLists.txt` 包含 `DlmLoopbackProtocol.cpp`。
- 验证：
  - Native Release build passed，证明新 native file 已编译并链接到 DLL。
  - Focused managed P/Invoke tests passed，证明 Release DLL 中新导出可加载。

| 验收标准 (AC) | 测试/证据 | 状态 |
|--------------|-----------|------|
| Native decode accepts valid Protocol v1 packet types | `NativeLoopbackProtocolTests` | pass |
| Native decode rejects invalid packets | `NativeLoopbackProtocolTests.InvalidPacketsAreRejectedByNativeProtocol` | pass |
| Native ack encode validates handle and returns caller-owned bytes | `NativeLoopbackProtocolTests.AckEncodingUsesProtocolV1WireShape` | pass |
| New native files included in `.vcxproj` and `CMakeLists.txt` | native Release build compiled `DlmLoopbackProtocol.cpp` | pass |
| Native Release build succeeds before managed DllImport tests | `build-native.ps1 -Configuration Release` before `dotnet test` | pass |

### 重构阶段
- 通过测试列表：
  - `NativeLoopbackProtocolTests` focused suite, 14 passed。
- 覆盖率：N/A（不可用原因：focused `dotnet test` 未启用 coverage collection；补救：后续 full managed regression 和 native build/file-list grep 覆盖）。

## test-cycle | 2026-06-30T18:44:27+08:00 | pass
任务：Task 3 - Migrate receiver route to native protocol wrapper
执行方式：characterization-first

### 红阶段 / characterization
- 旧行为固化：
  - `LocalLoopbackUdpReceiverTests` 已覆盖 inbound ack ignore、valid non-ack route、non-loopback remote ignore。
  - 本 task 新增 invalid payload no-ack regression。
- 迁移目标：
  - `LocalLoopbackUdpReceiver` 保留 loopback IP filter 和 UDP socket glue。
  - Decode/ack 从 C# `LocalLoopbackProtocol` 切到 `NativeLoopbackProtocol`。

### 绿阶段
- 通过测试列表：
  - `LocalLoopbackUdpReceiverTests`: 5 passed, 2 skipped。
  - `LocalLoopbackSignalSourceServiceTests`: 6 passed。
- 变更摘要：
  - `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackUdpReceiver.cs` - 修改 - route decode 和 ack encode 改走 `NativeLoopbackProtocol`。
  - `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackUdpReceiverTests.cs` - 修改 - 增加 invalid payload no-ack regression。
- 命令：
  - 并行 focused run 初次出现 WPF build output lock：`CS2012 Cannot open ... uMeter.dll ... because it is being used by another process`。这是并行 dotnet build 输出锁，不是产品行为失败；已串行重跑。
  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~LocalLoopbackSignalSourceServiceTests`
    - pass: 6 passed, 0 failed, 0 skipped。
  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~LocalLoopbackUdpReceiverTests`
    - pass: 5 passed, 0 failed, 2 skipped。
  - `rg -n "LocalLoopbackProtocol\.TryDecode|LocalLoopbackProtocol\.EncodeAck" src\DesktopLoudnessMeter.App\Audio\LocalLoopbackUdpReceiver.cs`
    - exit 1, no matches。

| 验收标准 (AC) | 测试/证据 | 状态 |
|--------------|-----------|------|
| `RouteReceivedPacket` keeps non-loopback remote filtering | `RouteReceivedPacketIgnoresNonLoopbackRemote` | pass |
| Valid non-ack routes to source ingest and returns ack handle | `RouteReceivedPacketRoutesValidNonAckFromLoopback` | pass |
| Inbound ack and invalid packets do not ingest or ack | inbound ack + invalid payload tests | pass |
| `SendAckAsync` uses native ack encoding | source grep for `NativeLoopbackProtocol.EncodeAck` | pass |
| Receiver no longer directly references C# decode/ack | grep no-match for `LocalLoopbackProtocol.TryDecode|EncodeAck` | pass |

### 重构阶段
- 通过测试列表：
  - `LocalLoopbackUdpReceiverTests` focused suite。
  - `LocalLoopbackSignalSourceServiceTests` focused suite。
- 覆盖率：N/A（不可用原因：focused `dotnet test` 未启用 coverage collection；补救：后续 full managed regression 覆盖）。

## test-cycle | 2026-06-30T18:48:29+08:00 | pass_with_tooling_limit
任务：Task 4 - Run regression, update codemap and prepare verification
执行方式：direct

### 直接验证
- 跳过测试循环理由：codemap/evidence 更新和整体验证收口；业务行为已由 Task 1-3 的 test-first / characterization-first 覆盖。
- 变更摘要：
  - `.aiknowledge/codemap/index.md` - 修改 - 更新 native protocol/source ingest 职责和 PCM flow 摘要。
  - `.aiknowledge/codemap/signal-source-ingest.md` - 修改 - 记录 `DlmLoopbackProtocol` / `NativeLoopbackProtocol` 边界。
  - `.aiknowledge/codemap/native-dsp.md` - 修改 - 记录 native protocol ingest key files 和 native DLL export freshness 约束。
  - `.aiknowledge/codemap/juce-sender.md` - 修改 - 记录 JUCE/SenderKit 仍是 sender-side，Host ack payload 由 native 生成。
  - `.aiknowledge/codemap/audio-capture-sources.md` - 修改 - 记录 Host UDP socket glue 与 native protocol decode 分工。
  - `.aiknowledge/codemap/chains/pcm-metric-flow.md` - 修改 - 记录 receiver path：Host socket glue -> native protocol decode -> managed packet -> native source ingest。
  - `.aiknowledge/logs/2026-06.md` - 修改 - 增加本 change 实施记录。

验证命令 / 结果：
- `powershell -ExecutionPolicy Bypass -File src\DesktopLoudnessMeter.Native\build-native.ps1 -Configuration Release`
  - pass: MSBuild 17.14，0 warnings，0 errors。
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter "FullyQualifiedName~NativeLoopbackProtocolTests|FullyQualifiedName~LocalLoopbackProtocolTests|FullyQualifiedName~LocalLoopbackUdpReceiverTests|FullyQualifiedName~LocalLoopbackSignalSourceServiceTests"`
  - pass: 36 passed，0 failed，2 skipped。
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj`
  - pass: 351 passed，0 failed，5 skipped。
- `dotnet build src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj`
  - pass: 0 warnings，0 errors。
- `npm --prefix src\DesktopLoudnessMeter.Web test -- --run`
  - pass: 6 test files，25 tests。
- `npm --prefix src\DesktopLoudnessMeter.Web run build`
  - pass: Vite production build succeeded。
- `where.exe cmake`
  - result: `INFO: Could not find files for the given pattern(s).`
- `Test-Path C:\JUCE\CMakeLists.txt`
  - result: `False`
- `rg -n "native protocol|DlmLoopbackProtocol|NativeLoopbackProtocol|UDP socket|Host glue|protocol decode|ack" .aiknowledge\codemap openspec\changes\2026-06-30-native-protocol-ingest-core\evidence.md`
  - pass: 命中 codemap/evidence 中的新边界描述。
- `rg -n "DlmLoopbackProtocol" src\DesktopLoudnessMeter.Native\DesktopLoudnessMeter.Native.vcxproj src\DesktopLoudnessMeter.Native\CMakeLists.txt src\DesktopLoudnessMeter.Native\DesktopLoudnessMeterNative.cpp`
  - pass: 命中新 native file list 和 exports。
- `git diff --check`
  - pass: exit 0；仅报告 LF->CRLF working-copy warnings，无 whitespace errors。

| 验收标准 (AC) | 证据 | 状态 |
|--------------|------|------|
| Codemap records Host UDP glue -> native protocol decode/ack -> managed packet -> native source ingest flow | codemap grep | pass |
| Codemap does not claim UDP socket or HostSnapshot projection moved to native | `signal-source-ingest`, `native-dsp`, `pcm-metric-flow` constraints | pass |
| Full managed tests, app build, web tests/build and native build have fresh evidence | commands above | pass |
| JUCE/CMake availability is probed and recorded without false pass | `where.exe cmake`, `Test-Path C:\JUCE\CMakeLists.txt` | pass_with_tooling_limit |

Allowed tooling limits:
- JUCE binary tests were not run because `cmake` is unavailable on PATH and `C:\JUCE\CMakeLists.txt` does not exist. This is non-blocking because this change does not modify JUCE sender code and managed/native/Web gates passed; no JUCE binary pass is claimed.

## Verification Evidence

## verify | 2026-06-30T18:53:44+08:00 | pass_with_warnings

方向：`tasks.md` + `specs/**/*.md` + `design.md` + 当前工作树 diff -> 验证通过，保留环境限制 warning。

证据：
- OpenSpec artifacts 已读取：`work.md`, `tasks.md`, `specs/change/spec.md`, `design.md`, `evidence.md`, `.openspec.yaml`。
- `opsx changes list`: active `2026-06-30-native-protocol-ingest-core [tasks profile=strict-change next=opsx-verify]`。
- `rg -n "^- \[ \]|\[ \]" openspec\changes\2026-06-30-native-protocol-ingest-core\tasks.md`: exit 1, no incomplete task checkboxes。
- `powershell -ExecutionPolicy Bypass -File src\DesktopLoudnessMeter.Native\build-native.ps1 -Configuration Release`: pass, MSBuild 17.14, 0 warnings, 0 errors, `DesktopLoudnessMeter.Native.dll` up to date。
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter "FullyQualifiedName~NativeLoopbackProtocolTests|FullyQualifiedName~LocalLoopbackProtocolTests|FullyQualifiedName~LocalLoopbackUdpReceiverTests|FullyQualifiedName~LocalLoopbackSignalSourceServiceTests"`: pass, 36 passed, 2 skipped。
- `rg -n "LocalLoopbackProtocol\.TryDecode|LocalLoopbackProtocol\.EncodeAck" src\DesktopLoudnessMeter.App\Audio\LocalLoopbackUdpReceiver.cs`: exit 1, no matches；receiver 不再直接调用 C# protocol decode/ack。
- `rg -n "DlmLoopbackProtocol" src\DesktopLoudnessMeter.Native\DesktopLoudnessMeter.Native.vcxproj src\DesktopLoudnessMeter.Native\CMakeLists.txt src\DesktopLoudnessMeter.Native\DesktopLoudnessMeterNative.cpp`: pass, 命中新 native file list 和 exports。
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj`: pass, 351 passed, 5 skipped。
- `dotnet build src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj`: pass, 0 warnings, 0 errors。
- `npm --prefix src\DesktopLoudnessMeter.Web test -- --run`: pass, 6 test files, 25 tests。
- `npm --prefix src\DesktopLoudnessMeter.Web run build`: pass, Vite production build succeeded。
- `where.exe cmake`: exit 1, `INFO: Could not find files for the given pattern(s).`
- `Test-Path C:\JUCE\CMakeLists.txt`: `False`。
- `rg -n "native protocol|DlmLoopbackProtocol|NativeLoopbackProtocol|UDP socket|Host glue|protocol decode|ack" .aiknowledge\codemap openspec\changes\2026-06-30-native-protocol-ingest-core\evidence.md`: pass, 命中 codemap/evidence 的新边界描述。
- `git diff --check`: exit 0；仅 LF->CRLF working-copy warnings，无 whitespace errors。
- Git: branch=`codex/2026-06-30-native-protocol-ingest-core`; HEAD=`845416989cf6c4393d86b297a0e44eaec93d6a74`; base=`codex/2026-06-30-web-settings-workflow-parity` / `845416989cf6c4393d86b297a0e44eaec93d6a74`; diff_status=working tree contains owned change files plus untracked change artifacts/native wrapper/tests; verification_coverage=worktree diff; manual_status=waived; stabilization_status=none; amendment_status=none。

allowed_failures:
- item: JUCE binary tests not run because `cmake` is unavailable on PATH and `C:\JUCE\CMakeLists.txt` does not exist.
  why_non_blocking: 本 change 未修改 JUCE sender code 或 sender wire shape；C# sender-side wire oracle、native receiver decode/ack tests、receiver/source-service tests、native Release build、managed regression、app build 和 Web tests/build 均通过；没有声明 JUCE binary pass。
  owner_or_followup: 后续 JUCE build environment / release smoke lane；任何修改 JUCE sender lifecycle 或 Protocol v1 sender implementation 的 change 必须复查。
  expiry_or_recheck: 当 CMake/JUCE runner 可用时，或进入 release gate / JUCE sender 相关 change 前重新运行。
- item: Real UDP loopback sender/receiver socket smoke remains skipped/waived in this local environment.
  why_non_blocking: 当前仓库已记录本环境 127.0.0.1 UDP datagrams 不可靠；本 change 的协议、receiver route 和 source ingest 行为由 deterministic tests 覆盖，Host UDP socket ownership 未被迁移。
  owner_or_followup: Windows platform smoke lane / packaging-startup integration follow-up。
  expiry_or_recheck: 在稳定 Windows loopback UDP 环境、CI smoke 环境或发布前平台验证时重新运行。

问题：
- 未发现阻断项。
- 保留两个非阻断 warning：JUCE/CMake runner 不可用、真实 UDP socket smoke 本地豁免。

需修正：
- 无。

## implement-amendment | 2026-06-30T18:57:14+08:00 | pass

observation:
- review 期间检查 native JSON string parser 时发现 `\uD800-\uDBFF` / `\uDC00-\uDFFF` surrogate pair 兼容风险；现有 parser 会把合并后的非 BMP code point 交给只支持 3-byte 的 UTF-8 writer。

classification:
- within-scope；属于 R1/R5 的 receiver-side Protocol v1 JSON decode parity，不改变 wire shape、UDP transport 或 source ingest ABI。

changes:
- `tests/DesktopLoudnessMeter.Tests/Audio/NativeLoopbackProtocolTests.cs` 增加 `EscapedNonBmpDisplayNameDecodesThroughNativeProtocol`。
- `src/DesktopLoudnessMeter.Native/DlmLoopbackProtocol.cpp` 增加 surrogate-pair parsing，拒绝孤立 low/high surrogate，并补齐 4-byte UTF-8 encode。

validation:
- 初次红灯：`dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~NativeLoopbackProtocolTests` failed，`Expected: "Mix 🎧"` / `Actual: "Mix "`。
- 修正后：`powershell -ExecutionPolicy Bypass -File src\DesktopLoudnessMeter.Native\build-native.ps1 -Configuration Release; dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~NativeLoopbackProtocolTests` passed，native build 0 warnings/0 errors，15 tests passed。

docs_synced:
- No spec/design change required；该 amendment 落在既有 R1/R5 parity 与 invalid JSON handling 约束内。

pending:
- none。

gate_impact:
- `.openspec.yaml gates.verify.status` 已标记 `needs_rerun`，必须重新执行 verify 后才能进入 review/archive。

## verify | 2026-06-30T18:58:46+08:00 | pass_with_warnings

方向：implement-amendment 后重新验证 `tasks.md` + `specs/**/*.md` + `design.md` + 当前工作树 diff -> 验证通过，保留环境限制 warning。

证据：
- `powershell -ExecutionPolicy Bypass -File src\DesktopLoudnessMeter.Native\build-native.ps1 -Configuration Release`: pass, `DlmLoopbackProtocol.cpp` 重新编译并链接，0 warnings, 0 errors。
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~NativeLoopbackProtocolTests`: pass, 15 passed；包含 amendment 新增的 escaped non-BMP displayName regression。
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter "FullyQualifiedName~NativeLoopbackProtocolTests|FullyQualifiedName~LocalLoopbackProtocolTests|FullyQualifiedName~LocalLoopbackUdpReceiverTests|FullyQualifiedName~LocalLoopbackSignalSourceServiceTests"`: pass, 37 passed, 2 skipped。
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj`: pass, 352 passed, 5 skipped。
- `dotnet build src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj`: pass, 0 warnings, 0 errors。
- `npm --prefix src\DesktopLoudnessMeter.Web test -- --run`: pass, 6 test files, 25 tests。
- `npm --prefix src\DesktopLoudnessMeter.Web run build`: pass, Vite production build succeeded。
- `rg -n "LocalLoopbackProtocol\.TryDecode|LocalLoopbackProtocol\.EncodeAck" src\DesktopLoudnessMeter.App\Audio\LocalLoopbackUdpReceiver.cs`: no matches before command sequence continued；receiver 不再直接调用 C# protocol decode/ack。
- `rg -n "DlmLoopbackProtocol" src\DesktopLoudnessMeter.Native\DesktopLoudnessMeter.Native.vcxproj src\DesktopLoudnessMeter.Native\CMakeLists.txt src\DesktopLoudnessMeter.Native\DesktopLoudnessMeterNative.cpp`: pass, 命中新 native file list 和 exports。
- `git diff --check`: exit 0；仅 LF->CRLF working-copy warnings，无 whitespace errors。
- `where.exe cmake`: exit 1, `INFO: Could not find files for the given pattern(s).`
- `Test-Path C:\JUCE\CMakeLists.txt`: `False`。
- Git: branch=`codex/2026-06-30-native-protocol-ingest-core`; HEAD=`845416989cf6c4393d86b297a0e44eaec93d6a74`; base=`codex/2026-06-30-web-settings-workflow-parity` / `845416989cf6c4393d86b297a0e44eaec93d6a74`; diff_status=working tree contains owned implementation/test/knowledge/OpenSpec files; verification_coverage=worktree diff after amendment; manual_status=waived; stabilization_status=none; amendment_status=reverified。

allowed_failures:
- item: JUCE binary tests not run because `cmake` is unavailable on PATH and `C:\JUCE\CMakeLists.txt` does not exist.
  why_non_blocking: 本 change 未修改 JUCE sender code 或 sender wire shape；C# sender-side wire oracle、native receiver decode/ack tests、receiver/source-service tests、native Release build、managed regression、app build 和 Web tests/build 均通过；没有声明 JUCE binary pass。
  owner_or_followup: 后续 JUCE build environment / release smoke lane；任何修改 JUCE sender lifecycle 或 Protocol v1 sender implementation 的 change 必须复查。
  expiry_or_recheck: 当 CMake/JUCE runner 可用时，或进入 release gate / JUCE sender 相关 change 前重新运行。
- item: Real UDP loopback sender/receiver socket smoke remains skipped/waived in this local environment.
  why_non_blocking: 当前仓库已记录本环境 127.0.0.1 UDP datagrams 不可靠；本 change 的协议、receiver route 和 source ingest 行为由 deterministic tests 覆盖，Host UDP socket ownership 未被迁移。
  owner_or_followup: Windows platform smoke lane / packaging-startup integration follow-up。
  expiry_or_recheck: 在稳定 Windows loopback UDP 环境、CI smoke 环境或发布前平台验证时重新运行。

问题：
- 未发现阻断项。
- 保留两个非阻断 warning：JUCE/CMake runner 不可用、真实 UDP socket smoke 本地豁免。

需修正：
- 无。

## implement-amendment | 2026-06-30T19:02:11+08:00 | pass

observation:
- review 期间继续核对 receiver-side handle compatibility 时发现旧 C# `LocalLoopbackProtocol.IsValidHandle` 以字符数限制 128，而 native parser 初版以 UTF-8 byte count 限制 128；非 ASCII handle 在 SenderKit public path 中理论上可能合法但被 native receiver 拒绝。

classification:
- within-scope；属于 R1/R5 的 receiver-side Protocol v1 decode parity，不改变 wire shape、UDP transport 或 source ingest ABI。

changes:
- `tests/DesktopLoudnessMeter.Tests/Audio/NativeLoopbackProtocolTests.cs` 增加 `NonAsciiHandleUsesCharacterLimitThroughNativeProtocol`。
- `src/DesktopLoudnessMeter.Native/DlmLoopbackProtocol.cpp` 将 handle 长度检查改为 UTF-8 code point count <= 128，并拒绝无效 UTF-8 continuation bytes。

validation:
- `powershell -ExecutionPolicy Bypass -File src\DesktopLoudnessMeter.Native\build-native.ps1 -Configuration Release; dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~NativeLoopbackProtocolTests`: pass，native build 0 warnings/0 errors，16 tests passed。

docs_synced:
- No spec/design change required；该 amendment 落在既有 R1/R5 parity 和 handle validation contract 内。

pending:
- none。

gate_impact:
- `.openspec.yaml gates.verify.status` 已标记 `needs_rerun`，必须重新执行 verify 后才能进入 review/archive。

## verify | 2026-06-30T19:02:11+08:00 | pass_with_warnings

方向：第二次 implement-amendment 后重新验证当前工作树 diff -> 验证通过，保留环境限制 warning。

证据：
- `powershell -ExecutionPolicy Bypass -File src\DesktopLoudnessMeter.Native\build-native.ps1 -Configuration Release`: pass, `DlmLoopbackProtocol.cpp` 重新编译并链接，0 warnings, 0 errors。
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~NativeLoopbackProtocolTests`: pass, 16 passed；包含 escaped non-BMP displayName 和 non-ASCII 128-character handle regressions。
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter "FullyQualifiedName~NativeLoopbackProtocolTests|FullyQualifiedName~LocalLoopbackProtocolTests|FullyQualifiedName~LocalLoopbackUdpReceiverTests|FullyQualifiedName~LocalLoopbackSignalSourceServiceTests"`: pass, 38 passed, 2 skipped。
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj`: pass, 353 passed, 5 skipped。
- `dotnet build src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj`: pass, 0 warnings, 0 errors。
- `git diff --check`: exit 0；仅 LF->CRLF working-copy warnings，无 whitespace errors。
- `npm --prefix src\DesktopLoudnessMeter.Web test -- --run`: pass, 6 test files, 25 tests（18:57 同 change verify 轮次，amendment 后未触及 Web files）。
- `npm --prefix src\DesktopLoudnessMeter.Web run build`: pass, Vite production build succeeded（18:58 同 change verify 轮次，amendment 后未触及 Web files）。
- `where.exe cmake`: exit 1, `INFO: Could not find files for the given pattern(s).`
- `Test-Path C:\JUCE\CMakeLists.txt`: `False`。
- Git: branch=`codex/2026-06-30-native-protocol-ingest-core`; HEAD=`845416989cf6c4393d86b297a0e44eaec93d6a74`; base=`codex/2026-06-30-web-settings-workflow-parity` / `845416989cf6c4393d86b297a0e44eaec93d6a74`; diff_status=working tree contains owned implementation/test/knowledge/OpenSpec files; verification_coverage=worktree diff after second amendment; manual_status=waived; stabilization_status=none; amendment_status=reverified。

allowed_failures:
- item: JUCE binary tests not run because `cmake` is unavailable on PATH and `C:\JUCE\CMakeLists.txt` does not exist.
  why_non_blocking: 本 change 未修改 JUCE sender code 或 sender wire shape；C# sender-side wire oracle、native receiver decode/ack tests、receiver/source-service tests、native Release build、managed regression、app build 和 Web tests/build 均通过；没有声明 JUCE binary pass。
  owner_or_followup: 后续 JUCE build environment / release smoke lane；任何修改 JUCE sender lifecycle 或 Protocol v1 sender implementation 的 change 必须复查。
  expiry_or_recheck: 当 CMake/JUCE runner 可用时，或进入 release gate / JUCE sender 相关 change 前重新运行。
- item: Real UDP loopback sender/receiver socket smoke remains skipped/waived in this local environment.
  why_non_blocking: 当前仓库已记录本环境 127.0.0.1 UDP datagrams 不可靠；本 change 的协议、receiver route 和 source ingest 行为由 deterministic tests 覆盖，Host UDP socket ownership 未被迁移。
  owner_or_followup: Windows platform smoke lane / packaging-startup integration follow-up。
  expiry_or_recheck: 在稳定 Windows loopback UDP 环境、CI smoke 环境或发布前平台验证时重新运行。

问题：
- 未发现阻断项。
- 保留两个非阻断 warning：JUCE/CMake runner 不可用、真实 UDP socket smoke 本地豁免。

需修正：
- 无。

## Review Evidence

## review | 2026-06-30T19:03:19+08:00 | pass_with_warnings

Review Scope Snapshot:
- branch: `codex/2026-06-30-native-protocol-ingest-core`
- HEAD: `845416989cf6c4393d86b297a0e44eaec93d6a74`
- diff_status: working tree contains owned implementation/test/knowledge/OpenSpec files; no unrelated dirty files observed.
- verify_gate: `.openspec.yaml gates.verify.status=pass_with_warnings`, `at=2026-06-30T19:02:11+08:00`。
- reviewed scope: native protocol parser/ABI, managed P/Invoke wrapper, receiver route migration, native build entries, focused tests, codemap updates, verify evidence and manual/tooling warnings。

findings:
- CRITICAL: 无发现。
- WARNING: 无新增代码级 release blocker。
- SUGGESTION: 无需要阻断归档的建议项。

risk review notes:
- API/ABI: `DlmLoopbackProtocol*Export` functions are added through `DesktopLoudnessMeterNative.cpp`; managed wrapper uses explicit Cdecl DllImport and always calls `DlmLoopbackProtocolDestroyPacketExport` in `finally` after successful decode.
- serialization/schema: review 期间发现并修复了两个 receiver-side parity risks：escaped non-BMP JSON displayName surrogate pair handling、非 ASCII handle 128-character compatibility。两者均有 focused regression，并在最终 verify 重新通过。
- resource lifecycle: native parse result remains caller-destroyed; invalid decode returns no packet handle; receiver keeps UDP socket ownership and catches expected send/decode failures without changing socket lifecycle。
- behavior boundary: Host still owns loopback remote filtering and UDP send/receive; protocol bytes and ack payload generation now route through native core as intended。
- VERIFY_DRIFT: 未发现当前 gate 与工作树 diff 的直接冲突；evidence 中的历史 `needs_rerun` 只属于 amendment 记录，当前 gate 已重新通过。

allowed_failures:
- item: JUCE binary tests not run because `cmake` is unavailable on PATH and `C:\JUCE\CMakeLists.txt` does not exist.
  why_non_blocking: 本 change 未修改 JUCE sender code 或 sender wire shape；sender-side oracle、native receiver tests、receiver/source-service tests、native Release build、managed regression、app build 和 Web tests/build 均通过；没有声明 JUCE binary pass。
  owner_or_followup: 后续 JUCE build environment / release smoke lane；任何修改 JUCE sender lifecycle 或 Protocol v1 sender implementation 的 change 必须复查。
  expiry_or_recheck: 当 CMake/JUCE runner 可用时，或进入 release gate / JUCE sender 相关 change 前重新运行。
- item: Real UDP loopback sender/receiver socket smoke remains skipped/waived in this local environment.
  why_non_blocking: 当前仓库已记录本环境 127.0.0.1 UDP datagrams 不可靠；本 change 的协议、receiver route 和 source ingest 行为由 deterministic tests 覆盖，Host UDP socket ownership 未被迁移。
  owner_or_followup: Windows platform smoke lane / packaging-startup integration follow-up。
  expiry_or_recheck: 在稳定 Windows loopback UDP 环境、CI smoke 环境或发布前平台验证时重新运行。

diff_status:
- release risk review completed for current working-tree diff; no CRITICAL findings.

## Manual Checks

- [~] [manual] Real UDP loopback smoke for sender/receiver packets | 原因：当前仓库测试已记录本环境 127.0.0.1 UDP datagrams 不可靠；本 change 使用 deterministic protocol/receiver/source-service tests 作为 gate，真实 UDP smoke 不作为归档阻塞项。

## Archive Notes

## archive | 2026-06-30T19:04:16+08:00 | pass

归档摘要：
- change: `2026-06-30-native-protocol-ingest-core`
- archive_path: `openspec/changes/archive/2026-06-30-native-protocol-ingest-core`
- schema: `spec-driven`
- spec_count: 1
- gates: readiness=`pass`, verify=`pass_with_warnings`, review=`pass_with_warnings`, archive=`pass`
- archived_status: `.openspec.yaml status=archived`

归档前检查：
- `tasks.md` 无未完成 checkbox。
- verify/review 均为有效通过状态，且 `pass_with_warnings` 均包含 structured `allowed_failures`。
- `Manual Checks` 中真实 UDP smoke 为 `[~]` 且含原因；无待验 `[ ] [manual]` blocker。
- codemap/log 已在本 change 内更新，记录 Host UDP glue -> native protocol decode/ack -> managed packet -> native source ingest 边界。

## Final Gate Summary

archive_path: `openspec/changes/archive/2026-06-30-native-protocol-ingest-core`

final_status:
- readiness: pass at `2026-06-30T18:36:35+08:00`
- verify: pass_with_warnings at `2026-06-30T19:02:11+08:00`
- review: pass_with_warnings at `2026-06-30T19:03:19+08:00`
- archive: pass at `2026-06-30T19:04:16+08:00`

allowed_failures:
- item: JUCE binary tests not run because `cmake` is unavailable on PATH and `C:\JUCE\CMakeLists.txt` does not exist.
  why_non_blocking: 本 change 未修改 JUCE sender code 或 sender wire shape；sender-side oracle、native receiver tests、receiver/source-service tests、native Release build、managed regression、app build 和 Web tests/build 均通过；没有声明 JUCE binary pass。
  owner_or_followup: 后续 JUCE build environment / release smoke lane；任何修改 JUCE sender lifecycle 或 Protocol v1 sender implementation 的 change 必须复查。
  expiry_or_recheck: 当 CMake/JUCE runner 可用时，或进入 release gate / JUCE sender 相关 change 前重新运行。
- item: Real UDP loopback sender/receiver socket smoke remains skipped/waived in this local environment.
  why_non_blocking: 当前仓库已记录本环境 127.0.0.1 UDP datagrams 不可靠；本 change 的协议、receiver route 和 source ingest 行为由 deterministic tests 覆盖，Host UDP socket ownership 未被迁移。
  owner_or_followup: Windows platform smoke lane / packaging-startup integration follow-up。
  expiry_or_recheck: 在稳定 Windows loopback UDP 环境、CI smoke 环境或发布前平台验证时重新运行。

manual_followups:
- item: Real UDP loopback sender/receiver socket smoke
  status: waived_non_blocking
  reason: 当前本机 loopback UDP datagrams 不可靠；deterministic protocol/receiver/source-service tests 覆盖本 change 的协议与路由行为。
  owner_or_followup: Windows platform smoke lane / packaging-startup integration follow-up。
  expiry_or_recheck: 稳定 UDP loopback 环境、CI smoke 环境或发布前平台验证时复查。

knowledge_status:
- status: done
- evidence_ref: `.aiknowledge/codemap/index.md`, `.aiknowledge/codemap/signal-source-ingest.md`, `.aiknowledge/codemap/native-dsp.md`, `.aiknowledge/codemap/juce-sender.md`, `.aiknowledge/codemap/audio-capture-sources.md`, `.aiknowledge/codemap/chains/pcm-metric-flow.md`, `.aiknowledge/logs/2026-06.md`
- pre_knowledge_status: codemap knew native source ingest existed but receiver-side Protocol v1 decode/ack remained C# Host glue.
- post_knowledge_status: codemap records native `DlmLoopbackProtocol` for receiver-side Protocol v1 decode/ack, while Host keeps UDP socket/loopback filtering and JUCE/SenderKit remain sender-side.
- diff_stat: codemap/log files updated in this change; no `.aiknowledge/pitfalls` change needed.
