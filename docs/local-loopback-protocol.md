# Local Loopback Protocol

First added in `2026-06-17-signal-source-expansion/02-local-loopback-ingest`.

## Transport

- UDP over `127.0.0.1`.
- Default port: `38173`.
- Packets are UTF-8 JSON.
- Audio samples are little-endian IEEE float32 values encoded as base64.

This first protocol is intentionally inspectable and testable. Future plugin work can add a binary fast path while keeping the same source-provider contract.

## Common Fields

```json
{
  "protocol": "desktop-loudness-meter.loopback.v1",
  "type": "register",
  "handle": "plugin-instance-id"
}
```

`handle` is the stable sender-owned identity. Display names may change; source keys remain `loopback:<handle>`.

## Packet Types

### Register

```json
{
  "protocol": "desktop-loudness-meter.loopback.v1",
  "type": "register",
  "handle": "plugin-1",
  "displayName": "Lead Vocal",
  "sourceKind": "plugin",
  "sampleRate": 48000,
  "channels": 2
}
```

`sourceKind` may be `loopback` or `plugin`; omitted values default to `loopback`.

Senders may repeat `register` while active. Receivers treat repeated register packets with the same handle/sample format as a refresh, which lets the desktop app discover a plugin that was already running before the receiver started.

### Heartbeat

```json
{
  "protocol": "desktop-loudness-meter.loopback.v1",
  "type": "heartbeat",
  "handle": "plugin-1"
}
```

### Rename

```json
{
  "protocol": "desktop-loudness-meter.loopback.v1",
  "type": "rename",
  "handle": "plugin-1",
  "displayName": "Vocal Bus"
}
```

### Audio

```json
{
  "protocol": "desktop-loudness-meter.loopback.v1",
  "type": "audio",
  "handle": "plugin-1",
  "sequence": 42,
  "samplesBase64": "..."
}
```

Samples are interleaved according to the format from the register packet.

### Close

```json
{
  "protocol": "desktop-loudness-meter.loopback.v1",
  "type": "close",
  "handle": "plugin-1"
}
```
