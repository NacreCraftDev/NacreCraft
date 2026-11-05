using NacreCraft.Codec;
using NacreCraft.DataFixer;
using NacreCraft.DataFixer.Fixes;
using NacreCraft.Nbt;

namespace NacreCraft.Storage;

//LevelDat the world metadata file, maps to vanilla net.minecraft.util.filefix.access.LevelDat
public sealed class LevelDat : CompressedNbt
{
    private readonly int _targetVersion;
    private readonly NacreCraft.DataFixer.DataFixer _fixer;

    public LevelDat(string path, int targetVersion, NacreCraft.DataFixer.DataFixer fixer)
        : base(path, MissingSeverity.IMPORTANT)
    {
        _targetVersion = targetVersion;
        _fixer = fixer;
    }

    public override Optional<Dynamic<Tag>> Read()
        => ReadFile().Map(data =>
        {
            var content = data.Get("Data").OrElse(data.EmptyMap());
            var dataVersion = NbtUtils.GetDataVersion(content);
            return _fixer.Update(References.Level, content, dataVersion, _targetVersion);
        });

    public override void Write(Dynamic<Tag> data)
        => WriteFile(data.EmptyMap().Set("Data", NbtUtils.AddDataVersion(data, _targetVersion)));
}
