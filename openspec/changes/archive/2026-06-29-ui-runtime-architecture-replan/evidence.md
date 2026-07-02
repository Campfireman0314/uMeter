# Evidence: 2026-06-29-ui-runtime-architecture-replan

正文默认使用中文；路径、命令、配置键名、frontmatter key 和状态枚举保持原文。

`evidence.md` 是唯一证据账本。readiness、test-cycle、verify、risk review 和人工验证结论都追加写入这里。

## Final Gate Summary

- final_readiness:
  - status: pass
  - evidence_ref: Readiness / 2026-06-29T17:11:17+08:00
- final_verify:
  - status: pass
  - evidence_ref: Verify / 2026-06-29T17:12:13+08:00
- final_review:
  - status: pass
  - evidence_ref: Risk Review / 2026-06-29T17:13:10+08:00
- final_archive:
  - status: pass
  - archive_path: `openspec/changes/archive/2026-06-29-ui-runtime-architecture-replan`
  - evidence_ref: Archive / 2026-06-29T17:13:59+08:00
- allowed_failures: []
- manual_followups: []
- knowledge_status:
  - status: done
  - evidence_ref: Knowledge Follow-up / 2026-06-29T17:17:00+08:00; pre_knowledge_status=clean; post_knowledge_status=modified codemap/log entries; diff_stat=5 knowledge files changed including new ui-runtime-architecture codemap

## Exploration / Plan Evidence

### 2026-06-29 plan baseline

- stage: plan
- commands:
  - `opsx changes list`
    - exit_status: 0
    - result: 无活动变更；说明当前不是接续 implementation change，而是需要创建新的 planning change。
  - `git rev-parse HEAD`
    - exit_status: 0
    - result: `05fd0a6e28bcb76d761aa809eda3dc489374b72c`
  - `git branch --show-current`
    - exit_status: 0
    - result: `opsx/native-core-dsp-migration`
  - `git status --short`
    - exit_status: 0
    - result: 创建本 change 前工作树干净。
  - `Get-Content -Encoding UTF8 .aiknowledge/codemap/index.md`
    - exit_status: 0
    - result: 命中 active codemap：`app-startup`、`source-list-workspace`、`host-contract`、`signal-source-ingest`、`analysis-rendering`、`juce-sender`、`audio-analysis`、`native-dsp`、`audio-capture-sources`。
  - `Get-Content -Encoding UTF8 .aiknowledge/pitfalls/index.md`
    - exit_status: 0
    - result: 命中 lifecycle、misc、performance 经验域；本 change 为规划文档，不直接实施运行时逻辑。
  - `Get-Content -Encoding UTF8 openspec/changes/archive/2026-06-29-cross-platform-architecture-plan/work.md`
    - exit_status: 0
    - result: 归档规划的推荐 route 包含 Flutter shell、Flutter packaging 等历史决策。
  - `Get-Content -Encoding UTF8 openspec/changes/archive/2026-06-29-host-snapshot-command-contract/work.md`
    - exit_status: 0
    - result: host snapshot/command/result contract 已归档，范围明确不实现 Flutter shell，可作为 WebView HostBridge v1 的上层契约基础。
  - `Get-Content -Encoding UTF8 .aiknowledge/codemap/host-contract.md`
    - exit_status: 0
    - result: `DesktopMeterHostController`、`HostSnapshot`、`HostCommand`、`HostCommandResult` 等 UI-neutral contract 已存在；WPF adapter 已作为 client。
  - `Get-Content -Encoding UTF8 .aiknowledge/codemap/native-dsp.md`
    - exit_status: 0
    - result: native DSP 主路径已归档；当前 native 项目仍是 Windows/MSVC DLL 边界，跨平台 core build 需后续 change。
  - `rg --files -g "package.json" -g "vite.config.*" -g "pubspec.yaml" -g "*.csproj" -g "*.vcxproj" -g "CMakeLists.txt"`
    - exit_status: 0
    - result: 发现 `.csproj`、`.vcxproj`、JUCE `CMakeLists.txt`；未发现 `package.json`、`vite.config.*` 或 `pubspec.yaml`。
  - `opsx changes init 2026-06-29-ui-runtime-architecture-replan spec-driven --profile strict-change --source-type feature`
    - exit_status: 0
    - result: 已初始化 `openspec/changes/2026-06-29-ui-runtime-architecture-replan`。
