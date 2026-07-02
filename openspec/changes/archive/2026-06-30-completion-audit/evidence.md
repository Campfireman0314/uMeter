# Evidence: 2026-06-30-completion-audit

正文默认使用中文；路径、命令、配置键名、frontmatter key 和状态枚举保留原文。

## Final Gate Summary

- final_readiness:
  - status: pass
  - evidence_ref: `## Readiness`
- final_verify:
  - status: pass
  - evidence_ref: `## Verify`
- final_review:
  - status: pass
  - evidence_ref: `## Risk Review`
- final_archive:
  - status: pass
  - archive_path: `openspec/changes/archive/2026-06-30-completion-audit`
  - evidence_ref: `## Archive`
- final_verdict:
  - status: not_complete
  - evidence_ref: `### Task 3.1 - Completion verdict and gap map`
- allowed_failures: []
- manual_followups: []
- knowledge_status:
  - status: done
  - evidence_ref: `## Knowledge Follow-up`

## Plan Evidence

- `opsx changes list`：无活动变更。
- 已读取归档规划：
  - `openspec/changes/archive/2026-06-29-cross-platform-architecture-plan/work.md`
  - `openspec/changes/archive/2026-06-29-cross-platform-architecture-plan/design.md`
- 已读取 codemap：
  - `.aiknowledge/codemap/index.md`
  - `.aiknowledge/codemap/ui-runtime-architecture.md`
  - `.aiknowledge/codemap/host-contract.md`
  - `.aiknowledge/codemap/webview-shell.md`
  - `.aiknowledge/codemap/native-dsp.md`
  - `.aiknowledge/codemap/signal-source-ingest.md`
  - `.aiknowledge/codemap/macos-runner-boundary.md`
- 当前已证实事实：
  - `opsx changes list` 在本 change 创建前无活动变更。
  - `macos-readiness` 已归档，且 codemap 明确 macOS 当前是 WKWebView skeleton/readiness boundary，不是 runtime parity。
  - Windows WebView shell、HostBridge、native DSP/source ingest、WPF retirement 均有归档 evidence 和 codemap 入口。
- 推断 / 待证：
  - 总目标是否完成仍未证明，必须由本 change 的 audit matrix 决定。

## Readiness

- 2026-06-30T17:39:08+08:00 readiness: pass
- 审查范围：
  - `.openspec.yaml`
  - `work.md`
  - `specs/change/spec.md`
  - `design.md`
  - `tasks.md`
  - `evidence.md`
- 证据：
  - `rg -n "\[R[1-5]\]|\[U[1-5]\]|需求追踪|Allowed Files|Evidence Required|Stabilization Rationale|Final Hardening|Worker Return Format|Forbidden Files|Shared Artifacts" openspec\changes\2026-06-30-completion-audit`：exit 0，命中 R/U trace、task dispatch 字段和 stabilization 段落。
  - `Get-ChildItem -Recurse openspec\changes\2026-06-30-completion-audit`：确认统一 OpenSpec artifacts 存在。
  - `git status --short --branch`：branch=`codex/2026-06-30-completion-audit`，dirty 仅包含当前 change artifacts。
- 结论：
  - `work.md` 包含 intent、scope、non-goals、success criteria、verification plan、rollback/escalation、workspace rationale。
  - `specs/change/spec.md` 中 R1-R5 全局唯一，覆盖 final-state coverage、evidence grading、current regression、gap map、lifecycle。
  - `design.md` 覆盖 R1-R5，并提供 U1-U5 与模块边界。
  - `tasks.md` 的三个 task 都有 Allowed Files、验收标准、验证方法、Evidence Required 和 stabilization rationale；Default Constraints 提供 Forbidden Files、Shared Artifacts 和 Worker Return Format。
- StageResult: `pass`

## Stabilization Ledger

### Task 1.1 - Final-state requirements matrix

- 2026-06-30T17:45:00+08:00 implementation: pass
- 执行方式：`stabilization-first`
- Stabilization Rationale:
  - 不确定性来源：总目标跨多个归档 change，且部分归档明确是 readiness boundary 或 follow-up，不可用“推荐序列已完成”替代逐条证据。
  - 为什么不能先写目标行为测试：completion 是架构级证据审计，必须综合源码、codemap、归档 evidence 和当前命令输出。
  - 退出条件：所有显式最终态要求均有 evidence grade；弱证据和缺口明确记录。

