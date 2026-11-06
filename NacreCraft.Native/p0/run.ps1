# P0 harness for the machine-code stack guard
# Builds the native layer and the test program, then starts the test under the profiler variables the product would use
# Usage: ./run.ps1                 run with the guard on
# Usage: ./run.ps1 -Guard off      run with the guard off, which is the baseline for the hot path measurement
# Usage: ./run.ps1 -Bench 50000000 run the hot path benchmark instead of the checks

param(
    [ValidateSet("on", "off")] [string]$Guard = "on",
    [long]$Bench = 0,
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$Harness = $PSScriptRoot
$Native = Split-Path -Parent $Harness

Push-Location $Native
try {
    & cargo build --release
    if ($LASTEXITCODE -ne 0) {
        throw "cargo build failed with exit code $LASTEXITCODE"
    }
}
finally {
    Pop-Location
}

$App = Join-Path $Harness "NcnHookTest"
& dotnet build (Join-Path $App "NcnHookTest.csproj") -c $Configuration -v quiet
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed with exit code $LASTEXITCODE"
}

$Output = Join-Path $App "bin\$Configuration\net10.0"
$Library = Join-Path $Native "target\release\nacrecraft_native.dll"
Copy-Item $Library (Join-Path $Output "nacrecraft_native.dll") -Force
Remove-Item (Join-Path $Output "nacrecraft_native.log") -ErrorAction SilentlyContinue

# The profiler is loaded by the runtime from these three variables before any managed code runs
$env:CORECLR_ENABLE_PROFILING = "1"
$env:CORECLR_PROFILER = "{2B5F8D34-6C1A-4E27-9B3D-52E8714AC618}"
$env:CORECLR_PROFILER_PATH = Join-Path $Output "nacrecraft_native.dll"

# The guard only arms after the throw entry is handed over; leaving the flag out of the child would disable it
$env:NC_NATIVE_ATTACHED = "1"
$env:NCN_STACK_HOOK = if ($Guard -eq "on") { "1" } else { "0" }

# Only this assembly is accepted, which keeps the test focused and the trampoline table small
$env:NCN_REWRITE_MODULES = "NcnHookTest"
$env:NCN_STACK_RESERVE = "262144"

# The measured run wants to see which entry shapes the JIT produced
$env:NCN_HOOK_TRACE = "1"

$Executable = Join-Path $Output "NcnHookTest.exe"
if ($Bench -gt 0) {
    & $Executable --bench $Bench
}
else {
    & $Executable
}

$Code = $LASTEXITCODE

Remove-Item Env:CORECLR_ENABLE_PROFILING, Env:CORECLR_PROFILER, Env:CORECLR_PROFILER_PATH, `
    Env:NC_NATIVE_ATTACHED, Env:NCN_STACK_HOOK, Env:NCN_REWRITE_MODULES, Env:NCN_STACK_RESERVE, `
    Env:NCN_HOOK_TRACE -ErrorAction SilentlyContinue

exit $Code
