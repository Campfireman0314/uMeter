import type { HostBridgeRequest, HostBridgeResponse, HostBridgeTransport } from "./hostBridge";

interface WebViewMessageHost {
  postMessage(message: HostBridgeRequest): void;
  addEventListener(type: "message", listener: (event: MessageEvent<unknown>) => void): void;
  removeEventListener(type: "message", listener: (event: MessageEvent<unknown>) => void): void;
}

interface WKWebViewMessageHandler {
  postMessage(message: HostBridgeRequest): void;
}

const DEFAULT_REQUEST_TIMEOUT_MS = 1_000;

declare global {
  interface Window {
    chrome?: {
      webview?: WebViewMessageHost;
    };
    webkit?: {
      messageHandlers?: {
        hostBridge?: WKWebViewMessageHandler;
      };
    };
  }
}

export class WebViewMessageTransport implements HostBridgeTransport {
  private readonly pending = new Map<
    string,
    {
      resolve: (response: HostBridgeResponse) => void;
      reject: (error: Error) => void;
      timeoutId: number;
    }
  >();

  public constructor(
    private readonly host: WebViewMessageHost = getWebViewHost(),
    private readonly requestTimeoutMs: number = DEFAULT_REQUEST_TIMEOUT_MS,
  ) {
    this.host.addEventListener("message", this.handleMessage);
  }

  public send(request: HostBridgeRequest): Promise<HostBridgeResponse> {
    return new Promise((resolve, reject) => {
      const timeoutId = window.setTimeout(() => {
        if (!this.pending.delete(request.id)) {
          return;
        }

        reject(new Error(`HostBridge request timed out: ${formatRequestLabel(request)}.`));
      }, this.requestTimeoutMs);
      this.pending.set(request.id, { resolve, reject, timeoutId });
      try {
        this.host.postMessage(request);
      } catch (error) {
        if (this.pending.delete(request.id)) {
          window.clearTimeout(timeoutId);
        }

        reject(error instanceof Error ? error : new Error("HostBridge postMessage failed."));
      }
    });
  }

  public dispose(): void {
    this.host.removeEventListener("message", this.handleMessage);
    for (const pending of this.pending.values()) {
      window.clearTimeout(pending.timeoutId);
      pending.reject(new Error("HostBridge transport was disposed."));
    }

    this.pending.clear();
  }

  private readonly handleMessage = (event: MessageEvent<unknown>): void => {
    const response = parseResponse(event.data);
    if (!response) {
      return;
    }

    const pending = this.pending.get(response.id);
    if (!pending) {
      return;
    }

    this.pending.delete(response.id);
    window.clearTimeout(pending.timeoutId);
    pending.resolve(response);
  };
}

export function isWebViewMessageHostAvailable(): boolean {
  return Boolean(getWebViewHostOrNull());
}

function getWebViewHost(): WebViewMessageHost {
  const host = getWebViewHostOrNull();
  if (!host) {
    throw new Error("WebView message host is not available.");
  }

  return host;
}

function getWebViewHostOrNull(): WebViewMessageHost | null {
  const chromeHost = window.chrome?.webview;
  if (chromeHost) {
    return chromeHost;
  }

  const wkHost = window.webkit?.messageHandlers?.hostBridge;
  if (!wkHost) {
    return null;
  }

  return {
    postMessage(message) {
      wkHost.postMessage(message);
    },
    addEventListener(type, listener) {
      window.addEventListener(type, listener);
    },
    removeEventListener(type, listener) {
      window.removeEventListener(type, listener);
    },
  };
}

function parseResponse(value: unknown): HostBridgeResponse | null {
  if (typeof value === "string") {
    try {
      return JSON.parse(value) as HostBridgeResponse;
    } catch {
      return null;
    }
  }

  if (typeof value === "object" && value !== null && "id" in value) {
    return value as HostBridgeResponse;
  }

  return null;
}

function formatRequestLabel(request: HostBridgeRequest): string {
  return request.kind === "command" ? `${request.kind}:${request.command}` : request.kind;
}
