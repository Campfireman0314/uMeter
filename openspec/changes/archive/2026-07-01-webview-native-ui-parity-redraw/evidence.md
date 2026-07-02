# Evidence: 2026-07-01-webview-native-ui-parity-redraw

`evidence.md` is the single evidence ledger. Readiness, test-cycle, verify, risk review, and manual validation notes are appended here.

## Final Gate Summary

- final_readiness:
  - status: pass
  - evidence_ref: readiness | 2026-07-01T20:39:07+08:00 | pass
- final_verify:
  - status: pass
  - evidence_ref: verify | 2026-07-01T20:53:34+08:00 | pass
- final_review:
  - status: pass
  - evidence_ref: risk-review | 2026-07-01T20:54:44+08:00 | pass
- final_archive:
  - status: pass
  - archive_path: openspec/changes/archive/2026-07-01-webview-native-ui-parity-redraw
  - evidence_ref: archive | 2026-07-01T20:56:44+08:00 | pass
- allowed_failures: []
- manual_followups: []
- knowledge_status:
  - status: done
  - evidence_ref: knowledge-auto | 2026-07-01T20:59:18+08:00 | done

## Planning Evidence

### 2026-07-01 plan scaffold

- stage: plan
- command: `opsx changes list` -> `无活动变更`
- command: `opsx changes init 2026-07-01-webview-native-ui-parity-redraw spec-driven --profile strict-change --source-type feature` -> initialized one change
- branch: `codex/2026-07-01-host-source-reconcile-boundary`
- current_head: `e0498f9770eb1f4e9f7ef4cad12bc0bae960399d`
- main_head: `4643c9626bd38f02570d6b9565b3bd52c331433e`
- merge_base_with_main: `4643c9626bd38f02570d6b9565b3bd52c331433e`
- dirty_after_init: `?? openspec/changes/2026-07-01-webview-native-ui-parity-redraw/`
- decision: KEEP_ONE_CHANGE
- explicit user constraint: do not split into multiple changes; complete only the needed aggregate change.

### 2026-07-01 UI audit summary

- WPF `MainWindow.xaml` is a compact native meter shell with custom title chrome, status strip, global controls, recent events, source list, and inline detail modules.
- WPF `DetailWindow.xaml` uses a compact selected-source header, readouts, settings controls, and stacked chart modules.
- Web `AppShell.tsx` already has source list, detail, recent events, settings, and chart modules, but the layout is a three-column dashboard rather than the native client shape.
- Web HostBridge currently supports refresh, back, select detail, show hidden, theme/recent/event folder settings, waveform/spectrum modes, detail module visibility, and chart settings commands.
- Web HostBridge does not expose native source alias/hide/order/forget command families, so the redraw will not fake those interactions.

## Readiness

### 2026-07-01T20:39:07+08:00

- stage: readiness
- decision: pass
- evidence_refs:
  - `work.md`
  - `specs/change/spec.md`
  - `design.md`
  - `tasks.md`
- checks:
  - scope/profile/split: pass; `strict-change` is appropriate for a full WebView shell redraw with tests and visual smoke, and `KEEP_ONE_CHANGE` is explicit.
  - trace: pass; R1-R6 are globally unique in spec and are covered by design/tasks.
  - task contract: pass; tasks include allowed files, verification methods, evidence requirements, done conditions, shared artifact handling, and worker return format through default constraints.
  - stabilization: pass; Task 3.1 includes uncertainty sources, manual/platform matrix, exit conditions, and final hardening plan.
  - workspace: pass; current-worktree rationale records clean baseline, current branch, and dirty files limited to this change directory.
  - boundaries: pass; plan forbids extra OpenSpec changes, macOS runner, C++ Core, packaging, installer, and broad HostBridge command expansion.
- findings:
  - warning: WPF has source-management actions that Web HostBridge does not expose; plan correctly treats them as out of scope instead of faking persisted actions.
- next: proceed to opsx-implement.

