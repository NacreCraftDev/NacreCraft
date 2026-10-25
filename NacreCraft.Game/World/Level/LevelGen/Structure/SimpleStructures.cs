using NacreCraft.Codec;
using NacreCraft.Registry;

namespace NacreCraft.Game.World.Level.LevelGen.Structure;

//SimpleStructure data-driven structure whose generator has not landed yet
//It parses the common settings so the registry element decodes and the structure set keeps its reference,
//but FindGenerationPoint returns null, so the world currently generates nothing for it
public sealed class SimpleStructure : Structure
{
    private readonly Identifier _id;
    private readonly StructureType _type;

    public SimpleStructure(Identifier id, StructureType type, StructureGenerationSettings settings)
        : base(settings)
    {
        _id = id;
        _type = type;
    }

    public override Identifier Id => _id;

    public override StructureType Type => _type;

    //TemplateManager injected by world assembly, ready for the generator once it is implemented
    public StructureTemplateManager? TemplateManager { get; set; }

    //FindGenerationPoint returns null until this structure's generator is implemented
    //Returning null is the valid "this chunk generates nothing" answer, so the structure registry stays loadable
    public override GenerationStub? FindGenerationPoint(GenerationContext context) => null;
}

//SimpleStructureCodec parses the common structure settings; type-specific fields are ignored until their generators land
internal sealed class SimpleStructureCodec : ScalarCodec<NacreCraft.Registry.Structure>
{
    private readonly Identifier _id;
    private StructureType? _type;

    public SimpleStructureCodec(Identifier id) => _id = id;

    //Type set after the StructureType is created, breaking the codec/type construction cycle
    public StructureType? Type { set => _type = value; }

    public override DataResult<NacreCraft.Registry.Structure> Parse<U>(DynamicOps<U> ops, U input)
    {
        var mapResult = ops.GetMap(input);
        if (!mapResult.Result().IsPresent)
            return DataResult<NacreCraft.Registry.Structure>.Error(() => "structure must be an object");
        var settings = StructureCodecs.ReadSettings(ops, mapResult.GetOrThrow());
        if (!settings.Result().IsPresent)
            return DataResult<NacreCraft.Registry.Structure>.Error(
                () => settings.ErrorMessage ?? "invalid structure settings");
        return DataResult<NacreCraft.Registry.Structure>.Success(
            new SimpleStructure(_id, _type!, settings.GetOrThrow()));
    }

    public override DataResult<U> EncodeStart<U>(DynamicOps<U> ops, NacreCraft.Registry.Structure value)
        => DataResult<U>.Error(() => "structure encoding not implemented yet");
}

//SimpleStructures registers the non-jigsaw structure types whose generators are still pending
//Maps to vanilla BuiltInRegistries.STRUCTURE_TYPE entries for these structures; registering the type is what lets
//worldgen/structure/&lt;name&gt;.json decode, since the dispatch codec rejects an unregistered type before reading settings
public static class SimpleStructures
{
    public static readonly StructureType BuriedTreasure = Register("buried_treasure");
    public static readonly StructureType DesertPyramid = Register("desert_pyramid");
    public static readonly StructureType EndCity = Register("end_city");
    public static readonly StructureType Fortress = Register("fortress");
    public static readonly StructureType Igloo = Register("igloo");
    public static readonly StructureType JungleTemple = Register("jungle_temple");
    public static readonly StructureType WoodlandMansion = Register("woodland_mansion");
    public static readonly StructureType Mineshaft = Register("mineshaft");
    public static readonly StructureType OceanMonument = Register("ocean_monument");
    public static readonly StructureType NetherFossil = Register("nether_fossil");
    public static readonly StructureType OceanRuin = Register("ocean_ruin");
    public static readonly StructureType RuinedPortal = Register("ruined_portal");
    public static readonly StructureType Shipwreck = Register("shipwreck");
    public static readonly StructureType Stronghold = Register("stronghold");
    public static readonly StructureType SwampHut = Register("swamp_hut");

    //RegisterAll touches every static field so their registration runs before the registry freezes
    public static void RegisterAll()
    {
        _ = BuriedTreasure;
        _ = DesertPyramid;
        _ = EndCity;
        _ = Fortress;
        _ = Igloo;
        _ = JungleTemple;
        _ = WoodlandMansion;
        _ = Mineshaft;
        _ = OceanMonument;
        _ = NetherFossil;
        _ = OceanRuin;
        _ = RuinedPortal;
        _ = Shipwreck;
        _ = Stronghold;
        _ = SwampHut;
    }

    private static StructureType Register(string path)
    {
        var id = Identifier.WithDefaultNamespace(path);
        var existing = BuiltInRegistries.STRUCTURE_TYPE.GetValue(id);
        if (existing is StructureType registered) return registered;
        var codec = new SimpleStructureCodec(id);
        var type = new StructureType(id, codec);
        codec.Type = type;
        return (StructureType)Registry<NacreCraft.Registry.StructureType<object>>.Register(
            BuiltInRegistries.STRUCTURE_TYPE, id, type);
    }
}
