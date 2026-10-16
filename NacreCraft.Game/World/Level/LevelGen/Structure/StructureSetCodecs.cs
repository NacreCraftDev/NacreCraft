using NacreCraft.Codec;
using NacreCraft.Registry;

namespace NacreCraft.Game.World.Level.LevelGen.Structure;

//StructureSetCodecs structure set codec entry point, maps to vanilla StructureSet.CODEC
public static class StructureSetCodecs
{
    //ElementCodec structure set registry element codec
    public static readonly Codec<NacreCraft.Registry.StructureSet> ElementCodec = new StructureSetCodec();
}

//StructureSetCodec parses one placement config and a weighted structure list, maps to vanilla StructureSet.CODEC
internal sealed class StructureSetCodec : ScalarCodec<NacreCraft.Registry.StructureSet>
{
    public override DataResult<NacreCraft.Registry.StructureSet> Parse<U>(DynamicOps<U> ops, U input)
    {
        var mapResult = ops.GetMap(input);
        if (!mapResult.Result().IsPresent) return DataResult<NacreCraft.Registry.StructureSet>.Error(() => "structure set must be an object");
        var map = mapResult.GetOrThrow();

        var placementTag = map.Get("placement");
        if (!placementTag.IsPresent) return DataResult<NacreCraft.Registry.StructureSet>.Error(() => "structure set is missing placement");
        var placementError = string.Empty;
        var placement = StructurePlacementCodecs.ElementCodec.Parse(ops, placementTag.Get())
            .ResultOrPartial(message => placementError = message);
        if (!placement.IsPresent)
            return DataResult<NacreCraft.Registry.StructureSet>.Error(() => $"failed to parse structure set placement {placementError}");

        var structuresTag = map.Get("structures");
        if (!structuresTag.IsPresent) return DataResult<NacreCraft.Registry.StructureSet>.Error(() => "structure set is missing structures");
        var stream = ops.GetStream(structuresTag.Get());
        if (!stream.Result().IsPresent) return DataResult<NacreCraft.Registry.StructureSet>.Error(() => "structures must be an array");

        var entries = new List<StructureSelectionEntry>();
        foreach (var element in stream.GetOrThrow())
        {
            var entryResult = ops.GetMap(element);
            if (!entryResult.Result().IsPresent) return DataResult<NacreCraft.Registry.StructureSet>.Error(() => "structures entries must be objects");
            var entry = entryResult.GetOrThrow();
            var structureTag = entry.Get("structure");
            if (!structureTag.IsPresent) return DataResult<NacreCraft.Registry.StructureSet>.Error(() => "structures entry is missing structure");
            var structureError = string.Empty;
            var structure = HolderSetCodecs.StructureRef.Parse(ops, structureTag.Get())
                .ResultOrPartial(message => structureError = message);
            if (!structure.IsPresent)
                return DataResult<NacreCraft.Registry.StructureSet>.Error(() => $"failed to parse structures entry structure {structureError}");
            var weight = StructurePlacementCodecs.ReadIntField(ops, entry, "weight");
            if (!weight.Result().IsPresent) return DataResult<NacreCraft.Registry.StructureSet>.Error(() => "structures entry is missing weight");
            entries.Add(new StructureSelectionEntry(structure.Get(), weight.GetOrThrow()));
        }

        return DataResult<NacreCraft.Registry.StructureSet>.Success(
            new StructureSet(placement.Get(), entries));
    }

    public override DataResult<U> EncodeStart<U>(DynamicOps<U> ops, NacreCraft.Registry.StructureSet value)
        => DataResult<U>.Error(() => "structure set encoding not implemented yet");
}

//StructureBootstrap structure subsystem type registration, maps to the structure type registration in vanilla BuiltInRegistries
//Each type registers idempotently through its static Instance; touching them here once ensures registration completes before the registry freezes
public static class StructureBootstrap
{
    public static void RegisterAll()
    {
        _ = RandomSpreadStructurePlacementType.Instance;
    }
}
