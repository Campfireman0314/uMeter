# Design：2026-06-29-cross-platform-architecture-plan

正文默认使用中文；路径、命令、配置键名、frontmatter key 和状态枚举保持原文。

## 上下文

当前 uMeter 仓库的事实基线：

- `DesktopLoudnessMeter.App` 是 WPF/.NET 8 Windows app，承担窗口、主题、托盘、单实例、settings、WASAPI/process loopback、local loopback receiver、ViewModel 和图表控件。
- `DesktopLoudnessMeter.Native` 已有 C++ native DSP 入口，且活动 change `2026-06-23-native-core-dsp` 正在把 PCM analyzer 主路径迁入 C++，当前阶段为 `tasks (6/7)`，下一步应是 `opsx-verify`。
- `DesktopLoudnessMeter.JucePlugin` 是 JUCE VST3 sender，负责 DAW 插件侧 sender lifecycle、UDP loopback packet、ack polling 和 same-handle restart。
- `DesktopLoudnessMeter.Protocol` 和 `DesktopLoudnessMeter.SenderKit` 提供 .NET 侧协议与 sender kit。
- 当前仓库未发现 `pubspec.yaml`，因此 Flutter app 还没有以项目资产落地。

## 目标 / 非目标

**目标：**
- 固化探索结果，给出跨平台架构演进的 top-level change map。
- 明确 WPF/C# 当前职责向 Flutter、host、native core 的迁移方向。
- 明确哪些 native 方向已被 `2026-06-23-native-core-dsp` 覆盖，哪些属于后续 build/source/protocol change。
- 为后续 OpenSpec changes 提供依赖顺序和 readiness 入口。

**非目标：**
- 不实现 Flutter shell。
- 不修改 C++ core 或 C# host。
- 不替 `2026-06-23-native-core-dsp` 完成 verify/review/archive。
- 不论证 macOS capture 可行性，只保留适配路径和待验证项。

## 需求追踪

- [R1] -> [U1]
- [R2] -> [U2]
- [R3] -> [U3]
- [R4] -> [U4]
- [R5] -> [U5]

## 实施单元

### [U1] 仓库事实基线
- 关联需求: [R1]
- 模块边界:
  - `.aiknowledge/codemap/index.md`
  - `.aiknowledge/codemap/app-startup.md`
  - `.aiknowledge/codemap/source-list-workspace.md`
  - `.aiknowledge/codemap/signal-source-ingest.md`
  - `.aiknowledge/codemap/audio-capture-sources.md`
  - `.aiknowledge/codemap/audio-analysis.md`
  - `.aiknowledge/codemap/native-dsp.md`
  - `.aiknowledge/codemap/juce-sender.md`
  - `.aiknowledge/codemap/chains/pcm-metric-flow.md`
  - `.aiknowledge/codemap/chains/same-handle-reconfiguration.md`
  - `openspec/changes/2026-06-23-native-core-dsp/**`
  - `docs/current-status.md`
  - `docs/product-roadmap/platform-strategy.md`
- 验证方式: evidence 中记录读取命令和关键事实摘要；readiness 检查本设计没有把推断写成事实。
- 知识沉淀: codemap-first 对跨平台规划仍是强制入口。

### [U2] 后续 top-level change map
- 关联需求: [R2]
- 模块边界:
  - `openspec/changes/2026-06-29-cross-platform-architecture-plan/work.md`
  - `openspec/changes/2026-06-29-cross-platform-architecture-plan/design.md`
  - `openspec/changes/2026-06-29-cross-platform-architecture-plan/specs/change/spec.md`
- 验证方式: 每个推荐 change 都有范围、不包含、依赖、建议 change 名、work 关注点；readiness 检查是否过大或过小。
- 知识沉淀: 多模块架构迁移先切 change，再进入各自 plan。

### [U3] 跨层边界矩阵
- 关联需求: [R3]
- 模块边界:
  - `openspec/changes/2026-06-29-cross-platform-architecture-plan/design.md`
  - `openspec/changes/2026-06-29-cross-platform-architecture-plan/work.md`
- 验证方式: boundary matrix 覆盖 Flutter、host、native core、JUCE sender、protocol、Windows/macOS 预留路径。
- 知识沉淀: Flutter 不直接接管 capture/DSP；native core 不接管用户交互；host 是平台 glue 和 bridge。

### [U4] 不确定性与回退规则
- 关联需求: [R4]
- 模块边界:
  - `openspec/changes/2026-06-29-cross-platform-architecture-plan/work.md`
  - `openspec/changes/2026-06-29-cross-platform-architecture-plan/evidence.md`
- 验证方式: facts/inferences/risks 明确分区；每个推断有后续处理方式。
- 知识沉淀: macOS capture、Flutter bridge、native cross-platform build 不能在规划阶段伪装成已证实。

### [U5] OpsX 生命周期入口
- 关联需求: [R5]
- 模块边界:
  - `openspec/changes/2026-06-29-cross-platform-architecture-plan/tasks.md`
  - `openspec/changes/2026-06-29-cross-platform-architecture-plan/evidence.md`
  - `openspec/changes/2026-06-29-cross-platform-architecture-plan/.openspec.yaml`