- facts:
  - 旧跨平台架构规划已归档，且保留 Flutter 主 UI route 的历史决策。
  - 用户已明确新目标是 “WebView UI + C++ Core”，并废弃未完成 Flutter shell。
  - host snapshot/command/result contract 已归档，可作为 WebView HostBridge v1 的上层消费边界。
  - 当前仓库未发现 Web frontend 或 Flutter project asset。
  - C++ native DSP 主路径已归档，但跨平台 core build boundary 仍未完成。
- inferences:
  - WebView shell 应先接入 host contract，而不是重新定义 WPF ViewModel 语义。
  - React/Vite app、WebView2 transport、DTO serialization 和 TypeScript type boundary 需要在 `webview-windows-app-shell` 中验证。
  - WKWebView runner 可作为 macOS 边界，但 macOS capture/package 风险需要后续 `macos-readiness`。
- non_blocking_uncertainties:
  - WebView2 runtime/package 选型和安装策略。
  - TypeScript DTO 是否手写、生成或通过 schema 同步。
  - HostBridge v1 event stream、command result 和 error DTO envelope 细节。
  - macOS WKWebView/capture/package feasibility。

## Test Cycle Evidence

### 2026-06-29 Task 1.1-1.3 direct

- task: 1.1, 1.2, 1.3
- phase: direct
- command: 文档生成与只读证据核对
- exit_status: 0
- result: `work.md`、`specs/change/spec.md`、`design.md`、`tasks.md`、`evidence.md` 已生成；本 change 不修改产品代码。
- coverage:
  - R1 WebView runtime decision
  - R2 HostBridge v1 and runner boundary
  - R3 Web UI stack and rendering split
  - R4 Core/Host/Sender/Protocol/WPF boundaries
  - R5 change map and lifecycle guard

## Readiness

### 2026-06-29T17:11:17+08:00

- stage: readiness
- decision: pass
- evidence_refs:
  - `work.md`：scope、non-goals、Workspace Rationale、切分结论、后续 change map 完整。
  - `specs/change/spec.md`：R1-R5 全局唯一，均为本 replan change 的可观察规划行为。
  - `design.md`：R1-R5 均映射到 U1-U5，每个 U 均含模块边界。
  - `tasks.md`：Default Constraints 存在，Task 1.1-1.3 已完成，Task 2.1 是 readiness 本身并已完成。
  - `evidence.md`：plan baseline、facts/inferences、manual waiver 和 rejected attempts 已记录。
  - `.openspec.yaml`：readiness gate 写入 `pass` at `2026-06-29T17:11:17+08:00`。
- checks:
  - R/U/task trace：通过；R1-R5、U1-U5 和 Task 1.1-2.1 对齐。
  - runtime 修订关系：通过；旧 Flutter route 作为归档历史保留，本 change 明确 WebView route 是后续主路径。
  - change map 粒度：通过；后续 implementation changes 拆分为可独立 plan/readiness/implement/verify/review/archive 的模块闭环。
  - worktree rationale：通过；本 change 只写规划文档，dirty file 仅限本 change。
  - manual gate：通过；本 planning change 的人工 UI/platform smoke 已用 `[~]` 豁免并写明原因。
- findings:
  - blocking: 无
  - warnings:
    - 本 change 是规划修订交付闭环，不进入产品代码 implement；readiness 通过后下一步应做本 planning change 的 verify/review/archive。
    - WebView2 runtime/package、HostBridge DTO envelope、React/Vite project layout 和 WKWebView/macOS capture 仍是后续 changes 的待验证项，不应在本 change 宣称完成。