## Test Cycle Evidence

## test-cycle | 2026-07-01T20:41:28+08:00 | red
任务：Task 1.1 - Capture native-shell parity expectations in Web tests
执行方式：characterization-first

### 红阶段
- 失败测试列表：
  - `AppShell > renders the native-style shell hierarchy, sources, recent events, detail, and settings entry` - Testing Library query failure: 旧 UI 没有 `App chrome`、`Status and controls`、`Meter console`、`Selected source detail` 这些目标 region，且 heading 仍为 `Detail`。
- 假红修正：无。
- characterization 旧行为：旧 UI 仍暴露 `Host workspace` 三栏结构；新增断言把目标行为改为原生客户端式区域结构。
- 命令：`npm test` in `src/DesktopLoudnessMeter.Web` exit=1。
- 结论：红灯有效，失败归因于产品 UI 尚未迁移到目标原生式 shell。

## test-cycle | 2026-07-01T20:44:52+08:00 | pass
任务：Task 1.1 - Capture native-shell parity expectations in Web tests
执行方式：characterization-first

### 绿阶段
- 通过测试列表：
  - `AppShell > renders the native-style shell hierarchy, sources, recent events, detail, and settings entry`
  - `AppShell > does not expose native-only source management commands without HostBridge support`
  - 既有 AppShell command/settings/chart tests 继续通过。
- 变更摘要：
  - `src/DesktopLoudnessMeter.Web/src/AppShell.test.tsx` - 修改 - 新增原生式 shell landmark 断言，并固化“不暴露无 HostBridge 支持的源管理命令”边界。
- 测试变更记录：新增 1 个边界测试，更新 1 个 shell 结构测试。
- characterization 目标行为：旧 `Host workspace` 三栏断言迁移为 `App chrome`、`Status and controls`、`Meter console`、`Selected source detail` 结构。
- 命令：`npm test` in `src/DesktopLoudnessMeter.Web` exit=0；6 files / 29 tests passed。
- 验收标准覆盖对应表：

| 验收标准 (AC) | 测试用例名称 | 通过状态 |
|---|---|---|
| Tests describe compact app chrome, status/global controls, sources, recent events, settings, and selected detail landmarks. | `renders the native-style shell hierarchy...` | pass |
| Tests preserve existing HostBridge command expectations. | `routes refresh, select, back, and show-hidden controls through the bridge`; chart/settings command tests | pass |
| Tests do not assert fake native-only source-management commands. | `does not expose native-only source management commands...` | pass |

### 重构阶段
- 通过测试列表：
  - Web suite 6 files / 29 tests。
- 变更摘要：
  - 无额外测试重构；Task 1.2/2.1 在同一测试保护下继续实施。
- 覆盖率：N/A；不可用原因：当前命令未启用覆盖率采集；补救：最终继续运行 `npm test`、`npm run build` 与浏览器 visual smoke。

## test-cycle | 2026-07-01T20:44:52+08:00 | pass
任务：Task 1.2 - Redraw AppShell into native-style core UI
执行方式：test-first

### 红阶段
- 失败测试列表：
  - 复用 Task 1.1 红灯：目标 shell landmarks 缺失，证明旧 AppShell 未实现原生式结构。
- 假红修正：无。

### 绿阶段
- 通过测试列表：
  - `AppShell > renders the native-style shell hierarchy, sources, recent events, detail, and settings entry`
  - `AppShell > routes refresh, select, back, and show-hidden controls through the bridge`
  - `AppShell > renders visible detail chart modules as stable canvases`
  - `AppShell > dispatches chart controls through HostBridge commands`
  - `AppShell > opens settings surface and dispatches editable global settings commands`
- 变更摘要：
  - `src/DesktopLoudnessMeter.Web/src/AppShell.tsx` - 修改 - 将三栏 workspace 重排为 app chrome、status controls、meter console 和 secondary strip。
  - `src/DesktopLoudnessMeter.Web/src/AppShell.tsx` - 修改 - selected detail 改为原生式 header/readout/stacked chart module 结构，并保留既有 HostBridge command wiring。
