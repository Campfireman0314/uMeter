import { describe, expect, it } from "vitest";
import { createHostStateStore, type HostBridgeLike } from "./hostState";
import type { HostCommandResultDto, HostSnapshotDto } from "../bridge/hostBridge";

describe("createHostStateStore", () => {
  it("loads the initial snapshot through an injected bridge", async () => {
    const bridge = new FakeBridge();
    const store = createHostStateStore(bridge);

    await store.loadSnapshot();

    expect(store.getSnapshot().snapshot?.statusText).toBe("loaded");
    expect(store.getSnapshot().errorMessage).toBeNull();
  });

  it("keeps the last safe snapshot when a bridge-level error occurs", async () => {
    const bridge = new FakeBridge();
    const store = createHostStateStore(bridge);
    await store.loadSnapshot();
    bridge.error = new Error("Unknown command.");

    await store.sendCommand("refresh");

    expect(store.getSnapshot().snapshot?.statusText).toBe("loaded");
    expect(store.getSnapshot().errorMessage).toBe("Unknown command.");
  });

  it("updates the safe snapshot from typed command results", async () => {
    const bridge = new FakeBridge();
    const store = createHostStateStore(bridge);

    await store.sendCommand("refresh");

    expect(store.getSnapshot().snapshot?.statusText).toBe("commanded");
    expect(store.getSnapshot().errorMessage).toBeNull();
  });

  it("ignores stale refresh results that resolve after a newer command", async () => {
    let finishRefresh: () => void = () => {
      throw new Error("Refresh did not start.");
    };
    const bridge = new FakeBridge();
    bridge.commandHandler = (command) => {
      if (command === "refresh") {
        return new Promise<HostCommandResultDto>((resolve) => {
          finishRefresh = () =>
            resolve({
              commandKind: "refresh",
              success: true,
              snapshot: snapshot("stale refresh"),
              error: null,
            });
        });
      }

      return Promise.resolve({
        commandKind: command,
        success: true,
        snapshot: snapshot("show hidden applied", true),
        error: null,
      });
    };
    const store = createHostStateStore(bridge);

    const refresh = store.sendCommand("refresh");
    await store.sendCommand("setShowHiddenSources", { showHiddenSources: true });
    finishRefresh();
    await refresh;

    expect(store.getSnapshot().snapshot?.statusText).toBe("show hidden applied");
    expect(store.getSnapshot().snapshot?.settings.showHiddenSources).toBe(true);
  });

  it("ignores refresh results that start while a newer command is still pending", async () => {
    let finishCommand: () => void = () => {
      throw new Error("Command did not start.");
    };
    const bridge = new FakeBridge();
    bridge.commandHandler = (command) => {
      if (command === "setShowHiddenSources") {
        return new Promise<HostCommandResultDto>((resolve) => {
          finishCommand = () =>
            resolve({
              commandKind: command,
              success: true,
              snapshot: snapshot("show hidden applied", true),
              error: null,
            });
        });
      }

      return Promise.resolve({
        commandKind: "refresh",
        success: true,
        snapshot: snapshot("refresh saw old hidden state"),
        error: null,
      });
    };
    const store = createHostStateStore(bridge);

    const command = store.sendCommand("setShowHiddenSources", { showHiddenSources: true });
    await store.sendCommand("refresh");
    finishCommand();
    await command;

    expect(store.getSnapshot().snapshot?.statusText).toBe("show hidden applied");
    expect(store.getSnapshot().snapshot?.settings.showHiddenSources).toBe(true);
  });

  it("optimistically applies show-hidden before the host command resolves", async () => {
    let finishCommand: () => void = () => {
      throw new Error("Command did not start.");
    };
    const bridge = new FakeBridge();
    bridge.commandHandler = (command) =>
      new Promise<HostCommandResultDto>((resolve) => {
        finishCommand = () =>
          resolve({
            commandKind: command,
            success: true,
            snapshot: snapshot("host confirmed show hidden", true),
            error: null,
          });
      });
    const store = createHostStateStore(bridge);
    await store.loadSnapshot();

    const command = store.sendCommand("setShowHiddenSources", { showHiddenSources: true });

    expect(store.getSnapshot().snapshot?.settings.showHiddenSources).toBe(true);
    finishCommand();
    await command;
    expect(store.getSnapshot().snapshot?.statusText).toBe("host confirmed show hidden");
  });
});

class FakeBridge implements HostBridgeLike {
  public error: Error | null = null;
  public commandHandler: ((command: string, payload?: unknown) => Promise<HostCommandResultDto>) | null = null;

  public async requestSnapshot(): Promise<HostSnapshotDto> {
    if (this.error) {
      throw this.error;
    }

    return snapshot("loaded");
  }

  public async sendCommand(command = "refresh", payload?: unknown): Promise<HostCommandResultDto> {
    if (this.error) {
      throw this.error;
    }

    if (this.commandHandler) {
      return this.commandHandler(command, payload);
    }

    return {
      commandKind: "refresh",
      success: true,
      snapshot: snapshot("commanded"),
      error: null,
    };
  }
}

function snapshot(statusText: string, showHiddenSources = false): HostSnapshotDto {
  return {
    sources: [],
    selectedDetail: null,
    settings: {
      schemaVersion: 1,
      themeId: null,
      maxRecentEventEntries: null,
      eventOutputFolder: null,
      loadDefaultDetailLayoutFromFile: false,
      defaultDetailLayoutFile: null,
      showHiddenSources,
      sources: [],
    },
    recentEvents: [],
    statusText,
    commands: {
      canBackToGlobal: false,
      canResetAll: false,
      canForgetSelectedSource: false,
      canMoveSelectedSourceUp: false,
      canMoveSelectedSourceDown: false,
    },
  };
}
