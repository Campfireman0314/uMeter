# Evidence: 2026-06-29-webview-windows-app-shell

正文默认使用中文；路径、命令、配置键名、frontmatter key 和状态枚举保持原文。

`evidence.md` 是唯一证据账本。readiness、test-cycle、verify、risk review 和人工验证结论都追加写入这里。

## Final Gate Summary

- final_readiness:
  - status: pass
  - evidence_ref: Readiness / 2026-06-29T17:26:16+08:00
- final_verify:
  - status: pass
  - evidence_ref: Verify / 2026-06-29T18:33:24+08:00
- final_review:
  - status: pass
  - evidence_ref: Risk Review / 2026-06-29T18:36:26+08:00
- final_archive:
  - status: pass
  - archive_path: `openspec/changes/archive/2026-06-29-webview-windows-app-shell/`
  - evidence_ref: Archive / 2026-06-29T18:38:37+08:00
- allowed_failures: []
- manual_followups: []
- knowledge_status:
  - status: done
  - evidence_ref: Archive Knowledge Follow-up / 2026-06-29T18:38:37+08:00
  - pre_knowledge_status: clean
  - post_knowledge_status:
    - `M .aiknowledge/codemap/host-contract.md`
    - `M .aiknowledge/codemap/index.md`
    - `M .aiknowledge/codemap/ui-runtime-architecture.md`
    - `M .aiknowledge/logs/2026-06.md`
    - `?? .aiknowledge/codemap/webview-shell.md`
  - diff_stat:
    - `.aiknowledge/codemap/host-contract.md | 12 ++++++++++--`
    - `.aiknowledge/codemap/index.md | 3 ++-`
    - `.aiknowledge/codemap/ui-runtime-architecture.md | 8 ++++++++`
    - `.aiknowledge/logs/2026-06.md | 1 +`
    - `.aiknowledge/codemap/webview-shell.md | new codemap module`

## Exploration / Plan Evidence

### 2026-06-29 plan baseline

- stage: plan
- commands:
  - `opsx changes list`
    - exit_status: 0
    - result: 无活动变更；上一轮 `ui-runtime-architecture-replan` 已归档。
  - `git status --short --branch`
    - exit_status: 0
    - result: 创建本 change 前工作树干净；当前分支为 `codex/2026-06-29-webview-windows-app-shell`。
  - `git rev-parse HEAD`
    - exit_status: 0
    - result: `11dece2bd6ad3546fccbf6430de4c481f60a6d05`
  - `Get-Content -Encoding UTF8 .aiknowledge/codemap/index.md`
    - exit_status: 0
    - result: 命中 `ui-runtime-architecture`、`host-contract`、`app-startup`、`source-list-workspace` 等 active codemap。
  - `Get-Content -Encoding UTF8 .aiknowledge/codemap/ui-runtime-architecture.md`
    - exit_status: 0
    - result: WebView UI + C++ Core 架构入口已存在，约束 Windows WebView2、HostBridge v1、React/Vite、WPF fallback。
  - `Get-Content -Encoding UTF8 src/DesktopLoudnessMeter.App/DesktopLoudnessMeter.App.csproj`
    - exit_status: 0
    - result: 当前 app 为 `net8.0-windows` WPF，尚无 WebView2 package。
  - `Get-Content -Encoding UTF8 src/DesktopLoudnessMeter.App/App.xaml.cs`
    - exit_status: 0
    - result: startup owns single-instance、settings、source service、main window、tray。
  - `Get-ChildItem -Recurse -File src/DesktopLoudnessMeter.App/Host`
    - exit_status: 0
    - result: `DesktopMeterHostController`、`HostSnapshot`、`HostCommand`、`HostCommandResult` 等 host contract files 存在。
  - `rg --files -g "package.json" -g "vite.config.*" -g "tsconfig*.json" -g "*.xaml" src tests`
    - exit_status: 0
    - result: 发现 WPF XAML files；未发现 web frontend project config。
  - `Get-ChildItem -Recurse -File tests/DesktopLoudnessMeter.Tests/Host`
    - exit_status: 0
    - result: host contract tests already exist.
  - `opsx changes init 2026-06-29-webview-windows-app-shell spec-driven --profile strict-change --source-type feature`
    - exit_status: 0
    - result: 已初始化本 change。
- facts:
  - 当前 Windows app 尚无 WebView2 host 和 Web frontend project。
  - Host contract 已存在并可作为 HostBridge v1 上层 vocabulary。
  - WPF startup/tray/single-instance currently lives in `App.xaml.cs` and must remain fallback.
  - .NET test infrastructure exists; Web frontend test infrastructure does not yet exist.
- inferences:
  - 初版 WebView shell 可以作为 WPF app 内独立 shell/窗口运行，而不是替换 startup 主窗口。
  - HostBridge DTO mapper should live in App WebView boundary until settings/schema/type contract is stabilized later.
  - Manual Windows smoke is required because WebView2 runtime/message channel cannot be fully proven by unit tests.
