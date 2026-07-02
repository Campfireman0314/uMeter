import { describe, expect, it } from "vitest";
import {
  HostBridgeClient,
  type HostBridgeRequest,
  type HostBridgeResponse,
  type HostBridgeTransport,
} from "./hostBridge";

describe("HostBridgeClient", () => {
  it("requests a snapshot through the transport envelope", async () => {
    const transport = new RecordingTransport({
      version: 1,
      id: "request-1",
      kind: "snapshot",
      ok: true,
      snapshot: {
        sources: [],
        selectedDetail: null,
        settings: {
          schemaVersion: 1,
          themeId: null,
          maxRecentEventEntries: null,
          eventOutputFolder: null,
          loadDefaultDetailLayoutFromFile: false,
          defaultDetailLayoutFile: null,
          showHiddenSources: false,
          sources: [],
        },
        recentEvents: [],
        statusText: "No active audio sources",
        commands: {
          canBackToGlobal: false,
          canResetAll: false,
          canForgetSelectedSource: false,
          canMoveSelectedSourceUp: false,
          canMoveSelectedSourceDown: false,
        },
      },
    });
    const client = new HostBridgeClient(transport, () => "request-1");

    const snapshot = await client.requestSnapshot();

    expect(transport.sent).toEqual([
      { version: 1, id: "request-1", kind: "snapshot" },
    ]);
    expect(snapshot.statusText).toBe("No active audio sources");
    expect(snapshot.settings.schemaVersion).toBe(1);
  });

  it("sends commands and preserves typed command errors", async () => {
    const transport = new RecordingTransport({
      version: 1,
      id: "request-2",
      kind: "commandResult",
      ok: false,
      result: {
        commandKind: "selectDetail",
        success: false,
        snapshot: emptySnapshot,
        error: {
          code: "sourceNotFound",
          message: "Source was not found.",
          targetSourceKey: "missing",
          detail: null,
        },
      },
    });
    const client = new HostBridgeClient(transport, () => "request-2");

    const result = await client.sendCommand("selectDetail", { sourceKey: "missing" });

    expect(transport.sent[0]).toEqual({
      version: 1,
      id: "request-2",
      kind: "command",
      command: "selectDetail",
      payload: { sourceKey: "missing" },
    });
    expect(result.success).toBe(false);
    expect(result.error?.code).toBe("sourceNotFound");
    expect(result.snapshot.statusText).toBe("safe");
  });

  it("sends typed global settings command payloads", async () => {
    const transport = new RecordingTransport({
      version: 1,
      id: "request-settings",
      kind: "commandResult",
      ok: true,
      result: {
        commandKind: "setRecentEventLimit",
        success: true,
        snapshot: emptySnapshot,
        error: null,
      },
    });
    const client = new HostBridgeClient(transport, () => "request-settings");

    await client.sendCommand("setTheme", { themeId: "forest-green" });
    await client.sendCommand("setRecentEventLimit", { maxRecentEventEntries: 12 });
    await client.sendCommand("setEventOutputFolder", { eventOutputFolder: "C:\\uMeter Events" });

    expect(transport.sent).toEqual([
      {
        version: 1,
        id: "request-settings",
        kind: "command",
        command: "setTheme",
        payload: { themeId: "forest-green" },
      },
      {
        version: 1,
        id: "request-settings",
        kind: "command",
        command: "setRecentEventLimit",
        payload: { maxRecentEventEntries: 12 },
      },
      {
        version: 1,
        id: "request-settings",
        kind: "command",
        command: "setEventOutputFolder",
        payload: { eventOutputFolder: "C:\\uMeter Events" },
      },
    ]);
  });

  it("sends typed native shell command payloads", async () => {
    const transport = new RecordingTransport({
      version: 1,
      id: "request-shell",
      kind: "commandResult",
      ok: true,
      result: {
        commandKind: "setAlwaysOnTop",
        success: true,
        snapshot: emptySnapshot,
        error: null,
      },
    });
    const client = new HostBridgeClient(transport, () => "request-shell");

    await client.sendCommand("openGlobalSettings");
    await client.sendCommand("beginWindowDrag");
    await client.sendCommand("toggleWindowState");
    await client.sendCommand("hideToTray");
    await client.sendCommand("closeWindow");
    await client.sendCommand("setAlwaysOnTop", { isAlwaysOnTop: false });
    await client.sendCommand("setDisplayMode", { displayMode: "lufs" });
    await client.sendCommand("setTruePeakEnabled", { truePeakEnabled: true });
    await client.sendCommand("saveDetailLayout");
    await client.sendCommand("loadDetailLayout");
    await client.sendCommand("openStereoSettings");

    expect(transport.sent).toEqual([
      {
        version: 1,
        id: "request-shell",
        kind: "command",
        command: "openGlobalSettings",
      },
      {
        version: 1,
        id: "request-shell",
        kind: "command",
        command: "beginWindowDrag",
      },
      {
        version: 1,
        id: "request-shell",
        kind: "command",
        command: "toggleWindowState",
      },
      {
        version: 1,
        id: "request-shell",
        kind: "command",
        command: "hideToTray",
      },
      {
        version: 1,
        id: "request-shell",
        kind: "command",
        command: "closeWindow",
      },
      {
        version: 1,
        id: "request-shell",
        kind: "command",
        command: "setAlwaysOnTop",
        payload: { isAlwaysOnTop: false },
      },
      {
        version: 1,
        id: "request-shell",
        kind: "command",
        command: "setDisplayMode",
        payload: { displayMode: "lufs" },
      },
      {
        version: 1,
        id: "request-shell",
        kind: "command",
        command: "setTruePeakEnabled",
        payload: { truePeakEnabled: true },
      },
      {
        version: 1,
        id: "request-shell",
        kind: "command",
        command: "saveDetailLayout",
      },
      {
        version: 1,
        id: "request-shell",
        kind: "command",
        command: "loadDetailLayout",
      },
      {
        version: 1,
        id: "request-shell",
        kind: "command",
        command: "openStereoSettings",
      },
    ]);
  });

  it("sends typed native source action payloads", async () => {
    const transport = new RecordingTransport({
      version: 1,
      id: "request-source-action",
      kind: "commandResult",
      ok: true,
      result: {
        commandKind: "moveSource",
        success: true,
        snapshot: emptySnapshot,
        error: null,
      },
    });
    const client = new HostBridgeClient(transport, () => "request-source-action");

    await client.sendCommand("openPopup", { sourceKey: "plugin:lead" });
    await client.sendCommand("restoreSource", { sourceKey: "plugin:lead" });
    await client.sendCommand("moveSource", { sourceKey: "plugin:lead", moveDirection: "down" });
    await client.sendCommand("forgetSource", { sourceKey: "plugin:lead" });

    expect(transport.sent).toEqual([
      {
        version: 1,
        id: "request-source-action",
        kind: "command",
        command: "openPopup",
        payload: { sourceKey: "plugin:lead" },
      },
      {
        version: 1,
        id: "request-source-action",
        kind: "command",
        command: "restoreSource",
        payload: { sourceKey: "plugin:lead" },
      },
      {
        version: 1,
        id: "request-source-action",
        kind: "command",
        command: "moveSource",
        payload: { sourceKey: "plugin:lead", moveDirection: "down" },
      },
      {
        version: 1,
        id: "request-source-action",
        kind: "command",
        command: "forgetSource",
        payload: { sourceKey: "plugin:lead" },
      },
    ]);
  });

  it("throws readable errors for bridge-level failures", async () => {
    const transport = new RecordingTransport({
      version: 1,
      id: "request-3",
      kind: "error",
      ok: false,
      error: {
        code: "invalidRequest",
        message: "Unknown command.",
        targetSourceKey: null,
        detail: null,
      },
    });
    const client = new HostBridgeClient(transport, () => "request-3");

    await expect(client.sendCommand("doesNotExist")).rejects.toThrow("Unknown command.");
  });
});

const emptySnapshot = {
  sources: [],
  selectedDetail: null,
  settings: {
    schemaVersion: 1,
    themeId: null,
    maxRecentEventEntries: null,
    eventOutputFolder: null,
    loadDefaultDetailLayoutFromFile: false,
    defaultDetailLayoutFile: null,
    showHiddenSources: false,
    sources: [],
  },
  recentEvents: [],
  statusText: "safe",
  commands: {
    canBackToGlobal: false,
    canResetAll: false,
    canForgetSelectedSource: false,
    canMoveSelectedSourceUp: false,
    canMoveSelectedSourceDown: false,
  },
};

class RecordingTransport implements HostBridgeTransport {
  public readonly sent: HostBridgeRequest[] = [];

  public constructor(private readonly response: HostBridgeResponse) {}

  public async send(request: HostBridgeRequest): Promise<HostBridgeResponse> {
    this.sent.push(request);
    return this.response;
  }
}