- 验证方式: plan 完成后进入 `opsx-readiness`；后续 implementation changes 不能跳过独立 plan/readiness。
- 知识沉淀: planning change 也要经过 readiness/verify/review/archive，不能只停留在会话摘要。

## 边界矩阵

| 层 | 当前职责 | 目标职责 | 首个迁移入口 | 暂不迁移 / 待验证 |
|----|----------|----------|--------------|-------------------|
| Flutter UI | 当前不存在 Flutter 项目；WPF XAML/controls/ViewModels 承担 UI | UI、交互、主题、窗口、设置入口、图表渲染 | `flutter-windows-app-shell` | 完整图表 parity、macOS runner、packaging |
| Desktop host | 当前由 WPF app/C# 同时承担 UI 与 host glue | 平台 glue、Flutter bridge、Windows capture adapter、process lifecycle | `host-snapshot-command-contract` | 是否长期保留 C# host 需后续评估 |
| Native core | 当前为 native DSP，活动 change 正在扩展 PCM analyzer | DSP、平台无关 source ingest、snapshot generation、核心状态机 | `native-core-build-boundary` 后接 `native-source-ingest-core` | Windows WASAPI capture、UI state、安装包 |
| JUCE sender | VST3 sender lifecycle、audio queue、ack 状态 | 继续保持 sender 小边界，复用协议 lifecycle | `juce-sender-protocol-boundary` | AAX/AU 正式落地 |
| Protocol | JSON/base64 UDP v1，C# 和 JUCE 各有实现资产 | 版本化 lifecycle vocabulary，保留 JSON v1，后续可加 binary path | `juce-sender-protocol-boundary` | 二进制性能优化不是首批目标 |
| Settings/workspace | `UserWorkspaceSettings` 绑定 WPF ViewModel 使用 | Flutter 可读写 schema，source-key 稳定，兼容旧数据 | `settings-workspace-schema` | 跨设备同步、云配置 |
| Packaging/startup | WPF app owns single-instance/tray/installer | Flutter app 成为主窗口后接管或重接 Windows startup/tray | `flutter-packaging-startup-tray` | macOS notarization/signing |

## 决策

1. **先规划 change map，再逐个创建实施 change。**
   - 原因：用户目标跨 UI、core、protocol、packaging；直接实施会过大。
   - 影响：本 change 的产物是后续 change 的入口，不修改产品代码。

2. **首个实施入口推荐 `host-snapshot-command-contract`。**
   - 原因：Flutter 需要稳定状态/命令边界；没有 contract 就建 UI 会把 WPF ViewModel 直接移植为隐性依赖。
   - 影响：WPF 仍可作为现有 client 回归，Flutter shell 后续接入同一 contract。

3. **native DSP 主路径不重复开 change。**
   - 原因：`2026-06-23-native-core-dsp` 已覆盖 PCM analyzer 和 C# wrapper；重复规划会冲突。
   - 影响：后续 native 工作从 build boundary 和 source ingest state machine 开始。

4. **Windows 优先，但 macOS 路径只做结构预留。**
   - 原因：当前可验证平台是 Windows；macOS capture、permissions、runner、packaging 都缺少仓库事实。
   - 影响：后续 changes 必须把 macOS 写成待验证项或非目标，不能宣称 parity。

## 风险 / 权衡

- 规划 change 可能被误解为实施完成：通过 scope/non-goals 和 tasks 明确它只完成架构切分。
- 后续 change 名带同日日期但未创建：本 change 只推荐名称，实际创建需用户选择后单独 `opsx-plan`。
- 当前分支仍有 native DSP 活动 change：本规划标记为链式，要求 native DSP 先 verify/review/archive。
- Flutter bridge 技术形态未定：交给 `host-snapshot-command-contract` 和 `flutter-windows-app-shell` 的 readiness 决定。

## 稳定化策略

- `[stabilization-first]` 适用单元：无
- doc impact 处理：`TASK_EVIDENCE_ONLY`
- final hardening：readiness 检查文档一致性；verify 阶段重跑只读命令确认 facts 仍成立。

## 回退方案

- 如果 readiness 发现推荐切分仍过大，修订 work/design 中的 top-level change map。
- 如果 readiness 发现事实不足，回到 opsx-explore 补读 codemap/docs/source。
- 如果当前 native DSP change 在 verify/review 中改变 ABI 或 scope，本规划需回到 plan 更新依赖和 native 后续切分。

## 并行 / Subagent 边界

- 默认串行：本 planning change 的 OpenSpec 产物。
- 可并行 lane：后续 implementation changes 的只读调研可并行，但必须在各自 plan 中重新裁决。
- 共享 artifact（controller 串行写入）：
  - `.openspec.yaml`
  - `tasks.md`
  - `evidence.md`

## 知识沉淀

归档后建议更新或追加 `.aiknowledge/codemap` 中的跨平台架构入口，记录本次切分结论、Flutter/host/native/protocol 边界，以及“native DSP 先归档，再推进 build/source ingest”的顺序约束。
