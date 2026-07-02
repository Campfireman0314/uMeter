# Design: 2026-06-30-macos-readiness

正文默认使用中文；路径、命令、配置键、frontmatter key 和状态枚举保留原文。

## 上下文

已证实事实：
- Windows 主 UI 现在由 WebView2 shell 承载，React/Vite frontend 通过 HostBridge v1 JSON DTO 消费 UI-neutral host contract。
- `src/DesktopLoudnessMeter.App/DesktopLoudnessMeter.App.csproj` 是 `net8.0-windows`，启用 WPF/WinForms，引用 WebView2 和 NAudio。
- 仓库没有现成 `WKWebView`、macOS runner、`.xcodeproj`、`Info.plist` 或 entitlements。
- `src/DesktopLoudnessMeter.Native/CMakePresets.json` 已包含 `macos-release` preset，但当前 Windows session 没有 CMake，且没有 macOS toolchain evidence。

推断：
- macOS 需要新的 native runner 边界，而不是复用 WPF/WebView2 app。
- HostBridge v1 DTO/command bus 应保持跨平台；差异应限制在 WebView native message transport 和 platform glue。
- capture/permissions/package/signing 属于 macOS platform adapter/glue，不应进入 C++ Core 算法或 Web frontend。

## 目标 / 非目标

**目标：**
- 新增 macOS WKWebView runner boundary skeleton。
- 固化 macOS readiness matrix：runner、native core、capture、packaging、notarization。
- 保持 Windows WebView primary path 不退化。

**非目标：**
- 不交付可运行/可发布的 macOS app。
- 不实现 CoreAudio/ScreenCaptureKit capture。
- 不修改 HostBridge DTO、native ABI、Web runtime、Windows app startup。

## 需求追踪

- [R1] -> [U1]
- [R2] -> [U2]
- [R3] -> [U3]
- [R4] -> [U4]
- [R5] -> [U5]

## 实施单元

### [U1] macOS WKWebView runner skeleton
- 关联需求: [R1]
- 模块边界:
  - `src/DesktopLoudnessMeter.MacHost/**`
  - `docs/platform/macos-readiness.md`
- 验证方式: grep/static inspection 确认 `WKWebView`、`WKScriptMessageHandler`、`hostBridge`、asset loading 和 response posting 入口存在。
- 知识沉淀: 归档后更新 `ui-runtime-architecture` 或新增 `macos-runner` codemap。

### [U2] readiness fact matrix
- 关联需求: [R2]
- 模块边界:
  - `docs/platform/macos-readiness.md`
  - `openspec/changes/2026-06-30-macos-readiness/evidence.md`
- 验证方式: 检查 readiness 表格包含 fact/unknown/follow-up，不含无证据 macOS pass。
- 知识沉淀: 记录 macOS readiness 仍是 platform gate，不是 product support claim。

### [U3] native core macOS build gate
- 关联需求: [R3]
- 模块边界:
  - `src/DesktopLoudnessMeter.Native/CMakePresets.json`
  - `src/DesktopLoudnessMeter.Native/CMakeLists.txt`
  - `docs/platform/macos-readiness.md`
- 验证方式: 记录当前 `cmake --version` / preset availability；后续 macOS 命令写入 manual follow-up。
- 知识沉淀: native build pitfall 继续要求最终 process/package payload 显式携带 native runtime。

### [U4] capture/package ownership
- 关联需求: [R4]
- 模块边界:
  - `docs/platform/macos-readiness.md`
  - `openspec/changes/2026-06-30-macos-readiness/tasks.md`
- 验证方式: readiness matrix 明确 capture adapter、permission、bundle、dylib、signing/notarization owner/follow-up。
- 知识沉淀: platform glue 边界，不混入 HostBridge DTO/Core。

### [U5] Windows regression guard
- 关联需求: [R5]
- 模块边界:
  - `src/DesktopLoudnessMeter.Web/**`
  - `tests/DesktopLoudnessMeter.Tests/**`
  - `src/DesktopLoudnessMeter.App/**`（只读/验证）
- 验证方式: `npm test`、`npm run build`、`dotnet test`、scope grep。
- 知识沉淀: macOS skeleton 不应影响 Windows WebView primary path。

## 决策

1. **新增 skeleton，而不是完整 macOS app。**
   - 原因：总目标需要清晰 WKWebView runner 边界；当前无 macOS toolchain，完整 app 会不可验证。
   - 影响：本 change 提供后续落地入口，但不声称 macOS support。

2. **HostBridge DTO 不变，只规划 transport 等价层。**
   - 原因：Windows HostBridge v1 已有 typed command bus + JSON DTO；跨平台差异应限制在 native WebView message channel。
   - 影响：`window.chrome.webview` 与 `window.webkit.messageHandlers.hostBridge` 的差异留在 transport/runner。

3. **capture/package 只做 readiness ownership。**
   - 原因：macOS capture 权限、loopback 技术、signing/notarization 都需要真机和 Apple toolchain。
   - 影响：后续应拆 `macos-capture-adapter`、`macos-packaging` 或合适 implementation change。

## 风险 / 权衡

- skeleton 可能随真正 Xcode 项目调整而重写：可接受，因为本 change 目标是边界和验证入口。
- 没有 macOS 真机验证：必须用 `[→]` follow-up 标记，不能 archive 成“macOS pass”。
- 增加新语言/目录可能让 Windows build 误扫：保持 skeleton 不被 `.sln` Windows build 默认编译。

## 稳定化策略

- `[stabilization-first]` 适用单元：[U3], [U4], [U5]
- doc impact 处理：`TASK_EVIDENCE_ONLY` 为主；若 implementation 发现边界需变更，则 `DESIGN_AMENDMENT`。
- final hardening：静态 boundary grep + Web/.NET regression + manual follow-up closure。

## 回退方案

- 删除 `src/DesktopLoudnessMeter.MacHost/**` 和 `docs/platform/macos-readiness.md`。
- 若 skeleton 影响 Windows build/test，立即回退 skeleton 纳入方式；不得通过修改 Windows app 行为来适配 skeleton。

## 并行 / Subagent 边界

- 默认串行：OpenSpec artifacts、runner skeleton、readiness doc。
- 可并行 lane：未来 macOS 真机验证可由单独 macOS worker 执行；本 change 当前 Windows session 不并行。
- 共享 artifact（controller 串行写入）：
  - `.openspec.yaml`
  - `tasks.md`
  - `evidence.md`

## 知识沉淀

归档后更新 codemap，记录：
- macOS runner boundary 位于 `src/DesktopLoudnessMeter.MacHost/**`。
- Windows WebView2 已落地，macOS WKWebView 仍为 readiness/skeleton。
- native CMake macOS preset 仍需真机/toolchain verify。
