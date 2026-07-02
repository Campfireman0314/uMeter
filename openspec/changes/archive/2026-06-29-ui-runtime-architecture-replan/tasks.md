# Tasks: 2026-06-29-ui-runtime-architecture-replan

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

## 1. UI runtime replan

- [x] 1.1 [direct] 固化 WebView runtime 决策

  **需求追踪**：[R1] → [U1]
  **执行方式**：[direct]
  **Allowed Files**：
  - `openspec/changes/2026-06-29-ui-runtime-architecture-replan/work.md` — runtime 决策和 scope
  - `openspec/changes/2026-06-29-ui-runtime-architecture-replan/design.md` — 旧规划修订关系和决策
  - `openspec/changes/2026-06-29-ui-runtime-architecture-replan/evidence.md` — controller 串行追加只读证据

  **验收标准**：
  - [x] 明确旧 Flutter route 是历史归档规划，不再作为主 UI runtime。
  - [x] 明确 WebView 是新的主 UI runtime。
  - [x] 明确本 change 是规划修订，不实施产品代码。

  **验证命令 / 方法**：
  - `Get-Content -Encoding UTF8 openspec/changes/archive/2026-06-29-cross-platform-architecture-plan/work.md`，预期：旧规划包含 Flutter route。
  - 人工阅读本 change `work.md` / `design.md`，预期：WebView route 取代 Flutter route。

  **Evidence Required**：
  - 旧规划读取摘要、本 change 修订摘要追加到 `evidence.md`。

  **Done When**：
  - 后续 UI shell change 可引用本 replan，而不会继续沿用 Flutter route。

  **依赖**：无

- [x] 1.2 [direct] 定义 HostBridge v1 和 Web frontend 边界

  **需求追踪**：[R2, R3, R4] → [U2, U3, U4]
  **执行方式**：[direct]
  **Allowed Files**：
  - `openspec/changes/2026-06-29-ui-runtime-architecture-replan/work.md` — 范围、依赖、后续 change map
  - `openspec/changes/2026-06-29-ui-runtime-architecture-replan/specs/change/spec.md` — 可验证规划需求
  - `openspec/changes/2026-06-29-ui-runtime-architecture-replan/design.md` — HostBridge/Web UI/Core/Host/WPF 边界矩阵
  - `openspec/changes/2026-06-29-ui-runtime-architecture-replan/evidence.md` — controller 串行追加只读证据

  **验收标准**：
  - [x] 定义 Windows WebView2 主路径和 macOS WKWebView reserved boundary。
  - [x] 定义 HostBridge v1 三层：native WebView message transport、typed command bus、JSON DTO payload。
  - [x] 定义 TypeScript + React + Vite，DOM UI 与 Canvas/WebGL 图表分工。
  - [x] 明确 C++ Core、Host glue、JUCE sender、Protocol 和 WPF fallback 职责。

  **验证命令 / 方法**：
  - `Get-Content -Encoding UTF8 .aiknowledge/codemap/host-contract.md`，预期：host snapshot/command/result contract 已存在。
  - `rg --files -g "package.json" -g "vite.config.*" -g "pubspec.yaml"`，预期：当前无 Web frontend/Flutter 项目资产。
  - 人工阅读 `design.md` 的 `边界矩阵`，预期：覆盖 Web UI、Windows runner、macOS runner、HostBridge、C++ Core、JUCE sender、Protocol、WPF。

  **Evidence Required**：
  - 命令输出摘要和边界矩阵检查结果追加到 `evidence.md`。

  **Done When**：
  - `webview-windows-app-shell` 可以按本边界独立进入 plan/readiness。

  **依赖**：Task 1.1

- [x] 1.3 [direct] 输出后续 change map 和 lifecycle guard

  **需求追踪**：[R5] → [U5]
  **执行方式**：[direct]
  **Allowed Files**：
  - `openspec/changes/2026-06-29-ui-runtime-architecture-replan/work.md` — 推荐 top-level changes、顺序和 completion audit 约束
  - `openspec/changes/2026-06-29-ui-runtime-architecture-replan/specs/change/spec.md` — change map 生命周期需求
  - `openspec/changes/2026-06-29-ui-runtime-architecture-replan/design.md` — 决策和风险
  - `openspec/changes/2026-06-29-ui-runtime-architecture-replan/tasks.md` — 执行合同
  - `openspec/changes/2026-06-29-ui-runtime-architecture-replan/evidence.md` — gate 证据账本

  **验收标准**：
  - [x] 推荐 changes 包含 `webview-windows-app-shell`、`native-core-build-boundary`、`settings-workspace-schema`、`native-source-ingest-core`、`juce-sender-protocol-boundary`、`webview-packaging-startup-tray`、`webview-chart-parity`、`wpf-retirement`、`macos-readiness`。
  - [x] 每个推荐 change 写明范围、不包含、依赖和 work 关注点。
  - [x] 明确每个后续 change 必须独立执行 OpsX/OpenSpec 生命周期。
  - [x] 明确单个模块归档不代表整体 goal 完成，最终需要 completion audit。

  **验证命令 / 方法**：
  - 人工阅读 `work.md` 的 `推荐切分 / Top-Level Changes`，预期：每项包含范围、不包含、依赖和 work 关注点。
  - 人工阅读 `specs/change/spec.md` 的 R5，预期：生命周期和 completion audit 约束清楚。

  **Evidence Required**：
  - readiness 阶段记录检查结果；若发现粒度问题，追加修订摘要。

  **Done When**：
  - 本 change 可以交给 `opsx-readiness`，且 readiness 不会误以为应实施 WebView/C++ 产品代码。

  **依赖**：Task 1.2

## 2. 后续阶段

- [x] 2.1 [direct] 执行 readiness 审查

  **需求追踪**：[R1, R2, R3, R4, R5] → [U1, U2, U3, U4, U5]
  **执行方式**：[direct]
  **Allowed Files**：
  - `openspec/changes/2026-06-29-ui-runtime-architecture-replan/evidence.md` — readiness evidence
  - `openspec/changes/2026-06-29-ui-runtime-architecture-replan/.openspec.yaml` — readiness gate status
  - `openspec/changes/2026-06-29-ui-runtime-architecture-replan/work.md` — readiness 修订如需要
  - `openspec/changes/2026-06-29-ui-runtime-architecture-replan/specs/change/spec.md` — readiness 修订如需要
  - `openspec/changes/2026-06-29-ui-runtime-architecture-replan/design.md` — readiness 修订如需要
  - `openspec/changes/2026-06-29-ui-runtime-architecture-replan/tasks.md` — readiness 修订如需要

  **验收标准**：
  - [x] readiness 检查 R 编号、U 编号、tasks 追踪、worktree rationale 和 scope 是否一致。
  - [x] readiness 检查旧 Flutter route 与新 WebView route 的关系是否明确。
  - [x] readiness 检查推荐切分是否符合 OpenSpec 下限和模块闭环要求。
  - [x] readiness 明确下一步是本 planning change 的 verify/review/archive，还是回到 plan 修订。

  **验证命令 / 方法**：
  - `opsx-readiness 2026-06-29-ui-runtime-architecture-replan` 或当前仓库等效 readiness 流程，预期：pass / pass_with_warnings / fail 均写入 evidence。

  **Evidence Required**：
  - readiness 决策、findings、修订摘要和下一步。

  **Done When**：
  - readiness gate 已有 fresh evidence，且 `.openspec.yaml` 与 `evidence.md` 同步。

  **依赖**：Task 1.3
