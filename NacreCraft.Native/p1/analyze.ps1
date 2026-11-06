# P1 analysis: split each measured run into its warm-up and steady chunk windows
#
# The per-chunk report is a windowed average, so replaying it chunk by chunk is an approximation; it is enough to keep
# the JIT-heavy first hundred chunks out of the steady figure. Only runs after the fixed level-seed change are useful.

param(
    [string]$Results = (Join-Path $PSScriptRoot "results"),
    [datetime]$Since = [datetime]::MinValue,
    [int]$WarmupChunks = 120
)

$runs = @()

foreach ($file in Get-ChildItem $Results -Filter "*.out.log" | Sort-Object LastWriteTime) {
    if ($file.LastWriteTime -lt $Since) { continue }
    if ($file.Name -notmatch "-(?<mode>default|off|il|machine)\.out\.log$") { continue }

    $mode = $Matches['mode']
    $chunks = 0
    $warm = New-Object System.Collections.Generic.List[double]
    $steady = New-Object System.Collections.Generic.List[double]

    foreach ($line in Get-Content $file.FullName) {
        if ($line -match "\[noise\] per-chunk create=\d+ms fill=(?<value>\d+)ms set=\d+ms builds=(?<builds>\d+)") {
            $built = [int]$Matches['builds']
            $value = [double]$Matches['value']
            for ($i = 0; $i -lt $built; $i++) {
                $chunks++
                if ($chunks -le $WarmupChunks) { $warm.Add($value) } else { $steady.Add($value) }
            }
        }
    }

    if ($chunks -eq 0) { continue }

    $warmAverage = if ($warm.Count -gt 0) { ($warm | Measure-Object -Average).Average } else { 0 }
    $steadyAverage = if ($steady.Count -gt 0) { ($steady | Measure-Object -Average).Average } else { 0 }

    $runs += [pscustomobject]@{
        Mode = $mode
        Time = $file.LastWriteTime.ToString("HH:mm:ss")
        Chunks = $chunks
        Warm = [math]::Round($warmAverage, 1)
        Steady = [math]::Round($steadyAverage, 1)
    }
}

$runs | Format-Table -AutoSize

$runs | Group-Object Mode | ForEach-Object {
    $steady = $_.Group | ForEach-Object { $_.Steady }
    $warm = $_.Group | ForEach-Object { $_.Warm }
    $steadyStats = $steady | Measure-Object -Average -Minimum
    $warmStats = $warm | Measure-Object -Average -Minimum
    [pscustomobject]@{
        Mode = $_.Name
        Runs = $steadyStats.Count
        WarmMean = [math]::Round($warmStats.Average, 1)
        WarmMin = [math]::Round($warmStats.Minimum, 1)
        SteadyMean = [math]::Round($steadyStats.Average, 1)
        SteadyMin = [math]::Round($steadyStats.Minimum, 1)
    }
} | Format-Table -AutoSize
