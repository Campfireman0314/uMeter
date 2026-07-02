# Evidence: 2026-06-29-juce-sender-protocol-boundary

## Intake Evidence

### 2026-06-29 - required round intake

Commands:
- `opsx changes list`
  - result: `无活动变更`
- `Get-Content .aiknowledge/codemap/index.md`
  - result: read codemap index; relevant modules include `juce-sender`, `signal-source-ingest`, `native-dsp`, `audio-capture-sources`, `pcm-metric-flow`, `same-handle-reconfiguration`.
- `Get-ChildItem openspec/changes/archive/2026-06-29-cross-platform-architecture-plan`
  - result: archived architecture planning artifacts exist.
- `Get-Content openspec/changes/archive/2026-06-29-ui-runtime-architecture-replan/design.md`
  - result: WebView route supersedes older Flutter planning route.
- `git status --short --branch`
  - result before branch switch: clean tree on `codex/2026-06-29-native-source-ingest-core`, ahead 1.

Key facts:
- Current route is WebView UI + C++ Core, not Flutter.
- `LocalLoopbackUdpReceiver` still owns UDP socket/decode/routing in C# host.
- `DlmSourceIngestCore` owns typed source lifecycle/audio ingest, not UDP JSON parser.
- `LoopbackProtocol` exists in both C# Protocol and JUCE plugin source.

## Explore Convergence

Self-check summary:
- Candidate recommendation: create `2026-06-29-juce-sender-protocol-boundary`.
- Round 1 scan: blocking=none; non-blocking=older architecture plan still contains Flutter historical text, but `ui-runtime-architecture-replan` is the current route; UDP loopback smoke is skipped in current tests, so deterministic contract tests must be the main verification gate.
- Evidence vs inference:
  - Evidence: source files and tests listed in work/design.
  - Inference: the safest current native protocol boundary is typed event seam rather than moving JSON parser to native in this change.
- Final scan conclusion: no new factual blocker; route to `opsx-plan`.

## Plan Evidence

### 2026-06-29 - plan artifacts initialized

Commands:
- `git switch -c codex/2026-06-29-juce-sender-protocol-boundary`
  - result: created and switched to new branch.
- `opsx changes init 2026-06-29-juce-sender-protocol-boundary spec-driven --profile strict-change --source-type feature`
  - result: initialized change root.

Plan decision:
- Split conclusion: `KEEP_ONE_CHANGE`.
- Profile: `strict-change`.
- Reason: public protocol boundary spans C# Protocol, JUCE sender, SenderKit, host receiver and native ingest seam; one vocabulary decision must be verified across all of them.

## Readiness Evidence

### 2026-06-29T20:24:52.8683152+08:00 - pass

StageResult:
- stage: `opsx-readiness`
- status: `pass`
- branch: `codex/2026-06-29-juce-sender-protocol-boundary`
- head: `f1367ff615ca6b3a46e6e6df9759fbf1a5b3c8b4`
- change: `2026-06-29-juce-sender-protocol-boundary`
- profile: `strict-change`

Checks:
- work.md: pass. Contains goal, scope, non-goals, dependencies, success criteria, verification plan, rollback, bugfix/root-cause context, readiness notes, workspace strategy and split decision.
- specs/change/spec.md: pass. Defines R1-R5 with globally unique trace ids and observable scenarios.
- design.md: pass. Covers R1-R5 through U1-U6, each implementation unit includes `模块边界`, and design records risks, decisions and rollback strategy.
- tasks.md: pass. Uses Default Constraints, task-level requirement trace, execution mode, allowed files, acceptance criteria, verification method, evidence required and done condition.
- workspace strategy: pass. Current worktree is clean except this change root after planning; branch is dedicated; base is previous native source ingest checkpoint `f1367ff615ca6b3a46e6e6df9759fbf1a5b3c8b4`.
- profile: pass. `strict-change` matches public protocol/cross-language/native boundary risk.
- manual checks: pass. Real UDP loopback smoke is marked `[~]` with reason; deterministic tests are the main gate.

