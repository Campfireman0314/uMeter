import {
  useEffect,
  useMemo,
  useRef,
  useState,
  type DragEvent as ReactDragEvent,
  type KeyboardEvent as ReactKeyboardEvent,
  type MouseEvent as ReactMouseEvent,
  type PointerEvent as ReactPointerEvent,
  type WheelEvent as ReactWheelEvent,
} from "react";
import type {
  HostDetailModuleSnapshotDto,
  HostDetailSnapshotDto,
  HostSourceSettingsSnapshotDto,
  HostSourceSnapshotDto,
} from "../bridge/hostBridge";
import { NativeComboBox } from "../ui/NativeComboBox";
import { focusFirstNativeMenuItem, handleNativeMenuKeyboard } from "../ui/nativeMenuKeyboard";
import {
  createCollisionBands,
  createLoudnessClockHistory,
  createStereoPoints,
  createWaveformPath,
  type CollisionBandGeometry,
  type Point,
  type StereoPoint,
} from "./chartGeometry";

interface ChartModuleCanvasProps {
  detail: HostDetailSnapshotDto;
  module: HostDetailModuleSnapshotDto;
  sources: HostSourceSnapshotDto[];
  sourceSettings?: HostSourceSettingsSnapshotDto | null;
  onWaveformModeChange: (mode: string) => void;
  onSpectrumModeChange: (mode: string) => void;
  onVisibilityChange: (kind: string, isVisible: boolean) => void;
  onHeightChange: (kind: string, height: number) => void;
  onSpectrumRangeChange: (floorDb: number, ceilingDb: number) => void;
  onLoudnessClockSettingsChange: (
    minimumDb: number,
    maximumDb: number,
    warningDb: number,
    cycleSeconds: number,
  ) => void;
  onStereoSettingsOpen: () => void;
  onCollisionSettingsChange: (
    levelFloorDb: number,
    prominenceThresholdDb: number,
    relativeThreshold: number,
    frequencyScale: string,
    comparisonSourceKey: string | null,
  ) => void;
  isDragging?: boolean;
  dropHint?: "before" | "after" | null;
  onDragStart?: (kind: string, event: ReactDragEvent<HTMLElement>) => void;
  onDragOver?: (kind: string, event: ReactDragEvent<HTMLElement>) => void;
  onDragLeave?: (kind: string, event: ReactDragEvent<HTMLElement>) => void;
  onDrop?: (kind: string, event: ReactDragEvent<HTMLElement>) => void;
  onDragEnd?: () => void;
}

let loudnessClockHistory = createLoudnessClockHistory(30);
let loudnessClockCycleSeconds = 30;
const spectrumRollHistories = new Map<string, SpectrumRollHistory>();
const spectrumRollMaximumColumns = 320;
const channelModeOptions = [
  { value: "overall", label: "Overall" },
  { value: "channels", label: "Channels" },
];
const collisionScaleOptions = [
  { value: "linear", label: "Linear" },
  { value: "logarithmic", label: "Logarithmic" },
  { value: "mel", label: "Mel" },
];
const spectrumMinimumFrequencyHz = 20;
const spectrumMaximumFrequencyHz = 22000;
const spectrumMinimumMel = hertzToMel(spectrumMinimumFrequencyHz);
const spectrumMaximumMel = hertzToMel(spectrumMaximumFrequencyHz);
const spectrumMelSpan = spectrumMaximumMel - spectrumMinimumMel;
const spectrumMinimumViewportSpan = spectrumMelSpan / 32;
const spectrumPreferredPlotHeight = 140;
const spectrumDefaultViewport: SpectrumViewport = {
  centerMel: (spectrumMinimumMel + spectrumMaximumMel) / 2,
  spanMel: spectrumMelSpan,
};
const waveformMinimumValue = -1;
const waveformMaximumValue = 1;
const waveformDomainSpan = waveformMaximumValue - waveformMinimumValue;
const waveformMinimumViewportSpan = 0.08;
const waveformPreferredPlotHeight = 122;
const waveformDefaultViewport: WaveformViewport = { center: 0, span: waveformDomainSpan };
const chartTrackColor = "#111419";
const chartPanelColor = "#20252d";
const chartBorderColor = "#3a424d";
const chartSubtleColor = "rgba(174, 183, 194, 0.45)";
const chartSubtleStrongColor = "rgba(174, 183, 194, 0.7)";
const chartTextMutedColor = "rgba(174, 183, 194, 0.88)";
const chartAccentColor = "#2ec4b6";
const chartWarmColor = "#ffb000";
const chartDangerColor = "255, 63, 63";
const spectrumMajorFrequencies = [20, 100, 1000, 4000, 12000, 22000];
const spectrumMinorFrequencies = [31.5, 50, 63, 250, 500, 2000, 8000, 16000];

