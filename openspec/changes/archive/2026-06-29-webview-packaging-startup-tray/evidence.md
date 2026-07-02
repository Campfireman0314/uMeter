# Evidence: 2026-06-29-webview-packaging-startup-tray

## Intake Evidence

### 2026-06-29 - required round intake

Commands:
- `opsx changes list`
  - result: no active changes.
- `Get-ChildItem openspec/changes/archive/2026-06-29-cross-platform-architecture-plan`
  - result: archived architecture planning artifacts exist.
- `Get-Content .aiknowledge/codemap/index.md`
  - result: relevant modules include `app-startup`, `ui-runtime-architecture`, `webview-shell`, `host-contract`, `source-list-workspace`.
- `git status --short --branch`
  - result: clean tree on `codex/2026-06-29-juce-sender-protocol-boundary`.

Key facts:
- `webview-shell` codemap states WPF remains default startup/fallback and WebView shell is opened explicitly.
- `ui-runtime-architecture` codemap states WebView2/WKWebView + TypeScript/React/Vite is current route; WPF remains fallback until parity and retirement.
- `app-startup` codemap states single-instance lock must happen before receivers, settings writers, tray icons or windows.

## Explore Evidence

Read sources:
- `src/DesktopLoudnessMeter.App/App.xaml.cs`
- `src/DesktopLoudnessMeter.App/MainWindow.xaml`
- `src/DesktopLoudnessMeter.App/MainWindow.xaml.cs`
- `src/DesktopLoudnessMeter.App/WebView/WebViewShellWindow.xaml`
- `src/DesktopLoudnessMeter.App/WebView/WebViewShellWindow.xaml.cs`
- `src/DesktopLoudnessMeter.App/WebView/WebViewAssetLocator.cs`
- `src/DesktopLoudnessMeter.App/DesktopLoudnessMeter.App.csproj`
- `build-installer.ps1`
- `src/DesktopLoudnessMeter.Setup/Program.cs`
- `tests/DesktopLoudnessMeter.Tests/Infrastructure/SingleInstanceCoordinatorTests.cs`
- `tests/DesktopLoudnessMeter.Tests/Infrastructure/TrayExitCoordinatorTests.cs`
- `tests/DesktopLoudnessMeter.Tests/WebView/WebViewAssetLocatorTests.cs`
- `docs/current-status.md`

Observations:
- Current startup creates and shows WPF `MainWindow` by default.
- `OpenWebViewShell()` already reuses `_sourceMeterService` and `_workspaceSettings`.
- Tray 鈥淪how uMeter鈥?and double-click currently call `ShowMainWindow()`.
- Setup project exists; `build-installer.ps1` publishes app output, zips it as payload, then publishes setup.
- App project has Web frontend build/copy target, but publish payload must be verified.

Self-check summary:
- Candidate recommendation: `2026-06-29-webview-packaging-startup-tray`.
- Round 1 blocking: suspected setup project missing. Resolved by direct `Test-Path` and `rg --files`: setup project exists.
- Round 2 blocking: none.
- Non-blocking uncertainty: full installer route may need JUCE/CMake; WebView2/tray/secondary activation require manual Windows runtime smoke.
- Final scan conclusion: route to `opsx-plan` with `strict-change`.

## Plan Evidence

### 2026-06-29 - plan artifacts initialized

Commands:
- `git switch -c codex/2026-06-29-webview-packaging-startup-tray`
  - result: created and switched to new branch.
- `opsx changes init 2026-06-29-webview-packaging-startup-tray spec-driven --profile strict-change --source-type feature`
  - result: initialized change root.

Plan decision:
- Split conclusion: `KEEP_ONE_CHANGE`.
- Profile: `strict-change`.
- Reason: default startup, tray/activation, WPF fallback and Web asset payload are one Windows user-visible route. Chart parity, WPF retirement and macOS readiness remain later changes.

## Readiness Evidence

### 2026-06-29 - readiness gate

StageResult:
- stage: readiness
- status: pass
- reviewer: main agent
- change: `2026-06-29-webview-packaging-startup-tray`

Checks:
- `work.md` contains goal, scope in/out, dependencies, completion criteria, split conclusion, workspace strategy, verification plan and rollback.
- `specs/change/spec.md` defines globally unique R1-R6 requirements with observable scenarios.
- `design.md` maps R1-R6 to U1-U5, preserves known facts vs inferences, and records risks, rollback and stabilization strategy.
- `tasks.md` contains executable task contracts with allowed files, verification, evidence and done conditions. Default Constraints cover forbidden/shared artifacts and worker return format.
- `[stabilization-first]` tasks include uncertainty source, platform/manual matrix, exit conditions and final hardening plan.
- Workspace strategy is `current-worktree` on branch `codex/2026-06-29-webview-packaging-startup-tray`, based on clean head `3c8069a405590da0e9ac36230100d9c3b39a27e5`.
- Relevant pitfalls were read:
  - `.aiknowledge/pitfalls/misc/filtered-views-are-not-lifecycle-state.md`
  - `.aiknowledge/pitfalls/misc/native-consumers-must-import-copy-targets.md`

Conclusion:
- Gate passes. The change can proceed to `opsx-implement`.

## Implementation Evidence

## test-cycle | 2026-06-29T21:03:59+08:00 | pass
浠诲姟锛歍ask 1 - Characterize current startup and tray routing
鎵ц鏂瑰紡锛歝haracterization-first

