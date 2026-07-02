# Design: 2026-06-29-ui-runtime-architecture-replan

正文默认使用中文；路径、命令、配置键名、frontmatter key 和状态枚举保持原文。

## 上下文

已证实事实：
- `openspec/changes/archive/2026-06-29-cross-platform-architecture-plan/` 已归档，但其中推荐后续 route 仍以 Flutter shell 为主。
- 用户已明确废弃未完成的 `2026-06-29-flutter-windows-app-shell`，后续不再以 Flutter 作为主 UI runtime。
- `openspec/changes/archive/2026-06-29-host-snapshot-command-contract/` 已归档，并在 `.aiknowledge/codemap/host-contract.md` 中记录了 UI-neutral host snapshot、typed command/result vocabulary 和 WPF adapter bridge。
- 当前仓库存在 WPF/.NET app、C++ native DSP、JUCE sender、Protocol、SenderKit；只读文件扫描未发现 `package.json`、`vite.config.*` 或 `pubspec.yaml`，因此还没有已落地的 Web frontend 或 Flutter project asset。
- `DesktopLoudnessMeter.Native` 目前是 Windows/MSVC native DSP 边界；codemap 明确后续跨平台 core build 需要单独 change。

推断：
- WebView UI 可以消费已归档 host snapshot/command/result contract，但 native WebView message transport、JSON DTO serialization 和 TypeScript type boundary 仍需在 `webview-windows-app-shell` 中验证。
- WKWebView runner 可作为 macOS host 边界，但 macOS capture、permissions、packaging、notarization 仍缺仓库事实，必须留给 `macos-readiness`。

## 目标 / 非目标

**目标：**
- 将主 UI runtime 决策从 Flutter 修订为 WebView。
- 定义 Windows WebView2 主路径和 macOS WKWebView 预留边界。
- 定义 HostBridge v1 的 transport/command bus/DTO 分层。
- 定义 Web UI 技术栈：TypeScript + React + Vite；DOM component + Canvas/WebGL 分工。
- 输出新的 WebView + C++ Core 后续 change map。

**非目标：**
- 不实现 WebView2/WKWebView host。
- 不创建 React/Vite app。
- 不迁移 native core build/source ingest/settings/packaging/charts。
- 不删除旧归档规划或 WPF UI。

## 需求追踪

- [R1] -> [U1]
- [R2] -> [U2]
- [R3] -> [U3]
- [R4] -> [U4]
- [R5] -> [U5]

## 实施单元

### [U1] Runtime decision replan
- 关联需求: [R1]
- 模块边界:
  - `openspec/changes/archive/2026-06-29-cross-platform-architecture-plan/work.md`
  - `openspec/changes/archive/2026-06-29-cross-platform-architecture-plan/design.md`
  - `openspec/changes/2026-06-29-ui-runtime-architecture-replan/work.md`
  - `openspec/changes/2026-06-29-ui-runtime-architecture-replan/design.md`
- 设计:
  - 旧 Flutter 规划保持归档历史事实，不回写 archive。
  - 本 change 作为新的修订规划入口，后续 implementation changes 应引用本 change 而非旧 Flutter shell route。
  - 废弃未完成 Flutter shell 不等于删除所有历史文档；只改变未来主路径。

### [U2] HostBridge v1 and runner boundary
- 关联需求: [R2]
- 模块边界:
  - `openspec/changes/2026-06-29-ui-runtime-architecture-replan/work.md`
  - `openspec/changes/2026-06-29-ui-runtime-architecture-replan/design.md`
  - `src/DesktopLoudnessMeter.App/Host/**`
  - 后续 Windows WebView host glue
  - 后续 macOS WKWebView runner glue
