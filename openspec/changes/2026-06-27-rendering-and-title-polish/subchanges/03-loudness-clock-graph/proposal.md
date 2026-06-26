# 03-loudness-clock-graph

## Goal
Add an optional loudness clock graph that plots recent momentary loudness around a circle, with radius mapped from a user-defined minimum and maximum loudness range.

## In Scope
- Add a WPF loudness clock visualization for detail surfaces.
- Plot momentary loudness as a circular history where angle advances around the circle and radius represents loudness.
- Hide samples below the configured minimum loudness.
- Clamp samples at or above the configured maximum loudness to the outer radius.
- Add user controls for minimum and maximum loudness.
- Persist clock visibility and range settings per source, for inline detail and pop-up detail.
- Add focused tests for loudness-to-radius mapping, plot ticking, and workspace persistence.

## Out of Scope
- Replacing existing level meters, waveform roll, spectrograph, collision view, or stereo image view.
- Changing loudness analysis math.
- Exporting clock graph data.
- Adding long-term session statistics beyond the graph's bounded display history.

## Depends On
- Existing per-source detail preference infrastructure.
- Existing momentary loudness values from `AudioSessionMeterViewModel`.

## Done Means
- Users can show/hide the loudness clock graph in inline detail and pop-up detail.
- Users can set minimum and maximum loudness for the clock graph.
- The graph plots momentary loudness around a circle with below-minimum values omitted and maximum values mapped to the outer radius.
- Settings persist per source.
- Focused tests, full managed tests, and solution build pass.
