import { type DragEvent as ReactDragEvent, type KeyboardEvent, useEffect, useMemo, useRef, useState } from "react";
import { Shirt } from "lucide-react";
import { ChartModuleCanvas } from "./charts/ChartModuleCanvas";
import { createHostStateStore, type HostBridgeLike } from "./state/hostState";
import { focusFirstNativeMenuItem, handleNativeMenuKeyboard } from "./ui/nativeMenuKeyboard";
import { NativeComboBox } from "./ui/NativeComboBox";
import type { HostDetailSnapshotDto, HostRecentEventSnapshotDto, HostSnapshotDto, HostSourceSnapshotDto } from "./bridge/hostBridge";

interface AppShellProps {
  bridge: HostBridgeLike;
}

const NORMAL_REFRESH_INTERVAL_MS = 33;
const ECO_REFRESH_INTERVAL_MS = 100;
const recentEventFilterOptions = [
  { value: "all", label: "All" },
  { value: "source", label: "Sources" },
  { value: "truePeak", label: "Peaks" },
  { value: "loudness", label: "Loudness" },
  { value: "collision", label: "Overlap" },
];
const displayModeOptions = [
  { value: "peak", label: "Peak" },
  { value: "lufs", label: "LUFS" },
];
const truePeakOversamplingOptions = [
  { value: "twoX", label: "2x" },
  { value: "fourX", label: "4x" },
  { value: "eightX", label: "8x" },
];
type SourceDropPosition = "before" | "after";
type ModuleDropPosition = "before" | "after";
interface SourceDropHint {
  sourceKey: string;
  position: SourceDropPosition;
}
interface ModuleDropHint {
  moduleKind: string;
  position: ModuleDropPosition;
}

export function AppShell({ bridge }: AppShellProps) {
  const store = useMemo(() => createHostStateStore(bridge), [bridge]);
  const [state, setState] = useState(store.getSnapshot());
  const [displayMode, setDisplayMode] = useState("peak");
  const [isTruePeakEnabled, setTruePeakEnabled] = useState(false);
  const [recentEventFilter, setRecentEventFilter] = useState("all");
  const [isEcoModeEnabled, setEcoModeEnabled] = useState(false);
  const [isAlwaysOnTop, setAlwaysOnTop] = useState(true);
  const [isThemeMenuOpen, setThemeMenuOpen] = useState(false);
  const themeMenuRef = useRef<HTMLDivElement | null>(null);
  const refreshIntervalMs = isEcoModeEnabled ? ECO_REFRESH_INTERVAL_MS : NORMAL_REFRESH_INTERVAL_MS;

  useEffect(() => {
    let isMounted = true;
    let isRefreshInFlight = false;
    store.loadSnapshot().then(() => {
      if (isMounted) {
        setState(store.getSnapshot());
      }
    });

    const intervalId = window.setInterval(() => {
      if (isRefreshInFlight) {
        return;
      }

      isRefreshInFlight = true;
      store
        .sendCommand("refresh")
        .then(() => {
          if (isMounted) {
            setState(store.getSnapshot());
          }
        })
        .finally(() => {
          isRefreshInFlight = false;
        });
    }, refreshIntervalMs);

    return () => {
      isMounted = false;
      window.clearInterval(intervalId);
    };
  }, [store, refreshIntervalMs]);

  useEffect(() => {
    if (!isThemeMenuOpen) {
      return;
    }

    focusFirstNativeMenuItem(themeMenuRef.current?.querySelector<HTMLElement>('[role="menu"]') ?? null);

    function handlePointerDown(event: PointerEvent) {
      if (event.target instanceof Node && themeMenuRef.current?.contains(event.target)) {
        return;
      }
      setThemeMenuOpen(false);
    }

    function handleKeyDown(event: globalThis.KeyboardEvent) {
      if (event.key === "Escape") {
        setThemeMenuOpen(false);
      }
    }

    window.addEventListener("pointerdown", handlePointerDown);
    window.addEventListener("keydown", handleKeyDown);
    return () => {
      window.removeEventListener("pointerdown", handlePointerDown);
      window.removeEventListener("keydown", handleKeyDown);
    };
  }, [isThemeMenuOpen]);

  async function run(action: () => Promise<void>): Promise<void> {
    const pending = action();
    setState(store.getSnapshot());
    await pending;
    setState(store.getSnapshot());
  }

  function sendChromeCommand(command: string): void {
    void bridge.sendCommand(command).finally(() => setState(store.getSnapshot()));
  }

  const snapshot = state.snapshot;
  const themeId = resolveThemeId(snapshot?.settings.themeId);
  const selectedThemeName = themeOptions.find((theme) => theme.id === themeId)?.name ?? themeOptions[0].name;
  const selectedDetail = snapshot?.selectedDetail ?? null;
  const selectedSourceSettings = selectedDetail
    ? snapshot?.settings.sources.find((source) => source.sourceKey === selectedDetail.source.sourceKey)
    : null;

  return (
    <main className="app-shell" data-theme={themeId}>
      <section className="app-chrome" aria-label="App chrome">
        <div
          className="chrome-title"
          onMouseDown={(event) => {
            if (event.button !== 0) {
              return;
            }

            event.preventDefault();
            sendChromeCommand(event.detail >= 2 ? "toggleWindowState" : "beginWindowDrag");
          }}
        >
          <h1>uMeter</h1>
          <p>WASAPI / VST3 source metering</p>
        </div>
        <div className="chrome-actions">
          <span className="theme-menu-wrap" ref={themeMenuRef}>
            <button
              type="button"
              className="chrome-icon-button"
              aria-label={`Theme: ${selectedThemeName}`}
              aria-haspopup="menu"
              aria-expanded={isThemeMenuOpen}
              title={`Theme: ${selectedThemeName}`}
              onClick={() => setThemeMenuOpen((isOpen) => !isOpen)}
            >
              <Shirt aria-hidden="true" size={16} />
            </button>
            {isThemeMenuOpen ? (
              <div className="theme-menu" role="menu" aria-label="Theme" onKeyDown={handleNativeMenuKeyboard}>
                {themeOptions.map((theme) => (
                  <button
                    key={theme.id}
                    type="button"
                    className="theme-menu-item"
                    role="menuitem"
                    onClick={() => {
                      setThemeMenuOpen(false);
                      void run(() => store.sendCommand("setTheme", { themeId: theme.id }));
                    }}
                  >
                    {theme.name}
                  </button>
                ))}
              </div>
            ) : null}
          </span>
          <button
            type="button"
            className="chrome-icon-button mdl2-icon-button"
            aria-label="Settings"
            title="Global settings"
            onClick={() => run(() => store.sendCommand("openGlobalSettings"))}
          >
            <span aria-hidden="true">&#xE713;</span>
          </button>
          <button
            type="button"
            className="chrome-text-button"
            aria-label="Hide to tray"
            title="Hide to tray"
            onClick={() => run(() => store.sendCommand("hideToTray"))}
          >
            -
          </button>
          <button
            type="button"
            className="chrome-text-button"
            aria-label="Close"
            title="Close"
            onClick={() => run(() => store.sendCommand("closeWindow"))}
          >
            X
          </button>
        </div>
      </section>

      {state.errorMessage ? <div className="error-banner">{state.errorMessage}</div> : null}

      {selectedDetail ? (
        <DetailView
          detail={selectedDetail}
          sources={snapshot?.sources ?? []}
          sourceSettings={selectedSourceSettings}
          canForgetSource={Boolean(snapshot?.commands.canForgetSelectedSource)}
          run={run}
          store={store}
        />
      ) : (
        <GlobalView
          snapshot={snapshot}
          refreshIntervalMs={refreshIntervalMs}
          recentEventFilter={recentEventFilter}
          setRecentEventFilter={setRecentEventFilter}
          displayMode={displayMode}
          setDisplayMode={setDisplayMode}
          isTruePeakEnabled={isTruePeakEnabled}
          setTruePeakEnabled={setTruePeakEnabled}
          isEcoModeEnabled={isEcoModeEnabled}
          setEcoModeEnabled={setEcoModeEnabled}
          isAlwaysOnTop={isAlwaysOnTop}
          setAlwaysOnTop={setAlwaysOnTop}
          run={run}
          store={store}
        />
      )}
    </main>
  );
}

