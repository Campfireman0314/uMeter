# Evidence: 2026-06-30-web-settings-workflow-parity

正文默认使用中文；路径、命令、配置键、frontmatter key 和状态枚举保留原文。

## Final Gate Summary

- final_readiness:
  - status: pass
  - evidence_ref: Readiness / 2026-06-30T18:04:22+08:00
- final_verify:
  - status: pass
  - evidence_ref: Verify / 2026-06-30T18:21:36+08:00
- final_review:
  - status: pass
  - evidence_ref: Risk Review / 2026-06-30T18:22:20+08:00
- final_archive:
  - status: pass
  - archive_path: openspec/changes/archive/2026-06-30-web-settings-workflow-parity/
  - evidence_ref: Archive / 2026-06-30T18:23:20+08:00
- allowed_failures: []
- manual_followups: []
- knowledge_status:
  - status: done
  - evidence_ref: Archive / 2026-06-30T18:25:49+08:00 knowledge auto follow-up

## Plan Evidence

- `opsx changes list`: 无活动变更。
- 已读取归档规划：
  - `openspec/changes/archive/2026-06-29-cross-platform-architecture-plan/work.md`
  - `openspec/changes/archive/2026-06-29-cross-platform-architecture-plan/design.md`
- 已读取 codemap：
  - `.aiknowledge/codemap/index.md`
  - `.aiknowledge/codemap/ui-runtime-architecture.md`
  - `.aiknowledge/codemap/webview-shell.md`
  - `.aiknowledge/codemap/host-contract.md`
  - `.aiknowledge/codemap/source-list-workspace.md`
- 已读取 completion audit：
  - `openspec/changes/archive/2026-06-30-completion-audit/evidence.md`
- 已读取关键代码：
  - `src/DesktopLoudnessMeter.Web/src/AppShell.tsx`
  - `src/DesktopLoudnessMeter.Web/src/bridge/hostBridge.ts`
  - `src/DesktopLoudnessMeter.Web/src/state/hostState.ts`
  - `src/DesktopLoudnessMeter.Web/src/mockHostBridge.ts`
  - `src/DesktopLoudnessMeter.Web/src/styles/app.css`
  - `src/DesktopLoudnessMeter.App/WebView/HostBridgeController.cs`
  - `src/DesktopLoudnessMeter.App/WebView/HostBridgeEnvelope.cs`
  - `src/DesktopLoudnessMeter.App/WebView/HostBridgeDtoMapper.cs`
  - `src/DesktopLoudnessMeter.App/Host/HostCommand.cs`
  - `src/DesktopLoudnessMeter.App/Host/DesktopMeterHostController.cs`
  - `src/DesktopLoudnessMeter.App/Host/HostSettingsSnapshot.cs`
  - `src/DesktopLoudnessMeter.App/Settings/UserWorkspaceSettings.cs`
  - `src/DesktopLoudnessMeter.App/Themes/AppThemes.cs`
- 已读取测试：
  - `src/DesktopLoudnessMeter.Web/src/AppShell.test.tsx`
  - `tests/DesktopLoudnessMeter.Tests/WebView/HostBridgeControllerTests.cs`
  - `tests/DesktopLoudnessMeter.Tests/Host/DesktopMeterHostControllerTests.cs`
  - `tests/DesktopLoudnessMeter.Tests/Host/HostCommandTests.cs`

## Exploration Baseline

