# 02-local-loopback-ingest

## Goal
Add a local loopback network ingest provider so external tools or plugins can register signal sources and stream PCM into the existing analyzer.

## In Scope
- Define protocol v1 for localhost source registration, heartbeat, rename/update, audio-format negotiation, PCM frames, and source shutdown.
- Implement a local receiver provider that creates and updates signal sources from protocol messages.
- Feed received PCM into the same analyzer pipeline used by WASAPI/process-loopback sources.
- Manage unique remote source handles independently from display names.
- Add timeout and recovery behavior for sources that stop sending heartbeat or audio.
- Provide a small local test sender or test harness for repeatable verification without a DAW plugin.

## Out of Scope
- VST3 or AAX plugin packaging.
- Non-local network discovery.
- Encrypted or authenticated transport.
- Cross-machine routing.
- Replacing the existing WASAPI source provider.

## Depends On
- `01-signal-source-foundation` for provider, identity, lifecycle, and UI contracts.

## Done Means
- A localhost sender can create a named non-WASAPI source, stream PCM, rename it, disconnect, and reconnect without losing the source identity contract.
- The source appears in the main list and detail views with clear type labeling and normal metering.
- Synthetic tests cover registration, duplicate handle handling, rename, heartbeat timeout, and analyzer feed.
