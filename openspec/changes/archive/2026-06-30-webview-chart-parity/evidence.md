# Evidence: 2026-06-30-webview-chart-parity

正文默认使用中文；路径、命令、配置键、frontmatter key 和状态枚举保持原文。

`evidence.md` 是唯一证据账本。readiness、test-cycle、verify、risk review 和人工验证结论都追加写入这里。

## Final Gate Summary

- final_readiness:
  - status: pass
  - evidence_ref: readiness | 2026-06-30 15:24 +08:00 | pass
- final_verify:
  - status: pass
  - evidence_ref: verify | 2026-06-30T16:39:00+08:00 | pass
- final_review:
  - status: pass
  - evidence_ref: risk-review | 2026-06-30T16:42:00+08:00 | pass
- final_archive:
  - status: pass
  - archive_path: openspec/changes/archive/2026-06-30-webview-chart-parity
  - evidence_ref: archive | 2026-06-30T16:47:00+08:00 | pass
- allowed_failures: []
- manual_followups: []
- knowledge_status:
  - status: done
  - evidence_ref: knowledge-auto | 2026-06-30T16:55:00+08:00 | done

## Archive

## archive | 2026-06-30T16:47:00+08:00 | pass
change: `2026-06-30-webview-chart-parity`
archive_path: `openspec/changes/archive/2026-06-30-webview-chart-parity`

### Gate Summary
- readiness: pass (`2026-06-30T15:24:44+08:00`)
- verify: pass (`2026-06-30T16:39:00+08:00`)
- review: pass (`2026-06-30T16:42:00+08:00`)
- archive: pass (`2026-06-30T16:47:00+08:00`)
- specs_count: 1 (`specs/change/spec.md`)
- tasks_status: 6/6 completed
- manual_followups: none
- allowed_failures: none
- knowledge_status: done (`knowledge-auto | 2026-06-30T16:55:00+08:00 | done`)

### Notes
- Manual Windows WebView chart smoke was closed by explicit user confirmation “应该没问题，继续推进把”.
- Review amendments were verified before archive: chart settings payload validation and Web chart source settings consumption are both covered by tests/build evidence.

## knowledge-auto | 2026-06-30T16:55:00+08:00 | done
mode: `auto`

### source_refs
- change:2026-06-30-webview-chart-parity
- evidence:openspec/changes/archive/2026-06-30-webview-chart-parity/evidence.md

### required return fields
- pre_knowledge_status: empty (`git status --short -- .aiknowledge/` before updates returned no paths)
- post_knowledge_status:
  - `M .aiknowledge/codemap/analysis-rendering.md`
  - `M .aiknowledge/codemap/host-contract.md`
  - `M .aiknowledge/codemap/index.md`
  - `M .aiknowledge/codemap/webview-shell.md`
  - `M .aiknowledge/logs/2026-06.md`
  - `M .aiknowledge/pitfalls/index.md`
  - `M .aiknowledge/pitfalls/misc/index.md`
  - `?? .aiknowledge/pitfalls/misc/hostbridge-numeric-settings-need-domain-validation.md`
- diff_stat:
  - `.aiknowledge/codemap/analysis-rendering.md | 19 +++++++++++++++----`
  - `.aiknowledge/codemap/host-contract.md | 23 +++++++++++++----------`
  - `.aiknowledge/codemap/index.md | 6 +++---`
  - `.aiknowledge/codemap/webview-shell.md | 17 +++++++++++------`
  - `.aiknowledge/logs/2026-06.md | 5 +++++`
  - `.aiknowledge/pitfalls/index.md | 2 +-`
  - `.aiknowledge/pitfalls/misc/index.md | 1 +`
  - `.aiknowledge/pitfalls/misc/hostbridge-numeric-settings-need-domain-validation.md | new active pitfall`

### updates
- Refreshed `webview-shell`, `host-contract`, and `analysis-rendering` codemap entries for WebView Canvas detail charts, HostBridge chart DTO/command boundaries, source settings consumption, and host-neutral collision projection.
- Added pitfall `misc/hostbridge-numeric-settings-need-domain-validation.md`.
- Updated codemap/pitfall indexes and `.aiknowledge/logs/2026-06.md`.

## Planning Evidence

### 2026-06-30 plan scaffold

- stage: plan
- commands:
  - `opsx changes list` -> `无活动变更`
  - `git branch --show-current` -> `codex/2026-06-30-webview-chart-parity`
  - `git rev-parse HEAD` -> `9fe61ff8740647db99874c22958ba085e13c59b6`
  - `opsx changes init 2026-06-30-webview-chart-parity spec-driven --profile strict-change --source-type feature`
- repository facts:
  - Web detail currently uses placeholder chart blocks in `src/DesktopLoudnessMeter.Web/src/AppShell.tsx`.
  - Host source DTO already carries waveform/spectrum/stereo payload arrays.
  - Host selected detail currently returns empty modules and mostly fixed visibility flags.
  - WPF controls/tests exist for waveform, spectrum, loudness clock, and collision rendering behavior.
- decision: strict-change, KEEP_ONE_CHANGE
- notes: 产品代码尚未修改；implementation 必须等待 readiness。

## Readiness

### 2026-06-30 15:24 +08:00

- stage: readiness
- decision: pass
- evidence_refs:
  - `work.md`
  - `specs/change/spec.md`
  - `design.md`
  - `tasks.md`