- 已证实事实：
  - completion audit 将 `Web settings/theme workflow incomplete` 标为 high-priority gap，建议 change 为 `web-settings-workflow-parity`。
  - `AppShell.tsx` 当前 Settings button 没有 `onClick`，侧栏 settings 区只读显示 `themeId` 和 `maxRecentEventEntries`。
  - `HostCommand` 已有 `SetThemeHostCommand`、`SetRecentEventLimitHostCommand`、`SetEventOutputFolderHostCommand`。
  - `DesktopMeterHostController.Execute` 已处理 `SetThemeHostCommand`、`SetRecentEventLimitHostCommand`、`SetEventOutputFolderHostCommand`。
  - `HostBridgeController.ToHostCommand` 当前未映射 `setTheme`、`setRecentEventLimit`、`setEventOutputFolder`。
  - `HostBridgeRequestPayload` / TS `HostCommandPayload` 当前没有 `themeId`、`maxRecentEventEntries`、`eventOutputFolder` 字段。
  - `HostSettingsSnapshotDto` 已投影 `schemaVersion`、`themeId`、`maxRecentEventEntries`、`eventOutputFolder`、`showHiddenSources`。
  - Web test harness、React Testing Library、Vitest 和 .NET WebView tests 已存在。
- 推断 / 需要 plan 固化：
  - Web theme 不应直接消费 WPF `AppTheme` 类型；当前更稳妥的 MVP 是 Web 侧固定已知 theme ids，并用 CSS token 应用主题。
  - 平台 folder/file picker 不应进入本 change；`eventOutputFolder` 先使用文本输入/clear 完成可编辑闭环。

## 自我反驳 / 漏洞扫描

- 候选推荐：创建 `2026-06-30-web-settings-workflow-parity`，范围覆盖 Web settings UI + HostBridge settings commands + Web theme tokens。
- 第 1 轮扫描：
  - blocking: theme options 未由 Host snapshot 暴露；如果强行要求 Host 提供 palette，会扩大 scope。
  - non-blocking: Web 侧可固定已知 theme ids，并通过 tests 防止 unknown id 持久化。
- 修订后推荐：本 change 不新增 theme palette DTO，只对齐已知 theme id。
- 第 2 轮扫描:
  - blocking: folder/file picker 可能引入平台 glue。
  - 处理方式: event output folder 只做文本输入；default detail layout file/picker 明确 out-of-scope。
- 最终扫描结论：无新的关键事实性漏洞；进入 opsx-plan 合理。

## Readiness

待 `opsx-readiness` 填写。

### 2026-06-30T18:04:22+08:00 - readiness pass

- status: pass
- 检查命令：
  - `opsx changes list`
  - `opsx changes resolve 2026-06-30-web-settings-workflow-parity`
  - `rg -n "意图|范围|范围内|范围外|验证计划|回退|升级|Workspace Strategy|Workspace Rationale|\[R[1-5]\]|\[U[1-4]\]|Allowed Files|Forbidden Files|Evidence Required|Worker Return Format|需求追踪|执行方式" openspec\changes\2026-06-30-web-settings-workflow-parity`
  - `git status --short --branch`
- 已证实：
  - 当前活动 change 唯一，状态为 `tasks (0/4) profile=lean-change next=opsx-readiness`。
  - `work.md` 覆盖 intent、scope、non-goals、verification plan、rollback/escalation、workspace strategy 和 rationale。
  - `specs/change/spec.md` 的 [R1]-[R5] 与 `design.md` 的 [U1]-[U4] 双向追踪完整。
  - `tasks.md` 有 Default Constraints，4 个 task 均包含需求追踪、执行方式、Allowed Files、验收标准、验证命令、Evidence Required 和 Done When。
  - 未使用 `[stabilization-first]` task；任务可按 test-first/direct 串行执行。
  - `git status --short --branch` 显示当前分支为 `codex/2026-06-30-web-settings-workflow-parity`，除本 change 文档外无业务代码修改。
- readiness 修订：
  - 在 `design.md` 澄清：新增 settings command 均要求 request payload object 存在；payload object 内字段可按命令规则允许为空、清空或必填。
- 相关 pitfall 已纳入实现约束：
  - `.aiknowledge/pitfalls/misc/hostbridge-numeric-settings-need-domain-validation.md`
  - `.aiknowledge/pitfalls/misc/filtered-views-are-not-lifecycle-state.md`
- 结论：readiness gate 通过，可以进入 `opsx-implement`。

## Implementation

待 `opsx-implement` 填写。