### 绾㈤樁娈?- 澶辫触娴嬭瘯鍒楄〃锛?  - `AppWindowRouterTests.StartupTrayAndSecondaryActivationRestoreWebViewPrimary` - `CS0246`: `AppWindowRouter` 涓嶅瓨鍦ㄣ€傝澶辫触鏄悎娉曠孩闃舵锛岀敤鏉ラ┍鍔ㄦ柊澧炲彲娴?routing seam銆?- characterization 鏃ц涓猴細
  - source record: `App.OnStartup` 鍘熷厛鏋勯€?`_mainWindow = new MainWindow { DataContext = _mainViewModel }`锛岃缃?`MainWindow = _mainWindow`锛岀劧鍚?`_mainWindow.Show()` / `_mainWindow.Activate()`銆?  - source record: tray double-click 鍜?`Show uMeter` 鍘熷厛璋冪敤 `ShowMainWindow()`锛岀洰鏍囨槸 WPF main window銆?  - source record: `OpenWebViewShell()` 鍘熷厛浠呬綔涓烘樉寮忓叆鍙ｏ紝骞舵妸 WebView owner 璁剧疆涓?`_mainWindow`銆?
### 缁块樁娈?- 閫氳繃娴嬭瘯鍒楄〃锛?  - `AppWindowRouterTests.StartupTrayAndSecondaryActivationRestoreWebViewPrimary`
  - `AppWindowRouterTests.ExplicitFallbackRequestOpensWpfFallbackOnly`
  - focused suite: `AppWindowRouter|Tray|SingleInstance|WebView|HostBridge`
- 鍙樻洿鎽樿锛?  - `src/DesktopLoudnessMeter.App/Infrastructure/AppWindowRouter.cs` - 鏂板 - 鎶藉嚭 startup/tray/secondary activation 涓?WPF fallback 鐨勭洰鏍囪矾鐢便€?  - `tests/DesktopLoudnessMeter.Tests/Infrastructure/AppWindowRouterTests.cs` - 鏂板 - 鍥哄畾 WebView-primary 涓庢樉寮?WPF fallback 璺敱銆?- characterization 鐩爣琛屼负锛?  - startup / tray restore / secondary activation 浠?WPF target 杩佺Щ涓?WebView target锛沇PF 鍙€氳繃 explicit fallback 鍏ュ彛鎵撳紑銆?
### 閲嶆瀯闃舵
- 閫氳繃娴嬭瘯鍒楄〃锛?  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter "AppWindowRouter|Tray|SingleInstance|WebView|HostBridge"` exit=0锛?0 passed銆?- 瑕嗙洊鐜囷細N/A锛堟湰杞湭鍚敤 coverage collector锛涜ˉ鏁戯細focused suite 瑕嗙洊鏂板 routing seam锛屽悗缁?final verification 璺戝畬鏁存祴璇曢」鐩€傦級

| 楠屾敹鏍囧噯 (AC) | 娴嬭瘯/璇佹嵁 | 閫氳繃鐘舵€?|
|--------------|-----------|---------|
| 鏃?startup 榛樿鏄剧ず WPF 琚褰?| `App.OnStartup` source record | pass |
| 鏃?tray show/double-click 鎸囧悜 WPF 琚褰?| `InitializeTrayIcon` / `RebuildTrayMenu` source record | pass |
| 鐩爣杩佺Щ琛屼负鏄庣‘ | `AppWindowRouterTests` | pass |

## test-cycle | 2026-06-29T21:03:59+08:00 | pass
浠诲姟锛歍ask 2 - Make WebView shell the default startup and activation target
鎵ц鏂瑰紡锛歵est-first

### 绾㈤樁娈?- 澶辫触娴嬭瘯鍒楄〃锛?  - `AppWindowRouterTests.StartupTrayAndSecondaryActivationRestoreWebViewPrimary` - `CS0246`: 浜у搧浠ｇ爜灏氭棤 WebView-primary routing seam銆?- 鍋囩孩淇锛氭棤銆?
### 缁块樁娈?- 閫氳繃娴嬭瘯鍒楄〃锛?  - `AppWindowRouterTests.StartupTrayAndSecondaryActivationRestoreWebViewPrimary`
  - existing focused tests under `SingleInstance|WebView|HostBridge`
- 鍙樻洿鎽樿锛?  - `src/DesktopLoudnessMeter.App/App.xaml.cs` - 淇敼 - primary startup 寤虹珛鍏变韩 service/viewmodel 鍚庤皟鐢?`AppWindowRouter.ShowStartupWindow()`锛岄粯璁ゅ垱寤?鏄剧ず `WebViewShellWindow`銆?  - `src/DesktopLoudnessMeter.App/App.xaml.cs` - 淇敼 - secondary activation replay 璋冪敤 `RestoreFromSecondaryActivation()`锛屼笉鍐嶈皟鐢?WPF `ShowMainWindow()`銆?  - `src/DesktopLoudnessMeter.App/WebView/WebViewShellWindow.xaml` - 淇敼 - WebView shell 浣滀负涓荤獥鍙ｅ眳涓樉绀猴紝閿欒闈㈡澘鎻愪緵 fallback 鎸夐挳銆?  - `src/DesktopLoudnessMeter.App/WebView/WebViewShellWindow.xaml.cs` - 淇敼 - fallback 鎸夐挳璋冪敤 `App.OpenWpfFallback()`銆?
### 閲嶆瀯闃舵
- 閫氳繃娴嬭瘯鍒楄〃锛?  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter "AppWindowRouter|Tray|SingleInstance|WebView|HostBridge"` exit=0锛?0 passed銆?  - `dotnet build src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj` exit=0銆?- 澶囨敞锛氫竴娆″苟琛屾墽琛?focused test 涓?app build 鏃讹紝app build 涓存椂 WPF project 鎶?`DetailWindow.g.cs` / `MainWindow.g.cs` not found锛涢『搴忛噸璺?app build 閫氳繃锛屽綊鍥犱负骞惰 MSBuild `obj` 绔炴€侊紝涓嶄綔涓轰骇鍝佷唬鐮佸け璐ャ€?- 瑕嗙洊鐜囷細N/A锛堟湭閲囬泦鏁板瓧瑕嗙洊鐜囷紱琛ユ晳锛歠ocused suite 鍔?app build 瑕嗙洊 routing seam 涓?XAML wiring銆傦級

