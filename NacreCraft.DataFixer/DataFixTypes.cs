namespace NacreCraft.DataFixer;

using System;
using System.Collections.Generic;
using NacreCraft.Codec;
using NacreCraft.Config;
using NacreCraft.DataFixer.Fixes;

//MC data fix type maps to vanilla net.minecraft.util.datafix.DataFixTypes
//holds a DSL.ITypeReference identifier; delegates version upgrades to DataFixer by type
//vanilla uses an enum; here a sealed class + static instances emulate enum semantics
//the CompoundTag overload relies on NbtOps; the caller wraps Dynamic<Tag> itself, keeping DFU cross-ops pure
public sealed class DataFixTypes
{
    public static readonly DataFixTypes Level = new(References.Level);
    public static readonly DataFixTypes LevelSummary = new(References.LightweightLevel);
    public static readonly DataFixTypes Player = new(References.Player);
    public static readonly DataFixTypes Chunk = new(References.Chunk);
    public static readonly DataFixTypes Hotbar = new(References.Hotbar);
    public static readonly DataFixTypes Options = new(References.Options);
    public static readonly DataFixTypes Structure = new(References.Structure);
    public static readonly DataFixTypes Stats = new(References.Stats);
    public static readonly DataFixTypes SavedDataCommandStorage = new(References.SavedDataCommandStorage);
    public static readonly DataFixTypes SavedDataCustomBossEvents = new(References.SavedDataCustomBossEvents);
    public static readonly DataFixTypes SavedDataEnderDragonFight = new(References.SavedDataEnderDragonFight);
    public static readonly DataFixTypes SavedDataGameRules = new(References.SavedDataGameRules);
    public static readonly DataFixTypes SavedDataForcedChunks = new(References.SavedDataTickets);
    public static readonly DataFixTypes SavedDataMapData = new(References.SavedDataMapData);
    public static readonly DataFixTypes SavedDataMapIndex = new(References.SavedDataMapIndex);
    public static readonly DataFixTypes SavedDataRaids = new(References.SavedDataRaids);
    public static readonly DataFixTypes SavedDataRandomSequences = new(References.SavedDataRandomSequences);
    public static readonly DataFixTypes SavedDataScheduledEvents = new(References.SavedDataScheduledEvents);
    public static readonly DataFixTypes SavedDataScoreboard = new(References.SavedDataScoreboard);
    public static readonly DataFixTypes SavedDataStopwatches = new(References.SavedDataStopwatches);
    public static readonly DataFixTypes SavedDataStructureFeatureIndices = new(References.SavedDataStructureFeatureIndices);
    public static readonly DataFixTypes SavedDataWanderingTrader = new(References.SavedDataWanderingTrader);
    public static readonly DataFixTypes SavedDataWeather = new(References.SavedDataWeather);
    public static readonly DataFixTypes SavedDataWorldBorder = new(References.SavedDataWorldBorder);
    public static readonly DataFixTypes SavedDataWorldClocks = new(References.SavedDataWorldClocks);
    public static readonly DataFixTypes SavedDataWorldGenSettings = new(References.SavedDataWorldGenSettings);
    public static readonly DataFixTypes Advancements = new(References.Advancements);
    public static readonly DataFixTypes PoiChunk = new(References.PoiChunk);
    public static readonly DataFixTypes WorldGenSettings = new(References.WorldGenSettings);
    public static readonly DataFixTypes EntityChunk = new(References.EntityChunk);
    public static readonly DataFixTypes DebugProfile = new(References.DebugProfile);

    //TYPES_FOR_LEVEL_LIST: vanilla uses Set.of(LEVEL_SUMMARY.type)
    public static readonly IReadOnlySet<DSL.ITypeReference> TypesForLevelList = new HashSet<DSL.ITypeReference> { References.LightweightLevel };

    public DSL.ITypeReference Type { get; }

    private DataFixTypes(DSL.ITypeReference type) => Type = type;

