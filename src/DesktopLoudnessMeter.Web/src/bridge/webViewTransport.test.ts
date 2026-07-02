import { afterEach, describe, expect, it, vi } from "vitest";
import { isWebViewMessageHostAvailable, WebViewMessageTransport } from "./webViewTransport";
import type { HostBridgeRequest, HostBridgeResponse } from "./hostBridge";

describe("WebViewMessageTransport", () => {
  afterEach(() => {
    setWindowHostState({});
  });

  it("posts requests and resolves matching responses by id", async () => {
    const host = new FakeWebViewHost();
    const transport = new WebViewMessageTransport(host);
    const promise = transport.send({ version: 1, id: "request-1", kind: "snapshot" });

    expect(host.sent).toEqual([{ version: 1, id: "request-1", kind: "snapshot" }]);

    host.emit({
      version: 1,
      id: "other",
      kind: "error",
      ok: false,
      error: { code: "invalidRequest", message: "Ignore me." },
    });
    host.emit({
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
        statusText: "native",
        commands: {
          canBackToGlobal: false,
          canResetAll: false,
          canForgetSelectedSource: false,
          canMoveSelectedSourceUp: false,
          canMoveSelectedSourceDown: false,
        },
      },
    });

    await expect(promise).resolves.toMatchObject({
      id: "request-1",
      snapshot: { statusText: "native" },
    });
    transport.dispose();
  });

  it("uses the Windows WebView2 host when it is available", async () => {
    const chromeHost = new FakeWebViewHost();
    const wkHost = new FakeWKWebViewHandler();
    setWindowHostState({ chromeHost, wkHost });

    expect(isWebViewMessageHostAvailable()).toBe(true);

    const transport = new WebViewMessageTransport();
    const promise = transport.send({ version: 1, id: "windows-1", kind: "snapshot" });

    expect(chromeHost.sent).toEqual([{ version: 1, id: "windows-1", kind: "snapshot" }]);
    expect(wkHost.sent).toEqual([]);

    chromeHost.emit(snapshotResponse("windows-1", "windows"));

    await expect(promise).resolves.toMatchObject({
      id: "windows-1",
      snapshot: { statusText: "windows" },
    });
    transport.dispose();
  });

  it("uses the WKWebView message handler when Chrome WebView is unavailable", async () => {
    const wkHost = new FakeWKWebViewHandler();
    setWindowHostState({ wkHost });

    expect(isWebViewMessageHostAvailable()).toBe(true);

    const transport = new WebViewMessageTransport();
    const promise = transport.send({ version: 1, id: "mac-1", kind: "snapshot" });

    expect(wkHost.sent).toEqual([{ version: 1, id: "mac-1", kind: "snapshot" }]);

    window.dispatchEvent(new MessageEvent("message", { data: snapshotResponse("mac-1", "mac") }));

    await expect(promise).resolves.toMatchObject({
      id: "mac-1",
      snapshot: { statusText: "mac" },
    });
    transport.dispose();
  });

  it("times out unanswered requests so the WebView can retry later", async () => {
    vi.useFakeTimers();
    try {
      const host = new FakeWebViewHost();
      const transport = new WebViewMessageTransport(host, 25);
      const promise = transport.send({ version: 1, id: "request-timeout", kind: "snapshot" });
      const expectation = expect(promise).rejects.toThrow("HostBridge request timed out: snapshot.");

      await vi.advanceTimersByTimeAsync(25);

      await expectation;
      transport.dispose();
    } finally {
      vi.useRealTimers();
    }
  });

  it("reports unavailable when neither WebView host exists", () => {
    setWindowHostState({});

    expect(isWebViewMessageHostAvailable()).toBe(false);
    expect(() => new WebViewMessageTransport()).toThrow("WebView message host is not available.");
  });
});

class FakeWebViewHost {
  public readonly sent: HostBridgeRequest[] = [];
  private listener: ((event: MessageEvent<unknown>) => void) | null = null;

  public postMessage(message: HostBridgeRequest): void {
    this.sent.push(message);
  }

  public addEventListener(_: "message", listener: (event: MessageEvent<unknown>) => void): void {
    this.listener = listener;
  }

  public removeEventListener(): void {
    this.listener = null;
  }

  public emit(response: HostBridgeResponse): void {
    this.listener?.({ data: response } as MessageEvent<unknown>);
  }
}

class FakeWKWebViewHandler {
  public readonly sent: HostBridgeRequest[] = [];

  public postMessage(message: HostBridgeRequest): void {
    this.sent.push(message);
  }
}

function setWindowHostState({
  chromeHost,
  wkHost,
}: {
  chromeHost?: FakeWebViewHost;
  wkHost?: FakeWKWebViewHandler;
}): void {
  Object.defineProperty(window, "chrome", {
    configurable: true,
    value: chromeHost ? { webview: chromeHost } : undefined,
  });
  Object.defineProperty(window, "webkit", {
    configurable: true,
    value: wkHost ? { messageHandlers: { hostBridge: wkHost } } : undefined,
  });
}

function snapshotResponse(id: string, statusText: string): HostBridgeResponse {
  return {
    version: 1,
    id,
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
      statusText,
      commands: {
        canBackToGlobal: false,
        canResetAll: false,
        canForgetSelectedSource: false,
        canMoveSelectedSourceUp: false,
        canMoveSelectedSourceDown: false,
      },
    },
  };
}
