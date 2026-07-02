# Evidence: 2026-06-30-macos-readiness

正文默认使用中文；路径、命令、配置键、frontmatter key 和状态枚举保留原文。

## Final Gate Summary

- final_readiness:
  - status: pass
  - evidence_ref: `## Readiness`
- final_verify:
  - status: pass_with_warnings
  - evidence_ref: `## Verify`
- final_review:
  - status: pass_with_warnings
  - evidence_ref: `## Risk Review`
- final_archive:
  - status: pass
  - archive_path: `openspec/changes/archive/2026-06-30-macos-readiness/`
  - evidence_ref: `## Archive`
- allowed_failures:
  - item: macOS native CMake configure/build 真机验证 deferred
    why_non_blocking: 本 change 的范围是 macOS readiness boundary，不宣称 native core macOS build 已通过；当前 Windows session 无 macOS/Darwin toolchain。
    owner_or_followup: `macos-runner-implementation` 或 macOS CI/toolchain change
    expiry_or_recheck: 首个声明 macOS runner/native support 的 change 必须重新验证。
  - item: WKWebView runner 真机 smoke 和 Web-side WKWebView adapter deferred
    why_non_blocking: 本 change 只建立 WKWebView host skeleton 和 transport 边界；当前 Web transport 仍为 Windows `window.chrome.webview`，已记录为 follow-up。
    owner_or_followup: `macos-runner-implementation`
    expiry_or_recheck: 接入 macOS runner runtime 前必须补齐并 smoke。
  - item: macOS capture permission/capability smoke deferred
    why_non_blocking: capture adapter 是平台 glue，不属于本轮 HostBridge/core readiness boundary 的可验证 Windows 工作。
    owner_or_followup: `macos-capture-adapter`
    expiry_or_recheck: 实现 macOS capture adapter 前必须验证 API/权限路径。
  - item: macOS packaging/signing/notarization smoke deferred
    why_non_blocking: packaging/signing/notarization 需要 macOS signing 环境和 distribution 决策，不阻塞本轮 readiness artifact。
    owner_or_followup: `macos-packaging`
    expiry_or_recheck: 首个 macOS 分发产物 change 必须重新验证。
- manual_followups:
  - macOS CMake configure/build 真机验证 -> `macos-runner-implementation` 或 macOS CI/toolchain change
  - WKWebView runner 真机 smoke -> `macos-runner-implementation`
  - macOS Web-side WKWebView adapter -> `macos-runner-implementation`
  - macOS capture permission/capability smoke -> `macos-capture-adapter`
  - macOS packaging/signing/notarization smoke -> `macos-packaging`
- knowledge_status:
  - status: done
  - evidence_ref: `## Knowledge Auto Follow-up`

## Plan Evidence

- `opsx changes list`：无活动变更。
- 已读取归档规划：
  - `openspec/changes/archive/2026-06-29-cross-platform-architecture-plan/work.md`
  - `openspec/changes/archive/2026-06-29-cross-platform-architecture-plan/design.md`
- 已读取 codemap：
  - `.aiknowledge/codemap/index.md`
  - `.aiknowledge/codemap/ui-runtime-architecture.md`
  - `.aiknowledge/codemap/webview-shell.md`
  - `.aiknowledge/codemap/native-dsp.md`
  - `.aiknowledge/codemap/audio-capture-sources.md`
- 已读取 pitfalls：
  - `.aiknowledge/pitfalls/index.md`
  - `.aiknowledge/pitfalls/misc/index.md`
  - `.aiknowledge/pitfalls/misc/native-consumers-must-import-copy-targets.md`
  - `.aiknowledge/pitfalls/misc/hostbridge-numeric-settings-need-domain-validation.md`
- Git baseline:
  - branch before change: `codex/2026-06-30-wpf-retirement`
  - base HEAD: `6254167abc773e8fc67130b732ca0a4075b3f25a`
  - status before branch: clean
  - new branch: `codex/2026-06-30-macos-readiness`
- Test framework detection:
  - solution: `desktop-loudness-meter.sln`
  - .NET tests: `tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj`
  - Web project: `src/DesktopLoudnessMeter.Web/package.json`

## Exploration Evidence