- 设计:
  - Windows runner 使用 WebView2 承载主 UI，具体 package/runtime 检测由后续 shell change 决定。
  - macOS runner 只预留 WKWebView + equivalent message channel 边界，不在本 change 宣称可运行。
  - HostBridge v1 分三层：native WebView message transport、typed command bus、JSON DTO payload。
  - 上层 command/snapshot/result vocabulary 复用 `host-snapshot-command-contract`，bridge change 只定义 transport DTO envelope 和 serialization boundary。

### [U3] Web frontend boundary
- 关联需求: [R3]
- 模块边界:
  - 后续 `src/DesktopLoudnessMeter.Web/**` 或等效 Web frontend 目录
  - 后续 `package.json`
  - 后续 `vite.config.*`
  - host snapshot/command DTO type boundary
- 设计:
  - Web UI 使用 TypeScript + React + Vite，普通 UI 使用 DOM component。
  - 基础 shell change 只覆盖 source list、status、recent events、基础 detail shell、theme/settings entry，不承诺复杂 chart parity。
  - 高频图表在 `webview-chart-parity` 中用 Canvas/WebGL 或等效 rendering path 验证 performance 和 parity。

### [U4] Layer responsibility matrix
- 关联需求: [R4]
- 模块边界:
  - `.aiknowledge/codemap/host-contract.md`
  - `.aiknowledge/codemap/app-startup.md`
  - `.aiknowledge/codemap/source-list-workspace.md`
  - `.aiknowledge/codemap/native-dsp.md`
  - `.aiknowledge/codemap/juce-sender.md`
  - `.aiknowledge/codemap/audio-capture-sources.md`
  - 当前 change `design.md`
- 设计:
  - Web UI owns presentation, interaction, themes, settings workflows and graph rendering.
  - C++ Core target owns DSP, protocol ingest boundary, platform-neutral source state machine, snapshot generation and core state machines.
  - Host owns platform glue: runner, WebView host, HostBridge v1, platform capture adapter, tray/startup/single-instance/installer.
  - JUCE sender remains DAW-side sender; Protocol remains versioned lifecycle vocabulary.
  - WPF remains fallback/regression oracle until `wpf-retirement`.

### [U5] Change map and lifecycle guard
- 关联需求: [R5]
- 模块边界:
  - `openspec/changes/2026-06-29-ui-runtime-architecture-replan/work.md`
  - `openspec/changes/2026-06-29-ui-runtime-architecture-replan/specs/change/spec.md`
  - `openspec/changes/2026-06-29-ui-runtime-architecture-replan/tasks.md`
  - `openspec/changes/2026-06-29-ui-runtime-architecture-replan/evidence.md`
- 设计:
  - 每个后续 top-level change 都单独执行 OpenSpec 生命周期。
  - 完成单个模块只代表局部交付，不代表整体 WebView + C++ Core goal 完成。
  - completion audit 只在最终架构真实落地、WPF retirement/macos readiness 策略闭环后执行。

## 边界矩阵

| 层 | 当前事实 | 目标职责 | 首个迁移入口 | 暂不迁移 / 待验证 |
|----|----------|----------|--------------|-------------------|
| Web UI | 当前未发现 web frontend 项目资产 | 主 UI、交互、主题、设置、用户工作流、DOM UI、Canvas/WebGL 图表 | `webview-windows-app-shell` | 图表 parity、macOS runner、packaging 切换 |
| Windows runner/host | 当前 WPF app owns startup/window/tray/single-instance/capture glue | WebView2 host、HostBridge v1、platform capture adapter、startup/tray/package glue | `webview-windows-app-shell` 后接 `webview-packaging-startup-tray` | WPF retirement |
| macOS runner/host | 当前无 macOS runner 事实 | WKWebView runner boundary、equivalent bridge、macOS capture/package readiness | `macos-readiness` | parity 宣称、notarization/signing |
| HostBridge v1 | Host snapshot/command/result contract 已归档，transport 未实现 | native WebView message channel transport + typed command bus + JSON DTO | `webview-windows-app-shell` | binary transport、remote protocol |
| C++ Core | 当前 native DSP 主路径已归档，构建仍 Windows/MSVC | DSP、protocol ingest boundary、source ingest state machine、snapshot generation、core state machines | `native-core-build-boundary` 后接 `native-source-ingest-core` | platform capture adapter、UI state |
| JUCE sender | VST3 sender lifecycle、audio packet、ack/restart 语义 | 继续 DAW sender 职责，配合版本化 lifecycle vocabulary | `juce-sender-protocol-boundary` | AAX/AU 正式交付 |
| Protocol | JSON/base64 UDP v1 和 lifecycle vocabulary | 保持版本化 lifecycle vocabulary，后续可扩展但不破坏 v1 | `juce-sender-protocol-boundary` | binary optimization 首批不做 |
| WPF | 当前主 UI，已有回归测试和 oracle 价值 | WebView parity 前作为 fallback/regression oracle | `wpf-retirement` | WebView 未 parity 前不得删除 |