export function ChartModuleCanvas({
  detail,
  module,
  sources,
  sourceSettings,
  onWaveformModeChange,
  onSpectrumModeChange,
  onVisibilityChange,
  onHeightChange,
  onSpectrumRangeChange,
  onLoudnessClockSettingsChange,
  onStereoSettingsOpen,
  onCollisionSettingsChange,
  isDragging = false,
  dropHint = null,
  onDragStart,
  onDragOver,
  onDragLeave,
  onDrop,
  onDragEnd,
}: ChartModuleCanvasProps) {
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const moduleRef = useRef<HTMLElement | null>(null);
  const resizeStartRef = useRef<{ pointerId: number; y: number; height: number } | null>(null);
  const waveformPanStartRef = useRef<{
    pointerId: number;
    y: number;
    center: number;
    span: number;
    plotHeight: number;
  } | null>(null);
  const spectrumPanStartRef = useRef<{
    pointerId: number;
    y: number;
    centerMel: number;
    spanMel: number;
    plotHeight: number;
  } | null>(null);
  const collisionSourceMenuRef = useRef<HTMLDivElement | null>(null);
  const [isWaveformSettingsOpen, setWaveformSettingsOpen] = useState(false);
  const [isSpectrumSettingsOpen, setSpectrumSettingsOpen] = useState(false);
  const [isClockSettingsOpen, setClockSettingsOpen] = useState(false);
  const [isCollisionSettingsOpen, setCollisionSettingsOpen] = useState(false);
  const [isCollisionSourceMenuOpen, setCollisionSourceMenuOpen] = useState(false);
  const [spectrumHover, setSpectrumHover] = useState<{
    x: number;
    y: number;
    label: string;
    side: "left" | "right";
  } | null>(null);
  const [waveformViewport, setWaveformViewport] = useState<WaveformViewport>(waveformDefaultViewport);
  const [spectrumViewport, setSpectrumViewport] = useState<SpectrumViewport>(spectrumDefaultViewport);
  const [spectrumFloorDb, setSpectrumFloorDb] = useState(sourceSettings?.spectrumDisplayFloorDb ?? -96);
  const [spectrumCeilingDb, setSpectrumCeilingDb] = useState(sourceSettings?.spectrumDisplayCeilingDb ?? -6);
  const [clockMinimumDb, setClockMinimumDb] = useState(sourceSettings?.loudnessClockMinimumDb ?? -32);
  const [clockMaximumDb, setClockMaximumDb] = useState(sourceSettings?.loudnessClockMaximumDb ?? -4);
  const [clockWarningDb, setClockWarningDb] = useState(sourceSettings?.loudnessClockWarningDb ?? -12);
  const [clockCycleSeconds, setClockCycleSeconds] = useState(sourceSettings?.loudnessClockCycleSeconds ?? 30);
  const collision = detail.collision;
  const [collisionSourceKey, setCollisionSourceKey] = useState(collision?.comparisonSourceKey ?? "__global__");
  const [collisionLevelFloorDb, setCollisionLevelFloorDb] = useState(
    sourceSettings?.collisionLevelFloorDb ?? collision?.levelFloorDb ?? -96,
  );
  const [collisionProminenceDb, setCollisionProminenceDb] = useState(
    sourceSettings?.collisionProminenceThresholdDb ?? collision?.prominenceThresholdDb ?? 9,
  );
  const [collisionRelativeThreshold, setCollisionRelativeThreshold] = useState(
    sourceSettings?.collisionRelativeThreshold ?? collision?.relativeThreshold ?? 0.25,
  );
  const [collisionFrequencyScale, setCollisionFrequencyScale] = useState(
    sourceSettings?.collisionFrequencyScale ?? collision?.frequencyScale ?? "logarithmic",
  );
  const source = detail.source;
  const collisionSourceOptions = useMemo(
    () => [
      { sourceKey: "__global__", displayName: "Global" },
      ...sources
        .filter((item) => item.sourceKey !== source.sourceKey && !item.isInstanceMissing)
        .map((item) => ({ sourceKey: item.sourceKey, displayName: item.displayTitle })),
    ],
    [source.sourceKey, sources],
  );
  const selectedCollisionSourceName =
    collisionSourceOptions.find((option) => option.sourceKey === collisionSourceKey)?.displayName ?? "Global";
  const data = useMemo(() => buildChartData(module.kind, detail, sourceSettings), [module.kind, detail, sourceSettings]);
  const moduleHeight = normalizeModuleHeight(module.kind, module.height);
  const [draftHeight, setDraftHeight] = useState(moduleHeight);
  const effectiveHeight = module.isVisible ? draftHeight : moduleHeight;
  const canvasHeight = module.isVisible ? Math.max(58, effectiveHeight - 54) : 0;
  const hasOpenChartPopup =
    isWaveformSettingsOpen ||
    isSpectrumSettingsOpen ||
    isClockSettingsOpen ||
    isCollisionSettingsOpen ||
    isCollisionSourceMenuOpen;

  useEffect(() => {
    setSpectrumFloorDb(sourceSettings?.spectrumDisplayFloorDb ?? -96);
    setSpectrumCeilingDb(sourceSettings?.spectrumDisplayCeilingDb ?? -6);
  }, [sourceSettings?.spectrumDisplayCeilingDb, sourceSettings?.spectrumDisplayFloorDb]);

  useEffect(() => {
    setClockMinimumDb(sourceSettings?.loudnessClockMinimumDb ?? -32);
    setClockMaximumDb(sourceSettings?.loudnessClockMaximumDb ?? -4);
    setClockWarningDb(sourceSettings?.loudnessClockWarningDb ?? -12);
    setClockCycleSeconds(sourceSettings?.loudnessClockCycleSeconds ?? 30);
  }, [
    sourceSettings?.loudnessClockCycleSeconds,
    sourceSettings?.loudnessClockMaximumDb,
    sourceSettings?.loudnessClockMinimumDb,
    sourceSettings?.loudnessClockWarningDb,
  ]);

  useEffect(() => {
    setCollisionSourceKey(sourceSettings?.inlineCollisionSourceKey ?? collision?.comparisonSourceKey ?? "__global__");
    setCollisionLevelFloorDb(sourceSettings?.collisionLevelFloorDb ?? collision?.levelFloorDb ?? -96);
    setCollisionProminenceDb(sourceSettings?.collisionProminenceThresholdDb ?? collision?.prominenceThresholdDb ?? 9);
    setCollisionRelativeThreshold(sourceSettings?.collisionRelativeThreshold ?? collision?.relativeThreshold ?? 0.25);
    setCollisionFrequencyScale(sourceSettings?.collisionFrequencyScale ?? collision?.frequencyScale ?? "logarithmic");
  }, [
    collision?.comparisonSourceKey,
    collision?.frequencyScale,
    collision?.levelFloorDb,
    collision?.prominenceThresholdDb,
    collision?.relativeThreshold,
    sourceSettings?.collisionFrequencyScale,
    sourceSettings?.collisionLevelFloorDb,
    sourceSettings?.collisionProminenceThresholdDb,
    sourceSettings?.collisionRelativeThreshold,
    sourceSettings?.inlineCollisionSourceKey,
  ]);

  useEffect(() => {
    if (!hasOpenChartPopup) {
      return;
    }

    const closeChartPopups = () => {
      setWaveformSettingsOpen(false);
      setSpectrumSettingsOpen(false);
      setClockSettingsOpen(false);
      setCollisionSettingsOpen(false);
      setCollisionSourceMenuOpen(false);
    };

    const handlePointerDown = (event: PointerEvent) => {
      if (event.target instanceof Node && moduleRef.current?.contains(event.target)) {
        return;
      }

      closeChartPopups();
    };

    const handleKeyDown = (event: globalThis.KeyboardEvent) => {
      if (event.key === "Escape") {
        closeChartPopups();
      }
    };

    window.addEventListener("pointerdown", handlePointerDown);
    window.addEventListener("keydown", handleKeyDown);

    return () => {
      window.removeEventListener("pointerdown", handlePointerDown);
      window.removeEventListener("keydown", handleKeyDown);
    };
  }, [hasOpenChartPopup]);

  useEffect(() => {
    if (!resizeStartRef.current) {
      setDraftHeight(moduleHeight);
    }
  }, [moduleHeight]);

  useEffect(() => {
    if (isCollisionSourceMenuOpen) {
      focusFirstNativeMenuItem(collisionSourceMenuRef.current);
    }
  }, [isCollisionSourceMenuOpen]);

  useEffect(() => {
    setSpectrumHover(null);
  }, [data.kind, data.emptyText]);

  useEffect(() => {
    const canvas = canvasRef.current;
    const context = canvas?.getContext("2d");
    if (!canvas || !context) {
      return;
    }

    const nextWidth = Math.max(1, Math.floor(canvas.clientWidth));
    const nextHeight = Math.max(1, Math.floor(canvas.clientHeight));
    if (canvas.width !== nextWidth || canvas.height !== nextHeight) {
      canvas.width = nextWidth;
      canvas.height = nextHeight;
    }

    drawChart(context, canvas.width, canvas.height, data, waveformViewport, spectrumViewport);
  }, [data, canvasHeight, waveformViewport, spectrumViewport]);

  function beginResize(event: ReactPointerEvent<HTMLDivElement>): void {
    if (!module.isVisible) {
      return;
    }

    event.preventDefault();
    event.currentTarget.setPointerCapture?.(event.pointerId);
    resizeStartRef.current = {
      pointerId: event.pointerId,
      y: event.clientY,
      height: draftHeight,
    };
  }

  function updateResize(event: ReactPointerEvent<HTMLDivElement>): void {
    const resizeStart = resizeStartRef.current;
    if (!resizeStart || resizeStart.pointerId !== event.pointerId) {
      return;
    }

    event.preventDefault();
    setDraftHeight(normalizeModuleHeight(module.kind, resizeStart.height + event.clientY - resizeStart.y));
  }

  function finishResize(event: ReactPointerEvent<HTMLDivElement>): void {
    const resizeStart = resizeStartRef.current;
    if (!resizeStart || resizeStart.pointerId !== event.pointerId) {
      return;
    }

    event.preventDefault();
    resizeStartRef.current = null;
    const nextHeight = normalizeModuleHeight(module.kind, resizeStart.height + event.clientY - resizeStart.y);
    setDraftHeight(nextHeight);
    event.currentTarget.releasePointerCapture?.(event.pointerId);
    onHeightChange(module.kind, nextHeight);
  }

  function beginWaveformPan(event: ReactPointerEvent<HTMLCanvasElement>): void {
    if (data.kind !== "waveform" || data.emptyText || event.button !== 0) {
      return;
    }

    event.preventDefault();
    const rect = event.currentTarget.getBoundingClientRect();
    const viewport = normalizeWaveformViewport(waveformViewport, rect.height);
    waveformPanStartRef.current = {
      pointerId: event.pointerId,
      y: event.clientY,
      center: viewport.center,
      span: viewport.span,
      plotHeight: rect.height,
    };
    event.currentTarget.setPointerCapture?.(event.pointerId);
  }

  function updateWaveformPan(event: ReactPointerEvent<HTMLCanvasElement>): void {
    const panStart = waveformPanStartRef.current;
    if (!panStart || panStart.pointerId !== event.pointerId) {
      return;
    }

    event.preventDefault();
    const deltaY = event.clientY - panStart.y;
    const valueDelta = deltaY * panStart.span / Math.max(1, panStart.plotHeight);
    setWaveformViewport(normalizeWaveformViewport({ center: panStart.center + valueDelta, span: panStart.span }, panStart.plotHeight));
  }

  function finishWaveformPan(event: ReactPointerEvent<HTMLCanvasElement>): void {
    const panStart = waveformPanStartRef.current;
    if (!panStart || panStart.pointerId !== event.pointerId) {
      return;
    }

    event.preventDefault();
    waveformPanStartRef.current = null;
    event.currentTarget.releasePointerCapture?.(event.pointerId);
  }

  function zoomWaveform(event: ReactWheelEvent<HTMLCanvasElement>): void {
    if (data.kind !== "waveform" || data.emptyText) {
      return;
    }

    event.preventDefault();
    const rect = event.currentTarget.getBoundingClientRect();
    const viewport = normalizeWaveformViewport(waveformViewport, rect.height);
    const y = clamp(event.clientY - rect.top, 0, rect.height);
    const anchorRatio = (rect.height - y) / Math.max(1, rect.height);
    const anchorValue = viewport.center - viewport.span / 2 + viewport.span * anchorRatio;
    const zoom = 1.16 ** (event.deltaY / -120);
    const nextSpan = clamp(viewport.span / zoom, waveformMinimumViewportSpan, waveformDomainSpan);
    const effectiveSpan = Math.min(nextSpan, waveformLayoutLimitedSpan(rect.height));
    const nextCenter = anchorValue + effectiveSpan * (0.5 - anchorRatio);
    setWaveformViewport(normalizeWaveformViewport({ center: nextCenter, span: nextSpan }, rect.height));
  }

  function resetWaveformViewport(event: ReactMouseEvent<HTMLCanvasElement>): void {
    if (data.kind !== "waveform" || data.emptyText) {
      return;
    }

    event.preventDefault();
    setWaveformViewport(waveformDefaultViewport);
  }

  function beginSpectrumPan(event: ReactPointerEvent<HTMLCanvasElement>): void {
    if (data.kind !== "spectrum" || data.emptyText || event.button !== 0) {
      return;
    }

    event.preventDefault();
    const rect = event.currentTarget.getBoundingClientRect();
    const viewport = normalizeSpectrumViewport(spectrumViewport, rect.height);
    spectrumPanStartRef.current = {
      pointerId: event.pointerId,
      y: event.clientY,
      centerMel: viewport.centerMel,
      spanMel: viewport.spanMel,
      plotHeight: rect.height,
    };
    event.currentTarget.setPointerCapture?.(event.pointerId);
  }

  function updateSpectrumPan(event: ReactPointerEvent<HTMLCanvasElement>): void {
    const panStart = spectrumPanStartRef.current;
    if (!panStart || panStart.pointerId !== event.pointerId) {
      return;
    }

    event.preventDefault();
    const deltaY = event.clientY - panStart.y;
    const melDelta = deltaY * panStart.spanMel / Math.max(1, panStart.plotHeight);
    const nextViewport = normalizeSpectrumViewport(
      { centerMel: panStart.centerMel + melDelta, spanMel: panStart.spanMel },
      panStart.plotHeight,
    );
    setSpectrumViewport(nextViewport);
    updateSpectrumHoverFromCanvas(event.currentTarget, event.clientX, event.clientY, nextViewport);
  }

  function finishSpectrumPan(event: ReactPointerEvent<HTMLCanvasElement>): void {
    const panStart = spectrumPanStartRef.current;
    if (!panStart || panStart.pointerId !== event.pointerId) {
      return;
    }

    event.preventDefault();
    spectrumPanStartRef.current = null;
    event.currentTarget.releasePointerCapture?.(event.pointerId);
  }

  function zoomSpectrum(event: ReactWheelEvent<HTMLCanvasElement>): void {
    if (data.kind !== "spectrum" || data.emptyText) {
      return;
    }

    event.preventDefault();
    const rect = event.currentTarget.getBoundingClientRect();
    const viewport = normalizeSpectrumViewport(spectrumViewport, rect.height);
    const y = clamp(event.clientY - rect.top, 0, rect.height);
    const anchorRatio = (rect.height - y) / Math.max(1, rect.height);
    const anchorMel = viewport.centerMel - viewport.spanMel / 2 + viewport.spanMel * anchorRatio;
    const zoom = 1.16 ** (event.deltaY / -120);
    const nextSpan = clamp(viewport.spanMel / zoom, spectrumMinimumViewportSpan, spectrumMelSpan);
    const effectiveSpan = Math.min(nextSpan, spectrumLayoutLimitedSpan(rect.height));
    const nextCenter = anchorMel + effectiveSpan * (0.5 - anchorRatio);
    const nextViewport = normalizeSpectrumViewport({ centerMel: nextCenter, spanMel: nextSpan }, rect.height);
    setSpectrumViewport(nextViewport);
    updateSpectrumHoverFromCanvas(event.currentTarget, event.clientX, event.clientY, nextViewport);
  }

  function resetSpectrumViewport(event: ReactMouseEvent<HTMLCanvasElement>): void {
    if (data.kind !== "spectrum" || data.emptyText) {
      return;
    }

    event.preventDefault();
    setSpectrumViewport(spectrumDefaultViewport);
    updateSpectrumHoverFromCanvas(event.currentTarget, event.clientX, event.clientY, spectrumDefaultViewport);
  }

  function updateSpectrumHover(event: ReactMouseEvent<HTMLCanvasElement>): void {
    if (data.kind !== "spectrum" || data.emptyText) {
      setSpectrumHover(null);
      return;
    }

    updateSpectrumHoverFromCanvas(event.currentTarget, event.clientX, event.clientY);
  }

  function updateSpectrumHoverFromCanvas(
    canvas: HTMLCanvasElement,
    clientX: number,
    clientY: number,
    viewportOverride?: SpectrumViewport,
  ): void {
    const rect = canvas.getBoundingClientRect();
    const x = clamp(clientX - rect.left, 0, rect.width);
    const y = clamp(clientY - rect.top, 0, rect.height);
    const viewport = normalizeSpectrumViewport(viewportOverride ?? spectrumViewport, rect.height);
    const frequency = spectrumFrequencyAtY(y, rect.height, viewport);
    setSpectrumHover({
      x,
      y,
      label: formatFrequencyReadout(frequency),
      side: x > rect.width - 96 ? "left" : "right",
    });
  }

  return (
    <section
      ref={moduleRef}
      className={`chart-module chart-module-${module.kind}${module.isVisible ? "" : " chart-module-collapsed"}${
        isDragging ? " chart-module-dragging" : ""
      }${dropHint === "before" ? " chart-module-drop-before" : ""}${
        dropHint === "after" ? " chart-module-drop-after" : ""
      }`}
      style={{ minHeight: module.isVisible ? effectiveHeight : undefined }}
      data-testid="chart-module"
      onDragOver={(event) => onDragOver?.(module.kind, event)}
      onDragLeave={(event) => {
        if (!event.currentTarget.contains(event.relatedTarget as Node | null)) {
          onDragLeave?.(module.kind, event);
        }
      }}
      onDrop={(event) => onDrop?.(module.kind, event)}
    >
      <span className="chart-module-drop-hint chart-module-drop-hint-before" aria-hidden="true" />
      <div className="chart-module-header">
        <span
          className="chart-module-drag-handle"
          aria-hidden="true"
          title="Drag to reorder"
          draggable
          onDragStart={(event) => onDragStart?.(module.kind, event)}
          onDragEnd={onDragEnd}
        >
          ::
        </span>
        <button
          type="button"
          className="chart-module-toggle"
          aria-label={`Show ${module.title}`}
          aria-pressed={module.isVisible}
          title={getModuleToggleTitle(module.kind, module.title)}
          onClick={() => onVisibilityChange(module.kind, !module.isVisible)}
        >
          {module.isVisible ? "v" : ">"}
        </button>
        <h4>{module.title}</h4>
        <div className="chart-controls">
          {module.kind === "waveform" ? (
            <button
              type="button"
              className="chart-settings-button"
              aria-label="Waveform settings"
              aria-expanded={isWaveformSettingsOpen}
              title="Waveform settings"
              onClick={() => setWaveformSettingsOpen((isOpen) => !isOpen)}
            >
              <span aria-hidden="true">&#xE713;</span>
            </button>
          ) : null}
          {module.kind === "spectrum" ? (
            <button
              type="button"
              className="chart-settings-button"
              aria-label="Spectrum settings"
              aria-expanded={isSpectrumSettingsOpen}
              title="Spectrum settings"
              onClick={() => setSpectrumSettingsOpen((isOpen) => !isOpen)}
            >
              <span aria-hidden="true">&#xE713;</span>
            </button>
          ) : null}
          {module.kind === "loudnessClock" ? (
            <button
              type="button"
              className="chart-settings-button"
              aria-label="Clock settings"
              aria-expanded={isClockSettingsOpen}
              title="Clock settings"
              onClick={() => setClockSettingsOpen((isOpen) => !isOpen)}
            >
              <span aria-hidden="true">&#xE713;</span>
            </button>
          ) : null}
          {module.kind === "stereo" ? (
            <button
              type="button"
              className="chart-settings-button"
              aria-label="Stereo settings"
              title="Stereo image scale settings"
              onClick={onStereoSettingsOpen}
            >
              <span aria-hidden="true">&#xE713;</span>
            </button>
          ) : null}
          {module.kind === "collision" ? (
            <>
              <span className="chart-collision-source-menu-wrap">
                <button
                  type="button"
                  className="chart-collision-source-button"
                  aria-label="Collision source"
                  aria-expanded={isCollisionSourceMenuOpen}
                  disabled={collisionSourceOptions.length <= 1}
                  onClick={() => {
                    setCollisionSettingsOpen(false);
                    setCollisionSourceMenuOpen((isOpen) => !isOpen);
                  }}
                >
                  <span>{selectedCollisionSourceName}</span>
                  <span aria-hidden="true">v</span>
                </button>
                {isCollisionSourceMenuOpen ? (
                  <div
                    ref={collisionSourceMenuRef}
                    className="chart-context-menu chart-collision-source-menu"
                    role="menu"
                    aria-label="Collision source menu"
                    onKeyDown={handleNativeMenuKeyboard}
                  >
                    {collisionSourceOptions.map((option) => (
                      <button
                        type="button"
                        key={option.sourceKey}
                        role="menuitem"
                        onClick={() => {
                          setCollisionSourceKey(option.sourceKey);
                          setCollisionSourceMenuOpen(false);
                          onCollisionSettingsChange(
                            collisionLevelFloorDb,
                            collisionProminenceDb,
                            collisionRelativeThreshold,
                            collisionFrequencyScale,
                            option.sourceKey === "__global__" ? null : option.sourceKey,
                          );
                        }}
                      >
                        {option.displayName}
                      </button>
                    ))}
                  </div>
                ) : null}
              </span>
              <span className="chart-collision-frequency-range">20 Hz - 22 kHz</span>
              <button
                type="button"
                className="chart-settings-button"
                aria-label="Collision settings"
                aria-expanded={isCollisionSettingsOpen}
                title="Collision settings"
                onClick={() => {
                  setCollisionSourceMenuOpen(false);
                  setCollisionSettingsOpen((isOpen) => !isOpen);
                }}
              >
                <span aria-hidden="true">&#xE713;</span>
              </button>
            </>
          ) : null}
        </div>
      </div>
      {module.kind === "waveform" && isWaveformSettingsOpen ? (
        <div className="chart-settings-panel chart-settings-panel-waveform" aria-label="Waveform settings panel">
          <NativeComboBox
            ariaLabel="Waveform mode"
            value={source.waveformDisplayMode}
            options={channelModeOptions}
            onChange={onWaveformModeChange}
          />
        </div>
      ) : null}
      {module.kind === "spectrum" && isSpectrumSettingsOpen ? (
        <div className="chart-settings-panel chart-settings-panel-spectrum" aria-label="Spectrum settings panel">
          <NativeComboBox
            ariaLabel="Spectrum mode"
            value={source.spectrumDisplayMode}
            options={channelModeOptions}
            onChange={onSpectrumModeChange}
          />
          <div className="chart-settings-fields">
            <NativeRangeSliderField
              label="Range"
              lowerLabel="Floor"
              upperLabel="Ceiling"
              minimum={-200}
              maximum={0}
              lowerValue={spectrumFloorDb}
              upperValue={spectrumCeilingDb}
              valueText={formatRangeSummary(spectrumFloorDb, spectrumCeilingDb, " dB")}
              onLowerChange={setSpectrumFloorDb}
              onUpperChange={setSpectrumCeilingDb}
            />
          </div>
          <button
            type="button"
            className="native-button"
            onClick={() => onSpectrumRangeChange(spectrumFloorDb, spectrumCeilingDb)}
          >
            Apply
          </button>
        </div>
      ) : null}
      {module.kind === "loudnessClock" && isClockSettingsOpen ? (
        <div className="chart-settings-panel chart-settings-panel-clock" aria-label="Clock settings panel">
          <div className="chart-settings-row">
            <span>Range</span>
            <span>
              {clockMinimumDb} to {clockMaximumDb} LUFS
            </span>
          </div>
          <div className="chart-settings-fields">
            <NativeRangeSliderField
              label="Range"
              lowerLabel="Min"
              upperLabel="Max"
              minimum={-120}
              maximum={0}
              lowerValue={clockMinimumDb}
              upperValue={clockMaximumDb}
              valueText={formatRangeSummary(clockMinimumDb, clockMaximumDb, " LUFS")}
              onLowerChange={setClockMinimumDb}
              onUpperChange={setClockMaximumDb}
            />
          </div>
          <NativeSliderField
            label="Warning"
            minimum={-120}
            maximum={0}
            value={clockWarningDb}
            valueText={`${clockWarningDb} LUFS`}
            onChange={setClockWarningDb}
          />
          <NativeSliderField
            label="Cycle"
            minimum={10}
            maximum={600}
            step={5}
            value={clockCycleSeconds}
            valueText={`${clockCycleSeconds} s`}
            onChange={setClockCycleSeconds}
          />
          <button
            type="button"
            className="native-button"
            onClick={() =>
              onLoudnessClockSettingsChange(clockMinimumDb, clockMaximumDb, clockWarningDb, clockCycleSeconds)
            }
          >
            Apply
          </button>
        </div>
      ) : null}
      {module.kind === "collision" && isCollisionSettingsOpen ? (
        <div className="chart-settings-panel chart-settings-panel-collision" aria-label="Collision settings panel">
          <div className="chart-settings-row">
            <span>Scale</span>
            <span>{toCollisionScaleLabel(collisionFrequencyScale)}</span>
          </div>
          <NativeComboBox
            ariaLabel="Collision scale"
            value={collisionFrequencyScale}
            options={collisionScaleOptions}
            onChange={setCollisionFrequencyScale}
          />
          <NativeSliderField
            label="Level floor"
            minimum={-200}
            maximum={0}
            value={collisionLevelFloorDb}
            valueText={`${collisionLevelFloorDb} dB`}
            onChange={setCollisionLevelFloorDb}
          />
          <NativeSliderField
            label="Prominence"
            minimum={0}
            maximum={24}
            value={collisionProminenceDb}
            valueText={`${collisionProminenceDb} dB`}
            onChange={setCollisionProminenceDb}
          />
          <NativeSliderField
            label="Match tolerance"
            minimum={2}
            maximum={80}
            step={2}
            value={Math.round(collisionRelativeThreshold * 100)}
            valueText={`${Math.round(collisionRelativeThreshold * 100)}%`}
            onChange={(value) => setCollisionRelativeThreshold(value / 100)}
          />
          <button
            type="button"
            className="native-button"
            onClick={() =>
              onCollisionSettingsChange(
                collisionLevelFloorDb,
                collisionProminenceDb,
                collisionRelativeThreshold,
                collisionFrequencyScale,
                collisionSourceKey === "__global__" ? null : collisionSourceKey,
              )
            }
          >
            Apply
          </button>
        </div>
      ) : null}
      {module.isVisible ? (
        <>
          <div className="chart-canvas-wrap">
            <canvas
              ref={canvasRef}
              style={{ height: canvasHeight }}
              role="img"
              aria-label={`${module.title} chart`}
              onPointerDown={
                module.kind === "waveform" ? beginWaveformPan : module.kind === "spectrum" ? beginSpectrumPan : undefined
              }
              onPointerMove={
                module.kind === "waveform" ? updateWaveformPan : module.kind === "spectrum" ? updateSpectrumPan : undefined
              }
              onPointerUp={
                module.kind === "waveform" ? finishWaveformPan : module.kind === "spectrum" ? finishSpectrumPan : undefined
              }
              onPointerCancel={
                module.kind === "waveform" ? finishWaveformPan : module.kind === "spectrum" ? finishSpectrumPan : undefined
              }
              onLostPointerCapture={
                module.kind === "waveform" ? finishWaveformPan : module.kind === "spectrum" ? finishSpectrumPan : undefined
              }
              onWheel={module.kind === "waveform" ? zoomWaveform : module.kind === "spectrum" ? zoomSpectrum : undefined}
              onDoubleClick={
                module.kind === "waveform"
                  ? resetWaveformViewport
                  : module.kind === "spectrum"
                    ? resetSpectrumViewport
                    : undefined
              }
              onMouseMove={module.kind === "spectrum" ? updateSpectrumHover : undefined}
              onMouseLeave={module.kind === "spectrum" ? () => setSpectrumHover(null) : undefined}
            />
            {module.kind === "spectrum" && spectrumHover ? (
              <>
                <span className="chart-hover-line" style={{ top: spectrumHover.y }} aria-hidden="true" />
                <span
                  className="chart-hover-readout"
                  data-side={spectrumHover.side}
                  style={{ left: spectrumHover.x, top: spectrumHover.y }}
                  aria-hidden="true"
                >
                  {spectrumHover.label}
                </span>
              </>
            ) : null}
            {data.emptyText && data.kind !== "spectrum" && data.kind !== "collision" && data.kind !== "loudnessClock" ? (
              <span className="chart-empty">{data.emptyText}</span>
            ) : null}
          </div>
          <div
            className="chart-module-resize-grip"
            aria-hidden="true"
            title="Resize module"
            onPointerDown={beginResize}
            onPointerMove={updateResize}
            onPointerUp={finishResize}
            onPointerCancel={finishResize}
          >
            <span className="chart-module-resize-rail" />
            <span className="chart-module-resize-handle" />
          </div>
        </>
      ) : null}
      <span className="chart-module-drop-hint chart-module-drop-hint-after" aria-hidden="true" />
    </section>
  );
}

