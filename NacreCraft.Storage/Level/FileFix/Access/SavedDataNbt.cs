using NacreCraft.Codec;
using NacreCraft.DataFixer;
using NacreCraft.Nbt;

namespace NacreCraft.Storage;

//SavedDataNbt one saved-data file, maps to vanilla net.minecraft.util.filefix.access.SavedDataNbt
//The payload sits under the "data" key and the data version sits beside it
public sealed class SavedDataNbt : CompressedNbt
{
    private readonly DSL.ITypeReference _type;
    private readonly int _targetVersion;
    private readonly NacreCraft.DataFixer.DataFixer _fixer;

    public SavedDataNbt(DSL.ITypeReference type, string path, int targetVersion,
        MissingSeverity missingSeverity, NacreCraft.DataFixer.DataFixer fixer)
        : base(path, missingSeverity)
    {
        _type = type;
        _targetVersion = targetVersion;
        _fixer = fixer;
    }

    public override Optional<Dynamic<Tag>> Read()
        => ReadFile().Map(data =>
        {
            var version = NbtUtils.GetDataVersion(data);
            var updated = _fixer.Update(_type, data, version, _targetVersion);
            return updated.Get("data").OrElse(updated.EmptyMap());
        });

    public override void Write(Dynamic<Tag> data)
        => WriteFile(NbtUtils.AddDataVersion(data.EmptyMap().Set("data", data), _targetVersion));
}
