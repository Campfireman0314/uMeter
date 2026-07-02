# Work Packet: 2026-06-30-macos-readiness

正文默认使用中文；路径、命令、配置键、frontmatter key 和状态枚举保留原文。

## 元数据

- profile: strict-change
- source_type: feature
- status: drafting
- created: 2026-06-30

## 切分结论

### 总判断
- 结论：KEEP_ONE_CHANGE
- 理由：本 change 的交付单元是“macOS readiness boundary”：把 WKWebView runner、HostBridge native message transport 等价层、native C++ Core CMake/macOS build gate、capture adapter 可行性和 packaging/notarization 风险收敛成一组可审查的边界文件、探针和平台矩阵。它不是完整 macOS 产品落地，也不是单纯文档；若拆成 runner/native/capture/package 多个 change，会在没有 macOS 运行证据前产生多个互相等待的半成品。
- OpenSpec 下限检查：通过。该 change 涉及跨平台 runner 边界、native build 验证入口、平台 capture/permission 风险、packaging 约束和后续验证矩阵，具备可评审、可验证、可归档的工程闭环。

### 能力簇
| 簇 | 用户价值 | 主要模块 | 独立交付性 | 备注 |
|----|----------|----------|------------|------|
| WKWebView runner boundary | 后续 macOS 主窗口有明确宿主入口和 bridge transport 形状 | `src/DesktopLoudnessMeter.MacHost/**`, Web frontend bridge contract | 中 | 本轮只交付 skeleton/边界，不宣称运行通过 |
| Native core macOS build gate | C++ Core 可以被后续 macOS runner 验证，不再停留在口头预留 | `src/DesktopLoudnessMeter.Native/CMakeLists.txt`, `CMakePresets.json` | 中 | 当前 Windows 无 CMake/macOS toolchain，仅做静态和 follow-up gate |
| Capture/permission readiness | 明确 Windows WASAPI 与 macOS capture adapter 不同职责，避免 Host/Core 混淆 | docs/OpenSpec | 中 | 不实现 CoreAudio/ScreenCaptureKit |
| Packaging readiness | 明确 app bundle、Web assets、native dylib 和 notarization 的后续检查点 | docs/OpenSpec | 中 | 不交付签名/installer |

### 推荐切分 / Top-Level Changes
1. `2026-06-30-macos-readiness`
   - 范围：新增 macOS runner 边界骨架与 readiness matrix；记录 WKWebView HostBridge transport 等价层、native CMake/macOS gate、capture adapter/permission 和 packaging/notarization 验证入口。
   - 不包含：完整 macOS app bundle、CoreAudio/ScreenCaptureKit capture adapter、notarization、真实 macOS smoke pass。
   - 依赖：`ui-runtime-architecture-replan`、`webview-windows-app-shell`、`native-core-build-boundary`、`native-source-ingest-core`、`webview-packaging-startup-tray`、`webview-chart-parity`、`wpf-retirement` 均已归档。
   - 建议 profile：`strict-change`
   - work 关注点：事实/推断隔离，macOS 平台 gate 不得被 Windows 静态检查冒充。

### 执行顺序
- 当前进入：`opsx-plan 2026-06-30-macos-readiness`
- readiness 通过后：实现 runner skeleton/readiness 文档/探针 -> verify -> review -> archive。

## 意图

Windows WebView2 主路径已经稳定，WPF 普通入口也已退休到内部 oracle seam。总目标的剩余关键风险集中在 macOS：仓库目前没有 macOS runner、没有 WKWebView host 文件、主 app 是 `net8.0-windows` + WPF/WebView2/WinForms/NAudio，native C++ Core 只有 CMake macOS preset 作为 reserved boundary。这个 change 要把 macOS 落地前的真实边界和验证入口变成仓库资产，避免后续继续把“预留”误读为“已支持”。

## 范围

### 范围内
- 新增 macOS runner/WKWebView host 边界骨架，表达主窗口、Web asset loading、native message channel、HostBridge v1 request/response JSON 传输形状。
- 新增 macOS readiness 文档或等价 artifact，覆盖 runner、native core、capture adapter、packaging/notarization 的事实、推断、阻塞项和后续验证命令。
- 更新 solution/docs/codemap 所需范围，使 macOS skeleton 不影响 Windows build/test/publish 主路径。
- 验证当前 Windows 主路径不回退：Web build、.NET tests、native/source-set 静态检查继续通过。
- 将 macOS 真机/工具链检查作为 `[→]` manual/platform follow-up，目标指向后续 macOS 环境验证，不宣称当前 Windows session pass。