type ChartData =
  | { kind: "waveform"; points: Point[]; emptyText: string | null }
  | {
      kind: "spectrum";
      sourceKey: string;
      frame: number;
      buckets: readonly number[];
      floorDb: number;
      ceilingDb: number;
      emptyText: string | null;
    }
  | { kind: "stereo"; points: StereoPoint[]; emptyText: string | null }
  | {
      kind: "loudnessClock";
      minimumDb: number;
      maximumDb: number;
      warningDb: number;
      points: Array<{ frame: number; loudness: number; phase: number }>;
      emptyText: string | null;
    }
  | { kind: "collision"; bands: CollisionBandGeometry[]; emptyText: string | null }
  | { kind: "unknown"; emptyText: string };

interface SpectrumRollHistory {
  frame: number;
  bucketCount: number;
  columns: number[][];
}

interface WaveformViewport {
  center: number;
  span: number;
}

interface SpectrumViewport {
  centerMel: number;
  spanMel: number;
}

export function buildChartData(
  kind: string,
  detail: HostDetailSnapshotDto,
  sourceSettings?: HostSourceSettingsSnapshotDto | null,
): ChartData {
  const source = detail.source;

  if (kind === "waveform") {
    const samples = selectWaveformSamples(source);
    return {
      kind,
      points: createWaveformPath(samples),
      emptyText: samples.length === 0 ? "No waveform samples" : null,
    };
  }

  if (kind === "spectrum") {
    const buckets = selectSpectrumBuckets(source);
    const floorDb = sourceSettings?.spectrumDisplayFloorDb ?? -96;
    const ceilingDb = sourceSettings?.spectrumDisplayCeilingDb ?? -6;
    return {
      kind,
      sourceKey: source.sourceKey,
      frame: source.plotFrameVersion,
      buckets,
      floorDb,
      ceilingDb,
      emptyText: buckets.length === 0 ? "No spectrum buckets" : null,
    };
  }

  if (kind === "stereo") {
    const points = source.stereoImagePoints ?? [];
    return {
      kind,
      points: createStereoPoints(points),
      emptyText: points.length === 0 ? "No stereo points" : null,
    };
  }

  if (kind === "loudnessClock") {
    const nextCycleSeconds = sourceSettings?.loudnessClockCycleSeconds ?? 30;
    if (nextCycleSeconds !== loudnessClockCycleSeconds) {
      loudnessClockCycleSeconds = nextCycleSeconds;
      loudnessClockHistory = createLoudnessClockHistory(nextCycleSeconds);
    }

    const loudness = source.momentaryLufs ?? source.shortTermLufs;
    if (loudness !== null && loudness !== undefined) {
      loudnessClockHistory.add(source.sourceKey, source.plotFrameVersion, loudness);
    }

    const points = loudnessClockHistory.pointsFor(source.sourceKey);
    return {
      kind,
      minimumDb: sourceSettings?.loudnessClockMinimumDb ?? -32,
      maximumDb: sourceSettings?.loudnessClockMaximumDb ?? -4,
      warningDb: sourceSettings?.loudnessClockWarningDb ?? -12,
      points,
      emptyText: points.length === 0 ? "No loudness history" : null,
    };
  }

  if (kind === "collision") {
    const bands = createCollisionBands(detail.collision?.bands ?? []);
    return {
      kind,
      bands,
      emptyText: bands.length === 0 ? "No collision bands" : null,
    };
  }

  return { kind: "unknown", emptyText: "Unsupported chart module" };
}