- non_blocking_uncertainties:
  - WebView2 package/runtime availability.
  - Static asset copy/build strategy.
  - Node/npm availability and exact package manager.
  - Initial command subset may need adjustment during implementation without expanding scope.

## Readiness

### 2026-06-29T17:26:16+08:00

- stage: readiness
- decision: pass
- evidence_refs:
  - `work.md`：scope、non-goals、dependencies、Workspace Rationale、verification plan、manual/platform uncertainty 完整。
  - `specs/change/spec.md`：R1-R5 全局唯一，覆盖 Windows WebView shell、HostBridge v1、React/Vite UI、commands、regression guard。
  - `design.md`：R1-R5 映射到 U1-U5，每个 U 均含模块边界。
  - `tasks.md`：Default Constraints 存在；Task 0.1、1.1、1.2、1.3、2.1、2.2、3.1、3.2 均有需求追踪、Allowed Files、验收标准、验证命令、Evidence Required 和 Done When；`[stabilization-first]` tasks 含 rationale、Manual / Platform Matrix 和 Final Hardening Plan。
  - `.openspec.yaml`：readiness gate 写入 `pass` at `2026-06-29T17:26:16+08:00`。
- checks:
  - split-or-continue：通过；基础 WebView shell、HostBridge transport 和 React/Vite UI 共同构成一个可运行闭环，charts/packaging/settings/native/macOS 已拆出后续 changes。
  - worktree rationale：通过；当前为专用分支 `codex/2026-06-29-webview-windows-app-shell`，dirty 仅本 change 文档。
  - test-cycle labels：通过；DTO/command/frontend 逻辑走 test-first，WebView runtime/manual smoke 走 stabilization-first，frontend scaffold 为 direct。
  - manual gates：通过；manual items 作为 implementation/verify/archive 前必须关闭的 blocker 保留。
- findings:
  - blocking: 无
  - warnings:
    - WebView2 runtime、Node/npm dependency install、static asset loading 和 manual smoke 可能成为 implementation blockers；这些已进入 stabilization tasks，不阻断 readiness。
- next: 进入 `opsx-implement`，按 Task 0.1 -> 1.1 -> 1.2 -> 2.1/2.2 -> 1.3 -> 3.1/3.2 推进。

## Test Cycle Evidence

### 2026-06-29 Task 0.1 direct

- task: 0.1
- phase: direct
- files:
  - `.gitignore`
  - `src/DesktopLoudnessMeter.Web/package.json`
  - `src/DesktopLoudnessMeter.Web/package-lock.json`
  - `src/DesktopLoudnessMeter.Web/index.html`
  - `src/DesktopLoudnessMeter.Web/tsconfig.json`
  - `src/DesktopLoudnessMeter.Web/tsconfig.app.json`
  - `src/DesktopLoudnessMeter.Web/tsconfig.node.json`
  - `src/DesktopLoudnessMeter.Web/vite.config.ts`
  - `src/DesktopLoudnessMeter.Web/src/main.tsx`
  - `src/DesktopLoudnessMeter.Web/src/App.tsx`
  - `src/DesktopLoudnessMeter.Web/src/App.test.tsx`
  - `src/DesktopLoudnessMeter.Web/src/test/setup.ts`
  - `src/DesktopLoudnessMeter.Web/src/styles/app.css`
- direct_rationale: Task 0.1 只建立 React/Vite/TypeScript/Vitest 脚手架和 smoke app，不实现业务 bridge logic；测试循环从后续 test-first tasks 开始。
- commands:
  - `npm ci`
    - working_directory: `src/DesktopLoudnessMeter.Web`
    - exit_status: 0
    - result: lockfile-based install succeeded; `found 0 vulnerabilities`; npm emitted `whatwg-encoding` deprecation warning from transitive dependency.
  - `npm test`
    - working_directory: `src/DesktopLoudnessMeter.Web`
    - exit_status: 0
    - result: Vitest v3.2.6; 1 test file passed; 1 test passed.
  - `npm run build`
    - working_directory: `src/DesktopLoudnessMeter.Web`
    - exit_status: 0
    - result: `tsc -b && vite build` succeeded; Vite v6.4.3 built production assets.
  - cleanup generated artifacts
    - working_directory: repo root
    - exit_status: non-blocking cleanup command returned 1 after generated files were already absent; follow-up inspection showed no tracked `dist/`, `node_modules/`, `*.tsbuildinfo`, or generated `vite.config.js` remained.
- coverage:
  - R3 React/Vite frontend foundation.
  - R5 repeatable build/test wiring.
- result: DONE

### 2026-06-29 Task 1.1 test-first

- task: 1.1
- phase: test-first
- files:
  - `src/DesktopLoudnessMeter.App/WebView/HostBridgeEnvelope.cs`
  - `src/DesktopLoudnessMeter.App/WebView/HostBridgeDtoMapper.cs`
  - `src/DesktopLoudnessMeter.App/WebView/HostBridgeJson.cs`
  - `tests/DesktopLoudnessMeter.Tests/WebView/HostBridgeDtoMapperTests.cs`
