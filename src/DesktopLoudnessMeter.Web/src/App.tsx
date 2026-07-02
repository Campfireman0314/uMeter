import { useMemo } from "react";
import { AppShell } from "./AppShell";
import { HostBridgeClient } from "./bridge/hostBridge";
import { isWebViewMessageHostAvailable, WebViewMessageTransport } from "./bridge/webViewTransport";
import { createMockHostBridge } from "./mockHostBridge";

export function App() {
  const bridge = useMemo(
    () =>
      isWebViewMessageHostAvailable()
        ? new HostBridgeClient(new WebViewMessageTransport())
        : createMockHostBridge(),
    [],
  );

  return <AppShell bridge={bridge} />;
}
