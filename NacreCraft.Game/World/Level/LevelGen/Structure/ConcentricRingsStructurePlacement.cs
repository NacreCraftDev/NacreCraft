using NacreCraft.Codec;
using NacreCraft.Registry;

namespace NacreCraft.Game.World.Level.LevelGen.Structure;

//ConcentricRingsStructurePlacement concentric ring placement used by strongholds, maps to vanilla ConcentricRingsStructurePlacement
//Holds the ring parameters from the structure set; the ring position algorithm itself is still pending
public sealed class ConcentricRingsStructurePlacement : StructurePlacement
{
    //Distance distance between rings in chunks, maps to vanilla distance
    public int Distance { get; }

    //Spread random spread inside a ring, maps to vanilla spread
    public int Spread { get; }

    //Count total strongholds to place, maps to vanilla count
    public int Count { get; }

    //PreferredBiomes biomes a stronghold prefers, maps to vanilla preferredBiomes
    public HolderSet<Biome> PreferredBiomes { get; }

    internal ConcentricRingsStructurePlacement(PlacementParts parts, int distance, int spread, int count,
        HolderSet<Biome> preferredBiomes)
        : base(parts.LocateOffset, parts.ReductionMethod, parts.Frequency, parts.Salt, parts.Exclusion)
    {
        Distance = distance;
        Spread = spread;
        Count = count;
        PreferredBiomes = preferredBiomes;
    }

    //IsPlacementChunk returns false until the ring position algorithm is implemented
    //Generating at a guessed position would put strongholds in the wrong chunks, so placement stays off rather than approximate
    protected override bool IsPlacementChunk(ChunkGeneratorStructureState state, int sourceX, int sourceZ) => false;
}

//ConcentricRingsStructurePlacementType concentric ring placement type, maps to vanilla StructurePlacementType.CONCENTRIC_RINGS
public sealed class ConcentricRingsStructurePlacementType : StructurePlacementType
{
    public static readonly ConcentricRingsStructurePlacementType Instance =
        Register(Identifier.WithDefaultNamespace("concentric_rings"), new ConcentricRingsStructurePlacementType());

    private ConcentricRingsStructurePlacementType()
        : base(Identifier.WithDefaultNamespace("concentric_rings")) { }

    //Decode parses the common placement fields plus distance/spread/count/preferred_biomes
    public override DataResult<StructurePlacement> Decode<U>(DynamicOps<U> ops, MapLike<U> input)
    {
        var parts = StructurePlacementCodecs.ReadCommon(ops, input);
        if (!parts.Result().IsPresent)
            return DataResult<StructurePlacement>.Error(() => parts.ErrorMessage ?? "invalid placement");

        var distance = StructurePlacementCodecs.ReadIntField(ops, input, "distance");
        if (!distance.Result().IsPresent)
            return DataResult<StructurePlacement>.Error(() => "concentric_rings is missing distance");
        var spread = StructurePlacementCodecs.ReadIntField(ops, input, "spread");
        if (!spread.Result().IsPresent)
            return DataResult<StructurePlacement>.Error(() => "concentric_rings is missing spread");
        var count = StructurePlacementCodecs.ReadIntField(ops, input, "count");
        if (!count.Result().IsPresent)
            return DataResult<StructurePlacement>.Error(() => "concentric_rings is missing count");

        var preferredTag = input.Get("preferred_biomes");
        if (!preferredTag.IsPresent)
            return DataResult<StructurePlacement>.Error(() => "concentric_rings is missing preferred_biomes");
        var preferred = HolderSetCodecs.BiomeSet.Parse(ops, preferredTag.Get());
        if (!preferred.Result().IsPresent)
            return DataResult<StructurePlacement>.Error(() => "failed to parse preferred_biomes");

        return DataResult<StructurePlacement>.Success(new ConcentricRingsStructurePlacement(
            parts.GetOrThrow(), distance.GetOrThrow(), spread.GetOrThrow(), count.GetOrThrow(),
            preferred.GetOrThrow()));
    }
}
