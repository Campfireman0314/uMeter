import Cocoa

@main
final class AppDelegate: NSObject, NSApplicationDelegate {
    private var windowController: MacHostWindowController?

    func applicationDidFinishLaunching(_ notification: Notification) {
        let controller = MacHostWindowController(
            bridge: PlaceholderHostBridge(),
            assetLocator: MacWebAssetLocator())
        controller.showWindow(nil)
        windowController = controller
    }

    func applicationShouldTerminateAfterLastWindowClosed(_ sender: NSApplication) -> Bool {
        true
    }
}
