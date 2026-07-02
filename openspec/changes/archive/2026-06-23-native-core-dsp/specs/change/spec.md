# Spec：2026-06-23-native-core-dsp

正文默认使用中文；路径、命令、配置键名、frontmatter key 和状态枚举保持原文。

## 新增需求

### 需求:C++ PCM analyzer 主实现
**Trace**: R1
**Slice**: native-dsp/pcm-analyzer-core
系统必须在 C++ native engine 中实现平台无关 PCM 指标分析，并覆盖当前 `Audio/Analysis` 中可复用算法范围。

#### 场景:处理 stereo PCM block
- **当** C# wrapper 向 native analyzer 传入 interleaved stereo float samples
- **那么** native analyzer 产出 momentary LUFS、short-term LUFS、integrated LUFS、true peak、RMS、correlation、stereo image、waveform、spectrum 和 channel 级可选数据

#### 场景:处理 mono PCM block
- **当** C# wrapper 向 native analyzer 传入 mono float samples
- **那么** native analyzer 产出单声道适用指标，并对 stereo-only 指标返回 unavailable 或等价空结果

#### 场景:按职责拆分 native analyzer 内部模块
- **当** native PCM analyzer 继续演进或排查问题
- **那么** loudness、true peak、recent window/waveform/spectrum 与 analyzer orchestration 必须保持职责分离，并通过 `DlmPcmAnalyzer` 统一编排，而不是重新回退为单个超大实现文件

### 需求:C# analyzer API 兼容
**Trace**: R2
**Slice**: app-audio-analysis/native-wrapper
`PcmMetricAnalyzer` 必须保持现有构造、`Process`、`Reset`、settings 方法和 `Snapshot` 消费语义，禁止要求 UI、ViewModel 或 capture service 改变调用方式。

#### 场景:现有调用方处理 block
- **当** `ProcessLoopbackCaptureWorker` 或 `LocalLoopbackSignalSourceService` 调用 `PcmMetricAnalyzer.Process(float[])`
- **那么** 调用方无需知道 native handle、buffer ownership 或 P/Invoke 细节

#### 场景:读取 snapshot
- **当** 上层服务读取 `PcmMetricAnalyzer.Snapshot`
- **那么** 返回的 `PcmMetricsSnapshot` 字段名称、null/unavailable 语义和 list 数据形状保持兼容

### 需求:native ABI 生命周期
**Trace**: R3
**Slice**: native-dsp/c-abi
native ABI 必须提供 analyzer create、process、reset、settings 更新、snapshot 读取和 destroy 能力，并禁止把 native-owned 临时指针暴露给长期持有的 C# 集合。

#### 场景:重置 analyzer
- **当** C# wrapper 调用 `Reset`
- **那么** native analyzer 清空 loudness、true peak、recent window、waveform/spectrum cache 和 frame version 状态

#### 场景:更新分析设置
- **当** C# wrapper 更新 true-peak oversampling、channel waveform、channel spectrum 或 performance mode
- **那么** native analyzer 按现有语义更新或重建内部状态，且 snapshot 不包含跨设置污染的旧数据

#### 场景:读取数组数据
- **当** C# wrapper 读取 waveform、channel waveform、spectrum 或 channel spectrum
- **那么** wrapper 获得由 C# 管理的稳定数组副本，native 不要求 C# 长期持有 native 内存

#### 场景:native ABI 作为薄门面
- **当** `DesktopLoudnessMeterNative.cpp` 暴露导出函数
- **那么** 导出层只负责 ABI 转发与参数校验，核心状态机和算法编排留在 `DlmPcmAnalyzer` 及其职责子模块中

### 需求:算法等价与回归保护
**Trace**: R4
**Slice**: tests/audio-analysis-equivalence
迁移必须包含自动化测试，验证 native-backed analyzer 与迁移前 managed 行为在代表性信号上等价或在明确容差内。

#### 场景:响度和 true peak 等价
- **当** 测试输入 sine、silence、quiet tail、stereo phase/correlation 信号
- **那么** loudness、true peak、RMS、correlation 和 stereo image 与基线结果满足测试定义的容差

#### 场景:频谱和波形形状稳定
- **当** 测试启用 waveform、channel waveform、spectrum 和 channel spectrum
- **那么** 输出长度、null/unavailable 条件、bucket 数量、frame version 和刷新节流符合现有 contract

### 需求:平台边界保持
**Trace**: R5
**Slice**: platform-boundary/csharp-host
Windows 平台相关 capture、UI、source/session 生命周期必须保留在 C# 宿主层，禁止迁入 C++ DSP core。

#### 场景:Windows capture 输入
- **当** process loopback 或 local loopback 收到 PCM 数据
- **那么** C# 仍负责采集、线程/源生命周期、packet/source 管理和 sample format 转换，C++ 只接收平台无关 interleaved PCM samples

#### 场景:未来多平台宿主复用
- **当** 后续非 Windows 宿主需要复用算法
- **那么** 它可以通过 C ABI 或等价 wrapper 复用 C++ analyzer，而不依赖 WPF、NAudio 或 Windows interop

## 修改需求

无。

## 移除需求

无。