- red:
  - command: `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "WebView|HostBridge"`
  - exit_status: 1
  - result: expected compile failure before implementation; tests referenced missing namespace `DesktopLoudnessMeter.App.WebView` (`CS0234`).
- green:
  - command: `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "WebView|HostBridge"`
  - exit_status: 0
  - result: `Passed! - Failed: 0, Passed: 3, Skipped: 0, Total: 3, Duration: 69 ms`.
- implementation_notes:
  - Added HostBridge v1 response envelope with `version`, `id`, `kind`, `ok`, typed `error`, `snapshot`, and `result` payload slots.
  - Added JSON DTO mapping over existing UI-neutral `HostSnapshot` / `HostCommandResult` contract without WPF binding primitives.
  - Added typed bridge error response for invalid request handling; command-bus request parsing remains in Task 1.2.
  - Adjusted implementation during green cycle to avoid C# factory/property naming collisions and to map `StereoImagePoint.Angle` / `Radius` from the existing host contract shape.
- coverage:
  - R2 HostBridge v1 JSON DTO boundary.
  - R4 WPF-neutral host contract consumption and typed errors.
- result: DONE

### 2026-06-29 Task 1.2 test-first

- task: 1.2
- phase: test-first
- files:
  - `src/DesktopLoudnessMeter.App/WebView/HostBridgeController.cs`
  - `src/DesktopLoudnessMeter.App/WebView/HostBridgeEnvelope.cs`
  - `src/DesktopLoudnessMeter.App/WebView/HostBridgeJson.cs`
  - `tests/DesktopLoudnessMeter.Tests/WebView/HostBridgeControllerTests.cs`
- red:
  - command: `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "WebView|HostBridge"`
  - exit_status: 1
  - result: expected compile failure before implementation; tests referenced missing `HostBridgeController` (`CS0246`).
- green:
  - command: `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "WebView|HostBridge"`
  - exit_status: 0
  - result: `Passed! - Failed: 0, Passed: 9, Skipped: 0, Total: 9, Duration: 70 ms`.
- intermediate_adjustment:
  - first green attempt reached build and ran tests, but `DetailCommandsRoundTripSelectedDetailState` failed because the JSON serializer omits null `selectedDetail` fields; the test was corrected to accept missing-or-null as global state, matching Task 1.1 JSON behavior.
  - an additional red check for host-command failure first failed because source-level command errors were being returned as bridge-level `error`; `ExecuteCommand` now preserves them as `commandResult` with `success=false`, typed error, and safe snapshot.
- refactor:
  - command: `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "WebView|HostBridge"`
  - exit_status: 0
  - result: same 9 tests passed after extracting `CreateSelectDetailCommand` and making `sourceKey` validation explicit.
  - coverage: N/A；不可用原因：当前命令未启用覆盖率收集；补救：后续 verify 继续运行窄测试和全量 .NET regression。
- implementation_notes:
  - Added HostBridge request envelope with `version` / `id` / `kind` / `command` / `payload`.
  - Added JSON request deserialization and controller-level `HandleJson` entry suitable for later WebView2 `WebMessageReceived` transport wiring.
  - Routed `snapshot`, `refresh`, `selectDetail`, `backToGlobal`, and `setShowHiddenSources` through `DesktopMeterHostController`.
  - Unknown kind, unsupported version, malformed JSON, missing payload, and unknown command return typed `invalidRequest` responses without throwing to the UI boundary.
- coverage:
  - R2 HostBridge v1 command bus above native transport.
  - R4 basic host command workflow and typed error response.
- result: DONE

### 2026-06-29 Task 2.1 test-first

- task: 2.1
- phase: test-first
- files:
  - `src/DesktopLoudnessMeter.Web/src/bridge/hostBridge.ts`
  - `src/DesktopLoudnessMeter.Web/src/bridge/hostBridge.test.ts`
  - `src/DesktopLoudnessMeter.Web/src/state/hostState.ts`
  - `src/DesktopLoudnessMeter.Web/src/state/hostState.test.ts`
- setup:
  - command: `npm ci`
  - working_directory: `src/DesktopLoudnessMeter.Web`
  - exit_status: 0
  - result: dependencies restored from lockfile; `found 0 vulnerabilities`; npm emitted existing transitive `whatwg-encoding` deprecation warning.
- red:
  - command: `npm test`
  - working_directory: `src/DesktopLoudnessMeter.Web`
  - exit_status: 1
  - result: first red after dependency restore failed because `src/bridge/hostBridge.ts` did not exist; later state-flow red failed because `src/state/hostState.ts` did not exist.
  - false_red: initial `npm test` before `npm ci` failed because `vitest` was unavailable in local `node_modules`; this was an environment/setup issue, not product behavior.
