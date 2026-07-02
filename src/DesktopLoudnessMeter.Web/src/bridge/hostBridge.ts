export interface HostBridgeTransport {
  send(request: HostBridgeRequest): Promise<HostBridgeResponse>;
}

export type HostBridgeRequest =
  | {
      version: 1;
      id: string;
      kind: "snapshot";
    }
  | {
      version: 1;
      id: string;
      kind: "command";
      command: HostCommandName | string;
      payload?: HostCommandPayload;
    };

export type HostCommandName =
  | "refresh"
  | "openNativeUi"
  | "openGlobalSettings"
  | "beginWindowDrag"
  | "toggleWindowState"
  | "hideToTray"
  | "closeWindow"
  | "setAlwaysOnTop"
  | "setDisplayMode"
  | "setTruePeakEnabled"
  | "saveDetailLayout"
  | "loadDetailLayout"
  | "openStereoSettings"
  | "backToGlobal"
  | "resetAll"
  | "selectDetail"
  | "openPopup"
  | "resetSource"
  | "hideSource"
  | "restoreSource"
  | "moveSource"
  | "forgetSource"
  | "setShowHiddenSources"
  | "setTheme"
  | "setPerformanceMode"
  | "setRecentEventLimit"
  | "setEventOutputFolder"
  | "setTruePeakOversampling"
  | "setWaveformDisplayMode"
  | "setSpectrumDisplayMode"
  | "setDetailModuleVisibility"
  | "setDetailModuleHeight"
  | "moveDetailModule"
  | "setSpectrumDisplayRange"
  | "setLoudnessClockSettings"
  | "setCollisionSettings";

export interface HostCommandPayload {
  sourceKey?: string;
  alias?: string | null;
  showHiddenSources?: boolean;
  themeId?: string | null;
  performanceMode?: string;
  isAlwaysOnTop?: boolean;
  displayMode?: string;
  truePeakEnabled?: boolean;
  moveDirection?: string;
  maxRecentEventEntries?: number;
  eventOutputFolder?: string | null;
  sourceKind?: string;
  processId?: number | null;
  truePeakOversampling?: string;
  waveformDisplayMode?: string;
  spectrumDisplayMode?: string;
  moduleKind?: string;
  targetModuleKind?: string;
  isVisible?: boolean;
  moduleHeight?: number;
  insertAfter?: boolean;
  spectrumDisplayFloorDb?: number;
  spectrumDisplayCeilingDb?: number;
  loudnessClockMinimumDb?: number;
  loudnessClockMaximumDb?: number;
  loudnessClockWarningDb?: number;
  loudnessClockCycleSeconds?: number;
  collisionLevelFloorDb?: number;
  collisionProminenceThresholdDb?: number;
  collisionRelativeThreshold?: number;
  collisionFrequencyScale?: string;
  inlineCollisionSourceKey?: string | null;
}

export interface HostBridgeResponse {
  version: 1;
  id: string;
  kind: "snapshot" | "commandResult" | "error";
  ok: boolean;
  snapshot?: HostSnapshotDto | null;
  result?: HostCommandResultDto | null;
  error?: HostBridgeErrorDto | null;
}

export interface HostBridgeErrorDto {
  code: string;
  message: string;
  targetSourceKey?: string | null;
  detail?: string | null;
}

export interface HostCommandResultDto {
  commandKind: string;
  success: boolean;
  snapshot: HostSnapshotDto;
  error?: HostBridgeErrorDto | null;
}

export interface HostSnapshotDto {
  sources: HostSourceSnapshotDto[];
  selectedDetail?: HostDetailSnapshotDto | null;
  settings: HostSettingsSnapshotDto;
  recentEvents: HostRecentEventSnapshotDto[];
  statusText: string;
  commands: HostCommandAvailabilityDto;
}

export interface HostSourceSnapshotDto {
  sourceKey: string;
  sourceKind: string;
  sourceKindLabel: string;
  processId?: number | null;
  displayName: string;
  alias?: string | null;
  effectiveDisplayName: string;
  displayTitle: string;
  sourceIdentityText: string;
  isHidden: boolean;
  orderIndex?: number | null;
  isInstanceMissing: boolean;
  peak: number;
  peakDb: number;
  peakDbText: string;
  state: string;
  truePeakOversampling: string;
  waveformDisplayMode: string;
  spectrumDisplayMode: string;
  momentaryLufs?: number | null;
  shortTermLufs?: number | null;
  integratedLufs?: number | null;
  truePeakDb?: number | null;
  rms100MsDb?: number | null;
  stereoImageX?: number | null;
  stereoImageY?: number | null;
  stereoImagePoints?: StereoImagePointDto[] | null;
  correlation?: number | null;
  waveformSamples?: number[] | null;
  channelWaveformSamples?: number[][] | null;
  spectrumBuckets?: number[] | null;
  channelSpectrumBuckets?: number[][] | null;
  plotFrameVersion: number;
}

