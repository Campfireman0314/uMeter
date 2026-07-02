# Work Packet: 2026-06-29-ui-runtime-architecture-replan

正文默认使用中文；路径、命令、配置键名、frontmatter key 和状态枚举保持原文。

## 元数据

- profile: strict-change
- source_type: feature
- status: drafting
- created: 2026-06-29

## 切分结论

### 总判断

- 结论：KEEP_ONE_CHANGE
- 理由：本 change 的交付单元是“UI runtime 架构重规划”，用于修订已归档 `cross-platform-architecture-plan` 中的 Flutter 方向，并把 WebView2/WKWebView、HostBridge v1、TypeScript + React + Vite、Windows/macOS 边界和后续 change map 固化为可审查规划资产。它不实施 WebView shell、native core、settings schema、packaging 或 charts parity；若直接把这些 implementation 都放入本 change，会跨多个独立能力簇而无法一次 readiness/verify/review 收敛。
- OpenSpec 下限检查：通过。本 change 形成工程规划闭环：基于归档规划、归档 host contract、codemap 和当前仓库事实，输出新的 UI runtime 决策、废弃 Flutter 后续路线、WebView 主路径切分合同和后续 gate 触发条件。

### 能力簇

| 簇 | 用户价值 | 主要模块 | 独立交付性 | 备注 |
|----|----------|----------|------------|------|
| UI runtime 决策修订 | 防止后续继续以 Flutter 为主 UI runtime | `openspec/changes/archive/2026-06-29-cross-platform-architecture-plan/**`, 当前 change 文档 | 高 | 本 change 明确 Flutter 旧方向已废弃 |
| WebView host boundary | 后续 Windows WebView2 shell 有可审查 bridge/transport 边界 | `src/DesktopLoudnessMeter.App/Host/**`, 后续 WebView host glue | 高 | 只规划 HostBridge v1，不实现 transport |
| Web frontend boundary | 后续 React/Vite UI 有明确目录、职责和验证入口 | 后续 `src/DesktopLoudnessMeter.Web/**` 或等效 web app | 高 | 当前仓库未发现现有 web frontend 资产 |
| 后续 change map | 让 WebView + C++ Core 演进可逐个模块进入 OpsX 生命周期 | 当前 change `work/design/spec/tasks/evidence` | 高 | 包含 Windows-first 和 macOS reserved boundary |
| WPF fallback policy | 保留 regression oracle，避免 WebView 未 parity 前丢失现有 UI 能力 | `DesktopLoudnessMeter.App` WPF 资产 | 高 | 后续 retirement 单独 change |

### 推荐切分 / Top-Level Changes

1. `webview-windows-app-shell`
   - 范围：新增 Windows WebView2 主 UI shell，接入 `host-snapshot-command-contract`，通过 HostBridge v1 transport/command bus 显示 source list、status、recent event、基础 detail shell、主题/设置入口。
   - 不包含：复杂图表 parity、native core build/source ingest 迁移、packaging 主窗口切换、macOS runner。
   - 依赖：`2026-06-29-host-snapshot-command-contract` 已归档；本 replan readiness/verify/review/archive 完成。
   - 建议 change 名：`2026-06-29-webview-windows-app-shell`
   - work 关注点：Windows WebView2 host、native message channel transport、typed command bus、JSON DTO、React/Vite app skeleton、WPF fallback 保留。

2. `native-core-build-boundary`
   - 范围：把 `DesktopLoudnessMeter.Native` 演进为跨平台 C++ Core 构建边界，保留 Windows DLL 输出并预留 macOS build/ABI 路径。
   - 不包含：source ingest 状态机、WebView UI、new DSP algorithm。
   - 依赖：`2026-06-23-native-core-dsp` 已归档；本 replan 完成。
   - 建议 change 名：`2026-06-29-native-core-build-boundary`
   - work 关注点：C ABI 稳定、MSVC/CMake 或等效构建策略、Windows parity、macOS 可验证边界。

3. `settings-workspace-schema`
   - 范围：定义 Web UI 可消费的 workspace/settings schema 与 TypeScript types，兼容旧 `UserWorkspaceSettings` 数据，并明确 host bridge DTO 映射。
   - 不包含：所有设置 UI 一次性重写、跨设备同步、WPF retirement。
   - 依赖：`host-snapshot-command-contract`；可与 WebView shell 早期并行，但 schema 变更必须独立 readiness。
   - 建议 change 名：`2026-06-29-settings-workspace-schema`
   - work 关注点：source-key 持久化、theme/settings schema、旧数据读取、Web UI type generation 或手写 DTO 同步策略。

