param(
    [ValidateSet("VST3", "AAX", "Both")]
    [string]$Format = "VST3",

    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [string]$JuceDir = "",

    [string]$Vst3BuildDir = (Join-Path $PSScriptRoot "..\build\juce-vst3"),

    [string]$AaxBuildDir = (Join-Path $PSScriptRoot "..\build\juce-aax"),

    [string]$AaxSdkDir = "",

    [switch]$InstallCopy,

    [switch]$NoInstallCopy,

    [switch]$RunTests
)

$ErrorActionPreference = "Stop"
$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$vst3ProjectDir = Join-Path $root "src\DesktopLoudnessMeter.JucePlugin"
$aaxProjectDir = Join-Path $root "src\DesktopLoudnessMeter.JuceAaxPlugin"
$vst3BuildDirFull = [System.IO.Path]::GetFullPath($Vst3BuildDir)
$aaxBuildDirFull = [System.IO.Path]::GetFullPath($AaxBuildDir)

function Test-IsUnderRoot {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$RootPath
    )

    $fullPath = [System.IO.Path]::GetFullPath($Path).TrimEnd([char[]]@('\', '/')) + '\'
    $fullRoot = [System.IO.Path]::GetFullPath($RootPath).TrimEnd([char[]]@('\', '/')) + '\'
    return $fullPath.StartsWith($fullRoot, [System.StringComparison]::OrdinalIgnoreCase)
}

function Resolve-CMake {
    $command = Get-Command cmake -ErrorAction SilentlyContinue
    if ($null -ne $command) {
        return $command.Source
    }

    $vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path -LiteralPath $vswhere) {
        $vsCMake = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find "Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe" |
            Select-Object -First 1
        if ($vsCMake -and (Test-Path -LiteralPath $vsCMake)) {
            return $vsCMake
        }
    }

    foreach ($candidate in @(
        "C:\Program Files\CMake\bin\cmake.exe",
        "C:\Program Files (x86)\CMake\bin\cmake.exe"
    )) {
        if (Test-Path -LiteralPath $candidate) {
            return $candidate
        }
    }

    throw "CMake was not found. Install CMake or add it to PATH."
}

function Resolve-JuceDir {
    param(
        [string]$ConfiguredPath
    )

    if (-not [string]::IsNullOrWhiteSpace($ConfiguredPath)) {
        if (Test-Path -LiteralPath (Join-Path $ConfiguredPath "CMakeLists.txt")) {
            return [System.IO.Path]::GetFullPath($ConfiguredPath)
        }

        throw "JUCE_DIR must point to a JUCE checkout containing CMakeLists.txt. Current value: $ConfiguredPath"
    }

    $candidates = @(
        $env:JUCE_DIR,
        $env:JUCE_ROOT,
        "C:\JUCE",
        "E:\JUCE"
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique

    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath (Join-Path $candidate "CMakeLists.txt")) {
            return [System.IO.Path]::GetFullPath($candidate)
        }
    }

    throw "JUCE checkout was not found. Pass -JuceDir <path> or set JUCE_DIR to a checkout containing CMakeLists.txt."
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

function Clear-StaleCMakeBuildDirectory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$BuildDir,

        [Parameter(Mandatory = $true)]
        [string]$SourceDir
    )

    $cache = Join-Path $BuildDir "CMakeCache.txt"
    if (-not (Test-Path -LiteralPath $cache)) {
        return
    }

    $cachedHome = Select-String -Path $cache -Pattern '^CMAKE_HOME_DIRECTORY:INTERNAL=(.*)$' |
        ForEach-Object { $_.Matches[0].Groups[1].Value } |
        Select-Object -First 1

    if ([string]::IsNullOrWhiteSpace($cachedHome)) {
        return
    }

    $sourceFull = [System.IO.Path]::GetFullPath($SourceDir).TrimEnd([char[]]@('\', '/'))
    $cachedFull = [System.IO.Path]::GetFullPath($cachedHome).TrimEnd([char[]]@('\', '/'))
    if ($sourceFull.Equals($cachedFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        return
    }

    if (-not (Test-IsUnderRoot -Path $BuildDir -RootPath $root)) {
        throw "Refusing to clean stale CMake build directory outside the repository: $BuildDir"
    }

    Write-Host "Removing stale CMake build directory: $BuildDir"
    Write-Host "  cached source: $cachedFull"
    Write-Host "  current source: $sourceFull"
    Remove-Item -LiteralPath $BuildDir -Recurse -Force
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
$JuceDir = Resolve-JuceDir $JuceDir
$copyVst3 = "OFF"
$copyAax = "OFF"
$buildTests = "OFF"

if ($InstallCopy -and $NoInstallCopy) {
    throw "Use either -InstallCopy or -NoInstallCopy, not both."
}

if ($InstallCopy) {
    $copyVst3 = "ON"
    $copyAax = "ON"
}

if ($RunTests) {
    $buildTests = "ON"
}

if ($Format -eq "VST3" -or $Format -eq "Both" -or $RunTests) {
    Clear-StaleCMakeBuildDirectory -BuildDir $vst3BuildDirFull -SourceDir $vst3ProjectDir

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
    Clear-StaleCMakeBuildDirectory -BuildDir $aaxBuildDirFull -SourceDir $aaxProjectDir

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
