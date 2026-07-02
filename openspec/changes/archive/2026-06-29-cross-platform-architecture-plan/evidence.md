# Evidence：2026-06-29-cross-platform-architecture-plan

正文默认使用中文；路径、命令、配置键名、frontmatter key 和状态枚举保持原文。

`evidence.md` 是唯一证据账本。readiness、test-cycle、verify、risk review 和人工验证结论都追加写入这里。

## Final Gate Summary

- final_readiness:
  - status: pass
  - evidence_ref: Readiness / 2026-06-29 15:19 +08:00
- final_verify:
  - status: pass
  - evidence_ref: Verify / 2026-06-29T15:24:00+08:00
- final_review:
  - status: pass
  - evidence_ref: Risk Review / 2026-06-29T15:23:25+08:00
- final_archive:
  - status: pass
  - archive_path: `openspec/changes/archive/2026-06-29-cross-platform-architecture-plan`
  - evidence_ref: Archive / 2026-06-29T15:30:00+08:00
- allowed_failures: []
- manual_followups: []
- knowledge_status:
  - status: skipped_with_reason
  - evidence_ref: Knowledge Follow-up / 2026-06-29T15:30:00+08:00; pre_knowledge_status=no_product_code_change; post_knowledge_status=skipped_with_reason; diff_stat=none

## Exploration / Plan Evidence

### 2026-06-29 plan baseline

- stage: plan
- commands:
  - `opsx changes list`
    - exit_status: 0
    - result: 活动变更包含 `2026-06-23-native-core-dsp [tasks (6/7) profile=strict-change next=opsx-verify]`
  - `git status --short --branch`
    - exit_status: 0
    - result: 当前分支 `opsx/native-core-dsp-migration...origin/opsx/native-core-dsp-migration`；创建本 change 前无 dirty，创建后 dirty 限于 `openspec/changes/2026-06-29-cross-platform-architecture-plan/**`
  - `Get-Content -Raw -Encoding UTF8 .aiknowledge\codemap\index.md`
    - exit_status: 0
    - result: 命中 active codemap：`app-startup`、`source-list-workspace`、`signal-source-ingest`、`analysis-rendering`、`juce-sender`、`audio-analysis`、`native-dsp`、`audio-capture-sources`
  - `Get-Content -Raw -Encoding UTF8 .aiknowledge\pitfalls\index.md`
    - exit_status: 0
    - result: 命中 lifecycle、performance、misc 经验域
  - `rg --files -g "*.csproj" -g "*.vcxproj" -g "CMakeLists.txt" -g "pubspec.yaml" -g "*.sln"`
    - exit_status: 0
    - result: 发现 `.sln`、`.csproj`、`.vcxproj`、JUCE `CMakeLists.txt`；未发现 `pubspec.yaml`
  - `opsx changes resolve 2026-06-29-cross-platform-architecture-plan`
    - exit_status: 0
    - result: 解析到 `/e/gitlab/uMeter/openspec/changes/2026-06-29-cross-platform-architecture-plan`
- facts:
  - 当前仓库已有 WPF/.NET 8 Windows app、C++ native DSP、JUCE VST3 sender、`.NET SenderKit` 和 `DesktopLoudnessMeter.Protocol`。
  - 当前没有已落地的 Flutter 项目资产。
  - `2026-06-23-native-core-dsp` 已覆盖 native PCM/DSP 主路径，应先完成 verify/review/archive。
- inferences:
  - Flutter 需要先有 host snapshot/command contract，否则会把 WPF ViewModel 结构隐性迁入 Flutter。
  - native core 后续优先处理 build boundary 和 source ingest 状态机，而不是重复 analyzer migration。
- non_blocking_uncertainties:
  - Flutter bridge 技术形态需要在 `host-snapshot-command-contract` 和 `flutter-windows-app-shell` 中验证。
  - macOS capture 可行性需要后续 explore/spike，不影响 Windows-first 切分。
  - native cross-platform build 需要后续 `native-core-build-boundary` 单独验证。

## Readiness

### 2026-06-29 15:19 +08:00

- stage: readiness
- decision: pass
- evidence_refs:
  - `work.md`：scope、non-goals、Workspace Rationale、切分结论完整
  - `specs/change/spec.md`：R1-R5 全局唯一，均为规划 change 的可观察行为
  - `design.md`：R1-R5 均映射到 U1-U5，每个 U 均含模块边界
  - `tasks.md`：Default Constraints 存在，Task 1.1-1.3 已完成，Task 2.1 是 readiness 本身
  - `evidence.md`：plan baseline、facts/inferences、manual waiver 已记录
- findings:
  - blocking: 无
  - warnings:
    - 本 change 是规划交付闭环，不进入产品代码 implement；readiness 通过后下一步应做本 change 的 verify/review/archive，后续实施模块需单独 `opsx-plan`。
    - 当前分支仍承载 `2026-06-23-native-core-dsp` 活动 change；本规划已标明其为前置依赖。
- next: 标记 readiness pass；进入本 planning change 的 verify/review/archive 收口，或由用户选择后续模块单独开 plan。

## Test Cycle Evidence

### 2026-06-29 Task 1.1-1.3 direct

- task: 1.1, 1.2, 1.3
- phase: direct
- command: 文档生成与只读证据核对
- exit_status: 0
- result: `work.md`、`specs/change/spec.md`、`design.md`、`tasks.md` 已生成；本 change 不修改产品代码。
- coverage:
  - R1 fact baseline
  - R2 top-level change slicing
  - R3 boundary map
  - R4 facts/inferences separation
  - R5 opsx lifecycle entry

