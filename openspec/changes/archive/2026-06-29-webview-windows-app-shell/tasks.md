# Tasks: 2026-06-29-webview-windows-app-shell

## Default Constraints

**Forbidden Files**（默认）：
- `.openspec.yaml`
- `tasks.md`
- `evidence.md`

**Shared Artifacts**（默认）：
- `.openspec.yaml` — controller 串行更新
- `tasks.md` — controller 串行更新
- `evidence.md` — controller 串行更新

**Worker Return Format**（默认）：DONE | DONE_WITH_CONCERNS | NEEDS_CONTEXT | BLOCKED

## 0. Frontend test foundation

- [x] 0.1 [direct] 建立 React/Vite 测试和构建脚手架

  **需求追踪**：[R3, R5] → [U3, U5]
  **执行方式**：[direct]
  **Allowed Files**：
  - `.gitignore` — ignore frontend dependency/build outputs
  - `src/DesktopLoudnessMeter.Web/package.json` — npm scripts and dependencies
  - `src/DesktopLoudnessMeter.Web/package-lock.json` — locked npm dependency graph
  - `src/DesktopLoudnessMeter.Web/vite.config.*` — Vite config
  - `src/DesktopLoudnessMeter.Web/tsconfig*.json` — TypeScript config
  - `src/DesktopLoudnessMeter.Web/src/**` — initial app/testable modules
  - `src/DesktopLoudnessMeter.Web/tests/**` 或 `src/DesktopLoudnessMeter.Web/src/**/*.test.*` — web tests

  **验收标准**：
  - [x] `package.json` 提供 `build` 和 `test` script。
  - [x] Vite/TypeScript 配置能构建一个最小 React app。
  - [x] 至少一个 smoke/unit test 能运行并验证 app 或 bridge client 基础行为。

  **验证命令 / 方法**：
  - `npm install` 或 implementation 选择的等效 install 命令，预期：依赖安装成功。
  - `npm test`，预期：web tests 通过。
  - `npm run build`，预期：Vite build 成功。

  **Evidence Required**：
  - install/build/test 命令、退出码、关键输出摘要。

  **Done When**：
  - Web frontend 具备可重复 build/test 基线。

  **依赖**：无

## 1. WebView host and bridge