- green:
  - command: `npm test`
  - working_directory: `src/DesktopLoudnessMeter.Web`
  - exit_status: 0
  - result: Vitest v3.2.6; 3 test files passed; 7 tests passed.
  - command: `npm run build`
  - working_directory: `src/DesktopLoudnessMeter.Web`
  - exit_status: 0
  - result: `tsc -b && vite build` succeeded; Vite v6.4.3 built production assets after test fixture types were tightened.
- intermediate_adjustment:
  - `npm run build` initially failed because `RecordingTransport` in the test returned `unknown`; the fixture now uses `HostBridgeRequest` / `HostBridgeResponse` so `tsc -b` verifies the bridge contract.
- cleanup:
  - generated `dist/` and `*.tsbuildinfo` outputs were removed after verification; they remain ignored and untracked.
- implementation_notes:
  - Added typed frontend HostBridge DTOs aligned to the C# JSON envelope from Task 1.1/1.2.
  - Added injectable `HostBridgeTransport` and `HostBridgeClient` for snapshot requests and command requests.
  - Added a mockable host state store that preserves the last safe snapshot on bridge-level failure and surfaces readable error messages.
- coverage:
  - R3 TypeScript React/Vite bridge foundation.
  - R4 command result/error state handling without WebView runtime.
- result: DONE

### 2026-06-29 Task 2.2 test-first

- task: 2.2
- phase: test-first
- files:
  - `src/DesktopLoudnessMeter.Web/src/App.tsx`
  - `src/DesktopLoudnessMeter.Web/src/App.test.tsx`
  - `src/DesktopLoudnessMeter.Web/src/AppShell.tsx`
  - `src/DesktopLoudnessMeter.Web/src/AppShell.test.tsx`
  - `src/DesktopLoudnessMeter.Web/src/mockHostBridge.ts`
  - `src/DesktopLoudnessMeter.Web/src/styles/app.css`
- red:
  - command: `npm test`
  - working_directory: `src/DesktopLoudnessMeter.Web`
  - exit_status: 1
  - result: expected import failure because `src/AppShell.tsx` did not exist.
- green:
  - command: `npm test`
  - working_directory: `src/DesktopLoudnessMeter.Web`
  - exit_status: 0
  - result: Vitest v3.2.6; 4 test files passed; 10 tests passed.
  - command: `npm run build`
  - working_directory: `src/DesktopLoudnessMeter.Web`
  - exit_status: 0
  - result: `tsc -b && vite build` succeeded; Vite v6.4.3 transformed 1582 modules and built production assets.
- intermediate_adjustment:
  - Existing `App.test.tsx` was updated from the old scaffold text to the current workspace shell behavior.
  - Test fake bridge was adjusted to return selected-detail state after `selectDetail`, allowing the Back control to represent the host workflow realistically.
- cleanup:
  - generated `dist/` and `*.tsbuildinfo` outputs were removed after verification; `node_modules/` remains ignored local dependency cache for subsequent frontend work.
- implementation_notes:
  - Added `AppShell` DOM layout for status, source list, recent events, detail shell, settings summary, and basic controls.
  - Added standalone mock host bridge so Vite build/dev can render a representative uMeter shell before native WebView transport is wired in Task 1.3.
  - Chart areas are readout placeholders only; no waveform/spectrum/loudness-clock/collision parity is claimed.
  - CSS uses responsive app/workspace layout, restrained mixed neutral/teal/soft-warm accents, and stable control dimensions.
- coverage:
  - R3 basic React/Vite UI shell rendering.
  - R4 refresh/select/back/show-hidden command workflow through bridge-like dependency.
- result: DONE

## Stabilization Ledger

## test-cycle | 2026-06-29T18:19:02+08:00 | pass
任务：Task 1.3 - 集成 WebView2 shell window 和 native message transport
执行方式：stabilization-first

### Stabilization Rationale
- 不确定性来源：WebView2 runtime 初始化、native WebView message channel 和本地 asset 加载必须在 Windows UI runtime 内观察。
- 为什么不能先写目标行为测试：WebView2 control 初始化、window 生命周期和真实 native message transport 需要本机窗口环境；DTO/controller 与 frontend transport 行为已分别由 Task 1.1、1.2 和本 task 的自动化测试覆盖。
- 退出条件：WebView shell 能渲染 built frontend asset；native message channel 能通过 `HostBridgeController` 返回真实 snapshot/command result；WPF 默认启动和 fallback 不回退；相关 build/test 通过。

