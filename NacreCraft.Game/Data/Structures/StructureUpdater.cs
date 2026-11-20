using NacreCraft.DataFixer;
using NacreCraft.Game.World.Level.LevelGen.Structure;
using NacreCraft.Logging;
using NacreCraft.Nbt;
//The DataFixer namespace shadows the type name inside an unqualified reference, so the type gets an alias
using DataFixerType = NacreCraft.DataFixer.DataFixer;

namespace NacreCraft.Game.Data.Structures;

//StructureUpdater upgrades old structure templates while SNBT is converted back to NBT, maps to vanilla StructureUpdater
//Only paths under data/minecraft/structure are touched; the template is loaded and saved again so renamed blocks and
//outdated palette entries normalize to the current format. NC has no global data fixer, so one can be passed in
public sealed class StructureUpdater : SnbtToNbt.Filter
{
    //Prefix the output path prefix of structure templates, maps to PackType.SERVER_DATA.getDirectory() + "/minecraft/structure/"
    public const string Prefix = "data/minecraft/structure/";

    private readonly DataFixerType? _fixer;

    public StructureUpdater()
    {
    }

    public StructureUpdater(DataFixerType? fixer) => _fixer = fixer;

    public CompoundTag Apply(string name, CompoundTag input)
        => name.StartsWith(Prefix, StringComparison.Ordinal) ? Update(name, input, _fixer) : input;

    //Update normalizes one structure template, maps to vanilla StructureUpdater.update
    //The data version below 4882 only warns, matching vanilla; without a fixer the tag is loaded as is
    public static CompoundTag Update(string name, CompoundTag tag, DataFixerType? fixer = null)
    {
        var fromVersion = NbtUtils.GetDataVersion(tag, 500);
        if (fromVersion < 4882)
            Log.Warning($"SNBT too old, do not forget to update: {fromVersion} < 4882: {name}");
        var updated = fixer is null ? tag : DataFixTypes.Structure.UpdateToCurrentVersion(fixer, tag, fromVersion);
        var structureTemplate = new StructureTemplate();
        structureTemplate.Load(updated);
        return structureTemplate.Save(new CompoundTag());
    }
}