function GlobalView({
  snapshot,
  refreshIntervalMs,
  recentEventFilter,
  setRecentEventFilter,
  displayMode,
  setDisplayMode,
  isTruePeakEnabled,
  setTruePeakEnabled,
  isEcoModeEnabled,
  setEcoModeEnabled,
  isAlwaysOnTop,
  setAlwaysOnTop,
  run,
  store,
}: {
  snapshot: HostSnapshotDto | null;
  refreshIntervalMs: number;
  recentEventFilter: string;
  setRecentEventFilter: (filter: string) => void;
  displayMode: string;
  setDisplayMode: (displayMode: string) => void;
  isTruePeakEnabled: boolean;
  setTruePeakEnabled: (isEnabled: boolean) => void;
  isEcoModeEnabled: boolean;
  setEcoModeEnabled: (isEnabled: boolean) => void;
  isAlwaysOnTop: boolean;
  setAlwaysOnTop: (isAlwaysOnTop: boolean) => void;
  run: (action: () => Promise<void>) => Promise<void>;
  store: ReturnType<typeof createHostStateStore>;
}) {
  const visibleRecentEvents = filterRecentEvents(snapshot?.recentEvents ?? [], recentEventFilter);
  const emptyRecentMessage = snapshot?.recentEvents.length ? "No matching events" : "No recent events";
  const [draggedSourceKey, setDraggedSourceKey] = useState<string | null>(null);
  const [sourceDropHint, setSourceDropHint] = useState<SourceDropHint | null>(null);
  const sources = snapshot?.sources ?? [];

  function updateSourceDropHint(targetSourceKey: string, event: ReactDragEvent<HTMLElement>): void {
    if (!draggedSourceKey || draggedSourceKey === targetSourceKey) {
      setSourceDropHint(null);
      return;
    }

    event.preventDefault();
    event.dataTransfer.dropEffect = "move";
    setSourceDropHint({
      sourceKey: targetSourceKey,
      position: getSourceDropPosition(event, sources, draggedSourceKey, targetSourceKey),
    });
  }

  async function dropSource(targetSourceKey: string, event: ReactDragEvent<HTMLElement>): Promise<void> {
    if (!draggedSourceKey || draggedSourceKey === targetSourceKey) {
      setDraggedSourceKey(null);
      setSourceDropHint(null);
      return;
    }

    event.preventDefault();
    const position =
      sourceDropHint?.sourceKey === targetSourceKey
        ? sourceDropHint.position
        : getSourceDropPosition(event, sources, draggedSourceKey, targetSourceKey);
    setDraggedSourceKey(null);
    setSourceDropHint(null);
    await moveSourceToDropPosition(sources, draggedSourceKey, targetSourceKey, position, run, store);
  }

  return (
    <div className="global-view">
      <div className="global-content">
        <section className="status-strip" aria-label="Status">
          <span>{snapshot?.statusText ?? "Waiting for HostBridge"}</span>
          <strong>{refreshIntervalMs} ms</strong>
        </section>

        <section className="control-panel" aria-label="Status and controls">
          <div className="control-row">
            <label className="field">
              <span>Mode</span>
              <NativeComboBox
                ariaLabel="Mode"
                value={displayMode}
                options={displayModeOptions}
                onChange={(next) => {
                  setDisplayMode(next);
                  void run(() => store.sendCommand("setDisplayMode", { displayMode: next }));
                }}
              />
            </label>
            <div className="field true-peak-field">
              <span>True Peak</span>
              <span className="inline-field">
                <input
                  type="checkbox"
                  aria-label="Enable true peak"
                  checked={isTruePeakEnabled}
                  onChange={(event) => {
                    const next = event.currentTarget.checked;
                    setTruePeakEnabled(next);
                    void run(() => store.sendCommand("setTruePeakEnabled", { truePeakEnabled: next }));
                  }}
                />
                <NativeComboBox
                  ariaLabel="True Peak oversampling"
                  value="fourX"
                  disabled={!isTruePeakEnabled}
                  options={truePeakOversamplingOptions}
                  onChange={(next) => applyTruePeakToAll(snapshot, store, run, next)}
                />
              </span>
            </div>
            <div className="field performance-field">
              <span>Performance</span>
              <label className="check-line">
                <input
                  type="checkbox"
                  checked={isEcoModeEnabled}
                  onChange={(event) => {
                    const next = event.currentTarget.checked;
                    setEcoModeEnabled(next);
                    void run(() =>
                      store.sendCommand("setPerformanceMode", {
                        performanceMode: next ? "eco" : "normal",
                      }),
                    );
                  }}
                />
                Eco mode
              </label>
            </div>
          </div>
          <div className="control-row lower">
            <div className="control-check-group">
              <label className="check-line">
                <input
                  type="checkbox"
                  checked={isAlwaysOnTop}
                  onChange={(event) => {
                    const next = event.currentTarget.checked;
                    setAlwaysOnTop(next);
                    void run(() => store.sendCommand("setAlwaysOnTop", { isAlwaysOnTop: next }));
                  }}
                />
                Always on top
              </label>
              <label className="check-line show-hidden-check">
                <input
                  type="checkbox"
                  checked={snapshot?.settings.showHiddenSources ?? false}
                  onChange={() =>
                    run(() =>
                      store.sendCommand("setShowHiddenSources", {
                        showHiddenSources: !(snapshot?.settings.showHiddenSources ?? false),
                      }),
                    )
                  }
                />
                Show hidden
              </label>
            </div>
            <button type="button" className="native-button reset-all" onClick={() => run(() => store.sendCommand("resetAll"))}>
              Reset All
            </button>
          </div>
        </section>

        <section className="recent-card" aria-label="Recent events">
          <div className="native-section-heading">
            <h2>Recent</h2>
            <NativeComboBox
              ariaLabel="Recent event filter"
              value={recentEventFilter}
              options={recentEventFilterOptions}
              onChange={setRecentEventFilter}
            />
          </div>
          {visibleRecentEvents.length ? (
            <ul className="recent-list">
              {visibleRecentEvents.map((event) => (
                <li key={`${event.timeText}-${event.title}-${event.detail}`}>
                  <time>{event.timeText}</time>
                  <span className={`event-dot ${event.severity}`} />
                  <span className="recent-event-copy">
                    <strong>{event.title}</strong>
                    <span>{event.detail}</span>
                  </span>
                </li>
              ))}
            </ul>
          ) : (
            <div className="empty-recent">{emptyRecentMessage}</div>
          )}
        </section>

        <section className="source-stack" aria-label="Sources">
          {sources.length ? (
            sources.map((source, index) => (
              <SourceCard
                key={source.sourceKey}
                source={source}
                displayMode={displayMode}
                canMoveUp={index > 0}
                canMoveDown={index < sources.length - 1}
                isDragging={draggedSourceKey === source.sourceKey}
                dropHint={sourceDropHint?.sourceKey === source.sourceKey ? sourceDropHint.position : null}
                onDragStart={(event) => {
                  setDraggedSourceKey(source.sourceKey);
                  setSourceDropHint(null);
                  event.dataTransfer.effectAllowed = "move";
                  event.dataTransfer.setData("text/plain", source.sourceKey);
                }}
                onDragOver={(event) => updateSourceDropHint(source.sourceKey, event)}
                onDragLeave={(event) => {
                  if (event.currentTarget.contains(event.relatedTarget as Node | null)) {
                    return;
                  }
                  setSourceDropHint((current) => (current?.sourceKey === source.sourceKey ? null : current));
                }}
                onDrop={(event) => void dropSource(source.sourceKey, event)}
                onDragEnd={() => {
                  setDraggedSourceKey(null);
                  setSourceDropHint(null);
                }}
                run={run}
                store={store}
              />
            ))
          ) : (
            <div className="source-card empty-source">No active audio sources</div>
          )}
        </section>
      </div>

      <p className="warmup-note">Per-process LUFS metrics warm up after active audio starts.</p>
    </div>
  );
}