### Adjustment Ledger
| Attempt | Hypothesis | Change Summary | Verification | Keep/Rollback | Doc Impact |
|---------|------------|----------------|--------------|---------------|------------|
| 1 | `file://.../Web/index.html` startup URI 可以加载 Vite output | `WebViewAssetLocator` 使用 file URL | manual Windows smoke：`uMeter WebView` window 打开，但 WebView 内容空白 | rollback：file URL 未可靠证明 module asset 加载 | TASK_EVIDENCE_ONLY |
| 2 | Vite `base: "./"` 可以修复本地文件 asset resolution | `src/DesktopLoudnessMeter.Web/vite.config.ts` 设置 relative base | manual Windows smoke：WebView 仍为空白 | partial keep：保留 relative base，但加载策略改为 virtual HTTPS origin | TASK_EVIDENCE_ONLY |
| 3 | WebView2 virtual host mapping 可稳定加载 output `Web/` 目录 | `WebViewShellWindow` 使用 `SetVirtualHostNameToFolderMapping`，`WebViewAssetLocator` 返回 `https://app.umeter.local/index.html` | manual Windows smoke：React UI 渲染 `uMeter`、`Sources`、`Detail`、`Recent`、`Settings` | keep：虚拟 host 映射稳定 | TASK_EVIDENCE_ONLY |
| 4 | WebView2 内应使用 native WebView message channel，非 WebView 环境才走 mock bridge | `src/DesktopLoudnessMeter.Web/src/bridge/webViewTransport.ts` + `HostBridgeClient` 接入 `window.chrome.webview` | manual Windows smoke：显示真实 WASAPI source `喜马拉雅`、PID/readouts，点击 `Refresh` 后 LUFS/dBTP 更新 | keep：native transport 主路径成立 | TASK_EVIDENCE_ONLY |

### Manual / Platform Matrix
| 覆盖项 | 方法 | 状态 | 阻断? | 证据 |
|-------|------|------|-------|------|
| Windows WPF default startup | manual app launch | pass | no | 启动 `uMeter.exe` 后 WPF main window 仍首先出现，已有 source list 和控件可用 |
| WebView shell render | manual title-bar entry click | pass | no | 点击显式 WebView shell 按钮后，`uMeter WebView` 打开并渲染 source/status/recent/detail/settings shell |
| Native message transport | manual refresh command | pass | no | WebView shell 显示真实 WASAPI source `喜马拉雅`；点击 `Refresh` 后 LUFS/dBTP readouts 通过 HostBridge 更新 |
| WPF fallback | manual coexistence check | pass | no | WebView shell 独立窗口运行时，原 WPF window 仍保持打开和可用；startup 仍是 WPF-first |