### test-cycle | 2026-06-30T18:08:34+08:00 | pass
任务: 1.1 - HostBridge v1 global settings command mapping
执行方式: test-first

#### 红阶段
- 失败测试列表:
  - `HostBridgeControllerTests.GlobalSettingsCommandsRoundTripWorkspaceProjection` / `EventOutputFolderBlankPayloadClearsWorkspaceProjection` / `InvalidGlobalSettingsPayloadReturnsTypedError` - 产品代码尚未映射新增 settings commands。
  - `HostBridgeClient sends typed global settings command payloads` - `.NET test` 构建 Web 项目时触发 TypeScript 编译失败，`HostCommandPayload` 缺少 `themeId`, `maxRecentEventEntries`, `eventOutputFolder`。
- 命令:
  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter "FullyQualifiedName~HostBridgeControllerTests"` exit=1，失败归因正确。
  - `npm test -- --run src/bridge/hostBridge.test.ts` exit=0；运行时 envelope 已可发送，但 typed build 仍缺字段。

#### 绿阶段
- 通过测试列表:
  - `HostBridgeControllerTests` 21 tests passed。
  - `src/bridge/hostBridge.test.ts` 4 tests passed。
- 命令:
  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter "FullyQualifiedName~HostBridgeControllerTests"` exit=0。
  - `npm test -- --run src/bridge/hostBridge.test.ts` exit=0。
- 变更摘要:
  - `src/DesktopLoudnessMeter.App/WebView/HostBridgeEnvelope.cs` - 新增 settings payload 字段。
  - `src/DesktopLoudnessMeter.App/WebView/HostBridgeController.cs` - 映射 `setTheme`, `setRecentEventLimit`, `setEventOutputFolder`，并在 bridge 边界校验 theme id、recent limit 和缺失 payload。
  - `src/DesktopLoudnessMeter.Web/src/bridge/hostBridge.ts` - 新增 TS command name 和 payload 字段。
  - `tests/DesktopLoudnessMeter.Tests/WebView/HostBridgeControllerTests.cs` - 覆盖 round-trip、blank clear、invalid payload 不变更 snapshot/workspace。
  - `src/DesktopLoudnessMeter.Web/src/bridge/hostBridge.test.ts` - 覆盖 typed settings payload envelope。
- 验收覆盖:
  - `setTheme` round-trip: pass。
  - `setRecentEventLimit` round-trip: pass。
  - `setEventOutputFolder` round-trip + blank clear: pass。
  - 缺失或越界 payload 返回 `invalidRequest` 且不修改 snapshot: pass。
  - TS command name/payload 字段: pass。
- 覆盖率: 不可用；当前 repo 未配置该任务级 coverage 命令；补救为后续执行完整 `npm test` 和相关 .NET test filter。

### test-cycle | 2026-06-30T18:12:39+08:00 | pass
任务: 2.1 - Web settings editable surface
执行方式: test-first

#### 红阶段
- 失败测试列表:
  - `AppShell opens settings surface and dispatches editable global settings commands` - Settings button 仅渲染，无 `onClick`，无法找到 `Settings panel` region。
- 命令:
  - `npm test -- --run src/AppShell.test.tsx` exit=1，失败归因正确。

#### 绿阶段
- 通过测试列表:
  - `src/AppShell.test.tsx` 7 tests passed。
- 命令:
  - `npm test -- --run src/AppShell.test.tsx` exit=0。
- 变更摘要:
  - `src/DesktopLoudnessMeter.Web/src/AppShell.tsx` - 新增 Settings button open/close state、editable settings panel、theme/recent/event-folder/show-hidden command dispatch。
  - `src/DesktopLoudnessMeter.Web/src/mockHostBridge.ts` - mock bridge 支持 global settings commands 并返回 post-command snapshot。
  - `src/DesktopLoudnessMeter.Web/src/styles/app.css` - 新增 settings panel/form/control 布局，控件使用流式宽度避免移动端固定过宽。
  - `src/DesktopLoudnessMeter.Web/src/AppShell.test.tsx` - 覆盖 settings surface workflow 和 post-command snapshot UI 更新。