function getSourceDropPosition(
  event: ReactDragEvent<HTMLElement>,
  sources: readonly HostSourceSnapshotDto[],
  sourceKey: string,
  targetSourceKey: string,
): SourceDropPosition {
  const rect = event.currentTarget.getBoundingClientRect();
  if (Number.isFinite(event.clientY) && event.clientY >= rect.top && event.clientY <= rect.bottom) {
    return getVerticalDropPosition(event, rect);
  }

  const sourceIndex = sources.findIndex((source) => source.sourceKey === sourceKey);
  const targetIndex = sources.findIndex((source) => source.sourceKey === targetSourceKey);
  return sourceIndex >= 0 && targetIndex >= sourceIndex ? "after" : "before";
}

function getVerticalDropPosition(event: ReactDragEvent<HTMLElement>, rect = event.currentTarget.getBoundingClientRect()): SourceDropPosition {
  return event.clientY >= rect.top + rect.height / 2 ? "after" : "before";
}

async function moveSourceToDropPosition(
  sources: readonly HostSourceSnapshotDto[],
  sourceKey: string,
  targetSourceKey: string,
  position: SourceDropPosition,
  run: (action: () => Promise<void>) => Promise<void>,
  store: ReturnType<typeof createHostStateStore>,
): Promise<void> {
  const sourceIndex = sources.findIndex((source) => source.sourceKey === sourceKey);
  const targetIndex = sources.findIndex((source) => source.sourceKey === targetSourceKey);
  if (sourceIndex < 0 || targetIndex < 0) {
    return;
  }

  const reordered = sources.map((source) => source.sourceKey);
  const [removed] = reordered.splice(sourceIndex, 1);
  const adjustedTargetIndex = reordered.indexOf(targetSourceKey);
  const destinationIndex = position === "after" ? adjustedTargetIndex + 1 : adjustedTargetIndex;
  reordered.splice(destinationIndex, 0, removed);
  const finalIndex = reordered.indexOf(sourceKey);
  if (sourceIndex === finalIndex) {
    return;
  }

  const direction = sourceIndex < finalIndex ? "down" : "up";
  const steps = Math.abs(finalIndex - sourceIndex);
  for (let step = 0; step < steps; step += 1) {
    await run(() => store.sendCommand("moveSource", { sourceKey, moveDirection: direction }));
  }
}