export function resetChartRuntimeStateForTests(): void {
  loudnessClockCycleSeconds = 30;
  loudnessClockHistory = createLoudnessClockHistory(30);
  spectrumRollHistories.clear();
}

function toCollisionScaleLabel(scale: string): string {
  if (scale === "mel") {
    return "Mel";
  }
  if (scale === "linear") {
    return "Linear";
  }
  return "Logarithmic";
}

function selectWaveformSamples(source: HostSourceSnapshotDto): readonly number[] {
  if (source.waveformDisplayMode === "channels") {
    return source.channelWaveformSamples?.flat() ?? source.waveformSamples ?? [];
  }

  return source.waveformSamples ?? [];
}

function selectSpectrumBuckets(source: HostSourceSnapshotDto): readonly number[] {
  if (source.spectrumDisplayMode === "channels") {
    return source.channelSpectrumBuckets?.flat() ?? source.spectrumBuckets ?? [];
  }

  return source.spectrumBuckets ?? [];
}

function getModuleToggleTitle(kind: string, fallbackTitle: string): string {
  switch (kind) {
    case "stereo":
      return "Show stereo image";
    case "collision":
      return "Show frequency collision";
    case "loudnessClock":
      return "Show clock";
    case "waveform":
      return "Show waveform";
    case "spectrum":
      return "Show spectrum";
    default:
      return `Show ${fallbackTitle.toLowerCase()}`;
  }
}