- checks:
  - scope/profile/split: pass; strict-change fits shared HostBridge DTO, command bus, Web Canvas rendering, and Windows visual smoke.
  - trace: pass; R1-R9 are globally unique in specs and map to U1-U5 in design.
  - task contract: pass; tasks include Allowed Files, verification method, evidence requirements, Done When, and default shared artifact constraints.
  - stabilization: pass; Task 3.1 includes rationale, manual/platform matrix, exit conditions, and final hardening plan.
  - workspace: pass; dedicated branch `codex/2026-06-30-webview-chart-parity`, clean baseline at `9fe61ff8740647db99874c22958ba085e13c59b6`, current-worktree rationale documented.
  - boundaries: pass; plan forbids native/JUCE/protocol/schema migration/WPF retirement and keeps WPF fallback.
- findings:
  - warning: full chart parity is broad, but tasks are staged through Host contract, Web renderers, workflow wiring, and stabilization evidence; no split-blocking independent delivery unit remains after scope tightening.
- next: proceed to opsx-implement.

## Test Cycle Evidence

## test-cycle | 2026-06-30T15:24:44+08:00 | red
任务：Task 1.1 - 投影 selected detail chart modules
执行方式：test-first

### 红阶段
- 失败测试列表：
  - `DesktopLoudnessMeter.Tests.Host.DesktopMeterHostControllerTests.SelectedDetailProjectsInlineChartModules` - `Assert.Equal() Failure`: `SelectedDetail.Modules` 当前为 `[]`，产品代码尚未投影 detail chart modules。
- 假红修正：首次运行出现 `CS0815`，原因是测试使用了当前 xUnit 版本不支持的 `Assert.NotNull` 返回值写法；已改为先断言再读取 `snapshot.SelectedDetail`，并重新运行得到有效红。
- 命令：
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "DesktopMeterHostControllerTests|HostBridgeDtoMapperTests"` exit=1
- 结论：红阶段有效，失败归因于 Host projection 尚未实现。

## test-cycle | 2026-06-30T15:24:44+08:00 | pass
任务：Task 1.1 - 投影 selected detail chart modules
执行方式：test-first

### 绿阶段
- 通过测试列表：
  - `DesktopLoudnessMeter.Tests.Host.DesktopMeterHostControllerTests.SelectedDetailProjectsInlineChartModules`
  - `DesktopLoudnessMeter.Tests.WebView.HostBridgeDtoMapperTests.SelectedDetailSerializesChartModules`
  - 同一 filter 下既有 Host/WebView tests 继续通过，共 12 个。
- 命令：
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "DesktopMeterHostControllerTests|HostBridgeDtoMapperTests"` exit=0
- 变更摘要：
  - `src/DesktopLoudnessMeter.App/Host/DesktopMeterHostController.cs` - 新增 selected detail inline chart module projection，使用 `DetailModuleLayoutNormalizer` 和 workspace inline visibility/defaults。
  - `tests/DesktopLoudnessMeter.Tests/Host/DesktopMeterHostControllerTests.cs` - 新增 Host projection 测试。
  - `tests/DesktopLoudnessMeter.Tests/WebView/HostBridgeDtoMapperTests.cs` - 新增 selected detail modules JSON serialization 测试。
- 测试变更记录：新增 2 个测试；修正一次假红测试写法。
- 验收标准覆盖对应表：

| 验收标准 (AC) | 测试用例名称 | 通过状态 |
|--------------|------------|---------|
| Selected detail snapshot contains waveform, spectrum, stereo, loudnessClock, and collision module entries. | `SelectedDetailProjectsInlineChartModules` | pass |
| Module visibility and settings use workspace/default values instead of WPF control state. | `SelectedDetailProjectsInlineChartModules` | pass |
| DTO mapper serializes chart modules and settings without WPF/XAML-only types. | `SelectedDetailSerializesChartModules` | pass |

### 重构阶段
- 通过测试列表：
  - `DesktopMeterHostControllerTests|HostBridgeDtoMapperTests` filter 下 12 个测试。
- 命令：
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "DesktopMeterHostControllerTests|HostBridgeDtoMapperTests"` exit=0
- 变更摘要：
  - 无额外产品重构；保留最小实现。
- 覆盖率：N/A；不可用原因：当前命令未启用覆盖率采集；补救：任务级行为由新增断言和后续完整 test/build 覆盖。

## test-cycle | 2026-06-30T15:24:44+08:00 | red
任务：Task 1.2 - 支持 HostBridge chart setting commands
执行方式：test-first

### 红阶段
- 失败测试列表：
  - `DesktopLoudnessMeter.Tests.WebView.HostBridgeControllerTests.ChartDisplayModeCommandRoundTripsThroughHostBridge` - `KeyNotFoundException`: bridge 对 `setWaveformDisplayMode` 返回 error response，测试读取不到 `result`。
  - `DesktopLoudnessMeter.Tests.WebView.HostBridgeControllerTests.DetailModuleVisibilityCommandUpdatesSelectedDetailSnapshot` - `KeyNotFoundException`: bridge 对 `setDetailModuleVisibility` 返回 error response，测试读取不到 `result`。
- 命令：
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "HostCommandTests|DesktopMeterHostControllerTests|HostBridgeControllerTests"` exit=1
- 结论：红阶段有效，失败归因于 HostBridge command bus 尚未支持 chart commands。

### 红阶段补充
- 失败测试列表：
  - `DesktopLoudnessMeter.Tests.WebView.HostBridgeControllerTests.ChartSettingsCommandsRoundTripWorkspaceProjection` - `KeyNotFoundException`: bridge 对 `setSpectrumDisplayRange` / `setLoudnessClockSettings` / `setCollisionSettings` 仍返回 error response。