function DetailView({
  detail,
  sources,
  sourceSettings,
  canForgetSource,
  run,
  store,
}: {
  detail: HostDetailSnapshotDto;
  sources: HostSnapshotDto["sources"];
  sourceSettings: HostSnapshotDto["settings"]["sources"][number] | null | undefined;
  canForgetSource: boolean;
  run: (action: () => Promise<void>) => Promise<void>;
  store: ReturnType<typeof createHostStateStore>;
}) {
  const source = detail.source;
  const detailModules = detail.modules;
  const switchTargets = sources.filter((item) => item.sourceKey !== source.sourceKey && !item.isInstanceMissing);
  const [isSwitchMenuOpen, setSwitchMenuOpen] = useState(false);
  const [draggedModuleKind, setDraggedModuleKind] = useState<string | null>(null);
  const [moduleDropHint, setModuleDropHint] = useState<ModuleDropHint | null>(null);
  const switchMenuRef = useRef<HTMLDivElement | null>(null);

  function updateModuleDropHint(moduleKind: string, event: ReactDragEvent<HTMLElement>): void {
    if (!draggedModuleKind || draggedModuleKind === moduleKind) {
      setModuleDropHint(null);
      return;
    }

    event.preventDefault();
    event.dataTransfer.dropEffect = "move";
    setModuleDropHint({
      moduleKind,
      position: getModuleDropPosition(event, detailModules, draggedModuleKind, moduleKind),
    });
  }

  async function dropModule(targetModuleKind: string, event: ReactDragEvent<HTMLElement>): Promise<void> {
    const moduleKind = draggedModuleKind ?? event.dataTransfer.getData("application/x-umeter-detail-module");
    if (!moduleKind || moduleKind === targetModuleKind) {
      setDraggedModuleKind(null);
      setModuleDropHint(null);
      return;
    }

    event.preventDefault();
    const position = getModuleDropPosition(event, detailModules, moduleKind, targetModuleKind);
    setDraggedModuleKind(null);
    setModuleDropHint(null);
    await run(() =>
      store.sendCommand("moveDetailModule", {
        sourceKey: source.sourceKey,
        moduleKind,
        targetModuleKind,
        insertAfter: position === "after",
      }),
    );
  }

  useEffect(() => {
    if (!isSwitchMenuOpen) {
      return;
    }

    focusFirstNativeMenuItem(switchMenuRef.current?.querySelector<HTMLElement>('[role="menu"]') ?? null);

    function handlePointerDown(event: PointerEvent) {
      if (event.target instanceof Node && switchMenuRef.current?.contains(event.target)) {
        return;
      }
      setSwitchMenuOpen(false);
    }

    function handleKeyDown(event: globalThis.KeyboardEvent) {
      if (event.key === "Escape") {
        setSwitchMenuOpen(false);
      }
    }

    window.addEventListener("pointerdown", handlePointerDown);
    window.addEventListener("keydown", handleKeyDown);
    return () => {
      window.removeEventListener("pointerdown", handlePointerDown);
      window.removeEventListener("keydown", handleKeyDown);
    };
  }, [isSwitchMenuOpen]);

  return (
    <section className="detail-view" aria-label="Detail view">
      <div className="detail-toolbar">
        <button type="button" className="native-button detail-back" onClick={() => run(() => store.sendCommand("backToGlobal"))}>
          Back
        </button>
        <div className="detail-actions">
          <button
            type="button"
            className="chrome-icon-button mdl2-icon-button"
            aria-label="Save detail layout"
            title="Save detail layout"
            onClick={() => run(() => store.sendCommand("saveDetailLayout"))}
          >
            <span aria-hidden="true">&#xE74E;</span>
          </button>
          <button
            type="button"
            className="chrome-icon-button mdl2-icon-button detail-layout-load"
            aria-label="Load detail layout"
            title="Load detail layout"
            onClick={() => run(() => store.sendCommand("loadDetailLayout"))}
          >
            <span aria-hidden="true">&#xE8E5;</span>
          </button>
          <span>True Peak</span>
          <NativeComboBox
            ariaLabel="Detail true peak oversampling"
            value={source.truePeakOversampling}
            options={truePeakOversamplingOptions}
            onChange={(next) =>
              run(() =>
                store.sendCommand("setTruePeakOversampling", {
                  sourceKey: source.sourceKey,
                  sourceKind: source.sourceKind,
                  processId: source.processId ?? null,
                  truePeakOversampling: next,
                }),
              )
            }
          />
          <button
            type="button"
            className="native-button detail-forget"
            disabled={!canForgetSource}
            title="Forget saved alias and detail settings for this source."
            onClick={() => run(() => store.sendCommand("forgetSource", { sourceKey: source.sourceKey }))}
          >
            Forget
          </button>
        </div>
      </div>

      <div className={source.isInstanceMissing ? "detail-scroll detail-scroll-missing" : "detail-scroll"}>
        <header className="detail-source-heading">
          <EditableSourceName source={source} className="detail-source-name" run={run} store={store} headingLevel={2} />
          <p>{source.sourceIdentityText}</p>
          <p>{source.state}</p>
        </header>

        <section className="detail-meters" aria-label="Detail meters">
          <DetailMeter label="Momentary loudness" value={formatLufs(source.momentaryLufs)} valueDb={source.momentaryLufs} />
          <DetailMeter label="Short-term loudness" value={formatLufs(source.shortTermLufs)} valueDb={source.shortTermLufs} />
          <DetailMeter label="Integrated loudness" value={formatLufs(source.integratedLufs)} valueDb={source.integratedLufs} />
          <DetailMeter
            label="True-peak level"
            value={formatCompactDb(source.truePeakDb)}
            valueDb={source.truePeakDb}
            scaleMaximum={3}
            ticks={[-60, -48, -36, -24, -12, 0, 3]}
            severity={getDbReadoutSeverity(source.truePeakDb)}
          />
          <DetailMeter label="100 ms RMS level" value={formatDb(source.rms100MsDb)} valueDb={source.rms100MsDb} />
        </section>

        <section className="detail-modules" aria-label="Detail modules">
          {detailModules.map((module) => (
            <ChartModuleCanvas
              key={module.kind}
              detail={detail}
              module={module}
              sources={sources}
              sourceSettings={sourceSettings}
              onWaveformModeChange={(waveformDisplayMode) =>
                void run(() =>
                  store.sendCommand("setWaveformDisplayMode", {
                    sourceKey: source.sourceKey,
                    sourceKind: source.sourceKind,
                    processId: source.processId ?? null,
                    waveformDisplayMode,
                  }),
                )
              }
              onSpectrumModeChange={(spectrumDisplayMode) =>
                void run(() =>
                  store.sendCommand("setSpectrumDisplayMode", {
                    sourceKey: source.sourceKey,
                    sourceKind: source.sourceKind,
                    processId: source.processId ?? null,
                    spectrumDisplayMode,
                  }),
                )
              }
              onSpectrumRangeChange={(spectrumDisplayFloorDb, spectrumDisplayCeilingDb) =>
                void run(() =>
                  store.sendCommand("setSpectrumDisplayRange", {
                    sourceKey: source.sourceKey,
                    spectrumDisplayFloorDb,
                    spectrumDisplayCeilingDb,
                  }),
                )
              }
              onLoudnessClockSettingsChange={(
                loudnessClockMinimumDb,
                loudnessClockMaximumDb,
                loudnessClockWarningDb,
                loudnessClockCycleSeconds,
              ) =>
                void run(() =>
                  store.sendCommand("setLoudnessClockSettings", {
                    sourceKey: source.sourceKey,
                    loudnessClockMinimumDb,
                    loudnessClockMaximumDb,
                    loudnessClockWarningDb,
                    loudnessClockCycleSeconds,
                  }),
                )
              }
              onStereoSettingsOpen={() => void run(() => store.sendCommand("openStereoSettings"))}
              onCollisionSettingsChange={(
                collisionLevelFloorDb,
                collisionProminenceThresholdDb,
                collisionRelativeThreshold,
                collisionFrequencyScale,
                inlineCollisionSourceKey,
              ) =>
                void run(() =>
                  store.sendCommand("setCollisionSettings", {
                    sourceKey: source.sourceKey,
                    collisionLevelFloorDb,
                    collisionProminenceThresholdDb,
                    collisionRelativeThreshold,
                    collisionFrequencyScale,
                    inlineCollisionSourceKey,
                  }),
                )
              }
              onVisibilityChange={(moduleKind, isVisible) =>
                void run(() =>
                  store.sendCommand("setDetailModuleVisibility", {
                    sourceKey: source.sourceKey,
                    moduleKind,
                    isVisible,
                  }),
                )
              }
              onHeightChange={(moduleKind, moduleHeight) =>
                void run(() =>
                  store.sendCommand("setDetailModuleHeight", {
                    sourceKey: source.sourceKey,
                    moduleKind,
                    moduleHeight,
                  }),
                )
              }
              isDragging={draggedModuleKind === module.kind}
              dropHint={moduleDropHint?.moduleKind === module.kind ? moduleDropHint.position : null}
              onDragStart={(moduleKind, event) => {
                event.dataTransfer.effectAllowed = "move";
                event.dataTransfer.setData("application/x-umeter-detail-module", moduleKind);
                setDraggedModuleKind(moduleKind);
                setModuleDropHint(null);
              }}
              onDragOver={(moduleKind, event) => updateModuleDropHint(moduleKind, event)}
              onDragLeave={(moduleKind) => {
                setModuleDropHint((hint) => (hint?.moduleKind === moduleKind ? null : hint));
              }}
              onDrop={(moduleKind, event) => {
                void dropModule(moduleKind, event);
              }}
              onDragEnd={() => {
                setDraggedModuleKind(null);
                setModuleDropHint(null);
              }}
            />
          ))}
        </section>

        {source.isInstanceMissing ? (
          <div className="detail-missing-overlay">
            <div className="detail-missing-panel">
              <strong>Instance not found</strong>
              <div className="detail-switch-menu-wrap" ref={switchMenuRef}>
                <button
                  type="button"
                  className="native-button detail-switch-button"
                  aria-label="Switch detail source"
                  aria-haspopup="menu"
                  aria-expanded={isSwitchMenuOpen}
                  onClick={() => setSwitchMenuOpen((isOpen) => !isOpen)}
                >
                  Switch to...
                </button>
                {isSwitchMenuOpen ? (
                  <div
                    className="detail-switch-menu"
                    role="menu"
                    aria-label="Switch detail source menu"
                    onKeyDown={handleNativeMenuKeyboard}
                  >
                    {switchTargets.map((target) => (
                      <button
                        key={target.sourceKey}
                        type="button"
                        role="menuitem"
                        title={target.sourceIdentityText}
                        onClick={() => {
                          setSwitchMenuOpen(false);
                          void run(() => store.sendCommand("selectDetail", { sourceKey: target.sourceKey }));
                        }}
                      >
                        {target.displayTitle}
                      </button>
                    ))}
                  </div>
                ) : null}
              </div>
            </div>
          </div>
        ) : null}
      </div>
      <button
        type="button"
        className="native-button detail-reset"
        onClick={() =>
          run(() =>
            store.sendCommand("resetSource", {
              sourceKey: source.sourceKey,
              sourceKind: source.sourceKind,
              processId: source.processId ?? null,
            }),
          )
        }
      >
        Reset
      </button>
    </section>
  );
}

