# Design: 2026-06-30-completion-audit

正文默认使用中文；路径、命令、配置键名和状态枚举保持原文。

## 上下文

当前推荐实施序列已经从 replan 到 macOS readiness 全部归档。codemap 显示 Windows WebView2 主路径、HostBridge v1、React/Vite shell、Canvas charts、native DSP/source ingest、protocol/JUCE 边界、WPF retirement 和 macOS skeleton 均有对应模块记录。但部分目标仍可能只达到 boundary/readiness，而不是完整支持。

## 目标 / 非目标

**目标：**
- 做一次证据驱动的 completion audit。
- 给出总体 verdict：`complete` 或 `not_complete`。
- 如果 `not_complete`，列出后续 changes。

**非目标：**
- 不修复 audit 发现的缺口。
- 不把已有归档数量当作完成证明。
- 不跳过 OpenSpec readiness/verify/review/archive。

## 需求追踪

- [R1] -> [U1]
- [R2] -> [U2]
- [R3] -> [U3]
- [R4] -> [U4]
- [R5] -> [U5]

## 实施单元

### [U1] Final-state requirements matrix
- 关联需求: [R1]
- 模块边界:
  - `openspec/changes/2026-06-30-completion-audit/evidence.md`
  - `openspec/changes/archive/**/evidence.md`
  - `.aiknowledge/codemap/*.md`
- 验证方式: evidence 中包含逐条 requirements matrix。

### [U2] Evidence grading and verdict
- 关联需求: [R2]
- 模块边界:
  - `openspec/changes/2026-06-30-completion-audit/evidence.md`
  - `src/**` 只读
  - `tests/**` 只读
- 验证方式: 每条要求都有证据等级和 verdict；弱证据不得升级为 proven。

### [U3] Current regression commands
- 关联需求: [R3]
- 模块边界:
  - `src/DesktopLoudnessMeter.Web`
  - `tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj`
- 验证方式: 运行 Web tests/build 和 .NET tests。

### [U4] Remaining gap map
- 关联需求: [R4]
- 模块边界:
  - `openspec/changes/2026-06-30-completion-audit/evidence.md`
- 验证方式: verdict 非 complete 时，evidence 包含后续 change map。

### [U5] Audit lifecycle integrity
- 关联需求: [R5]
- 模块边界:
  - `.openspec.yaml`
  - `work.md`
  - `specs/change/spec.md`
  - `design.md`
  - `tasks.md`
  - `evidence.md`
- 验证方式: readiness/verify/review/archive gates 与 evidence verdict 一致。

## 审计矩阵维度

| Requirement | Evidence Sources | Strong Evidence |
|-------------|------------------|-----------------|
| WebView 承载主 UI | app startup/router/WebView shell/code/tests | WebView is default startup/activation/tray path; WPF not user fallback |
| Web frontend owns content/workflows | React/Vite AppShell/charts/settings commands | Web UI renders source list/detail/settings/chart workflows |
| Windows WebView2 | WebView2 host code/package/tests | WebView2 shell initializes and is primary |
| macOS WKWebView boundary | MacHost skeleton/readiness doc | Boundary exists and explicitly marks runtime follow-ups |
| C++ Core DSP/source ingest/snapshot/core state | native DSP/ingest code and wrappers | DSP + platform-neutral source lifecycle in native with tests |
| Host glue only | C# host modules/codemap | Host does not own platform-neutral core state beyond glue/adapters |
| HostBridge v1 | native message transport + typed bus + JSON DTOs | WebView2 transport, HostBridge controller, TS DTOs |
| Web TypeScript React Vite / Canvas | package.json/AppShell/charts/tests | DOM shell and Canvas charts are active |
| JUCE sender / Protocol vocabulary | JUCE/SenderKit/Protocol tests | Sender-side responsibility and vocabulary constants |
| WPF retired/isolated | App startup/MainWindow/tray/tests | No normal user fallback; WPF remains oracle seam only |

## 决策

1. **audit verdict 默认为未完成，直到证据证明完成。**
   - 原因：用户目标要求 completion audit，弱证据不能当作完成。

2. **macOS readiness 可以满足“预留边界”，但不能满足“macOS runtime parity”。**
   - 原因：当前目标写的是 macOS 预留 WKWebView runner 边界，非完整 macOS app；audit 必须按该原文判断。

3. **Host glue / C++ Core 职责需要代码级证据。**
   - 原因：可能存在 C# host 仍承担平台无关核心状态或 snapshot generation；必须检查源码和 codemap。

## 风险 / 权衡

- audit 可能发现 goal 未完成；这是正确结果，不是失败。
- 运行命令只覆盖 Windows 当前环境，macOS 真机项只能按目标 scope 判定为 boundary 或 follow-up。
- 如果审计范围扩大到修复，会破坏 change 闭环，因此发现 gap 后仅创建建议。

## 稳定化策略

- `[stabilization-first]` 适用于 U1/U2/U4：通过 adjustment ledger 记录证据等级变化和最终 verdict。
- doc impact：`TASK_EVIDENCE_ONLY`，除非发现 audit spec 本身遗漏最终态要求。

## 回退方案

- 如果 readiness 发现 audit matrix 漏需求，回到 plan 补 spec/tasks。
- 如果 implement 发现重大未知，先补只读 explore evidence；若仍无法判定，标记 weak evidence 并给后续 change。

## 并行 / Subagent 边界

- 默认串行，避免 matrix 与 verdict 分裂。
- 可并行执行独立只读 grep，但 evidence/gates 由 controller 串行写入。

## 知识沉淀

归档后根据 audit verdict 更新 `ui-runtime-architecture` 或新增 completion/gap codemap；若发现旧 codemap 声明过强，应标记并修订。