Tool/framework probe:
- `opsx changes list`
  - result: active change routes to `next=opsx-readiness` before gate write.
- `where.exe cmake; where.exe dotnet; where.exe npm`
  - result: `dotnet` and `npm` found; `cmake` not found on PATH in this shell. JUCE runner availability must be probed again during implement/verify and recorded without claiming pass if unavailable.

Decision:
- readiness passed; route to `opsx-implement`.

## Implementation Evidence

## test-cycle | 2026-06-29T20:27:56.9926538+08:00 | pass
任务：Task 1 - Characterize and centralize C# Protocol v1 vocabulary
执行方式：characterization-first

### 红阶段
- 失败测试列表：
  - `LocalLoopbackProtocolTests.AudioPacketRoundTripsFloatSamples` - `Assert.Equal`: 初次 characterization 期望的 base64 写成了 `AACAPwAAAL8=`，实际 wire payload 是 `AACAPgAAAL8=`。
- 假红修正：是。修正测试期望值为 `0.25f, -0.5f` 的真实 little-endian float32 base64：`AACAPgAAAL8=`。
- characterization 旧行为：新增 JSON field/value 断言后，当前实现通过，证明测试锁定的是既有 v1 wire shape。

### 绿阶段
- 通过测试列表：
  - `LocalLoopbackProtocolTests.RegisterPacketRoundTrips`
  - `LocalLoopbackProtocolTests.AudioPacketRoundTripsFloatSamples`
  - `LocalLoopbackProtocolTests.SimpleControlPacketsRoundTrip`
  - `LocalLoopbackProtocolTests.RenamePacketRoundTrips`
  - `LocalLoopbackProtocolTests.InvalidPacketsAreRejected`
