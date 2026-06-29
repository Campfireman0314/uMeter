# Test Report

## Focused tests

- Red: `dotnet test .\tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~AudioSessionMeterViewModelTests` failed the new hibernation/warmup graph-retention tests before implementation.
- Green: `dotnet test .\tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~AudioSessionMeterViewModelTests` passed, 30 tests.
- Green: `dotnet test .\tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~LocalLoopbackSignalSourceServiceTests` passed, 4 tests.
- Red: `.\scripts\build-juce-sender.ps1 -Format VST3 -Configuration Debug -RunTests` failed the new same-handle restart close-order test before implementation.
- Green: `.\scripts\build-juce-sender.ps1 -Format VST3 -Configuration Debug -RunTests` passed, 1 CTest target.
- Green: `dotnet test .\tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter "FullyQualifiedName~AudioSessionMeterViewModelTests|FullyQualifiedName~LocalLoopbackSignalSourceServiceTests"` passed, 34 tests.

## Notes

- Graph buffers are preserved during temporary handle loss and analyzer warmup frames.
- Same-handle loopback re-registration reconfigures an existing source state rather than removing it.
- JUCE sender same-handle restarts suppress close packets; explicit stop still closes.
