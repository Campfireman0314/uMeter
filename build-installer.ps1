param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [string]$Runtime = "win-x64",

    [string]$Version = "2.2.2",

    [string]$JuceDir = "E:\JUCE",

    [string]$Vst3BuildDir = (Join-Path $PSScriptRoot "build\juce-vst3-release"),

    [switch]$SkipVst3Plugin
)

$ErrorActionPreference = "Stop"
$root = [System.IO.Path]::GetFullPath($PSScriptRoot)
$artifacts = [System.IO.Path]::GetFullPath((Join-Path $root "artifacts"))
$publishDir = Join-Path $artifacts "app-publish"
$payloadZip = Join-Path $artifacts "uMeter.Payload.zip"
$setupPublishDir = Join-Path $artifacts "setup-publish"
$setupExe = Join-Path $artifacts "uMeter.Setup.exe"
$versionedSetupExe = Join-Path $artifacts "uMeter-Setup-$Version.exe"
$appProject = Join-Path $root "src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj"
$setupProject = Join-Path $root "src\DesktopLoudnessMeter.Setup\DesktopLoudnessMeter.Setup.csproj"
$nativeBuild = Join-Path $root "src\DesktopLoudnessMeter.Native\build-native.ps1"
$pluginProjectDir = Join-Path $root "src\DesktopLoudnessMeter.JucePlugin"
$pluginBundleName = "uMeter Sender.vst3"
$pluginPayloadDir = Join-Path $publishDir "Plugins\VST3"
$pluginPayloadBundle = Join-Path $pluginPayloadDir $pluginBundleName

function Resolve-ToolPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$CommandName,

        [string[]]$FallbackPaths = @()
    )

    $command = Get-Command $CommandName -ErrorAction SilentlyContinue
    if ($null -ne $command) {
        return $command.Source
    }

    foreach ($path in $FallbackPaths) {
        if (Test-Path -LiteralPath $path) {
            return $path
        }
    }

    return $CommandName
}

function Test-IsUnderRoot {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$RootPath
    )

    $fullPath = [System.IO.Path]::GetFullPath($Path).TrimEnd('\', '/') + '\'
    $fullRoot = [System.IO.Path]::GetFullPath($RootPath).TrimEnd('\', '/') + '\'
    return $fullPath.StartsWith($fullRoot, [System.StringComparison]::OrdinalIgnoreCase)
}

function Invoke-Native {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Description,

        [Parameter(Mandatory = $true)]
        [string]$FilePath,

        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Description failed with exit code $LASTEXITCODE."
    }
}

if (-not (Test-IsUnderRoot -Path $artifacts -RootPath $root)) {
    throw "Refusing to clean artifacts outside the repository: $artifacts"
}

if (Test-Path -LiteralPath $artifacts) {
    Remove-Item -LiteralPath $artifacts -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $artifacts | Out-Null

Invoke-Native "Native FFT build" "powershell" @(
    "-ExecutionPolicy", "Bypass",
    "-File", $nativeBuild,
    "-Configuration", $Configuration
)

Invoke-Native "App publish" "dotnet" @(
    "publish", $appProject,
    "--configuration", $Configuration,
    "--runtime", $Runtime,
    "--self-contained", "true",
    "-p:Version=$Version",
    "-p:AssemblyVersion=$Version.0",
    "-p:FileVersion=$Version.0",
    "-p:InformationalVersion=$Version",
    "-p:PublishSingleFile=false",
    "-p:PublishReadyToRun=true",
    "--output", $publishDir
)

if (-not $SkipVst3Plugin) {
    if (-not (Test-Path -LiteralPath (Join-Path $JuceDir "CMakeLists.txt"))) {
        throw "JUCE_DIR must point to a JUCE checkout containing CMakeLists.txt. Current value: $JuceDir"
    }

    $cmake = Resolve-ToolPath "cmake" @(
        "C:\Program Files\CMake\bin\cmake.exe",
        "C:\Program Files (x86)\CMake\bin\cmake.exe",
        "D:\Program Files\Microsoft Visual Studio\2022\BuildTools\Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe"
    )

    Invoke-Native "VST3 configure" $cmake @(
        "-S", $pluginProjectDir,
        "-B", $Vst3BuildDir,
        "-DJUCE_DIR=$JuceDir",
        "-DDLM_BUILD_JUCE_PLUGIN_TESTS=OFF",
        "-DDLM_COPY_VST3_AFTER_BUILD=OFF",
        "-DDLM_COPY_EXTRA_VST3_AFTER_BUILD=OFF"
    )

    Invoke-Native "VST3 build" $cmake @(
        "--build", $Vst3BuildDir,
        "--config", $Configuration,
        "--target", "DesktopLoudnessMeterSender_VST3"
    )

    $bundle = Get-ChildItem -Path $Vst3BuildDir -Recurse -Directory -Filter $pluginBundleName |
        Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName "Contents") } |
        Select-Object -First 1

    if ($null -eq $bundle) {
        throw "Could not find built VST3 bundle '$pluginBundleName' under $Vst3BuildDir."
    }

    New-Item -ItemType Directory -Force -Path $pluginPayloadDir | Out-Null
    if (Test-Path -LiteralPath $pluginPayloadBundle) {
        Remove-Item -LiteralPath $pluginPayloadBundle -Recurse -Force
    }

    Copy-Item -LiteralPath $bundle.FullName -Destination $pluginPayloadBundle -Recurse -Force
    Write-Host "Bundled VST3 sender: $pluginPayloadBundle"
}

Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $payloadZip -Force

Invoke-Native "Setup publish" "dotnet" @(
    "publish", $setupProject,
    "--configuration", $Configuration,
    "--runtime", $Runtime,
    "--self-contained", "true",
    "-p:Version=$Version",
    "-p:AssemblyVersion=$Version.0",
    "-p:FileVersion=$Version.0",
    "-p:InformationalVersion=$Version",
    "-p:PublishSingleFile=true",
    "-p:EnableCompressionInSingleFile=true",
    "-p:PublishReadyToRun=true",
    "-p:PayloadZip=$payloadZip",
    "--output", $setupPublishDir
)

Copy-Item -LiteralPath (Join-Path $setupPublishDir "uMeter.Setup.exe") -Destination $setupExe -Force
Copy-Item -LiteralPath $setupExe -Destination $versionedSetupExe -Force

Write-Host "Installer created: $setupExe"
Write-Host "Versioned installer created: $versionedSetupExe"
