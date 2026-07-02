# Tasks：2026-06-29-cross-platform-architecture-plan

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

## 1. 架构规划产物

- [x] 1.1 [direct] 固化仓库事实基线

  **需求追踪**：[R1, R4] → [U1, U4]
  **执行方式**：[direct]
  **Allowed Files**：
  - `openspec/changes/2026-06-29-cross-platform-architecture-plan/work.md` — 事实基线和 scope
  - `openspec/changes/2026-06-29-cross-platform-architecture-plan/design.md` — 上下文和边界矩阵
  - `openspec/changes/2026-06-29-cross-platform-architecture-plan/evidence.md` — controller 串行追加只读证据

  **验收标准**：
  - [x] 记录当前仓库已有 WPF/.NET 8 app、C++ native DSP、JUCE sender、protocol/sender kit。
  - [x] 记录当前未发现 Flutter 项目资产。
  - [x] 记录 `2026-06-23-native-core-dsp` 是前置活动 change。
  - [x] 区分已证实事实和仍需后续验证的推断。

  **验证命令 / 方法**：
  - `opsx changes list`，预期：列出 `2026-06-23-native-core-dsp` 与本 change。
  - `rg --files -g "pubspec.yaml"`，预期：无结果。
  - `Get-Content -Raw -Encoding UTF8 .aiknowledge\codemap\index.md`，预期：命中相关 active codemap。

  **Evidence Required**：
  - 命令、退出码、关键输出摘要追加到 `evidence.md`。

  **Done When**：
  - planning 文档中的事实基线可被 readiness 追溯到当前仓库证据。

  **依赖**：无

- [x] 1.2 [direct] 生成模块化 change 切分方案

  **需求追踪**：[R2, R3] → [U2, U3]
  **执行方式**：[direct]
  **Allowed Files**：
  - `openspec/changes/2026-06-29-cross-platform-architecture-plan/work.md` — 切分结论和推荐 top-level changes
  - `openspec/changes/2026-06-29-cross-platform-architecture-plan/specs/change/spec.md` — 可验证规划需求
  - `openspec/changes/2026-06-29-cross-platform-architecture-plan/design.md` — 边界矩阵和决策

  **验收标准**：
  - [x] 推荐 changes 覆盖 Flutter app shell / desktop host integration、native core build、signal source ingest、settings/workspace、JUCE/protocol、packaging/startup/tray。
  - [x] 每个推荐 change 写明范围、不包含、依赖、建议 change 名和 work 关注点。
  - [x] 明确 Flutter、host、native core、JUCE sender、protocol 的职责边界。
  - [x] 明确首个后续实施入口和前置条件。

  **验证命令 / 方法**：
  - 人工阅读 `work.md` 的 `推荐切分 / Top-Level Changes`，预期：每项包含范围、不包含、依赖、建议 change 名和 work 关注点。
  - 人工阅读 `design.md` 的 `边界矩阵`，预期：覆盖 Flutter、host、native core、JUCE sender、protocol、settings、packaging/startup。

  **Evidence Required**：
  - readiness 阶段记录检查结果；若发现粒度问题，追加修订摘要。

  **Done When**：
  - 后续任一模块都能单独进入 `opsx-plan`，而不依赖本 change 塞入实施任务。

  **依赖**：Task 1.1

- [x] 1.3 [direct] 建立 OpsX 生命周期入口

  **需求追踪**：[R5] → [U5]
  **执行方式**：[direct]
  **Allowed Files**：
  - `openspec/changes/2026-06-29-cross-platform-architecture-plan/tasks.md` — 执行合同
  - `openspec/changes/2026-06-29-cross-platform-architecture-plan/evidence.md` — gate 证据账本
  - `openspec/changes/2026-06-29-cross-platform-architecture-plan/.openspec.yaml` — gate 状态由对应 OpsX 阶段维护
  - `openspec/changes/2026-06-29-cross-platform-architecture-plan/work.md` — readiness notes

  **验收标准**：
  - [x] tasks 不包含产品代码实现任务。
  - [x] work 明确本 change 完成 plan 后必须进入 readiness。
  - [x] 后续 implementation changes 被要求单独执行 `opsx-plan -> readiness -> implement -> verify -> review -> archive`。
  - [x] evidence 保留 readiness/verify/review/archive ledger 入口。

  **验证命令 / 方法**：
  - `opsx changes resolve 2026-06-29-cross-platform-architecture-plan`，预期：解析到当前 change root。
  - 后续 `opsx-readiness 2026-06-29-cross-platform-architecture-plan` 或等效 readiness 流程，预期：审查通过或给出 plan 修订项。

  **Evidence Required**：
  - resolve 命令输出；readiness 结果由 readiness 阶段追加。

  **Done When**：
  - 本 change 可以交给 `opsx-readiness`，且 readiness 不会误以为应当实施 Flutter/C++ 产品代码。

  **依赖**：Task 1.2

## 2. 后续阶段

- [x] 2.1 [direct] 执行 readiness 审查

  **需求追踪**：[R5] → [U5]
  **执行方式**：[direct]
  **Allowed Files**：
  - `openspec/changes/2026-06-29-cross-platform-architecture-plan/evidence.md` — readiness evidence
  - `openspec/changes/2026-06-29-cross-platform-architecture-plan/.openspec.yaml` — readiness gate status
  - `openspec/changes/2026-06-29-cross-platform-architecture-plan/work.md` — readiness 修订如需要
  - `openspec/changes/2026-06-29-cross-platform-architecture-plan/specs/change/spec.md` — readiness 修订如需要
  - `openspec/changes/2026-06-29-cross-platform-architecture-plan/design.md` — readiness 修订如需要
  - `openspec/changes/2026-06-29-cross-platform-architecture-plan/tasks.md` — readiness 修订如需要

  **验收标准**：
  - [x] readiness 检查 R 编号、U 编号、tasks 追踪、worktree rationale 和 scope 是否一致。
  - [x] readiness 检查推荐切分是否符合 OpenSpec 下限和模块闭环要求。
  - [x] readiness 明确下一步是本 planning change 的 verify/review/archive，还是回到 plan 修订。

  **验证命令 / 方法**：
  - `opsx-readiness 2026-06-29-cross-platform-architecture-plan` 或当前仓库等效 readiness 流程，预期：pass / pass_with_warnings / fail 均写入 evidence。

  **Evidence Required**：
  - readiness 决策、findings、修订摘要和下一步。

  **Done When**：
  - readiness gate 已有 fresh evidence，且 `.openspec.yaml` 与 `evidence.md` 同步。

  **依赖**：Task 1.3
