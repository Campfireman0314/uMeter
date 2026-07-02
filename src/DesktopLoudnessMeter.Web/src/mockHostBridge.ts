import type { HostBridgeLike } from "./state/hostState";
import type { HostCommandResultDto, HostSnapshotDto } from "./bridge/hostBridge";

export function createMockHostBridge(): HostBridgeLike {
  let snapshot = createMockSnapshot();

  return {
    async requestSnapshot() {
      return snapshot;
    },
    async sendCommand(command, payload) {
      if (command === "setShowHiddenSources") {
        snapshot = {
          ...snapshot,
          settings: {
            ...snapshot.settings,
            showHiddenSources: Boolean(payload?.showHiddenSources),
          },
        };
      }

      if (command === "setTheme") {
        snapshot = {
          ...snapshot,
          settings: {
            ...snapshot.settings,
            themeId: payload?.themeId ?? null,
          },
        };
      }

      if (
        command === "setAlwaysOnTop" ||
        command === "openGlobalSettings" ||
        command === "beginWindowDrag" ||
        command === "toggleWindowState" ||
        command === "hideToTray" ||
        command === "closeWindow" ||
        command === "setDisplayMode" ||
        command === "setTruePeakEnabled" ||
        command === "saveDetailLayout" ||
        command === "loadDetailLayout" ||
        command === "openStereoSettings" ||
        command === "openPopup"
      ) {
        snapshot = { ...snapshot };
      }

      if (command === "setRecentEventLimit" && typeof payload?.maxRecentEventEntries === "number") {
        snapshot = {
          ...snapshot,
          settings: {
            ...snapshot.settings,
            maxRecentEventEntries: payload.maxRecentEventEntries,
          },
        };
      }

      if (command === "setEventOutputFolder") {
        snapshot = {
          ...snapshot,
          settings: {
            ...snapshot.settings,
            eventOutputFolder: payload?.eventOutputFolder ?? null,
          },
        };
      }

      if (command === "setSourceAlias" && payload?.sourceKey) {
        const alias = normalizeAlias(payload.alias);
        updateSource(payload.sourceKey, (source) => {
          const effectiveDisplayName = alias ?? source.displayName;
          return {
            ...source,
            alias,
            effectiveDisplayName,
            displayTitle: effectiveDisplayName,
          };
        });
      }

      if (command === "selectDetail" && payload?.sourceKey) {
        const source = snapshot.sources.find((item) => item.sourceKey === payload.sourceKey);
        snapshot = {
          ...snapshot,
          selectedDetail: source
            ? {
                source,
                modules: createChartModules(),
                isWaveformVisible: true,
                isSpectrumVisible: true,
                isStereoVisible: true,
                isLoudnessClockVisible: true,
                isCollisionVisible: true,
                collision: {
                  comparisonSourceKey: "plugin:keys",
                  comparisonDisplayName: "Keys",
                  levelFloorDb: -72,
                  prominenceThresholdDb: 8,
                  relativeThreshold: 0.6,
                  frequencyScale: "logarithmic",
                  bands: [
                    { startRatio: 0.24, endRatio: 0.36, intensity: 0.8 },
                    { startRatio: 0.58, endRatio: 0.68, intensity: 0.55 },
                  ],
                },
              }
            : null,
        };
      }

      if (command === "hideSource" && payload?.sourceKey) {
        updateSource(payload.sourceKey, (source) => ({ ...source, isHidden: true }));
      }

      if (command === "restoreSource" && payload?.sourceKey) {
        updateSource(payload.sourceKey, (source) => ({ ...source, isHidden: false }));
      }

      if (command === "moveSource" && payload?.sourceKey && payload.moveDirection) {
        const index = snapshot.sources.findIndex((source) => source.sourceKey === payload.sourceKey);
        const offset = payload.moveDirection === "up" ? -1 : 1;
        const targetIndex = index + offset;
        if (index >= 0 && targetIndex >= 0 && targetIndex < snapshot.sources.length) {
          const sources = [...snapshot.sources];
          const [source] = sources.splice(index, 1);
          sources.splice(targetIndex, 0, source);
          snapshot = {
            ...snapshot,
            sources: sources.map((source, orderIndex) => ({ ...source, orderIndex })),
          };
        }
      }

      if (command === "setWaveformDisplayMode" && payload?.sourceKey && payload.waveformDisplayMode) {
        updateSource(payload.sourceKey, (source) => ({
          ...source,
          waveformDisplayMode: String(payload.waveformDisplayMode),
        }));
      }

      if (command === "setSpectrumDisplayMode" && payload?.sourceKey && payload.spectrumDisplayMode) {
        updateSource(payload.sourceKey, (source) => ({
          ...source,
          spectrumDisplayMode: String(payload.spectrumDisplayMode),
        }));
      }

      if (
        command === "setSpectrumDisplayRange" &&
        payload?.sourceKey &&
        typeof payload.spectrumDisplayFloorDb === "number" &&
        typeof payload.spectrumDisplayCeilingDb === "number"
      ) {
        updateSourceSettings(payload.sourceKey, (source) => ({
          ...source,
          spectrumDisplayFloorDb: payload.spectrumDisplayFloorDb,
          spectrumDisplayCeilingDb: payload.spectrumDisplayCeilingDb,
        }));
      }

      if (
        command === "setLoudnessClockSettings" &&
        payload?.sourceKey &&
        typeof payload.loudnessClockMinimumDb === "number" &&
        typeof payload.loudnessClockMaximumDb === "number" &&
        typeof payload.loudnessClockWarningDb === "number" &&
        typeof payload.loudnessClockCycleSeconds === "number"
      ) {
        updateSourceSettings(payload.sourceKey, (source) => ({
          ...source,
          loudnessClockMinimumDb: payload.loudnessClockMinimumDb,
          loudnessClockMaximumDb: payload.loudnessClockMaximumDb,
          loudnessClockWarningDb: payload.loudnessClockWarningDb,
          loudnessClockCycleSeconds: payload.loudnessClockCycleSeconds,
        }));
      }

      if (
        command === "setCollisionSettings" &&
        payload?.sourceKey &&
        typeof payload.collisionLevelFloorDb === "number" &&
        typeof payload.collisionProminenceThresholdDb === "number" &&
        typeof payload.collisionRelativeThreshold === "number" &&
        payload.collisionFrequencyScale
      ) {
        updateSourceSettings(payload.sourceKey, (source) => ({
          ...source,
          collisionLevelFloorDb: payload.collisionLevelFloorDb,
          collisionProminenceThresholdDb: payload.collisionProminenceThresholdDb,
          collisionRelativeThreshold: payload.collisionRelativeThreshold,
          collisionFrequencyScale: String(payload.collisionFrequencyScale),
          inlineCollisionSourceKey: payload.inlineCollisionSourceKey ?? null,
        }));
        snapshot = {
          ...snapshot,
          selectedDetail: snapshot.selectedDetail
            ? {
                ...snapshot.selectedDetail,
                collision: snapshot.selectedDetail.collision
                  ? {
                      ...snapshot.selectedDetail.collision,
                      comparisonSourceKey: payload.inlineCollisionSourceKey ?? null,
                      comparisonDisplayName:
                        snapshot.sources.find((source) => source.sourceKey === payload.inlineCollisionSourceKey)
                          ?.displayTitle ?? null,
                      levelFloorDb: payload.collisionLevelFloorDb,
                      prominenceThresholdDb: payload.collisionProminenceThresholdDb,
                      relativeThreshold: payload.collisionRelativeThreshold,
                      frequencyScale: String(payload.collisionFrequencyScale),
                    }
                  : null,
              }
            : null,
        };
      }

      if (command === "setDetailModuleVisibility" && payload?.moduleKind && typeof payload.isVisible === "boolean") {
        snapshot = {
          ...snapshot,
          selectedDetail: snapshot.selectedDetail
            ? {
                ...snapshot.selectedDetail,
                modules: snapshot.selectedDetail.modules.map((module) =>
                  module.kind === payload.moduleKind ? { ...module, isVisible: Boolean(payload.isVisible) } : module,
                ),
              }
            : null,
        };
      }

      if (command === "setDetailModuleHeight" && payload?.moduleKind && typeof payload.moduleHeight === "number") {
        const moduleHeight = payload.moduleHeight;
        snapshot = {
          ...snapshot,
          selectedDetail: snapshot.selectedDetail
            ? {
                ...snapshot.selectedDetail,
                modules: snapshot.selectedDetail.modules.map((module) =>
                  module.kind === payload.moduleKind ? { ...module, height: normalizeModuleHeight(module.kind, moduleHeight) } : module,
                ),
              }
            : null,
        };
      }

      if (
        command === "moveDetailModule" &&
        payload?.moduleKind &&
        payload.targetModuleKind &&
        typeof payload.insertAfter === "boolean"
      ) {
        snapshot = {
          ...snapshot,
          selectedDetail: snapshot.selectedDetail
            ? {
                ...snapshot.selectedDetail,
                modules: moveModuleToDropPosition(
                  snapshot.selectedDetail.modules,
                  String(payload.moduleKind),
                  String(payload.targetModuleKind),
                  payload.insertAfter,
                ),
              }
            : null,
        };
      }

      if (command === "backToGlobal") {
        snapshot = {
          ...snapshot,
          selectedDetail: null,
        };
      }

      if (command === "forgetSource" && payload?.sourceKey) {
        snapshot = {
          ...snapshot,
          selectedDetail: null,
          settings: {
            ...snapshot.settings,
            sources: snapshot.settings.sources.filter((source) => source.sourceKey !== payload.sourceKey),
          },
        };
      }

      return {
        commandKind: command,
        success: true,
        snapshot,
        error: null,
      } satisfies HostCommandResultDto;
    },
  };

  function updateSource(
    sourceKey: string,
    update: (source: HostSnapshotDto["sources"][number]) => HostSnapshotDto["sources"][number],
  ): void {
    const sources = snapshot.sources.map((source) => (source.sourceKey === sourceKey ? update(source) : source));
    const selectedDetail = snapshot.selectedDetail;
    snapshot = {
      ...snapshot,
      sources,
      selectedDetail: selectedDetail
        ? {
            ...selectedDetail,
            source: sources.find((source) => source.sourceKey === selectedDetail.source.sourceKey) ?? selectedDetail.source,
          }
        : null,
    };
  }

  function updateSourceSettings(
    sourceKey: string,
    update: (source: HostSnapshotDto["settings"]["sources"][number]) => HostSnapshotDto["settings"]["sources"][number],
  ): void {
    const existing = snapshot.settings.sources.find((source) => source.sourceKey === sourceKey) ?? {
      sourceKey,
      alias: null,
      isHidden: false,
    };
    const sources = [
      ...snapshot.settings.sources.filter((source) => source.sourceKey !== sourceKey),
      update(existing),
    ];
    snapshot = {
      ...snapshot,
      settings: {
        ...snapshot.settings,
        sources,
      },
    };
  }
}