Evidence Grade Legend:
- `proven`: 当前源码、测试/命令或归档 evidence 直接证明该要求已达成。
- `incomplete`: 当前证据直接显示该要求尚未完全达成。
- `weak_evidence`: 有部分证据，但覆盖面不足以证明最终态。
- `contradicted`: 当前证据与要求相反。
- `not_applicable_by_scope`: 要求原文只要求边界或预留，不要求完整实现。

Requirements Matrix:

| # | 最终态要求 | Evidence grade | 当前证据 | 结论 |
|---|------------|----------------|----------|------|
| 1 | WebView 承载主 UI | proven | `AppWindowRouter.ShowStartupWindow/RestoreFromTray/RestoreFromSecondaryActivation` 均调用 WebView primary；`App.xaml.cs` startup 调用 `_windowRouter.ShowStartupWindow()`；tray normal menu 无 WPF fallback；`webview-packaging-startup-tray` 与 `wpf-retirement` 已归档 | Windows 主 UI 已 WebView-primary |
| 2 | Web 前端承担窗口内容、交互、主题、设置、图表和用户工作流 | incomplete | `AppShell.tsx` 承担 source list、detail、recent、show hidden、refresh、chart mode/visibility；`Settings` 按钮目前无交互 handler，settings 面板只显示 theme/recent limit；完整设置工作流未在 Web UI 中证明 | Web 普通工作流和图表已迁移，但设置/主题完整工作流不足 |
| 3 | Windows 使用 WebView2 | proven | `WebViewShellWindow.xaml` 使用 WebView2 WPF control；`WebViewShellWindow.xaml.cs` 调用 `EnsureCoreWebView2Async`、`WebMessageReceived`、`PostWebMessageAsJson` | Windows WebView2 path 已落地 |
| 4 | macOS 预留 WKWebView runner 边界 | proven for boundary | `src/DesktopLoudnessMeter.MacHost/**` 包含 SwiftPM/AppKit/WKWebView skeleton；`macos-readiness` 归档记录 pass_with_warnings | 满足“预留边界”，不等于 macOS runtime parity |
| 5 | C++ Core 承担 DSP | proven | `DlmPcmAnalyzer.*`、`NativePcmAnalyzer.cs`、`native-core-dsp` 归档；native-dsp codemap 记录 K-weighting、LUFS、true peak、waveform/spectrum 等 native core | DSP 已迁入 native C++ core |
| 6 | C++ Core 承担协议接入 | incomplete | `LocalLoopbackUdpReceiver.RouteReceivedPacket` 仍在 C# 使用 `LocalLoopbackProtocol.TryDecode` 做 UDP/Protocol decode 与 ack；native `DlmSourceIngestCore` 接收 typed fields，不接收 protocol bytes | Protocol ingest 仍是 C#/Host glue，不是 C++ Core |
| 7 | C++ Core 承担 source ingest 状态机 | proven | `DlmSourceIngestCore.cpp` owns register/heartbeat/rename/process/close/timeout/source registry/analyzer ownership；`LocalLoopbackSignalSourceService` delegates to `_core` under lock | 平台无关 source ingest state 已在 native core |
| 8 | C++ Core 承担 snapshot generation | weak_evidence | native `DlmSourceIngestCoreGetSourceSnapshotAt` 生成 flat native source snapshot；但 `SignalSourceSnapshot`、`HostSnapshot`、selected detail、recent events、settings projection 仍由 C# `LocalLoopbackSignalSourceService` 和 `DesktopMeterHostController` 生成 | native 生成底层 ingest/analyzer snapshot，但 UI-facing host snapshot generation 未完全在 C++ Core |
| 9 | C++ Core 承担核心状态机 | weak_evidence | source lifecycle core 在 native；workspace/UI selection/recent events/command state still in `DesktopMeterHostController` | 核心音频/source 状态机部分满足，整体 core state machine 边界未完全证明 |
| 10 | Host 只保留 runner/WebView/HostBridge/platform capture/tray/startup/single-instance/installer glue | incomplete | Host still owns `DesktopMeterHostController` workspace mutation, UI-facing snapshot projection, selected detail, recent events, frequency collision projection, settings persistence commands; WASAPI capture remains platform glue as intended | Host 仍承担较多 UI-neutral state/projection，不只是 glue |
| 11 | HostBridge v1 使用 native WebView message channel + typed command bus + JSON DTO | proven for Windows; incomplete for macOS runtime | Windows WebView2 `WebMessageReceived` -> `HostBridgeController` -> JSON response; TS `hostBridge.ts`; Mac skeleton exposes `window.webkit.messageHandlers.hostBridge` but Web-side adapter deferred | Windows complete；macOS transport runtime adapter 未完成 |
| 12 | Web UI 使用 TypeScript + React + Vite；DOM 普通 UI；Canvas/WebGL 高频图表 | proven | `package.json` React/Vite scripts/deps；`AppShell.tsx` DOM workflow；`ChartModuleCanvas.tsx` Canvas rendering | 已满足 |
| 13 | JUCE sender 保持 DAW 发送端职责；Protocol 保持版本化 lifecycle vocabulary | proven | `LocalLoopbackProtocol.ProtocolVersion` and packet constants; JUCE `LoopbackSender.cpp` / `LoopbackProtocol.cpp`; `juce-sender-protocol-boundary` archived | 已满足 |
| 14 | 先完成 Windows 可运行主路径，同时为 macOS 预留边界 | proven | Windows WebView regression passes in prior changes and will be rerun in Task 2.1; macOS skeleton/readiness exists | 方向满足 |
| 15 | WPF 在 WebView parity 前 fallback/oracle，之后退休或隔离 | proven for user workflow isolation | `wpf-retirement` archived; normal tray menu does not expose fallback; WPF remains compiled/internal oracle seam | 已隔离为 internal oracle seam |