- next: 进入本 planning change 的 `opsx-verify`；后续 implementation 从 `webview-windows-app-shell` 单独 plan。

## Verify

### 2026-06-29T17:12:13+08:00

## verify | 2026-06-29T17:12:13+08:00 | pass
方向：work.md + specs/change/spec.md + design.md + tasks.md + evidence.md -> 验证通过
证据：
- `opsx changes resolve 2026-06-29-ui-runtime-architecture-replan` -> `/e/gitlab/uMeter/openspec/changes/2026-06-29-ui-runtime-architecture-replan`
- `opsx changes list` -> 本 change 为 `tasks (4/4) profile=strict-change next=opsx-verify`
- `git rev-parse HEAD` -> `05fd0a6e28bcb76d761aa809eda3dc489374b72c`
- `git status --short --branch` -> 当前分支 `opsx/native-core-dsp-migration...origin/opsx/native-core-dsp-migration [ahead 2]`；untracked 仅 `openspec/changes/2026-06-29-ui-runtime-architecture-replan/**`，归属本 planning change
- `rg -n "final_readiness|stage: readiness|decision: pass|status: pass|\\[ \\]|\\[~\\].*原因|Trace\\*\\*: R|\\[R[0-9]\\]|\\[U[0-9]\\]|webview-windows-app-shell|Flutter route|WebView route" openspec/changes/2026-06-29-ui-runtime-architecture-replan` -> R1-R5、U1-U5、readiness pass、manual waiver、WebView route 和后续 shell change 均可追溯；未发现未终态 manual 项
- `Get-Content -Encoding UTF8 openspec/changes/2026-06-29-ui-runtime-architecture-replan/.openspec.yaml` -> readiness gate 为 `pass` at `2026-06-29T17:11:17+08:00`
- Git: branch=`opsx/native-core-dsp-migration`; HEAD=`05fd0a6e28bcb76d761aa809eda3dc489374b72c`; base=`opsx/native-core-dsp-migration` at same HEAD for this planning change; diff_status=untracked `openspec/changes/2026-06-29-ui-runtime-architecture-replan/**` only, 属于本 change; verification_coverage=worktree OpenSpec 文档 diff; manual_status=waived，原因：本 change 只改规划文档不改运行行为; stabilization_status=none; amendment_status=none
allowed_failures:
- item: 无
  why_non_blocking: 无
  owner_or_followup: 无
  expiry_or_recheck: 无
问题：
- 无 critical finding。
- 无 warning finding；manual 项已用 `[~]` 且包含原因。
需修正：
- 无

## Risk Review

### 2026-06-29T17:13:10+08:00

## risk review | 2026-06-29T17:13:10+08:00 | pass

- stage: review
- decision: pass
- Review Scope Snapshot:
  - branch: `opsx/native-core-dsp-migration`
  - HEAD: `05fd0a6e28bcb76d761aa809eda3dc489374b72c`
  - diff_status: untracked `openspec/changes/2026-06-29-ui-runtime-architecture-replan/**`，归属本 planning change
  - reviewed_files:
    - `openspec/changes/2026-06-29-ui-runtime-architecture-replan/.openspec.yaml`
    - `openspec/changes/2026-06-29-ui-runtime-architecture-replan/work.md`
    - `openspec/changes/2026-06-29-ui-runtime-architecture-replan/specs/change/spec.md`
    - `openspec/changes/2026-06-29-ui-runtime-architecture-replan/design.md`
    - `openspec/changes/2026-06-29-ui-runtime-architecture-replan/tasks.md`
    - `openspec/changes/2026-06-29-ui-runtime-architecture-replan/evidence.md`
- findings:
  - CRITICAL: 无
  - WARNING: 无
  - SUGGESTION: 无
- verify_drift: 未发现；`.openspec.yaml` 的 readiness/verify gate 与 `evidence.md` 中的 readiness/verify 记录一致，manual 项已豁免且含原因。
- exclusions:
  - 本 change 不含产品代码，未审查 `src/**` 行为风险。
  - 后续 implementation changes 需要各自执行独立 review。
