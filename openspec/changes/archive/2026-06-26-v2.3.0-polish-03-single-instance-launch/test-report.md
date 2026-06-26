# Test Report

## Focused tests

- Green: `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~SingleInstanceCoordinatorTests` passed in subagent verification, 4 tests.
- Green: `dotnet test .\tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~SingleInstanceCoordinatorTests` passed after review, 5 tests.
- Green: `dotnet test .\tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj` passed, 220 tests.

## Notes

- Startup ownership is acquired before duplicate source receivers, settings, tray icon, or windows can be constructed.
- Secondary launch requests activation through a per-user/session named pipe and exits with code 0.
- Primary activation restores minimized/tray-hidden state through existing window show logic.