function DetailMeter({
  label,
  value,
  valueDb,
  scaleMinimum = -60,
  scaleMaximum = 0,
  ticks = [-60, -48, -36, -24, -12, 0],
  severity = "normal",
}: {
  label: string;
  value: string;
  valueDb: number | null | undefined;
  scaleMinimum?: number;
  scaleMaximum?: number;
  ticks?: readonly number[];
  severity?: string;
}) {
  const level = toScaledPercent(valueDb, scaleMinimum, scaleMaximum);
  const hasLevel = level > 0;

  return (
    <div className="detail-meter" aria-label={`${label}: ${value}`}>
      <span className="detail-meter-label">{label}</span>
      <div className="detail-meter-scale" aria-hidden="true">
        <div className="detail-meter-track">
          {hasLevel ? <span className="detail-meter-fill" style={{ width: `${level}%` }} /> : null}
          {hasLevel ? <span className="detail-meter-marker" style={{ left: `${level}%` }} /> : null}
        </div>
        {ticks.map((tick) => {
          const position = toScaledPercent(tick, scaleMinimum, scaleMaximum);
          return (
            <span key={tick} className="detail-meter-tick" style={{ left: `${position}%` }}>
              <span>{formatTick(tick)}</span>
            </span>
          );
        })}
      </div>
      <strong className={`detail-meter-value ${severity}`}>{value}</strong>
    </div>
  );
}

