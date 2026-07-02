# Tasks: 2026-06-30-macos-readiness

## Default Constraints

- Forbidden Files:
  - `.openspec.yaml`
  - `tasks.md`
  - `evidence.md`
- Shared Artifacts:
  - `.openspec.yaml` - controller 串行更新
  - `tasks.md` - controller 串行更新
  - `evidence.md` - controller 串行更新
- Worker Return Format: `DONE | DONE_WITH_CONCERNS | NEEDS_CONTEXT | BLOCKED`

## 1. Establish macOS Runner Boundary

- [x] 1.1 [direct] 新增 WKWebView runner skeleton

  **需求追踪**: [R1] -> [U1]
  **执行方式**: [direct]
  **Allowed Files**:
  - `src/DesktopLoudnessMeter.MacHost/**` - macOS runner skeleton
  - `docs/platform/macos-readiness.md` - runner boundary notes

  **验收标准**:
  - [x] `src/DesktopLoudnessMeter.MacHost/**` 包含 WKWebView host skeleton。
  - [x] skeleton 表达 `hostBridge` message handler、JSON request input 和 JSON response output。
  - [x] skeleton 不被 Windows `.sln` 默认编译，不影响 Windows app/test build。

  **验证命令 / 方法**:
  - `rg -n "WKWebView|WKScriptMessageHandler|hostBridge|postMessage|evaluateJavaScript" src/DesktopLoudnessMeter.MacHost docs/platform/macos-readiness.md`，预期：命中 runner boundary。
  - `git diff -- desktop-loudness-meter.sln src/DesktopLoudnessMeter.App src/DesktopLoudnessMeter.Web`，预期：无非预期 Windows build/runtime 改动。

  **Evidence Required**:
  - 新增文件列表和静态 grep 摘要。
  - 说明 skeleton 是 readiness boundary，不是 macOS pass。

  **Done When**:
  - 后续 macOS 实现可从 skeleton 看出 WKWebView/HostBridge transport 形状。

  **依赖**: 无

## 2. Build macOS Readiness Matrix

- [x] 2.1 [stabilization-first] 固化 macOS native/capture/package readiness

  **需求追踪**: [R2, R3, R4] -> [U2, U3, U4]
  **执行方式**: [stabilization-first]
  **Allowed Files**:
  - `docs/platform/macos-readiness.md` - readiness matrix and follow-up gates

  **验收标准**:
  - [x] readiness matrix 区分已证实事实、Windows-only 限制、macOS 待验证项和推断。
  - [x] native C++ Core macOS gate 包含 `cmake --preset macos-release` 和 build command 的后续执行入口。
  - [x] capture adapter、permissions、bundle/native dylib payload、signing/notarization 均有 owner/follow-up。
  - [→] [manual] macOS CMake configure/build 真机验证 | 目标：后续 `macos-runner-implementation` 或 macOS CI/toolchain change
  - [→] [manual] WKWebView runner 真机 smoke | 目标：后续 `macos-runner-implementation`
  - [→] [manual] macOS capture permission/capability smoke | 目标：后续 `macos-capture-adapter`
  - [→] [manual] macOS packaging/signing/notarization smoke | 目标：后续 `macos-packaging`

  **验证命令 / 方法**:
  - `cmake --version`，预期：当前 Windows session 若失败，记录为后续 macOS/toolchain gate。
  - `rg -n "fact|unknown|follow-up|cmake --preset macos-release|ScreenCaptureKit|CoreAudio|notarization|dylib|WKWebView" docs/platform/macos-readiness.md`，预期：命中 readiness matrix。

  **Evidence Required**:
  - Stabilization Rationale
  - Manual / Platform Matrix
  - 当前 Windows 环境 CMake/toolchain 检测结果
  - Final Hardening summary

  **Stabilization Rationale**:
  - 不确定性来源：真实 macOS build/run/capture/package 需要 macOS toolchain、权限和签名环境；当前 Windows session 无法执行。
  - 为什么不能先写目标行为测试：WKWebView、CMake macOS preset、capture permission 和 notarization 都依赖 Apple 平台环境；Windows 静态测试无法证明运行通过。
  - 退出条件：所有 macOS manual/platform 项转为 `[→]` 且包含目标；当前可验证的静态边界和 Windows 回归通过。

  **Manual / Platform Matrix**:
  | 覆盖项 | 方法 | 阻断? | 期望 |
  |-------|------|-------|------|
  | macOS native CMake build | macOS 上执行 configure/build preset | no | 后续 follow-up 验证 |
  | WKWebView runner smoke | macOS 上启动 runner 并交换 HostBridge message | no | 后续 follow-up 验证 |
  | macOS capture capability | macOS 上验证 capture/permission 方案 | no | 后续 follow-up 验证 |
  | macOS packaging/signing | macOS 上验证 `.app` payload/sign/notarize | no | 后续 follow-up 验证 |

  **Final Hardening Plan**:
  - 本轮用静态 matrix 和 follow-up 目标关闭 readiness，不宣称 macOS pass。
  - doc impact 分类：`TASK_EVIDENCE_ONLY`

  **Done When**:
  - macOS 未验证事项全部有明确后续目标，且无裸 `[ ]` manual gate。

  **依赖**: Task 1.1

## 3. Preserve Current Windows/WebView Path

- [x] 3.1 [stabilization-first] 验证 Windows/WebView 主路径不退化

  **需求追踪**: [R5] -> [U5]
  **执行方式**: [stabilization-first]
  **Allowed Files**:
  - `src/DesktopLoudnessMeter.MacHost/**` - only if static isolation needs metadata adjustment
  - `docs/platform/macos-readiness.md` - evidence notes

  **验收标准**:
  - [x] Web frontend tests pass。
  - [x] Web frontend production build pass。
  - [x] .NET regression tests pass。
  - [x] 新增 macOS skeleton 不改变 Windows app/WebView/native production files。

  **验证命令 / 方法**:
  - `npm test` in `src/DesktopLoudnessMeter.Web`，预期：通过。
  - `npm run build` in `src/DesktopLoudnessMeter.Web`，预期：通过。
  - `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj`，预期：通过。
  - `git diff --stat -- src/DesktopLoudnessMeter.App src/DesktopLoudnessMeter.Web src/DesktopLoudnessMeter.Native`，预期：无非预期生产改动。

  **Evidence Required**:
  - Stabilization Rationale
  - command output summary
  - Doc Drift Resolution

  **Stabilization Rationale**:
  - 不确定性来源：新增跨平台 skeleton 不能被 Windows build/test/publish 主路径误消费。
  - 为什么不能先写目标行为测试：本 task 是跨环境守护；最终证据来自真实 build/test 命令和 diff scope，而不是单一 unit test。
  - 退出条件：Web/.NET regression 通过，diff scope 证明 Windows production files 未被误改。

  **Manual / Platform Matrix**:
  | 覆盖项 | 方法 | 阻断? | 期望 |
  |-------|------|-------|------|
  | Windows Web regression | Web tests/build + .NET tests | yes | pass |
  | macOS runner runtime | 后续 macOS smoke | no | `[→]` follow-up |

  **Final Hardening Plan**:
  - 保留 Web/.NET regression evidence；manual macOS runtime 不作为本轮 Windows pass 条件。
  - doc impact 分类：`TASK_EVIDENCE_ONLY`

  **Done When**:
  - 当前 Windows 主路径可验证未退化，macOS runtime 未验证项有 follow-up。

  **依赖**: Task 1.1, Task 2.1