- 验收覆盖:
  - Settings 按钮打开/关闭 surface: pass。
  - surface 显示 schema/theme/recent/event folder/show-hidden: pass。
  - theme/recent/event folder command + post-command UI update: pass。
  - event output folder clear: pass。
  - top-bar 和 settings surface show-hidden 同步: pass。
  - 移动宽度约束: pass，依据 `.settings-field` / input/select `min-width: 0; width: 100%` 与 flex-wrap actions；最终由 `npm run build` 在 final regression 覆盖。
- 覆盖率: 不可用；当前 repo 未配置该任务级 coverage 命令；补救为后续执行完整 `npm test` 和 `npm run build`。

### test-cycle | 2026-06-30T18:15:11+08:00 | pass
任务: 3.1 - Web theme token application
执行方式: test-first

#### 红阶段
- 失败测试列表:
  - `AppShell applies stable web theme tokens from the host snapshot` - `main` 缺少 `data-theme`。
  - `AppShell falls back to the default web theme for null or unknown theme ids` - `main` 缺少 fallback `data-theme`。
- 命令:
  - `npm test -- --run src/AppShell.test.tsx` exit=1，失败归因正确。

#### 绿阶段
- 通过测试列表:
  - `src/AppShell.test.tsx` 9 tests passed。
- 命令:
  - `npm test -- --run src/AppShell.test.tsx` exit=0。
  - `rg -n "#[0-9a-fA-F]{3,8}|rgb\(|hsl\(" src\DesktopLoudnessMeter.Web\src\styles\app.css` exit=0，仅命中 CSS token 定义区。
- 变更摘要:
  - `src/DesktopLoudnessMeter.Web/src/AppShell.tsx` - 新增 theme id allowlist、`resolveThemeId` fallback 和 root `data-theme`。
  - `src/DesktopLoudnessMeter.Web/src/styles/app.css` - 新增 `midnight`, `milk-white`, `forest-green`, `pink-dream` custom properties，并将 shell/surface/control/chart/error 颜色改为 token 消费。
  - `src/DesktopLoudnessMeter.Web/src/AppShell.test.tsx` - 覆盖已知 theme、post-command theme update、null/unknown fallback。
- 验收覆盖:
  - 已知 theme id 映射到稳定 `data-theme`: pass。
  - null/unknown fallback 到默认 `midnight`: pass。
  - CSS custom properties 覆盖主要颜色: pass。
  - 未引入 WPF theme runtime/type dependency: pass，变更仅在 Web TS/CSS 文件内完成。
- 覆盖率: 不可用；当前 repo 未配置该任务级 coverage 命令；补救为 final regression 执行完整 Web test/build。

### test-cycle | 2026-06-30T18:16:06+08:00 | pass
任务: 4.1 - Final regression
执行方式: direct

