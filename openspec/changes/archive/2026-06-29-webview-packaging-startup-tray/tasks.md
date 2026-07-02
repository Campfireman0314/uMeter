# Tasks: 2026-06-29-webview-packaging-startup-tray

## Default Constraints

**Forbidden Files**:
- `.openspec.yaml`
- `tasks.md`
- `evidence.md`

**Shared Artifacts**:
- `.openspec.yaml` - controller 串行更新
- `tasks.md` - controller 串行更新
- `evidence.md` - controller 串行更新

**Worker Return Format**: `DONE | DONE_WITH_CONCERNS | NEEDS_CONTEXT | BLOCKED`

## Task 1: Characterize current startup and tray routing [x]

**需求追�?*: [R1], [R2], [R3], [R4] -> [U1], [U2], [U3]
**执行方式**: [characterization-first]
**Allowed Files**:
- `src/DesktopLoudnessMeter.App/App.xaml.cs`
- `src/DesktopLoudnessMeter.App/MainWindow.xaml.cs`
- `src/DesktopLoudnessMeter.App/WebView/WebViewShellWindow.xaml.cs`
- `tests/DesktopLoudnessMeter.Tests/Infrastructure/**`
- `tests/DesktopLoudnessMeter.Tests/WebView/**`

**验收标准**:
- [x] Tests or source-level characterization record that current startup shows WPF by default before implementation.
- [x] Tests or source-level characterization record tray show/double-click currently targets WPF before implementation.
- [x] Characterization identifies which behavior changes are intentional in this change.