- 命令：
  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~LocalLoopbackProtocolTests`
  - 结果：11 passed, 0 failed, 0 skipped.
- 变更摘要：
  - `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackProtocolTests.cs` - 修改 - 增加 canonical v1 JSON field/value 和 audio base64 wire-shape characterization。
  - `src/DesktopLoudnessMeter.Protocol/LocalLoopbackProtocol.cs` - 修改 - 增加 `PacketTypes`、`SourceKinds`、`Fields` centralized vocabulary，并保持 public encode/decode API 不变。
- 测试变更记录：
  - 新增 register/audio/simple/rename wire field assertions。
  - 保留 invalid packet rejection tests。
- characterization 目标行为：旧断言（round-trip only） -> 新断言（round-trip + canonical JSON shape），原因是本 change 需要防止 C# / JUCE / SenderKit v1 vocabulary 漂移。

| 验收标准 (AC) | 测试/证据 | 状态 |
|--------------|-----------|------|
| canonical v1 JSON shape locked | `LocalLoopbackProtocolTests` focused run | pass |
| centralized vocabulary/DTO boundary | `LocalLoopbackProtocol.PacketTypes/SourceKinds/Fields` | pass |
| public API signatures remain compatible | existing callers compile in focused test build | pass |
| invalid packets rejected | `LocalLoopbackProtocolTests.InvalidPacketsAreRejected` | pass |

### 重构阶段
- 通过测试列表：
  - `LocalLoopbackProtocolTests` focused suite.
- 变更摘要：
  - `LocalLoopbackProtocol.cs` - 修改 - encode/decode now consumes centralized vocabulary constants for packet types and source kinds.
- 覆盖率：N/A（不可用原因：本仓库 focused `dotnet test` 未启用 coverage collection；补救：后续 final verification 运行 full managed tests）。

## test-cycle | 2026-06-29T20:30:35.9221448+08:00 | pass_with_tooling_limit
任务：Task 2 - Align JUCE protocol contract with C# v1 vocabulary
执行方式：characterization-first

### 红阶段
- 失败测试列表：
  - 无可执行红阶段：本机缺少 `cmake`，且 `C:\JUCE\CMakeLists.txt` 不存在，无法生成/运行 JUCE test binary。
- characterization 旧行为：
  - `LoopbackProtocolTests.cpp` 已存在 v1 shape、audio little-endian base64 和 basic ack matching tests。
  - `LoopbackSenderTests.cpp` 已存在 same-handle restart no-close-before-register 与 explicit stop close coverage。

### 绿阶段
- 源码契约覆盖：
  - `LoopbackProtocolTests.cpp` 新增 heartbeat/rename/close/ack protocol+handle field assertions。
  - `LoopbackProtocolTests.cpp` 新增 ack rejection cases：wrong protocol、wrong type、wrong handle、invalid JSON、null data、empty data。
  - `LoopbackProtocol.h/.cpp` 新增 `PacketTypes`、`SourceKinds`、`Fields` centralized vocabulary，并保持 emitted JSON field names/values 不变。
  - `LoopbackSenderTests.cpp` 继续包含 same-handle restart guard 和 explicit stop close guard。
- 工具链探测：
  - `where.exe cmake` -> `INFO: Could not find files for the given pattern(s).`
  - `Test-Path C:\JUCE\CMakeLists.txt` -> `False`
- 替代检查：
  - `rg -n "wrong protocol|wrong type|wrong handle|invalid JSON|null data|same-handle|must not emit close|explicit stop|encodeAck|PacketTypes|Fields|SourceKinds" src\DesktopLoudnessMeter.JucePlugin\Source src\DesktopLoudnessMeter.JucePlugin\Tests`
  - 结果：命中 JUCE vocabulary constants、ack rejection tests、same-handle restart guard 和 explicit stop close guard。

| 验收标准 (AC) | 测试/证据 | 状态 |
|--------------|-----------|------|
| JUCE tests assert v1 field names/values | `LoopbackProtocolTests.cpp` source evidence | pass |
| ack rejects wrong protocol/type/handle/invalid/null/empty | `ackMatchingRejectsInvalidPackets` source evidence | pass |
| same-handle restart no close before register | `LoopbackSenderTests.cpp` source evidence | pass |
| explicit stop emits close | `LoopbackSenderTests.cpp` source evidence | pass |

### 重构阶段
- 通过测试列表：
  - JUCE binary tests not run; toolchain unavailable in this shell.
  - C# Protocol focused tests still pass after shared vocabulary work: 11 passed, 0 failed, 0 skipped.
- 变更摘要：
  - `src/DesktopLoudnessMeter.JucePlugin/Source/LoopbackProtocol.h` - 修改 - centralized v1 packet/field/source-kind constants.
  - `src/DesktopLoudnessMeter.JucePlugin/Source/LoopbackProtocol.cpp` - 修改 - packet encoding and ack matching consume centralized constants.
  - `src/DesktopLoudnessMeter.JucePlugin/Tests/LoopbackProtocolTests.cpp` - 修改 - expanded wire-shape and ack rejection contract coverage.
- 覆盖率：N/A（不可用原因：JUCE runner/toolchain unavailable；补救：final verification will record the same probe and avoid claiming binary JUCE pass）。

## test-cycle | 2026-06-29T20:31:50.4851742+08:00 | pass
任务：Task 3 - Preserve SenderKit as sender-side protocol companion
执行方式：characterization-first

### 红阶段
- 失败测试列表：无。Task 3 固化现有 SenderKit 边界，focused deterministic tests 已存在。
- characterization 旧行为：
  - `LocalLoopbackSenderTests.AudioHandoffDropsInsteadOfBlockingWhenPoolIsFull`
  - `LocalLoopbackSenderTests.OversizedFrameChunksAreRejectedForUdpJsonTransport`
  - invalid handle tests

### 绿阶段
- 命令：
  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~LocalLoopbackSenderTests`
  - 结果：5 passed, 0 failed, 2 skipped. 两个 skipped tests 是真实 UDP loopback smoke，已有 skip reason。