| 楠屾敹鏍囧噯 (AC) | 娴嬭瘯/璇佹嵁 | 閫氳繃鐘舵€?|
|--------------|-----------|---------|
| Primary startup default window target is WebView shell | `AppWindowRouterTests.StartupTrayAndSecondaryActivationRestoreWebViewPrimary` + `App.OnStartup` diff | pass |
| Secondary activation replay targets WebView shell | `AppWindowRouterTests.StartupTrayAndSecondaryActivationRestoreWebViewPrimary` + `OnSingleInstanceActivationRequested` diff | pass |
| Single-instance rejection path still exits before app graph | existing `SingleInstanceCoordinatorTests` in focused suite | pass |
| WebView failure remains visible and fallback accessible | `WebViewShellWindow.xaml` visible status panel + fallback button; app build validates wiring | pass |

## test-cycle | 2026-06-29T21:03:59+08:00 | pass
浠诲姟锛歍ask 3 - Preserve WPF fallback and migrate tray actions
鎵ц鏂瑰紡锛歵est-first

### 绾㈤樁娈?- 澶辫触娴嬭瘯鍒楄〃锛?  - `AppWindowRouterTests.ExplicitFallbackRequestOpensWpfFallbackOnly` - `CS0246`: 浜у搧浠ｇ爜灏氭棤 explicit fallback routing seam銆?- 鍋囩孩淇锛氭棤銆?
### 缁块樁娈?- 閫氳繃娴嬭瘯鍒楄〃锛?  - `AppWindowRouterTests.ExplicitFallbackRequestOpensWpfFallbackOnly`
  - existing `TrayExitCoordinatorTests`
- 鍙樻洿鎽樿锛?  - `src/DesktopLoudnessMeter.App/App.xaml.cs` - 淇敼 - tray double-click 鍜?`Show uMeter` 璧?WebView restore锛泃ray 鏂板 `Open WPF Fallback`銆?  - `src/DesktopLoudnessMeter.App/App.xaml.cs` - 淇敼 - `OpenWpfFallback()` 鎳掑垱寤?WPF `MainWindow` 骞跺鐢ㄧ幇鏈?`_mainViewModel`锛屼笉浼氬垱寤虹浜屽 source service / workspace graph銆?  - `src/DesktopLoudnessMeter.App/App.xaml.cs` - 淇敼 - WebView shell 涓嶅啀 owner 鍒?WPF fallback銆?
### 閲嶆瀯闃舵
- 閫氳繃娴嬭瘯鍒楄〃锛?  - `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter "AppWindowRouter|Tray|SingleInstance|WebView|HostBridge"` exit=0锛?0 passed銆?  - `dotnet build src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj` exit=0銆?- 瑕嗙洊鐜囷細N/A锛堟湭閲囬泦鏁板瓧瑕嗙洊鐜囷紱琛ユ晳锛歠ocused suite 瑕嗙洊 tray/fallback 璺敱锛宮anual smoke 瑕嗙洊鐪熷疄鎵樼洏浜や簰銆傦級

| 楠屾敹鏍囧噯 (AC) | 娴嬭瘯/璇佹嵁 | 閫氳繃鐘舵€?|
|--------------|-----------|---------|
| Tray show/double-click show WebView shell | `AppWindowRouterTests.StartupTrayAndSecondaryActivationRestoreWebViewPrimary` + tray diff | pass |
| Tray exposes explicit WPF fallback | `AppWindowRouterTests.ExplicitFallbackRequestOpensWpfFallbackOnly` + tray diff | pass |
| WPF fallback reuses graph | `OpenWpfFallback()` uses existing `_mainViewModel`; source service/settings remain App-owned | pass |
| Closing/hiding WPF fallback does not exit app | fallback is no longer `Application.MainWindow`; `Closed` only clears `_mainWindow` | pass |
| Existing pop-up detail and tray exit semantics remain intact | existing focused suite including `TrayExitCoordinatorTests` | pass |

## test-cycle | 2026-06-29T21:11:28+08:00 | pass
浠诲姟锛歍ask 4 - Validate publish and installer payload Web assets
鎵ц鏂瑰紡锛歴tabilization-first