function getModuleDropPosition(
  event: ReactDragEvent<HTMLElement>,
  modules: readonly HostDetailSnapshotDto["modules"][number][],
  moduleKind: string,
  targetModuleKind: string,
): ModuleDropPosition {
  const rect = event.currentTarget.getBoundingClientRect();
  if (Number.isFinite(event.clientY) && event.clientY >= rect.top && event.clientY <= rect.bottom) {
    return getVerticalDropPosition(event, rect);
  }

  const sourceIndex = modules.findIndex((module) => module.kind === moduleKind);
  const targetIndex = modules.findIndex((module) => module.kind === targetModuleKind);
  return sourceIndex >= 0 && targetIndex >= sourceIndex ? "after" : "before";
}

function SourceCard({
  source,
  displayMode,
  canMoveUp,
  canMoveDown,
  isDragging,
  dropHint,
  onDragStart,
  onDragOver,
  onDragLeave,
  onDrop,
  onDragEnd,
  run,
  store,
}: {
  source: HostSourceSnapshotDto;
  displayMode: string;
  canMoveUp: boolean;
  canMoveDown: boolean;
  isDragging: boolean;
  dropHint: SourceDropPosition | null;
  onDragStart: (event: ReactDragEvent<HTMLElement>) => void;
  onDragOver: (event: ReactDragEvent<HTMLElement>) => void;
  onDragLeave: (event: ReactDragEvent<HTMLElement>) => void;
  onDrop: (event: ReactDragEvent<HTMLElement>) => void;
  onDragEnd: () => void;
  run: (action: () => Promise<void>) => Promise<void>;
  store: ReturnType<typeof createHostStateStore>;
}) {
  return (
    <article
      className={`source-card${isDragging ? " source-card-dragging" : ""}${dropHint ? ` source-drop-${dropHint}` : ""}`}
      onDragOver={onDragOver}
      onDragLeave={onDragLeave}
      onDrop={onDrop}
    >
      <span className="source-drop-hint source-drop-hint-before" aria-hidden="true" />
      <div className="source-card-top">
        <span
          className="source-drag-handle"
          aria-hidden="true"
          title="Drag to reorder"
          draggable
          onDragStart={onDragStart}
          onDragEnd={onDragEnd}
        >
          ::
        </span>
        <span className="source-badge">{source.sourceKindLabel}</span>
        <EditableSourceName source={source} className="source-name" run={run} store={store} />
        <span className={`peak-pill ${getPrimaryValueSeverity(source, displayMode)}`}>{formatPrimaryValue(source, displayMode)}</span>
      </div>
      <div className="meter-track" aria-label={`${source.displayTitle} level`}>
        <span style={{ width: `${toMeterPercent(source)}%` }} />
      </div>
      <div className="source-actions">
        <button
          type="button"
          className="mini-button source-move-button"
          disabled={!canMoveUp}
          aria-label="Move source up"
          title="Move up"
          onClick={() => run(() => store.sendCommand("moveSource", { sourceKey: source.sourceKey, moveDirection: "up" }))}
        >
          <span aria-hidden="true">&#xE70E;</span>
        </button>
        <button
          type="button"
          className="mini-button source-move-button"
          disabled={!canMoveDown}
          aria-label="Move source down"
          title="Move down"
          onClick={() => run(() => store.sendCommand("moveSource", { sourceKey: source.sourceKey, moveDirection: "down" }))}
        >
          <span aria-hidden="true">&#xE70D;</span>
        </button>
        {source.isHidden ? (
          <button
            type="button"
            className="native-button restore-button"
            onClick={() => run(() => store.sendCommand("restoreSource", { sourceKey: source.sourceKey }))}
          >
            Restore
          </button>
        ) : (
          <button
            type="button"
            className="native-button hide-button"
            onClick={() => run(() => store.sendCommand("hideSource", { sourceKey: source.sourceKey }))}
          >
            Hide
          </button>
        )}
        <button
          type="button"
          className="native-button details-button"
          onClick={() => run(() => store.sendCommand("selectDetail", { sourceKey: source.sourceKey }))}
        >
          Details
        </button>
        <button
          type="button"
          className="native-button popup-button"
          onClick={() => run(() => store.sendCommand("openPopup", { sourceKey: source.sourceKey }))}
        >
          Pop-up
        </button>
        <button
          type="button"
          className="native-button reset-source-button"
          onClick={() =>
            run(() =>
              store.sendCommand("resetSource", {
                sourceKey: source.sourceKey,
                sourceKind: source.sourceKind,
                processId: source.processId ?? null,
              }),
            )
          }
        >
          Reset
        </button>
      </div>
      <span className="source-drop-hint source-drop-hint-after" aria-hidden="true" />
    </article>
  );
}

