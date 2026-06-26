# Test Report

## Verification

- Green: `dotnet test .\tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj` passed, 231 tests.
- Green: `dotnet build .\desktop-loudness-meter.sln` passed with 0 warnings and 0 errors.
- Green: `.\scripts\build-juce-sender.ps1 -Format VST3 -Configuration Debug -RunTests` passed, 1 CTest target.
- Green: `.\build-installer.ps1 -Configuration Release` passed.

## Artifact

- `artifacts\uMeter.Setup.exe`
- `artifacts\uMeter-Setup-2.3.0.exe`
- SHA256: `5C960D7F41969773738928B3B6145B8CED35F9278D838246B64D9995FE977263`

## Payload Check

- `artifacts\app-publish\uMeter.exe` exists.
- `artifacts\app-publish\Plugins\VST3\uMeter Sender.vst3` exists.