### Stabilization Rationale
- 涓嶇‘瀹氭€ф潵婧愶細MSBuild publish target ordering銆乂ite dist copy銆乮nstaller payload zip contents銆佸彲閫?JUCE/VST3 toolchain銆?- 涓轰粈涔堜笉鑳藉厛鍐欑洰鏍囪涓烘祴璇曪細璇ヨ涓烘槸 build/publish/zip 浜х墿锛屼笉鏄函涓氬姟閫昏緫锛涘繀椤昏繍琛?publish 鍜?installer route 鍚庢鏌ユ枃浠剁郴缁?zip銆?- 閫€鍑烘潯浠讹細publish output 鍜?installer payload 鍧囧寘鍚?`Web/index.html` 涓?`Web/assets/*`銆?
### Adjustment Ledger
| Attempt | Hypothesis | Change Summary | Verification | Keep/Rollback | Doc Impact |
|---------|------------|----------------|--------------|---------------|------------|
| 1 | 鐜版湁 `BeforeBuild` copy 鍙兘宸茬粡杩涘叆 publish | 鏃犱唬鐮佹敼鍔?| `dotnet publish ... --output artifacts\app-publish-webview-smoke` exit=0锛沗Test-Path ...\Web\index.html` = False | rollback/continue锛岀‘璁ゅ瓨鍦ㄧ湡瀹?payload 缂哄彛 | TASK_EVIDENCE_ONLY |
| 2 | 閫氳繃 `ResolvedFileToPublish` 娉ㄥ叆 Web dist | `DesktopLoudnessMeter.App.csproj` 澧炲姞 `IncludeWebFrontendInPublish` | publish fail: `NETSDK1152` duplicate relative path | rollback锛屾敼鐢?post-publish copy 閬垮紑 SDK conflict resolution | TASK_EVIDENCE_ONLY |
| 3 | publish 瀹屾垚鍚庡鍒?dist 鍒?`$(PublishDir)\Web` | `DesktopLoudnessMeter.App.csproj` 澧炲姞 `CopyWebFrontendToPublishDirectory` | publish exit=0锛沗index=True`锛沗assets=2` | keep锛屾弧瓒?app publish payload | TASK_EVIDENCE_ONLY |
| 4 | installer `-SkipVst3Plugin` route 浼氬帇缂?app publish output 骞朵繚鐣?Web assets | 鏃犻澶栦唬鐮佹敼鍔?| `.\build-installer.ps1 -Configuration Release -Runtime win-x64 -Version 2.3.4 -SkipVst3Plugin` exit=0锛泎ip contains `Web\index.html` and two `Web\assets\*` files | keep | TASK_EVIDENCE_ONLY |

### Manual / Platform Matrix
| 瑕嗙洊椤?| 鏂规硶 | 鐘舵€?| 闃绘柇? | 璇佹嵁 |
|-------|------|------|-------|------|
| App publish Web assets | command + filesystem inspection | pass | yes | `artifacts\app-publish-webview-smoke\Web\index.html` exists; assets count 2 |
| Installer full route | `build-installer.ps1` | pass-with-limitation | no | `-SkipVst3Plugin` route passed; full VST3 route not run because `C:\JUCE` absent in environment scan |
| Installer payload zip | zip entry inspection | pass | yes | `uMeter.Payload.zip` contains `Web\index.html`, CSS and JS assets |

### Final Hardening
- 娴嬭瘯 / 楠岃瘉锛?  - `dotnet publish src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj --configuration Release --runtime win-x64 --self-contained true --output artifacts\app-publish-webview-smoke` exit=0銆?  - `Test-Path artifacts\app-publish-webview-smoke\Web\index.html` = True銆?  - `Get-ChildItem artifacts\app-publish-webview-smoke\Web\assets -File` = 2 files銆?  - `.\build-installer.ps1 -Configuration Release -Runtime win-x64 -Version 2.3.4 -SkipVst3Plugin` exit=0銆?  - zip inspection: `Web\index.html`, `Web\assets\index-Co0mOOWW.css`, `Web\assets\index-DlWCO6n-.js` present銆?- 璞佸厤锛氭湭杩愯 full VST3 installer route锛涘師鍥狅細鏈満 `C:\JUCE` 涓嶅瓨鍦紝change 鐩爣鍏佽鐢?`-SkipVst3Plugin` 楠岃瘉 app/Web payload锛屽苟璁板綍 full route limitation銆?- 瑕嗙洊鐜囷細N/A锛坆uild/publish artifact check 鏃犳暟瀛楄鐩栫巼锛涜ˉ鏁戯細final verification 浼氶噸澶?app publish/payload 妫€鏌ャ€傦級

### Doc Drift Resolution
- doc impact 姹囨€伙細TASK_EVIDENCE_ONLY
- 宸插悓姝ヤ骇鐗╋細`DesktopLoudnessMeter.App.csproj` 淇 publish copy锛涙棤闇€ spec/design amendment銆?- 鏈棴鍚堥」锛歠ull VST3 route 浠嶄緷璧栧閮?JUCE checkout锛屽凡缁撴瀯鍖栬褰曘€?
| 楠屾敹鏍囧噯 (AC) | 璇佹嵁 | 閫氳繃鐘舵€?|
|--------------|------|---------|
| App build/publish output contains `Web/index.html` | publish smoke output | pass |
| App build/publish output contains Vite assets | publish smoke output assets count 2 | pass |
| Installer payload route verifies Web payload | `-SkipVst3Plugin` installer route + zip inspection | pass |
| Setup code does not strip `Web/` | payload zip contains `Web\*`; setup extracts payload recursively | pass |

## implement-amendment | 2026-06-29T21:25:44+08:00 | pass
鏉ユ簮锛歮anual-test
瑙傚療锛?- expected: published app 鍚姩 WebView 涓昏矾寰勫苟鍔犺浇 Web UI銆?- actual: 棣栨浠?`artifacts\app-publish\uMeter.exe` 鍚姩鏃讹紝app 鍦ㄧ獥鍙ｅ垱寤哄墠閫€鍑猴紝鏃ュ織璁板綍 `DllNotFoundException: Unable to load DLL 'DesktopLoudnessMeter.Native'`銆?- platform/device/bundle/log: `%LOCALAPPDATA%\uMeter\umeter.log` at `2026-06-29T21:12:51+08:00`锛沗Test-Path artifacts\app-publish\DesktopLoudnessMeter.Native.dll` was False before fix.
鑼冨洿鍒ゅ畾锛?- trace: [R5]/Task 4/Task 5銆俁5 鍘熷厛鍙啓 Web assets锛屼絾鐢ㄦ埛鍙瀵熺洰鏍囨槸鍙戝竷/瀹夎鍚庣殑 WebView 涓昏矾寰勫彲鍚姩锛沶ative DLL 鏄悓涓€ publish payload 鐨勫惎鍔ㄤ緷璧栥€?- classification: spec-amendment
鍚屾锛?- code/tests:
  - `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeter.Native.targets` - 澧炲姞 `CopyDesktopLoudnessMeterNativeBinaryToPublishDirectory`锛宲ublish 鍚庡鍒?`DesktopLoudnessMeter.Native.dll`銆?  - `src/DesktopLoudnessMeter.App/DesktopLoudnessMeter.App.csproj` - 淇濈暀 post-publish Web dist copy銆?- docs:
  - `specs/change/spec.md` - R5 鎵╁睍涓?Web assets + native runtime dependency銆?  - `design.md` - U4 妯″潡杈圭晫鍔犲叆 native target锛屽苟璁板綍 native publish 缂哄彛澶勭悊銆?  - `tasks.md` - Task 4銆丗inal Verification 鍔犲叆 native DLL publish/payload checks銆?  - `work.md` - 瀹屾垚鏍囧噯鍔犲叆 native runtime dependency銆?楠岃瘉锛?- mini-readiness:
  - `rg -n "R5|native runtime|DesktopLoudnessMeter.Native.dll|Web/native|Web assets and native" ...` exit=0锛孯5 鍦?spec/design/tasks/work 涓棴鐜€?  - spec -> design: R5 still maps to U4銆?  - spec -> tasks: R5 referenced by Task 4, Task 6 and Final Verification銆?- artifact checks:
  - `Test-Path artifacts\app-publish\Web\index.html` = True銆?  - `Test-Path artifacts\app-publish\DesktopLoudnessMeter.Native.dll` = True銆?  - `uMeter.Payload.zip` contains `Web\index.html` and `DesktopLoudnessMeter.Native.dll`銆?寰呭鐞嗭細