- 已证实事实：
  - 仓库没有现成 `WKWebView` / macOS runner / `.xcodeproj` / `Info.plist` / entitlements。
  - `src/DesktopLoudnessMeter.App/DesktopLoudnessMeter.App.csproj` 是 `net8.0-windows`，启用 WPF/WinForms，引用 WebView2/NAudio。
  - `src/DesktopLoudnessMeter.Native/CMakePresets.json` 含 `macos-release` preset。
  - 当前 Windows session 中 `cmake --version` 失败，`C:\Program Files\CMake\bin\cmake.exe` 不存在，`where.exe cmake` 无结果。
- 推断：
  - macOS 需要独立 WKWebView runner boundary，不能复用 Windows WPF/WebView2 app。
  - 当前 change 可交付 skeleton/readiness matrix，但不能宣称 macOS build/run/capture/package pass。

## Readiness

- 2026-06-30T17:14:28+08:00 readiness: pass
- 审查范围：
  - `.openspec.yaml`
  - `work.md`
  - `specs/change/spec.md`
  - `design.md`
  - `tasks.md`
  - `evidence.md`
- 结论：
  - `work.md` 包含 intent、scope、non-goals、dependencies、verification plan、rollback/escalation、workspace rationale。
  - `specs/change/spec.md` 中 R1-R5 全局唯一，每条需求表达单一可验证行为。
  - `design.md` 覆盖 R1-R5，并提供 U1-U5、模块边界、风险、回退和稳定化策略。
  - `tasks.md` 覆盖 R/U trace；每个 task 都有 Allowed Files、验收标准、验证命令/方法、Evidence Required、Done When；Default Constraints 提供 Forbidden Files/Shared Artifacts/Worker Return Format。
  - `[stabilization-first]` task 2.1 和 3.1 均包含 Stabilization Rationale、Manual / Platform Matrix 和 Final Hardening Plan。
  - manual macOS 平台项均为 `[→]`，且包含 `目标：` follow-up；没有裸 `[ ]` manual gate。
  - workspace strategy 已解析为 dedicated branch 上的 `current-worktree`，dirty 仅包含当前 change artifacts。
- 风险记录：
  - 当前 Windows session 无法验证 macOS WKWebView/native/capture/package 真机行为；实现阶段必须保持为 `[→]` follow-up 或补充真实 macOS evidence。
  - skeleton 不能改动 HostBridge DTO、Web runtime、native ABI 或 Windows startup；若需要改动，必须回到 plan 拆分。
- StageResult: `pass`

## Test Cycle Evidence

### Task 1.1 - WKWebView runner skeleton

- 2026-06-30T17:18:00+08:00 implementation: pass
- 执行方式：`direct`
- 变更摘要：
  - 新增 `src/DesktopLoudnessMeter.MacHost/README.md`，说明该目录是 macOS readiness boundary，不是已支持 app。
  - 新增 Swift skeleton：`AppDelegate.swift`、`MacHostWindowController.swift`、`HostBridgeHandling.swift`、`MacWebAssetLocator.swift`。
  - 新增 `docs/platform/macos-readiness.md` 初版，记录 WKWebView boundary 和 macOS readiness matrix。
- 验证：
  - `rg -n "WKWebView|WKScriptMessageHandler|hostBridge|postMessage|evaluateJavaScript" src\DesktopLoudnessMeter.MacHost docs\platform\macos-readiness.md`：exit 0，命中 WKWebView、`WKScriptMessageHandler`、`hostBridge`、`postMessage`、`evaluateJavaScript`。
  - `git diff -- desktop-loudness-meter.sln src\DesktopLoudnessMeter.App src\DesktopLoudnessMeter.Web`：exit 0，无输出；说明 skeleton 未修改 Windows solution/app/Web runtime。
  - `Get-ChildItem -Recurse -Path src\DesktopLoudnessMeter.MacHost, docs\platform`：列出新增 MacHost skeleton 和 readiness doc。
- 跳过测试循环理由：该 task 是 skeleton/docs 级边界建立，无产品运行逻辑接入；行为性验证由后续 Task 2/3 的 stabilization 和 regression 命令覆盖。

## Stabilization Ledger

### Task 2.1 - macOS native/capture/package readiness

- 2026-06-30T17:20:00+08:00 implementation: pass
- 执行方式：`stabilization-first`
- Stabilization Rationale:
  - 不确定性来源：真实 macOS build/run/capture/package 需要 macOS toolchain、权限和签名环境；当前 Windows session 无法执行。
  - 为什么不能先写目标行为测试：WKWebView、CMake macOS preset、capture permission 和 notarization 都依赖 Apple 平台环境；Windows 静态测试无法证明运行通过。
  - 退出条件：所有 macOS manual/platform 项转为 `[→]` 且包含目标；当前可验证的静态边界和 Windows 回归通过。