function applyTruePeakToAll(
  snapshot: HostSnapshotDto | null,
  store: ReturnType<typeof createHostStateStore>,
  run: (action: () => Promise<void>) => Promise<void>,
  truePeakOversampling: string,
): void {
  const firstSource = snapshot?.sources[0];
  if (!firstSource) {
    return;
  }

  void run(() =>
    store.sendCommand("setTruePeakOversampling", {
      sourceKey: firstSource.sourceKey,
      sourceKind: firstSource.sourceKind,
      processId: firstSource.processId ?? null,
      truePeakOversampling,
    }),
  );
}

function EditableSourceName({
  source,
  className,
  headingLevel,
  run,
  store,
}: {
  source: HostSourceSnapshotDto;
  className: string;
  headingLevel?: 2;
  run: (action: () => Promise<void>) => Promise<void>;
  store: ReturnType<typeof createHostStateStore>;
}) {
  const [isEditing, setEditing] = useState(false);
  const [value, setValue] = useState(source.alias ?? source.displayName);
  const didFinishEditRef = useRef(false);

  useEffect(() => {
    if (!isEditing) {
      setValue(source.alias ?? source.displayName);
    }
  }, [isEditing, source.alias, source.displayName]);

  function startEditing(): void {
    setValue(source.alias ?? source.displayName);
    didFinishEditRef.current = false;
    setEditing(true);
  }

  function cancelEditing(): void {
    didFinishEditRef.current = true;
    setValue(source.alias ?? source.displayName);
    setEditing(false);
  }

  async function commitEditing(): Promise<void> {
    if (didFinishEditRef.current) {
      return;
    }

    didFinishEditRef.current = true;
    const next = value.trim();
    setEditing(false);
    await run(() =>
      store.sendCommand("setSourceAlias", {
        sourceKey: source.sourceKey,
        sourceKind: source.sourceKind,
        processId: source.processId ?? null,
        alias: next.length ? next : null,
      }),
    );
  }

  function handleKeyDown(event: KeyboardEvent<HTMLInputElement>): void {
    if (event.key === "Enter") {
      event.preventDefault();
      void commitEditing();
    } else if (event.key === "Escape") {
      event.preventDefault();
      cancelEditing();
    }
  }

  if (isEditing) {
    const editor = (
      <input
        className={`${className} source-name-editor`}
        aria-label={`Alias for ${source.displayName}`}
        value={value}
        autoFocus
        onFocus={(event) => event.currentTarget.select()}
        onChange={(event) => setValue(event.currentTarget.value)}
        onBlur={() => void commitEditing()}
        onKeyDown={handleKeyDown}
      />
    );

    return headingLevel === 2 ? <h2>{editor}</h2> : editor;
  }

  const label = (
    <span className={className} title={source.sourceIdentityText}>
      {source.displayTitle}
    </span>
  );

  return headingLevel === 2 ? (
    <h2 onDoubleClick={startEditing}>{label}</h2>
  ) : (
    <strong onDoubleClick={startEditing}>{label}</strong>
  );
}

