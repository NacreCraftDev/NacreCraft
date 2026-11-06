# P2 overflow comparison: run one recursion to its end under each guard mode
#
# The three modes share the same program and the same recursion. With a guard the process catches
# InsufficientExecutionStackException and exits 0; without one it reaches the real guard page and the runtime ends it.
#
# Usage: ./compare-overflow.ps1 [-BuildNative] [-Configuration Debug]

param(
    [switch]$BuildNative,
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

$Here = $PSScriptRoot
$Native = Split-Path -Parent $Here
$Repo = Split-Path -Parent $Native
$Project = Join-Path $Here "OverflowTest\OverflowTest.csproj"
$Output = Join-Path $Here "OverflowTest\bin\$Configuration\net10.0"
$Executable = Join-Path $Output "OverflowTest.exe"
$NativeDll = Join-Path $Output "nacrecraft_native.dll"

if ($BuildNative) {
    & (Join-Path $Native "build.ps1")
    if ($LASTEXITCODE -ne 0) { throw "native build failed" }
}

& dotnet build $Project -c $Configuration -v quiet
if ($LASTEXITCODE -ne 0) { throw "OverflowTest build failed" }

Copy-Item (Join-Path $Native "out\win-x64\nacrecraft_native.dll") $NativeDll -Force

$Results = Join-Path $Here "results"
New-Item -ItemType Directory -Force -Path $Results | Out-Null
$Stamp = Get-Date -Format "yyyyMMdd-HHmmss"

$Modes = @(
    [pscustomobject]@{ Name = "default"; Check = ""; Hook = "" },
    [pscustomobject]@{ Name = "off"; Check = "0"; Hook = "0" },
    [pscustomobject]@{ Name = "machine"; Check = "1"; Hook = "1" }
)

$Rows = @()

foreach ($mode in $Modes) {
    $env:CORECLR_ENABLE_PROFILING = "1"
    $env:CORECLR_PROFILER = "{2B5F8D34-6C1A-4E27-9B3D-52E8714AC618}"
    $env:CORECLR_PROFILER_PATH = $NativeDll
    $env:NC_NATIVE_ATTACHED = "1"
    $env:NCN_STACK_CHECK = $mode.Check
    $env:NCN_STACK_HOOK = $mode.Hook
    $env:NCN_REWRITE_MODULES = "OverflowTest"
    $env:NCN_STACK_RESERVE = "524288"
    $env:NCN_OVERFLOW_MODE = $mode.Name

    $Log = Join-Path $Results "$Stamp-$($mode.Name).log"

    # Direct invocation keeps $LASTEXITCODE, which is the process exit code; Start-Process left it empty here
    # The runtime writes its own stack overflow report to stderr, which PowerShell would turn into a terminating error
    $Previous = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    & $Executable *> $Log
    $Code = $LASTEXITCODE
    $ErrorActionPreference = $Previous

    $Lines = if (Test-Path $Log) { Get-Content $Log } else { @() }
    $Caught = ($Lines | Select-String -Pattern "caught=InsufficientExecutionStackException" -Quiet)
    $DepthLine = ($Lines | Select-String -Pattern "depth=(\d+)" | Select-Object -Last 1)
    $Depth = if ($DepthLine) { $DepthLine.Matches[0].Groups[1].Value } else { "0" }

    $Rows += [pscustomobject]@{
        Mode = $mode.Name
        Exit = ("0x{0:X8}" -f ([int]$Code))
        Caught = if ($Caught) { "yes" } else { "no" }
        Depth = $Depth
        Log = Split-Path $Log -Leaf
    }
}

$Rows | Format-Table -AutoSize

Remove-Item Env:CORECLR_ENABLE_PROFILING, Env:CORECLR_PROFILER, Env:CORECLR_PROFILER_PATH, Env:NC_NATIVE_ATTACHED, `
    Env:NCN_STACK_CHECK, Env:NCN_STACK_HOOK, Env:NCN_REWRITE_MODULES, Env:NCN_STACK_RESERVE, Env:NCN_OVERFLOW_MODE `
    -ErrorAction SilentlyContinue
