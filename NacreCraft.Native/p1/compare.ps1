# P1 comparison: interleaved runs of the three guard modes
#
# Running the modes in sequence lets the machine's drift fall on one mode only; cycling through all three keeps every
# sample inside the same window. The aggregate is printed and the per-run JSON stays in results/.
#
# Usage: ./compare.ps1 [-Cycles 3] [-Seconds 25]

param(
    [int]$Cycles = 3,
    [int]$Seconds = 25
)

$ErrorActionPreference = "Stop"
$Here = $PSScriptRoot
$Samples = @()

# Each cycle rotates the order, so a drift in machine state does not always land on the same mode
$Orders = @(
    @("off", "machine"),
    @("machine", "off")
)

foreach ($cycle in 1..$Cycles) {
    $Order = $Orders[($cycle - 1) % $Orders.Count]
    foreach ($mode in $Order) {
        Write-Host "[p1] cycle $cycle mode $mode"
        & (Join-Path $Here "measure-server.ps1") -Mode $mode -Seconds $Seconds | Out-Null

        $json = Get-ChildItem (Join-Path $Here "results") -Filter "*-$mode.json" |
            Sort-Object LastWriteTime | Select-Object -Last 1
        $sample = Get-Content $json.FullName -Raw | ConvertFrom-Json
        $Samples += [pscustomobject]@{
            Mode = $mode
            Cycle = $cycle
            Boot = [double]($sample.boot -replace "s", "")
            Fill = [double]$sample.noise_fill_per_chunk
            Hooks = [int]$sample.hooks
        }
    }
}

$Samples | Format-Table -AutoSize

$Samples | Group-Object Mode | ForEach-Object {
    $fill = $_.Group | ForEach-Object { $_.Fill }
    $boot = $_.Group | ForEach-Object { $_.Boot }
    $stats = $fill | Measure-Object -Average -Minimum -Maximum
    $bootStats = $boot | Measure-Object -Average
    [pscustomobject]@{
        Mode = $_.Name
        Samples = $stats.Count
        FillMean = [math]::Round($stats.Average, 1)
        FillMin = [math]::Round($stats.Minimum, 1)
        FillMax = [math]::Round($stats.Maximum, 1)
        BootMean = [math]::Round($bootStats.Average, 2)
        Hooks = ($_.Group | Measure-Object Hooks -Maximum).Maximum
    }
} | Format-Table -AutoSize
