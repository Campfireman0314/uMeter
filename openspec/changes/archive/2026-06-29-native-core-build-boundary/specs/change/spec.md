# Spec: 2026-06-29-native-core-build-boundary

正文默认使用中文；路径、命令、配置键名、frontmatter key 和状态枚举保持原文。

## 新增需求

### 需求: Native Core Build Entry
**Trace**: R1
**Slice**: native-core/build-entry
`DesktopLoudnessMeter.Native` 必须提供一个平台中立的 native core build entry，能够表达当前 analyzer source set。

#### 场景: Source Set Covers Analyzer Core
- **当** 开发者查看 native core build entry
- **那么** 它列出 `DesktopLoudnessMeterNative.cpp`、`DlmPcmAnalyzer.Core.cpp`、`DlmPcmAnalyzer.Loudness.cpp`、`DlmPcmAnalyzer.RecentWindow.cpp`、`DlmPcmAnalyzer.TruePeak.cpp` 和 `RfRapid` 编译单元

#### 场景: Build Entry Uses Portable Defaults
- **当** 开发者配置 native core build entry
- **那么** 它要求 C++17，并对 Windows dynamic library 输出保留 `DesktopLoudnessMeter.Native` target name

### 需求: Windows DLL Output Parity
**Trace**: R2
**Slice**: native-core/windows-output
Windows native build 必须继续产出当前 C# P/Invoke 可加载的 `DesktopLoudnessMeter.Native.dll`。

#### 场景: MSVC Release Build
- **当** Windows 开发者运行 native Release build helper 或等效 VS MSBuild 命令
- **那么** `src/DesktopLoudnessMeter.Native/x64/Release/DesktopLoudnessMeter.Native.dll` 可生成

#### 场景: Debug Configuration Remains Addressable
- **当** managed project 使用 Debug configuration 构建
- **那么** native deployment rule 优先查找 Debug 输出，并在缺失时按既有策略回退到 Release 输出

### 需求: Managed Native Deployment Rule
**Trace**: R3
**Slice**: native-core/deployment
每个会运行 native P/Invoke 路径的 managed executable/test/benchmark 必须有显式 native DLL deployment rule。

#### 场景: App Output Receives Native DLL
- **当** `src/DesktopLoudnessMeter.App/DesktopLoudnessMeter.App.csproj` 构建完成
- **那么** app 输出目录包含 `DesktopLoudnessMeter.Native.dll`

#### 场景: Test Output Receives Native DLL
- **当** `tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj` 构建完成
- **那么** test 输出目录包含 `DesktopLoudnessMeter.Native.dll`

#### 场景: Benchmark Output Receives Native DLL
- **当** `tests/DesktopLoudnessMeter.Benchmarks/DesktopLoudnessMeter.Benchmarks.csproj` 运行 native benchmark path
- **那么** benchmark 进程可以加载 `DesktopLoudnessMeter.Native.dll`

### 需求: ABI Export Compatibility
**Trace**: R4
**Slice**: native-core/abi-compat
当前 `NativePcmAnalyzer.cs` 使用的 native exports 必须保持兼容。

#### 场景: Current Entry Points Remain Exported
- **当** Windows native DLL 构建完成
- **那么** DLL 继续导出 `DlmPcmAnalyzerCreateExport`、`DlmPcmAnalyzerDestroyExport`、`DlmPcmAnalyzerResetExport`、`DlmPcmAnalyzerProcessF32Export`、`DlmPcmAnalyzerGetSnapshotExport`、settings exports 和 copy exports

#### 场景: Wrapper Smoke Uses Existing Signatures
- **当** `NativePcmAnalyzer` smoke tests 执行
- **那么** create/process/snapshot/copy/destroy 调用不需要修改 P/Invoke signature

### 需求: macOS Reserved Boundary
**Trace**: R5
**Slice**: native-core/macos-boundary
本 change 必须把 macOS native build 记录为 reserved boundary，禁止声称 macOS runtime 已完成。

#### 场景: No macOS Runtime Claim
- **当** 开发者阅读 native build 文档或 evidence
- **那么** 文档明确 Windows 是本轮已验证主路径，macOS runner/capture/package 留给 `macos-readiness`

#### 场景: Future macOS Verification Has Entry
- **当** 后续 `macos-readiness` 开始
- **那么** 它可以引用本 change 的 native core source set 和 build entry 作为验证入口

## 修改需求

无。

## 移除需求

无。
