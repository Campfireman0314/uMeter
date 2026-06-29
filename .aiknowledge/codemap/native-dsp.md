---
status: active
created_at: 2026-06-23
created_from: opsx-explore:core-dsp-cpp-migration
last_verified_at: 2026-06-23
last_verified_by: opsx-knowledge
verification_basis: codemap-initialization
applies_to:
  - src/DesktopLoudnessMeter.Native
source_refs:
  - user:/opsx-explore core DSP replace with C++
superseded_by:
merged_from:
deprecated_reason:
---

# native-dsp

## 职责

承载当前 C++ native DSP backend：RfRapid FFT、sinc/Hann true-peak oversampling、C ABI 导出。由 WPF app 通过 `DllImport("DesktopLoudnessMeter.Native")` 调用。

## 关键文件
| 文件 | 角色 |
|------|------|
| `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeterNative.cpp` | native DLL C ABI；导出 FFT 和 true-peak create/process/reset/destroy |
| `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeter.Native.vcxproj` | MSVC x64 DLL 项目；C++17；AVX2；输出到 `x64/<Configuration>` |
| `src/DesktopLoudnessMeter.Native/RfRapid/RfRapid.h` | RfRapid API 声明 |
| `src/DesktopLoudnessMeter.Native/RfRapid/RfRapid.cpp` | RfRapid FFT/vector 实现 |
| `src/DesktopLoudnessMeter.Native/RfRapid/RfCPUID.*` | CPU feature detection |
| `src/DesktopLoudnessMeter.App/DesktopLoudnessMeter.App.csproj` | 将 native DLL 复制到 app 输出目录 |

## 隐式约束
- 现有 native 项目只配置 x64 Debug/Release，非跨平台构建；若目标是平台无关核心，需要迁移/新增 CMake 或多平台构建策略。
- 导出 ABI 当前围绕 RfRapid 命名，不是完整 DSP engine API。
- `DlmRfRapidTruePeakProcessF64` 需要 caller 提供完整输出缓冲；C# 当前按 `frameCount * oversampling` 预分配。
- native true-peak 算法与 managed `TruePeakEstimator` 同构；可作为迁移参照和等价性基准。
