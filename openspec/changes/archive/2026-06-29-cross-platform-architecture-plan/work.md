# Work Packet：2026-06-29-cross-platform-architecture-plan

正文默认使用中文；路径、命令、配置键名、frontmatter key 和状态枚举保持原文。

## 元数据

- profile: strict-change
- source_type: feature
- status: drafting
- created: 2026-06-29

## 切分结论

### 总判断

- 结论：KEEP_ONE_CHANGE
- 理由：本 change 的交付单元不是实施 Flutter 或 C++ Core，而是把已完成的 opsx-explore 收敛结果固化为可审查、可验证、可归档的跨平台架构切分方案。它只修改当前 change 的 OpenSpec 产物，输出后续顶层 changes 的边界、依赖顺序、事实/推断分类和 readiness 入口。若把后续 Flutter shell、native core build、source ingest、settings、JUCE/protocol、packaging 全部放进本 change，会明显过大；若只口头总结而不落 OpenSpec，则无法进入 readiness/review/archive。
- OpenSpec 下限检查：通过。它形成一个工程规划闭环：基于 codemap/OpenSpec/源码事实，产出后续模块化 changes 的切分合同，并可通过 readiness 检查其一致性、粒度和证据来源。

### 能力簇

| 簇 | 用户价值 | 主要模块 | 独立交付性 | 备注 |
|----|----------|----------|------------|------|
| 架构事实基线 | 防止迁移规划脱离当前仓库 | `.aiknowledge/codemap/**`, `openspec/changes/**`, `docs/**`, `src/**` | 高 | 只读核对，明确已证实事实和推断 |
| 后续 change 切分 | 让 Flutter + C++ Core 演进可以逐个模块进入 OpenSpec 生命周期 | `openspec/changes/2026-06-29-cross-platform-architecture-plan/**` | 高 | 本 change 只产出切分方案，不创建 sibling changes |
| 边界矩阵 | 澄清 Flutter、host、native core、JUCE sender、protocol 分工 | `design.md`, `specs/change/spec.md` | 高 | 后续每个实施 change 需要引用或修订 |
| 流程守护 | 保证不绕开 readiness/implement/verify/review/archive | `tasks.md`, `evidence.md` | 高 | 本 change 完成 plan 后必须进入 readiness |

### 推荐切分 / Top-Level Changes

1. `host-snapshot-command-contract`
   - 范围：抽出 UI-neutral host contract，定义 Flutter 将消费的 source snapshot、detail state、settings state、command 请求和错误反馈。
   - 不包含：Flutter 项目脚手架、native capture、packaging。
   - 依赖：`2026-06-23-native-core-dsp` 完成 verify/review/archive 后，使用其稳定 `PcmMetricsSnapshot` 语义。
   - 建议 change 名：`2026-06-29-host-snapshot-command-contract`
   - work 关注点：WPF ViewModel 职责拆分、C# host contract、现有 WPF 回归验证。

2. `flutter-windows-app-shell`
   - 范围：新增 Flutter Windows app shell，承接窗口、导航、主题、source list、基础 detail shell，并通过 host contract 显示真实或可回放状态。
   - 不包含：完整图表重写、macOS、安装包替换。
   - 依赖：`host-snapshot-command-contract`。
   - 建议 change 名：`2026-06-29-flutter-windows-app-shell`
   - work 关注点：Flutter runner、host bridge、UI 状态映射、Windows 首屏可运行。

3. `native-core-build-boundary`
   - 范围：把 `DesktopLoudnessMeter.Native` 从 Windows/MSVC DLL 边界演进为跨平台 core 构建边界，保留 Windows 输出并预留 macOS CMake/ABI 路径。
   - 不包含：新 DSP 算法、source ingest、Flutter UI。
   - 依赖：`2026-06-23-native-core-dsp` 归档。
   - 建议 change 名：`2026-06-29-native-core-build-boundary`
   - work 关注点：C ABI 稳定、CMake/MSVC 输出、Windows parity、macOS 编译路径预留。

4. `native-source-ingest-core`
   - 范围：将 loopback source registry、register/heartbeat/rename/audio/close 状态机、snapshot projection 的平台无关部分迁向 C++ core。
   - 不包含：WASAPI capture 迁移、二进制协议优化、JUCE sender UI。
   - 依赖：`native-core-build-boundary` 和稳定 host contract。
   - 建议 change 名：`2026-06-29-native-source-ingest-core`
   - work 关注点：source identity、same-handle reconfiguration、thread/instance ownership、C# host adapter。

5. `settings-workspace-schema`
   - 范围：定义 Flutter 可用的 workspace/settings schema，迁移或封装 `UserWorkspaceSettings` 的稳定字段和兼容策略。
   - 不包含：所有设置 UI 一次性重写、跨设备同步。
   - 依赖：`host-snapshot-command-contract`。
   - 建议 change 名：`2026-06-29-settings-workspace-schema`
   - work 关注点：source-key 持久化、detail layout、theme/settings schema、旧数据读取。

