# Plugin Sender Prototype

This pass adds the sender-side foundation for future VST3/AAX routing. It does not yet produce a distributable VST3 or AAX plugin binary.

## What Exists Now

- `DesktopLoudnessMeter.Protocol`: shared localhost packet model and v1 JSON/base64 encoder/decoder.
- `DesktopLoudnessMeter.SenderKit`: reusable sender core for plugin-style wrappers.
- `DesktopLoudnessMeter.SenderProbe`: DAW-free executable that behaves like a plugin source from the app's point of view.

The durable wrapper boundary is:

```text
plugin process callback -> LocalLoopbackSender.TryEnqueueAudio(...) -> bounded pooled queue -> sender worker -> localhost protocol -> desktop app receiver
```

The audio handoff returns `false` when the queue is full, and the UDP/network work is kept off the caller's audio path.

## Smoke Test

Start the desktop app first:

```powershell
dotnet run --project .\src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj
```

Then run the sender probe from another PowerShell window:

```powershell
dotnet run --project .\src\DesktopLoudnessMeter.SenderProbe -- --name "Sender Probe" --seconds 10 --frequency 440 --amplitude 0.25 --rename-after "Sender Probe Renamed"
```

Expected result:

- A `Plugin` source row appears in the app.
- The row animates while the probe streams.
- Around halfway through, the display name changes to `Sender Probe Renamed`.
- When the probe exits, the source closes and disappears.

Useful options:

```powershell
dotnet run --project .\src\DesktopLoudnessMeter.SenderProbe -- --help
```

Common knobs:

- `--handle <id>`: explicit stable sender handle. If omitted, the probe persists one under `%LOCALAPPDATA%\DesktopLoudnessMeter\sender-probe.identity`.
- `--sample-rate <hz>`: default `48000`.
- `--channels <1|2>`: default `2`.
- `--frames <count>`: default `480`. Keep this modest while v1 uses JSON/base64 UDP packets.
- `--port <number>`: default `38173`.

## VST3/AAX Status

The follow-up `05-juce-vst3-sender` pass adds the first real JUCE VST3 plugin sender.

See `docs/juce-vst3-sender.md` for build, install, and host smoke-test steps.

Current local observations:

- CMake is available at `C:\Program Files\CMake\bin\cmake.exe`.
- Visual Studio 18 2026 Community is available to CMake in this shell.
- `C:\JUCE` is available and provides the JUCE CMake plugin helpers.
- Steinberg/Avid program folders exist under `C:\Program Files`, but no standalone AAX SDK checkout was found in the checked repo/SDK paths.

The VST3 plugin mirrors the sender-kit boundary in native C++: create/load a stable instance handle from plugin state, start a worker sender when the processor is activated, enqueue audio from the audio callback, and close during teardown.

Future AAX work is conditional on the AAX SDK, licensing, signing, validation, and the required build chain. Until that is available, this repo should treat AAX as a documented integration boundary rather than a build artifact.