## Stabilization Ledger

无 `[stabilization-first]` task。

## Implement Amendments

<!-- 未触发。 -->

## Verify

### 2026-06-29T15:24:00+08:00

## verify | 2026-06-29T15:24:00+08:00 | pass
方向：work.md + specs/change/spec.md + design.md + tasks.md + evidence.md -> 验证通过
证据：
- `git rev-parse HEAD` -> `2a2b5d185c144534972a81f8f8158f01d207489f`
- `git branch --show-current` -> `opsx/native-core-dsp-migration`
- `git status --short` -> 仅 `?? openspec/changes/2026-06-29-cross-platform-architecture-plan/`，归属本 planning change
- `opsx changes resolve 2026-06-29-cross-platform-architecture-plan` -> `/e/gitlab/uMeter/openspec/changes/2026-06-29-cross-platform-architecture-plan`
- `opsx changes list` -> 本 change 为 `tasks (4/4) profile=strict-change next=opsx-verify`，前置活动 change `2026-06-23-native-core-dsp` 仍为 `tasks (6/7) next=opsx-verify`
- `rg --files -g "pubspec.yaml"` -> `NO_PUBSPEC_FOUND`，支持“当前仓库未发现 Flutter 项目资产”的事实
- `rg -n "Trace\\*\\*: R|\\[R[0-9]\\]|\\[U[0-9]\\]|\\[ \\]|\\[x\\]|\\[~\\]|status: pass|decision: pass|blocking: 无" openspec\\changes\\2026-06-29-cross-platform-architecture-plan` -> R1-R5、U1-U5、tasks completion、readiness pass 和 manual waiver 均可追溯
- Git: branch=`opsx/native-core-dsp-migration`; HEAD=`2a2b5d185c144534972a81f8f8158f01d207489f`; base=`opsx/native-core-dsp-migration` at same HEAD for this planning change; diff_status=untracked `openspec/changes/2026-06-29-cross-platform-architecture-plan/**` only, 属于本 change; verification_coverage=worktree OpenSpec 文档 diff; manual_status=waived，原因：本 change 只改规划文档不改运行行为; stabilization_status=none; amendment_status=none
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

### 2026-06-29T15:23:25+08:00

## risk review | 2026-06-29T15:23:25+08:00 | pass

- stage: review
- decision: pass
- Review Scope Snapshot:
  - branch: `opsx/native-core-dsp-migration`
  - HEAD: `2a2b5d185c144534972a81f8f8158f01d207489f`
  - diff_status: untracked `openspec/changes/2026-06-29-cross-platform-architecture-plan/**`，归属本 planning change
  - reviewed_files:
    - `openspec/changes/2026-06-29-cross-platform-architecture-plan/.openspec.yaml`
    - `openspec/changes/2026-06-29-cross-platform-architecture-plan/work.md`
    - `openspec/changes/2026-06-29-cross-platform-architecture-plan/specs/change/spec.md`
    - `openspec/changes/2026-06-29-cross-platform-architecture-plan/design.md`
    - `openspec/changes/2026-06-29-cross-platform-architecture-plan/tasks.md`
    - `openspec/changes/2026-06-29-cross-platform-architecture-plan/evidence.md`
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

- [~] [manual] 本 planning change 不需要人工 UI/平台 smoke | 原因：范围仅为 OpenSpec 架构规划文档，不修改产品运行行为；后续 Flutter、native build、packaging changes 必须各自定义人工/平台验证。

## Rejected Attempts

### Attempt 1

- hypothesis: 创建一个覆盖 Flutter UI、C++ Core、source ingest、settings、JUCE/protocol、packaging 的单一实施 change。
- change_summary: 未执行，只在 plan 阶段评估。
- verification: 对照 codemap 和现有活动 change，发现它跨多个独立能力簇。
- rejection_reason: 无法一次 readiness、implement、verify、review 收敛，违反用户对 change 粒度的要求。

### Attempt 2

- hypothesis: 直接创建 Flutter app shell change 作为第一步。
- change_summary: 未执行，只在 plan 阶段评估。
- verification: 当前仓库未发现 Flutter 项目资产，且缺少 UI-neutral host contract。
- rejection_reason: 缺少 stable host snapshot/command boundary，容易将 WPF ViewModel 语义硬搬进 Flutter。

## Archive

### 2026-06-29T15:30:00+08:00

- stage: archive
- decision: pass
- source_path: `openspec/changes/2026-06-29-cross-platform-architecture-plan`
- archive_path: `openspec/changes/archive/2026-06-29-cross-platform-architecture-plan`
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
  - 本 change 是跨平台架构规划交付闭环，只移动 OpenSpec 文档，不修改产品代码。
  - 后续实施仍需逐个模块单独创建 OpenSpec change。

## Knowledge Follow-up

### 2026-06-29T15:30:00+08:00

- pre_knowledge_status: no_product_code_change
- post_knowledge_status: skipped_with_reason
- diff_stat: none
- reason: 本 change 只归档 OpenSpec 架构规划文档，没有修改 `src/**`、`tests/**`、`docs/**` 或 `.aiknowledge/**` 中的产品/知识条目；现有 codemap 已在 plan/readiness 中作为事实来源读取，后续真正实施模块时再由各自 archive 触发 knowledge auto 更新。
