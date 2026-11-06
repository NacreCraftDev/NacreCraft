# P1 measurement: one server run under one stack-guard mode
#
# Runs the dedicated server headless against a fresh world, waits for the ready line, connects a minimal protocol probe
# so the spawn preload and the view-distance chunk load actually happen, and then stops the process. Console output, the
# native log and a parsed summary are left in p1/results for the comparison.
#
# The probe performs: handshake, login start, login acknowledged, select_known_packs (empty), finish_configuration, then
# it drains whatever the server sends until the window ends. Online mode is turned off in server.properties first.
#
# Usage: ./measure-server.ps1 -Mode default|off|machine [-Seconds 30] [-BuildNative]

param(
    [ValidateSet("default", "off", "machine")] [string]$Mode = "default",
    [int]$Seconds = 30,
    [string]$Configuration = "Debug",
    [switch]$BuildNative
)

$ErrorActionPreference = "Stop"

trap {
    Write-Host ("[p1] failed: " + $_.Exception.Message)
    Write-Host $_.ScriptStackTrace
    exit 9
}

Write-Host "[p1] mode=$Mode configuration=$Configuration"

$Here = $PSScriptRoot
$Native = Split-Path -Parent $Here
$Repo = Split-Path -Parent $Native
$ServerDir = Join-Path $Repo "NacreCraft.ServerStart\bin\$Configuration\net10.0"
$Executable = Join-Path $ServerDir "NacreCraft.ServerStart.exe"
$NativeDll = Join-Path $ServerDir "runtimes\win-x64\native\nacrecraft_native.dll"
$Properties = Join-Path $ServerDir "server.properties"

if (-not (Test-Path $Executable)) {
    throw "server executable missing at $Executable; build the solution first"
}

if ($BuildNative) {
    & (Join-Path $Native "build.ps1")
    if ($LASTEXITCODE -ne 0) {
        throw "native build failed"
    }

    $Built = Join-Path $Native "out\win-x64\nacrecraft_native.dll"
    if (Test-Path $Built) {
        Copy-Item $Built $NativeDll -Force
    }
}

if (-not (Test-Path $NativeDll)) {
    throw "native library missing at $NativeDll"
}

# One fresh world per run: the whole point is generation, and an existing save would skip it
Remove-Item (Join-Path $ServerDir "world") -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item (Join-Path $ServerDir "logs") -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item (Join-Path $ServerDir "nacrecraft_native.log") -Force -ErrorAction SilentlyContinue

$Results = Join-Path $Here "results"
New-Item -ItemType Directory -Force -Path $Results | Out-Null
$Stamp = Get-Date -Format "yyyyMMdd-HHmmss"
Write-Host "[p1] world cleaned, server dir $ServerDir"

# The probe logs in offline, and every run has to generate the same terrain; both keys are forced here
if (Test-Path $Properties) {
    $text = [System.IO.File]::ReadAllText($Properties)
    $text = if ($text -match '(?m)^online-mode=') { $text -replace '(?m)^online-mode=.*$', 'online-mode=false' } else { $text + "`r`nonline-mode=false`r`n" }
    $text = if ($text -match '(?m)^level-seed=') { $text -replace '(?m)^level-seed=.*$', 'level-seed=12345' } else { $text + "`r`nlevel-seed=12345`r`n" }
    [System.IO.File]::WriteAllText($Properties, $text, (New-Object System.Text.UTF8Encoding($false)))
}

$Port = 25565
if (Test-Path $Properties) {
    $portLine = [System.IO.File]::ReadAllLines($Properties) | Where-Object { $_ -match '^server-port=(\d+)' } | Select-Object -First 1
    if ($portLine -and $portLine -match '^server-port=(\d+)') { $Port = [int]$Matches[1] }
}

# The profiler variables are set here, so the relay in NativeRelaunch sees NC_NATIVE_ATTACHED and leaves the process
# alone; one process means one pid to wait on and kill
$env:CORECLR_ENABLE_PROFILING = "1"
$env:CORECLR_PROFILER = "{2B5F8D34-6C1A-4E27-9B3D-52E8714AC618}"
$env:CORECLR_PROFILER_PATH = $NativeDll
$env:NC_NATIVE_ATTACHED = "1"

