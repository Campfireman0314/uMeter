# JUCE VST3 Sender

First added in `2026-06-17-signal-source-expansion/05-juce-vst3-sender`.

## What It Is

`uMeter Sender` is a native JUCE VST3 audio-effect plugin. It passes host audio through unchanged and mirrors the monitored audio into the desktop app over the existing local loopback protocol.

The first production UI stays intentionally small. It focuses on DAW compatibility, stable source identity, and real-time-safe handoff.

The plugin now has a minimal editor with:

- A source-name field. Press Enter or leave the field to rename the source in the desktop app.
- Source-name changes are sent while typing, so the desktop app follows edits immediately.
- A temporary `Mute` button. This silences the plugin output while active, but it is not saved with the project state.

## Build

From the repo root:

```powershell
cmake -S .\src\DesktopLoudnessMeter.JucePlugin -B .\build\juce-vst3 -DJUCE_DIR=C:/JUCE
cmake --build .\build\juce-vst3 --config Release --target DesktopLoudnessMeterSender_VST3
```

The default `JUCE_DIR` is already `C:/JUCE`, so the `-DJUCE_DIR=...` argument is only needed when JUCE lives somewhere else.

By default, each VST3 build copies the finished bundle to Steinberg's standard VST3 folder:

```text
C:\Program Files\Common Files\VST3
```

It also keeps a second copy in the legacy VST2-style scan folder used during local Cubase smoke tests:

```text
C:\Program Files\VSTPlugins
```

Override these with `-DDLM_VST3_COPY_DIR=<path>` and `-DDLM_EXTRA_VST3_COPY_DIR=<path>`. Disable them with `-DDLM_COPY_VST3_AFTER_BUILD=OFF` or `-DDLM_COPY_EXTRA_VST3_AFTER_BUILD=OFF`.

## Native Tests

```powershell
cmake --build .\build\juce-vst3 --config Debug --target DesktopLoudnessMeterJucePluginTests
ctest --test-dir .\build\juce-vst3 -C Debug --output-on-failure
```

These tests cover v1 packet shape, little-endian float sample encoding, bounded queue drops, plugin state restore, and the sender worker's register/heartbeat/audio/close UDP lifecycle.

## Artifact Location

For a Release build, the VST3 bundle is expected under:

```text
build\juce-vst3\DesktopLoudnessMeterSender_artefacts\Release\VST3\uMeter Sender.vst3
```

For a Debug build, replace `Release` with `Debug`.

## Install For Host Scan

The development build copies to Steinberg's standard VST3 path:

```text
C:\Program Files\Common Files\VST3
```

It also mirrors the bundle to:

```text
C:\Program Files\VSTPlugins
```

For Cubase, the standard VST3 path is the important one. `C:\Program Files\VSTPlugins` is commonly used as a VST2 scan path, so Cubase's VST2 activation does not affect whether this VST3 plugin appears.

When copying manually, copy the whole `.vst3` bundle directory, not just the inner binary:

```powershell
Copy-Item -Recurse -Force ".\build\juce-vst3\DesktopLoudnessMeterSender_artefacts\Release\VST3\uMeter Sender.vst3" "C:\Program Files\Common Files\VST3\"
```

Run PowerShell as administrator if the copy is denied.

## Smoke Test

1. Start the desktop app.
2. Build and install the VST3 bundle.
3. Open a VST3 host or DAW and rescan plugins.
4. Insert `uMeter Sender` as an audio effect on a track or bus.
5. Play audio through that track.

Expected result:

- A `Plugin` source appears in the desktop app.
- The source name defaults to the host process name plus a short stable suffix, such as `Cubase 7F3A`.
- The source animates while the host plays audio.
- Removing the plugin or closing the host removes the source from the main list. Any open detail view for that instance shows `Instance not found` until the same instance returns or the detail view is closed.

The plugin repeats its register packet while active. This means the startup order is flexible: if the plugin is already running and the desktop app starts later, the next register packet should create the source automatically.

If the desktop app shows `Plugin receiver unavailable: UDP port 38173 is already in use.`, plugin sources cannot be received. Close the other process using that port and restart the desktop app.

## Current Boundaries

- VST3 is implemented.
- AAX is not implemented in this pass. It needs its own SDK/licensing/signing/validation path.
- The protocol is still the inspectable v1 JSON/base64 UDP format. A later performance slice can add a binary fast path without changing the app's source model.
