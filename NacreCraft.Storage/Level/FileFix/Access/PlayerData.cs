using NacreCraft.Codec;
using NacreCraft.DataFixer;
using NacreCraft.DataFixer.Fixes;
using NacreCraft.Nbt;

namespace NacreCraft.Storage;

//PlayerData one player data file, maps to vanilla net.minecraft.util.filefix.access.PlayerData
public sealed class PlayerData : CompressedNbt
{
    private readonly int _targetVersion;
    private readonly NacreCraft.DataFixer.DataFixer _fixer;

    public PlayerData(string path, int targetVersion, NacreCraft.DataFixer.DataFixer fixer)
        : base(path, MissingSeverity.NEUTRAL)
    {
        _targetVersion = targetVersion;
        _fixer = fixer;
    }

    public override Optional<Dynamic<Tag>> Read()
        => ReadFile().Map(data =>
        {
            var version = NbtUtils.GetDataVersion(data);
            return _fixer.Update(References.Player, data, version, _targetVersion);
        });

    public override void Write(Dynamic<Tag> data)
        => WriteFile(NbtUtils.AddDataVersion(data, _targetVersion));
}