# default leaves both switches unset, which is the product behaviour now that the guard is on by default
$env:NCN_STACK_CHECK = if ($Mode -eq "off") { "0" } else { "" }
$env:NCN_STACK_HOOK = if ($Mode -eq "machine") { "1" } else { "" }
# Both guarded modes are narrowed to the game assembly, which is the scope the plan gives P1
$env:NCN_REWRITE_MODULES = "NacreCraft.Game"
$env:NCN_STACK_RESERVE = "524288"
# Per-hook tracing is a P0 tool; a measured run only keeps the periodic count
$env:NCN_HOOK_TRACE = "0"

$OutLog = Join-Path $Results "$Stamp-$Mode.out.log"
$ErrLog = Join-Path $Results "$Stamp-$Mode.err.log"

$Clock = [System.Diagnostics.Stopwatch]::StartNew()
$Process = Start-Process -FilePath $Executable -ArgumentList "--nogui", "--noconsole" `
    -WorkingDirectory $ServerDir -RedirectStandardOutput $OutLog -RedirectStandardError $ErrLog `
    -PassThru -NoNewWindow
Write-Host "[p1] server started pid=$($Process.Id)"

# Wait for the ready line; the initial tracked chunks are loaded before it, the real generation follows the login
$Ready = $false
while ($Clock.Elapsed.TotalSeconds -lt 240) {
    if ($Process.HasExited) { break }
    if (Test-Path $OutLog) {
        if (Select-String -Path $OutLog -Pattern "NacreCraft server ready" -Quiet -ErrorAction SilentlyContinue) {
            $Ready = $true
            break
        }
    }
    Start-Sleep -Milliseconds 250
}

if (-not $Ready) {
    if (-not $Process.HasExited) { Stop-Process -Id $Process.Id -Force -ErrorAction SilentlyContinue }
    Write-Host "[p1] no ready line within 240s"
    exit 3
}

Write-Host "[p1] ready after $([math]::Round($Clock.Elapsed.TotalSeconds, 1))s, connecting the probe"

# The probe speaks just enough of the protocol to make the server run its spawn preload and the view-distance load
$Probe = Start-Job -ArgumentList $Port, $Seconds -ScriptBlock {
    param([int]$Port, [int]$Seconds)

    function Write-VarInt([System.IO.Stream]$stream, [long]$value) {
        do {
            $byte = [byte]($value -band 0x7F)
            $value = $value -shr 7
            if ($value -ne 0) { $byte = $byte -bor 0x80 }
            $stream.WriteByte($byte)
        } while ($value -ne 0)
    }

    function Write-String([System.IO.Stream]$stream, [string]$text) {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($text)
        Write-VarInt $stream $bytes.Length
        $stream.Write($bytes, 0, $bytes.Length)
    }

    function Send-Packet([System.IO.Stream]$stream, [byte[]]$payload) {
        Write-VarInt $stream $payload.Length
        $stream.Write($payload, 0, $payload.Length)
        $stream.Flush()
    }

    $client = New-Object System.Net.Sockets.TcpClient
    $client.NoDelay = $true
    $client.Connect("127.0.0.1", $Port)
    $stream = $client.GetStream()

    # Handshake: protocol 776, localhost, next state login
    $payload = New-Object System.IO.MemoryStream
    Write-VarInt $payload 0x00
    Write-VarInt $payload 776
    Write-String $payload "127.0.0.1"
    $portBytes = [BitConverter]::GetBytes([uint16]$Port)
    [Array]::Reverse($portBytes)
    $payload.Write($portBytes, 0, 2)
    Write-VarInt $payload 2
    Send-Packet $stream $payload.ToArray()

    # Login start: name and a fixed UUID
    $payload = New-Object System.IO.MemoryStream
    Write-VarInt $payload 0x00
    Write-String $payload "P1Probe"
    $uuid = [byte[]](0..15)
    $payload.Write($uuid, 0, 16)
    Send-Packet $stream $payload.ToArray()

    Start-Sleep -Milliseconds 500
    Send-Packet $stream ([byte[]](0x03))          # login acknowledged
    Start-Sleep -Milliseconds 500
    Send-Packet $stream ([byte[]](0x07, 0x00))    # select_known_packs, empty
    Start-Sleep -Milliseconds 1000
    Send-Packet $stream ([byte[]](0x03))          # finish_configuration

    # Drain whatever the server sends so its write side never blocks on a full socket buffer
    $stream.ReadTimeout = 100
    $buffer = New-Object byte[] 65536
    $deadline = (Get-Date).AddSeconds($Seconds)
    while ((Get-Date) -lt $deadline) {
        try { [void]$stream.Read($buffer, 0, $buffer.Length) } catch [System.IO.IOException] { }
        Start-Sleep -Milliseconds 20
    }

    $client.Close()
}