- Continue Task 5 smoke and final verification.
Gate impact:
- Historical state at amendment time: verify/review had not yet run.
- Current state is superseded by the 2026-06-30 verify pass entry below.
涓嬩竴姝ワ細
- Superseded by the later Task 5 manual evidence and final-command verification sections. Tray restore and tray exit manual checks were completed on 2026-06-30.

## Verification Evidence

Implementation verification was previously blocked by Task 5 tray manual checks. The user supplied tray restore and tray exit evidence on 2026-06-30; Task 5 is now complete.

### Task 5 partial Windows runtime smoke - 2026-06-29

Command / method:
- Launched published app with Computer Use: `E:\gitlab\uMeter\artifacts\app-publish\uMeter.exe`.
- Inspected real Windows accessibility tree for the app window.
- Launched a second instance through the same published exe.
- Used host fallback shortcut `Ctrl+Shift+F` to open WPF fallback, then closed fallback with `Alt+F4`.

Observed facts:
- Default launch opened a `uMeter` window containing `ShellWebView`.
- After WebView load, the accessibility tree contained `RootWebArea`, `Sources`, `Detail`, `Recent`, `Settings`, and a live source row, proving the React/Vite UI loaded inside WebView rather than only the host status panel.
- Secondary launch left one `uMeter.exe` app/window visible and restored the WebView shell; the tree still contained `ShellWebView` and `RootWebArea`.
- Explicit fallback opened a second `uMeter` window whose tree contained the WPF markers `WASAPI / VST3 source metering` and `Open WebView shell`.
- Closing the WPF fallback left one `uMeter` window alive and that remaining window contained the WebView shell.

Incomplete manual facts:
- Tray double-click / `Show uMeter` restore was not directly clicked in the Windows notification area.
- Tray `Exit` was not directly clicked in the Windows notification area.

Reason:
- Computer Use exposed ordinary app and Explorer windows, but did not expose the taskbar notification area or notify icon as a targetable window. I did not use PowerShell SendKeys/UIAutomation as a fallback because this turn already used Computer Use and the skill forbids mixing direct PowerShell UI automation with Computer Use.
- Source and focused tests cover the tray routing seam (`AppWindowRouterTests`) and deferred exit coordinator (`TrayExitCoordinatorTests`), but the change contract requires actual manual tray checks before archive. This historical blocker was later resolved by user manual evidence on 2026-06-30.

### Task 5 continuation tray automation attempt - 2026-06-29