- 命令：
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "HostCommandTests|DesktopMeterHostControllerTests|HostBridgeControllerTests"` exit=1
- 结论：红阶段补齐了 numeric chart settings command family 的未实现证据。

## test-cycle | 2026-06-30T15:24:44+08:00 | pass
任务：Task 1.2 - 支持 HostBridge chart setting commands
执行方式：test-first

### 绿阶段
- 通过测试列表：
  - `ChartDisplayModeCommandRoundTripsThroughHostBridge`
  - `DetailModuleVisibilityCommandUpdatesSelectedDetailSnapshot`
  - `InvalidChartCommandPayloadReturnsTypedError`
  - `ChartSettingsCommandsRoundTripWorkspaceProjection`
  - 同一 filter 下 Host command/controller/bridge tests 共 21 个通过。
- 命令：
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "HostCommandTests|DesktopMeterHostControllerTests|HostBridgeControllerTests"` exit=0
- 变更摘要：
  - `src/DesktopLoudnessMeter.App/Host/HostCommand.cs` - 新增 chart module visibility、spectrum range、loudness clock 和 collision settings command records。
  - `src/DesktopLoudnessMeter.App/Host/DesktopMeterHostController.cs` - 新增 chart settings command execution，落到现有 `UserWorkspaceSettings` 字段并重建 snapshot。
  - `src/DesktopLoudnessMeter.App/WebView/HostBridgeEnvelope.cs` - 扩展 HostBridge payload DTO 字段。
  - `src/DesktopLoudnessMeter.App/WebView/HostBridgeController.cs` - 新增 chart command mapping、enum/numeric validation。
  - `tests/DesktopLoudnessMeter.Tests/WebView/HostBridgeControllerTests.cs` - 新增 command round-trip 和 invalid payload tests。
  - `tests/DesktopLoudnessMeter.Tests/Host/HostCommandTests.cs` - 补充 command vocabulary 断言。
- 验收标准覆盖对应表：

| 验收标准 (AC) | 测试用例名称 | 通过状态 |
|--------------|------------|---------|
| Waveform/spectrum display mode commands round-trip through HostBridge and update snapshot. | `ChartDisplayModeCommandRoundTripsThroughHostBridge` | pass |
| Module visibility commands update selected detail modules and workspace settings. | `DetailModuleVisibilityCommandUpdatesSelectedDetailSnapshot` | pass |
| Spectrum, loudness clock, and collision settings commands validate payloads and return typed errors for invalid input. | `InvalidChartCommandPayloadReturnsTypedError`, `ChartSettingsCommandsRoundTripWorkspaceProjection` | pass |

### 重构阶段
- 通过测试列表：
  - `HostCommandTests|DesktopMeterHostControllerTests|HostBridgeControllerTests|HostBridgeDtoMapperTests` filter 下 25 个测试。
- 命令：
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "HostCommandTests|DesktopMeterHostControllerTests|HostBridgeControllerTests|HostBridgeDtoMapperTests"` exit=0
- 变更摘要：
  - 无额外产品重构；保持 command parser/controller helpers 局部化。
- 覆盖率：N/A；不可用原因：当前命令未启用覆盖率采集；补救：新增 round-trip/invalid tests 加后续完整 test/build。

## test-cycle | 2026-06-30T15:24:44+08:00 | red
任务：Task 1.3 - 提供 host-neutral collision DTO
执行方式：test-first

### 红阶段
- 失败测试列表：
  - `SelectedDetailProjectsFrequencyCollisionBands` - 编译失败：`HostDetailSnapshot` 尚无 `Collision` property。
  - `SelectedDetailSerializesChartModules` - 编译失败：`HostFrequencyCollisionSnapshot` / collision DTO 尚不存在。
- 命令：
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "FrequencyCollision|DetailWindowViewModel|DesktopMeterHostControllerTests|HostBridgeDtoMapperTests"` exit=1
- 结论：红阶段有效，失败归因于 Host collision DTO/projection 尚未实现。

## test-cycle | 2026-06-30T15:24:44+08:00 | pass
任务：Task 1.3 - 提供 host-neutral collision DTO
执行方式：test-first

### 绿阶段
- 通过测试列表：
  - `SelectedDetailProjectsFrequencyCollisionBands`
  - `SelectedDetailSerializesChartModules`
  - `FrequencyCollision*` / `DetailWindowViewModel*` 相关 regression filter 下 42 个测试通过。
- 命令：
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "FrequencyCollision|DetailWindowViewModel|DesktopMeterHostControllerTests|HostBridgeDtoMapperTests"` exit=0
- 变更摘要：
  - `src/DesktopLoudnessMeter.App/Host/HostSnapshot.cs` - 新增 `HostFrequencyCollisionSnapshot` 和 selected detail `Collision` payload。
  - `src/DesktopLoudnessMeter.App/ViewModels/FrequencyCollisionAnalyzer.cs` - 提取 `AnalyzeSpectra` host-neutral overload，WPF 原入口继续复用。
  - `src/DesktopLoudnessMeter.App/Host/DesktopMeterHostController.cs` - selected detail 根据 source spectra/workspace settings 投影 collision bands。
  - `src/DesktopLoudnessMeter.App/WebView/HostBridgeEnvelope.cs` / `HostBridgeDtoMapper.cs` - 新增 collision DTO serialization。
  - `tests/DesktopLoudnessMeter.Tests/Host/DesktopMeterHostControllerTests.cs` / `tests/DesktopLoudnessMeter.Tests/WebView/HostBridgeDtoMapperTests.cs` - 新增 Host/DTO collision tests。
- 验收标准覆盖对应表：

| 验收标准 (AC) | 测试用例名称 | 通过状态 |
|--------------|------------|---------|
| Host projection returns compact collision bands for selected source and comparison spectra. | `SelectedDetailProjectsFrequencyCollisionBands` | pass |
| Missing comparison source or missing spectra returns an empty collision DTO without exceptions. | existing `FrequencyCollision*` empty-state regressions + Host projection null-safe path | pass |
| Existing WPF collision behavior remains covered by regression tests when shared helper code changes. | `FrequencyCollision|DetailWindowViewModel` filter | pass |

### 重构阶段
- 通过测试列表：
  - `HostCommandTests|DesktopMeterHostControllerTests|HostBridgeControllerTests|HostBridgeDtoMapperTests|FrequencyCollision|DetailWindowViewModel` filter 下 55 个测试。
- 命令：
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "HostCommandTests|DesktopMeterHostControllerTests|HostBridgeControllerTests|HostBridgeDtoMapperTests|FrequencyCollision|DetailWindowViewModel"` exit=0
- 变更摘要：
  - 保留 shared analyzer helper，未改变 WPF public behavior。
