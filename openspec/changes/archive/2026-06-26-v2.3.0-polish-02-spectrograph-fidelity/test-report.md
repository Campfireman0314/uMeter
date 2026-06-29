# Test Report

## Focused tests

- Red: `dotnet test .\tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~SpectrumRollColumnRasterizerTests` failed before the rasterizer existed.
- Red: spectrum tone tests initially showed the previous rendered/analyzer band width contract was too broad in dB-normalized bucket space.
- Green: `dotnet test .\tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~SpectrumRollColumnRasterizerTests` passed, 2 tests.
- Green: `dotnet test .\tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter "FullyQualifiedName~TruePeakAndStereoMetricTests.SpectrumDominantBucketTracksSingleToneFrequency|FullyQualifiedName~TruePeakAndStereoMetricTests.SpectrumDominantBucketIsStableAcrossTruePeakOversampling"` passed, 5 tests.
- Green: `dotnet test .\tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter "FullyQualifiedName~TruePeakAndStereoMetricTests|FullyQualifiedName~PcmMetricAnalyzerContractTests|FullyQualifiedName~SpectrumRollColumnRasterizerTests"` passed, 32 tests.
- Green: `dotnet test .\tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj` passed, 220 tests.

## Notes

- Render blur was reduced by removing vertical smoothing/gap filling and using nearest-neighbor bitmap scaling.
- Spectrum FFT input now comes from raw PCM history instead of true-peak oversampled waveform history.
- Hann leakage and center-sampled Mel buckets remain documented approximations.
