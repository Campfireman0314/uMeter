# Spec: 2026-06-30-macos-readiness

正文默认使用中文；路径、命令、配置键、frontmatter key 和状态枚举保留原文。

## 新增需求

### 需求: macOS WKWebView runner boundary
**Trace**: R1
**Slice**: macos-runner/wkwebview-boundary

仓库必须包含一个 macOS runner 边界 artifact，表达 WKWebView 主窗口、Web asset loading、HostBridge v1 native message transport 和 JSON request/response 转发形状。

#### 场景: 查看 macOS runner 边界
- **当** 开发者检查 macOS runner artifact
- **那么** 可以看到 WKWebView host、`hostBridge` message handler、Web asset URL/loading 策略和 response posting 入口

### 需求: readiness facts and unknowns
**Trace**: R2
**Slice**: macos-readiness/facts-unknowns

readiness artifact 必须区分已证实事实、Windows-only 限制、macOS 待验证项和推断，并禁止把未执行的 macOS build/run/capture/package 描述为已通过。

#### 场景: 审查 macOS readiness
- **当** reviewer 阅读 readiness artifact
- **那么** 每个 macOS 项都有 `fact`、`unknown`、`follow-up` 或等价状态，不会出现无证据的 pass 声明

### 需求: native core macOS gate
**Trace**: R3
**Slice**: native-core/macos-gate

readiness artifact 必须给出后续 macOS 环境可执行的 native C++ Core CMake configure/build gate，并记录当前 Windows session 的 CMake/toolchain 限制。

#### 场景: 后续 macOS 环境接手验证
- **当** 开发者在 macOS 环境开始验证 native core
- **那么** 可以从 readiness artifact 找到 CMake preset/build 命令、预期产物和当前未验证原因

### 需求: capture and packaging ownership
**Trace**: R4
**Slice**: macos-platform/capture-packaging

readiness artifact 必须明确 macOS capture adapter、permissions、app bundle、native dylib packaging、signing/notarization 的 owner/follow-up，并声明这些不属于 HostBridge DTO 或 C++ Core 算法职责。

#### 场景: 规划后续 macOS capture/package
- **当** 开发者拆分后续 macOS implementation change
- **那么** 可以把 capture adapter、permission、package/signing 分配到平台 glue，而不是改写 HostBridge DTO 或 native DSP/source-ingest core

### 需求: Windows primary path preservation
**Trace**: R5
**Slice**: windows-regression/preserve-primary-path

本 change 必须保持 Windows WebView 主路径、Web frontend build/test、.NET regression 不退化。

#### 场景: 验证本 change 后的 Windows 主路径
- **当** 执行既有 Web 和 .NET regression 命令
- **那么** 测试/构建通过，或任何失败被证明与本 change 无关并记录为 allowed failure

## 修改需求

无。

## 移除需求

无。