#### 直接验证
- 跳过测试循环理由: final regression task 仅汇总已实现行为的 change 级回归命令，无新增产品行为。
- 验证命令:
  - `npm test` in `src/DesktopLoudnessMeter.Web` exit=0，6 files / 25 tests passed。
  - `npm run build` in `src/DesktopLoudnessMeter.Web` exit=0，`tsc -b && vite build` passed。
  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter "FullyQualifiedName~WebView|FullyQualifiedName~Host"` exit=0，43 tests passed。
  - `git diff --check` exit=0；仅输出 LF→CRLF working-copy warning，无 whitespace error。
- dirty scope:
  - 产品/测试修改集中在 WebView HostBridge DTO/mapper boundary、Web React shell、Web bridge types/mock、Web CSS、WebView tests。
  - OpenSpec change 目录为 `openspec/changes/2026-06-30-web-settings-workflow-parity/`。
- 验收覆盖:
  - `npm test`: pass。
  - `npm run build`: pass。
  - `.NET WebView/Host filtered tests`: pass。
  - `git diff --check`: pass。
  - evidence 记录命令、退出码、dirty scope: pass。

## Verify

### implement-amendment | 2026-06-30T18:20:41+08:00 | pass
来源: review-finding
观察:
- expected: HostBridge 接受已知 theme id 后，持久化和 snapshot 应使用 Web UI 可识别的 canonical id。
- actual: `Forest-Green` 会被 case-insensitive allowlist 接受，但原样持久化，Web `resolveThemeId` 会 fallback 到 `midnight`。
范围判定:
- trace: [R3], [R4], Task 1.1/3.1
- classification: within-scope
同步:
- code/tests:
  - `src/DesktopLoudnessMeter.App/WebView/HostBridgeController.cs` - 将 supported theme ids 改为 case-insensitive canonical map，创建 command 前规范化到小写 id。
  - `tests/DesktopLoudnessMeter.Tests/WebView/HostBridgeControllerTests.cs` - 新增 `ThemeCommandNormalizesKnownThemeIdCasing` regression。
- docs: `evidence.md`
验证:
- red: `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter "FullyQualifiedName~HostBridgeControllerTests"` exit=1，`ThemeCommandNormalizesKnownThemeIdCasing` 断言 expected `forest-green` actual `Forest-Green`。
- green:
  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter "FullyQualifiedName~HostBridgeControllerTests"` exit=0，22 tests passed。
  - `npm test` exit=0，6 files / 25 tests passed。
  - `npm run build` exit=0。
  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter "FullyQualifiedName~WebView|FullyQualifiedName~Host"` exit=0，44 tests passed。
  - `git diff --check` exit=0；仅 LF→CRLF working-copy warning，无 whitespace error。
待处理:
- none
Gate impact:
- verify: stale
- review: stale
下一步:
- 重新执行 `opsx-verify` 与 `opsx-review`。

### verify | 2026-06-30T18:17:19+08:00 | pass
方向: tasks.md + specs/change/spec.md + design.md + code diff -> 验证通过

证据:
- `opsx changes list` 显示 `2026-06-30-web-settings-workflow-parity [tasks (4/4) profile=lean-change next=opsx-verify]`。
- `.openspec.yaml` readiness gate 为 `status: pass`。
- `rg -n "^- \[ \] [0-9]|- \[ \] `|\\[stabilization-first\\]|\\[manual\\]|implement-amendment" ...` 未发现未完成顶层 task、manual AC、stabilization task 或 amendment 缺口。
- Final regression fresh evidence:
  - `npm test` exit=0，6 files / 25 tests passed。
  - `npm run build` exit=0，`tsc -b && vite build` passed。
  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter "FullyQualifiedName~WebView|FullyQualifiedName~Host"` exit=0，43 tests passed。
  - `git diff --check` exit=0；仅 LF→CRLF working-copy warning，无 whitespace error。
- Spec compliance:
  - [R1] settings surface: `AppShell.tsx` 新增 open/close settings panel，`AppShell.test.tsx` 覆盖打开/关闭和 snapshot settings 展示。
  - [R2] global settings commands: Web settings controls 使用 `store.sendCommand` 发送 `setTheme`, `setRecentEventLimit`, `setEventOutputFolder`, `setShowHiddenSources`，并通过 post-command snapshot 更新 UI。
  - [R3] HostBridge settings DTOs: `HostBridgeEnvelope.cs`, `HostBridgeController.cs`, `hostBridge.ts` 增加 settings payload/command mapping；`.NET` tests 覆盖 valid/invalid payload。
  - [R4] Web theme application: `AppShell.tsx` root `data-theme` + CSS custom properties；tests 覆盖已知 id 和 null/unknown fallback。
  - [R5] regression coverage: Web tests/build 和 .NET WebView/Host filtered tests 均通过。