4. `native-source-ingest-core`
   - 范围：迁移平台无关 source ingest 状态机、source registry、lifecycle handling 和 snapshot projection 到 C++ Core。
   - 不包含：Windows/macOS capture adapter、HostBridge transport、图表渲染。
   - 依赖：`native-core-build-boundary` 和稳定 host snapshot/schema。
   - 建议 change 名：`2026-06-29-native-source-ingest-core`
   - work 关注点：same-handle reconfiguration、source identity、thread/instance ownership、C# host adapter。

5. `juce-sender-protocol-boundary`
   - 范围：统一 JUCE sender、SenderKit、Protocol 与 future native core 的协议/lifecycle vocabulary，保留 JSON v1 并明确后续 binary path 非目标。
   - 不包含：AAX/AU 正式落地、Web UI、native ingest 实施细节。
   - 依赖：source ingest contract 至少完成 plan/readiness，或已有足够协议边界事实。
   - 建议 change 名：`2026-06-29-juce-sender-protocol-boundary`
   - work 关注点：sender lifecycle、ack 语义、same-handle restart、版本协商。

6. `webview-packaging-startup-tray`
   - 范围：迁移 Windows startup、tray、single-instance、activation、installer/startup packaging，让 WebView shell 成为主窗口。
   - 不包含：Web frontend feature parity、native core migration、macOS packaging。
   - 依赖：`webview-windows-app-shell` 达到可运行主路径。
   - 建议 change 名：`2026-06-29-webview-packaging-startup-tray`
   - work 关注点：Windows app startup glue、WPF fallback switch、installer behavior、single-instance activation replay。

7. `webview-chart-parity`
   - 范围：迁移 waveform、spectrum、loudness clock、frequency collision 等高频图表到 Canvas/WebGL，并与 host snapshot/metric tokens 对齐。
   - 不包含：基础 shell、native DSP 算法、packaging。
   - 依赖：`webview-windows-app-shell` 和 host snapshot metric fields 足够稳定。
   - 建议 change 名：`2026-06-29-webview-chart-parity`
   - work 关注点：Canvas/WebGL rendering loop、performance budget、visual parity evidence、WPF graph regression oracle。

8. `wpf-retirement`
   - 范围：WebView UI 达到 parity 后退休或隔离 WPF UI，保留必要 regression oracle 或 fallback 策略。
   - 不包含：补做 WebView 缺失功能、native core migration。
   - 依赖：WebView app shell、settings、packaging、chart parity 达到约定 parity。
   - 建议 change 名：`2026-06-29-wpf-retirement`
   - work 关注点：fallback removal/isolation decision、test migration、docs/install behavior。

9. `macos-readiness`
   - 范围：Windows 主路径稳定后验证 macOS WKWebView runner、native core build、capture adapter 和 packaging readiness。
   - 不包含：宣称 macOS parity、Windows WebView shell 实施。
   - 依赖：native core build boundary 和 WebView host abstraction 已稳定。
   - 建议 change 名：`2026-06-29-macos-readiness`
   - work 关注点：WKWebView runner boundary、native message transport equivalent、permissions/capture feasibility、packaging/notarization unknowns。

### 执行顺序

- 当前 focus：`ui-runtime-architecture-replan -> readiness -> verify -> review -> archive`
- 推荐后续顺序：`webview-windows-app-shell -> native-core-build-boundary -> settings-workspace-schema -> native-source-ingest-core -> juce-sender-protocol-boundary -> webview-packaging-startup-tray -> webview-chart-parity -> wpf-retirement -> macos-readiness`
- 并行可能：`settings-workspace-schema` 可在 `webview-windows-app-shell` early contract 稳定后并行；`native-core-build-boundary` 可与 WebView shell 并行，但不得互相假设未验证 API。
- 现在应该进入：本 change 的 `opsx-readiness`，审查 runtime 决策修订、facts/inferences、change map 和 task trace 是否一致。

## 意图

已归档的 `2026-06-29-cross-platform-architecture-plan` 以 Flutter 为主 UI runtime；用户已明确废弃未完成的 `2026-06-29-flutter-windows-app-shell`，并将最终态修订为 “WebView UI + C++ Core”。因此必须先修订架构规划资产，避免后续 change 继续引用旧 Flutter route。

