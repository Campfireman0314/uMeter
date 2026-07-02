import { describe, expect, it } from "vitest";
import type { HostDetailSnapshotDto, HostSourceSnapshotDto } from "../bridge/hostBridge";
import {
  buildChartData,
  resetChartRuntimeStateForTests,
} from "./ChartModuleCanvas";
import {
  createCollisionBands,
  createLoudnessClockHistory,
  createSpectrumBars,
  createStereoPoints,
  createWaveformPath,
} from "./chartGeometry";

describe("chart geometry utilities", () => {
  it("maps waveform samples into stable normalized points", () => {
    expect(createWaveformPath([-2, -0.5, 0, 0.5, 2])).toEqual([
      { x: 0, y: 1 },
      { x: 0.25, y: 0.75 },
      { x: 0.5, y: 0.5 },
      { x: 0.75, y: 0.25 },
      { x: 1, y: 0 },
    ]);
    expect(createWaveformPath([])).toEqual([]);
  });

  it("maps spectrum buckets with floor and ceiling settings", () => {
    expect(createSpectrumBars([0, 0.9, 1], -100, -20)).toEqual([
      { x: 0, width: 1 / 3, value: 0 },
      { x: 1 / 3, width: 1 / 3, value: 1 },
      { x: 2 / 3, width: 1 / 3, value: 1 },
    ]);
  });

  it("maps stereo polar points into bounded cartesian points", () => {
    expect(createStereoPoints([{ angle: 0, radius: 2 }])).toEqual([
      { x: 1, y: 0.5, intensity: 1 },
    ]);
  });

  it("keeps collision bands ordered and bounded", () => {
    expect(
      createCollisionBands([
        { startRatio: -0.25, endRatio: 0.25, intensity: 2 },
        { startRatio: 0.75, endRatio: 0.5, intensity: 0.5 },
      ]),
    ).toEqual([
      { startRatio: 0, endRatio: 0.25, intensity: 1 },
      { startRatio: 0.5, endRatio: 0.75, intensity: 0.5 },
    ]);
  });

  it("resets loudness clock history when the selected source changes", () => {
    const history = createLoudnessClockHistory(30);

    history.add("plugin:lead", 1, -18);
    history.add("plugin:lead", 2, -12);
    expect(history.pointsFor("plugin:lead")).toHaveLength(2);

    history.add("plugin:keys", 1, -24);

    expect(history.pointsFor("plugin:lead")).toEqual([]);
    expect(history.pointsFor("plugin:keys")).toEqual([{ frame: 1, loudness: -24, phase: 0 }]);
  });

  it("uses source settings when building spectrum chart data", () => {
    const data = buildChartData("spectrum", detailWithSource({ spectrumBuckets: [0.52] }), {
      sourceKey: "plugin:lead",
      isHidden: false,
      spectrumDisplayFloorDb: -120,
      spectrumDisplayCeilingDb: 0,
    });

    expect(data).toMatchObject({
      kind: "spectrum",
      buckets: [0.52],
      floorDb: -120,
      ceilingDb: 0,
    });
  });

  it("rebuilds loudness clock history when cycle settings change", () => {
    resetChartRuntimeStateForTests();

    buildChartData("loudnessClock", detailWithSource({ plotFrameVersion: 1, momentaryLufs: -18 }), {
      sourceKey: "plugin:lead",
      isHidden: false,
      loudnessClockCycleSeconds: 30,
    });
    const data = buildChartData("loudnessClock", detailWithSource({ plotFrameVersion: 2, momentaryLufs: -12 }), {
      sourceKey: "plugin:lead",
      isHidden: false,
      loudnessClockCycleSeconds: 10,
    });

    expect(data).toMatchObject({
      kind: "loudnessClock",
      points: [{ frame: 2, loudness: -12, phase: 0 }],
    });
  });
});

function detailWithSource(overrides: Partial<HostSourceSnapshotDto> = {}): HostDetailSnapshotDto {
  const source = { ...baseSource(), ...overrides };
  return {
    source,
    modules: [],
    isWaveformVisible: true,
    isSpectrumVisible: true,
    isStereoVisible: true,
    isLoudnessClockVisible: true,
    isCollisionVisible: true,
    collision: null,
  };
}

function baseSource(): HostSourceSnapshotDto {
  return {
    sourceKey: "plugin:lead",
    sourceKind: "plugin",
    sourceKindLabel: "Plugin sender",
    processId: null,
    displayName: "Lead",
    alias: null,
    effectiveDisplayName: "Lead",
    displayTitle: "Lead",
    sourceIdentityText: "plugin:lead",
    isHidden: false,
    orderIndex: 0,
    isInstanceMissing: false,
    peak: 0.5,
    peakDb: -6,
    peakDbText: "-6.0 dB",
    state: "Active",
    truePeakOversampling: "fourX",
    waveformDisplayMode: "overall",
    spectrumDisplayMode: "overall",
    momentaryLufs: -18,
    shortTermLufs: -16,
    integratedLufs: null,
    truePeakDb: -1,
    rms100MsDb: -20,
    stereoImageX: 0,
    stereoImageY: 0,
    stereoImagePoints: [],
    correlation: null,
    waveformSamples: [],
    channelWaveformSamples: [],
    spectrumBuckets: [],
    channelSpectrumBuckets: [],
    plotFrameVersion: 1,
  };
}