- Adjustment Ledger:
  - Attempt 1：在 `docs/platform/macos-readiness.md` 固化 fact matrix、WKWebView boundary、SwiftPM probe、native CMake gate、capture/permission gate 和 packaging gate。
  - Amendment：补充 `src/DesktopLoudnessMeter.MacHost/Package.swift` 和 `swift build` 后续 probe，使 runner skeleton 在 macOS 环境有直接构建入口；仍不接入 Windows solution。
- Manual / Platform Matrix:
  | 覆盖项 | 方法 | 状态 | 阻断? | 证据 |
  |-------|------|------|-------|------|
  | macOS native CMake build | 后续 macOS 执行 configure/build preset | deferred | no | tasks/evidence `[→]` 指向后续 macOS CI/toolchain 或 runner implementation |
  | WKWebView runner smoke | 后续 macOS 执行 `swift build` 和 HostBridge smoke | deferred | no | readiness doc 写入 `swift build` probe |
  | macOS capture capability | 后续 macOS 验证 capture/permission 方案 | deferred | no | readiness doc 指向 `macos-capture-adapter` |
  | macOS packaging/signing | 后续 macOS 验证 `.app` payload/sign/notarize | deferred | no | readiness doc 指向 `macos-packaging` |
- 验证：
  - `cmake --version`：exit 1，当前 Windows session 无 CMake。
  - `Test-Path 'C:\Program Files\CMake\bin\cmake.exe'; where.exe cmake`：`False`，且 `where.exe` 未找到 CMake。
  - `rg -n "fact|unknown|follow-up|cmake --preset macos-release|ScreenCaptureKit|CoreAudio|notarization|dylib|entitlements|swift build" docs\platform\macos-readiness.md`：exit 0，命中 readiness matrix 和后续 gate。
- Final Hardening:
  - 本轮不宣称 macOS pass；所有 macOS 真机/toolchain 项保持 `[→]` follow-up。
  - doc impact 分类：`TASK_EVIDENCE_ONLY`。

### Task 3.1 - Windows/WebView regression guard

- 2026-06-30T17:24:00+08:00 implementation: pass
- 执行方式：`stabilization-first`
- Stabilization Rationale:
  - 不确定性来源：新增跨平台 skeleton 不能被 Windows build/test/publish 主路径误消费。
  - 为什么不能先写目标行为测试：本 task 是跨环境守护；最终证据来自真实 Web/.NET build/test 命令和 diff scope。
  - 退出条件：Web/.NET regression 通过，且 diff scope 证明 Windows production files 未被误改。
- Adjustment Ledger:
  - Attempt 1：运行 Web unit tests、Web production build、.NET regression tests。
  - Amendment：发现当前 Web frontend transport 仍是 Windows `window.chrome.webview`，而 Mac skeleton 暴露 `window.webkit.messageHandlers.hostBridge`；已在 `docs/platform/macos-readiness.md` 记录为后续 `macos-runner-implementation` 的 Web-side adapter follow-up，不在本轮改 Web runtime。
- Manual / Platform Matrix:
  | 覆盖项 | 方法 | 状态 | 阻断? | 证据 |
  |-------|------|------|-------|------|
  | Windows Web regression | `npm test` + `npm run build` | pass | yes | 6 test files / 21 tests passed；Vite production build passed |
  | Windows .NET regression | `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj` | pass | yes | Passed: 327, Skipped: 5, Failed: 0 |
  | Windows production diff scope | `git diff --stat -- src/DesktopLoudnessMeter.App src/DesktopLoudnessMeter.Web src/DesktopLoudnessMeter.Native` | pass | yes | no output |
  | macOS runner runtime | 后续 macOS smoke | deferred | no | tasks/evidence `[→]` follow-up |
