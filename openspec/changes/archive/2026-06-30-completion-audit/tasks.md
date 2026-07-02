# Tasks: 2026-06-30-completion-audit

## Default Constraints

- Forbidden Files:
  - 产品源码、测试源码、docs、`.aiknowledge/**`
  - 其他 `openspec/changes/**`
- Shared Artifacts:
  - `.openspec.yaml` - controller 串行更新
  - `tasks.md` - controller 串行更新
  - `evidence.md` - controller 串行更新
- Worker Return Format: `DONE | DONE_WITH_CONCERNS | NEEDS_CONTEXT | BLOCKED`

## 1. Build Completion Audit Matrix

- [x] 1.1 [stabilization-first] 构建最终态 requirements-to-evidence matrix

  **需求追踪**: [R1, R2] -> [U1, U2]
  **执行方式**: [stabilization-first]
  **Allowed Files**:
  - `openspec/changes/2026-06-30-completion-audit/evidence.md`

  **验收标准**:
  - [x] matrix 覆盖所有显式最终态要求。
  - [x] 每项都有证据来源和 evidence grade。
  - [x] 弱证据或推断不会被标记为 `proven`。

  **验证命令 / 方法**:
  - 读取 `.aiknowledge/codemap/*.md` 和相关 `openspec/changes/archive/**/evidence.md`。
  - `rg -n "window.chrome.webview|window.webkit|WebView2|HostBridge|DlmSourceIngestCore|DlmPcmAnalyzer|WPF|fallback|ICommand|ObservableCollection" src tests .aiknowledge openspec/changes/archive`。

  **Evidence Required**:
  - Stabilization Rationale
  - Requirements Matrix
  - Evidence Grade Legend
  - Final Hardening

  **Stabilization Rationale**:
  - 不确定性来源：总目标是跨多个已归档 change 的综合状态；单个归档证据不能证明整体完成。
  - 为什么不能先写目标行为测试：completion 是架构级审计，需要多源证据和当前源码核对。
  - 退出条件：matrix 完整、等级清晰、缺口明确。

## 2. Run Current Regression Evidence

- [x] 2.1 [stabilization-first] 当前 Windows/WebView 主路径回归验证

  **需求追踪**: [R3] -> [U3]
  **执行方式**: [stabilization-first]
  **Allowed Files**:
  - `openspec/changes/2026-06-30-completion-audit/evidence.md`

  **验收标准**:
  - [x] Web frontend tests pass。
  - [x] Web frontend production build pass。
  - [x] .NET regression tests pass。
  - [x] 审计未修改产品源码。

  **验证命令 / 方法**:
  - `npm test` in `src/DesktopLoudnessMeter.Web`
  - `npm run build` in `src/DesktopLoudnessMeter.Web`
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj`
  - `git diff --stat -- src tests docs .aiknowledge`

  **Evidence Required**:
  - Command output summary
  - Dirty/scope status

  **Stabilization Rationale**:
  - 不确定性来源：audit 是只读，但验证命令必须证明当前主路径仍可运行。
  - 退出条件：当前可运行验证通过，或失败被结构化记录。

## 3. Verdict And Gap Map

- [x] 3.1 [stabilization-first] 输出 completion verdict 和后续 change map

  **需求追踪**: [R4, R5] -> [U4, U5]
  **执行方式**: [stabilization-first]
  **Allowed Files**:
  - `openspec/changes/2026-06-30-completion-audit/evidence.md`

  **验收标准**:
  - [x] verdict 为 `complete` 或 `not_complete`，且与 matrix 一致。
  - [x] 若 `not_complete`，包含 remaining gap map。
  - [x] 若 `complete`，列出逐条完成证据并允许 goal completion audit。
  - [x] OpenSpec 后续路由明确：archive 后是否需要创建 implementation changes。

  **验证命令 / 方法**:
  - `rg -n "verdict:|remaining gap|not_complete|complete|proven|weak_evidence|incomplete|contradicted" openspec/changes/2026-06-30-completion-audit/evidence.md`

  **Evidence Required**:
  - Final verdict
  - Remaining gap map
  - Goal completion decision

  **Stabilization Rationale**:
  - 不确定性来源：完成性结论必须由 matrix 驱动，不能凭推荐序列已完成。
  - 退出条件：verdict 与 evidence grade 一致，后续动作不含矛盾。