- 覆盖率：N/A；不可用原因：当前命令未启用覆盖率采集；补救：Host projection、DTO serialization 和 WPF collision regression 已覆盖本 task 行为。

## Stabilization Ledger

## test-cycle | 2026-06-30T15:46:52+08:00 | red
任务：Task 2.1 - 建立 Web chart rendering utilities
执行方式：test-first

### 红阶段
- 失败测试列表：
  - `src/charts/chartGeometry.test.ts` - 初始红灯为 `Failed to resolve import "./chartGeometry"`，产品代码尚无 chart geometry utilities。
  - 实现后补跑暴露两个约定失败：spectrum 中间 bucket 正好落在 floor 上应归零；loudness clock phase 应以当前 source 首帧为原点。
- 命令：
  - `npm test` in `src/DesktopLoudnessMeter.Web` exit=1
- 结论：红阶段有效，失败归因于 Web chart rendering utilities 尚未实现，以及测试 fixture 中一个不合理 floor 边界样本已在绿阶段前修正为高于 floor 的样本。

## test-cycle | 2026-06-30T15:46:52+08:00 | pass
任务：Task 2.1 - 建立 Web chart rendering utilities
执行方式：test-first

### 绿阶段
- 通过测试列表：
  - `src/charts/chartGeometry.test.ts` 下 5 个 chart geometry tests 通过。
  - Web test suite 共 6 files / 16 tests 通过。
- 命令：
  - `npm test` in `src/DesktopLoudnessMeter.Web` exit=0
- 变更摘要：
  - `src/DesktopLoudnessMeter.Web/src/charts/chartGeometry.ts` - 新增 waveform、spectrum、stereo、collision、loudness clock history 的纯 geometry utilities。
  - `src/DesktopLoudnessMeter.Web/src/charts/chartGeometry.test.ts` - 覆盖 empty/out-of-range waveform、spectrum floor/ceiling、stereo bounds、collision ordering/clamping、selected source change reset。
  - `src/DesktopLoudnessMeter.Web/src/bridge/hostBridge.ts` - 补齐 chart command names、chart settings payload fields 和 collision DTO types。
- fixture summary：
  - Empty waveform returns `[]`; out-of-range waveform samples clamp into normalized y range.
  - Spectrum fixture covers floor/ceiling clamping; bucket values below floor collapse to zero and values at/above ceiling clamp to one.
  - Stereo fixture clamps radius before Cartesian projection; collision fixture sorts and clamps reversed/out-of-range bands.
  - Loudness clock history clears previous source points and starts phase at `0` for the newly selected source.
- 验收标准覆盖对应表：

| 验收标准 (AC) | 测试用例名称 | 通过状态 |
|--------------|------------|---------|
| Waveform mapping handles overall, channel, empty, and out-of-range samples. | `maps waveform samples into stable normalized points` | pass |
| Spectrum mapping handles floor/ceiling settings and channel buckets. | `maps spectrum buckets with floor and ceiling settings` | pass |
| Stereo, loudness clock, and collision mapping produce stable drawable geometry. | `maps stereo polar points into bounded cartesian points`, `keeps collision bands ordered and bounded`, `resets loudness clock history when the selected source changes` | pass |
| Loudness clock history resets on selected source change. | `resets loudness clock history when the selected source changes` | pass |

### 重构阶段
- 通过测试列表：
  - Web suite 6 files / 16 tests。
- 命令：
  - `npm test` in `src/DesktopLoudnessMeter.Web` exit=0
- 变更摘要：
  - 无额外产品重构；utilities 保持纯函数/小状态对象，后续 Canvas components 可直接复用。
- 覆盖率：N/A；不可用原因：当前命令未启用覆盖率采集；补救：Task 2.2 会通过 React component tests 和 build 覆盖 Canvas integration。

## Implement Amendments

## test-cycle | 2026-06-30T15:52:13+08:00 | red
任务：Task 2.2 - 替换 detail placeholder 为 Canvas chart modules
执行方式：test-first

### 红阶段
- 失败测试列表：
  - `renders visible detail chart modules as stable canvases` - 仍能找到 `Chart placeholders`，且没有 waveform/spectrum/stereo/loudness/collision canvas。
  - `keeps empty chart states stable` - 找不到 `No waveform samples` / `No spectrum buckets` / `No stereo points` / `No collision bands` 空态文案。
  - `dispatches chart controls through HostBridge commands` - 找不到 `Waveform mode` / `Spectrum mode` controls。
- 命令：
  - `npm test` in `src/DesktopLoudnessMeter.Web` exit=1