- Git:
  - branch=`codex/2026-06-30-web-settings-workflow-parity`
  - HEAD=`ff75f0535d9a6d3f1367f1b58055e0b26647c8ec`
  - base=`ff75f0535d9a6d3f1367f1b58055e0b26647c8ec` / merge target=`codex/2026-06-30-completion-audit`（来自 `work.md`）
  - diff_status=unstaged product/test diff + untracked current OpenSpec change directory，全部归属本 change；无 staged diff。
  - verification_coverage=worktree
  - manual_status=none
  - stabilization_status=none
  - amendment_status=none

allowed_failures:
- item: 无
  why_non_blocking: 无
  owner_or_followup: 无
  expiry_or_recheck: 无

问题:
- 无发现。

需修正:
- 无。

### verify | 2026-06-30T18:21:36+08:00 | pass
方向: implement-amendment + tasks.md + specs/change/spec.md + design.md + code diff -> 验证通过

证据:
- `opsx changes list` 显示 `2026-06-30-web-settings-workflow-parity [tasks (4/4) profile=lean-change next=opsx-verify]`。
- `implement-amendment | 2026-06-30T18:20:41+08:00 | pass` 已记录 review finding 的修复和重验证。
- Amendment 后 fresh verification:
  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter "FullyQualifiedName~HostBridgeControllerTests"` exit=0，22 tests passed。
  - `npm test` exit=0，6 files / 25 tests passed。
  - `npm run build` exit=0。
  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter "FullyQualifiedName~WebView|FullyQualifiedName~Host"` exit=0，44 tests passed。
  - `git diff --check` exit=0；仅 LF→CRLF working-copy warning，无 whitespace error。
- 静态状态:
  - no unfinished top-level task。
  - no manual AC。
  - no stabilization task。
  - amendment_status=reverified。
- Git:
  - branch=`codex/2026-06-30-web-settings-workflow-parity`
  - HEAD=`ff75f0535d9a6d3f1367f1b58055e0b26647c8ec`
  - base=`ff75f0535d9a6d3f1367f1b58055e0b26647c8ec` / merge target=`codex/2026-06-30-completion-audit`
  - diff_status=unstaged product/test diff + untracked current OpenSpec change directory，全部归属本 change；无 staged diff。
  - verification_coverage=worktree
  - manual_status=none
  - stabilization_status=none
  - amendment_status=reverified

allowed_failures:
- item: 无
  why_non_blocking: 无
  owner_or_followup: 无
  expiry_or_recheck: 无

问题:
- 无发现。

需修正:
- 无。

## Risk Review

待 `opsx-review` 填写。

### risk-review | 2026-06-30T18:18:37+08:00 | fail
Review Scope Snapshot:
- branch=`codex/2026-06-30-web-settings-workflow-parity`
- HEAD=`ff75f0535d9a6d3f1367f1b58055e0b26647c8ec`
- diff_status=unstaged product/test diff + untracked current OpenSpec change directory，全部归属本 change；无 staged diff。
- verify gate=`pass` at `2026-06-30T18:17:19+08:00`

findings:
- severity: WARNING
  risk_type: schema / serialization
  file: `src/DesktopLoudnessMeter.App/WebView/HostBridgeController.cs`
  line: 128
  issue: `SupportedThemeIds` 使用 case-insensitive allowlist，但 `CreateThemeCommand` 将原始 trimmed `themeId` 传给 `SetThemeHostCommand` 持久化。Host 会接受 `Forest-Green` 这类大小写变体，而 Web `resolveThemeId` 只识别规范小写 id，后续 snapshot 会 fallback 到 `midnight`，导致设置成功但 UI 呈现为默认主题。
  execution_path: WebView message `setTheme` -> `HostBridgeController.CreateThemeCommand` -> workspace `ThemeId` -> Host snapshot -> Web `resolveThemeId`。
  consequence: 用户或第三方 bridge client 发送大小写变体时，settings command 返回成功且持久化，但 Web UI 不应用用户选择的主题，形成跨层 DTO 规范化不一致。
  fix: HostBridge 边界应把已知 theme id 规范化为 Web 支持的 canonical id 后再创建 `SetThemeHostCommand`，并补 regression test。