6. `juce-sender-protocol-boundary`
   - 范围：统一 JUCE sender、`.NET SenderKit`、`DesktopLoudnessMeter.Protocol` 与 future native core 的协议边界，明确 JSON v1 兼容和后续 binary path 的非目标。
   - 不包含：AAX/AU 正式落地、协议性能重写。
   - 依赖：source ingest 边界已稳定或至少已定义 contract。
   - 建议 change 名：`2026-06-29-juce-sender-protocol-boundary`
   - work 关注点：sender lifecycle、ack 语义、same-handle restart、版本协商。

7. `flutter-packaging-startup-tray`
   - 范围：当 Flutter shell 可用后，迁移或重接 Windows startup、single-instance、tray、activation、installer/startup packaging。
   - 不包含：DSP/source ingest 设计重做。
   - 依赖：Flutter shell 已能作为主窗口运行。
   - 建议 change 名：`2026-06-29-flutter-packaging-startup-tray`
   - work 关注点：Windows 落地体验、现有托盘/单实例语义、安装包回归。

### 执行顺序

- 必须先完成：`2026-06-23-native-core-dsp -> opsx-verify -> opsx-review -> opsx-archive`
- 推荐顺序：`host-snapshot-command-contract -> flutter-windows-app-shell -> native-core-build-boundary -> settings-workspace-schema -> native-source-ingest-core -> juce-sender-protocol-boundary -> flutter-packaging-startup-tray`
- 并行可能：`settings-workspace-schema` 可在 Flutter shell 之后与 `native-core-build-boundary` 并行；`juce-sender-protocol-boundary` 可在 source ingest contract 稳定后并行。
- 现在应该进入：本 change 的 `opsx-readiness`，审查规划产物是否一致、粒度是否合适、事实/推断是否被标明。

## 意图

当前仓库已经有 WPF/.NET 8 Windows app、C++ native DSP、JUCE VST3 sender、`.NET SenderKit` 和 loopback protocol。用户目标是演进到 “Flutter UI + C++ Core”，但直接开一个全栈迁移 change 会跨 UI、host、native core、protocol、packaging 多个独立能力簇，无法一次 readiness、implement、verify、review 收敛。本 change 将探索结论落到 OpenSpec 规划资产中，作为后续每个模块化 change 的入口地图。

## 范围

### 范围内

- 固化当前仓库事实基线：已有 WPF 职责、C# capture/analysis 链、native DSP 现状、JUCE/protocol 现状、是否已有 Flutter 资产。
- 输出后续顶层 change 切分方案，说明范围、不包含、依赖、建议 profile 和验证关注点。
- 明确 Flutter、host、native core、JUCE sender、protocol 的职责边界。
- 明确哪些结论来自代码/codemap/OpenSpec/docs，哪些仍是推断或需后续 spike。
- 生成 `work.md`、`specs/change/spec.md`、`design.md`、`tasks.md`、`evidence.md`，供 readiness 审查。

### 范围外

- 不创建 Flutter 项目、不修改 WPF UI、不迁移任何产品代码。
- 不修改 `DesktopLoudnessMeter.Native` 构建或 ABI。
- 不创建后续 sibling changes；这些 changes 需要在本规划通过 readiness/review 后逐个使用 `opsx-plan` 创建。
- 不替当前活动的 `2026-06-23-native-core-dsp` 完成 verify/review/archive。
- 不承诺 macOS capture 可行性；只为 macOS 适配保留路径和待验证问题。

## 成功标准

- OpenSpec 产物能说明当前仓库中 WPF、C# host、C++ native、JUCE sender、protocol 的真实边界。
- 后续 changes 的切分每个都形成独立模块闭环、验证边界和交付价值。
- 规划明确禁止把多个松散模块或过大的跨层迁移塞进一个 change。
- 规划明确首个后续实施入口和前置条件。
- 所有保留推断都有处理方式：进入后续 change 的 readiness notes、task、验证项，或明确 out-of-scope。

## 需求 / 验收摘要

- [R1] 规划必须基于当前仓库事实，而不是抽象目标。
- [R2] 规划必须输出模块化 top-level changes，并说明依赖顺序。
- [R3] 规划必须澄清 Flutter、host、native core、JUCE sender、protocol 的边界。
- [R4] 规划必须标明事实、推断、风险和回到 OpsX 阶段的触发条件。
- [R5] 规划必须保持 OpsX/OpenSpec 生命周期，不绕开 readiness、implement、verify、review、archive。

## 方案摘要

采用“规划 change + 后续模块化 changes”的路线。本 change 不改产品行为，只把探索证据、切分结论、边界矩阵和后续 change 入口固化为可审查文档。后续实施从 `host-snapshot-command-contract` 开始，让 Flutter shell 有稳定输入输出边界；native core 方向先等 `2026-06-23-native-core-dsp` 归档，再处理跨平台构建边界和 source ingest 状态机。

