# Tasks: 2026-06-30-web-settings-workflow-parity

## Default Constraints

- Forbidden Files:
  - `.openspec.yaml`
  - `tasks.md`
  - `evidence.md`
  - `src/DesktopLoudnessMeter.Native/**`
  - `src/DesktopLoudnessMeter.JucePlugin/**`
  - `src/DesktopLoudnessMeter.Protocol/**`
  - `docs/**`
  - `.aiknowledge/**`
  - 其他 `openspec/changes/**`
- Shared Artifacts:
  - `.openspec.yaml` - controller 串行更新
  - `tasks.md` - controller 串行更新
  - `evidence.md` - controller 串行更新
- Worker Return Format: `DONE | DONE_WITH_CONCERNS | NEEDS_CONTEXT | BLOCKED`

## 1. Expose Settings Commands Through HostBridge

- [x] 1.1 [test-first] 补齐 HostBridge v1 全局 settings command 映射

  **需求追踪**: [R2, R3] -> [U2, U4]
  **执行方式**: [test-first]
  **Allowed Files**:
  - `src/DesktopLoudnessMeter.App/WebView/HostBridgeEnvelope.cs`
  - `src/DesktopLoudnessMeter.App/WebView/HostBridgeController.cs`
  - `src/DesktopLoudnessMeter.Web/src/bridge/hostBridge.ts`
  - `tests/DesktopLoudnessMeter.Tests/WebView/HostBridgeControllerTests.cs`
  - `src/DesktopLoudnessMeter.Web/src/bridge/hostBridge.test.ts`

  **验收标准**:
  - [x] `setTheme` round-trip 后 response 为 `commandResult`，snapshot settings 中 `themeId` 更新。
  - [x] `setRecentEventLimit` round-trip 后 snapshot settings 中 `maxRecentEventEntries` 更新。
  - [x] `setEventOutputFolder` round-trip 后 snapshot settings 中 `eventOutputFolder` 更新；空白 payload 归一为空。
  - [x] 缺失或越界 payload 返回 `invalidRequest`，并且 workspace/snapshot 不被修改。
  - [x] TS `HostCommandName` 与 `HostCommandPayload` 包含新增 settings fields。

  **验证命令 / 方法**:
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "FullyQualifiedName~WebView"`，预期通过。
  - `npm test` in `src/DesktopLoudnessMeter.Web`，预期通过。

  **Evidence Required**:
  - 失败优先的 HostBridge settings test 摘要。
  - 通过后的 .NET/Web test 摘要。

  **Done When**:
  - HostBridge settings command tests 和 TS bridge tests 都通过。

## 2. Build Web Settings Workflow

- [x] 2.1 [test-first] 将 Web settings 摘要升级为可编辑 surface

  **需求追踪**: [R1, R2] -> [U1, U4]
  **执行方式**: [test-first]
  **Allowed Files**:
  - `src/DesktopLoudnessMeter.Web/src/AppShell.tsx`
  - `src/DesktopLoudnessMeter.Web/src/state/hostState.ts`
  - `src/DesktopLoudnessMeter.Web/src/mockHostBridge.ts`
  - `src/DesktopLoudnessMeter.Web/src/styles/app.css`
  - `src/DesktopLoudnessMeter.Web/src/AppShell.test.tsx`

  **验收标准**:
  - [x] Settings 按钮可打开/关闭 settings surface。
  - [x] surface 显示 `schemaVersion`、theme、recent limit、event output folder 和 show-hidden 状态。
  - [x] 修改 theme 发送 `setTheme` 并根据 post-command snapshot 更新 UI。
  - [x] 修改 recent limit 发送 `setRecentEventLimit` 并根据 post-command snapshot 更新 UI。
  - [x] 修改 event output folder 发送 `setEventOutputFolder`，支持 clear。
  - [x] show-hidden 在 top-bar 和 settings surface 中保持同一 snapshot state。
  - [x] UI 控件文本在移动宽度下不依赖固定过宽布局。

  **验证命令 / 方法**:
  - `npm test` in `src/DesktopLoudnessMeter.Web`，预期通过。
  - `npm run build` in `src/DesktopLoudnessMeter.Web`，预期通过。

  **Evidence Required**:
  - Web settings interaction tests 摘要。
  - Web build 摘要。

  **Done When**:
  - React tests 覆盖 settings surface interaction，并且 build 通过。

## 3. Apply Web Theme Tokens

- [x] 3.1 [test-first] 让 Web shell 根据 `themeId` 应用主题 token

  **需求追踪**: [R4, R5] -> [U3, U4]
  **执行方式**: [test-first]
  **Allowed Files**:
  - `src/DesktopLoudnessMeter.Web/src/AppShell.tsx`
  - `src/DesktopLoudnessMeter.Web/src/styles/app.css`
  - `src/DesktopLoudnessMeter.Web/src/AppShell.test.tsx`

  **验收标准**:
  - [x] 已知 theme id 映射到稳定 `data-theme` 或等效 token。
  - [x] null/unknown theme fallback 到默认 theme。
  - [x] CSS 使用 custom properties 覆盖主要 shell/surface/control/chart/error colors。
  - [x] 不引入 WPF theme 类型或 runtime dependency。

  **验证命令 / 方法**:
  - `npm test` in `src/DesktopLoudnessMeter.Web`，预期通过。
  - `npm run build` in `src/DesktopLoudnessMeter.Web`，预期通过。

  **Evidence Required**:
  - theme application tests 摘要。
  - CSS/theme diff 摘要。

  **Done When**:
  - theme tests 和 Web build 通过。

## 4. Final Regression

- [x] 4.1 [direct] 运行 change 级回归并更新 evidence

  **需求追踪**: [R5] -> [U4]
  **执行方式**: [direct]
  **Allowed Files**:
  - `openspec/changes/2026-06-30-web-settings-workflow-parity/evidence.md`

  **验收标准**:
  - [x] `npm test` 通过。
  - [x] `npm run build` 通过。
  - [x] `.NET WebView/Host filtered tests` 通过。
  - [x] `git diff --check` 通过。
  - [x] evidence 记录命令、退出码、当前 dirty scope。

  **验证命令 / 方法**:
  - `npm test` in `src/DesktopLoudnessMeter.Web`
  - `npm run build` in `src/DesktopLoudnessMeter.Web`
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj --filter "FullyQualifiedName~WebView|FullyQualifiedName~Host"`
  - `git diff --check`

  **Evidence Required**:
  - Final regression command output summary。
  - Dirty/scope status。

  **Done When**:
  - 所有 final regression 命令通过并写入 evidence。