- 命令：`npm test` in `src/DesktopLoudnessMeter.Web` exit=0；6 files / 29 tests passed。
- 验收标准覆盖对应表：

| 验收标准 (AC) | 测试用例名称 | 通过状态 |
|---|---|---|
| App chrome, status/global controls, source list, selected detail, recent events, and settings render in one native-client-style shell. | `renders the native-style shell hierarchy...` | pass |
| Existing HostBridge commands still dispatch with the same payloads. | command wiring/settings/chart tests | pass |
| Selected detail shows compact source identity, readouts, and stacked visible chart modules. | `renders visible detail chart modules as stable canvases` | pass |
| No additional OpenSpec change is created. | `opsx changes list` planning evidence showed one active change; final check remains in Task 3.1 | pass |

### 重构阶段
- 通过测试列表：
  - Web suite 6 files / 29 tests。
- 变更摘要：
  - AppShell keeps interaction logic local and does not expand HostBridge command scope.
- 覆盖率：N/A；不可用原因：当前命令未启用覆盖率采集；补救：最终继续运行 `npm test`、`npm run build` 与浏览器 visual smoke。

## test-cycle | 2026-07-01T20:44:52+08:00 | pass
任务：Task 2.1 - Replace dashboard styling with compact native-meter styling
执行方式：test-first

### 红阶段
- 失败测试列表：
  - 复用 Task 1.1 红灯：旧 CSS/DOM 仍是 dashboard-style three-column landmarks，目标原生式 shell structure 缺失。
- 假红修正：无。

### 绿阶段
- 通过测试列表：
  - Web suite 6 files / 29 tests。
- 变更摘要：
  - `src/DesktopLoudnessMeter.Web/src/styles/app.css` - 修改 - 替换为 compact chrome、status band、meter console、stacked chart modules、secondary strip 和 responsive layout。
  - `src/DesktopLoudnessMeter.Web/src/AppShell.tsx` - 修改 - 添加 CSS 所需结构和 selected-source state class。
- 命令：
  - `npm test` in `src/DesktopLoudnessMeter.Web` exit=0；6 files / 29 tests passed。
  - `npm run build` in `src/DesktopLoudnessMeter.Web` exit=0；Vite production build passed。
- 验收标准覆盖对应表：

| 验收标准 (AC) | 证据 | 通过状态 |
|---|---|---|
| The UI uses compact meter-oriented bands/sections instead of a three-column dashboard. | `AppShell` landmarks + CSS structure | pass |
| Source rows, readout tiles, toolbar buttons, and chart modules have stable dimensions. | CSS min heights/grid constraints + component tests | pass |
| Chart modules stack predictably and canvases remain nonblank when data exists. | chart module component tests + build evidence; browser smoke remains Task 3.1 | pass |
| Narrow viewport layout stacks without incoherent text/control overlap. | responsive CSS rules; browser smoke remains Task 3.1 | pass |

### 重构阶段
- 通过测试列表：
  - Web suite 6 files / 29 tests。
  - Web production build.
- 变更摘要：
  - CSS keeps charts vertical and avoids nested decorative card layout.
- 覆盖率：N/A；不可用原因：当前命令未启用覆盖率采集；补救：Task 3.1 将执行 browser visual smoke。

## Stabilization Ledger

## test-cycle | 2026-07-01T20:51:27+08:00 | pass
任务：Task 3.1 - Run browser visual smoke and final verification
执行方式：stabilization-first

### Stabilization Rationale
- 不确定性来源：真实浏览器 viewport、CSS wrapping、chart canvas 绘制、长页面滚动和窄屏控件尺寸无法仅靠 jsdom 完整验证。
- 为什么不能只写目标行为测试：组件测试能验证 HostBridge wiring 和 DOM landmarks，但不能证明真实浏览器中 canvas 非空、无水平溢出、控件没有明显挤压。
- 退出条件：桌面/窄屏浏览器 smoke 通过，Web tests/build 通过，并确认没有新增额外 OpenSpec change。