### 范围外
- 不实现可发布的 macOS `.app` bundle。
- 不实现 CoreAudio、ScreenCaptureKit、AudioUnit、system extension、权限弹窗或 loopback capture adapter。
- 不实现 macOS notarization/signing/DMG/pkg。
- 不把 Windows WPF/WebView2 app 改成跨平台 .NET UI。
- 不修改 HostBridge v1 DTO 语义，不修改 Web frontend runtime contract。
- 不修改 native DSP/source ingest 算法。

## 依赖

- `openspec/changes/archive/2026-06-29-ui-runtime-architecture-replan/`
- `openspec/changes/archive/2026-06-29-webview-windows-app-shell/`
- `openspec/changes/archive/2026-06-29-native-core-build-boundary/`
- `openspec/changes/archive/2026-06-29-native-source-ingest-core/`
- `openspec/changes/archive/2026-06-30-webview-chart-parity/`
- `openspec/changes/archive/2026-06-30-wpf-retirement/`
- `.aiknowledge/codemap/ui-runtime-architecture.md`
- `.aiknowledge/codemap/webview-shell.md`
- `.aiknowledge/codemap/native-dsp.md`
- `.aiknowledge/codemap/audio-capture-sources.md`
- `.aiknowledge/pitfalls/misc/native-consumers-must-import-copy-targets.md`

## 成功标准

- 仓库中存在清晰的 macOS runner/WKWebView boundary artifact，后续开发者可以看到 HostBridge v1 transport 与 Windows WebView2 transport 的对应关系。
- readiness matrix 明确列出 macOS native core build、capture adapter、permissions、Web assets、native dylib packaging、signing/notarization 的当前证据和缺口。
- 当前 Windows 环境没有伪造 macOS pass；所有 macOS 真机/toolchain 项要么有真实证据，要么以 `[→]` 指向后续 macOS 环境验证。
- Windows WebView 主路径、Web frontend build/test、.NET regression 不被新增 skeleton 破坏。
- codemap 在归档后记录 macOS readiness 的边界和未完成项。

## 需求 / 验收摘要

- [R1] 仓库必须新增或更新一个 macOS WKWebView runner 边界 artifact，表达 HostBridge v1 native message transport 的等价层。
- [R2] macOS readiness 必须区分已证实事实、Windows-only 限制、macOS 待验证项和推断，不得宣称未验证的 macOS 支持。
- [R3] native C++ Core macOS build gate 必须可被后续 macOS 环境执行，并说明当前 Windows session 的 CMake/toolchain限制。
- [R4] capture adapter 与 permission/package/notarization 风险必须有明确 owner/follow-up，不得混入 HostBridge 或 C++ Core 职责。
- [R5] 本 change 必须保持 Windows WebView 主路径和现有 tests/build 不退化。

## 方案摘要

采用“边界 skeleton + readiness matrix + Windows 回归守护”的方案。`src/DesktopLoudnessMeter.MacHost/**` 用最小 Swift/WKWebView skeleton 表达未来 macOS runner 结构和 `window.webkit.messageHandlers.hostBridge.postMessage` transport；`docs/platform/macos-readiness.md` 记录 native core、capture、packaging、permission 的平台验证矩阵。实现阶段只允许静态/文档/骨架级变更，任何需要真实 macOS runner 运行、capture、签名或 package 的事项都写成后续平台 gate。

## 文件 / Ownership

### 允许修改
- `src/DesktopLoudnessMeter.MacHost/**` - macOS WKWebView runner boundary skeleton。
- `docs/platform/macos-readiness.md` - readiness matrix、事实/推断、后续验证命令。
- `desktop-loudness-meter.sln` - 如需加入 solution folder 或文档项，仅允许非破坏性更新。
- `README.md`, `docs/current-status.md` - 如需同步 macOS readiness 状态，仅允许状态说明。
- `.aiknowledge/codemap/**`, `.aiknowledge/logs/**` - 归档后知识维护。
- `openspec/changes/2026-06-30-macos-readiness/**` - 当前 change artifacts。

### 禁止修改
- `src/DesktopLoudnessMeter.App/**`，除非 readiness 发现 Windows 回归需要 within-scope 小修并回到 plan 修订。
- `src/DesktopLoudnessMeter.Web/**`，除非仅为 WKWebView transport detection 的独立后续 change；本 change 不改 Web runtime。
- `src/DesktopLoudnessMeter.Native/**`，除非只是补充非行为性 macOS CMake metadata 并有静态验证；默认不改 native core。
- `src/DesktopLoudnessMeter.Protocol/**`
- `src/DesktopLoudnessMeter.JucePlugin/**`
- `src/DesktopLoudnessMeter.Setup/**`
- workspace/settings schema migration 文件。

