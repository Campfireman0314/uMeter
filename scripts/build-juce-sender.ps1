param(
    [ValidateSet("VST3", "AAX", "Both")]
    [string]$Format = "VST3",

    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [string]$JuceDir = "E:\JUCE",

    [string]$Vst3BuildDir = (Join-Path $PSScriptRoot "..\build\juce-vst3"),

    [string]$AaxBuildDir = (Join-Path $PSScriptRoot "..\build\juce-aax"),

    [string]$AaxSdkDir = "",

    [switch]$NoInstallCopy,

    [switch]$RunTests
)

$ErrorActionPreference = "Stop"
$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$vst3ProjectDir = Join-Path $root "src\DesktopLoudnessMeter.JucePlugin"
$aaxProjectDir = Join-Path $root "src\DesktopLoudnessMeter.JuceAaxPlugin"
$vst3BuildDirFull = [System.IO.Path]::GetFullPath($Vst3BuildDir)
$aaxBuildDirFull = [System.IO.Path]::GetFullPath($AaxBuildDir)

function Resolve-CMake {
    $command = Get-Command cmake -ErrorAction SilentlyContinue
    if ($null -ne $command) {
        return $command.Source
    }

    foreach ($candidate in @(
        "C:\Program Files\CMake\bin\cmake.exe",
        "C:\Program Files (x86)\CMake\bin\cmake.exe",
        "D:\Program Files\Microsoft Visual Studio\2022\BuildTools\Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe"
    )) {
        if (Test-Path -LiteralPath $candidate) {
            return $candidate
        }
    }

    throw "CMake was not found. Install CMake or add it to PATH."
}

function Resolve-CMakeTool {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ToolName,

        [Parameter(Mandatory = $true)]
        [string]$CMakePath
    )

    $command = Get-Command $ToolName -ErrorAction SilentlyContinue
    if ($null -ne $command) {
        return $command.Source
    }

    $sibling = Join-Path (Split-Path -Parent $CMakePath) $ToolName
    if (Test-Path -LiteralPath $sibling) {
        return $sibling
    }

    return $ToolName
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

$cmake = Resolve-CMake
$ctest = Resolve-CMakeTool "ctest.exe" $cmake
$copyVst3 = "ON"
$copyAax = "ON"
$buildTests = "OFF"

if ($NoInstallCopy) {
    $copyVst3 = "OFF"
    $copyAax = "OFF"
}

if ($RunTests) {
    $buildTests = "ON"
}

if ($Format -eq "VST3" -or $Format -eq "Both" -or $RunTests) {
    Invoke-Native "JUCE VST3 configure" $cmake @(
        "-S", $vst3ProjectDir,
        "-B", $vst3BuildDirFull,
        "-DJUCE_DIR=$JuceDir",
        "-DDLM_BUILD_JUCE_PLUGIN_TESTS=$buildTests",
        "-DDLM_COPY_VST3_AFTER_BUILD=$copyVst3",
        "-DDLM_COPY_EXTRA_VST3_AFTER_BUILD=$copyVst3"
    )

    if ($Format -eq "VST3" -or $Format -eq "Both") {
        Invoke-Native "JUCE VST3 build" $cmake @(
            "--build", $vst3BuildDirFull,
            "--config", $Configuration,
            "--target", "DesktopLoudnessMeterSender_VST3"
        )
    }

    if ($RunTests) {
        Invoke-Native "JUCE sender tests build" $cmake @(
            "--build", $vst3BuildDirFull,
            "--config", $Configuration,
            "--target", "DesktopLoudnessMeterJucePluginTests"
        )

        Invoke-Native "JUCE sender CTest" $ctest @(
            "--test-dir", $vst3BuildDirFull,
            "-C", $Configuration,
            "--output-on-failure"
        )
    }
}

if ($Format -eq "AAX" -or $Format -eq "Both") {
    $configureArgs = @(
        "-S", $aaxProjectDir,
        "-B", $aaxBuildDirFull,
        "-DJUCE_DIR=$JuceDir",
        "-DDLM_COPY_AAX_AFTER_BUILD=$copyAax"
    )

    if (-not [string]::IsNullOrWhiteSpace($AaxSdkDir)) {
        $configureArgs += "-DDLM_AAX_SDK_DIR=$AaxSdkDir"
    }

    Invoke-Native "JUCE AAX configure" $cmake $configureArgs

    Invoke-Native "JUCE AAX build" $cmake @(
        "--build", $aaxBuildDirFull,
        "--config", $Configuration,
        "--target", "DesktopLoudnessMeterSender_AAX"
    )
}