const supportedThemeIds = new Set(["midnight", "milk-white", "forest-green", "pink-dream"]);
const themeOptions = [
  { id: "midnight", name: "Midnight" },
  { id: "milk-white", name: "Milk White" },
  { id: "forest-green", name: "Forest Green" },
  { id: "pink-dream", name: "Pink Dream" },
];

function resolveThemeId(themeId: string | null | undefined): string {
  return themeId && supportedThemeIds.has(themeId) ? themeId : "midnight";
}

function filterRecentEvents(events: HostRecentEventSnapshotDto[], filter: string): HostRecentEventSnapshotDto[] {
  return filter === "all" ? events : events.filter((event) => event.kind === filter);
}

function formatPrimaryValue(source: HostSourceSnapshotDto, displayMode: string): string {
  return displayMode === "lufs" ? formatLufs(source.momentaryLufs) : source.peakDbText;
}

function getPrimaryValueSeverity(source: HostSourceSnapshotDto, displayMode: string): string {
  if (source.isInstanceMissing || !isFiniteNumber(source.peakDb)) {
    return "muted";
  }

  if (displayMode !== "peak") {
    return "normal";
  }

  if (source.peakDb > 0) {
    return "over";
  }

  if (source.peakDb >= -1) {
    return "critical";
  }

  if (source.peakDb >= -6) {
    return "hot";
  }

  if (source.peakDb >= -12) {
    return "warm";
  }

  return "normal";
}

function formatLufs(value: number | null | undefined): string {
  return isFiniteNumber(value) ? `${value.toFixed(1)} LUFS` : "LUFS n/a";
}

function formatDb(value: number | null | undefined): string {
  return isFiniteNumber(value) ? `${value.toFixed(1)} dB` : "n/a";
}

function formatCompactDb(value: number | null | undefined): string {
  if (!isFiniteNumber(value)) {
    return "n/a";
  }

  return `${value > 0 ? "+" : ""}${value.toFixed(1)}dB`;
}

function toMeterPercent(source: HostSourceSnapshotDto): number {
  if (Number.isFinite(source.peak) && source.peak > 0) {
    return Math.max(2, Math.min(100, source.peak * 100));
  }

  if (Number.isFinite(source.peakDb)) {
    return Math.max(2, Math.min(100, ((source.peakDb + 60) / 60) * 100));
  }

  return 2;
}

function toScaledPercent(value: number | null | undefined, floor: number, ceiling: number): number {
  if (!isFiniteNumber(value)) {
    return 0;
  }

  return Math.max(0, Math.min(100, ((value - floor) / (ceiling - floor)) * 100));
}

function getDbReadoutSeverity(value: number | null | undefined): string {
  if (!isFiniteNumber(value)) {
    return "muted";
  }

  if (value > 0) {
    return "over";
  }

  if (value >= -1) {
    return "critical";
  }

  if (value >= -6) {
    return "hot";
  }

  if (value >= -12) {
    return "warm";
  }

  return "normal";
}

function formatTick(value: number): string {
  return value > 0 ? `+${value}` : `${value}`;
}

function isFiniteNumber(value: number | null | undefined): value is number {
  return typeof value === "number" && Number.isFinite(value);
}
