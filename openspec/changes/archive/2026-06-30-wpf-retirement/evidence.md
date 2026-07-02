# Evidence: 2026-06-30-wpf-retirement

正文默认使用中文；路径、命令、配置键、frontmatter key 和状态枚举保持原文。

## Plan Evidence

- `opsx changes list`：无活动变更。
- 已读取归档规划：`openspec/changes/archive/2026-06-29-cross-platform-architecture-plan/work.md` 和 `design.md`。
- 已读取 codemap：
  - `.aiknowledge/codemap/index.md`
  - `.aiknowledge/codemap/webview-shell.md`
  - `.aiknowledge/codemap/app-startup.md`
  - `.aiknowledge/codemap/source-list-workspace.md`
- 已读取 pitfalls：
  - `.aiknowledge/pitfalls/index.md`
  - `.aiknowledge/pitfalls/misc/index.md`
- Git baseline：
  - branch before change: `codex/2026-06-30-webview-chart-parity`
  - base HEAD: `9deb60b443997389e4a040683585016f76565a16`
  - status before change init: clean
  - new branch: `codex/2026-06-30-wpf-retirement`
- Test framework detection：
  - repo root has `src/` and `tests/`
  - `.NET` test project: `tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj`
  - Web test/build project: `src/DesktopLoudnessMeter.Web/package.json`

## Exploration Evidence

- 已证实：
  - `src/DesktopLoudnessMeter.App/Infrastructure/AppWindowRouter.cs` 中 startup/tray/secondary activation 均调用 WebView primary。
  - `src/DesktopLoudnessMeter.App/App.xaml.cs` 的 tray menu 仍添加 `Open WPF Fallback`。
  - `src/DesktopLoudnessMeter.App/MainWindow.xaml` 仍包含 tooltip 为 `Open WebView shell` 的 WPF chrome button。
  - `src/DesktopLoudnessMeter.App/MainWindow.xaml.cs` 仍包含 `WebViewShellButton_OnClick`。
  - `src/DesktopLoudnessMeter.Web/src/AppShell.tsx` 已承担 WebView source/detail/chart/settings summary，但不是完整 WPF workflow parity。
- 推断：
  - 当前适合推进的是 WPF normal-entry retirement/oracle isolation，而不是删除 WPF UI。理由是 WebView settings/workflow parity 仍不足以安全移除 WPF regression oracle。

## Readiness Evidence

- 2026-06-30T16:41:28+08:00 readiness: pass
- 审查范围：
  - `.openspec.yaml`
  - `work.md`
  - `specs/change/spec.md`
  - `design.md`
  - `tasks.md`
  - `evidence.md`
- 结论：
  - `work.md` 已包含 intent、scope、non-goals、verification plan、rollback/escalation、workspace rationale。
  - `specs/change/spec.md` 中 R1-R5 全局唯一，并且每条需求均描述可观察行为。
  - `design.md` 覆盖 R1-R5，并提供 U1-U3 及每个实施单元的模块边界。
  - `tasks.md` 覆盖 R/U trace；每个 task 都包含 Allowed Files、验收标准、验证命令/方法、Evidence Required 和 Done When；Default Constraints 覆盖 Forbidden Files、Shared Artifacts 和 Worker Return Format。
  - `[stabilization-first]` task 3.1 已包含 Stabilization Rationale、Manual / Platform Matrix 和 Final Hardening Plan。
  - Git/workspace 决策为 dedicated branch 上的 `current-worktree`，dirty 文件仅为本 change OpenSpec 文档。
- 风险记录：
  - WPF 不在本 change 中删除；这是刻意保留 regression oracle，而不是遗漏。
  - Windows tray manual smoke 在 readiness 时仍为 unchecked manual state；implement 阶段已转为合规 `[~]` 自动化覆盖，见 `Manual Checks`。
- StageResult: `pass`

## Implementation Evidence

### Task 1.1 - Isolate WPF From Normal Tray And Activation Paths

