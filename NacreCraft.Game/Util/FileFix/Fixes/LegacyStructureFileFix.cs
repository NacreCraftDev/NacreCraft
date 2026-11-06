using NacreCraft.Codec;
using NacreCraft.DataFixer;
using NacreCraft.DataFixer.Fixes;
using NacreCraft.DataFixer.Schemas;
using NacreCraft.Game.Util.WorldUpdate;
using NacreCraft.Nbt;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;

namespace NacreCraft.Game.Util.FileFix.Fixes;

//LegacyStructureFileFix moves the pre-1.13 structure index files into the chunk structure references, maps to vanilla net.minecraft.util.filefix.fixes.LegacyStructureFileFix
public sealed class LegacyStructureFileFix : FileFix
{
    public const int StructureRange = 8;

    public static readonly IReadOnlyList<string> OverworldLegacyStructures =
        new[] { "Monument", "Stronghold", "Mineshaft", "Temple", "Mansion" };

    public static readonly IReadOnlyDictionary<string, string> LegacyToCurrentMap =
        new Dictionary<string, string>
        {
            ["Iglu"] = "Igloo",
            ["TeDP"] = "Desert_Pyramid",
            ["TeJP"] = "Jungle_Pyramid",
            ["TeSH"] = "Swamp_Hut"
        };

    public static readonly IReadOnlyList<string> NetherLegacyStructures = new[] { "Fortress" };
    public static readonly IReadOnlyList<string> EndLegacyStructures = new[] { "EndCity" };

    public LegacyStructureFileFix(Schema schema, NacreCraft.DataFixer.DataFixer fixer)
        : base(schema, fixer)
    {
    }

    public override void MakeFixer()
    {
        AddFileContentFix(files =>
        {
            var overworldStructureData = OverworldLegacyStructures
                .Select(id => GetLegacyStructureData(files, id)).ToList();
            var overworldInfo = new RegionStorageInfo("overworld", LevelKeys.OVERWORLD, "chunk");
            var netherStructureData = NetherLegacyStructures
                .Select(id => GetLegacyStructureData(files, id)).ToList();
            var netherInfo = new RegionStorageInfo("the_nether", LevelKeys.NETHER, "chunk");
            var endStructureData = EndLegacyStructures
                .Select(id => GetLegacyStructureData(files, id)).ToList();
            var endInfo = new RegionStorageInfo("the_end", LevelKeys.END, "chunk");

            var levelDat = files.GetFileAccess(FileResourceTypes.LEVEL_DAT, FileRelations.ORIGIN.ForFile("level.dat"));
            var overworldChunks = files.GetFileAccess(FileResourceTypes.Chunk(DataFixTypes.Chunk, overworldInfo),
                FileRelations.OLD_OVERWORLD.Resolve(FileRelations.REGION));
            var netherChunks = files.GetFileAccess(FileResourceTypes.Chunk(DataFixTypes.Chunk, netherInfo),
                FileRelations.OLD_NETHER.Resolve(FileRelations.REGION));
            var endChunks = files.GetFileAccess(FileResourceTypes.Chunk(DataFixTypes.Chunk, endInfo),
                FileRelations.OLD_END.Resolve(FileRelations.REGION));

            return upgradeProgress =>
            {
                var levelData = levelDat.GetOnlyFile().Read();
                if (!levelData.IsPresent) return;

                upgradeProgress.SetType(UpgradeProgress.Type.LEGACY_STRUCTURES);
                ExtractAndStoreLegacyStructureData(levelData.Get(), new[]
                {
                    new DimensionFixEntry(LevelKeys.OVERWORLD, overworldStructureData, overworldChunks),
                    new DimensionFixEntry(LevelKeys.NETHER, netherStructureData, netherChunks),
                    new DimensionFixEntry(LevelKeys.END, endStructureData, endChunks)
                }, upgradeProgress);
            };
        });
    }

    private static FileAccess<SavedDataNbt> GetLegacyStructureData(FileAccessProvider files, string structureId)
        => files.GetFileAccess(
            FileResourceTypes.SavedData(References.SavedDataStructureFeatureIndices, CompressedNbt.MissingSeverity.MINOR),
            FileRelations.DATA.ForFile(structureId + ".dat"));

    private static void ExtractAndStoreLegacyStructureData(Dynamic<Tag> levelData,
        IReadOnlyList<DimensionFixEntry> dimensionFixEntries, UpgradeProgress upgradeProgress)
    {
        upgradeProgress.SetStatus(UpgradeProgress.Status.COUNTING);
        foreach (var dimension in dimensionFixEntries)
        {
            foreach (var structureFileAccess in dimension.StructureFileAccess)
            {
                var structureData = structureFileAccess.GetOnlyFile().Read();
                if (structureData.IsPresent) ExtractLegacyStructureData(structureData.Get(), dimension.Structures);
            }
            upgradeProgress.AddTotalFileFixOperations(dimension.Structures.Count);
        }

        upgradeProgress.SetStatus(UpgradeProgress.Status.UPGRADING);
        foreach (var dimension in dimensionFixEntries)
        {
            var chunkGeneratorType = "minecraft:noise";
            if (ReferenceEquals(dimension.DimensionKey, LevelKeys.OVERWORLD))
            {
                chunkGeneratorType = levelData.Get("generatorName").AsString("buffet") switch
                {
                    "flat" => "minecraft:flat",
                    "debug_all_block_states" => "minecraft:debug",
                    _ => "minecraft:noise"
                };
            }

            var generatorIdentifier = Identifier.TryParse(chunkGeneratorType);
            var dataFixContext = WorldUpgrader.GetChunkDataFixContextTag(dimension.DimensionKey, generatorIdentifier);
            StoreLegacyStructureDataToChunks(dimension.Structures, dimension.ChunkFileAccess.GetOnlyFile(),
                dataFixContext, upgradeProgress);
        }
    }