export interface StereoImagePointDto {
  angle: number;
  radius: number;
}

export interface HostDetailSnapshotDto {
  source: HostSourceSnapshotDto;
  modules: HostDetailModuleSnapshotDto[];
  isWaveformVisible: boolean;
  isSpectrumVisible: boolean;
  isStereoVisible: boolean;
  isLoudnessClockVisible: boolean;
  isCollisionVisible: boolean;
  collision?: HostFrequencyCollisionSnapshotDto | null;
}

export interface HostDetailModuleSnapshotDto {
  kind: string;
  title: string;
  orderIndex: number;
  isVisible: boolean;
  height: number;
}

export interface HostFrequencyCollisionSnapshotDto {
  comparisonSourceKey?: string | null;
  comparisonDisplayName?: string | null;
  levelFloorDb: number;
  prominenceThresholdDb: number;
  relativeThreshold: number;
  frequencyScale: string;
  bands: FrequencyCollisionBandDto[];
}

export interface FrequencyCollisionBandDto {
  startRatio: number;
  endRatio: number;
  intensity: number;
}

export interface HostRecentEventSnapshotDto {
  timeText: string;
  kind: string;
  title: string;
  detail: string;
  severity: string;
}

export interface HostSettingsSnapshotDto {
  schemaVersion: number;
  themeId?: string | null;
  maxRecentEventEntries?: number | null;
  eventOutputFolder?: string | null;
  loadDefaultDetailLayoutFromFile: boolean;
  defaultDetailLayoutFile?: string | null;
  showHiddenSources: boolean;
  sources: HostSourceSettingsSnapshotDto[];
}

export interface HostSourceSettingsSnapshotDto {
  sourceKey: string;
  alias?: string | null;
  isHidden: boolean;
  orderIndex?: number | null;
  truePeakOversampling?: string | null;
  waveformDisplayMode?: string | null;
  spectrumDisplayMode?: string | null;
  inlineStereoVisible?: boolean | null;
  inlineWaveformVisible?: boolean | null;
  inlineSpectrumVisible?: boolean | null;
  inlineLoudnessClockVisible?: boolean | null;
  inlineCollisionVisible?: boolean | null;
  inlineCollisionSourceKey?: string | null;
  collisionLevelFloorDb?: number | null;
  collisionProminenceThresholdDb?: number | null;
  collisionRelativeThreshold?: number | null;
  collisionFrequencyScale?: string | null;
  spectrumDisplayFloorDb?: number | null;
  spectrumDisplayCeilingDb?: number | null;
  loudnessClockMinimumDb?: number | null;
  loudnessClockMaximumDb?: number | null;
  loudnessClockWarningDb?: number | null;
  loudnessClockCycleSeconds?: number | null;
}

export interface HostCommandAvailabilityDto {
  canBackToGlobal: boolean;
  canResetAll: boolean;
  canForgetSelectedSource: boolean;
  canMoveSelectedSourceUp: boolean;
  canMoveSelectedSourceDown: boolean;
}

export class HostBridgeClient {
  public constructor(
    private readonly transport: HostBridgeTransport,
    private readonly createId: () => string = createRequestId,
  ) {}

  public async requestSnapshot(): Promise<HostSnapshotDto> {
    const response = await this.transport.send({
      version: 1,
      id: this.createId(),
      kind: "snapshot",
    });

    if (response.kind === "snapshot" && response.snapshot) {
      return response.snapshot;
    }

    throw toBridgeError(response);
  }

  public async sendCommand(
    command: HostCommandName | string,
    payload?: HostCommandPayload,
  ): Promise<HostCommandResultDto> {
    const request: HostBridgeRequest = {
      version: 1,
      id: this.createId(),
      kind: "command",
      command,
      ...(payload === undefined ? {} : { payload }),
    };
    const response = await this.transport.send(request);

    if (response.kind === "commandResult" && response.result) {
      return response.result;
    }

    throw toBridgeError(response);
  }
}

function toBridgeError(response: HostBridgeResponse): Error {
  const message = response.error?.message ?? "HostBridge request failed.";
  return new Error(message);
}

function createRequestId(): string {
  if (typeof crypto !== "undefined" && "randomUUID" in crypto) {
    return crypto.randomUUID();
  }

  return `request-${Date.now().toString(36)}`;
}
