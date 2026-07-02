import type {
  HostCommandName,
  HostCommandPayload,
  HostCommandResultDto,
  HostSnapshotDto,
} from "../bridge/hostBridge";

export interface HostBridgeLike {
  requestSnapshot(): Promise<HostSnapshotDto>;
  sendCommand(
    command: HostCommandName | string,
    payload?: HostCommandPayload,
  ): Promise<HostCommandResultDto>;
}

export interface HostStateSnapshot {
  snapshot: HostSnapshotDto | null;
  isLoading: boolean;
  errorMessage: string | null;
}

export interface HostStateStore {
  getSnapshot(): HostStateSnapshot;
  loadSnapshot(): Promise<void>;
  sendCommand(command: HostCommandName | string, payload?: HostCommandPayload): Promise<void>;
}

export function createHostStateStore(bridge: HostBridgeLike): HostStateStore {
  let mutationSequence = 0;
  let pendingMutations = 0;
  let state: HostStateSnapshot = {
    snapshot: null,
    isLoading: false,
    errorMessage: null,
  };

  function logShowHidden(message: string): void {
    if (typeof console !== "undefined") {
      console.debug(`[uMeter showHidden] ${message}`);
    }
  }

  function setLoading(): void {
    state = {
      ...state,
      isLoading: true,
      errorMessage: null,
    };
  }

  function setSnapshot(snapshot: HostSnapshotDto, errorMessage: string | null = null): void {
    state = {
      snapshot,
      isLoading: false,
      errorMessage,
    };
  }

  function setOptimisticShowHiddenSources(showHiddenSources: boolean): void {
    if (!state.snapshot) {
      logShowHidden(`optimistic skipped because snapshot is null target=${showHiddenSources}`);
      return;
    }

    state = {
      snapshot: {
        ...state.snapshot,
        settings: {
          ...state.snapshot.settings,
          showHiddenSources,
        },
      },
      isLoading: state.isLoading,
      errorMessage: state.errorMessage,
    };
    logShowHidden(`optimistic target=${showHiddenSources}`);
  }

  function applyOptimisticCommand(command: HostCommandName | string, payload?: HostCommandPayload): void {
    if (command === "setShowHiddenSources" && typeof payload?.showHiddenSources === "boolean") {
      setOptimisticShowHiddenSources(payload.showHiddenSources);
    }
  }

  function setSnapshotIfNoNewerMutation(
    mutationSnapshot: number,
    snapshot: HostSnapshotDto,
    errorMessage: string | null = null,
  ): void {
    if (mutationSnapshot !== mutationSequence || pendingMutations > 0) {
      return;
    }

    setSnapshot(snapshot, errorMessage);
  }

  function setError(error: unknown): void {
    state = {
      ...state,
      isLoading: false,
      errorMessage: error instanceof Error ? error.message : "HostBridge request failed.",
    };
  }

  return {
    getSnapshot() {
      return state;
    },
    async loadSnapshot() {
      const mutationSnapshot = mutationSequence;
      setLoading();
      try {
        const snapshot = await bridge.requestSnapshot();
        setSnapshotIfNoNewerMutation(mutationSnapshot, snapshot);
      } catch (error) {
        if (mutationSnapshot === mutationSequence && pendingMutations === 0) {
          setError(error);
        }
      }
    },
    async sendCommand(command, payload) {
      if (command === "refresh") {
        const mutationSnapshot = mutationSequence;
        const before = state.snapshot?.settings.showHiddenSources;
        setLoading();
        try {
          const result = await bridge.sendCommand(command, payload);
          const incoming = result.snapshot.settings.showHiddenSources;
          if (before !== incoming || pendingMutations > 0 || mutationSnapshot !== mutationSequence) {
            logShowHidden(
              `refresh result incoming=${incoming} before=${before} mutationSnapshot=${mutationSnapshot} currentMutation=${mutationSequence} pendingMutations=${pendingMutations}`,
            );
          }
          setSnapshotIfNoNewerMutation(mutationSnapshot, result.snapshot, result.error?.message ?? null);
        } catch (error) {
          if (mutationSnapshot === mutationSequence && pendingMutations === 0) {
            setError(error);
          }
        }
        return;
      }

      mutationSequence += 1;
      pendingMutations += 1;
      setLoading();
      if (command === "setShowHiddenSources") {
        logShowHidden(
          `command start payload=${payload?.showHiddenSources} mutationSequence=${mutationSequence} pendingMutations=${pendingMutations}`,
        );
      }
      applyOptimisticCommand(command, payload);
      try {
        const result = await bridge.sendCommand(command, payload);
        if (command === "setShowHiddenSources") {
          logShowHidden(
            `command result success=${result.success} snapshot=${result.snapshot.settings.showHiddenSources} error=${result.error?.message ?? "none"}`,
          );
        }
        setSnapshot(result.snapshot, result.error?.message ?? null);
      } catch (error) {
        if (command === "setShowHiddenSources") {
          logShowHidden(`command error=${error instanceof Error ? error.message : String(error)}`);
        }
        setError(error);
      } finally {
        pendingMutations = Math.max(0, pendingMutations - 1);
      }
    },
  };
}
