# ================= 前置：函数 + 变量（一次定义，会话内有效） =================
function Show-Lines {
    param([Parameter(Mandatory)][string]$Path,
          [Parameter(Mandatory)][int]$From,
          [Parameter(Mandatory)][int]$To)
    if (-not (Test-Path -LiteralPath $Path)) {
        Write-Host "!! 文件不存在: $Path" -ForegroundColor Red; return
    }
    Write-Host ("`n===== {0} : {1}-{2} =====" -f $Path, $From, $To) -ForegroundColor Cyan
    Get-Content -LiteralPath $Path |
        Select-Object -Skip ($From-1) -First ($To-$From+1) |
        ForEach-Object -Begin { $i = $From } -Process { "{0,5}: {1}" -f $i, $_; $i++ }
}
$R = 'D:\Programming\C#\NetCraft\NetCraft'   # ← 若报"文件不存在"，把 $R 改成实际仓库根
Write-Host "R = $R"
Test-Path -LiteralPath $R

# ================= 组 1：真 bug / 隐患（先跑这段） =================
Show-Lines "$R\NacreCraft.Util\ReportedException.cs" 1 40
Show-Lines "$R\NacreCraft.Game\World\Level\LevelGen\Synth\PerlinNoise.cs" 55 110
Show-Lines "$R\NacreCraft.Storage\Light\LayerLightSectionStorage.cs" 35 65
Show-Lines "$R\NacreCraft.DataFixer\TypeObjectWrapper.cs" 25 60
Show-Lines "$R\NacreCraft.Resources\ZipPackResources.cs" 55 85
Show-Lines "$R\NacreCraft.Resources\VanillaPackResources.cs" 25 55
Show-Lines "$R\NacreCraft.Resources\PackResources.cs" 25 45
Show-Lines "$R\NacreCraft.Network\FriendlyByteBuf.cs" 390 410
Show-Lines "$R\NacreCraft.Commands\Execution\ExecutionContext.cs" 140 160
Show-Lines "$R\NacreCraft.Server\ServerGui\PotatoIcon.cs" 115 145
Show-Lines "$R\NacreCraft.Server\ServerGui\ServerLogPage.axaml" 15 25
Get-Content -LiteralPath "$R\NacreCraft.ServerExe\NacreCraft.ServerExe.csproj"
Show-Lines "$R\NacreCraft.Registry\IdMap.cs" 28 48
Show-Lines "$R\NacreCraft.DataFixer\DSL.cs" 150 175
Show-Lines "$R\NacreCraft.Nbt\SnbtGrammar.CreateParser.cs" 88 112
Show-Lines "$R\NacreCraft.Nbt\SnbtGrammar.CreateParser.cs" 345 360
Show-Lines "$R\NacreCraft.Game\Commands\Arguments\EntityArgument.cs" 38 58
Show-Lines "$R\NacreCraft.Game\Commands\Synchronization\ArgumentTypeInfos.cs" 242 260