function normalizeAlias(alias: string | null | undefined): string | null {
  const next = typeof alias === "string" ? alias.trim() : "";
  return next.length ? next : null;
}

function createChartModules() {
  return [
    { kind: "waveform", title: "Waveform", orderIndex: 0, isVisible: true, height: 140 },
    { kind: "spectrum", title: "Spectrum", orderIndex: 1, isVisible: true, height: 140 },
    { kind: "stereo", title: "Stereo", orderIndex: 2, isVisible: true, height: 140 },
    { kind: "loudnessClock", title: "Loudness Clock", orderIndex: 3, isVisible: true, height: 140 },
    { kind: "collision", title: "Frequency Collision", orderIndex: 4, isVisible: true, height: 140 },
  ];
}

function normalizeModuleHeight(kind: string, height: number): number {
  if (kind === "collision") {
    return 112;
  }

  const minimum = kind === "stereo" ? 320 : 160;
  return Math.max(minimum, Math.min(1200, Math.round(height)));
}

function moveModuleToDropPosition(
  modules: NonNullable<HostSnapshotDto["selectedDetail"]>["modules"],
  moduleKind: string,
  targetModuleKind: string,
  insertAfter: boolean,
): NonNullable<HostSnapshotDto["selectedDetail"]>["modules"] {
  const nextModules = [...modules];
  const sourceIndex = nextModules.findIndex((module) => module.kind === moduleKind);
  if (sourceIndex < 0 || moduleKind === targetModuleKind) {
    return modules;
  }

  const [source] = nextModules.splice(sourceIndex, 1);
  const targetIndex = nextModules.findIndex((module) => module.kind === targetModuleKind);
  if (targetIndex < 0) {
    return modules;
  }

  nextModules.splice(targetIndex + (insertAfter ? 1 : 0), 0, source);
  return nextModules.map((module, orderIndex) => ({ ...module, orderIndex }));
}

