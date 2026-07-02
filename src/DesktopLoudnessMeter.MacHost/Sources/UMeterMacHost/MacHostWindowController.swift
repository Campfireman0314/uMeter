import Cocoa
import WebKit

final class MacHostWindowController: NSWindowController {
    private let bridge: HostBridgeHandling
    private let assetLocator: MacWebAssetLocating
    private let contentController = WKUserContentController()
    private var webView: WKWebView?

    init(bridge: HostBridgeHandling, assetLocator: MacWebAssetLocating) {
        self.bridge = bridge
        self.assetLocator = assetLocator
        let window = NSWindow(
            contentRect: NSRect(x: 0, y: 0, width: 1180, height: 720),
            styleMask: [.titled, .closable, .miniaturizable, .resizable],
            backing: .buffered,
            defer: false)
        window.title = "uMeter"
        super.init(window: window)
        configureWebView()
    }

    required init?(coder: NSCoder) {
        fatalError("init(coder:) has not been implemented")
    }

    private func configureWebView() {
        contentController.add(self, name: "hostBridge")

        let configuration = WKWebViewConfiguration()
        configuration.userContentController = contentController

        let view = WKWebView(frame: .zero, configuration: configuration)
        view.autoresizingMask = [.width, .height]
        window?.contentView = view
        webView = view

        view.loadFileURL(
            assetLocator.indexFileURL,
            allowingReadAccessTo: assetLocator.webRootURL)
    }
}

extension MacHostWindowController: WKScriptMessageHandler {
    func userContentController(_ userContentController: WKUserContentController, didReceive message: WKScriptMessage) {
        guard message.name == "hostBridge" else {
            return
        }

        let requestJson = HostBridgeMessageJson.stringify(message.body)
        let responseJson = bridge.handle(json: requestJson)
        let script = "window.dispatchEvent(new MessageEvent('message', { data: \(HostBridgeMessageJson.javascriptStringLiteral(responseJson)) }));"
        webView?.evaluateJavaScript(script)
    }
}
