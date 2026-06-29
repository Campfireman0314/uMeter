# Codemap

## 模块

| 模块 | 状态 | 最近复核 | 职责 | 入口 |
|------|------|----------|------|------|
| [audio-analysis](audio-analysis.md) | active | 2026-06-23 | PCM 指标计算、响度、真峰值、波形/频谱/声像派生数据 | `src/DesktopLoudnessMeter.App/Audio/Analysis/PcmMetricAnalyzer.cs` |
| [native-dsp](native-dsp.md) | active | 2026-06-23 | C++ FFT 与真峰值 native backend，当前由 C# P/Invoke 调用 | `src/DesktopLoudnessMeter.Native/DesktopLoudnessMeterNative.cpp` |
| [audio-capture-sources](audio-capture-sources.md) | active | 2026-06-23 | WASAPI/本地 loopback/plugin packet 到 `PcmMetricAnalyzer` 的采集和指标汇入 | `src/DesktopLoudnessMeter.App/Audio` |

## 链路

| 链路 | 状态 | 最近复核 | 涉及模块 | 说明 |
|------|------|----------|----------|------|
| [pcm-metric-flow](chains/pcm-metric-flow.md) | active | 2026-06-23 | audio-capture-sources, audio-analysis, native-dsp | PCM block 进入分析器后生成 UI snapshot 的关键链路 |