Commands / method:
- Ran required round intake again: `opsx changes list`, archived architecture work/design reads, `.aiknowledge/codemap/index.md`, and `git status --short --branch`.
- Re-read `opsx-implement` and `computer-use` skill instructions.
- Used Computer Use `sky.list_apps()` and `sky.list_windows()` to search for `uMeter`, `DesktopLoudnessMeter`, `explorer`, `taskbar`, `tray`, `notification`, and `shell`.

Observed facts:
- Historical state at this attempt: `opsx changes list` reported active change `2026-06-29-webview-packaging-startup-tray` with next stage `opsx-verify`, and `tasks.md` / `evidence.md` still kept two Task 5 tray checks open. This historical blocker was later superseded by user manual evidence on 2026-06-30.
- Computer Use returned no targetable taskbar, tray, notification-area, shell, or notify-icon window.
- The only matching Windows shell objects exposed were ordinary `explorer.exe` File Explorer folder windows such as `uMeter - File Explorer`; these are not the notification area and cannot prove tray restore or tray exit.

Conclusion:
- This was a second direct attempt to target the real Windows tray through the approved Computer Use path. At that time the two manual tray checks were still unverified.
- This historical blocker was later resolved by user manual tray restore and tray exit evidence on 2026-06-30.

### Task 5 user manual tray restore evidence - 2026-06-30

User-provided evidence:
- Screenshot: `C:\Users\N22116\AppData\Local\Temp\codex-clipboard-d49a4951-00d3-41ee-a45c-dc26d97ffd9d.png`
- User response: "鍙互鐨?.

Observed from screenshot:
- Window title/content shows the WebView/React UI: header `uMeter`, `Show hidden`, `Refresh`, `Settings`, `Sources`, `Detail`, `Recent`, and `Settings` sections.
- The visible UI includes a WASAPI source row and WebView-style detail/readout cards, not the WPF fallback UI.

Conclusion:
- Treat `Tray double-click or Show uMeter restores WebView shell` as manually verified by the user-provided screenshot and confirmation.
- `Tray Exit exits app` was later verified by the user response `Tray exit: pass`.

### Task 5 user manual tray exit evidence - 2026-06-30

User-provided evidence:
- User response: `Tray exit: pass`.

Conclusion:
- Treat `Tray Exit exits app` as manually verified by the user.
- All Task 5 manual smoke checks are now complete.

### Task 6 docs/codemap refresh - 2026-06-29

Updated:
- `.aiknowledge/codemap/app-startup.md`
- `.aiknowledge/codemap/webview-shell.md`
- `.aiknowledge/codemap/ui-runtime-architecture.md`
- `.aiknowledge/codemap/index.md`
- `.aiknowledge/logs/2026-06.md`
- `docs/current-status.md`

Verification:
- `rg -n "explicit WPF fallback entry|default startup|WPF fallback|WebView" .aiknowledge\codemap docs\current-status.md` exit=0.
- Remaining matches state that WebView is the Windows default startup/activation path and WPF is fallback/regression oracle.
- No updated doc claims chart parity, WPF retirement, or macOS readiness are complete.

### Final command verification - 2026-06-29

Commands:
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj`
  - result: exit=0; 316 passed, 5 skipped.
- `powershell -ExecutionPolicy Bypass -File src\DesktopLoudnessMeter.Native\build-native.ps1 -Configuration Release`
  - result: exit=0; native DLL produced at `src\DesktopLoudnessMeter.Native\x64\Release\DesktopLoudnessMeter.Native.dll`.
- `dotnet build src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj`
  - result: exit=0; app build passed.
- `npm --prefix src\DesktopLoudnessMeter.Web test -- --run`
  - result: exit=0; 5 files / 11 tests passed.
- `npm --prefix src\DesktopLoudnessMeter.Web run build`
  - result: exit=0; Vite build produced `index.html`, CSS, and JS assets.
- `dotnet publish src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj --configuration Release --runtime win-x64 --self-contained true --output artifacts\app-publish-webview-smoke`
  - result: exit=0.
  - checks: `Web/index.html` = true; `Web/assets` file count = 2; `DesktopLoudnessMeter.Native.dll` = true.
- `.\build-installer.ps1 -Configuration Release -Runtime win-x64 -Version 2.3.4 -SkipVst3Plugin`
  - result: installer generated `artifacts\uMeter.Setup.exe` and `artifacts\uMeter-Setup-2.3.4.exe`.
  - note: one follow-up zip check initially used the wrong path (`artifacts\setup-build\uMeter.Payload.zip`) and failed after installer success; this was a check-script path error, not an installer failure.
  - corrected payload check: `artifacts\uMeter.Payload.zip` exists; after normalizing path separators, `Web/index.html` = true; `Web/assets` file count = 2; `DesktopLoudnessMeter.Native.dll` = true.

## Review Evidence

Pending.

## Manual Checks

- [x] [manual] Windows WebView shell opens by default on app launch.
- [x] [manual] Tray double-click or `Show uMeter` restores WebView shell.
- [x] [manual] Secondary launch activates WebView shell and exits.
- [x] [manual] WPF fallback opens explicitly and can be hidden/closed without exiting app.
- [x] [manual] Tray Exit exits app.

## Archive Notes

Pending.
### Final command verification refresh - 2026-06-30

Commands and results:
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj` - pass; 316 passed, 5 skipped, 321 total.
- `powershell -ExecutionPolicy Bypass -File src\DesktopLoudnessMeter.Native\build-native.ps1 -Configuration Release` - pass; native Release DLL built.
- `dotnet build src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj` - pass.
- `npm --prefix src\DesktopLoudnessMeter.Web test -- --run` - pass; 5 files, 11 tests.
- `npm --prefix src\DesktopLoudnessMeter.Web run build` - pass; Vite build generated `dist/index.html` and assets.
- `dotnet publish src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj --configuration Release --runtime win-x64 --self-contained true --output artifacts\app-publish-webview-smoke` - pass; publish output contained `Web/index.html`, 2 web assets, and `DesktopLoudnessMeter.Native.dll`.
- `.\build-installer.ps1 -Configuration Release -Runtime win-x64 -Version 2.3.4 -SkipVst3Plugin` - pass; installer generated and payload zip contained `Web/index.html`, 2 web assets, and `DesktopLoudnessMeter.Native.dll`.