**验证命令 / 方法**:
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter "SingleInstance|Tray|WebView|HostBridge"`，预期：existing relevant tests pass before/after characterization additions.

**Evidence Required**:
- Characterization summary and focused test output.

**Done When**:
- Existing behavior is documented enough that startup/tray changes can be reviewed as intentional migration rather than accidental regression.

## Task 2: Make WebView shell the default startup and activation target [x]

**需求追�?*: [R1], [R3], [R4], [R6] -> [U1], [U2]
**执行方式**: [test-first]
**Allowed Files**:
- `src/DesktopLoudnessMeter.App/App.xaml.cs`
- `src/DesktopLoudnessMeter.App/WebView/WebViewShellWindow.xaml`
- `src/DesktopLoudnessMeter.App/WebView/WebViewShellWindow.xaml.cs`
- `tests/DesktopLoudnessMeter.Tests/Infrastructure/**`
- `tests/DesktopLoudnessMeter.Tests/WebView/**`

**验收标准**:
- [x] Primary startup default window target is WebView shell.
- [x] Secondary activation replay targets WebView shell.
- [x] Single-instance rejection path still exits before source receivers, tray icons or windows are created.
- [x] WebView initialization failure remains visible and does not remove fallback access.

**验证命令 / 方法**:
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter "Startup|SingleInstance|WebView|HostBridge"`，预期：new and existing focused tests pass.

**Evidence Required**:
- Red/green test-cycle record for startup/activation target behavior.
- Summary of App startup ownership changes.

**Done When**:
- App startup code defaults to WebView shell without changing single-instance gate order or service ownership.

## Task 3: Preserve WPF fallback and migrate tray actions [x]

**需求追�?*: [R2], [R3], [R6] -> [U1], [U3]
**执行方式**: [test-first]
**Allowed Files**:
- `src/DesktopLoudnessMeter.App/App.xaml.cs`
- `src/DesktopLoudnessMeter.App/MainWindow.xaml`
- `src/DesktopLoudnessMeter.App/MainWindow.xaml.cs`
- `src/DesktopLoudnessMeter.App/Infrastructure/TrayExitCoordinator.cs`
- `tests/DesktopLoudnessMeter.Tests/Infrastructure/**`

**验收标准**:
- [x] Tray “Show uMeter�?and tray double-click show WebView shell.
- [x] Tray exposes an explicit WPF fallback action.
- [x] WPF fallback reuses the existing `MainViewModel`, source service and workspace settings.
- [x] Closing/hiding WPF fallback does not exit the app.
- [x] Existing pop-up detail and tray exit semantics remain intact.

**验证命令 / 方法**:
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter "Tray|Startup|SingleInstance"`，预期：focused tests pass.

**Evidence Required**:
- Red/green test-cycle record for tray/fallback behavior.
- Source summary proving no duplicate source service/receiver graph.

**Done When**:
- Tray and fallback behavior align with WebView-primary startup while preserving WPF as an explicit fallback.

## Task 4: Validate publish and installer payload Web assets and native runtime dependency [x]

**需求追�?*: [R5] -> [U4]
**执行方式**: [stabilization-first]
**Allowed Files**:
- `src/DesktopLoudnessMeter.App/DesktopLoudnessMeter.App.csproj`
- `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeter.Native.targets`
- `build-installer.ps1`
- `src/DesktopLoudnessMeter.Setup/Program.cs`
- `tests/DesktopLoudnessMeter.Tests/WebView/**`

**验收标准**:
- [x] App build/publish output contains `Web/index.html`.
- [x] App build/publish output contains Vite assets under `Web/assets/`.
- [x] App build/publish output contains `DesktopLoudnessMeter.Native.dll`.
- [x] Installer payload route is run, or full VST3 route limitation is explicitly recorded and `-SkipVst3Plugin` route verifies Web/native payload.
- [x] Installer/setup code does not strip `Web/` or native runtime dependency from payload extraction.

**验证命令 / 方法**:
- `dotnet publish src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj --configuration Release --runtime win-x64 --self-contained true --output artifacts\app-publish-webview-smoke`
- `Test-Path artifacts\app-publish-webview-smoke\Web\index.html`
- `Get-ChildItem artifacts\app-publish-webview-smoke\Web\assets`
- `Test-Path artifacts\app-publish-webview-smoke\DesktopLoudnessMeter.Native.dll`
- `.\build-installer.ps1 -Configuration Release -Runtime win-x64 -Version 2.3.4` or `.\build-installer.ps1 -Configuration Release -Runtime win-x64 -Version 2.3.4 -SkipVst3Plugin`

**Evidence Required**:
- Publish output checks for Web assets and native runtime dependency.
- Installer command output or documented toolchain limitation plus `-SkipVst3Plugin` payload evidence.

**Stabilization Rationale**:
- 不确定性来源：MSBuild publish target ordering, npm build/copy behavior, installer payload zip contents, optional JUCE/VST3 packaging toolchain.
- 为什么不能先写目标行为测试：installer payload and publish output are filesystem/build artifacts; target behavior requires running build/publish commands and inspecting produced output.
- 退出条件：publish/payload Web assets are observed or a concrete build-script fix is made and verified.

**Manual / Platform Matrix**:
| 覆盖�?| 方法 | 阻断? | 期望 |
|-------|------|-------|------|
| App publish Web assets | command + filesystem inspection | yes | `Web/index.html` and `Web/assets/*` exist |
| App publish native dependency | command + filesystem inspection | yes | `DesktopLoudnessMeter.Native.dll` exists |
| Installer full route | `build-installer.ps1` | no | Pass if toolchain available; otherwise record limitation and run `-SkipVst3Plugin` route |

**Final Hardening Plan**:
- Add or update automated invariant tests only where build artifact path logic is unit-testable.
- doc impact: `TASK_EVIDENCE_ONLY` unless publish target semantics change, then `DESIGN_AMENDMENT`.

**Done When**:
- WebView default startup will not ship without frontend assets or native runtime dependency in app publish/installer payload.

## Task 5: Windows WebView-primary manual smoke [x]

**需求追�?*: [R1], [R2], [R3], [R6] -> [U1], [U2], [U3]
**执行方式**: [stabilization-first]
**Allowed Files**:
- `src/DesktopLoudnessMeter.App/App.xaml.cs`
- `src/DesktopLoudnessMeter.App/WebView/**`
- `src/DesktopLoudnessMeter.App/MainWindow*`
- `docs/current-status.md`

**验收标准**:
- [x] [manual] Launch app and verify WebView shell opens by default.
- [x] [manual] Tray double-click or `Show uMeter` restores WebView shell.
- [x] [manual] Secondary launch activates WebView shell and exits.
- [x] [manual] WPF fallback opens explicitly and can be hidden/closed without exiting app.
- [x] [manual] Exit from tray exits app.

**验证命令 / 方法**:
- Manual Windows app smoke from built app or `dotnet run --project src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj`.

**Evidence Required**:
- Manual smoke checklist with date, commands and observed results.

**Stabilization Rationale**:
- 不确定性来源：WebView2 runtime, Windows tray UI, secondary process activation and WPF/WebView window focus are runtime UI behaviors.
- 为什么不能先写目标行为测试：WPF/WebView2/tray activation requires an interactive Windows shell; non-UI orchestration can be covered by focused tests but not the actual runtime smoke.
- 退出条件：manual checklist is `[x]` before archive, or change must not archive.

**Manual / Platform Matrix**:
| 覆盖�?| 方法 | 阻断? | 期望 |
|-------|------|-------|------|
| Windows WebView default launch | manual app launch | yes | WebView shell visible |
| Tray restore | manual tray double-click/menu | yes | WebView shell restored |
| Secondary activation | manual second process launch | yes | Primary WebView shell activated; secondary exits |
| WPF fallback | manual tray/menu action | yes | WPF fallback opens and can close/hide without app exit |
| Tray exit | manual tray menu | yes | app exits |

**Final Hardening Plan**:
- Keep automated startup/tray helper tests as regression coverage.
- doc impact: `TASK_EVIDENCE_ONLY` plus status/codemap refresh in Task 6.

**Done When**:
- WebView-primary startup is manually proven on Windows and evidence is recorded.

## Task 6: Refresh docs, codemap and final verification [x]

**需求追�?*: [R1], [R2], [R3], [R5] -> [U5]
**执行方式**: [direct]
**Allowed Files**:
- `.aiknowledge/codemap/app-startup.md`
- `.aiknowledge/codemap/webview-shell.md`
- `.aiknowledge/codemap/ui-runtime-architecture.md`
- `.aiknowledge/codemap/index.md`
- `.aiknowledge/logs/2026-06.md`
- `docs/current-status.md`

**验收标准**:
- [x] Codemap records WebView as Windows default startup target.
- [x] Codemap records WPF as fallback/regression oracle, not default startup.
- [x] `docs/current-status.md` no longer says WebView shell is only an explicit WPF fallback entry.
- [x] No docs/codemap claim chart parity, WPF retirement or macOS readiness are complete.

**验证命令 / 方法**:
- `rg -n "explicit WPF fallback entry|default startup|WPF fallback|WebView" .aiknowledge\codemap docs\current-status.md`

**Evidence Required**:
- Documentation/codemap grep output and summary.

**Done When**:
- Long-lived knowledge reflects the implemented startup/packaging state without overstating unfinished parity work.

## Final Verification

**需求追�?*: [R1], [R2], [R3], [R4], [R5], [R6]
**执行方式**: [direct]
**Allowed Files**:
- 无产品代码修改；controller 只更�?`evidence.md` �?gate metadata�?
**验收标准**:
- [x] All task-level focused checks are recorded.
- [x] Full managed test project passes or failures are triaged as unrelated with evidence.
- [x] Native Release build passes.
- [x] App build passes.
- [x] Web tests/build pass.
- [x] App publish Web/native asset checks pass.
- [x] Installer payload route is recorded.
- [x] Manual WebView-primary smoke is `[x]`.

**验证命令 / 方法**:
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj`
- `powershell -ExecutionPolicy Bypass -File src\DesktopLoudnessMeter.Native\build-native.ps1 -Configuration Release`
- `dotnet build src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj`
- `npm --prefix src\DesktopLoudnessMeter.Web test -- --run`
- `npm --prefix src\DesktopLoudnessMeter.Web run build`
- app publish + Web/native asset checks
- installer payload route command

**Evidence Required**:
- Final command output summary.
- Manual Checks section updated.

**Done When**:
- Evidence supports route to `opsx-verify`.

## Manual Checks

- [x] [manual] Windows WebView shell opens by default on app launch.
- [x] [manual] Tray double-click or `Show uMeter` restores WebView shell.
- [x] [manual] Secondary launch activates WebView shell and exits.
- [x] [manual] WPF fallback opens explicitly and can be hidden/closed without exiting app.
- [x] [manual] Tray Exit exits app.