本 change 不实施产品代码，而是把新的 WebView runtime 目标固化为 OpenSpec 规划合同：Windows 使用 WebView2，macOS 预留 WKWebView runner 边界；HostBridge v1 使用 native WebView message channel 作 transport，上层提供 typed command bus，payload 为 JSON DTO；Web UI 使用 TypeScript + React + Vite，普通 UI 使用 DOM component，高频图表使用 Canvas/WebGL；WPF 在 WebView parity 前保留为 fallback 和 regression oracle。

## 范围

### 范围内

- 修订归档规划中的 UI runtime 方向：Flutter 不再作为主 UI runtime，WebView 成为主路径。
- 固化 WebView2/WKWebView、HostBridge v1、typed command bus、JSON DTO、TypeScript + React + Vite 的架构边界。
- 明确 C++ Core、Host glue、Web UI、JUCE sender、Protocol、WPF fallback 的目标职责。
- 输出后续 top-level change map，包含范围、不包含、依赖和 work 关注点。
- 标明事实、推断、风险和回到 explore/plan 的触发条件。

### 范围外

- 不创建 WebView2 host，不添加 WebView2 NuGet/package，不创建 React/Vite 项目。
- 不修改 C++ native build、DSP、source ingest、protocol、settings schema 或 packaging。
- 不删除 Flutter 旧文档或 WPF 产品代码；旧归档规划保持历史记录，本 change 作为修订规划。
- 不承诺 macOS 可运行；只预留 WKWebView runner 和后续验证边界。
- 不把某个后续 implementation change 的完成视为整体 goal 完成。

## 依赖

- `openspec/changes/archive/2026-06-29-cross-platform-architecture-plan/` 已归档，但其 Flutter UI runtime 决策需要被本 change 修订。
- `openspec/changes/archive/2026-06-29-host-snapshot-command-contract/` 已归档；其 UI-neutral snapshot/command/result contract 是 WebView HostBridge v1 的主要上层消费边界。
- `openspec/changes/archive/2026-06-23-native-core-dsp/` 已归档；native DSP 主路径是后续 C++ Core build boundary 的事实基线。
- `.aiknowledge/codemap/host-contract.md`、`app-startup.md`、`source-list-workspace.md`、`native-dsp.md` 提供当前模块边界。

## 成功标准

- OpenSpec 产物明确旧 Flutter route 已被 WebView route 取代，并说明这是规划修订而非产品实现。
- 后续 top-level changes 每个都形成独立模块闭环、验证边界和交付价值。
- HostBridge v1 边界清楚区分 native WebView message transport、typed command bus 和 JSON DTO payload。
- Web UI、Host glue、C++ Core、JUCE sender、Protocol、WPF fallback 的职责不互相混淆。
- 所有推断都有处理方式：进入后续 change 的 readiness notes、tasks、验证项，或明确 out-of-scope。

## 需求 / 验收摘要

- [R1] 规划必须修订 Flutter 旧方向，确立 WebView 为主 UI runtime。
- [R2] 规划必须定义 WebView2/WKWebView runner 与 HostBridge v1 边界。
- [R3] 规划必须定义 Web UI 技术栈和渲染职责分层。
- [R4] 规划必须明确 C++ Core、Host glue、JUCE sender、Protocol 和 WPF fallback 的职责边界。
- [R5] 规划必须输出后续模块化 changes，并保持 OpsX 生命周期和 completion audit 约束。

## 方案摘要

采用“replan change + 后续模块化 implementation changes”的路线。本 change 只修改当前 OpenSpec 规划产物，作为对历史 Flutter 规划的修订记录。后续实施从 `webview-windows-app-shell` 开始，优先在 Windows 上跑通 WebView2 + HostBridge v1 + React/Vite app skeleton + host snapshot/command 主路径；C++ Core、settings schema、packaging、charts parity、WPF retirement 和 macOS readiness 继续拆分为独立 changes。

## 文件 / Ownership

### 允许修改

- `openspec/changes/2026-06-29-ui-runtime-architecture-replan/.openspec.yaml`
- `openspec/changes/2026-06-29-ui-runtime-architecture-replan/work.md`
- `openspec/changes/2026-06-29-ui-runtime-architecture-replan/specs/change/spec.md`
- `openspec/changes/2026-06-29-ui-runtime-architecture-replan/design.md`
- `openspec/changes/2026-06-29-ui-runtime-architecture-replan/tasks.md`
- `openspec/changes/2026-06-29-ui-runtime-architecture-replan/evidence.md`