Manual evidence:
- User-provided screenshot `C:\Users\N22116\AppData\Local\Temp\codex-clipboard-d49a4951-00d3-41ee-a45c-dc26d97ffd9d.png` plus confirmation `可以的` verifies tray restore / Show uMeter returns to the WebView shell.
- User response `Tray exit: pass` verifies tray Exit exits the app.

Conclusion:
- Task 5 manual smoke is complete.
- The change is ready for `opsx-verify`.

## verify | 2026-06-30T15:04:34+08:00 | pass

Direction: `tasks.md` + `specs/**/*.md` + `design.md` + implementation diff -> verified.

Evidence:
- Read current change artifacts: `.openspec.yaml`, `work.md`, `tasks.md`, `design.md`, `specs/change/spec.md`, `evidence.md`.
- Read required OpsX verify rules: `opsx-verify/SKILL.md`, `references/reviewer-prompt.md`, `references/fresh-evidence.md`, and `~/.opsx/common/git-operations.md`.
- `opsx changes resolve 2026-06-29-webview-packaging-startup-tray` resolved to `/e/gitlab/uMeter/openspec/changes/2026-06-29-webview-packaging-startup-tray`.
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj` passed: 316 passed, 5 skipped, 321 total.
- `npm --prefix src\DesktopLoudnessMeter.Web test -- --run` passed: 5 files, 11 tests.
- `powershell -ExecutionPolicy Bypass -File src\DesktopLoudnessMeter.Native\build-native.ps1 -Configuration Release` passed: native Release DLL built.
- `dotnet build src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj` passed with 0 warnings and 0 errors.
- `npm --prefix src\DesktopLoudnessMeter.Web run build` passed and produced `dist/index.html` plus CSS/JS assets.
- `dotnet publish src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj --configuration Release --runtime win-x64 --self-contained true --output artifacts\app-publish-webview-smoke` passed; publish output check returned `{"index":true,"assets":2,"native":true}`.
- `.\build-installer.ps1 -Configuration Release -Runtime win-x64 -Version 2.3.4 -SkipVst3Plugin` passed and produced `artifacts\uMeter.Setup.exe` plus `artifacts\uMeter-Setup-2.3.4.exe`.
- Initial payload inspection used obsolete path `artifacts\payload.zip` and failed to find the file; script inspection showed the actual payload is `artifacts\uMeter.Payload.zip`.
- Corrected payload inspection of `artifacts\uMeter.Payload.zip` passed: `{"payload":true,"setup":true,"versionedSetup":true,"index":true,"assets":2,"native":true}`.
- Manual status is verified: user screenshot and confirmation `可以的` verify tray restore / Show uMeter returns to the WebView shell; user response `Tray exit: pass` verifies tray Exit.
- `rg -n "\[ \]|\[~\]|\[→\]|\[鈫" openspec\changes\2026-06-29-webview-packaging-startup-tray\tasks.md` returned no incomplete/deferred task checkboxes.
- Implementation evidence includes `AppWindowRouter` routing startup/tray/secondary activation to WebView and explicit fallback to WPF, WebView shell fallback entry, app publish Web copy target, and native publish copy target.
- Git: branch=`codex/2026-06-29-webview-packaging-startup-tray`; HEAD=`3c8069a405590da0e9ac36230100d9c3b39a27e5`; base=`codex/2026-06-29-juce-sender-protocol-boundary` at `3c8069a405590da0e9ac36230100d9c3b39a27e5`; diff_status=unstaged/untracked current-change files only; verification_coverage=worktree; manual_status=verified; stabilization_status=closed; amendment_status=reverified.

StageResult:
```json
{
  "stage": "verify",
  "decision": "pass",
  "change": "2026-06-29-webview-packaging-startup-tray",
  "findings": [],
  "allowed_failures": [],
  "summary": {
    "spec_compliance": "pass",
    "completeness": "all tasks and manual checks are complete",
    "correctness": "fresh automated, publish, installer payload, and manual evidence cover R1-R6",
    "consistency": "implementation follows WebView-primary startup while preserving WPF fallback and single-instance/tray ownership",
    "test_cycle_evidence": "test-first, characterization-first, stabilization-first, and direct tasks have recorded command/manual evidence",
    "manual": "verified"
  }
}
```

Issues:
- None blocking.

Corrections:
- Payload verification path corrected from obsolete `artifacts\payload.zip` to actual `artifacts\uMeter.Payload.zip`.

## review | 2026-06-30T15:07:35+08:00 | pass

Review Scope Snapshot:
- branch: `codex/2026-06-29-webview-packaging-startup-tray`
- HEAD: `3c8069a405590da0e9ac36230100d9c3b39a27e5`
- base: `codex/2026-06-29-juce-sender-protocol-boundary` at `3c8069a405590da0e9ac36230100d9c3b39a27e5`
- diff_status: unstaged/untracked files are attributed to this change: startup/tray/WebView shell code, publish/native targets, AppWindowRouter tests, OpenSpec artifacts, docs, and codemap updates.
- gate precondition: `gates.verify.status=pass` at `2026-06-30T15:04:34+08:00`.

Reviewed risk threads:
- WebView-primary startup/tray/secondary activation: `AppWindowRouter` sends startup, tray restore, and secondary activation to `OpenWebViewShell`; explicit fallback routes to `OpenWpfFallback`.
- WPF fallback lifecycle: fallback is lazy-created with the existing `MainViewModel`, source service, and workspace settings; closing fallback only nulls `_mainWindow` and does not create a second service graph.
- WebView host lifecycle: `WebViewShellWindow.Closed` unsubscribes `WebMessageReceived` and disposes `ShellWebView`; `DesktopMeterHostController` and `HostBridgeController` do not hold external subscriptions or disposable resources.
- Publish/installer payload: app publish and installer `uMeter.Payload.zip` checks cover `Web/index.html`, two Vite assets, and `DesktopLoudnessMeter.Native.dll`, matching the active native copy target pitfall.
- Docs/knowledge drift: codemap and `docs/current-status.md` now say WebView is Windows primary startup/tray/activation target, WPF remains fallback, and chart parity/WPF retirement/macOS readiness remain incomplete.

Findings:
- None.

StageResult:
```json
{
  "stage": "review",
  "decision": "pass",
  "change": "2026-06-29-webview-packaging-startup-tray",
  "findings": [],
  "allowed_failures": [],
  "summary": "No CRITICAL, WARNING, or SUGGESTION release-risk findings in the current change diff."
}
```

Next:
- Route to `opsx-archive`.

## archive | 2026-06-30T15:09:34+08:00 | pass

Archive result:
- Moved change from `openspec/changes/2026-06-29-webview-packaging-startup-tray/` to `openspec/changes/archive/2026-06-29-webview-packaging-startup-tray/`.
- Updated archived `.openspec.yaml`: `status: archived`, `gates.archive.status: pass`, `gates.archive.at: 2026-06-30T15:09:34+08:00`.
- Spec file count: 1.

## Final Gate Summary

archive_path: `openspec/changes/archive/2026-06-29-webview-packaging-startup-tray/`

gates:
- readiness: pass at `2026-06-29T20:59:38+08:00`
- verify: pass at `2026-06-30T15:04:34+08:00`
- review: pass at `2026-06-30T15:07:35+08:00`
- archive: pass at `2026-06-30T15:09:34+08:00`

manual_followups: []

allowed_failures: []

warnings: []

knowledge_status:
- status: done
- evidence_ref: `knowledge auto follow-up | 2026-06-30T15:10+08:00`

## knowledge auto follow-up | 2026-06-30T15:10+08:00 | done

Context:
- Archived change: `2026-06-29-webview-packaging-startup-tray`
- Archive path: `openspec/changes/archive/2026-06-29-webview-packaging-startup-tray/`
- source_refs use `change:2026-06-29-webview-packaging-startup-tray` and `evidence:openspec/changes/archive/2026-06-29-webview-packaging-startup-tray/evidence.md`.

pre_knowledge_status:
```text
 M .aiknowledge/codemap/app-startup.md
 M .aiknowledge/codemap/index.md
 M .aiknowledge/codemap/ui-runtime-architecture.md
 M .aiknowledge/codemap/webview-shell.md
 M .aiknowledge/logs/2026-06.md
