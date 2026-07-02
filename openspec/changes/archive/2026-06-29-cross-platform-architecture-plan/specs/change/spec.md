# Spec：2026-06-29-cross-platform-architecture-plan

正文默认使用中文；路径、命令、配置键名、frontmatter key 和状态枚举保持原文。

## 新增需求

### 需求:事实基线
**Trace**: R1
**Slice**: architecture-planning/fact-baseline
规划必须基于当前 uMeter 仓库的 codemap、OpenSpec、docs 和源码事实，并禁止把未验证设想写成已完成能力。

#### 场景:记录当前技术栈事实
- **当** 阅读规划产物
- **那么** 可以看到当前仓库已证实存在 WPF/.NET 8 Windows app、C++ native DSP、JUCE VST3 sender、`.NET SenderKit`、`DesktopLoudnessMeter.Protocol`，且当前尚未发现 Flutter 项目资产

#### 场景:记录当前活动 change
- **当** 规划讨论 native analysis core
- **那么** 必须把 `2026-06-23-native-core-dsp` 作为前置活动 change，而不是重复规划同一 PCM/DSP 主路径

### 需求:模块化 change 切分
**Trace**: R2
**Slice**: architecture-planning/change-slicing
规划必须输出大小合适的 top-level changes，每个 change 必须形成独立模块闭环、验证边界和交付价值。

#### 场景:后续 change 可独立进入 OpenSpec
- **当** 用户选择任一推荐 change 继续推进
- **那么** 该 change 必须具有清晰范围、不包含项、依赖、建议 profile 和 work 关注点

#### 场景:避免过大迁移
- **当** 某个候选 change 同时覆盖 Flutter UI、native core、source ingest、settings、protocol 和 packaging
- **那么** 规划必须要求继续拆分，不允许作为单个实施 change

#### 场景:避免零碎 change
- **当** 某个候选 change 不能形成可审查、可验证、可归档的模块闭环
- **那么** 规划必须要求并回上层模块或回到 explore/plan 重新判定

### 需求:跨层职责边界
**Trace**: R3
**Slice**: architecture-planning/boundary-map
规划必须澄清 Flutter、host、native core、JUCE sender、protocol 之间的职责边界，并指出现有 WPF/C# 职责迁移方向。

#### 场景:Flutter 边界
- **当** 规划描述 UI 演进
- **那么** Flutter 必须承担 UI、交互、主题、窗口和设置层，且通过 host contract 消费状态和发出命令

#### 场景:native core 边界
- **当** 规划描述 C++ Core 演进
- **那么** native core 必须逐步承担 DSP、平台无关 source ingest 状态机、snapshot generation 和核心状态机，并明确 Windows capture glue 可暂留 host

#### 场景:sender/protocol 边界
- **当** 规划描述 JUCE sender 和 protocol
- **那么** JUCE sender 必须保持发送端生命周期、DAW 内 source identity、audio packet 和 ack 状态职责，protocol 必须保留版本化 lifecycle vocabulary

### 需求:事实与推断分离
**Trace**: R4
**Slice**: architecture-planning/uncertainty-handling
规划必须把已证实事实、仍属推断、blocking 风险和处理方式分开记录。

#### 场景:保留推断
- **当** 规划涉及 Flutter bridge、macOS capture 或跨平台 native build
- **那么** 必须标明这些内容仍需后续 change 验证，而不是在本 change 中宣称完成

#### 场景:发现 blocking 风险
- **当** 后续 readiness 或 review 发现事实不足以支撑切分结论
- **那么** 必须回到 opsx-explore 或 opsx-plan 修订，而不是硬进入 implement

### 需求:OpsX 生命周期守护
**Trace**: R5
**Slice**: architecture-planning/opsx-lifecycle
规划必须明确后续所有模块化 changes 都要经过 explore、plan、readiness、implement、verify、review、archive，不允许绕开任一 gate。

#### 场景:本规划 change 完成 plan
- **当** work/spec/design/tasks/evidence 生成后
- **那么** 下一步必须进入 `opsx-readiness` 审查一致性和可实施性

#### 场景:后续实施 change 启动
- **当** 用户选择任一推荐模块继续实施
- **那么** 必须为该模块单独执行 `opsx-plan`，并在 readiness 通过后才允许 implement

## 修改需求

无。

## 移除需求

无。
