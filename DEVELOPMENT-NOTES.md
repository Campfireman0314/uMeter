# Development Notes

## Shared Design Conventions

The desktop app, DAW sender plugins, diagnostic probes, and future mobile work should remain clients of the same metering model.

- Source identity is sender-owned and stable. Display names may change, but source keys must not.
- Local sender traffic uses the v1 loopback protocol until a later binary fast path is introduced.
- Metering semantics belong to the receiver/app side unless a platform cannot deliver raw or near-raw samples.
- Sender plugins should be pass-through effects. Monitoring must not alter host audio unless the user explicitly engages a diagnostic control such as temporary mute.
- UI preferences such as theme and Eco mode are app concerns; sender plugins only expose routing and identity controls.

## Shareable Surfaces

- `docs/local-loopback-protocol.md` defines the transport contract.
- `src/DesktopLoudnessMeter.Protocol` is the managed protocol model for desktop-side tests and probes.
- `src/DesktopLoudnessMeter.SenderKit` is the managed sender-side reference for diagnostics and future non-JUCE tools.
- `src/DesktopLoudnessMeter.JucePlugin/Source` contains the native sender implementation used by VST3 and the AAX wrapper.
- `scripts/build-juce-sender.ps1` is the shared build entry for JUCE sender formats.

## iOS Planning

The iOS direction should be planned as an alike-function client, not a direct port of the WPF desktop shell.

Open product questions:

- Is the first iOS target an observer app, a sender, or both?
- Does it need to talk to Windows desktop over LAN, or only monitor sources on-device?
- Should it prioritize AUv3/plugin-host style workflows, external interface input, or companion-display workflows?

Suggested first slice:

1. Extract DSP test vectors from desktop tests into platform-neutral fixtures.
2. Prototype a Swift receiver model for source registration, heartbeat, rename, audio, and close.
3. Build a small SwiftUI meter screen using synthetic source packets.
4. Decide whether live capture or network receiving should be the first real input path.

## AAX Sender

The first AAX pass uses the same JUCE `AudioProcessor` and native sender code as the VST3 plugin. This avoids a parallel DSP or protocol fork.

Build VST3:

```powershell
.\scripts\build-juce-sender.ps1 -Format VST3 -Configuration Release -JuceDir E:\JUCE
```

Build AAX:

```powershell
.\scripts\build-juce-sender.ps1 -Format AAX -Configuration Release -JuceDir E:\JUCE -AaxSdkDir <path-to-aax-sdk>
```

If the JUCE checkout already carries a usable bundled AAX SDK path, `-AaxSdkDir` can be omitted. A distributable AAX build still needs Avid/PACE signing, validation, and Pro Tools smoke testing.