- 边界检查：
  - `rg -n "LocalLoopbackProtocol\.Encode|LocalLoopbackUdpReceiver|LocalLoopbackSignalSourceService|NativeSourceIngestCore|DlmSourceIngestCore|DesktopLoudnessMeter.App|DesktopLoudnessMeter.Native" src\DesktopLoudnessMeter.SenderKit tests\DesktopLoudnessMeter.Tests\SenderKit`
  - 结果：`src\DesktopLoudnessMeter.SenderKit\LocalLoopbackSender.cs` 只命中 `LocalLoopbackProtocol.Encode*`; host/native references only appear in skipped integration tests.
  - `src\DesktopLoudnessMeter.SenderKit\DesktopLoudnessMeter.SenderKit.csproj` only references `DesktopLoudnessMeter.Protocol`.
- 变更摘要：
  - 无 SenderKit 产品代码修改；Task 1 的 C# Protocol vocabulary centralization is transparent to SenderKit.

| 验收标准 (AC) | 测试/证据 | 状态 |
|--------------|-----------|------|
| SenderKit encodes through Protocol | source grep + project reference check | pass |
| backpressure/drop non-blocking | `LocalLoopbackSenderTests` focused run | pass |
| oversized UDP JSON guard covered | `LocalLoopbackSenderTests` focused run | pass |
| no host/native ingest references in SenderKit | source grep + `.csproj` | pass |

### 重构阶段
- 通过测试列表：
  - `LocalLoopbackSenderTests` focused suite.
- 变更摘要：无产品代码重构。
- 覆盖率：N/A（不可用原因：focused `dotnet test` 未启用 coverage collection；补救：后续 final verification 运行 full managed tests）。

## test-cycle | 2026-06-29T20:34:35.4062061+08:00 | pass
任务：Task 4 - Add deterministic receiver-to-native ingest seam coverage
执行方式：test-first

### 红阶段
- 失败测试列表：
  - `LocalLoopbackUdpReceiverTests.RouteReceivedPacketIgnoresInboundAckWithoutAckResponse` - `CS0117`: `LocalLoopbackUdpReceiver` did not contain `RouteReceivedPacket`.
  - `LocalLoopbackUdpReceiverTests.RouteReceivedPacketRoutesValidNonAckFromLoopback` - `CS0117`: same missing helper.
  - `LocalLoopbackUdpReceiverTests.RouteReceivedPacketIgnoresNonLoopbackRemote` - `CS0117`: same missing helper.
- 假红修正：无；失败原因是产品代码尚未提供可测试 routing seam。

### 绿阶段
- 命令：
  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~LocalLoopbackUdpReceiverTests`
  - 结果：4 passed, 0 failed, 2 skipped. Skipped tests are existing real UDP loopback smoke with explicit reason.
  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~LocalLoopbackSignalSourceServiceTests`
  - 结果：6 passed, 0 failed, 0 skipped.
  - `rg -n "ProcessJson|Json|Udp|Datagram|Socket|Receive|protocol" src\DesktopLoudnessMeter.Native\DlmSourceIngestCore.h src\DesktopLoudnessMeter.Native\DlmSourceIngestCore.cpp`
  - 结果：no matches; native source ingest did not gain UDP/JSON parser API.
- 变更摘要：
  - `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackUdpReceiver.cs` - 修改 - added internal `RouteReceivedPacket` helper and used it in receive loop.
  - `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackUdpReceiverTests.cs` - 修改 - added deterministic non-socket routing tests for inbound ack, valid non-ack and non-loopback remote.
- 测试变更记录：
  - 新增 tests use `LocalLoopbackProtocol.Encode*` and in-memory routed packet list; no socket dependency.

| 验收标准 (AC) | 测试/证据 | 状态 |
|--------------|-----------|------|
| inbound ack ignored | `RouteReceivedPacketIgnoresInboundAckWithoutAckResponse` | pass |
| valid non-ack routed | `RouteReceivedPacketRoutesValidNonAckFromLoopback` | pass |
| non-loopback remote ignored | `RouteReceivedPacketIgnoresNonLoopbackRemote` | pass |
| native source ingest remains typed | native grep no-match | pass |

