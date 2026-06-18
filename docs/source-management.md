# Source Management

uMeter can now show several source types in one list:

- `Plugin`: a sender using the plugin-style loopback sender kit or probe.
- `Loopback`: another local sender using the localhost loopback protocol.
- `WASAPI`: a Windows render-session source discovered through Core Audio.

## Ordering

Rows are sorted in a deterministic order:

1. Plugin
2. Loopback
3. WASAPI
4. Unknown

Within each group, rows sort by display name and then by source key. This keeps the list stable even when the underlying providers return sources in a different order.

## Names And Handles

`DisplayName` stays provider-owned. If two current rows use the same display name, the UI adds a display-only qualifier such as `Shared Name - Plugin lead-vocal-bus`. The provider name is not rewritten.

Stable source identity still comes from source keys:

- Plugin/loopback: `loopback:<sender-handle>`
- WASAPI: provider/session key plus process id where available

The detail header and tray pop-up include source identity text so same-name rows can be distinguished.

## Reconnect Behavior

While the app is running, per-source analysis preferences are remembered by source key:

- true-peak oversampling ratio
- waveform overall/channel mode
- spectrum overall/channel mode

If a plugin or loopback source disappears and later returns with the same handle, the row gets those analysis preferences back. New sources without source-specific preferences use the current global true-peak oversampling selection.

## Deferred UI Work

Persistent aliases, hidden-source rules, and source visibility management are intentionally deferred. They need explicit UI affordances so users can see and undo what has been renamed or hidden.