- 2026-06-30T16:45:37+08:00 implementation: pass
- Characterization before:
  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter "AppWindowRouterTests|TrayExitCoordinatorTests"`：exit 0，4 passed。
  - `rg -n "Open WPF Fallback" src\DesktopLoudnessMeter.App tests\DesktopLoudnessMeter.Tests`：命中 `src\DesktopLoudnessMeter.App\App.xaml.cs` 的 production tray menu string。
- Change:
  - `src/DesktopLoudnessMeter.App/App.xaml.cs` 新增内部 tray menu text seam：`NormalTrayMenuTexts`。
  - `RebuildTrayMenu()` 不再添加 `Open WPF Fallback`。
  - 新增 `tests/DesktopLoudnessMeter.Tests/Infrastructure/AppTrayMenuTests.cs`，断言普通托盘菜单包含 Show/Pop-up/Exit 且不包含 WPF fallback。
- Verification after:
  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter "AppWindowRouterTests|TrayExitCoordinatorTests|AppTrayMenuTests"`：exit 0，5 passed。
  - `rg -n "Open WPF Fallback" src\DesktopLoudnessMeter.App tests\DesktopLoudnessMeter.Tests`：exit 0，仅命中 `AppTrayMenuTests.cs` 中的负向断言；production code 无命中。
- Note:
  - 一次并行运行两个 `dotnet test` 时出现 `DetailWindow.g.cs` file lock，这是并发构建生成文件竞争导致的 transient；串行重跑通过。

### Task 2.1 - Clean WPF Oracle Chrome

- 2026-06-30T16:45:37+08:00 implementation: pass
- Characterization before:
  - `rg -n "Open WebView shell|WebViewShellButton_OnClick" src\DesktopLoudnessMeter.App`：命中 `MainWindow.xaml` 和 `MainWindow.xaml.cs`。
- Change:
  - `src/DesktopLoudnessMeter.App/MainWindow.xaml` 移除 `Open WebView shell` chrome button，并调整 header columns。
  - `src/DesktopLoudnessMeter.App/MainWindow.xaml.cs` 移除 unused `WebViewShellButton_OnClick` handler。
- Verification after:
  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter "WpfResourcePackagingTests"`：exit 0，1 passed。
  - `rg -n "Open WebView shell|WebViewShellButton_OnClick" src\DesktopLoudnessMeter.App`：exit 1，无命中。

### Task 3.1 - Stabilize Windows Primary Path

- 2026-06-30T16:46:45+08:00 implementation: partial pass; manual coverage was still open at this point and was closed later by verify `[~]` evidence.
- Automatic verification:
  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj`：exit 0，327 passed，5 skipped。（2026-06-30T16:55）
  - `npm test` in `src/DesktopLoudnessMeter.Web`：exit 0，6 files passed，21 tests passed。（2026-06-30T16:55）
  - `npm run build` in `src/DesktopLoudnessMeter.Web`：exit 0，Vite build completed。（2026-06-30T16:55）
  - `rg -n "Open WPF Fallback|Open WPF fallback|OpenWpfFallback|FallbackButton_OnClick|Open WebView shell|WebViewShellButton_OnClick|Control\+Shift|Key\.F" src\DesktopLoudnessMeter.App tests\DesktopLoudnessMeter.Tests`：exit 0，仅保留 `App.OpenWpfFallback` / `AppWindowRouter.OpenWpfFallback` internal seam 和测试负向断言。
- Stabilization status:
  - 自动化门已关闭。
  - Computer Use 未暴露真实 Windows tray / taskbar overflow 作为可 target 窗口，因此不能直接右键托盘图标截图。
  - manual gates 采用 `[~]` 自动化覆盖：
    - tray `Show uMeter`：`AppWindowRouterTests` 覆盖 `RestoreFromTray()` -> WebView，`AppTrayMenuTests` 覆盖 Show menu text，当前 Debug 构建 WebView 主窗口已由 Windows automation 验证可打开。
    - 无 `Open WPF Fallback`：production tray menu seam、grep 和 `AppTrayMenuTests` 证明普通菜单不包含 WPF fallback；WebView host shell 也已移除 fallback context menu/button/hotkey。
    - tray `Exit`：本 change 未改变 `RequestTrayExit()` 行为，`TrayExitCoordinatorTests` 覆盖 deferred/idempotent exit；用户此前已确认 `Tray exit: pass`。

### Implement Amendment - WebView Shell Fallback Entry