Audit Summary:
- `proven`: 9 项或部分项。
- `incomplete`: Web settings/workflow completeness、C++ Protocol ingest、Host glue-only boundary、macOS runtime HostBridge adapter。
- `weak_evidence`: native/UI-facing snapshot generation 和整体 core state machine。
- `contradicted`: 无；但多项不足以支持 goal completion。

Final Hardening:
- 本 task 不调用 goal completion。
- doc impact 分类：`TASK_EVIDENCE_ONLY`。

### Task 2.1 - Current Windows/WebView regression evidence

- 2026-06-30T17:48:00+08:00 implementation: pass
- 执行方式：`stabilization-first`
- Stabilization Rationale:
  - 不确定性来源：completion audit 是只读工作，但当前主路径仍需要真实命令确认。
  - 退出条件：Web tests/build 与 .NET tests 均通过，且产品源码没有 diff。
- 验证：
  - `npm test` in `src/DesktopLoudnessMeter.Web`：exit 0；6 passed test files，21 passed tests。
  - `npm run build` in `src/DesktopLoudnessMeter.Web`：exit 0；`tsc -b && vite build` passed，1586 modules transformed。
  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj`：exit 0；Failed 0，Passed 327，Skipped 5，Total 332。
  - `git diff --stat -- src tests docs .aiknowledge`：exit 0，无输出。
  - `git status --short --branch`：dirty 仅包含 `openspec/changes/2026-06-30-completion-audit/`。
- Final Hardening:
  - 当前 Windows/WebView 主路径可构建和测试。
  - 审计未修改产品源码、测试、docs 或 knowledge。
  - doc impact 分类：`TASK_EVIDENCE_ONLY`。

### Task 3.1 - Completion verdict and gap map

- 2026-06-30T17:50:00+08:00 implementation: pass
- 执行方式：`stabilization-first`
- Final verdict:
  - verdict: `not_complete`
  - reason: completion audit matrix 中存在 `incomplete` 和 `weak_evidence` 项；因此不能调用 `update_goal complete`。

Remaining gap map:

| Gap | Evidence | Impact | Suggested next change | Priority |
|-----|----------|--------|-----------------------|----------|
| Web settings/theme workflow incomplete | `AppShell.tsx` 有 Settings button 但无 handler；settings panel 仅显示 theme/recent limit；HostBridge 支持部分 settings commands | 用户目标要求 Web 前端承担主题、设置和用户工作流；当前 Web UI 只覆盖部分 | `web-settings-workflow-parity` | high |
| Protocol ingest remains C# host glue | `LocalLoopbackUdpReceiver.RouteReceivedPacket` uses `LocalLoopbackProtocol.TryDecode`; native `DlmSourceIngestCore` receives typed fields only | 用户目标要求 C++ Core 承担协议接入；当前协议 decode/ack/routing 未进入 C++ Core | `native-protocol-ingest-core` | high |
| UI-facing snapshot generation remains partly in C# Host | `DlmSourceIngestCoreGetSourceSnapshotAt` produces flat native source snapshot, but `LocalLoopbackSignalSourceService.CreateSnapshot` and `DesktopMeterHostController.BuildSnapshot/BuildDetailSnapshot` generate UI-facing source/host/detail/settings/recent snapshots | 用户目标要求 C++ Core 承担 snapshot generation；当前仅底层 metrics/source snapshot native-backed | `native-host-snapshot-projection-core` | high |
| Host is not glue-only | `DesktopMeterHostController` owns workspace mutations, selected detail, recent events, frequency collision projection, command state, and settings persistence commands | 用户目标要求 Host 只保留 runner/WebView/bridge/platform glue；当前 Host 仍承担 UI-neutral state/projection | `host-glue-boundary-reduction` | high |
| Core state machine boundary not fully proven | Native source lifecycle is proven, but workspace/detail/recent/command availability state remains C# Host | 总目标中的 “核心状态机” 未能逐条证明全部在 C++ Core | combine with `native-host-snapshot-projection-core` or split `native-core-state-machine-boundary` | medium |
| macOS Web-side WKWebView adapter not implemented | `macos-readiness` allowed failure says current Web transport is `window.chrome.webview`; Mac skeleton names `window.webkit.messageHandlers.hostBridge` | macOS boundary exists, but future runner cannot claim runtime support without Web-side adapter | `macos-runner-implementation` | medium |

Goal completion decision:
- Do not call `update_goal complete`.
- Keep active goal open.
- Recommended next implementation order:
  1. `web-settings-workflow-parity`
  2. `native-protocol-ingest-core`
  3. `native-host-snapshot-projection-core`
  4. `host-glue-boundary-reduction`
  5. `macos-runner-implementation`

Verification:
- `rg -n "verdict:|remaining gap|not_complete|complete|proven|weak_evidence|incomplete|contradicted" openspec/changes/2026-06-30-completion-audit/evidence.md` should find verdict, matrix grades, and remaining gap map.

Final Hardening:
- verdict 与 matrix 一致。
- 缺口均有建议后续 change。
- doc impact 分类：`TASK_EVIDENCE_ONLY`。

## Verify

- 2026-06-30T17:46:26+08:00 verify: pass
- 方向：`tasks.md` + `specs/**/*.md` + `design.md` + `evidence.md` + worktree scope -> 验证通过。
- StageResult:
  - decision: `pass`
  - summary: completion audit change 的任务、spec、design 与 evidence 一致；审计 verdict 为 `not_complete`，且由 matrix 中的 `incomplete` / `weak_evidence` 项支撑。该 verdict 是本 change 的正确输出，不代表 verify failure。
- Evidence:
  - `opsx changes resolve 2026-06-30-completion-audit`: exit 0, resolved to `E:\gitlab\uMeter\openspec\changes\2026-06-30-completion-audit`.
  - Read: `.openspec.yaml`, `work.md`, `specs/change/spec.md`, `design.md`, `tasks.md`, `evidence.md`.
  - `rg -n "^- \[ \]|\[ \] [0-9]|\[ \] .*\[manual\]" openspec\changes\2026-06-30-completion-audit\tasks.md`: exit 1, no incomplete tasks or manual ACs found.
  - `rg -n "verdict:|Remaining gap|remaining gap|not_complete|complete|proven|weak_evidence|incomplete|contradicted|Goal completion decision" openspec\changes\2026-06-30-completion-audit\evidence.md`: exit 0, found matrix grades, `verdict: not_complete`, remaining gap map, and goal completion decision.
  - `git diff --check`: exit 0.
  - Git: branch=`codex/2026-06-30-completion-audit`; HEAD=`d50be931720dfb00da575fe9645cba0e45b1706e`; base=`codex/2026-06-30-macos-readiness`; diff_status=`untracked only: openspec/changes/2026-06-30-completion-audit/**`; verification_coverage=`worktree`; manual_status=`none`; stabilization_status=`closed`; amendment_status=`none`.
- Spec Compliance:
  - R1 satisfied: matrix covers explicit final-state requirements.
  - R2 satisfied: grades distinguish `proven`, `incomplete`, `weak_evidence`, `contradicted`, and do not promote weak evidence to completion.
  - R3 satisfied: Web tests/build and .NET regression command summaries are recorded in Task 2.1 evidence.
  - R4 satisfied: `not_complete` verdict includes remaining gap map and next change suggestions.
  - R5 satisfied: evidence explicitly says not to call `update_goal complete`; audit lifecycle continues to review/archive.
- allowed_failures: []
- 问题:
  - 无发现。
- 需修正:
  - 无。

## Risk Review

- 2026-06-30T17:48:57+08:00 review: pass
- Review Scope Snapshot:
  - branch: `codex/2026-06-30-completion-audit`
  - HEAD: `d50be931720dfb00da575fe9645cba0e45b1706e`
  - base: `codex/2026-06-30-macos-readiness`
  - committed diff: none; `git diff --stat codex/2026-06-30-macos-readiness...HEAD` exit 0 with no output.
  - staged diff: none; `git diff --cached --stat` exit 0 with no output.
  - unstaged tracked diff: none; `git diff --stat` exit 0 with no output.
  - untracked scope: `openspec/changes/2026-06-30-completion-audit/.openspec.yaml`, `work.md`, `design.md`, `tasks.md`, `evidence.md`, `specs/change/spec.md`.
- Scope ownership:
  - 所有待发布变更均归属本 completion audit change。
  - 产品源码、测试源码、docs、`.aiknowledge/**` 当前没有 diff。
- Gate check:
  - `.openspec.yaml` has `gates.verify.status: pass` at `2026-06-30T17:46:26+08:00`.
  - review 未发现 verify drift；`not_complete` 是审计 verdict，且 evidence 明确禁止调用 `update_goal complete`。
- Codemap / pitfalls:
  - Read `.aiknowledge/codemap/index.md` and `.aiknowledge/codemap/ui-runtime-architecture.md`.
  - Read `.aiknowledge/pitfalls/index.md`; no active pitfall applies to this docs-only OpenSpec audit diff.
- Findings:
  - CRITICAL: none.
  - WARNING: none.
  - SUGGESTION: none.
- Exclusions:
  - 未审查产品运行时代码发布风险，因为本 change 未修改 `src/**`, `tests/**`, `docs/**`, `.aiknowledge/**`。
  - 未重复执行完整 Spec Compliance Review；该部分由 `## Verify` 记录覆盖。
- allowed_failures: []

## Archive

- 2026-06-30T17:50:08+08:00 archive: pass
- Archive path: `openspec/changes/archive/2026-06-30-completion-audit`
- Preconditions:
  - `opsx changes list`: `2026-06-30-completion-audit [tasks (3/3) profile=strict-change next=opsx-archive]`.
  - `.openspec.yaml` before archive had `gates.verify.status: pass` and `gates.review.status: pass`.
  - `rg -n "^- \[ \]|\[ \] [0-9]|\[manual\]|pending_blocker|DOC_DRIFT|doc_drift|hardening_gap|platform_blocker|allowed_failures|manual_followups|knowledge_status" openspec\changes\2026-06-30-completion-audit`: exit 0, only final summary / allowed failure metadata hits; no incomplete task/manual/blocker hits.
  - `Test-Path openspec\changes\archive\2026-06-30-completion-audit`: `False` before move.
  - spec file count: 1.
- Actions:
  - Moved `openspec/changes/2026-06-30-completion-audit` to `openspec/changes/archive/2026-06-30-completion-audit`.
  - Updated archived `.openspec.yaml`: `status: archived`, `gates.archive.status: pass`, `gates.archive.at: 2026-06-30T17:50:08+08:00`.
- Final result:
  - archived: yes
  - retained warnings: none
  - allowed_failures: []
  - manual_followups: []
  - knowledge follow-up: completed in `## Knowledge Follow-up`; `Final Gate Summary.knowledge_status` is `done`.

## Knowledge Follow-up

- 2026-06-30T17:50:08+08:00 knowledge: done
- mode: `auto`
- source_refs:
  - `change:2026-06-30-completion-audit`
  - `evidence:openspec/changes/archive/2026-06-30-completion-audit/evidence.md`
- pre_knowledge_status:
  - `git status --short -- .aiknowledge`: no output.
- post_knowledge_status:
  - `M .aiknowledge/codemap/ui-runtime-architecture.md`
  - `M .aiknowledge/logs/2026-06.md`
- diff_stat:
  - `.aiknowledge/codemap/ui-runtime-architecture.md | 4 ++++`
  - `.aiknowledge/logs/2026-06.md | 4 ++++`
  - `2 files changed, 8 insertions(+)`
- updates:
  - `ui-runtime-architecture` now records `2026-06-30-completion-audit` as source evidence and preserves the `not_complete` architecture verdict.
  - `.aiknowledge/logs/2026-06.md` records the archive follow-up and next high-priority gap list.