### 重构阶段
- 通过测试列表：
  - `LocalLoopbackUdpReceiverTests` focused suite.
  - `LocalLoopbackSignalSourceServiceTests` focused suite.
- 变更摘要：
  - Receiver production loop now delegates decode/routing decision to the tested helper; public receiver API unchanged.
- 覆盖率：N/A（不可用原因：focused `dotnet test` 未启用 coverage collection；补救：后续 final verification 运行 full managed tests）。

## test-cycle | 2026-06-29T20:37:36.6551764+08:00 | pass
任务：Task 5 - Refresh codemap and verification evidence
执行方式：direct

### 直接验证
- 跳过测试循环理由：codemap / knowledge 文档更新，无产品运行时行为。
- 验证命令 / 方法：
  - `rg -n "Protocol v1|typed ingest|LoopbackProtocol|SenderKit|DlmSourceIngestCore|UDP JSON" .aiknowledge\codemap`
  - 结果：命中 `juce-sender`、`signal-source-ingest`、`pcm-metric-flow`、`same-handle-reconfiguration`、`index` 中更新后的边界描述。
- 变更摘要：
  - `.aiknowledge/codemap/index.md` - 修改 - refreshed juce-sender, audio-capture-sources and same-handle entries.
  - `.aiknowledge/codemap/juce-sender.md` - 修改 - documented Protocol v1 vocabulary emission, JUCE sender boundary and JUCE runner limitation.
  - `.aiknowledge/codemap/signal-source-ingest.md` - 修改 - documented host receiver UDP JSON glue and native typed ingest seam.
  - `.aiknowledge/codemap/chains/pcm-metric-flow.md` - 修改 - appended protocol boundary update.
  - `.aiknowledge/codemap/chains/same-handle-reconfiguration.md` - 修改 - updated register path to native source ingest.
  - `.aiknowledge/logs/2026-06.md` - 修改 - added monthly knowledge log entry.

| 验收标准 (AC) | 证据 | 状态 |
|--------------|------|------|
| Protocol v1 vocabulary authority and sender boundary recorded | codemap grep | pass |
| receiver to native typed ingest seam recorded | codemap grep | pass |
| no claim that native owns UDP JSON parser | codemap grep shows native remains typed event boundary | pass |

## test-cycle | 2026-06-29T20:39:35.9886504+08:00 | pass_with_tooling_limit
浠诲姟锛欶inal Verification - implementation handoff to opsx-verify
鎵ц鏂瑰紡锛歞irect

### 鏈€缁堝疄鐜伴樁楠岃瘉
- Task-level focused checks recorded:
  - `LocalLoopbackProtocolTests` focused run: 11 passed.
  - `LocalLoopbackUdpReceiverTests` focused run: 4 passed, 2 skipped.
  - `LocalLoopbackSignalSourceServiceTests` focused run: 6 passed.
  - `LocalLoopbackSenderTests` focused run: 5 passed, 2 skipped.
  - Combined protocol/receiver/sender/source-service focused run: 26 passed, 4 skipped.
- Native Release build:
  - `powershell -ExecutionPolicy Bypass -File src\DesktopLoudnessMeter.Native\build-native.ps1 -Configuration Release`
  - Result: pass; MSBuild completed with 0 warnings, 0 errors.