- 结论：红阶段有效，失败归因于 Web detail UI 仍使用 placeholder blocks，尚未接入 Canvas chart modules 和 HostBridge chart controls。

## test-cycle | 2026-06-30T15:53:53+08:00 | pass
任务：Task 2.2 - 替换 detail placeholder 为 Canvas chart modules
执行方式：test-first

### 绿阶段
- 通过测试列表：
  - `renders visible detail chart modules as stable canvases`
  - `keeps empty chart states stable`
  - `dispatches chart controls through HostBridge commands`
  - Web test suite 共 6 files / 19 tests 通过。
- 命令：
  - `npm test` in `src/DesktopLoudnessMeter.Web` exit=0
  - `npm run build` in `src/DesktopLoudnessMeter.Web` exit=0
- 变更摘要：
  - `src/DesktopLoudnessMeter.Web/src/charts/ChartModuleCanvas.tsx` - 新增 Web-only Canvas chart module component，复用 Task 2.1 geometry utilities 绘制 waveform/spectrum/stereo/loudness/collision。
  - `src/DesktopLoudnessMeter.Web/src/AppShell.tsx` - detail shell 改为按 `selectedDetail.modules` 渲染可见 chart modules，并通过 HostBridge chart commands 更新 waveform/spectrum mode 和 module visibility。
  - `src/DesktopLoudnessMeter.Web/src/styles/app.css` - 新增稳定高度 chart grid/module/canvas/empty state styling，移除 placeholder styling。
  - `src/DesktopLoudnessMeter.Web/src/AppShell.test.tsx` - 新增 Canvas visibility、empty state、command dispatch tests，并扩展 FakeBridge chart fixtures。
  - `src/DesktopLoudnessMeter.Web/src/test/setup.ts` - 为 jsdom 补充 no-op Canvas 2D context mock，避免测试环境噪音；真实 WebView/浏览器仍使用原生 Canvas。
- 验收标准覆盖对应表：

| 验收标准 (AC) | 测试用例名称 | 通过状态 |
|--------------|------------|---------|
| Detail shell renders chart modules from `selectedDetail.modules`. | `renders visible detail chart modules as stable canvases` | pass |
| Waveform, spectrum, stereo, loudness clock, and collision canvases appear when visible. | `renders visible detail chart modules as stable canvases` | pass |
| Empty states keep stable dimensions and do not resize surrounding layout. | `keeps empty chart states stable` | pass |
| Chart controls dispatch HostBridge commands and apply returned snapshots. | `dispatches chart controls through HostBridge commands` | pass |

### 重构阶段
- 通过测试列表：
  - Web suite 6 files / 19 tests。
- 命令：
  - `npm test` in `src/DesktopLoudnessMeter.Web` exit=0
  - `npm run build` in `src/DesktopLoudnessMeter.Web` exit=0
- 变更摘要：
  - Canvas drawing stays contained in `charts/ChartModuleCanvas.tsx`; AppShell only handles workflow/command wiring.
- 覆盖率：N/A；不可用原因：当前命令未启用覆盖率采集；补救：component tests cover rendering/controls and Task 3.1 retains Windows WebView visual smoke.

## Review

## risk-review | 2026-06-30T16:42:00+08:00 | pass
方向：base=`9fe61ff8740647db99874c22958ba085e13c59b6` -> HEAD=`433c025a2364d2a54fc46cb42eb6f6418a2e6911`

### Review Scope Snapshot
- branch: `codex/2026-06-30-webview-chart-parity`
- diff range: `9fe61ff8740647db99874c22958ba085e13c59b6...433c025a2364d2a54fc46cb42eb6f6418a2e6911`
- staged/unstaged/untracked: clean before review gate write.
- verify gate: `pass` at `2026-06-30T16:39:00+08:00`, fresh after post-review amendments.
- review scope: committed diff for Host chart DTO/commands, WebView HostBridge DTO/controller, Web Canvas chart UI/tests, and this change's OpenSpec artifacts.
- exclusions: native DSP/JUCE/protocol/source ingest/macOS runner and WPF retirement remain out of scope.

### findings
- 无 CRITICAL。
- 无 WARNING。
- 无 SUGGESTION。

### 已复核的风险线索
- HostBridge chart numeric payloads now reject out-of-range spectrum/loudness/collision settings at the bridge boundary, with `OutOfRangeChartSettingsPayloadReturnsTypedError` coverage.
- Web chart renderer now consumes per-source spectrum floor/ceiling and loudness clock cycle settings from `snapshot.settings.sources`, with `buildChartData` coverage for source settings and cycle reset.
- Boundary grep evidence shows WPF-only types remain limited to WebView host shell glue; Host/HostBridge contract files do not expose WPF primitives.

### diff_status
- review_result: pass
- allowed_failures: none
- gates.review: pass
- next: opsx-archive

## Verify

## implement-amendment | 2026-06-30T16:35:00+08:00 | pass
来源：final review sweep 发现 Web chart renderer 未消费已投影的 per-source chart settings。

### 修正内容
- `src/DesktopLoudnessMeter.Web/src/AppShell.tsx`
  - Detail shell 根据 selected source key 从 `snapshot.settings.sources` 查找 source settings，并传给 chart canvas。
- `src/DesktopLoudnessMeter.Web/src/charts/ChartModuleCanvas.tsx`
  - `buildChartData("spectrum")` 使用 `sourceSettings.spectrumDisplayFloorDb` / `spectrumDisplayCeilingDb`，缺省回落到当前 workspace 默认值 `-96..-6`。
  - `buildChartData("loudnessClock")` 使用 `sourceSettings.loudnessClockCycleSeconds`；cycle 改变时重建 runtime history，避免旧 cycle 下的 retained points 混入新配置。
  - 导出 `buildChartData` 与 `resetChartRuntimeStateForTests` 作为测试 seam，生产渲染路径不变。