```

post_knowledge_status:
```text
 M .aiknowledge/codemap/app-startup.md
 M .aiknowledge/codemap/index.md
 M .aiknowledge/codemap/ui-runtime-architecture.md
 M .aiknowledge/codemap/webview-shell.md
 M .aiknowledge/logs/2026-06.md
 M .aiknowledge/pitfalls/index.md
 M .aiknowledge/pitfalls/misc/index.md
 M .aiknowledge/pitfalls/misc/native-consumers-must-import-copy-targets.md
```

diff_stat:
```text
 .aiknowledge/codemap/app-startup.md                    | 18 ++++++++++++------
 .aiknowledge/codemap/index.md                          |  4 ++--
 .aiknowledge/codemap/ui-runtime-architecture.md        |  6 ++++--
 .aiknowledge/codemap/webview-shell.md                  | 14 +++++++++-----
 .aiknowledge/logs/2026-06.md                           |  8 ++++++++
 .aiknowledge/pitfalls/index.md                         |  2 +-
 .aiknowledge/pitfalls/misc/index.md                    |  2 +-
 .../misc/native-consumers-must-import-copy-targets.md  | 11 ++++++++---
 8 files changed, 45 insertions(+), 20 deletions(-)
```

Actions:
- Refreshed codemap source refs from active change path to archive evidence.
- Updated `app-startup`, `webview-shell`, and `ui-runtime-architecture` verification dates to 2026-06-30 where this archive follow-up reverified them.
- Extended the active `Native Consumers Must Import Copy Targets` pitfall to include app publish and installer payload routes after this change exposed a missing native DLL in the published WebView-primary app.
- Fixed `.aiknowledge/pitfalls/index.md` misc active count to match the four indexed misc entries.
- Verified misc pitfall file count and index row count are both 4.
