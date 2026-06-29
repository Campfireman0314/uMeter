# Rendering Performance Responsive Spectrum Spec

## Requirements

### R1: Waveform Buffer Refresh Must Be Throttled
**Trace**: R1

Waveform sample buffers SHALL refresh at a bounded frame interval rather than rebuilding on every audio processing block when an existing waveform buffer is available.

### R2: Waveform Rendering Must Adapt Point Count To Viewport Width
**Trace**: R2

Waveform roll rendering SHALL choose a bounded display point count from the current plot width, preserving a minimum for readability and a maximum for CPU safety.

### R3: Spectrograph Bitmap Resolution Must Adapt To Viewport Size
**Trace**: R3

Spectrograph rendering SHALL choose bounded history-column and frequency-row counts from the current plot size instead of always using a fixed bitmap resolution.

### R4: Spectrograph Row Resampling Must Preserve Peak Visibility
**Trace**: R4

When dynamic spectrograph frequency rows are fewer than analyzer buckets, row resampling SHALL use the strongest source bucket in each row range so narrow peaks remain visible.

### R5: Existing Interaction And Visual Contracts Must Remain
**Trace**: R5

Existing waveform/spectrum pan, zoom, display range, sharp bucket rendering, and deferred UI update behavior SHALL continue to work after adaptive rendering changes.