- Full managed test project:
  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj`
  - Result: pass; 314 passed, 0 failed, 5 skipped.
- App build:
  - `dotnet build src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj`
  - Result: pass; 0 warnings, 0 errors.
- Web checks:
  - `npm --prefix src\DesktopLoudnessMeter.Web test -- --run`
  - Result: pass; 5 files, 11 tests.
  - `npm --prefix src\DesktopLoudnessMeter.Web run build`
  - Result: pass.
- JUCE route:
  - `where.exe cmake` -> `INFO: Could not find files for the given pattern(s).`
  - `Test-Path C:\JUCE\CMakeLists.txt` -> `False`
  - Result: tooling limitation recorded. JUCE binary tests were not run and are not claimed as passed; source-level protocol contract coverage was added and inspected.

| 楠屾敹鏍囧噯 (AC) | 璇佹嵁 | 鐘舵€?|
|--------------|------|------|
| All task-level focused checks recorded | Task 1-5 test-cycle entries | pass |
| Native Release build passes | native build command | pass |
| Full managed test project passes | `dotnet test` full project | pass |
| App build passes | app `dotnet build` | pass |
| Web tests/build pass | npm test/build | pass |
| JUCE route recorded without false pass | cmake/JUCE probe | pass_with_tooling_limit |

## Verification Evidence

## verify | 2026-06-29T20:44:05.6064829+08:00 | pass_with_warnings
方向：tasks.md + specs/change/spec.md + design.md + implementation diff -> verify gate passed with documented non-blocking environment/toolchain limits.

证据：
- `opsx changes resolve 2026-06-29-juce-sender-protocol-boundary` -> `/e/gitlab/uMeter/openspec/changes/2026-06-29-juce-sender-protocol-boundary`.
- `opsx changes list` -> active change routes to `next=opsx-verify` before gate write.
- `docs/stage-packet-protocol.md` lookup -> not found in this repository; verify used the StageResult field expectations from `opsx-verify` skill instructions.
- `git diff --stat` before gate write -> 13 product/knowledge files plus the active change root, all attributable to this change.
- `rg -n "\[ \]|\[~\]|TODO|Pending" tasks.md evidence.md` -> no unchecked task ACs; one `[~]` manual real UDP smoke item with reason; review/archive sections still pending as expected before later gates.
- `powershell -ExecutionPolicy Bypass -File src\DesktopLoudnessMeter.Native\build-native.ps1 -Configuration Release` -> pass; 0 warnings, 0 errors.
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj` -> pass; 314 passed, 0 failed, 5 skipped.
- `dotnet build src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj` -> pass; 0 warnings, 0 errors.
- `npm --prefix src\DesktopLoudnessMeter.Web test -- --run` -> pass; 5 test files, 11 tests.
- `npm --prefix src\DesktopLoudnessMeter.Web run build` -> pass.
- JUCE toolchain probe: `where.exe cmake` -> not found; `Test-Path C:\JUCE\CMakeLists.txt` -> `False`.
- Git: branch=codex/2026-06-29-juce-sender-protocol-boundary; HEAD=f1367ff615ca6b3a46e6e6df9759fbf1a5b3c8b4; base=codex/2026-06-29-native-source-ingest-core at f1367ff615ca6b3a46e6e6df9759fbf1a5b3c8b4; merge_target=deferred to user; diff_status=unstaged product/knowledge/OpenSpec files all attributed to this change, no staged files; verification_coverage=worktree diff; manual_status=waived; stabilization_status=none; amendment_status=none.

StageResult:
```json
{
  "stage": "opsx-verify",
  "decision": "pass_with_warnings",
  "change": "2026-06-29-juce-sender-protocol-boundary",
  "branch": "codex/2026-06-29-juce-sender-protocol-boundary",
  "head": "f1367ff615ca6b3a46e6e6df9759fbf1a5b3c8b4",
  "findings": [],
  "allowed_failures": [
    {
      "item": "Real UDP loopback smoke for SenderKit/JUCE packets remains waived in this environment",
      "why_non_blocking": "The change's primary acceptance surface is deterministic Protocol, SenderKit and receiver routing contract/unit tests; current repository tests already mark 127.0.0.1 UDP smoke unreliable in this environment.",
      "owner_or_followup": "Follow up in a platform/network environment smoke pass before relying on real UDP smoke as a release gate.",
      "expiry_or_recheck": "Recheck when a stable local UDP loopback test environment or CI lane is available."
    },
    {
      "item": "JUCE binary tests were not run because CMake/JUCE runner is unavailable",
      "why_non_blocking": "JUCE source-level contract coverage was added and inspected, while managed/native/Web gates passed; no JUCE binary pass is claimed.",
      "owner_or_followup": "Follow up in a JUCE build environment with CMake and JUCE checkout installed.",
      "expiry_or_recheck": "Recheck before changing JUCE sender lifecycle beyond source-level vocabulary alignment."
    }
  ],
  "evidence": [
    "native Release build passed",
    "full managed tests passed",
    "app build passed",
    "web tests and build passed",
    "JUCE runner probe recorded as unavailable"
  ]
}
```