function NativeSliderField({
  label,
  minimum,
  maximum,
  step = 1,
  value,
  valueText,
  onChange,
}: {
  label: string;
  minimum: number;
  maximum: number;
  step?: number;
  value: number;
  valueText: string;
  onChange: (value: number) => void;
}) {
  const percent = valueToPercent(value, minimum, maximum);
  const [isEditing, setIsEditing] = useState(false);
  const [draftValue, setDraftValue] = useState(formatSliderEditorValue(value, step));
  const editorRef = useRef<HTMLInputElement | null>(null);
  const visualRef = useRef<HTMLDivElement | null>(null);
  const dragPointerIdRef = useRef<number | null>(null);
  const shouldSkipNextCommitRef = useRef(false);

  useEffect(() => {
    if (!isEditing) {
      setDraftValue(formatSliderEditorValue(value, step));
    }
  }, [isEditing, step, value]);

  useEffect(() => {
    if (isEditing) {
      editorRef.current?.focus();
      editorRef.current?.select();
    }
  }, [isEditing]);

  function beginEdit(): void {
    setDraftValue(formatSliderEditorValue(value, step));
    setIsEditing(true);
  }

  function endEdit(commit: boolean): void {
    if (commit) {
      const nextValue = Number(draftValue);
      if (Number.isFinite(nextValue)) {
        onChange(clamp(snapToStep(nextValue, step), minimum, maximum));
      }
    }

    setIsEditing(false);
  }

  function handleEditorKeyDown(event: ReactKeyboardEvent<HTMLInputElement>): void {
    if (event.key === "Enter") {
      event.preventDefault();
      event.stopPropagation();
      endEdit(true);
    } else if (event.key === "Escape") {
      event.preventDefault();
      event.stopPropagation();
      shouldSkipNextCommitRef.current = true;
      endEdit(false);
    }
  }

  function handleFieldKeyDown(event: ReactKeyboardEvent<HTMLDivElement>): void {
    if (event.key === "Enter") {
      event.preventDefault();
      beginEdit();
    } else if (event.key === "ArrowLeft" || event.key === "ArrowDown") {
      event.preventDefault();
      onChange(clamp(snapToStep(value - step, step), minimum, maximum));
    } else if (event.key === "ArrowRight" || event.key === "ArrowUp") {
      event.preventDefault();
      onChange(clamp(snapToStep(value + step, step), minimum, maximum));
    } else if (event.key === "Home") {
      event.preventDefault();
      onChange(minimum);
    } else if (event.key === "End") {
      event.preventDefault();
      onChange(maximum);
    }
  }

  function updateValueFromPointer(event: ReactPointerEvent<HTMLElement>): void {
    const nextValue = sliderValueFromPointer(event, visualRef.current, minimum, maximum, step);
    if (nextValue !== null) {
      onChange(nextValue);
    }
  }

  function handlePointerDown(event: ReactPointerEvent<HTMLDivElement>): void {
    if (event.button > 0) {
      return;
    }

    event.preventDefault();
    event.currentTarget.closest<HTMLElement>(".native-slider-field")?.focus();
    endEdit(true);
    dragPointerIdRef.current = event.pointerId;
    event.currentTarget.setPointerCapture?.(event.pointerId);
    updateValueFromPointer(event);
  }

  function handlePointerMove(event: ReactPointerEvent<HTMLDivElement>): void {
    if (dragPointerIdRef.current !== event.pointerId) {
      return;
    }

    event.preventDefault();
    updateValueFromPointer(event);
  }

  function handlePointerUp(event: ReactPointerEvent<HTMLDivElement>): void {
    if (dragPointerIdRef.current !== event.pointerId) {
      return;
    }

    event.preventDefault();
    updateValueFromPointer(event);
    dragPointerIdRef.current = null;
    event.currentTarget.releasePointerCapture?.(event.pointerId);
  }

  return (
    <div className="native-slider-field" tabIndex={0} onDoubleClick={beginEdit} onKeyDown={handleFieldKeyDown}>
      <div className="chart-settings-row native-slider-summary">
        <span>{label}</span>
        <span>{valueText}</span>
      </div>
      {isEditing ? (
        <input
          ref={editorRef}
          className="native-slider-editor"
          aria-label={label}
          type="number"
          min={minimum}
          max={maximum}
          step={step}
          value={draftValue}
          style={{ left: `${percent}%` }}
          onBlur={() => {
            if (shouldSkipNextCommitRef.current) {
              shouldSkipNextCommitRef.current = false;
              return;
            }

            endEdit(true);
          }}
          onChange={(event) => setDraftValue(event.currentTarget.value)}
          onKeyDown={handleEditorKeyDown}
        />
      ) : null}
      <div
        ref={visualRef}
        className="native-slider-visual"
        aria-hidden="true"
        onPointerDown={handlePointerDown}
        onPointerMove={handlePointerMove}
        onPointerUp={handlePointerUp}
        onPointerCancel={handlePointerUp}
      >
        <span className="native-slider-track">
          <span className="native-slider-fill" style={{ width: `${percent}%` }} />
          <span className="native-slider-thumb" style={{ left: `${percent}%` }} />
        </span>
        <NativeSliderTicks />
        <span className="native-slider-endpoints">
          <span>{formatSliderEndpoint(minimum)}</span>
          <span>{formatSliderEndpoint(maximum)}</span>
        </span>
      </div>
    </div>
  );
}

function NativeRangeSliderField({
  label,
  lowerLabel,
  upperLabel,
  minimum,
  maximum,
  step = 1,
  lowerValue,
  upperValue,
  valueText,
  onLowerChange,
  onUpperChange,
}: {
  label: string;
  lowerLabel: string;
  upperLabel: string;
  minimum: number;
  maximum: number;
  step?: number;
  lowerValue: number;
  upperValue: number;
  valueText: string;
  onLowerChange: (value: number) => void;
  onUpperChange: (value: number) => void;
}) {
  const lowerPercent = valueToPercent(lowerValue, minimum, maximum);
  const upperPercent = valueToPercent(upperValue, minimum, maximum);
  const fillLeft = Math.min(lowerPercent, upperPercent);
  const fillWidth = Math.abs(upperPercent - lowerPercent);
  const visualRef = useRef<HTMLDivElement | null>(null);
  const [editingThumb, setEditingThumb] = useState<"lower" | "upper" | null>(null);
  const [draftValue, setDraftValue] = useState("");
  const editorRef = useRef<HTMLInputElement | null>(null);
  const dragPointerIdRef = useRef<number | null>(null);
  const dragThumbRef = useRef<"lower" | "upper" | null>(null);
  const shouldSkipNextCommitRef = useRef(false);

  useEffect(() => {
    if (editingThumb === "lower") {
      setDraftValue(formatSliderEditorValue(lowerValue, step));
    } else if (editingThumb === "upper") {
      setDraftValue(formatSliderEditorValue(upperValue, step));
    }
  }, [editingThumb, lowerValue, step, upperValue]);

  useEffect(() => {
    if (editingThumb) {
      editorRef.current?.focus();
      editorRef.current?.select();
    }
  }, [editingThumb]);

  function beginEdit(event: ReactMouseEvent<HTMLDivElement>): void {
    const thumb = chooseRangeThumb(event, visualRef.current, lowerPercent, upperPercent);
    setDraftValue(formatSliderEditorValue(thumb === "lower" ? lowerValue : upperValue, step));
    setEditingThumb(thumb);
  }

  function updateThumbValue(thumb: "lower" | "upper", value: number): void {
    if (thumb === "lower") {
      onLowerChange(clamp(value, minimum, upperValue - step));
    } else {
      onUpperChange(clamp(value, lowerValue + step, maximum));
    }
  }

  function endEdit(commit: boolean): void {
    if (commit && editingThumb) {
      const nextValue = Number(draftValue);
      if (Number.isFinite(nextValue)) {
        const snapped = snapToStep(nextValue, step);
        if (editingThumb === "lower") {
          onLowerChange(clamp(snapped, minimum, upperValue - step));
        } else {
          onUpperChange(clamp(snapped, lowerValue + step, maximum));
        }
      }
    }

    setEditingThumb(null);
  }

  function handleEditorKeyDown(event: ReactKeyboardEvent<HTMLInputElement>): void {
    if (event.key === "Enter") {
      event.preventDefault();
      event.stopPropagation();
      endEdit(true);
    } else if (event.key === "Escape") {
      event.preventDefault();
      event.stopPropagation();
      shouldSkipNextCommitRef.current = true;
      endEdit(false);
    }
  }

  function updateRangeFromPointer(event: ReactPointerEvent<HTMLElement>, thumb: "lower" | "upper"): void {
    const nextValue = sliderValueFromPointer(event, visualRef.current, minimum, maximum, step);
    if (nextValue !== null) {
      updateThumbValue(thumb, nextValue);
    }
  }

  function handlePointerDown(event: ReactPointerEvent<HTMLDivElement>): void {
    if (event.button > 0) {
      return;
    }

    event.preventDefault();
    event.currentTarget.closest<HTMLElement>(".native-slider-field")?.focus();
    endEdit(true);
    const thumb = chooseRangeThumb(event, visualRef.current, lowerPercent, upperPercent);
    dragPointerIdRef.current = event.pointerId;
    dragThumbRef.current = thumb;
    event.currentTarget.setPointerCapture?.(event.pointerId);
    updateRangeFromPointer(event, thumb);
  }

  function handlePointerMove(event: ReactPointerEvent<HTMLDivElement>): void {
    if (dragPointerIdRef.current !== event.pointerId || !dragThumbRef.current) {
      return;
    }

    event.preventDefault();
    updateRangeFromPointer(event, dragThumbRef.current);
  }

  function handlePointerUp(event: ReactPointerEvent<HTMLDivElement>): void {
    if (dragPointerIdRef.current !== event.pointerId) {
      return;
    }

    event.preventDefault();
    if (dragThumbRef.current) {
      updateRangeFromPointer(event, dragThumbRef.current);
    }

    dragPointerIdRef.current = null;
    dragThumbRef.current = null;
    event.currentTarget.releasePointerCapture?.(event.pointerId);
  }

  const editorPercent = editingThumb === "lower" ? lowerPercent : upperPercent;
  const editorLabel = editingThumb === "lower" ? lowerLabel : upperLabel;
  const editorMinimum = editingThumb === "lower" ? minimum : Math.min(maximum, lowerValue + step);
  const editorMaximum = editingThumb === "lower" ? Math.max(minimum, upperValue - step) : maximum;

  return (
    <div className="native-slider-field native-slider-field-range" tabIndex={0} onDoubleClick={beginEdit}>
      <div className="chart-settings-row native-slider-summary">
        <span>{label}</span>
        <span>{valueText}</span>
      </div>
      {editingThumb ? (
        <input
          ref={editorRef}
          className="native-slider-editor"
          aria-label={editorLabel}
          type="number"
          min={editorMinimum}
          max={editorMaximum}
          step={step}
          value={draftValue}
          style={{ left: `${editorPercent}%` }}
          onBlur={() => {
            if (shouldSkipNextCommitRef.current) {
              shouldSkipNextCommitRef.current = false;
              return;
            }

            endEdit(true);
          }}
          onChange={(event) => setDraftValue(event.currentTarget.value)}
          onKeyDown={handleEditorKeyDown}
        />
      ) : null}
      <div
        ref={visualRef}
        className="native-slider-visual"
        aria-hidden="true"
        onPointerDown={handlePointerDown}
        onPointerMove={handlePointerMove}
        onPointerUp={handlePointerUp}
        onPointerCancel={handlePointerUp}
      >
        <span className="native-slider-track">
          <span className="native-slider-fill native-slider-fill-range" style={{ left: `${fillLeft}%`, width: `${fillWidth}%` }} />
          <span className="native-slider-thumb" style={{ left: `${lowerPercent}%` }} />
          <span className="native-slider-thumb" style={{ left: `${upperPercent}%` }} />
        </span>
        <NativeSliderTicks />
        <span className="native-slider-endpoints">
          <span>{formatSliderEndpoint(minimum)}</span>
          <span>{formatSliderEndpoint(maximum)}</span>
        </span>
      </div>
    </div>
  );
}

