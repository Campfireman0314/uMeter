# Test Report

## Focused tests

- Red: hide/order view-model tests failed at compile before workspace/view-model properties existed.
- Green: `dotnet test .\tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter "FullyQualifiedName~HiddenSourcesAreFilteredUntilShowHiddenIsEnabled|FullyQualifiedName~MoveCommandsPersistSourceOrderAcrossRefreshAndReconnect"` passed, 2 tests.
- Green: `dotnet test .\tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~AudioSessionMeterViewModelTests.AliasEditCommandsCommitTrimmedAliasOrCancel` passed, 1 test.
- Green: `dotnet test .\tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --filter FullyQualifiedName~MainViewModelAdvancedControlsTests` passed, 43 tests.
- Green: `dotnet test .\tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj` passed, 225 tests.

## Notes

- Hidden rows are filtered from the displayed collection unless `ShowHiddenSources` is enabled.
- User ordering persists via source-key order indices and survives WASAPI hibernation/reconnect.
- Row title double-click editing replaces the separate global-row alias editor.
