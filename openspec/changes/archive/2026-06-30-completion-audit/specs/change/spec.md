# Spec: 2026-06-30-completion-audit

正文默认使用中文；路径、命令、配置键名和状态枚举保持原文。

## 新增需求

### 需求: final-state coverage audit
**Trace**: R1
**Slice**: completion/final-state-coverage

completion audit 必须把用户目标中的每条最终态要求拆成独立检查项，并为每项给出证据状态。

#### 场景: 审计最终态覆盖
- **当** reviewer 读取 audit evidence
- **那么** 可以看到 WebView UI、Windows WebView2、macOS WKWebView boundary、C++ Core、Host glue、HostBridge v1、Web frontend、JUCE sender、Protocol vocabulary、Windows-first/macOS-boundary、WPF retirement/isolation 的逐条结论

### 需求: evidence-grade verdicts
**Trace**: R2
**Slice**: completion/evidence-grading

completion audit 必须基于当前仓库证据给出 `proven`、`incomplete`、`weak_evidence`、`contradicted` 或 `not_applicable_by_scope`，并禁止把推断写成通过。

#### 场景: 审计证据等级
- **当** 某个最终态要求缺少当前源码、命令输出、归档证据或运行证据
- **那么** audit verdict 不能是 `complete`

### 需求: current regression guard
**Trace**: R3
**Slice**: completion/current-regression

completion audit 必须运行当前可用的 Web/.NET 回归命令，证明审计期间 Windows WebView 主路径仍可构建和测试。

#### 场景: 验证当前主路径
- **当** 执行 audit 验证命令
- **那么** Web tests/build 和 .NET tests 通过，或失败被记录为带 owner/follow-up 的 allowed failure

### 需求: gap map and next changes
**Trace**: R4
**Slice**: completion/gap-map

如果最终目标未完成，completion audit 必须输出 remaining gap map，包含缺口、证据、影响、建议后续 change 和优先级。

#### 场景: 目标未完成
- **当** audit verdict 不是 `complete`
- **那么** evidence 中包含后续 change map，且不会调用 goal completion

### 需求: lifecycle integrity
**Trace**: R5
**Slice**: completion/lifecycle

completion audit change 必须完整走 OpenSpec lifecycle，并明确 audit 完成不等于总体 goal 完成，除非 evidence matrix 逐条证明最终态。

#### 场景: 审计归档
- **当** audit change 归档
- **那么** verify/review/archive gates 存在，且 final verdict 与 evidence matrix 一致

## 修改需求

无。

## 移除需求

无。