    private static void ExtractLegacyStructureData(Dynamic<Tag> structureData,
        Dictionary<long, LegacyStructureData> extractedDataContainer)
    {
        var features = structureData.Get("Features").Result();
        if (!features.IsPresent) return;

        foreach (var pair in features.Get().AsMapOpt())
        {
            var value = pair.Second;
            var pos = ChunkPos.Pack(value.Get("ChunkX").AsInt(0), value.Get("ChunkZ").AsInt(0));

            var children = value.Get("Children").AsStreamOpt().ToList();
            if (children.Count > 0)
            {
                var childId = children[0].Get("id").AsString().Result();
                if (childId.IsPresent && LegacyToCurrentMap.TryGetValue(childId.Get(), out var renamed))
                    value = value.Set("id", value.CreateString(renamed));
            }

            var id = value.Get("id").AsString().Result();
            if (!id.IsPresent) continue;

            var structureId = id.Get();
            GetOrCreate(extractedDataContainer, pos).AddStart(structureId, value);
            for (var neighborX = ChunkPos.GetX(pos) - StructureRange;
                 neighborX <= ChunkPos.GetX(pos) + StructureRange; neighborX++)
            {
                for (var neighborZ = ChunkPos.GetZ(pos) - StructureRange;
                     neighborZ <= ChunkPos.GetZ(pos) + StructureRange; neighborZ++)
                {
                    GetOrCreate(extractedDataContainer, ChunkPos.Pack(neighborX, neighborZ))
                        .AddIndex(structureId, pos);
                }
            }
        }
    }

    private static LegacyStructureData GetOrCreate(Dictionary<long, LegacyStructureData> structures, long pos)
    {
        if (!structures.TryGetValue(pos, out var data))
        {
            data = new LegacyStructureData();
            structures[pos] = data;
        }
        return data;
    }

    private static void StoreLegacyStructureDataToChunks(Dictionary<long, LegacyStructureData> structures,
        ChunkNbt chunksAccess, CompoundTag dataFixContext, UpgradeProgress upgradeProgress)
    {
        var entries = structures
            .OrderBy(entry => ChunkPos.Pack(ChunkPos.GetRegionX(entry.Key), ChunkPos.GetRegionZ(entry.Key)))
            .ToList();

        foreach (var entry in entries)
        {
            if (upgradeProgress.IsCanceled())
                throw new CanceledFileFixException();

            var legacyData = entry.Value;
            chunksAccess.UpdateChunk(ChunkPos.Unpack(entry.Key), dataFixContext, tag =>
            {
                var levelTag = tag.GetCompoundOrEmpty("Level");
                var structureTag = levelTag.GetCompoundOrEmpty("Structures");
                var startTag = structureTag.GetCompoundOrEmpty("Starts");
                var referencesTag = structureTag.GetCompoundOrEmpty("References");

                foreach (var (id, value) in legacyData.Starts)
                    startTag.Put(id, value.Value);
                foreach (var (id, indexes) in legacyData.Indexes)
                    referencesTag.PutLongArray(id, indexes.ToArray());

                structureTag.Put("Starts", startTag);
                structureTag.Put("References", referencesTag);
                levelTag.Put("Structures", structureTag);
                tag.Put("Level", levelTag);
                return tag;
            }).GetAwaiter().GetResult();
            upgradeProgress.IncrementFinishedOperationsBy(1);
        }
    }

    private sealed record DimensionFixEntry(ResourceKey<Level> DimensionKey,
        IReadOnlyList<FileAccess<SavedDataNbt>> StructureFileAccess, FileAccess<ChunkNbt> ChunkFileAccess)
    {
        public Dictionary<long, LegacyStructureData> Structures { get; } = new();
    }

    //LegacyStructureData the starts and chunk references collected for one chunk position
    private sealed class LegacyStructureData
    {
        public Dictionary<string, Dynamic<Tag>> Starts { get; } = new();

        public Dictionary<string, List<long>> Indexes { get; } = new();

        public void AddStart(string id, Dynamic<Tag> data) => Starts[id] = data;

        public void AddIndex(string id, long sourcePos)
        {
            if (!Indexes.TryGetValue(id, out var indexes))
            {
                indexes = new List<long>();
                Indexes[id] = indexes;
            }
            indexes.Add(sourcePos);
        }
    }
}