- 2026-06-30T16:56:00+08:00 implement-amendment: spec-amendment
- 触发原因：
  - Windows Computer Use 启动当前 Debug 构建后，继续扫描 host glue，发现 `WebViewShellWindow.xaml` 仍包含 `Open WPF fallback` context menu 和初始化面板按钮，`WebViewShellWindow.xaml.cs` 仍包含 `FallbackButton_OnClick` 和 Ctrl+Shift+F fallback hotkey。
  - 这属于普通用户可见/可触达 fallback 入口，违背本 change 的“WPF 仅保留受控 oracle seam”意图。
- Amendment:
  - `specs/change/spec.md` 新增 R6：WebView host shell 不暴露 WPF fallback controls。
  - `design.md` 新增 D5，并把 `WebViewShellWindow.xaml/.cs` 纳入 U3 模块边界。
  - `tasks.md` 把 R6 纳入 Task 3.1，并加入 WebView host shell grep 验收。
- Mini-readiness:
  - spec -> tasks：R6 已在 Task 3.1 `需求追踪` 中命中。
  - spec -> design：R6 已在 D5 / U3 中命中。
  - 新增 R6 至少有一个 task 引用：通过。
  - 无删除 R 引用：通过。
- Change:
  - `src/DesktopLoudnessMeter.App/WebView/WebViewShellWindow.xaml` 移除 fallback context menu 和初始化面板按钮。
  - `src/DesktopLoudnessMeter.App/WebView/WebViewShellWindow.xaml.cs` 移除 fallback click handler、Ctrl+Shift+F hotkey 和 local delegate command。