- 验证：
  - `npm test` in `src/DesktopLoudnessMeter.Web`：exit 0；6 passed test files，21 passed tests。
  - `npm run build` in `src/DesktopLoudnessMeter.Web`：exit 0；`tsc -b && vite build` passed，1586 modules transformed。
  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj`：exit 0；Failed 0，Passed 327，Skipped 5，Total 332。
  - `git diff --stat -- src\DesktopLoudnessMeter.App src\DesktopLoudnessMeter.Web src\DesktopLoudnessMeter.Native`：exit 0，无输出。
  - `git diff -- src\DesktopLoudnessMeter.Web\src\bridge\webViewTransport.ts`：exit 0，无输出；说明本轮未改变 Windows WebView2 transport。
  - `rg -n "chrome\.webview|window\.webkit|Web transport adapter|WKWebView host boundary" docs\platform\macos-readiness.md src\DesktopLoudnessMeter.Web\src\bridge\webViewTransport.ts`：exit 0；命中 Web transport follow-up 和当前 Windows transport 事实。
- Final Hardening:
  - Windows/WebView 主路径通过当前可运行回归。
  - macOS runtime 未验证事项保持 follow-up，不作为本轮 pass 声明。
  - doc impact 分类：`TASK_EVIDENCE_ONLY`。

## Verify

## implement-amendment | 2026-06-30T17:28:00+08:00 | within-scope

observation:
- opsx-review 风险审查中发现 macOS skeleton 仍有两个边界级运行风险：纯代码创建的 `NSWindowController` 不应依赖 `windowDidLoad` 才配置 WebView；placeholder bridge 固定返回 `id="unknown"`，会让后续 HostBridge request/response smoke 难以完成 pending 匹配。

classification: `within-scope`

changes:
- `src/DesktopLoudnessMeter.MacHost/Sources/UMeterMacHost/MacHostWindowController.swift`：将 `configureWebView()` 移到 initializer 中，避免依赖 nib/windowDidLoad 生命周期。
- `src/DesktopLoudnessMeter.MacHost/Sources/UMeterMacHost/HostBridgeHandling.swift`：placeholder bridge 从 request JSON 中提取 `id` 并回显到 error response。

docs_synced:
- 不需要修改 specs/design；仍属于 R1 的 WKWebView/HostBridge boundary skeleton 质量修正。

validation:
- 重新验证已执行：
  - `rg -n "configureWebView\(\)|windowDidLoad|extractRequestId|id" src\DesktopLoudnessMeter.MacHost\Sources\UMeterMacHost`：exit 0，命中 initializer 中的 `configureWebView()` 和 `extractRequestId`，未命中旧 `windowDidLoad` 实现。
  - `git diff --check`：exit 0。
  - `npm test` in `src/DesktopLoudnessMeter.Web`：exit 0；6 passed test files，21 passed tests。
  - `npm run build` in `src/DesktopLoudnessMeter.Web`：exit 0；1586 modules transformed。
  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj`：exit 0；Failed 0，Passed 327，Skipped 5，Total 332。
  - `git diff --stat -- src\DesktopLoudnessMeter.App src\DesktopLoudnessMeter.Web src\DesktopLoudnessMeter.Native`：exit 0，无输出。

pending:
- 无。

gate_impact:
- `gates.verify` 已重新执行；`gates.review` 需要重新执行。

## verify | 2026-06-30T17:28:01+08:00 | pass_with_warnings

方向：implement-amendment 后重新验证 `tasks.md` + `specs/**/*.md` + `design.md` + runner/docs artifacts -> 验证通过，带平台 follow-up warning。

证据：
- amendment validation 已执行并写入本节上方：Swift skeleton 静态检查、`git diff --check`、Web tests/build、.NET regression 和 Windows production diff scope 均通过。
- `git status --short`：仅包含 `docs/platform/`、`openspec/changes/2026-06-30-macos-readiness/`、`src/DesktopLoudnessMeter.MacHost/` 这三个当前 change 拥有的 untracked scope。
- `git diff --stat -- src\DesktopLoudnessMeter.App src\DesktopLoudnessMeter.Web src\DesktopLoudnessMeter.Native`：exit 0，无输出；amendment 未改变 Windows app/Web/native production files。
- Git: branch=`codex/2026-06-30-macos-readiness`; HEAD=`6254167abc773e8fc67130b732ca0a4075b3f25a`; base=`6254167abc773e8fc67130b732ca0a4075b3f25a`; diff_status=untracked current-change files only; verification_coverage=worktree; manual_status=deferred; stabilization_status=closed_with_non_blocking_platform_followups; amendment_status=reverified.