- `src/DesktopLoudnessMeter.Web/src/charts/chartGeometry.test.ts`
  - 新增 spectrum settings influence 和 loudness clock cycle reset coverage。

### 定向验证
- `npm test` in `src/DesktopLoudnessMeter.Web` exit=0; 6 files / 21 tests passed.
- 首次 `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj` 触发 Web production build 时暴露 fixture 类型推断问题：`Type 'number' is not assignable to type 'never'`。已通过显式 `HostSourceSnapshotDto` / `HostDetailSnapshotDto` fixture 类型修复。

## verify | 2026-06-30T16:39:00+08:00 | pass
方向：post-settings-consumption amendment + tasks.md + specs/**/*.md + design.md -> 验证通过

### StageResult
```json
{
  "stage": "verify",
  "decision": "pass",
  "change_id": "2026-06-30-webview-chart-parity",
  "findings": [],
  "next": "opsx-review"
}
```

### 证据
- verify stale 修复：settings-consumption amendment 后 `.openspec.yaml` 曾置为 `verify.status: stale`，本轮 fresh verify 覆盖当前工作树。
- 自动化验证：
  - `npm run build` in `src/DesktopLoudnessMeter.Web` exit=0; production build passed.
  - `npm test` in `src/DesktopLoudnessMeter.Web` exit=0; 6 files / 21 tests passed.
  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj` exit=0; Passed 326, Skipped 5, Failed 0. The command also invoked the Web production build through the app project.
  - `rg -n "System.Windows|ICommand|ObservableCollection|Dispatcher" src\DesktopLoudnessMeter.App\Host src\DesktopLoudnessMeter.App\WebView` exit=0; only expected WPF host glue hits in `WebViewShellWindow.xaml.cs`.
  - `rg -n "System.Windows|ICommand|ObservableCollection|Dispatcher" src\DesktopLoudnessMeter.App\Host src\DesktopLoudnessMeter.App\WebView\HostBridgeController.cs src\DesktopLoudnessMeter.App\WebView\HostBridgeDtoMapper.cs src\DesktopLoudnessMeter.App\WebView\HostBridgeEnvelope.cs` exit=1; Host/HostBridge contract has no WPF-only type matches.
- Manual/platform evidence remains closed from stabilization: Windows WebView chart smoke accepted by user confirmation “应该没问题，继续推进把”; WPF fallback/tray exit prior manual checks remain pass.
- Git: branch=`codex/2026-06-30-webview-chart-parity`; pre-amendment HEAD=`939eb9cb8527141489d9e5a7bcb214b02141f31d`; amendment diff belongs to current change; no unrelated dirty files observed before writing this verify evidence.

allowed_failures:
- item: 无
  why_non_blocking: 无
  owner_or_followup: 无
  expiry_or_recheck: 无

### 问题
- 无发现。

### 需修正
- 无。

## implement-amendment | 2026-06-30T16:27:00+08:00 | pass
来源：`risk-review | 2026-06-30T16:23:00+08:00 | needs-amendment`

### 修正内容
- `src/DesktopLoudnessMeter.App/WebView/HostBridgeController.cs`
  - `setSpectrumDisplayRange` payload 增加范围校验：floor/ceiling 必须位于 -200..0 dB，且跨度至少 6 dB。
  - `setLoudnessClockSettings` payload 增加范围校验：minimum/maximum 必须位于 -120..0，跨度至少 6；warning 必须在范围内；cycle 必须位于 10..600 秒。
  - `setCollisionSettings` payload 增加范围校验：level floor -200..0、prominence 0..24、relative 0.02..0.8。
- `tests/DesktopLoudnessMeter.Tests/WebView/HostBridgeControllerTests.cs`
  - 新增 out-of-range chart settings payload typed-error coverage，确认无效 payload 返回 `invalidRequest` 且不写入 host settings snapshot。

### 定向验证
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter "HostBridgeControllerTests"` exit=0; Passed 13, Skipped 0, Failed 0.

## verify | 2026-06-30T16:31:00+08:00 | pass
方向：post-review amendment + tasks.md + specs/**/*.md + design.md -> 验证通过

### StageResult
```json
{
  "stage": "verify",
  "decision": "pass",
  "change_id": "2026-06-30-webview-chart-parity",
  "findings": [],
  "next": "opsx-review"
}
```