### Adjustment Ledger
| Attempt | Hypothesis | Change Summary | Verification | Keep/Rollback | Doc Impact |
|---|---|---|---|---|---|
| 1 | Vite dev server 可在 5174 端口启动 | 初次用 `npm` 直接启动失败，改用 `npm.cmd` | `vite-smoke.log` 显示 `http://127.0.0.1:5174/` ready；5174 端口由 node 监听 | keep；Windows 下 `npm.cmd` 是正确入口 | TASK_EVIDENCE_ONLY |
| 2 | 桌面浏览器可验证 native-style shell 和 chart 非空 | in-app browser 打开本地页面，选择 mock source | DOM 显示 1 个 `main`、目标 regions 全部存在、5 个 chart modules、无水平溢出；viewport 截图显示 waveform/spectrum 非空 | keep | TASK_EVIDENCE_ONLY |
| 3 | 下方 chart modules 也可见且无溢出 | 滚动到 chart 下半部分 | 截图显示 stereo/loudness clock/frequency collision；DOM `chartCount=5`、`horizontalOverflow=false` | keep | TASK_EVIDENCE_ONLY |
| 4 | 窄屏布局可稳定堆叠 | 临时设置 viewport `390x820`，选择 source 后检查 DOM 和截图 | `chartCount=5`、`canvasCount=5`、`horizontalOverflow=false`、`clipped=[]`；截图显示控件稳定堆叠 | keep；验证后已 reset viewport | TASK_EVIDENCE_ONLY |

### Manual / Platform Matrix
| 覆盖项 | 方法 | 状态 | 阻断? | 证据 |
|---|---|---|---|---|
| Browser desktop layout | Vite dev server + in-app browser viewport screenshot/DOM checks | pass | yes | `mainCount=1`，目标 regions 存在，`horizontalOverflow=false`，5 chart modules |
| Browser chart rendering | source selected + screenshots/DOM | pass | yes | waveform/spectrum/stereo/loudness/collision 均可见；5 canvas DOM labels present |
| Browser narrow layout | viewport `390x820` + DOM/screenshot | pass | yes | `horizontalOverflow=false`，`clipped=[]`，`chartCount=5` |
| Windows WebView host | 未单独启动宿主 | waived | no | 本 change 只改 Web shell；Web production build + browser smoke 覆盖前端渲染。宿主 smoke 可在后续 verify/release 节点补跑 |

### Final Hardening
- 测试 / 验证：
  - `npm test` in `src/DesktopLoudnessMeter.Web` exit=0；6 files / 29 tests passed。
  - `npm run build` in `src/DesktopLoudnessMeter.Web` exit=0；Vite production build passed。
  - `opsx changes list` exit=0；活动变更只有 `2026-07-01-webview-native-ui-parity-redraw`。
- 豁免：
  - Windows WebView host 真机 smoke 未在本轮强制执行；原因：本 change 未改 WebView2 host/C# bridge，前端行为已由 browser smoke、tests、production build 覆盖。后续 verify/release 可补宿主 smoke。
- 覆盖率：N/A；不可用原因：当前 test 命令未启用覆盖率采集；补救：组件测试、生产 build、桌面/窄屏浏览器 smoke 已覆盖本 change 的主要风险。

### Doc Drift Resolution
- doc impact 汇总：TASK_EVIDENCE_ONLY。
- 已同步产物：`tasks.md`、`evidence.md`。
- 未闭合项：无。

