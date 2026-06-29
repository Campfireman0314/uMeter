# 02-spectrograph-fidelity

## Goal
Make the spectrograph visually trustworthy by reducing perceived blur/leakage and documenting the real causes behind the current look.

## In Scope
- Audit the full spectrum path: analyzer sample window, oversampled display samples, FFT size, window function, magnitude normalization, Mel bucket sampling, display smoothing, bitmap scaling, and theme color mapping.
- Build synthetic spectrum tests or diagnostics for single-tone, two-tone, chirp, silence, and transient conditions.
- Determine whether blur comes from DSP leakage, bucket interpolation, render smoothing, bitmap scaling, soft-floor behavior, or refresh/history policy.
- Adjust FFT/window/bucket/render settings where evidence shows improvement without hiding real spectral behavior.
- Preserve performance modes and keep Eco/Normal tradeoffs explicit.
- Update rendering/DSP docs with the chosen spectrum model and remaining approximations.

## Out of Scope
- Source lifecycle fixes other than consuming the no-empty-frame contract from `01-source-continuity-reconfiguration`.
- Replacing the current rolling spectrograph UI with a different visualization.
- Formal acoustic measurement certification.

## Depends On
- `01-source-continuity-reconfiguration` for stable input continuity and no transient blank columns.
- Existing managed/native FFT providers and rendering architecture notes.

## Done Means
- Synthetic tests or diagnostics explain the previous blur/leakage symptoms.
- Narrowband tones render as narrow, stable bands within expected FFT/window limits.
- Render-only smoothing and soft-floor behavior are separated from analyzer data and can be reasoned about.
- Documentation records what changed, what remains approximate, and how to validate future spectrum changes.