- [x] 1.1 [test-first] 定义 HostBridge v1 envelope 和 DTO mapper

  **需求追踪**：[R2, R4] → [U2]
  **执行方式**：[test-first]
  **Allowed Files**：
  - `src/DesktopLoudnessMeter.App/WebView/HostBridgeEnvelope.cs` — envelope/result/error DTO
  - `src/DesktopLoudnessMeter.App/WebView/HostBridgeDtoMapper.cs` — host DTO mapping
  - `src/DesktopLoudnessMeter.App/WebView/HostBridgeJson.cs` — serialization helpers
  - `src/DesktopLoudnessMeter.App/Host/**` — only non-breaking helper if needed
  - `tests/DesktopLoudnessMeter.Tests/WebView/**` — mapper/envelope tests

  **验收标准**：
  - [x] DTO envelope 包含 version/id/kind/ok/error/snapshot 或等效字段。
  - [x] Representative `HostSnapshot` can serialize to JSON without WPF-only types.
  - [x] Unknown/invalid command payload maps to typed bridge error.

  **验证命令 / 方法**：
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "WebView|HostBridge"`，预期：bridge tests 通过。

  **Evidence Required**：
  - test-first red/green 摘要、最终命令输出。

  **Done When**：
  - HostBridge v1 DTO boundary is tested independently of WebView runtime.

  **依赖**：无

- [x] 1.2 [test-first] 实现 HostBridge controller command bus

  **需求追踪**：[R2, R4] → [U2]
  **执行方式**：[test-first]
  **Allowed Files**：
  - `src/DesktopLoudnessMeter.App/WebView/HostBridgeController.cs` — request dispatch to host controller
  - `src/DesktopLoudnessMeter.App/WebView/HostBridgeEnvelope.cs` — request/response refinements
  - `src/DesktopLoudnessMeter.App/WebView/HostBridgeDtoMapper.cs` — result mapping
  - `tests/DesktopLoudnessMeter.Tests/WebView/**` — controller tests

  **验收标准**：
  - [x] `snapshot` request returns current/refresh host snapshot DTO.
  - [x] `refresh`、`selectDetail`、`backToGlobal`、`setShowHiddenSources` commands execute through `DesktopMeterHostController` or tested equivalent.
  - [x] Invalid request returns typed error response and preserves safe snapshot state.

  **验证命令 / 方法**：
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "WebView|HostBridge"`，预期：controller tests 通过。

  **Evidence Required**：
  - test-first red/green 摘要、最终命令输出。

  **Done When**：
  - Command bus can be exercised without UI and returns deterministic DTO responses.

  **依赖**：Task 1.1

- [x] 1.3 [stabilization-first] 集成 WebView2 shell window 和 native message transport

  **需求追踪**：[R1, R2, R5] → [U1, U2, U4]
  **执行方式**：[stabilization-first]
  **Allowed Files**：
  - `src/DesktopLoudnessMeter.App/DesktopLoudnessMeter.App.csproj` — WebView2 package and web asset copy/build hook
  - `src/DesktopLoudnessMeter.App/App.xaml.cs` — shell service/controller wiring only if needed
  - `src/DesktopLoudnessMeter.App/MainWindow.xaml` — optional launch entry
  - `src/DesktopLoudnessMeter.App/WebView/WebViewShellWindow.xaml`
  - `src/DesktopLoudnessMeter.App/WebView/WebViewShellWindow.xaml.cs`
  - `src/DesktopLoudnessMeter.App/WebView/WebViewAssetLocator.cs`
  - `src/DesktopLoudnessMeter.App/WebView/HostBridgeController.cs`
  - `tests/DesktopLoudnessMeter.Tests/WebView/**`
  - `tests/DesktopLoudnessMeter.Tests/Infrastructure/**` — startup fallback regression if needed

  **验收标准**：
  - [x] WebView2 shell window initializes and loads frontend build/dev asset.
  - [x] Native WebView message channel routes request/response JSON through `HostBridgeController`.
  - [x] Existing WPF main window startup remains default/fallback.
  - [x] [manual] Windows manual smoke: open app, launch WebView shell, verify shell renders loaded UI and no startup/tray regression.

  **验证命令 / 方法**：
  - `dotnet build src/DesktopLoudnessMeter.App/DesktopLoudnessMeter.App.csproj`，预期：build 通过。
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "WebView|SingleInstance|Tray|Host"`，预期：relevant tests 通过。
  - Manual smoke，预期：WebView shell opens and WPF fallback remains available.

  **Evidence Required**：
  - build/test output、WebView2 runtime observations、manual smoke result。

  **Stabilization Rationale**：
  - 不确定性来源：WebView2 runtime availability、static asset path、native WebView message channel only fully observable inside Windows UI runtime。
  - 为什么不能先写目标行为测试：WebView2 control initialization and message transport need real runtime/window integration; DTO/controller behavior is covered test-first in Task 1.1/1.2.
  - 退出条件：manual smoke passes; build/test pass; any runtime-specific workaround is documented in evidence.

  **Manual / Platform Matrix**：
  | 覆盖项 | 方法 | 阻断? | 期望 |
  |-------|------|-------|------|
  | Windows WebView2 shell | Manual app launch | yes | Shell opens and renders frontend |
  | Native message transport | Manual refresh/select command | yes | UI updates or typed error displays |
  | WPF fallback | Manual WPF main window check | yes | Existing WPF UI remains available |

  **Final Hardening Plan**：
  - DTO/controller regression is covered by Task 1.1/1.2.
  - Add or retain startup/tray tests if shell wiring touches those paths.
  - doc impact: `TASK_EVIDENCE_ONLY` unless implementation changes scope or bridge schema.

  **Done When**：
  - Shell integration has automated build/test evidence plus closed manual smoke.

  **依赖**：Task 0.1, Task 1.2

## 2. Web frontend shell

- [x] 2.1 [test-first] 实现 bridge client 和 mockable state flow

  **需求追踪**：[R3, R4] → [U3]
  **执行方式**：[test-first]
  **Allowed Files**：
  - `src/DesktopLoudnessMeter.Web/src/bridge/**` — bridge client/types
  - `src/DesktopLoudnessMeter.Web/src/state/**` — snapshot state flow
  - `src/DesktopLoudnessMeter.Web/src/**/*.test.*` 或 `src/DesktopLoudnessMeter.Web/tests/**` — tests

  **验收标准**：
  - [x] Bridge client can request snapshot and send commands using JSON DTO envelope.
  - [x] Mock bridge can drive UI tests without WebView runtime.
  - [x] Command error preserves safe UI state and exposes readable message.

  **验证命令 / 方法**：
  - `npm test`，预期：bridge client/state tests pass.

  **Evidence Required**：
  - test-first red/green 摘要、最终 `npm test` output。

  **Done When**：
  - Frontend state flow is testable without native WebView.

  **依赖**：Task 0.1, Task 1.1

- [x] 2.2 [test-first] 实现基础 Web UI DOM shell

  **需求追踪**：[R3, R4] → [U3]
  **执行方式**：[test-first]
  **Allowed Files**：
  - `src/DesktopLoudnessMeter.Web/src/components/**` — source list, status, recent events, detail shell, settings entry
  - `src/DesktopLoudnessMeter.Web/src/App.*`
  - `src/DesktopLoudnessMeter.Web/src/styles/**`
  - `src/DesktopLoudnessMeter.Web/src/**/*.test.*` 或 `src/DesktopLoudnessMeter.Web/tests/**`

  **验收标准**：
  - [x] UI renders status, empty state, source list, source identity/title/readout.
  - [x] UI renders recent events and selected detail placeholder.
  - [x] UI exposes refresh/select/back/show-hidden controls through bridge client.
  - [x] UI includes theme/settings entry without claiming full settings schema migration.
  - [x] Chart areas are placeholders/readouts only; no chart parity claim.

  **验证命令 / 方法**：
  - `npm test`，预期：component tests pass.
  - `npm run build`，预期：frontend build succeeds.

  **Evidence Required**：
  - test-first red/green 摘要、build/test output。

  **Done When**：
  - Web frontend can render representative host snapshot and basic command workflow.

  **依赖**：Task 2.1

## 3. Regression and final verification

- [x] 3.1 [characterization-first] 保护 WPF fallback、startup 和 host regression

  **需求追踪**：[R1, R5] → [U4, U5]
  **执行方式**：[characterization-first]
  **Allowed Files**：
  - `tests/DesktopLoudnessMeter.Tests/Host/**`
  - `tests/DesktopLoudnessMeter.Tests/ViewModels/**`
  - `tests/DesktopLoudnessMeter.Tests/Infrastructure/**`
  - `src/DesktopLoudnessMeter.App/App.xaml.cs`
  - `src/DesktopLoudnessMeter.App/MainWindow.xaml`
  - `src/DesktopLoudnessMeter.App/WebView/**`

  **验收标准**：
  - [x] Existing host contract tests pass.
  - [x] Existing startup/single-instance/tray tests pass.
  - [x] WPF fallback remains default unless explicitly launching WebView shell.

  **验证命令 / 方法**：
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj`，预期：full tests pass or evidence records pre-existing environmental limitation.

  **Evidence Required**：
  - characterization baseline and final full test output.

  **Done When**：
  - Full .NET regression is run and WPF fallback behavior is documented.

  **依赖**：Task 1.3, Task 2.2

- [x] 3.2 [stabilization-first] 执行 Windows end-to-end smoke 和收口

  **需求追踪**：[R1, R2, R3, R4, R5] → [U1, U2, U3, U4, U5]
  **执行方式**：[stabilization-first]
  **Allowed Files**：
  - `openspec/changes/2026-06-29-webview-windows-app-shell/evidence.md` — controller records evidence
  - `openspec/changes/2026-06-29-webview-windows-app-shell/tasks.md` — controller closes manual checks
  - `openspec/changes/2026-06-29-webview-windows-app-shell/.openspec.yaml` — gate status by stage only
  - implementation files from prior tasks only for final smoke fixes if verified

  **验收标准**：
  - [x] [manual] Windows WebView shell opens and renders source/status/recent/detail/settings shell.
  - [x] [manual] At least one bridge command updates UI or returns visible typed error.
  - [x] [manual] WPF fallback remains available.
  - [x] All required build/test commands are recorded in evidence.

  **验证命令 / 方法**：
  - `dotnet build src/DesktopLoudnessMeter.App/DesktopLoudnessMeter.App.csproj`
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj`
  - `npm test`
  - `npm run build`
  - Manual Windows smoke

  **Evidence Required**：
  - Command outputs, manual smoke observations, any allowed environmental limitation.

  **Stabilization Rationale**：
  - 不确定性来源：combined native WebView runtime + web asset + host bridge behavior.
  - 为什么不能先写目标行为测试：E2E WebView2 rendering and native message channel require real Windows UI runtime; lower layers are covered by prior automated tests.
  - 退出条件：manual smoke closed as `[x]` or justified `[~]`; no blocking platform/manual item remains.

  **Manual / Platform Matrix**：
  | 覆盖项 | 方法 | 阻断? | 期望 |
  |-------|------|-------|------|
  | WebView shell render | Manual Windows smoke | yes | UI visible and loaded |
  | Bridge command | Manual command action | yes | UI updates or typed error visible |
  | WPF fallback | Manual Windows smoke | yes | WPF remains usable |

  **Final Hardening Plan**：
  - Keep DTO/controller/component tests from prior tasks as regression.
  - Final evidence must include full .NET and web build/test commands.
  - doc impact: `TASK_EVIDENCE_ONLY` unless smoke reveals scope/spec/design drift.

  **Done When**：
  - All automated and manual smoke evidence is present and ready for verify/review.

  **依赖**：Task 3.1