问题：
- 无 blocking findings.

需修正：
- 无。

## Review Evidence

## risk-review | 2026-06-29T20:46:20.4271789+08:00 | pass
Review Scope Snapshot:
- branch: `codex/2026-06-29-juce-sender-protocol-boundary`
- HEAD: `f1367ff615ca6b3a46e6e6df9759fbf1a5b3c8b4`
- base: `codex/2026-06-29-native-source-ingest-core`
- merge-base: `f1367ff615ca6b3a46e6e6df9759fbf1a5b3c8b4`
- committed diff: none beyond base; current release scope is the working-tree diff.
- staged diff: none.
- unstaged/untracked diff: product, test, OpenSpec and `.aiknowledge` files attributed to this change.
- verify gate: `pass_with_warnings` at `2026-06-29T20:44:05.6064829+08:00`; warnings are tool/environment limitations, not open implementation blockers.

Reviewed risk threads:
- Protocol v1 JSON vocabulary and compatibility:
  - `src/DesktopLoudnessMeter.Protocol/LocalLoopbackProtocol.cs:10`
  - `src/DesktopLoudnessMeter.Protocol/LocalLoopbackProtocol.cs:228`
  - Conclusion: constants are additive; public encode/decode method signatures remain unchanged; `Register` enum default remains covered by the existing special-case parse guard.
- JUCE packet emission and ack matching:
  - `src/DesktopLoudnessMeter.JucePlugin/Source/LoopbackProtocol.cpp:74`
  - `src/DesktopLoudnessMeter.JucePlugin/Source/LoopbackProtocol.cpp:129`
  - Conclusion: constants preserve emitted field names/values; ack matching still requires protocol, type and handle equality and rejects invalid/null/empty inputs in source-level tests.
- Host receiver routing and ack behavior:
  - `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackUdpReceiver.cs:55`
  - `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackUdpReceiver.cs:105`
  - `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackUdpReceiver.cs:111`
  - Conclusion: loopback filtering, decode failure handling and inbound ack ignore behavior are preserved; ack is sent only after ingest succeeds, matching the previous production ordering.
- Lifecycle pitfall check:
  - `.aiknowledge/pitfalls/lifecycle/publish-stop-mode-before-worker-exit.md`
  - Conclusion: this diff does not alter `LoopbackSender` stop/restart worker state; active pitfall is not reintroduced.

Findings:
- 无 CRITICAL findings.
- 无 WARNING findings.
- 无 SUGGESTION findings.

Exclusions:
- Did not rerun full spec compliance; that belongs to `opsx-verify`.
- Did not claim JUCE binary test pass; verify already records runner/toolchain limitation.

## Manual Checks

- [~] [manual] Real UDP loopback smoke for SenderKit/JUCE packets | 原因：当前测试环境已记录 127.0.0.1 UDP datagrams 不可靠；本 change 使用 deterministic contract/unit tests 覆盖主 gate，real UDP smoke 保持非阻塞。

## Archive Notes

## archive | 2026-06-29T20:48:40.2420838+08:00 | pass
归档结果：
- change: `2026-06-29-juce-sender-protocol-boundary`
- archive_path: `openspec/changes/archive/2026-06-29-juce-sender-protocol-boundary`
- specs_count: 1
- `.openspec.yaml`: `status: archived`; `gates.archive.status: pass`; `gates.archive.at: 2026-06-29T20:47:21.9035010+08:00`
- verify gate: `pass_with_warnings`
- review gate: `pass`
- manual gate: one `[~]` real UDP smoke item with reason; no `[ ]` manual blockers.
- knowledge auto: completed after archive; codemap source refs now point to the archived evidence path.