function NativeSliderTicks() {
  return (
    <span className="native-slider-ticks">
      {[0, 25, 50, 75, 100].map((percent) => (
        <span key={percent} style={{ left: `${percent}%` }} />
      ))}
    </span>
  );
}

function valueToPercent(value: number, minimum: number, maximum: number): number {
  const span = Math.max(0.0001, maximum - minimum);
  return Math.max(0, Math.min(100, ((value - minimum) / span) * 100));
}

function hertzToMel(frequency: number): number {
  return 2595 * Math.log10(1 + frequency / 700);
}

function melToHertz(mel: number): number {
  return 700 * (10 ** (mel / 2595) - 1);
}

function formatFrequencyReadout(frequency: number): string {
  if (frequency < 1000) {
    return `${Math.round(frequency)} Hz`;
  }

  const kilohertz = frequency / 1000;
  return `${kilohertz < 10 ? kilohertz.toFixed(2) : kilohertz.toFixed(1)} kHz`;
}

function clamp(value: number, minimum: number, maximum: number): number {
  return Math.max(minimum, Math.min(maximum, value));
}

function dbToDisplayLevel(decibels: number): number {
  return clamp((decibels + 200) / 200, 0, 1);
}

function normalizeModuleHeight(kind: string, height: number): number {
  if (kind === "collision") {
    return 112;
  }

  const minimum = kind === "stereo" ? 320 : 160;
  const finiteHeight = Number.isFinite(height) && height > 0 ? height : defaultModuleHeight(kind);
  return clamp(Math.round(finiteHeight), minimum, 1200);
}

function defaultModuleHeight(kind: string): number {
  switch (kind) {
    case "stereo":
      return 320;
    case "collision":
      return 112;
    case "loudnessClock":
      return 260;
    case "waveform":
    case "spectrum":
    default:
      return 220;
  }
}

function snapToStep(value: number, step: number): number {
  if (!Number.isFinite(step) || step <= 0) {
    return value;
  }

  return Math.round(value / step) * step;
}

function formatSliderEndpoint(value: number): string {
  return Number.isInteger(value) ? `${value}` : value.toFixed(2);
}

function formatSliderEditorValue(value: number, step: number): string {
  if (Number.isInteger(step) && Number.isInteger(value)) {
    return `${value}`;
  }

  const precision = Math.min(6, Math.max(0, `${step}`.split(".")[1]?.length ?? 0));
  return value.toFixed(precision);
}

function formatRangeSummary(lowerValue: number, upperValue: number, suffix: string): string {
  return `${lowerValue} to ${upperValue}${suffix}`;
}

function sliderValueFromPointer(
  event: { clientX: number },
  visual: HTMLElement | null,
  minimum: number,
  maximum: number,
  step: number,
): number | null {
  if (!visual || !Number.isFinite(event.clientX)) {
    return null;
  }

  const rect = visual.getBoundingClientRect();
  const trackLeft = rect.left + 14;
  const trackWidth = Math.max(1, rect.width - 28);
  const pointerPercent = clamp((event.clientX - trackLeft) / trackWidth, 0, 1);
  return clamp(snapToStep(minimum + (maximum - minimum) * pointerPercent, step), minimum, maximum);
}

function chooseRangeThumb(
  event: { clientX: number },
  visual: HTMLElement | null,
  lowerPercent: number,
  upperPercent: number,
): "lower" | "upper" {
  if (!visual) {
    return Math.abs(lowerPercent - 50) <= Math.abs(upperPercent - 50) ? "lower" : "upper";
  }

  const rect = visual.getBoundingClientRect();
  const trackLeft = rect.left + 14;
  const trackWidth = Math.max(1, rect.width - 28);
  const pointerPercent = clamp(((event.clientX - trackLeft) / trackWidth) * 100, 0, 100);
  return Math.abs(pointerPercent - lowerPercent) <= Math.abs(pointerPercent - upperPercent) ? "lower" : "upper";
}

function drawChart(
  context: CanvasRenderingContext2D,
  width: number,
  height: number,
  data: ChartData,
  waveformViewport: WaveformViewport,
  spectrumViewport: SpectrumViewport,
): void {
  context.clearRect(0, 0, width, height);
  context.fillStyle = chartTrackColor;
  context.fillRect(0, 0, width, height);

  if (data.emptyText && data.kind !== "spectrum" && data.kind !== "collision" && data.kind !== "loudnessClock") {
    drawGrid(context, width, height);
    return;
  }

  if (data.kind === "waveform") {
    drawWaveform(context, width, height, data.points, waveformViewport);
  } else if (data.kind === "spectrum") {
    drawSpectrum(context, width, height, data, spectrumViewport);
  } else if (data.kind === "stereo") {
    drawStereo(context, width, height, data.points);
  } else if (data.kind === "loudnessClock") {
    drawLoudnessClock(context, width, height, data);
  } else if (data.kind === "collision") {
    drawCollision(context, width, height, data.bands);
  }
}

function drawGrid(context: CanvasRenderingContext2D, width: number, height: number): void {
  context.strokeStyle = chartSubtleColor;
  context.lineWidth = 1;
  for (let index = 1; index < 4; index += 1) {
    const y = (height / 4) * index;
    context.beginPath();
    context.moveTo(0, y);
    context.lineTo(width, y);
    context.stroke();
  }
}

function drawWaveform(
  context: CanvasRenderingContext2D,
  width: number,
  height: number,
  points: Point[],
  waveformViewport: WaveformViewport,
): void {
  drawGrid(context, width, height);
  const viewport = normalizeWaveformViewport(waveformViewport, height);
  const viewportMinimum = viewport.center - viewport.span / 2;
  context.strokeStyle = chartAccentColor;
  context.lineWidth = 1.8;
  context.beginPath();
  points.forEach((point, index) => {
    const x = point.x * width;
    const value = 1 - point.y * waveformDomainSpan;
    const y = height - ((value - viewportMinimum) / viewport.span) * height;
    if (index === 0) {
      context.moveTo(x, y);
    } else {
      context.lineTo(x, y);
    }
  });
  context.stroke();
}

function normalizeWaveformViewport(viewport: WaveformViewport, plotHeight: number): WaveformViewport {
  const span = clamp(viewport.span, waveformMinimumViewportSpan, waveformDomainSpan);
  const effectiveSpan = Math.min(span, waveformLayoutLimitedSpan(plotHeight));
  return {
    center: clampWaveformCenter(viewport.center, effectiveSpan),
    span: effectiveSpan,
  };
}