## 决策

1. **WebView 取代 Flutter 作为主 UI runtime。**
   - 原因：用户明确修订最终态并废弃未完成 Flutter shell。
   - 影响：后续 UI implementation change 使用 WebView2/WKWebView，而不是 Flutter runner。

2. **Windows-first WebView2，macOS 只预留 WKWebView boundary。**
   - 原因：当前仓库可验证主路径是 Windows；macOS runner/capture/package 事实不足。
   - 影响：`macos-readiness` 在 Windows 主路径稳定后单独验证。

3. **HostBridge v1 复用 host contract vocabulary，但 transport 由 WebView native channel 承担。**
   - 原因：`host-snapshot-command-contract` 已归档，可以避免 Web UI 直接耦合 WPF ViewModel。
   - 影响：bridge change 应重点定义 JSON DTO envelope、request/result/event stream 和 error mapping。

4. **基础 shell 与 chart parity 拆分。**
   - 原因：source list/status/settings/detail shell 与高频 Canvas/WebGL 图表的风险和验证方式不同。
   - 影响：`webview-windows-app-shell` 可先跑通主路径，`webview-chart-parity` 后续专门做 performance/visual regression。

5. **WPF 保留到 parity 后再 retirement。**
   - 原因：WPF 当前是可运行 UI 和 regression oracle；过早删除会破坏回退路径。
   - 影响：packaging/startup change 必须保留 fallback switch 或明确隔离策略。

## 风险 / 权衡

- 旧归档规划仍写 Flutter，后续读者可能混淆：本 change 明确作为修订规划入口，后续 changes 引用本 change。
- WebView2 runtime/package 细节未验证：交给 `webview-windows-app-shell` 的 readiness/task 验证。
- HostBridge DTO 可能与 C# host DTO 漂移：后续 shell/schema change 需要定义 type sync 或 contract tests。
- React/Vite 项目会引入 Node toolchain：后续 shell change 必须定义 build/test/install policy。
- macOS WKWebView/capture 风险未知：保持 non-goal，单独 readiness。

## 稳定化策略

- `[stabilization-first]` 适用单元：无。
- doc impact 处理：`TASK_EVIDENCE_ONLY`，本 change 只改 OpenSpec 规划文档。
- final hardening：readiness 检查 R/U/task/evidence trace；verify 重跑只读命令确认仓库事实仍成立。

## 回退方案

删除或修订本 change 文档即可回退规划；不影响产品代码。若后续发现 WebView route 不成立，必须回到 opsx-explore 比较其他 runtime，而不是恢复 Flutter 历史规划作为默认。

## 并行 / Subagent 边界

本 planning change 默认串行，无需 subagent。后续 implementation changes 可分别并行调研，但必须在各自 plan 中重新执行 codemap-first、pitfalls 和 worktree 裁决。

## 知识沉淀

归档后建议更新 `.aiknowledge/codemap` 或新增跨平台架构入口，记录 WebView runtime 修订、HostBridge v1 边界、WPF fallback policy 和新的 change map。