### 验收标准覆盖对应表
| 验收标准 (AC) | 证据 | 通过状态 |
|---|---|---|
| Browser smoke confirms desktop and narrow layouts have no obvious overlap. | desktop `horizontalOverflow=false`; mobile `horizontalOverflow=false`, `clipped=[]` | pass |
| Browser smoke confirms visible chart canvases are nonblank when data exists. | screenshots show waveform/spectrum/stereo/loudness/collision visuals; 5 canvas labels present | pass |
| `npm test` passes. | `npm test` exit=0；6 files / 29 tests | pass |
| `npm run build` passes. | `npm run build` exit=0 | pass |
| Single-change boundary holds. | `opsx changes list` shows one active change | pass |

## Implement Amendments

## Verify

## verify | 2026-07-01T20:53:34+08:00 | pass
方向：tasks.md + specs/**/*.md + design.md + Web AppShell/CSS/tests 工作树 diff -> 验证通过

### StageResult
```json
{
  "stage": "verify",
  "decision": "pass",
  "change_id": "2026-07-01-webview-native-ui-parity-redraw",
  "findings": [],
  "next": "opsx-review"
}
```

### 证据
- OpenSpec 产物读取：`.openspec.yaml`、`work.md`、`specs/change/spec.md`、`design.md`、`tasks.md`、`evidence.md`。
- 完成度：`tasks.md` 中 4/4 顶层任务已完成；R1-R6 均有 spec/design/tasks/implementation/test evidence 追踪。
- 实现 diff 读取：
  - `src/DesktopLoudnessMeter.Web/src/AppShell.tsx` - Web shell 改为 `App chrome`、`Status and controls`、`Meter console`、`Selected source detail`、`Recent and settings`，并保留既有 HostBridge command wiring。
  - `src/DesktopLoudnessMeter.Web/src/styles/app.css` - 替换 dashboard styling 为 compact native-meter layout，包含桌面和窄屏 responsive rules。
  - `src/DesktopLoudnessMeter.Web/src/AppShell.test.tsx` - 增加 native-style shell 断言和不暴露 native-only source management 命令的边界测试。
- 自动化验证：
  - `npm test` in `src/DesktopLoudnessMeter.Web` exit=0；6 files / 29 tests passed。
  - `npm run build` in `src/DesktopLoudnessMeter.Web` exit=0；Vite production build passed。
  - `opsx changes list` exit=0；活动变更只有 `2026-07-01-webview-native-ui-parity-redraw`。
- 浏览器稳定化证据：
  - desktop DOM: `mainCount=1`，目标 regions 存在，5 chart modules，`horizontalOverflow=false`。
  - desktop screenshots: waveform/spectrum/stereo/loudness/collision 可见，未见明显控件重叠。
  - narrow viewport `390x820`: `chartCount=5`、`canvasCount=5`、`horizontalOverflow=false`、`clipped=[]`，viewport 已 reset。
- Git:
  - branch: `codex/2026-07-01-host-source-reconcile-boundary`
  - HEAD: `e0498f9770eb1f4e9f7ef4cad12bc0bae960399d`
  - base: `main` at `4643c9626bd38f02570d6b9565b3bd52c331433e`
  - diff_status: unstaged tracked Web files + untracked current OpenSpec change directory, all belong to this change; no unrelated dirty files observed.
  - verification_coverage: worktree diff.
  - manual_status: verified/waived mix; browser desktop and narrow smoke verified, Windows WebView host smoke explicitly waived as non-blocking because this change only touches Web shell and no host/C# files.
  - stabilization_status: closed.
  - amendment_status: none.

allowed_failures:
- item: 无
  why_non_blocking: 无
  owner_or_followup: 无
  expiry_or_recheck: 无

### 问题
- 未发现。

### 需修正
- 无。

## Risk Review

## risk-review | 2026-07-01T20:54:44+08:00 | pass

### Review Scope Snapshot
- branch: `codex/2026-07-01-host-source-reconcile-boundary`
- HEAD: `e0498f9770eb1f4e9f7ef4cad12bc0bae960399d`
- base: `main` at `4643c9626bd38f02570d6b9565b3bd52c331433e`
- gates.verify: pass at `2026-07-01T20:53:34+08:00`
- staged: none
- unstaged tracked:
  - `src/DesktopLoudnessMeter.Web/src/AppShell.test.tsx`
  - `src/DesktopLoudnessMeter.Web/src/AppShell.tsx`
  - `src/DesktopLoudnessMeter.Web/src/styles/app.css`