allowed_failures:
- item: macOS native CMake configure/build 真机验证 deferred
  why_non_blocking: 本 change 的范围是 macOS readiness boundary，不宣称 native core macOS build 已通过；当前 Windows session 无 macOS/Darwin toolchain。
  owner_or_followup: `macos-runner-implementation` 或 macOS CI/toolchain change
  expiry_or_recheck: 首个声明 macOS runner/native support 的 change 必须重新验证。
- item: WKWebView runner 真机 smoke 和 Web-side WKWebView adapter deferred
  why_non_blocking: 本 change 只建立 WKWebView host skeleton 和 transport 边界；当前 Web transport 仍为 Windows `window.chrome.webview`，已记录为 follow-up。
  owner_or_followup: `macos-runner-implementation`
  expiry_or_recheck: 接入 macOS runner runtime 前必须补齐并 smoke。
- item: macOS capture permission/capability smoke deferred
  why_non_blocking: capture adapter 是平台 glue，不属于本轮 HostBridge/core readiness boundary 的可验证 Windows 工作。
  owner_or_followup: `macos-capture-adapter`
  expiry_or_recheck: 实现 macOS capture adapter 前必须验证 API/权限路径。
- item: macOS packaging/signing/notarization smoke deferred
  why_non_blocking: packaging/signing/notarization 需要 macOS signing 环境和 distribution 决策，不阻塞本轮 readiness artifact。
  owner_or_followup: `macos-packaging`
  expiry_or_recheck: 首个 macOS 分发产物 change 必须重新验证。

问题：
- 无 critical finding。
- warning：macOS 真机/toolchain/capture/package 验证均为 deferred，已结构化记录为 allowed failures 和 manual followups。

需修正：
- 无。

## verify | 2026-06-30T17:23:37+08:00 | pass_with_warnings

方向：`tasks.md` + `specs/**/*.md` + `design.md` + runner/docs artifacts -> 验证通过，带平台 follow-up warning。

证据：
- `opsx changes list`：`2026-06-30-macos-readiness [tasks (3/3) profile=strict-change next=opsx-verify]`。
- 已读取 `.openspec.yaml`、`work.md`、`tasks.md`、`specs/change/spec.md`、`design.md`、`evidence.md`、`docs/platform/macos-readiness.md`、`src/DesktopLoudnessMeter.MacHost/**`。
- `rg -n "^- \[ \]|\[ \] [0-9]|\[ \] .*\[manual\]" openspec\changes\2026-06-30-macos-readiness\tasks.md openspec\changes\2026-06-30-macos-readiness\evidence.md`：exit 1，无未完成 task 或裸 manual gate 命中。
- `rg -n "\[R[1-5]\]|\[U[1-5]\]|WKWebView|cmake --preset macos-release|window\.chrome\.webview|window\.webkit|TASK_EVIDENCE_ONLY" openspec\changes\2026-06-30-macos-readiness docs\platform\macos-readiness.md src\DesktopLoudnessMeter.MacHost`：exit 0，命中 R/U trace、WKWebView boundary、native CMake gate、Windows/macOS transport 差异和 stabilization doc impact。
- `git diff --check`：exit 0。
- `git status --short`：仅包含 `docs/platform/`、`openspec/changes/2026-06-30-macos-readiness/`、`src/DesktopLoudnessMeter.MacHost/` 这三个当前 change 拥有的 untracked scope。
- Task 3.1 fresh regression evidence：`npm test` exit 0（6 test files / 21 tests passed）；`npm run build` exit 0；`dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj` exit 0（Failed 0, Passed 327, Skipped 5, Total 332）。
- Git: branch=`codex/2026-06-30-macos-readiness`; HEAD=`6254167abc773e8fc67130b732ca0a4075b3f25a`; base=`6254167abc773e8fc67130b732ca0a4075b3f25a`; diff_status=untracked current-change files only; verification_coverage=worktree; manual_status=deferred; stabilization_status=closed_with_non_blocking_platform_followups; amendment_status=none.

allowed_failures:
- item: macOS native CMake configure/build 真机验证 deferred
  why_non_blocking: 本 change 的范围是 macOS readiness boundary，不宣称 native core macOS build 已通过；当前 Windows session 无 macOS/Darwin toolchain。
  owner_or_followup: `macos-runner-implementation` 或 macOS CI/toolchain change
  expiry_or_recheck: 首个声明 macOS runner/native support 的 change 必须重新验证。
