## verify | 2026-06-18T22:38:10.1441802+08:00 | pass
Direction: tasks.md + specs/**/*.md + design.md + code files -> verification passed
Fixes: none

Checks:
- Completeness: all task acceptance criteria are checked.
- Correctness: R1-R6 are represented by CMake/plugin code, sender lifecycle code, tests, and docs.
- Consistency: implementation follows the native JUCE VST3 sender design; AAX remains deferred.
- Test records: test-first tasks have red/green/refactor records.
- Manual validation: no manual acceptance criteria in this slice.

Commands:
- `cmake -S .\src\DesktopLoudnessMeter.JucePlugin -B .\build\juce-vst3 -DJUCE_DIR=C:/JUCE`
- `cmake --build .\build\juce-vst3 --config Debug --target DesktopLoudnessMeterJucePluginTests`
- `ctest --test-dir .\build\juce-vst3 -C Debug --output-on-failure`
- `cmake --build .\build\juce-vst3 --config Release --target DesktopLoudnessMeterSender_VST3`
- `dotnet test --no-build`
- `dotnet format --verify-no-changes --no-restore`