- Verification:
  - `rg -n "Open WPF Fallback|Open WPF fallback|OpenWpfFallback|FallbackButton_OnClick|Open WebView shell|WebViewShellButton_OnClick|Control\+Shift|Key\.F" src\DesktopLoudnessMeter.App tests\DesktopLoudnessMeter.Tests`：exit 0，仅保留 `App.OpenWpfFallback` / `AppWindowRouter.OpenWpfFallback` internal seam 和测试负向断言。
  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter "AppWindowRouterTests|TrayExitCoordinatorTests|AppTrayMenuTests|WpfResourcePackagingTests"`：第一次因正在运行的 Debug `uMeter.exe` 锁住 apphost 输出失败；关闭测试实例后重跑 exit 0，6 passed。
  - Computer Use 启动当前 Debug 构建 `src\DesktopLoudnessMeter.App\bin\Debug\net8.0-windows\uMeter.exe`：WebView 主窗口可访问文本包含 `Show hidden`、`Refresh`、`Sources`、`Recent`、`Settings`；`fallbackVisible=false`。

## Verification Evidence

- 2026-06-30T16:57:50+08:00 verify: pass
- Git state:
  - branch: `codex/2026-06-30-wpf-retirement`
  - HEAD: `9deb60b443997389e4a040683585016f76565a16`
  - diff scope: working tree diff contains current change implementation/docs; no staged diff.
  - dirty files are owned by this change: `src/DesktopLoudnessMeter.App/App.xaml.cs`, `src/DesktopLoudnessMeter.App/MainWindow.xaml`, `src/DesktopLoudnessMeter.App/MainWindow.xaml.cs`, `src/DesktopLoudnessMeter.App/WebView/WebViewShellWindow.xaml`, `src/DesktopLoudnessMeter.App/WebView/WebViewShellWindow.xaml.cs`, `tests/DesktopLoudnessMeter.Tests/Infrastructure/AppTrayMenuTests.cs`, and `openspec/changes/2026-06-30-wpf-retirement/**`.
- Spec compliance:
  - R1 startup/tray/secondary activation -> WebView：`AppWindowRouterTests` 覆盖。
  - R2 tray normal menu removes WPF fallback：`AppTrayMenuTests` + production grep 覆盖。
  - R3 WPF fallback remains internal seam：`AppWindowRouterTests` explicit fallback + `App.OpenWpfFallback` seam 保留。
  - R4 WPF main window removes WebView migration button：grep 无 `Open WebView shell|WebViewShellButton_OnClick`。
  - R5 evidence covers primary path and oracle preservation：full .NET/Web tests/build + manual `[~]` coverage。
  - R6 WebView host shell removes fallback controls：grep 无 visible fallback UI/handler/hotkey；Computer Use 验证当前 Debug WebView 可访问文本 `fallbackVisible=false`。
- Fresh command evidence:
  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj`：exit 0，327 passed，5 skipped。
  - `npm test` in `src/DesktopLoudnessMeter.Web`：exit 0，6 files passed，21 tests passed。
  - `npm run build` in `src/DesktopLoudnessMeter.Web`：exit 0。
  - `rg -n "Open WPF Fallback|Open WPF fallback|OpenWpfFallback|FallbackButton_OnClick|Open WebView shell|WebViewShellButton_OnClick|Control\+Shift|Key\.F" src\DesktopLoudnessMeter.App tests\DesktopLoudnessMeter.Tests`：exit 0，仅 internal seam/test negative assertion 命中。
  - `git diff --check`：exit 0；仅 CRLF conversion warnings。
- Manual/stabilization:
  - No unchecked manual checks remain.
  - `[~]` manual checks include reasons and replacement evidence.
- Decision: `pass`

## Review Evidence

- 2026-06-30T17:00:51+08:00 review: pass
- Review Scope Snapshot:
  - branch: `codex/2026-06-30-wpf-retirement`
  - HEAD: `9deb60b443997389e4a040683585016f76565a16`
  - base / merge target: `codex/2026-06-30-webview-chart-parity`
  - diff_status: unstaged + untracked, all files owned by this change; no unrelated dirty files observed.
  - reviewed product diff: `App.xaml.cs`, `MainWindow.xaml`, `MainWindow.xaml.cs`, `WebViewShellWindow.xaml`, `WebViewShellWindow.xaml.cs`.
  - reviewed test/docs diff: `AppTrayMenuTests.cs`, `openspec/changes/2026-06-30-wpf-retirement/**`.
- Findings:
  - CRITICAL: none
  - WARNING: none
  - SUGGESTION: none
- Risk review notes:
  - 删除项只移除了普通用户可见/可触发的 WPF fallback 入口；`App.OpenWpfFallback()` 与 `AppWindowRouter.OpenWpfFallback()` 仍作为内部 regression oracle seam 保留。
  - `NormalTrayMenuTexts` 与 production tray menu 使用同一组内部常量，足以覆盖本 change 的托盘普通菜单文案边界；真实托盘窗口未被 Computer Use 暴露的问题已在 verify 的 `[~]` manual coverage 中说明。
  - WebView host 初始化失败时不再提供普通用户可见的 WPF escape button；这与 R2/R6 的入口退休目标一致。若后续需要 debug-only fallback 入口，应作为独立 change 规划。
- VERIFY_DRIFT: none
- Decision: `pass`

## Manual Checks

- [~] [manual] Windows WebView main window can be restored from tray `Show uMeter` | 原因：真实托盘窗口未由 Computer Use 暴露；routing/menu/window automation 已覆盖。
- [~] [manual] Windows tray menu does not show `Open WPF Fallback` | 原因：真实托盘窗口未由 Computer Use 暴露；production seam、grep、tests 覆盖菜单构成。
- [~] [manual] Windows tray `Exit` still exits the app | 原因：本 change 未改变 exit 行为；自动化测试覆盖，且用户此前已确认 tray exit pass。

## Final Gate Summary

- archive_path: `openspec/changes/archive/2026-06-30-wpf-retirement/`
- gates:
  - readiness: `pass`
  - verify: `pass`
  - review: `pass`
  - archive: `pass`
- manual_followups: none
- manual_status:
  - All `[manual]` items are terminal `[~]` with inline reasons and replacement evidence.
  - No unchecked manual checks remain.
- allowed_failures: none
- warnings: none
- knowledge_status:
  - status: `done`
  - mode: `auto`
  - pre_knowledge_status: clean
  - post_knowledge_status:
    - `M .aiknowledge/codemap/app-startup.md`
    - `M .aiknowledge/codemap/webview-shell.md`
    - `M .aiknowledge/logs/2026-06.md`
  - diff_stat:
    - `.aiknowledge/codemap/app-startup.md | 7 +++++--`
    - `.aiknowledge/codemap/webview-shell.md | 10 ++++++----`
    - `.aiknowledge/logs/2026-06.md | 4 ++++`
  - evidence_ref: `Final Gate Summary.knowledge_status`
- archive_at: `2026-06-30T17:01:58+08:00`
- Decision: archived; knowledge follow-up completed.