    //currentVersion: the current world data version
    internal static int CurrentVersion() => SharedConstants.WorldDataVersion;

    //wrapCodec wraps a codec so it writes the current DataVersion and upgrades the payload before decoding, maps to vanilla wrapCodec
    public Codec<A> WrapCodec<A>(Codec<A> codec, DataFixer dataFixer, int defaultVersion)
        => new VersionedCodec<A>(this, codec, dataFixer, defaultVersion);

    //update applies updates to a Dynamic by version range, delegating to DataFixer.update
    public Dynamic<T> Update<T>(DataFixer fixerUpper, Dynamic<T> input, int fromVersion, int toVersion)
        => fixerUpper.Update(Type, input, fromVersion, toVersion);

    //updateToCurrentVersion upgrades to the current version
    public Dynamic<T> UpdateToCurrentVersion<T>(DataFixer fixerUpper, Dynamic<T> input, int dataVersion)
        => Update(fixerUpper, input, dataVersion, CurrentVersion());

    //update upgrades a CompoundTag by version range, maps to the vanilla CompoundTag overload
    public NacreCraft.Nbt.CompoundTag Update(DataFixer fixer, NacreCraft.Nbt.CompoundTag tag, int fromVersion, int toVersion)
        => (NacreCraft.Nbt.CompoundTag)Update(fixer, new Dynamic<NacreCraft.Nbt.Tag>(NacreCraft.Nbt.NbtOps.Instance, tag), fromVersion, toVersion).Value;

    //updateToCurrentVersion upgrades a CompoundTag to the current version
    public NacreCraft.Nbt.CompoundTag UpdateToCurrentVersion(DataFixer fixer, NacreCraft.Nbt.CompoundTag tag, int fromVersion)
        => Update(fixer, tag, fromVersion, CurrentVersion());
}

//VersionedCodec codec wrapper that round-trips the DataVersion field, maps to the anonymous codec of vanilla wrapCodec
//Decoding reads DataVersion, strips it, runs the fixer to the current version and then parses
//Encoding writes the payload and merges the current DataVersion into the map
internal sealed class VersionedCodec<A> : ScalarCodec<A>
{
    private const string DataVersionKey = "DataVersion";

    private readonly DataFixTypes _types;
    private readonly Codec<A> _codec;
    private readonly DataFixer _dataFixer;
    private readonly int _defaultVersion;

    public VersionedCodec(DataFixTypes types, Codec<A> codec, DataFixer dataFixer, int defaultVersion)
    {
        _types = types;
        _codec = codec;
        _dataFixer = dataFixer;
        _defaultVersion = defaultVersion;
    }

    public override DataResult<A> Parse<U>(DynamicOps<U> ops, U input)
    {
        var fromVersion = ReadVersion(ops, input);
        var withoutVersion = ops.Remove(input, DataVersionKey);
        var fixedData = _types.UpdateToCurrentVersion(_dataFixer, new Dynamic<U>(ops, withoutVersion), fromVersion);
        return _codec.Parse(fixedData.Ops, fixedData.Value);
    }

    public override DataResult<U> EncodeStart<U>(DynamicOps<U> ops, A value)
        => _codec.EncodeStart(ops, value)
            .FlatMap(data => ops.MergeToMap(data,
                ops.CreateString(DataVersionKey), ops.CreateInt(DataFixTypes.CurrentVersion())));

    //ReadVersion takes the DataVersion field, falling back to the codec's default when absent or not numeric
    private int ReadVersion<U>(DynamicOps<U> ops, U input)
    {
        var map = ops.GetMap(input);
        if (!map.Result().IsPresent) return _defaultVersion;
        var versionTag = map.GetOrThrow().Get(DataVersionKey);
        if (!versionTag.IsPresent) return _defaultVersion;
        var number = ops.GetNumberValue(versionTag.Get());
        return number.Result().IsPresent ? (int)number.GetOrThrow() : _defaultVersion;
    }
}
