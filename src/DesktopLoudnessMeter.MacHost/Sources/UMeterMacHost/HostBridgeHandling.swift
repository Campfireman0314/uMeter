import Foundation

protocol HostBridgeHandling {
    func handle(json: String) -> String
}

final class PlaceholderHostBridge: HostBridgeHandling {
    func handle(json: String) -> String {
        let requestId = HostBridgeMessageJson.extractRequestId(from: json)
        let response = [
            "version": 1,
            "id": requestId,
            "kind": "error",
            "error": [
                "code": "unavailable",
                "message": "macOS HostBridge core is not implemented in the readiness skeleton."
            ]
        ] as [String: Any]

        return HostBridgeMessageJson.stringify(response)
    }
}

enum HostBridgeMessageJson {
    static func extractRequestId(from json: String) -> String {
        guard let data = json.data(using: .utf8),
              let value = try? JSONSerialization.jsonObject(with: data),
              let object = value as? [String: Any],
              let id = object["id"] as? String,
              !id.isEmpty else {
            return "unknown"
        }

        return id
    }

    static func stringify(_ value: Any) -> String {
        if let text = value as? String {
            return text
        }

        guard JSONSerialization.isValidJSONObject(value),
              let data = try? JSONSerialization.data(withJSONObject: value),
              let text = String(data: data, encoding: .utf8) else {
            return "{}"
        }

        return text
    }

    static func javascriptStringLiteral(_ value: String) -> String {
        guard let data = try? JSONEncoder().encode(value),
              let literal = String(data: data, encoding: .utf8) else {
            return "\"\""
        }

        return literal
    }
}