function waveformLayoutLimitedSpan(plotHeight: number): number {
  const ratio = clamp(plotHeight / waveformPreferredPlotHeight, waveformMinimumViewportSpan / waveformDomainSpan, 1);
  return clamp(waveformDomainSpan * ratio, waveformMinimumViewportSpan, waveformDomainSpan);
}

function clampWaveformCenter(center: number, span: number): number {
  const halfSpan = span / 2;
  return clamp(center, waveformMinimumValue + halfSpan, waveformMaximumValue - halfSpan);
}

function normalizeSpectrumViewport(viewport: SpectrumViewport, plotHeight: number): SpectrumViewport {
  const spanMel = clamp(viewport.spanMel, spectrumMinimumViewportSpan, spectrumMelSpan);
  const effectiveSpan = Math.min(spanMel, spectrumLayoutLimitedSpan(plotHeight));
  return {
    centerMel: clampSpectrumCenter(viewport.centerMel, effectiveSpan),
    spanMel: effectiveSpan,
  };
}

function spectrumLayoutLimitedSpan(plotHeight: number): number {
  const ratio = clamp(plotHeight / spectrumPreferredPlotHeight, spectrumMinimumViewportSpan / spectrumMelSpan, 1);
  return clamp(spectrumMelSpan * ratio, spectrumMinimumViewportSpan, spectrumMelSpan);
}

function clampSpectrumCenter(centerMel: number, spanMel: number): number {
  const halfSpan = spanMel / 2;
  return clamp(centerMel, spectrumMinimumMel + halfSpan, spectrumMaximumMel - halfSpan);
}

function spectrumFrequencyAtY(y: number, plotHeight: number, viewport: SpectrumViewport): number {
  const ratio = (plotHeight - y) / Math.max(1, plotHeight);
  const mel = viewport.centerMel - viewport.spanMel / 2 + viewport.spanMel * ratio;
  return clamp(melToHertz(mel), spectrumMinimumFrequencyHz, spectrumMaximumFrequencyHz);
}

function drawSpectrum(
  context: CanvasRenderingContext2D,
  width: number,
  height: number,
  data: Extract<ChartData, { kind: "spectrum" }>,
  spectrumViewport: SpectrumViewport,
): void {
  const plot = spectrumPlotRect(width, height);
  drawSpectrumBackground(context, width, height, plot, spectrumViewport);
  context.save?.();
  clipRect(context, plot.left, plot.top, plot.width, plot.height);

  const viewport = normalizeSpectrumViewport(spectrumViewport, height);
  const viewportMinimum = viewport.centerMel - viewport.spanMel / 2;
  const history = updateSpectrumRollHistory(data);
  const visibleColumns = history.columns.slice(-Math.max(1, Math.floor(plot.width)));
  const columnWidth = Math.max(1, plot.width / Math.max(1, visibleColumns.length));
  visibleColumns.forEach((column, columnIndex) => {
    const x = plot.right - (visibleColumns.length - columnIndex) * columnWidth;
    column.forEach((displayValue, bucketIndex) => {
      const bucketRatio = column.length === 1 ? 0 : bucketIndex / (column.length - 1);
      const bucketMel = spectrumMinimumMel + spectrumMelSpan * bucketRatio;
      const y = plot.bottom - ((bucketMel - viewportMinimum) / viewport.spanMel) * plot.height;
      if (y < plot.top - 2 || y > plot.bottom + 2) {
        return;
      }

      const rowHeight = Math.max(1, plot.height / Math.max(1, column.length - 1));
      context.fillStyle = spectrumPixelColor(displayValue);
      if (columnIndex === 0) {
        context.fillRect(0, y - rowHeight / 2, 0, Math.max(1, rowHeight + 0.4));
      }
      context.fillRect(x, y - rowHeight / 2, Math.max(1, columnWidth + 0.4), Math.max(1, rowHeight + 0.4));
    });
  });
  context.restore?.();
}

function updateSpectrumRollHistory(data: Extract<ChartData, { kind: "spectrum" }>): SpectrumRollHistory {
  const existing = spectrumRollHistories.get(data.sourceKey);
  const bucketCount = data.buckets.length;
  const history =
    existing && existing.bucketCount === bucketCount
      ? existing
      : { frame: -1, bucketCount, columns: [] };

  if (bucketCount > 0 && history.frame !== data.frame) {
    history.frame = data.frame;
    history.columns = [
      ...history.columns,
      normalizeSpectrumColumn(data.buckets, data.floorDb, data.ceilingDb),
    ].slice(-spectrumRollMaximumColumns);
  }

  spectrumRollHistories.set(data.sourceKey, history);
  return history;
}

function normalizeSpectrumColumn(buckets: readonly number[], floorDb: number, ceilingDb: number): number[] {
  const floorLevel = dbToDisplayLevel(Math.min(floorDb, ceilingDb));
  const ceilingLevel = dbToDisplayLevel(Math.max(floorDb, ceilingDb));
  const levelSpan = Math.max(0.000001, ceilingLevel - floorLevel);
  return buckets.map((bucket) => clamp((clamp(bucket, 0, 1) - floorLevel) / levelSpan, 0, 1));
}

function spectrumPixelColor(value: number): string {
  const displayValue = clamp(value, 0, 1);
  const alpha = 0.08 + Math.pow(displayValue, 0.72) * 0.78;
  if (displayValue > 0.8) {
    return `rgba(255, 176, 0, ${alpha})`;
  }

  return `rgba(46, 196, 182, ${alpha})`;
}

function spectrumPlotRect(width: number, height: number): { left: number; top: number; width: number; height: number; right: number; bottom: number } {
  const left = 42;
  const top = 9;
  const plotWidth = Math.max(1, width - 52);
  const plotHeight = Math.max(1, height - 20);
  return {
    left,
    top,
    width: plotWidth,
    height: plotHeight,
    right: left + plotWidth,
    bottom: top + plotHeight,
  };
}

function drawSpectrumBackground(
  context: CanvasRenderingContext2D,
  width: number,
  height: number,
  plot: ReturnType<typeof spectrumPlotRect>,
  spectrumViewport: SpectrumViewport,
): void {
  context.fillStyle = chartTrackColor;
  context.fillRect(0, 0, width, height);
  context.strokeStyle = chartBorderColor;
  context.lineWidth = 1;
  context.beginPath();
  context.moveTo(0.5, 0.5);
  context.lineTo(Math.max(1, width - 0.5), 0.5);
  context.lineTo(Math.max(1, width - 0.5), Math.max(1, height - 0.5));
  context.lineTo(0.5, Math.max(1, height - 0.5));
  context.lineTo(0.5, 0.5);
  context.stroke();

  const viewport = normalizeSpectrumViewport(spectrumViewport, plot.height);
  spectrumMinorFrequencies.forEach((frequency) => drawSpectrumFrequencyTick(context, plot, viewport, frequency, false));
  spectrumMajorFrequencies.forEach((frequency) => drawSpectrumFrequencyTick(context, plot, viewport, frequency, true));

  context.strokeStyle = chartSubtleColor;
  for (let index = 1; index < 4; index += 1) {
    const x = plot.left + (plot.width * index) / 4;
    context.beginPath();
    context.moveTo(x, plot.top);
    context.lineTo(x, plot.bottom);
    context.stroke();
  }
}

function drawSpectrumFrequencyTick(
  context: CanvasRenderingContext2D,
  plot: ReturnType<typeof spectrumPlotRect>,
  viewport: SpectrumViewport,
  frequency: number,
  major: boolean,
): void {
  const y = plot.bottom - ((hertzToMel(frequency) - (viewport.centerMel - viewport.spanMel / 2)) / viewport.spanMel) * plot.height;
  if (y < plot.top || y > plot.bottom) {
    return;
  }

  context.strokeStyle = major ? chartSubtleStrongColor : chartSubtleColor;
  context.beginPath();
  context.moveTo(plot.left, y);
  context.lineTo(plot.right, y);
  context.stroke();
  if (major && typeof context.fillText === "function") {
    context.fillStyle = chartTextMutedColor;
    context.font = "10px Segoe UI, sans-serif";
    context.fillText(formatFrequencyAxis(frequency), 7, y - 3);
  }
}

function clipRect(context: CanvasRenderingContext2D, left: number, top: number, width: number, height: number): void {
  if (typeof context.clip !== "function") {
    return;
  }

  context.beginPath();
  context.moveTo(left, top);
  context.lineTo(left + width, top);
  context.lineTo(left + width, top + height);
  context.lineTo(left, top + height);
  context.lineTo(left, top);
  context.clip();
}

function formatFrequencyAxis(frequency: number): string {
  return frequency >= 1000 ? `${frequency / 1000 >= 10 ? Math.round(frequency / 1000) : frequency / 1000}k` : `${frequency}`;
}