function createMockSnapshot(): HostSnapshotDto {
  return {
    sources: [
      {
        sourceKey: "plugin:lead",
        sourceKind: "plugin",
        sourceKindLabel: "Plugin sender",
        processId: null,
        displayName: "Lead",
        alias: "Lead Bus",
        effectiveDisplayName: "Lead Bus",
        displayTitle: "Lead Bus",
        sourceIdentityText: "plugin:lead",
        isHidden: false,
        orderIndex: 0,
        isInstanceMissing: false,
        peak: 0.54,
        peakDb: -5.4,
        peakDbText: "-5.4 dB",
        state: "Active",
        truePeakOversampling: "fourX",
        waveformDisplayMode: "overall",
        spectrumDisplayMode: "overall",
        momentaryLufs: -18.2,
        shortTermLufs: -16.7,
        integratedLufs: -17.4,
        truePeakDb: -1.1,
        rms100MsDb: -21.5,
        stereoImageX: 0.18,
        stereoImageY: -0.1,
        stereoImagePoints: [
          { angle: 0.1, radius: 0.62 },
          { angle: 1.8, radius: 0.42 },
          { angle: 4.4, radius: 0.35 },
        ],
        waveformSamples: [-0.3, -0.1, 0.28, 0.64, 0.18, -0.42, -0.12, 0.22],
        channelWaveformSamples: [
          [-0.2, 0.1, 0.55, 0.18, -0.34, -0.08],
          [0.22, -0.18, 0.38, 0.12, -0.46, 0.16],
        ],
        spectrumBuckets: [0.06, 0.2, 0.42, 0.74, 0.55, 0.32, 0.18, 0.1],
        channelSpectrumBuckets: [
          [0.06, 0.18, 0.4, 0.7, 0.48, 0.28],
          [0.08, 0.24, 0.45, 0.6, 0.52, 0.2],
        ],
        plotFrameVersion: 1,
      },
      {
        sourceKey: "plugin:hidden",
        sourceKind: "plugin",
        sourceKindLabel: "Plugin sender",
        processId: null,
        displayName: "Hidden Reference",
        alias: null,
        effectiveDisplayName: "Hidden Reference",
        displayTitle: "Hidden Reference",
        sourceIdentityText: "plugin:hidden",
        isHidden: true,
        orderIndex: 1,
        isInstanceMissing: false,
        peak: 0.2,
        peakDb: -14,
        peakDbText: "-14.0 dB",
        state: "Hidden",
        truePeakOversampling: "fourX",
        waveformDisplayMode: "overall",
        spectrumDisplayMode: "overall",
        plotFrameVersion: 1,
      },
    ],
    selectedDetail: null,
    settings: {
      schemaVersion: 1,
      themeId: "milk-white",
      maxRecentEventEntries: 6,
      eventOutputFolder: null,
      loadDefaultDetailLayoutFromFile: false,
      defaultDetailLayoutFile: null,
      showHiddenSources: false,
      sources: [],
    },
    recentEvents: [
      {
        timeText: "12:34:56",
        kind: "source",
        title: "Source appeared",
        detail: "Lead Bus",
        severity: "normal",
      },
    ],
    statusText: "1 source tracked",
    commands: {
      canBackToGlobal: false,
      canResetAll: true,
      canForgetSelectedSource: true,
      canMoveSelectedSourceUp: true,
      canMoveSelectedSourceDown: true,
    },
  };
}
