# 03-plugin-sender-prototypes

## Goal
Prototype DAW plugin senders that route plugin audio to the app through the local loopback ingest protocol.

## In Scope
- Build a minimal VST3 sender prototype or bridge that registers with the desktop app and streams audio over the local protocol.
- Define the plugin instance identity model, including persistent unique handle, user-visible name, host/session context, and rename messages.
- Validate real-time-safety boundaries so the plugin audio callback does not block on UI, network, allocation-heavy, or logging work.
- Investigate AAX sender feasibility and document the exact SDK/build constraints before committing to a distributable AAX binary.
- Add a DAW-free diagnostic sender path if full plugin validation is not available on the current machine.

## Out of Scope
- Full plugin UI design.
- Public plugin installer.
- Remote-network streaming.
- Reworking the app analyzer algorithms.

## Depends On
- `02-local-loopback-ingest` for the stable protocol and receiver.
- Availability of the relevant VST3/AAX SDKs and build tools.

## Done Means
- At least one plugin-style sender path can create a source in the app, stream audio, and rename the displayed source through a stable unique handle.
- Real-time callback risks are documented and guarded by buffering or worker-thread handoff.
- AAX either has a validated build path or a clear blocked/conditional note.