Start-Sleep -Seconds $Seconds

if (-not $Process.HasExited) {
    Stop-Process -Id $Process.Id -Force -ErrorAction SilentlyContinue
}
Stop-Job -Job $Probe -ErrorAction SilentlyContinue
Remove-Job -Job $Probe -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 500

# The console sink writes through as it goes; the log file is the fallback when stdout was not attached
$Lines = @()
if (Test-Path $OutLog) {
    $Lines = Get-Content $OutLog -Encoding UTF8
}
if (-not ($Lines | Select-String -Pattern "\[noise\] per-chunk" -Quiet)) {
    $Latest = Get-ChildItem (Join-Path $ServerDir "logs") -Filter "*.log" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime | Select-Object -Last 1
    if ($Latest) {
        $Lines = Get-Content $Latest.FullName -Encoding UTF8
    }
}

function Measure-Lines([string]$Pattern, [int]$FixedBuilds = 0) {
    $batches = 0
    $chunks = 0
    $total = 0.0
    foreach ($line in $Lines) {
        if ($line -match $Pattern) {
            $built = if ($FixedBuilds -gt 0) { $FixedBuilds } else { [int]$Matches['builds'] }
            $batches++
            $chunks += $built
            $total += [double]$Matches['value'] * $built
        }
    }
    [pscustomobject]@{ Batches = $batches; Chunks = $chunks; TotalMs = $total }
}

$Noise = Measure-Lines '\[noise\] per-chunk create=\d+ms fill=(?<value>\d+)ms set=\d+ms builds=(?<builds>\d+)'
# The surface report averages over a fixed fifty-chunk window and carries no builds field
$Surface = Measure-Lines '\[surface\] per-chunk .*?rule=(?<value>\d+)ms' 50
$Carvers = Measure-Lines '\[carvers\] per-chunk .*?carve=(?<value>\d+)ms .*?builds=(?<builds>\d+)'

$Boot = ($Lines | Select-String -Pattern "Done \(([\d.]+)s\)! NacreCraft server ready" | Select-Object -First 1)
$BootSeconds = if ($Boot) { $Boot.Matches[0].Groups[1].Value + "s" } else { "missing" }

$NativeLog = Join-Path $ServerDir "nacrecraft_native.log"
$HookCount = 0
$GuardLine = "no native log"
if (Test-Path $NativeLog) {
    $NativeLines = Get-Content $NativeLog -Encoding UTF8
    $Milestone = ($NativeLines | Select-String -Pattern "(\d+) method entries hooked so far" |
        Select-Object -Last 1)
    if ($Milestone) { $HookCount = [int]$Milestone.Matches[0].Groups[1].Value }
    $GuardLine = ($NativeLines | Select-String -Pattern "machine-code guard is armed|machine-code guard is off|early stack checks are off|module NacreCraft.Game: accepted" |
        Select-Object -Last 1).Line
    if (-not $GuardLine) { $GuardLine = "armed line missing" }
}

$Summary = [ordered]@{
    mode = $Mode
    boot = $BootSeconds
    noise_chunks = $Noise.Chunks
    noise_fill_ms = [math]::Round($Noise.TotalMs, 1)
    noise_fill_per_chunk = if ($Noise.Chunks -gt 0) { [math]::Round($Noise.TotalMs / $Noise.Chunks, 2) } else { 0 }
    surface_chunks = $Surface.Chunks
    surface_rule_ms = [math]::Round($Surface.TotalMs, 1)
    carver_chunks = $Carvers.Chunks
    carver_carve_ms = [math]::Round($Carvers.TotalMs, 1)
    hooks = $HookCount
    lines = $Lines.Count
    guard = $GuardLine
}

$Summary | ConvertTo-Json -Depth 3 | Set-Content -Path (Join-Path $Results "$Stamp-$Mode.json") -Encoding UTF8
$Summary.GetEnumerator() | ForEach-Object { "{0}: {1}" -f $_.Key, $_.Value }

Remove-Item Env:CORECLR_ENABLE_PROFILING, Env:CORECLR_PROFILER, Env:CORECLR_PROFILER_PATH, Env:NC_NATIVE_ATTACHED, `
    Env:NCN_STACK_CHECK, Env:NCN_STACK_HOOK, Env:NCN_REWRITE_MODULES, Env:NCN_STACK_RESERVE, Env:NCN_HOOK_TRACE `
    -ErrorAction SilentlyContinue
