# Tasks: 2026-06-30-webview-chart-parity

## Default Constraints

- Forbidden Files:
  - `.openspec.yaml`
  - `tasks.md`
  - `evidence.md`
- Shared Artifacts:
  - `.openspec.yaml` - controller 串行更新
  - `tasks.md` - controller 串行更新
  - `evidence.md` - controller 串行更新
- Worker Return Format: `DONE | DONE_WITH_CONCERNS | NEEDS_CONTEXT | BLOCKED`

## 1. Host Chart Contract

- [x] 1.1 [test-first] 投影 selected detail chart modules

  **需求追踪**: [R1] -> [U1]
  **执行方式**: [test-first]
  **Allowed Files**:
  - `src/DesktopLoudnessMeter.App/Host/**` - Host detail module/settings projection
  - `src/DesktopLoudnessMeter.App/WebView/HostBridgeDtoMapper.cs` - DTO mapping
  - `tests/DesktopLoudnessMeter.Tests/Host/**` - Host projection tests
  - `tests/DesktopLoudnessMeter.Tests/WebView/HostBridgeDtoMapperTests.cs` - DTO serialization tests

  **验收标准**:
  - [x] Selected detail snapshot contains waveform, spectrum, stereo, loudnessClock, and collision module entries.
  - [x] Module visibility and settings use workspace/default values instead of WPF control state.
  - [x] DTO mapper serializes chart modules and settings without WPF/XAML-only types.

  **验证命令 / 方法**:
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "DesktopMeterHostControllerTests|HostBridgeDtoMapperTests"`，预期：通过

  **Evidence Required**:
  - red/green test evidence for Host chart module projection
  - DTO sample/assertion summary

  **Done When**:
  - Host snapshot and HostBridge DTO expose stable chart module projection for Web UI.

  **依赖**: 无

- [x] 1.2 [test-first] 支持 HostBridge chart setting commands

  **需求追踪**: [R2] -> [U2]
  **执行方式**: [test-first]
  **Allowed Files**:
  - `src/DesktopLoudnessMeter.App/Host/HostCommand.cs` - chart command records/enums
  - `src/DesktopLoudnessMeter.App/Host/DesktopMeterHostController.cs` - command execution
  - `src/DesktopLoudnessMeter.App/WebView/**` - request payload and bridge command mapping
  - `tests/DesktopLoudnessMeter.Tests/Host/**` - host command tests
  - `tests/DesktopLoudnessMeter.Tests/WebView/HostBridgeControllerTests.cs` - bridge command tests

  **验收标准**:
  - [x] Waveform/spectrum display mode commands round-trip through HostBridge and update snapshot.
  - [x] Module visibility commands update selected detail modules and workspace settings.
  - [x] Spectrum, loudness clock, and collision settings commands validate payloads and return typed errors for invalid input.

  **验证命令 / 方法**:
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "HostCommandTests|DesktopMeterHostControllerTests|HostBridgeControllerTests"`，预期：通过

  **Evidence Required**:
  - red/green test evidence for each new command family
  - invalid payload/error evidence

  **Done When**:
  - Web UI can send typed chart commands and receive updated snapshots.

  **依赖**: Task 1.1

- [x] 1.3 [test-first] 提供 host-neutral collision DTO

  **需求追踪**: [R3] -> [U1, U3]
  **执行方式**: [test-first]
  **Allowed Files**:
  - `src/DesktopLoudnessMeter.App/Host/**` - collision DTO/projection
  - `src/DesktopLoudnessMeter.App/ViewModels/FrequencyCollisionAnalyzer.cs` - host-neutral helper extraction only
  - `tests/DesktopLoudnessMeter.Tests/Host/**` - collision projection tests
  - `tests/DesktopLoudnessMeter.Tests/ViewModels/DetailWindowViewModelTests.cs` - WPF regression if helper extraction touches behavior

  **验收标准**:
  - [x] Host projection returns compact collision bands for selected source and comparison spectra.
  - [x] Missing comparison source or missing spectra returns an empty collision DTO without exceptions.
  - [x] Existing WPF collision behavior remains covered by regression tests when shared helper code changes.

  **验证命令 / 方法**:
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "FrequencyCollision|DetailWindowViewModel|DesktopMeterHostControllerTests"`，预期：通过

  **Evidence Required**:
  - collision projection test evidence
  - WPF regression evidence if analyzer/helper changed

  **Done When**:
  - Web DTO can render collision without depending on WPF ViewModel instances.

  **依赖**: Task 1.1

## 2. Web Chart UI

- [x] 2.1 [test-first] 建立 Web chart rendering utilities

  **需求追踪**: [R4, R5, R6, R7, R8] -> [U3]
  **执行方式**: [test-first]
  **Allowed Files**:
  - `src/DesktopLoudnessMeter.Web/src/charts/**` - pure rendering/mapping utilities
  - `src/DesktopLoudnessMeter.Web/src/bridge/hostBridge.ts` - chart DTO types
  - `src/DesktopLoudnessMeter.Web/src/**/*.test.ts` - Vitest coverage
  - `src/DesktopLoudnessMeter.Web/src/mockHostBridge.ts` - chart fixture data

  **验收标准**:
  - [x] Waveform mapping handles overall, channel, empty, and out-of-range samples.
  - [x] Spectrum mapping handles floor/ceiling settings and channel buckets.
  - [x] Stereo, loudness clock, and collision mapping produce stable drawable geometry.
  - [x] Loudness clock history resets on selected source change.

  **验证命令 / 方法**:
  - `npm test` in `src/DesktopLoudnessMeter.Web`，预期：通过

  **Evidence Required**:
  - red/green Vitest evidence for chart utilities
  - fixture summary for empty and channel modes

  **Done When**:
  - Chart rendering logic is testable without WebView/native host.

  **依赖**: Task 1.1

- [x] 2.2 [test-first] 替换 detail placeholder 为 Canvas chart modules

  **需求追踪**: [R1, R2, R4, R5, R6, R7, R8] -> [U4]
  **执行方式**: [test-first]
  **Allowed Files**:
  - `src/DesktopLoudnessMeter.Web/src/AppShell.tsx` - detail workflow and chart modules
  - `src/DesktopLoudnessMeter.Web/src/AppShell.test.tsx` - React component tests
  - `src/DesktopLoudnessMeter.Web/src/state/**` - host state integration
  - `src/DesktopLoudnessMeter.Web/src/styles/app.css` - chart layout and responsive styling
  - `src/DesktopLoudnessMeter.Web/src/charts/**` - chart components

  **验收标准**:
  - [x] Detail shell renders chart modules from `selectedDetail.modules`.
  - [x] Waveform, spectrum, stereo, loudness clock, and collision canvases appear when visible.
  - [x] Empty states keep stable dimensions and do not resize surrounding layout.
  - [x] Chart controls dispatch HostBridge commands and apply returned snapshots.

  **验证命令 / 方法**:
  - `npm test` in `src/DesktopLoudnessMeter.Web`，预期：通过
  - `npm run build` in `src/DesktopLoudnessMeter.Web`，预期：通过

  **Evidence Required**:
  - React test evidence for visible modules, empty states, and command dispatch
  - Web build evidence

  **Done When**:
  - Web detail UI no longer uses placeholder chart blocks.

  **依赖**: Task 1.2, Task 1.3, Task 2.1

## 3. Stabilization And Final Verification

- [x] 3.1 [stabilization-first] 执行 Windows WebView chart visual smoke

  **需求追踪**: [R9] -> [U5]
  **执行方式**: [stabilization-first]
  **Allowed Files**:
  - `src/DesktopLoudnessMeter.Web/src/AppShell.tsx` - within-scope visual/layout fixes
  - `src/DesktopLoudnessMeter.Web/src/styles/app.css` - within-scope responsive/chart fixes
  - `src/DesktopLoudnessMeter.Web/src/charts/**` - within-scope rendering fixes
  - `tests/DesktopLoudnessMeter.Tests/ViewModels/**` - WPF fallback regression if needed
  - `tests/DesktopLoudnessMeter.Tests/Controls/**` - WPF chart regression if needed

  **验收标准**:
  - [x] Windows WebView main window shows nonblank chart modules for a source with chart data.
  - [x] [manual] 在 Windows WebView 主窗口手动验证 waveform/spectrum/stereo/loudness/collision 图表非空、无明显重叠，settings/controls 可操作。
  - [x] [manual] 手动确认 WPF fallback 仍可打开或由既有启动路径 regression evidence 豁免。
  - [x] Boundary grep confirms Host/WebView chart contract does not expose WPF-only types.

  **验证命令 / 方法**:
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj`，预期：通过
  - `npm test` in `src/DesktopLoudnessMeter.Web`，预期：通过
  - `npm run build` in `src/DesktopLoudnessMeter.Web`，预期：通过
  - `rg -n "System.Windows|ICommand|ObservableCollection|Dispatcher" src/DesktopLoudnessMeter.App/Host src/DesktopLoudnessMeter.App/WebView`，预期：无 HostBridge contract 依赖命中
  - Windows manual smoke，预期：manual gates 转为 `[x]` 或有合规豁免

  **Evidence Required**:
  - Stabilization Rationale
  - Manual / Platform Matrix
  - command evidence for .NET/Web tests/build/boundary grep
  - screenshot or explicit user/manual confirmation
  - Final Hardening summary

  **Stabilization Rationale**:
  - 不确定性来源：Windows WebView2 runtime、Canvas rendering、DPI/resize behavior、live or mock chart data availability。
  - 为什么不能先写完整目标行为测试：jsdom 无法完整验证真实 WebView2/canvas pixels/DPI/resize；必须用 manual visual smoke 关闭最后一段平台风险。
  - 退出条件：自动化测试通过，Windows WebView 图表 manual smoke 通过，WPF fallback regression 未破坏。

  **Manual / Platform Matrix**:
  | 覆盖项 | 方法 | 阻断? | 期望 |
  |-------|------|-------|------|
  | Windows WebView charts | manual launch + screenshot/user confirmation | yes | chart modules nonblank, no obvious overlap |
  | WPF fallback | manual launch or regression evidence | yes | fallback remains available |

  **Final Hardening Plan**:
  - 行为稳定后补齐 component/unit/regression tests；manual-only 部分仅保留 WebView2 visual smoke。
  - doc impact 分类：TASK_EVIDENCE_ONLY，除非 smoke 发现设计或范围需要修订。

  **Evidence Required**:
  - 同本 task 上方 Evidence Required。

  **Done When**:
  - Manual gates closed and final verify/review can run without known chart parity blockers.

  **依赖**: Task 2.2