## 文件 / Ownership

### 允许修改

- `openspec/changes/2026-06-29-cross-platform-architecture-plan/.openspec.yaml` — 当前 change 元数据。
- `openspec/changes/2026-06-29-cross-platform-architecture-plan/work.md` — 当前工作包与切分结论。
- `openspec/changes/2026-06-29-cross-platform-architecture-plan/specs/change/spec.md` — 当前规划能力规格。
- `openspec/changes/2026-06-29-cross-platform-architecture-plan/design.md` — 架构边界和后续 change map。
- `openspec/changes/2026-06-29-cross-platform-architecture-plan/tasks.md` — 规划 change 的执行合同。
- `openspec/changes/2026-06-29-cross-platform-architecture-plan/evidence.md` — 只读探索和后续 gate 证据账本。

### 禁止修改

- `src/**`
- `tests/**`
- `docs/**`
- `.aiknowledge/**`
- 其他 `openspec/changes/**`，除非后续 gate 明确要求引用或修正。

## Git / Workspace

- Workspace Strategy: `current-worktree`
- Workspace Path: `E:\gitlab\uMeter`
- Workspace Branch: `opsx/native-core-dsp-migration`
- Base Branch: `opsx/native-core-dsp-migration` at `2a2b5d185c144534972a81f8f8158f01d207489f`
- Merge Target: `opsx/native-core-dsp-migration`
- Workspace Rationale:
  - dirty 分类：本 change 创建前工作树干净；创建后唯一 dirty 为 `openspec/changes/2026-06-29-cross-platform-architecture-plan/**`，归属本 change。无明确无关 dirty file；无归属不明 dirty file。
  - 链式判断：当前分支承载活动 change `2026-06-23-native-core-dsp`，本规划依赖其当前事实并要求其先完成 verify/review/archive；因此按链式规划处理，`base_head=2a2b5d185c144534972a81f8f8158f01d207489f`。
  - isolated 触发：本 change 只写 OpenSpec 规划文档，不改共享接口/schema/产品代码，不需要并行 worker 或隔离依赖；虽然后续实施会触及公共边界，但本 change 本身不触发 isolated worktree。
  - 结论：`current-worktree`。若 readiness 要求在 native DSP 归档后重定基线，再更新本段或新开后续 change。

## 验证计划

- `opsx changes list`，预期：能看到本 change 与当前 native DSP 活动 change，且阶段清晰。
- `opsx changes resolve 2026-06-29-cross-platform-architecture-plan`，预期：解析到当前 change root。
- `rg --files -g "pubspec.yaml"`，预期：无结果，证明当前仓库尚无 Flutter 项目资产。
- `git status --short --branch`，预期：dirty 仅包含本 change 文档。
- `opsx-readiness 2026-06-29-cross-platform-architecture-plan` 或等效 readiness 流程，预期：审查 work/spec/design/tasks/evidence 一致性。

## 稳定化 / 人工平台不确定性

- 是否需要 `[stabilization-first]`：no
- 不确定性来源：后续 Flutter bridge、macOS capture、native cross-platform build 可行性仍需在各自 change 中验证；本 change 只要求规划明确这些不确定性。
- 人工 / 平台覆盖：本 change 不需要人工 UI 或平台 smoke；后续 Flutter/packaging/native build changes 必须各自定义 manual/platform matrix。
- 最终硬化策略：readiness/review 检查文档一致性；不添加产品测试。

## 回退 / 升级

- 回退方式：删除或修订 `openspec/changes/2026-06-29-cross-platform-architecture-plan/**`，不会影响产品代码。
- 升级条件：如果 readiness 发现某个推荐 change 仍然过大/过小，回到 plan 修订切分；如果发现关键事实不足以支撑切分，回到 opsx-explore 补只读调查。

## Bugfix Root Cause

none（feature）。

## Rejected Attempts

- 不采用“一个超大 Flutter + C++ Core migration change”：跨 UI、host、native build、source ingest、settings、protocol、packaging，无法一次收敛。
- 不采用“直接先建 Flutter 项目”：当前仓库没有 Flutter 资产，且缺少 host snapshot/command contract，先建 UI 会把 WPF ViewModel 语义硬搬过去。
- 不采用“再开一个 native analyzer change”：`2026-06-23-native-core-dsp` 已覆盖 PCM/DSP 主路径，后续 native work 应等其归档后聚焦 build boundary 或 source ingest。

## Readiness Notes

- readiness 需检查 R 编号在 spec/design/tasks 中一致。
- readiness 需检查本 change 是否确实只规划，不假装完成后续实施。
- readiness 需检查后续 top-level changes 是否满足模块闭环和 OpenSpec 下限。
- readiness 需检查事实/推断和前置依赖是否足够清晰。