### 禁止修改

- `src/**`
- `tests/**`
- `docs/**`
- `.aiknowledge/**`
- `openspec/changes/archive/**`

## Git / Workspace

- Workspace Strategy: `current-worktree`
- Workspace Path: `E:\gitlab\uMeter`
- Workspace Branch: `opsx/native-core-dsp-migration`
- Base Branch: `opsx/native-core-dsp-migration` at `05fd0a6e28bcb76d761aa809eda3dc489374b72c`
- Merge Target: `opsx/native-core-dsp-migration`
- Workspace Rationale:
  - dirty 分类：创建本 change 前工作树干净；创建后 dirty 仅包含 `openspec/changes/2026-06-29-ui-runtime-architecture-replan/**`，归属本 change。无明确无关 dirty file；无归属不明 dirty file。
  - 链式判断：当前分支已承载跨平台架构推进链；本 change 基于已归档 native DSP、host contract 和旧 cross-platform planning 产物，属于链式规划修订，`base_head=05fd0a6e28bcb76d761aa809eda3dc489374b72c`。
  - isolated 触发：本 change 只写 OpenSpec 规划文档，不改共享接口/schema/产品代码，不需要并行 worker 或隔离依赖；后续 implementation changes 触及公共边界时再独立裁决。
  - 结论：`current-worktree`。

## 验证计划

- `opsx changes list`，预期：仅本 replan change 为活动变更，阶段清晰。
- `opsx changes resolve 2026-06-29-ui-runtime-architecture-replan`，预期：解析到当前 change root。
- `git status --short --branch`，预期：dirty 仅包含本 change 文档。
- `rg --files -g "package.json" -g "vite.config.*" -g "pubspec.yaml"`，预期：当前仓库没有现有 Flutter 或 Web frontend 项目资产；后续 WebView shell 需要新建。
- `opsx-readiness 2026-06-29-ui-runtime-architecture-replan` 或等效 readiness 流程，预期：审查 work/spec/design/tasks/evidence 一致性。

## 稳定化 / 人工平台不确定性

- 是否需要 `[stabilization-first]`：no
- 不确定性来源：WebView2 package/runtime、WKWebView runner、React/Vite 项目布局、native message channel 细节和 macOS capture/packaging 均需后续 implementation changes 验证；本 change 只规划边界。
- 人工 / 平台覆盖：本 change 不需要人工 UI 或平台 smoke；后续 `webview-windows-app-shell` 必须定义 Windows WebView2 manual/platform smoke。
- 最终硬化策略：readiness/review 检查文档一致性；verify 阶段重跑只读命令确认事实仍成立。

## 回退 / 升级

- 回退方式：删除或修订 `openspec/changes/2026-06-29-ui-runtime-architecture-replan/**`，不会影响产品代码。
- 升级条件：如果 readiness 发现 WebView route 的关键事实不足以支撑切分，回到 opsx-explore 补只读调查。
- 升级条件：如果 WebView shell scope 仍过大，回到 plan 拆分 transport、frontend shell 或 settings entry。

## Bugfix Root Cause

none（feature）。

## Rejected Attempts

- 不采用“继续 Flutter route”：用户已明确废弃未完成的 Flutter shell，最终态要求 WebView UI。
- 不采用“直接创建 WebView shell implementation”：旧规划仍指向 Flutter，若不先 replan，后续 implementation 的目标和 evidence source 会冲突。
- 不采用“一个大 change 完成 WebView + C++ Core”：跨 UI runtime、bridge、frontend、native build、source ingest、settings、packaging、charts 和 macOS readiness，无法一次收敛。
- 不采用“立即退休 WPF”：WebView UI 未达 parity 前，WPF 是 fallback 和 regression oracle。

## Readiness Notes

- readiness 需检查本 change 是否只完成 runtime 架构修订，不假装完成 WebView/C++ Core implementation。
- readiness 需检查 R1-R5 在 spec/design/tasks 中有完整 trace。
- readiness 需检查旧 Flutter 规划与新 WebView route 的关系是否清楚：旧规划归档保留，本 change 作为修订入口。
- readiness 需检查后续 change map 是否满足模块闭环和 OpenSpec 下限。