Knowledge auto evidence fields:
- pre_knowledge_status:
  - `M .aiknowledge/codemap/chains/pcm-metric-flow.md`
  - `M .aiknowledge/codemap/chains/same-handle-reconfiguration.md`
  - `M .aiknowledge/codemap/index.md`
  - `M .aiknowledge/codemap/juce-sender.md`
  - `M .aiknowledge/codemap/signal-source-ingest.md`
  - `M .aiknowledge/logs/2026-06.md`
- post_knowledge_status:
  - `M .aiknowledge/codemap/chains/pcm-metric-flow.md`
  - `M .aiknowledge/codemap/chains/same-handle-reconfiguration.md`
  - `M .aiknowledge/codemap/index.md`
  - `M .aiknowledge/codemap/juce-sender.md`
  - `M .aiknowledge/codemap/signal-source-ingest.md`
  - `M .aiknowledge/logs/2026-06.md`
- diff_stat:
  - `.aiknowledge/codemap/chains/pcm-metric-flow.md | 8 ++++++++`
  - `.aiknowledge/codemap/chains/same-handle-reconfiguration.md | 13 ++++++++-----`
  - `.aiknowledge/codemap/index.md | 6 +++---`
  - `.aiknowledge/codemap/juce-sender.md | 17 +++++++++++++----`
  - `.aiknowledge/codemap/signal-source-ingest.md | 5 +++++`
  - `.aiknowledge/logs/2026-06.md | 2 ++`

## Final Gate Summary

- archive_path: `openspec/changes/archive/2026-06-29-juce-sender-protocol-boundary`
- verify: `pass_with_warnings` at `2026-06-29T20:44:05.6064829+08:00`
- review: `pass` at `2026-06-29T20:46:20.4271789+08:00`
- archive: `pass` at `2026-06-29T20:47:21.9035010+08:00`
- manual_followups:
  - item: Real UDP loopback smoke for SenderKit/JUCE packets
    status: waived
    reason: 当前测试环境已记录 `127.0.0.1` UDP datagrams 不可靠；本 change 使用 deterministic contract/unit tests 覆盖主 gate。
    owner_or_followup: Follow up in a platform/network environment smoke pass before relying on real UDP smoke as a release gate.
    expiry_or_recheck: Recheck when a stable local UDP loopback test environment or CI lane is available.
- allowed_failures:
  - item: Real UDP loopback smoke for SenderKit/JUCE packets remains waived in this environment
    why_non_blocking: The change's primary acceptance surface is deterministic Protocol, SenderKit and receiver routing contract/unit tests; current repository tests already mark `127.0.0.1` UDP smoke unreliable in this environment.
    owner_or_followup: Follow up in a platform/network environment smoke pass before relying on real UDP smoke as a release gate.
    expiry_or_recheck: Recheck when a stable local UDP loopback test environment or CI lane is available.
  - item: JUCE binary tests were not run because CMake/JUCE runner is unavailable
    why_non_blocking: JUCE source-level contract coverage was added and inspected, while managed/native/Web gates passed; no JUCE binary pass is claimed.
    owner_or_followup: Follow up in a JUCE build environment with CMake and JUCE checkout installed.
    expiry_or_recheck: Recheck before changing JUCE sender lifecycle beyond source-level vocabulary alignment.
- knowledge_status:
  status: done
  evidence_ref: `Archive Notes -> Knowledge auto evidence fields`
  pre_knowledge_status: `.aiknowledge` already contained this change's codemap/log updates from implementation.
  post_knowledge_status: `.aiknowledge` codemap/log updates retained and archive evidence refs normalized.
  diff_stat: 6 `.aiknowledge` files changed, 39 insertions, 12 deletions.