review decision: fail
allowed_failures:
- item: 无
  why_non_blocking: 无
  owner_or_followup: 无
  expiry_or_recheck: 无

下一步:
- 回到 `opsx-implement` 使用 `implement-amendment` 修复，并重新执行 `opsx-verify` / `opsx-review`。

### risk-review | 2026-06-30T18:22:20+08:00 | pass
Review Scope Snapshot:
- branch=`codex/2026-06-30-web-settings-workflow-parity`
- HEAD=`ff75f0535d9a6d3f1367f1b58055e0b26647c8ec`
- diff_status=unstaged product/test diff + untracked current OpenSpec change directory，全部归属本 change；无 staged diff。
- verify gate=`pass` at `2026-06-30T18:21:36+08:00`

已复查风险线:
- HostBridge settings command DTO/serialization:
  - `setTheme` / `setRecentEventLimit` / `setEventOutputFolder` payload 字段新增在 C# 和 TS 两端一致。
  - `setRecentEventLimit` 在 bridge 边界限制 1..200，命中既有 numeric setting pitfall guardrail。
  - `setTheme` 现在使用 case-insensitive canonical map，大小写变体会规范化为 Web `data-theme` 可识别 id，前一轮 schema/serialization finding 已关闭。
- Web settings workflow:
  - Settings panel 从 post-command snapshot 更新 UI；top-bar 与 settings surface 的 show-hidden 状态共享 snapshot。
  - `setEventOutputFolder` text input 支持 clear；空白在 Host controller 中归一为空。
- Web theme tokens:
  - CSS 硬编码颜色集中在 token 定义区，主要 shell/surface/control/chart/error colors 使用 custom properties。
  - 未引入 WPF theme runtime/type dependency。
- Scope:
  - 未发现 Native/JUCE/Protocol/macOS/WPF 越界实现。

findings:
- 无 CRITICAL。
- 无 WARNING。
- 无 SUGGESTION。

review decision: pass
allowed_failures:
- item: 无
  why_non_blocking: 无
  owner_or_followup: 无
  expiry_or_recheck: 无

下一步:
- 进入 `opsx-archive`。

## Archive

### archive | 2026-06-30T18:23:20+08:00 | pass

- archive_path: `openspec/changes/archive/2026-06-30-web-settings-workflow-parity/`
- spec_count: 1
- gates:
  - readiness: pass
  - verify: pass
  - review: pass
  - archive: pass
- manual_followups: []
- allowed_failures: []

### knowledge-auto | 2026-06-30T18:25:49+08:00 | done

- pre_knowledge_status: clean before knowledge writes.
- post_knowledge_status:
  - `M .aiknowledge/codemap/host-contract.md`
  - `M .aiknowledge/codemap/source-list-workspace.md`
  - `M .aiknowledge/codemap/webview-shell.md`
  - `M .aiknowledge/logs/2026-06.md`
  - `M .aiknowledge/pitfalls/index.md`
  - `M .aiknowledge/pitfalls/misc/index.md`
  - `?? .aiknowledge/pitfalls/misc/hostbridge-enum-like-settings-need-canonical-values.md`
- diff_stat:
  - `.aiknowledge/codemap/host-contract.md` updated.
  - `.aiknowledge/codemap/source-list-workspace.md` updated.
  - `.aiknowledge/codemap/webview-shell.md` updated.
  - `.aiknowledge/logs/2026-06.md` updated.
  - `.aiknowledge/pitfalls/index.md` updated.
  - `.aiknowledge/pitfalls/misc/index.md` updated.
  - new pitfall `.aiknowledge/pitfalls/misc/hostbridge-enum-like-settings-need-canonical-values.md`.
- checks:
  - `opsx changes list` -> no active changes.
  - `rg -n "hostbridge-enum-like-settings-need-canonical-values|HostBridge Enum-Like|2026-06-30-web-settings-workflow-parity" .aiknowledge` confirmed codemap, pitfall index, new pitfall, and monthly log references.

待 `opsx-archive` 填写。
