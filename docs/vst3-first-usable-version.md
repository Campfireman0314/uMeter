# VST3 First Usable Version

Pinned on 2026-06-18.

This checkpoint marks the first usable VST3-compatible signal-source path for Desktop Loudness Meter. It is not a numbered desktop release by itself; it is the milestone where a real DAW plugin can feed the app through the same source model as WASAPI sessions and localhost sender probes.

## Scope

- JUCE-based `Desktop Loudness Meter Sender` VST3 audio-effect plugin.
- Pass-through audio by default, so inserting the plugin does not alter the host signal unless temporary mute is enabled.
- Local UDP loopback sender that mirrors plugin audio into the desktop app.
- Stable plugin source handle persisted in plugin state.
- Host-based generated source display names, such as `Cubase 7F3A`, persisted in plugin state and sent to the app through rename packets.
- Minimal plugin editor with a rounded live-update source-name field and temporary mute button.
- Sender worker thread with a bounded audio queue, keeping the audio callback free of socket work.
- Periodic register packets while the plugin is active, so the desktop app can discover an already-running plugin after the app starts.
- VST3 build copies to `C:\Program Files\Common Files\VST3` and mirrors to `C:\Program Files\VSTPlugins` for local smoke-test convenience.
- Desktop app surfaces a warning if the fixed plugin receiver port, UDP `38173`, cannot be bound.

## Smoke Test

1. Build the VST3 plugin or use the installed bundle.
2. Open Cubase or another VST3 host.
3. Insert `Desktop Loudness Meter Sender` on a track or bus.
4. Start Desktop Loudness Meter before or after the plugin is active.
5. Play audio through the host track.
6. Rename the source in the plugin editor and confirm the desktop app row follows it.
7. Toggle temporary mute and confirm the plugin output and metered signal drop while mute is active.

Expected result: a `Plugin` source appears in the desktop app, animates while host audio plays, survives flexible host/app startup order, and closes when the host/plugin is removed.

## Latest Verification

- `dotnet test` passed with 143 tests.
- JUCE native CTest passed with 1 test.
- Release VST3 build succeeded.
- Release VST3 bundle copied to both standard and local smoke-test plugin folders.
- `dotnet format --verify-no-changes --no-restore` passed.

## Known Boundaries

- AAX is not implemented yet.
- The loopback protocol is still the inspectable v1 JSON/base64 UDP format.
- The plugin receiver uses fixed UDP port `38173`; if another process owns it, plugin sources cannot be received.
- Temporary mute intentionally is not saved with plugin state.
- Temporary mute currently silences plugin output, not only the metering send.
- The plugin UI is intentionally minimal and ready for a dedicated polish pass.
- Source-name editing is persisted as plugin state, but is not exposed as host automation.

## Next Slice

- Polish the plugin editor visual design.
- Add sender connection/status feedback inside the plugin editor.
- Consider a protocol performance pass, probably a binary packet path, after the UI direction settles.
- Keep AAX as a separate integration slice because SDK access, validation, signing, and distribution constraints are different from VST3.