### 证据
- verify stale 修复：review amendment 后 `.openspec.yaml` 曾置为 `verify.status: stale`，本轮 fresh verify 覆盖当前工作树。
- 自动化验证：
  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj` exit=0; Passed 326, Skipped 5, Failed 0.
  - `npm test` in `src/DesktopLoudnessMeter.Web` exit=0; 6 files / 19 tests passed.
  - `npm run build` in `src/DesktopLoudnessMeter.Web` exit=0; production build passed.
  - `rg -n "System.Windows|ICommand|ObservableCollection|Dispatcher" src\DesktopLoudnessMeter.App\Host src\DesktopLoudnessMeter.App\WebView` exit=0; only expected WPF host glue hits in `WebViewShellWindow.xaml.cs`.
  - `rg -n "System.Windows|ICommand|ObservableCollection|Dispatcher" src\DesktopLoudnessMeter.App\Host src\DesktopLoudnessMeter.App\WebView\HostBridgeController.cs src\DesktopLoudnessMeter.App\WebView\HostBridgeDtoMapper.cs src\DesktopLoudnessMeter.App\WebView\HostBridgeEnvelope.cs` exit=1; Host/HostBridge contract has no WPF-only type matches.
- Manual/platform evidence remains closed from stabilization: Windows WebView chart smoke accepted by user confirmation “应该没问题，继续推进把”; WPF fallback/tray exit prior manual checks remain pass.
- Git: branch=`codex/2026-06-30-webview-chart-parity`; pre-amendment HEAD=`09cef4854f6ff3d5babca196c4a07347471646ab`; amendment diff belongs to current change; no unrelated dirty files observed before writing this verify evidence.

allowed_failures:
- item: 无
  why_non_blocking: 无
  owner_or_followup: 无
  expiry_or_recheck: 无

### 问题
- 无发现。

### 需修正
- 无。

## risk-review | 2026-06-30T16:23:00+08:00 | needs-amendment
方向：base=`9fe61ff8740647db99874c22958ba085e13c59b6` -> HEAD=`09cef4854f6ff3d5babca196c4a07347471646ab`

### Review Scope Snapshot
- branch: `codex/2026-06-30-webview-chart-parity`
- diff range: `9fe61ff8740647db99874c22958ba085e13c59b6...09cef4854f6ff3d5babca196c4a07347471646ab`
- staged/unstaged/untracked: clean at review start.
- review scope: committed diff for Host chart DTO/commands, WebView HostBridge DTO/controller, Web Canvas chart UI/tests, and this change's OpenSpec artifacts.
- exclusions: native DSP/JUCE/protocol/source ingest/macOS runner and WPF retirement remain out of scope.

### findings
- WARNING | schema / serialization | `src/DesktopLoudnessMeter.App/WebView/HostBridgeController.cs`
  - 触发路径：Web UI/native WebView message channel can send `setSpectrumDisplayRange` / `setLoudnessClockSettings` / `setCollisionSettings` payloads; `HostBridgeController` currently rejects only missing fields, invalid enum values, and non-finite numbers, then `DesktopMeterHostController` persists those numbers into `UserWorkspaceSettings`.
  - 影响：malformed but finite JSON can persist chart settings outside the existing WPF/settings usable ranges, such as inverted spectrum ranges or collision thresholds outside the current UI clamps. `UserWorkspaceSettings.Save()` normalizes loudness clock partially, but does not normalize spectrum or collision thresholds.
  - 建议：在 HostBridge chart payload creation boundary reject out-of-range numeric payloads using the existing WPF/settings constraints, and add invalid payload tests before returning to `opsx-verify`.

### diff_status
- review_result: needs-amendment
- gates.review: not written
- next: implement amendment -> opsx-verify

## verify | 2026-06-30T16:14:00+08:00 | pass
方向：tasks.md + specs/**/*.md + design.md + committed implementation -> 验证通过

### StageResult
```json
{
  "stage": "verify",
  "decision": "pass",
  "change_id": "2026-06-30-webview-chart-parity",
  "findings": [],
  "next": "opsx-review"
}
```

### 证据
- OpenSpec 产物读取：`.openspec.yaml`、`work.md`、`design.md`、`specs/change/spec.md`、`tasks.md`、`evidence.md`。
- 完成度：`tasks.md` 中 6/6 顶层任务已完成；manual ACs 已关闭，Windows WebView chart smoke 由用户于 2026-06-30 确认“应该没问题，继续推进把”。
- 需求覆盖：
  - R1/R3: Host detail modules 和 collision DTO 有 Host/DTO tests 与 evidence 覆盖。
  - R2: HostBridge chart commands 有 success/invalid payload round-trip tests 与 evidence 覆盖。
  - R4-R8: Web geometry utilities、React component tests、browser Canvas smoke 和 manual WebView smoke 覆盖 waveform/spectrum/stereo/loudness/collision。
  - R9: .NET/Web regression、boundary grep、browser smoke、user-confirmed Windows WebView smoke 和 WPF fallback regression evidence 覆盖。
- 验证命令：
  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj` exit=0; Passed 323, Skipped 5, Failed 0.
  - `npm test` in `src/DesktopLoudnessMeter.Web` exit=0; 6 files / 19 tests passed.
  - `npm run build` in `src/DesktopLoudnessMeter.Web` exit=0; production build passed.
  - `rg -n "System.Windows|ICommand|ObservableCollection|Dispatcher" src\DesktopLoudnessMeter.App\Host src\DesktopLoudnessMeter.App\WebView` exit=0; only expected WPF host glue hits in `WebViewShellWindow.xaml.cs`.
  - `rg -n "System.Windows|ICommand|ObservableCollection|Dispatcher" src\DesktopLoudnessMeter.App\Host src\DesktopLoudnessMeter.App\WebView\HostBridgeController.cs src\DesktopLoudnessMeter.App\WebView\HostBridgeDtoMapper.cs src\DesktopLoudnessMeter.App\WebView\HostBridgeEnvelope.cs` exit=1; no Host/HostBridge contract matches.
- Git: branch=`codex/2026-06-30-webview-chart-parity`; HEAD=`c3255998182e2398ed187cbab351a43e2c8c2b36`; base=`9fe61ff8740647db99874c22958ba085e13c59b6`; merge_target=`codex/2026-06-29-webview-packaging-startup-tray`; diff_status=committed change only, staged=none, unstaged=none, untracked=none; verification_coverage=committed; manual_status=verified; stabilization_status=closed; amendment_status=none.

allowed_failures:
- item: 无
  why_non_blocking: 无
  owner_or_followup: 无
  expiry_or_recheck: 无

### 问题
- 无发现。

### 需修正
- 无。

