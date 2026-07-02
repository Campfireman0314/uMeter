import Foundation

protocol MacWebAssetLocating {
    var webRootURL: URL { get }
    var indexFileURL: URL { get }
}

struct MacWebAssetLocator: MacWebAssetLocating {
    var webRootURL: URL {
        if let override = ProcessInfo.processInfo.environment["UMETER_WEBVIEW_DIST_DIR"],
           !override.isEmpty {
            return URL(fileURLWithPath: override, isDirectory: true)
        }

        return Bundle.main.resourceURL?
            .appendingPathComponent("Web", isDirectory: true)
            ?? URL(fileURLWithPath: "Web", isDirectory: true)
    }

    var indexFileURL: URL {
        webRootURL.appendingPathComponent("index.html", isDirectory: false)
    }
}