- item: WKWebView runner 真机 smoke 和 Web-side WKWebView adapter deferred
  why_non_blocking: 本 change 只建立 WKWebView host skeleton 和 transport 边界；当前 Web transport 仍为 Windows `window.chrome.webview`，已记录为 follow-up。
  owner_or_followup: `macos-runner-implementation`
  expiry_or_recheck: 接入 macOS runner runtime 前必须补齐并 smoke。
- item: macOS capture permission/capability smoke deferred
  why_non_blocking: capture adapter 是平台 glue，不属于本轮 HostBridge/core readiness boundary 的可验证 Windows 工作。
  owner_or_followup: `macos-capture-adapter`
  expiry_or_recheck: 实现 macOS capture adapter 前必须验证 API/权限路径。
- item: macOS packaging/signing/notarization smoke deferred
  why_non_blocking: packaging/signing/notarization 需要 macOS signing 环境和 distribution 决策，不阻塞本轮 readiness artifact。
  owner_or_followup: `macos-packaging`
  expiry_or_recheck: 首个 macOS 分发产物 change 必须重新验证。

问题：
- 无 critical finding。
- warning：macOS 真机/toolchain/capture/package 验证均为 deferred，已结构化记录为 allowed failures 和 manual followups。

需修正：
- 无。

## Risk Review

## risk-review | 2026-06-30T17:28:46+08:00 | pass_with_warnings

Review Scope Snapshot:
- branch: `codex/2026-06-30-macos-readiness`
- HEAD: `6254167abc773e8fc67130b732ca0a4075b3f25a`
- base: `6254167abc773e8fc67130b732ca0a4075b3f25a`
- diff_status: untracked current-change files only
- scope:
  - `docs/platform/macos-readiness.md`
  - `openspec/changes/2026-06-30-macos-readiness/**`
  - `src/DesktopLoudnessMeter.MacHost/**`
- excluded:
  - Windows app/Web/native production files；`git diff --stat -- src\DesktopLoudnessMeter.App src\DesktopLoudnessMeter.Web src\DesktopLoudnessMeter.Native` 无输出。

审查证据：
- `.openspec.yaml`：`gates.verify.status: pass_with_warnings`，时间为 `2026-06-30T17:28:01+08:00`，允许进入 release risk review。
- `git ls-files --others --exclude-standard`：仅列出当前 change 的 docs/OpenSpec/MacHost 新增文件。
- 已复核 `MacHostWindowController.swift`、`HostBridgeHandling.swift`、`MacWebAssetLocator.swift`、`AppDelegate.swift`、`Package.swift`。
- `rg -n "DesktopLoudnessMeter.MacHost|MacHost|Package.swift|WKWebView" desktop-loudness-meter.sln src\DesktopLoudnessMeter.App src\DesktopLoudnessMeter.Web src\DesktopLoudnessMeter.Native`：exit 1；MacHost skeleton 未接入 Windows solution/app/Web/native 主路径。
- `rg -n "TODO|FIXME|fatalError|windowDidLoad|unknown|needs_rerun|VERIFY_DRIFT|SCOPE_CHANGE|DESIGN_AMENDMENT|SPEC_AMENDMENT" ...`：exit 0；命中均为 readiness 状态、历史 amendment 记录、纯代码 runner 的 `init(coder:)` guard 或 fallback id，不构成 release blocker。

findings:
- CRITICAL: 无发现。
- WARNING: 无新增 release-risk warning；verify 阶段的 macOS 真机/toolchain/capture/package deferred 项继续作为 allowed failures 跟踪。
- SUGGESTION: 无。

已处理的 review 发现：
- `MacHostWindowController` 原先依赖 `windowDidLoad` 配置 WebView；已在 implement-amendment 中改为 initializer 内配置，并重新 verify。
- `PlaceholderHostBridge` 原先固定返回 `id="unknown"`；已改为从 request JSON 提取 id 并回显，并重新 verify。

allowed_failures:
- item: macOS native CMake configure/build 真机验证 deferred
  why_non_blocking: 本 change 的范围是 macOS readiness boundary，不宣称 native core macOS build 已通过；当前 Windows session 无 macOS/Darwin toolchain。
  owner_or_followup: `macos-runner-implementation` 或 macOS CI/toolchain change
  expiry_or_recheck: 首个声明 macOS runner/native support 的 change 必须重新验证。