## Git / Workspace

- Workspace Strategy: `current-worktree`
- Workspace Path: `E:\gitlab\uMeter`
- Workspace Branch: `codex/2026-06-30-macos-readiness`
- Base Branch: `codex/2026-06-30-wpf-retirement` at `6254167abc773e8fc67130b732ca0a4075b3f25a`
- Merge Target: `codex/2026-06-30-wpf-retirement`
- Workspace Rationale:
  - dirty 分类：创建 change 前工作树干净；创建后 dirty 仅包含 `openspec/changes/2026-06-30-macos-readiness/**`，归属本 change。无明确无关 dirty file；无归属不明 dirty file。
  - 链式判断：本 change 基于已归档 `wpf-retirement` 分支继续推进总体架构，`base_head=6254167abc773e8fc67130b732ca0a4075b3f25a`，属于链式 change。
  - isolated 触发：任务涉及跨平台边界和新增 skeleton，但当前为单 agent 串行、工作树干净、无需并行依赖安装或独立回滚目录；采用 dedicated branch 上的 `current-worktree`。若实现阶段需要真实 macOS toolchain 或并行平台验证，停止当前 Windows session implementation，回到 plan 切换 isolated/macOS workspace。
  - 结论：`current-worktree` on dedicated branch。

## 验证计划

- `opsx changes list`：预期仅本 change 活动，阶段清楚。
- `opsx changes resolve 2026-06-30-macos-readiness`：预期解析当前 change root。
- `rg --files | rg -i "(mac|wkwebview|darwin|cmake|plist|entitlements)"`：预期能看到新增 macOS boundary 与既有 CMake preset。
- `rg -n "WKWebView|hostBridge|messageHandlers|macOS|CMake|notarization|ScreenCaptureKit|CoreAudio" src/DesktopLoudnessMeter.MacHost docs/platform openspec/changes/2026-06-30-macos-readiness`：预期命中 readiness 边界。
- `npm test` in `src/DesktopLoudnessMeter.Web`：预期通过。
- `npm run build` in `src/DesktopLoudnessMeter.Web`：预期通过。
- `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj`：预期通过。
- `cmake --version`：当前 Windows session 预期失败；必须记录为后续 macOS/toolchain follow-up，不能算 pass。

## 稳定化 / 人工平台不确定性

- 是否需要 `[stabilization-first]`：yes。
- 不确定性来源：当前环境是 Windows；仓库没有 macOS runner；CMake 不在 PATH；没有 macOS signing/notarization/capture 权限环境；WKWebView skeleton 无法在本机编译运行。
- 人工 / 平台覆盖：macOS native build、WKWebView runner smoke、capture permission smoke、bundle/package/notarization 均为后续 macOS 环境 gate；本轮只能通过静态 artifact、Windows 回归和明确 follow-up 关闭。
- 最终硬化策略：行为稳定后保留 static boundary tests/grep、Windows regression、Web build/test；manual-only macOS 项必须转为 `[→]` 并带目标 change/issue，不得留 `[ ]` 归档。

## 回退 / 升级

- 回退方式：删除新增 `src/DesktopLoudnessMeter.MacHost/**` 和 `docs/platform/macos-readiness.md`，保留 Windows 主路径不变。
- 升级条件：如果实现 skeleton 需要修改 HostBridge DTO、Web runtime、native ABI 或 Windows app startup，则停止 implementation，回 plan 拆出独立 change。
- 升级条件：如果获得真实 macOS 环境并能执行 Swift/CMake/capture/package，则可在本 change 内补平台 evidence；若需要大量修复，拆成 `macos-runner-implementation` 或 `macos-capture-adapter`。

## Bugfix Root Cause

none（feature）。

## Rejected Attempts

- 不采用“直接声明 macOS supported”：当前没有 macOS runner 或真机/toolchain evidence。
- 不采用“一次实现完整 macOS app/capture/package”：会跨 WKWebView runner、native build、capture permissions、signing/notarization 多个独立高风险能力簇，无法在当前 Windows session 一次 verify。
- 不采用“只写 docs 不放 runner skeleton”：总目标要求 macOS runner 边界清晰，纯文档不足以给后续实现提供可落地入口。

## Readiness Notes

- readiness 需检查 R1-R5 是否在 specs/design/tasks 中完整 trace。
- readiness 需确认 `[stabilization-first]` task 对 macOS 平台 gate 有明确 `[→]` 或 `[~]` 处理方式。
- readiness 需确认本 change 不改 HostBridge DTO/Web runtime/native ABI。
- readiness 需确认工作区策略已解析为 dedicated branch 上的 `current-worktree`。