### Final Hardening
- 测试 / 验证：
  - `npm test`（working directory: `src/DesktopLoudnessMeter.Web`）exit=0，结果：Vitest v3.2.6；5 test files passed；11 tests passed。
  - `npm run build`（working directory: `src/DesktopLoudnessMeter.Web`）exit=0，结果：`tsc -b && vite build` 成功，Vite transformed 1584 modules。
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "WebView|HostBridge"` exit=0，结果：`Passed! - Failed: 0, Passed: 11, Skipped: 0, Total: 11, Duration: 80 ms`。
  - `dotnet build src/DesktopLoudnessMeter.App/DesktopLoudnessMeter.App.csproj` exit=0，结果：build succeeded with 0 warnings and 0 errors；Web frontend build target 运行并将 `Web/index.html`、CSS、JS assets 复制到 app output。
- 豁免：无。
- 覆盖率：N/A（当前 .NET/Vitest 命令未启用 coverage 采集；补救：verify 阶段继续运行完整 regression/build/test 命令，覆盖率采集留给后续测试基础设施 change 或仓库统一配置）。
- 非产品观察：一次并行 `dotnet test` 与 `dotnet build` 曾因同时写入 `obj/.../uMeter.dll` 触发 `CS2012`；串行重跑通过，判定为执行争用，不是产品回归。
- 清理：验证后移除了 source tree 下生成的 `src/DesktopLoudnessMeter.Web/dist/` 和 `*.tsbuildinfo`；app output 中的 `Web/` 仍为 build artifact。

### Doc Drift Resolution
- doc impact 汇总：TASK_EVIDENCE_ONLY
- 已同步产物：`tasks.md`、`evidence.md`
- 未闭合项：无

### 验收标准覆盖对应表
| 验收标准 (AC) | 证据 | 通过状态 |
|--------------|------|---------|
| WebView2 shell window initializes and loads frontend build/dev asset. | Manual / Platform Matrix: WebView shell render；`dotnet build src/DesktopLoudnessMeter.App/DesktopLoudnessMeter.App.csproj` | pass |
| Native WebView message channel routes request/response JSON through `HostBridgeController`. | Manual / Platform Matrix: Native message transport；`webViewTransport.test.ts` via `npm test`；HostBridge tests via `dotnet test ... --filter "WebView|HostBridge"` | pass |
| Existing WPF main window startup remains default/fallback. | Manual / Platform Matrix: Windows WPF default startup + WPF fallback | pass |
| [manual] Windows manual smoke: open app, launch WebView shell, verify shell renders loaded UI and no startup/tray regression. | Manual / Platform Matrix 全部 pass | pass |

## test-cycle | 2026-06-29T18:23:11+08:00 | pass
任务：Task 3.1 - 保护 WPF fallback、startup 和 host regression
执行方式：characterization-first

### 红阶段
- 失败测试列表：无。该 task 用现有回归套件作为 characterization baseline，不新增目标行为测试。
- 假红修正：无。
- characterization 旧行为：`tests/DesktopLoudnessMeter.Tests/Host/**`、`tests/DesktopLoudnessMeter.Tests/Infrastructure/**`、`tests/DesktopLoudnessMeter.Tests/ViewModels/**` 已固化 host contract、single-instance、tray exit、WPF view model/fallback adapter 行为；Task 1.3 仅新增显式 WebView shell 入口，未替换默认 WPF startup。

### 绿阶段
- 通过测试列表：
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj`
- 变更摘要：
  - `openspec/changes/2026-06-29-webview-windows-app-shell/tasks.md` - 修改 - 标记 Task 3.1 和对应验收标准完成。
  - `openspec/changes/2026-06-29-webview-windows-app-shell/evidence.md` - 修改 - 记录 full .NET regression 与 WPF fallback characterization 证据。
- 测试变更记录：无测试修改。
- characterization 目标行为：旧断言保持不变；目标是确认 WebView shell 接入后 host/startup/tray/WPF fallback 行为未回退。
- 验收标准覆盖对应表：

| 验收标准 (AC) | 测试用例名称 | 通过状态 |
|--------------|------------|---------|
| Existing host contract tests pass. | `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj` 覆盖 `tests/DesktopLoudnessMeter.Tests/Host/**` | pass |
| Existing startup/single-instance/tray tests pass. | `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj` 覆盖 `SingleInstanceCoordinatorTests`、`TrayExitCoordinatorTests` 和 packaging/resource regressions | pass |
| WPF fallback remains default unless explicitly launching WebView shell. | Full .NET regression + Task 1.3 manual matrix；`App.xaml.cs` 仍创建 WPF `MainWindow`，WebView shell 仅由显式入口打开 | pass |

### 重构阶段
- 通过测试列表：
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj` exit=0，结果：`Passed! - Failed: 0, Passed: 306, Skipped: 5, Total: 311, Duration: 1 s`。
- 变更摘要：
  - 无产品代码重构；仅同步 OpenSpec task/evidence。
- 覆盖率：N/A（当前 .NET test 命令未启用 coverage；补救：verify 阶段继续记录 full regression，coverage 采集留给仓库统一测试配置）。
- skips：5 个既有 UDP/local-loopback sender/receiver tests 被 xUnit 标记 skipped；本 task 未新增或改变这些 skip。

## test-cycle | 2026-06-29T18:29:45+08:00 | pass
任务：Task 3.2 - 执行 Windows end-to-end smoke 和收口
执行方式：stabilization-first

### Stabilization Rationale
- 不确定性来源：最终闭环同时包含 WebView2 runtime、built web asset、native WebView message channel、HostBridge command bus、真实 host snapshot 和 WPF fallback。
- 为什么不能先写目标行为测试：E2E WebView2 渲染和 native message channel 需要真实 Windows UI runtime；DTO/controller/component/state flow 已由前序自动化测试覆盖。
- 退出条件：manual smoke 三项均关闭；`npm test`、`npm run build`、`dotnet build`、full `dotnet test` 均通过；无阻断平台项。

### Adjustment Ledger
| Attempt | Hypothesis | Change Summary | Verification | Keep/Rollback | Doc Impact |
|---------|------------|----------------|--------------|---------------|------------|
| 1 | 已构建的 app output 可以作为最终 smoke bundle 运行 | 无产品代码变更；使用 `dotnet build` 输出的 `src/DesktopLoudnessMeter.App/bin/Debug/net8.0-windows/uMeter.exe` | manual Windows smoke：WPF 默认窗口启动，WebView shell 可通过显式按钮打开 | keep：最终 smoke bundle 成立 | TASK_EVIDENCE_ONLY |
| 2 | WebView shell 的 `Refresh` 按钮能通过 native message channel 更新 UI | 无产品代码变更；点击 WebView DOM button `Refresh` | manual Windows smoke：Momentary 从 `-13.7 LUFS` 更新到 `-22.6 LUFS`，True peak 从 `-31.9 dBTP` 更新到 `-37.0 dBTP` | keep：HostBridge command loop 成立 | TASK_EVIDENCE_ONLY |

### Manual / Platform Matrix
| 覆盖项 | 方法 | 状态 | 阻断? | 证据 |
|-------|------|------|-------|------|
| WebView shell render | Computer Use manual Windows smoke | pass | no | 启动 `uMeter.exe` 后点击 WPF title bar `Open WebView shell`；`uMeter WebView` 显示 `uMeter`、`Sources`、`Detail`、`Recent`、`Settings`、`Show hidden`、`Refresh`，并显示真实 source `喜马拉雅` / `WASAPI` / PID 34828 |
| Bridge command | Computer Use manual Windows smoke | pass | no | 点击 WebView `Refresh` 后，source/detail readouts 从 `-13.7 LUFS` / `-31.9 dBTP` 更新到 `-22.6 LUFS` / `-37.0 dBTP` |
| WPF fallback | Computer Use manual Windows smoke | pass | no | App 默认启动仍是 WPF `uMeter` window；WebView shell 打开后，WPF window 仍暴露 `Open WebView shell`、`Always on top`、`Show hidden`、`Reset All`、`Details`、`Pop-up`、source/recent UI 并保持可用 |

### Final Hardening
- 测试 / 验证：
  - `npm test`（working directory: `src/DesktopLoudnessMeter.Web`）exit=0，结果：Vitest v3.2.6；5 test files passed；11 tests passed。
  - `npm run build`（working directory: `src/DesktopLoudnessMeter.Web`）exit=0，结果：`tsc -b && vite build` 成功，Vite transformed 1584 modules。
  - `dotnet build src/DesktopLoudnessMeter.App/DesktopLoudnessMeter.App.csproj` exit=0，结果：Build succeeded；0 warnings；0 errors；build target 运行 web build 并复制 app output assets。
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj` exit=0，结果：`Passed! - Failed: 0, Passed: 306, Skipped: 5, Total: 311, Duration: 1 s`。
- 豁免：无。
- 覆盖率：N/A（当前 .NET/Vitest 命令未启用 coverage；补救：verify 阶段重复记录 full regression，coverage 采集留给仓库统一测试配置）。
- skips：5 个既有 UDP/local-loopback sender/receiver tests 被 xUnit 标记 skipped；本 change 未新增这些 skip。
- 清理：manual smoke 后停止 `uMeter` 进程，并移除 source tree 下 `src/DesktopLoudnessMeter.Web/dist/` 与 `*.tsbuildinfo` 生成输出。

### Doc Drift Resolution
- doc impact 汇总：TASK_EVIDENCE_ONLY
- 已同步产物：`tasks.md`、`evidence.md`
- 未闭合项：无

### 验收标准覆盖对应表
| 验收标准 (AC) | 证据 | 通过状态 |
|--------------|------|---------|
| [manual] Windows WebView shell opens and renders source/status/recent/detail/settings shell. | Manual / Platform Matrix: WebView shell render | pass |
| [manual] At least one bridge command updates UI or returns visible typed error. | Manual / Platform Matrix: Bridge command | pass |
| [manual] WPF fallback remains available. | Manual / Platform Matrix: WPF fallback | pass |
| All required build/test commands are recorded in evidence. | Final Hardening: `npm test`、`npm run build`、`dotnet build`、`dotnet test` | pass |

## Implement Amendments

### 2026-06-29 Task 0.1 within-scope task amendment

- classification: within-scope
- observation: Task 0.1 创建 npm/Vite 脚手架时需要提交 `package-lock.json`，并需要更新 `.gitignore` 避免 `node_modules/` 和 frontend build output 进入工作树。
- docs_synced: `tasks.md` Task 0.1 Allowed Files 已加入 `.gitignore` 和 `src/DesktopLoudnessMeter.Web/package-lock.json`。
- validation: 该修改不改变 spec/design scope；仍属于 frontend test/build foundation。
- pending: 后续 Task 0.1 执行 `npm install`、`npm test`、`npm run build` 后关闭。

## Verify

## verify | 2026-06-29T18:33:24+08:00 | pass
方向：tasks.md + specs/change/spec.md + design.md + 代码文件 -> 验证通过
证据：
- 读取 `work.md`、`design.md`、`specs/change/spec.md`、`tasks.md`、`evidence.md`、`.openspec.yaml`；`tasks.md` 已完成 8/8，所有 `[manual]` AC 已关闭为 `[x]`。
- 读取 `.aiknowledge/codemap/index.md`、`ui-runtime-architecture.md`、`host-contract.md`、`app-startup.md`、`source-list-workspace.md`；实现边界符合 WebView2 HostBridge v1、WPF fallback、host contract consumption 约束。
- `npm test`（working directory: `src/DesktopLoudnessMeter.Web`）exit=0，结果：Vitest v3.2.6；5 test files passed；11 tests passed。
- `npm run build`（working directory: `src/DesktopLoudnessMeter.Web`）exit=0，结果：`tsc -b && vite build` 成功，Vite transformed 1584 modules。
- `dotnet build src/DesktopLoudnessMeter.App/DesktopLoudnessMeter.App.csproj` exit=0，结果：Build succeeded；0 warnings；0 errors；Web frontend build/copy target 执行。
- `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj` exit=0，结果：`Passed! - Failed: 0, Passed: 306, Skipped: 5, Total: 311, Duration: 1 s`。
- Manual status：Task 1.3 和 Task 3.2 的 Windows manual smoke 已在 implement evidence 中记录为 pass；WebView shell 渲染 source/status/recent/detail/settings，`Refresh` command 更新 LUFS/dBTP，WPF fallback 仍可用。
- Git: branch=`codex/2026-06-29-webview-windows-app-shell`; HEAD=`a423e4fe0090f144892d6d2d23638381319a7577`; base=`11dece2bd6ad3546fccbf6430de4c481f60a6d05`; merge_target=`opsx/native-core-dsp-migration`; diff_status=committed diff only, staged=none, unstaged=none, untracked=none; verification_coverage=committed; manual_status=verified; stabilization_status=closed; amendment_status=reverified。
allowed_failures:
- item: 无
  why_non_blocking: 无
  owner_or_followup: 无
  expiry_or_recheck: 无
问题：
- 无发现。
需修正：
- 无。

## Risk Review

## review | 2026-06-29T18:36:26+08:00 | pass
Review Scope Snapshot:
- branch: `codex/2026-06-29-webview-windows-app-shell`
- HEAD: `6dd8a73fe6ab5ed14d9d97cc46f78eb85180d5b8`
- base / merge target: `opsx/native-core-dsp-migration` at merge-base `11dece2bd6ad3546fccbf6430de4c481f60a6d05`
- diff_status: committed diff only；staged=none；unstaged=none；untracked=none
- scope: `git diff opsx/native-core-dsp-migration...HEAD`
- exclusions: 无无关 dirty tree；不审查全仓未变更代码。

审查摘要：
- 审查 WebView2 shell lifecycle、HostBridge JSON request/response、frontend WebView transport、host snapshot DTO mapping、WPF fallback entry、build/copy hook 和测试/证据 gate。
- `gates.verify.status` 为 `pass`，verify evidence 对应当前 HEAD 之前的 committed diff；本轮 review 未发现 `VERIFY_DRIFT`。
- Active pitfalls 核查：`filtered-views-are-not-lifecycle-state` 已由 host controller live key tracking 与 tests/evidence 覆盖；`native-consumers-must-import-copy-targets` 未被本 WebView change 引入；WPF hidden visualization pitfalls 不在当前 diff 触发路径。

findings:
- CRITICAL: 无发现。
- WARNING: 无发现。
- SUGGESTION: 无发现。

allowed_failures:
- item: 无
  why_non_blocking: 无
  owner_or_followup: 无
  expiry_or_recheck: 无

结论：
- 当前待发布 diff 未发现会导致崩溃、数据损坏、安全漏洞、兼容性破坏或明显错误处理缺口的发布阻断风险；可进入 `opsx-archive`。

## Manual Checks

- [x] [manual] Windows WebView shell opens and renders source/status/recent/detail/settings shell.
- [x] [manual] At least one bridge command updates UI or returns visible typed error.
- [x] [manual] WPF fallback remains available after WebView shell integration.

## Archive

## archive | 2026-06-29T18:38:37+08:00 | pass
变更：`2026-06-29-webview-windows-app-shell`
归档路径：`openspec/changes/archive/2026-06-29-webview-windows-app-shell/`
schema：spec-driven
spec 数量：1
gate 状态：
- readiness: pass
- verify: pass
- review: pass
- archive: pass
manual_followups：[]
allowed_failures：[]
knowledge_status：done

## Archive Knowledge Follow-up / 2026-06-29T18:38:37+08:00

- pre_knowledge_status: clean
- post_knowledge_status:
  - `M .aiknowledge/codemap/host-contract.md`
  - `M .aiknowledge/codemap/index.md`
  - `M .aiknowledge/codemap/ui-runtime-architecture.md`
  - `M .aiknowledge/logs/2026-06.md`
  - `?? .aiknowledge/codemap/webview-shell.md`
- diff_stat:
  - `.aiknowledge/codemap/host-contract.md | 12 ++++++++++--`
  - `.aiknowledge/codemap/index.md | 3 ++-`
  - `.aiknowledge/codemap/ui-runtime-architecture.md | 8 ++++++++`
  - `.aiknowledge/logs/2026-06.md | 1 +`
  - `.aiknowledge/codemap/webview-shell.md | new codemap module`
- result: done

## Rejected Attempts

### Attempt 1

- hypothesis: 直接替换 WPF main window/startup 为 WebView shell。
- change_summary: 未执行，只在 plan 阶段评估。
- verification: `app-startup` codemap shows startup/single-instance/tray ordering is existing glue; replan says packaging/startup/tray is later change.
- rejection_reason: 会把 shell proof-of-path 和 packaging/startup migration 合并，scope 过大。

### Attempt 2

- hypothesis: 只创建 React/Vite app，不接 HostBridge。
- change_summary: 未执行，只在 plan 阶段评估。
- verification: host contract already exists and shell value depends on consuming real host snapshot/command.
- rejection_reason: 无法形成 uMeter 可感知闭环，也不能验证 HostBridge v1。

### Attempt 3

- hypothesis: 在本 change 中完成 complex chart parity。
- change_summary: 未执行，只在 plan 阶段评估。
- verification: `ui-runtime-architecture` codemap and replan place chart parity in separate `webview-chart-parity` change.
- rejection_reason: 高频图表性能/视觉回归风险独立，应拆分。
