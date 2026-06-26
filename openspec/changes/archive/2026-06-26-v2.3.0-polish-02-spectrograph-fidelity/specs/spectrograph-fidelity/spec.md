# Spectrograph Fidelity Spec

## Requirements

### R1: Renderer Must Not Add Hidden Frequency Bleed
The rolling spectrograph renderer SHALL map each analyzer bucket to its own bitmap row without default vertical neighbor smoothing or isolated-row gap filling.

### R2: Bitmap Scaling Must Preserve Bucket Edges
The spectrograph control SHALL use deterministic non-blurring bitmap scaling for the rolling bitmap so WPF interpolation does not make narrow bands look wider.

### R3: Spectrum Analysis Must Be Independent Of True-Peak Oversampling
Changing true-peak oversampling between 2x, 4x, and 8x SHALL NOT materially move the dominant spectrum bucket for the same PCM input.

### R4: Narrowband Tones Must Localize Predictably
Single-tone diagnostics at representative audible frequencies SHALL identify a dominant Mel bucket near the expected frequency and keep the primary energy band bounded.

### R5: The Spectrum Model Must Be Documented
The repo SHALL document which blur/leakage sources were measured, which were changed, and which approximations remain intentional.