- untracked:
  - `openspec/changes/2026-07-01-webview-native-ui-parity-redraw/**`
- review scope: current change's Web UI/test/CSS working tree diff plus this change's OpenSpec artifacts.
- exclusions: no C# HostBridge, C++ Core, macOS runner, packaging, installer, schema, or native source-management command changes are present.

### findings
- 无 CRITICAL。
- 无 WARNING。
- 无 SUGGESTION。

### 审查说明
- 风险线索集中在 Web DOM/CSS layout、existing HostBridge command wiring、chart module sizing、settings/recent events reachability 和 single-change boundary。
- `AppShell.tsx` 只重排现有 Web shell 与 readouts，不新增 HostBridge command name 或 payload schema。
- `AppShell.test.tsx` 覆盖新的 native-style shell landmarks，并保留 refresh/select/back/show-hidden/settings/chart command 断言。
- `app.css` 只调整 Web layout/styling；browser smoke 已覆盖桌面/窄屏无水平溢出和 chart modules 可见。
- `git diff --check` 未发现 whitespace error；仅报告仓库现有 CRLF 转换 warning。

### diff_status
- review_result: pass
- allowed_failures: none
- gates.review: pass
- next: opsx-archive 可由用户显式触发；当前未自动归档。

## Manual Checks

- [x] Browser desktop visual smoke for native-style shell and nonblank charts.
- [x] Browser narrow viewport visual smoke for stable wrapping and no obvious overlap.
- [x] Single-change boundary check: no extra OpenSpec changes created for this redraw.

## Rejected Attempts

- None yet.

## Archive

## archive | 2026-07-01T20:56:44+08:00 | pass
change: `2026-07-01-webview-native-ui-parity-redraw`
archive_path: `openspec/changes/archive/2026-07-01-webview-native-ui-parity-redraw`

### Gate Summary
- readiness: pass (`2026-07-01T20:39:07+08:00`)
- verify: pass (`2026-07-01T20:53:34+08:00`)
- review: pass (`2026-07-01T20:54:44+08:00`)
- archive: pass (`2026-07-01T20:56:44+08:00`)
- specs_count: 1 (`specs/change/spec.md`)
- tasks_status: 4/4 completed
- manual_followups: none
- allowed_failures: none
- knowledge_status: done (`knowledge-auto | 2026-07-01T20:59:18+08:00 | done`)

### Notes
- The change remained a single aggregate OpenSpec unit as requested.
- Product scope stayed limited to the Web shell, Web tests, and Web CSS.

## knowledge-auto | 2026-07-01T20:59:18+08:00 | done

- mode: auto
- source_refs:
  - change: `2026-07-01-webview-native-ui-parity-redraw`
  - evidence: `openspec/changes/archive/2026-07-01-webview-native-ui-parity-redraw/evidence.md`
- pre_knowledge_status:
  - no dirty `.aiknowledge` files before this knowledge update.
- post_knowledge_status:
  - `M .aiknowledge/codemap/index.md`
  - `M .aiknowledge/codemap/webview-shell.md`
  - `M .aiknowledge/logs/2026-07.md`
- diff_stat:
  - `.aiknowledge/codemap/index.md`: 2 lines changed
  - `.aiknowledge/codemap/webview-shell.md`: 9 lines changed
  - `.aiknowledge/logs/2026-07.md`: 6 lines changed
  - total: 3 files changed, 13 insertions(+), 4 deletions(-)
- updates:
  - refreshed the WebView shell codemap responsibility and verification date.
  - recorded that Web shell now follows compact WPF/native hierarchy while preserving existing HostBridge command wiring.
  - logged the archived single-change UI redraw in the July knowledge log.