- allowed_failures:
  - item: 无
    why_non_blocking: 无
    owner_or_followup: 无
    expiry_or_recheck: 无
- next: 转入 `opsx-archive`。

## Manual Checks

- [~] [manual] 本 planning change 不需要人工 UI/平台 smoke | 原因：范围仅为 OpenSpec 架构规划文档，不修改产品运行行为；后续 `webview-windows-app-shell`、`webview-packaging-startup-tray`、`webview-chart-parity` 和 `macos-readiness` 必须各自定义人工/平台验证。

## Rejected Attempts

### Attempt 1

- hypothesis: 继续沿用归档规划中的 Flutter route。
- change_summary: 未执行，只在 plan 阶段评估。
- verification: 用户已明确废弃未完成 Flutter shell，最终态修订为 WebView UI。
- rejection_reason: 与当前目标冲突，会让后续 implementation change 沿用错误 runtime。

### Attempt 2

- hypothesis: 直接创建 `webview-windows-app-shell` implementation change。
- change_summary: 未执行，只在 plan 阶段评估。
- verification: 归档规划仍指向 Flutter route，后续 change map 需要先统一修订。
- rejection_reason: 缺少新的 WebView runtime planning baseline，implementation evidence source 会冲突。

### Attempt 3

- hypothesis: 创建单一 WebView + C++ Core mega change。
- change_summary: 未执行，只在 plan 阶段评估。
- verification: 对照 codemap 和目标范围，覆盖 UI runtime、bridge、frontend、native build、source ingest、settings、protocol、packaging、charts、macOS readiness 多个能力簇。
- rejection_reason: 无法一次 readiness、implement、verify、review、archive 收敛，违反模块闭环要求。

## Archive

### 2026-06-29T17:13:59+08:00

- stage: archive
- decision: pass
- source_path: `openspec/changes/2026-06-29-ui-runtime-architecture-replan`
- archive_path: `openspec/changes/archive/2026-06-29-ui-runtime-architecture-replan`
- schema: spec-driven
- specs_count: 1
- gates:
  - readiness: pass
  - verify: pass
  - review: pass
  - archive: pass
- manual_followups: []
- allowed_failures: []
- notes:
  - 本 change 是 UI runtime 架构重规划交付闭环，只移动 OpenSpec 文档，不修改产品代码。
  - 旧 Flutter route 保留为归档历史，本 change 作为后续 WebView route 的规划入口。
  - 后续 implementation 仍需逐个模块单独创建 OpenSpec change。

## Knowledge Follow-up

### 2026-06-29T17:17:00+08:00

- pre_knowledge_status: clean
- post_knowledge_status:
  - `M .aiknowledge/codemap/host-contract.md`
  - `M .aiknowledge/codemap/index.md`
  - `M .aiknowledge/codemap/source-list-workspace.md`
  - `M .aiknowledge/logs/2026-06.md`
  - `?? .aiknowledge/codemap/ui-runtime-architecture.md`
- diff_stat:
  - `.aiknowledge/codemap/host-contract.md`: refreshed future consumption boundary from old Flutter wording to WebView HostBridge v1 and added replan source refs
  - `.aiknowledge/codemap/index.md`: added `ui-runtime-architecture` and updated `host-contract` responsibility
  - `.aiknowledge/codemap/source-list-workspace.md`: refreshed `HostSnapshot` bridge wording to WebView HostBridge v1
  - `.aiknowledge/codemap/ui-runtime-architecture.md`: new codemap entry for WebView UI + C++ Core target architecture and follow-up change map
  - `.aiknowledge/logs/2026-06.md`: appended archive follow-up log entry
- post_knowledge_decision: done
- notes:
  - 本次 knowledge 更新不修改产品代码；只更新 codemap 和知识日志。
  - 保留的 `future Flutter consumption language` 命中仅出现在 supersede 说明中，用于提醒后续不再沿用旧 Flutter route。
