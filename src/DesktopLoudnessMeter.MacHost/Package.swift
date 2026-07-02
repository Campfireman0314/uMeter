// swift-tools-version: 5.9
import PackageDescription

let package = Package(
    name: "DesktopLoudnessMeterMacHost",
    platforms: [
        .macOS(.v13)
    ],
    products: [
        .executable(
            name: "uMeterMacHost",
            targets: ["UMeterMacHost"])
    ],
    targets: [
        .executableTarget(
            name: "UMeterMacHost",
            path: "Sources/UMeterMacHost")
    ])