- item: WKWebView runner 真机 smoke 和 Web-side WKWebView adapter deferred
  why_non_blocking: 本 change 只建立 WKWebView host skeleton 和 transport 边界；当前 Web transport 仍为 Windows `window.chrome.webview`，已记录为 follow-up。
  owner_or_followup: `macos-runner-implementation`
  expiry_or_recheck: 接入 macOS runner runtime 前必须补齐并 smoke。
- item: macOS capture permission/capability smoke deferred
  why_non_blocking: capture adapter 是平台 glue，不属于本轮 HostBridge/core readiness boundary 的可验证 Windows 工作。
  owner_or_followup: `macos-capture-adapter`
  expiry_or_recheck: 实现 macOS capture adapter 前必须验证 API/权限路径。
- item: macOS packaging/signing/notarization smoke deferred
  why_non_blocking: packaging/signing/notarization 需要 macOS signing 环境和 distribution 决策，不阻塞本轮 readiness artifact。
  owner_or_followup: `macos-packaging`
  expiry_or_recheck: 首个 macOS 分发产物 change 必须重新验证。

结论：
- 当前待发布 diff 无 CRITICAL release risk。
- 可进入 `opsx-archive`；归档后需要更新 `.aiknowledge`，记录 macOS runner boundary 和后续 follow-up。

## Manual Checks

- [→] [manual] macOS CMake configure/build 真机验证 | 目标：后续 `macos-runner-implementation` 或 macOS CI/toolchain change
- [→] [manual] WKWebView runner 真机 smoke | 目标：后续 `macos-runner-implementation`
- [→] [manual] macOS capture permission/capability smoke | 目标：后续 `macos-capture-adapter`
- [→] [manual] macOS packaging/signing/notarization smoke | 目标：后续 `macos-packaging`

## Archive

- 2026-06-30T17:30:00+08:00 archive: pass
- archive_path: `openspec/changes/archive/2026-06-30-macos-readiness/`
- spec_count: 1
- gate snapshot:
  - readiness: pass
  - verify: pass_with_warnings
  - review: pass_with_warnings
  - archive: pass
- manual followups retained:
  - macOS CMake configure/build 真机验证 -> `macos-runner-implementation` 或 macOS CI/toolchain change
  - WKWebView runner 真机 smoke -> `macos-runner-implementation`
  - macOS Web-side WKWebView adapter -> `macos-runner-implementation`
  - macOS capture permission/capability smoke -> `macos-capture-adapter`
  - macOS packaging/signing/notarization smoke -> `macos-packaging`
- non-blocking warnings:
  - macOS native CMake configure/build、WKWebView runner smoke/Web-side adapter、capture permission/capability、packaging/signing/notarization 均为 deferred follow-up；本 change 不宣称 macOS runtime support。
- next required step:
  - `opsx-knowledge mode=auto` 已完成并回写 `Final Gate Summary.knowledge_status`。

## Knowledge Auto Follow-up

- 2026-06-30T17:32:00+08:00 knowledge: done
- pre_knowledge_status: empty
- post_knowledge_status:
  - `M .aiknowledge/codemap/index.md`
  - `M .aiknowledge/codemap/ui-runtime-architecture.md`
  - `M .aiknowledge/codemap/webview-shell.md`
  - `M .aiknowledge/logs/2026-06.md`
  - `?? .aiknowledge/codemap/macos-runner-boundary.md`
- diff_stat:
  - `.aiknowledge/codemap/index.md`: 1 insertion
  - `.aiknowledge/codemap/ui-runtime-architecture.md`: 7 insertions / 1 deletion
  - `.aiknowledge/codemap/webview-shell.md`: 6 insertions
  - `.aiknowledge/logs/2026-06.md`: 4 insertions
  - staged diff: empty
- summary:
  - 新增 `macos-runner-boundary` codemap。
  - 刷新 `ui-runtime-architecture` 和 `webview-shell`，记录 macOS skeleton 只是 readiness boundary，Web-side WKWebView adapter 仍由 `macos-runner-implementation` 跟进。
  - 追加 2026-06 月度日志。

## Rejected Attempts

- 不采用“直接声明 macOS supported”：没有 macOS runner 或真机/toolchain evidence。
- 不采用“一次实现完整 macOS app/capture/package”：当前 Windows session 无法验证，且范围会跨多个独立高风险能力簇。
- 不采用“只写 docs 不放 runner skeleton”：无法为后续 WKWebView runner 提供可落地入口。