function drawStereo(context: CanvasRenderingContext2D, width: number, height: number, points: StereoPoint[]): void {
  const centerX = width / 2;
  const baseY = height - 20;
  const radius = Math.max(24, Math.min(width / 2 - 14, height - 34));
  drawFanSurface(context, centerX, baseY, radius);
  context.strokeStyle = chartSubtleStrongColor;
  context.lineWidth = 1;
  [0.33, 0.66, 1].forEach((ratio) => {
    context.beginPath();
    context.arc(centerX, baseY, radius * ratio, Math.PI, Math.PI * 2);
    context.stroke();
  });
  for (let degrees = 0; degrees <= 180; degrees += 30) {
    const angle = Math.PI - (Math.PI * degrees) / 180;
    context.beginPath();
    context.moveTo(centerX, baseY);
    context.lineTo(centerX + Math.cos(angle) * radius, baseY - Math.sin(angle) * radius);
    context.stroke();
  }

  if (typeof context.fillText === "function") {
    context.fillStyle = chartTextMutedColor;
    context.font = "10px Segoe UI, sans-serif";
    context.fillText("L", centerX - radius - 3, baseY + 13);
    context.fillText("M", centerX - 4, baseY - radius - 8);
    context.fillText("R", centerX + radius - 7, baseY + 13);
  }

  const sourcePoints = points.length > 0 ? points : [{ x: 0.5, y: 0.58, intensity: 0.5 }];
  sourcePoints.forEach((point) => {
    const x = centerX + (point.x - 0.5) * 2 * radius;
    const y = baseY - clamp(1 - point.y, 0.05, 0.98) * radius;
    const distance = Math.hypot(x - centerX, baseY - y);
    const clampRatio = distance > radius ? radius / distance : 1;
    const projectedX = centerX + (x - centerX) * clampRatio;
    const projectedY = baseY - (baseY - y) * clampRatio;
    const intensity = clamp(point.intensity, 0, 1);
    context.fillStyle = `rgba(46, 196, 182, ${0.12 + intensity * 0.28})`;
    context.beginPath();
    context.arc(projectedX, projectedY, 7 + intensity * 4, 0, Math.PI * 2);
    context.fill();
    context.fillStyle = `rgba(46, 196, 182, ${0.42 + intensity * 0.48})`;
    context.beginPath();
    context.arc(projectedX, projectedY, 2.5 + intensity * 2.2, 0, Math.PI * 2);
    context.fill();
  });
}

function drawFanSurface(context: CanvasRenderingContext2D, centerX: number, baseY: number, radius: number): void {
  context.beginPath();
  context.moveTo(centerX, baseY);
  context.arc(centerX, baseY, radius, Math.PI, Math.PI * 2);
  context.lineTo(centerX, baseY);
  context.fillStyle = "rgba(46, 196, 182, 0.12)";
  context.fill();
  context.strokeStyle = chartSubtleStrongColor;
  context.lineWidth = 1;
  context.stroke();
}

function drawLoudnessClock(
  context: CanvasRenderingContext2D,
  width: number,
  height: number,
  data: Extract<ChartData, { kind: "loudnessClock" }>,
): void {
  const centerX = width / 2;
  const centerY = height / 2;
  const radius = Math.min(width, height) * 0.38;
  const innerRadius = radius * 0.18;
  const minimumDb = data.minimumDb;
  const maximumDb = data.maximumDb <= minimumDb ? minimumDb + 1 : data.maximumDb;
  context.strokeStyle = chartSubtleColor;
  context.lineWidth = 1;
  [0.25, 0.5, 0.75, 1].forEach((ratio, index) => {
    context.beginPath();
    context.arc(centerX, centerY, innerRadius + (radius - innerRadius) * ratio, 0, Math.PI * 2);
    context.strokeStyle = index === 0 ? chartSubtleStrongColor : chartSubtleColor;
    context.stroke();
  });

  const warningRatio = clamp((data.warningDb - minimumDb) / (maximumDb - minimumDb), 0, 1);
  if (warningRatio > 0 && warningRatio < 1) {
    context.strokeStyle = "rgba(255, 176, 0, 0.5)";
    context.beginPath();
    context.arc(centerX, centerY, innerRadius + (radius - innerRadius) * warningRatio, 0, Math.PI * 2);
    context.stroke();
  }

  context.strokeStyle = chartSubtleStrongColor;
  for (let index = 0; index < 12; index += 1) {
    const angle = (index / 12) * Math.PI * 2 - Math.PI / 2;
    context.beginPath();
    context.moveTo(centerX + Math.cos(angle) * (radius - 10), centerY + Math.sin(angle) * (radius - 10));
    context.lineTo(centerX + Math.cos(angle) * radius, centerY + Math.sin(angle) * radius);
    context.stroke();
  }

  if (typeof context.fillText === "function") {
    context.fillStyle = chartTextMutedColor;
    context.font = "10px Segoe UI, sans-serif";
    [0, 5, 10, 15, 20, 25].forEach((seconds) => {
      const angle = (seconds / 30) * Math.PI * 2 - Math.PI / 2;
      context.fillText(`${seconds}s`, centerX + Math.cos(angle) * (radius - 26) - 6, centerY + Math.sin(angle) * (radius - 26) + 4);
    });
    [-32, -25, -18, -11, -4].forEach((mark) => {
      if (mark < minimumDb || mark > maximumDb) {
        return;
      }
      const markRadius = innerRadius + (radius - innerRadius) * ((mark - minimumDb) / (maximumDb - minimumDb));
      context.fillText(`${mark}`, centerX + markRadius + 4, centerY - 5);
    });
  }

  context.fillStyle = chartPanelColor;
  context.beginPath();
  context.arc(centerX, centerY, innerRadius, 0, Math.PI * 2);
  context.fill();

  drawLoudnessFan(context, centerX, centerY, innerRadius, radius, minimumDb, maximumDb, data.warningDb, data.points);
  context.strokeStyle = chartAccentColor;
  context.lineWidth = 1.8;
  context.beginPath();
  data.points.forEach((point, index) => {
    const angle = point.phase * Math.PI * 2 - Math.PI / 2;
    const loudnessRatio = clamp((point.loudness - minimumDb) / (maximumDb - minimumDb), 0, 1);
    const pointRadius = innerRadius + (radius - innerRadius) * loudnessRatio;
    const x = centerX + Math.cos(angle) * pointRadius;
    const y = centerY + Math.sin(angle) * pointRadius;
    if (index === 0) {
      context.moveTo(x, y);
    } else {
      context.lineTo(x, y);
    }
  });
  context.stroke();
}

function drawLoudnessFan(
  context: CanvasRenderingContext2D,
  centerX: number,
  centerY: number,
  innerRadius: number,
  outerRadius: number,
  minimumDb: number,
  maximumDb: number,
  warningDb: number,
  points: Array<{ loudness: number; phase: number }>,
): void {
  if (points.length < 2) {
    return;
  }

  for (let index = 1; index < points.length; index += 1) {
    const first = points[index - 1];
    const second = points[index];
    const firstAngle = first.phase * Math.PI * 2 - Math.PI / 2;
    const secondAngle = second.phase * Math.PI * 2 - Math.PI / 2;
    if (Math.abs(secondAngle - firstAngle) > Math.PI) {
      continue;
    }

    const firstRadius = innerRadius + (outerRadius - innerRadius) * clamp((first.loudness - minimumDb) / (maximumDb - minimumDb), 0, 1);
    const secondRadius = innerRadius + (outerRadius - innerRadius) * clamp((second.loudness - minimumDb) / (maximumDb - minimumDb), 0, 1);
    const isWarning = first.loudness >= warningDb || second.loudness >= warningDb;
    context.fillStyle = isWarning ? "rgba(255, 176, 0, 0.28)" : "rgba(46, 196, 182, 0.18)";
    context.beginPath();
    context.moveTo(centerX + Math.cos(firstAngle) * innerRadius, centerY + Math.sin(firstAngle) * innerRadius);
    context.lineTo(centerX + Math.cos(firstAngle) * firstRadius, centerY + Math.sin(firstAngle) * firstRadius);
    context.lineTo(centerX + Math.cos(secondAngle) * secondRadius, centerY + Math.sin(secondAngle) * secondRadius);
    context.lineTo(centerX + Math.cos(secondAngle) * innerRadius, centerY + Math.sin(secondAngle) * innerRadius);
    context.lineTo(centerX + Math.cos(firstAngle) * innerRadius, centerY + Math.sin(firstAngle) * innerRadius);
    context.fill();
  }
}

function drawCollision(context: CanvasRenderingContext2D, width: number, height: number, bands: CollisionBandGeometry[]): void {
  const trackTop = 10.5;
  const trackHeight = 18;
  context.strokeStyle = chartBorderColor;
  context.lineWidth = 1;
  context.fillStyle = chartTrackColor;
  context.fillRect(0, 0, width, height);
  context.beginPath();
  context.moveTo(0.5, trackTop);
  context.lineTo(Math.max(1, width - 0.5), trackTop);
  context.lineTo(Math.max(1, width - 0.5), trackTop + trackHeight);
  context.lineTo(0.5, trackTop + trackHeight);
  context.lineTo(0.5, trackTop);
  context.stroke();
  [20, 100, 1000, 4000, 12000, 22000].forEach((frequency) => {
    const ratio = collisionFrequencyRatio(frequency);
    const x = 0.5 + (width - 1) * ratio;
    context.strokeStyle = chartSubtleColor;
    context.beginPath();
    context.moveTo(x, trackTop);
    context.lineTo(x, trackTop + trackHeight + 4);
    context.stroke();
    if (typeof context.fillText === "function") {
      context.fillStyle = chartTextMutedColor;
      context.font = "10px Segoe UI, sans-serif";
      context.fillText(formatFrequencyAxis(frequency), Math.min(width - 18, Math.max(0, x - 8)), trackTop + trackHeight + 16);
    }
  });

  bands.forEach((band) => {
    const intensity = clamp(band.intensity, 0, 1);
    context.fillStyle = intensity > 0.9
      ? `rgba(${chartDangerColor}, ${0.34 + intensity * 0.48})`
      : intensity > 0.72
      ? `rgba(255, 176, 0, ${0.32 + intensity * 0.5})`
      : `rgba(46, 196, 182, ${0.24 + intensity * 0.44})`;
    context.fillRect(band.startRatio * width, trackTop + 2, Math.max(2, (band.endRatio - band.startRatio) * width), trackHeight - 4);
  });
}

function collisionFrequencyRatio(frequency: number): number {
  return clamp((Math.log10(frequency) - Math.log10(20)) / (Math.log10(22000) - Math.log10(20)), 0, 1);
}