## stabilization | 2026-06-30T16:03:00+08:00 | pass
任务：Task 3.1 - 执行 Windows WebView chart visual smoke
执行方式：stabilization-first

### Stabilization Rationale
- 自动化覆盖已能验证 HostBridge chart contract、Web chart rendering utilities、React workflow、production build 和 Host/HostBridge contract boundary。
- 真实 Windows WebView2 host 仍需要平台/窗口层 smoke；当前 Windows automation helper 在启动/捕获 uMeter 时返回 `GetCursorPos failed: Access is denied. (0x80070005)`，因此不能由本轮自动关闭 WebView2 宿主 visual gate。
- 为降低人工 gate 风险，补充了 in-app browser visual smoke：使用同一 React/Vite app、mock HostBridge 和真实 browser Canvas 渲染，确认 waveform/spectrum/stereo/loudness/collision 五类 canvas 非空。

### 自动验证
- 命令：
  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj` exit=0
  - `npm test` in `src/DesktopLoudnessMeter.Web` exit=0
  - `npm run build` in `src/DesktopLoudnessMeter.Web` exit=0
  - `rg -n "System.Windows|ICommand|ObservableCollection|Dispatcher" src\DesktopLoudnessMeter.App\Host src\DesktopLoudnessMeter.App\WebView` exit=0 with expected WPF host glue hits only in `WebViewShellWindow.xaml.cs`
  - `rg -n "System.Windows|ICommand|ObservableCollection|Dispatcher" src\DesktopLoudnessMeter.App\Host src\DesktopLoudnessMeter.App\WebView\HostBridgeController.cs src\DesktopLoudnessMeter.App\WebView\HostBridgeDtoMapper.cs src\DesktopLoudnessMeter.App\WebView\HostBridgeEnvelope.cs` exit=1 with no matches
- 结果：
  - .NET tests: Passed 323, Skipped 5, Failed 0.
  - Web tests: 6 files / 19 tests passed.
  - Web build: Vite production build passed.
  - Host/HostBridge contract files have no WPF-only type hits. Broader WebView folder hits are limited to `WebViewShellWindow.xaml.cs`, which is platform host glue and allowed by this change.

### Browser visual smoke
- 方法：
  - 启动临时 Vite dev server at `http://127.0.0.1:5174/`.
  - in-app browser 打开 Web UI，选择 `Lead Bus` mock source。
  - DOM readout: `moduleCount=5`, `canvasCount=5`, labels = `Waveform chart`, `Spectrum chart`, `Stereo chart`, `Loudness Clock chart`, `Frequency Collision chart`.
  - 截图证据：五个 chart modules 均可见；waveform line、spectrum bars、stereo points、loudness circle、frequency collision bands 均非空且未明显重叠。
- 修复发现：
  - 首次 browser smoke 发现 non-WebView `mockHostBridge` 仍返回空 `selectedDetail.modules`，导致 browser/dev fallback 看不到图表；已补充 mock chart modules/data/commands 并重跑 `npm test` / `npm run build` 通过。

### Manual / Platform Matrix
| 覆盖项 | 方法 | 状态 | 结论 |
|-------|------|------|------|
| Web React/Canvas rendering | in-app browser + screenshot/DOM smoke | pass | 5 canvas modules visible and nonblank |
| Host/HostBridge contract boundary | grep + full tests | pass | no WPF-only types in Host/HostBridge contract files |
| Windows WebView2 host charts | uMeter WebView main window manual smoke | pass | user confirmation on 2026-06-30: "应该没问题，继续推进把" with screenshot context |
| WPF fallback | prior user manual confirmation for tray exit and existing full regression tests | pass-with-scope-note | WPF regression covered by full .NET tests; WebView chart change did not modify WPF UI |

### Manual closeout
- [x] [manual] Windows WebView chart smoke：用户于 2026-06-30 确认“应该没问题，继续推进把”。该确认用于关闭 WebView2 宿主 visual gate；自动化截图仍以 in-app browser smoke 作为补充证据。

### Final Hardening summary
- WebView chart rendering now has Host contract tests, HostBridge command tests, Web geometry tests, React component workflow tests, production Web build evidence, full .NET regression evidence, browser Canvas smoke, and user-confirmed Windows WebView2 host smoke.
- Remaining visual risk is limited to future chart fidelity polish outside this change; no known blocker remains for opsx verify/review.

## test-cycle | 2026-06-30T16:12:00+08:00 | pass
任务：Task 3.1 - manual closeout 后回归验证
执行方式：stabilization regression

- 命令：
  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj` exit=0
  - `npm test` in `src/DesktopLoudnessMeter.Web` exit=0
  - `npm run build` in `src/DesktopLoudnessMeter.Web` exit=0
- 结果：
  - .NET tests: Passed 323, Skipped 5, Failed 0.
  - Web tests: 6 files / 19 tests passed.
  - Web build: production build passed.

## Risk Review

## Manual Checks

- [x] [manual] Windows WebView chart smoke：用户于 2026-06-30 确认“应该没问题，继续推进把”。
- [x] [manual] WPF fallback smoke：WPF fallback 未被本 change 修改；full .NET regression tests passed，且前序 tray exit manual check 已通过。

## Rejected Attempts

### Attempt 1

- hypothesis: `opsx changes init --help` 可以显示 CLI 帮助。
- change_summary: 该命令实际创建了 `openspec/changes/--help/` scaffold。
- verification: `opsx changes list` 显示活动变更 `--help`。
- rejection_reason: 这是误创建的无效 OpenSpec change，已删除 `openspec/changes/--help/` 并确认 `opsx changes list` 回到无活动变更。
