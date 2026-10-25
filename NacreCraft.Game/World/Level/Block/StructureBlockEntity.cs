using NacreCraft.Game.Bootstrap;
using NacreCraft.Game.World.Level.LevelGen.Structure;
using NacreCraft.Nbt;
using NacreCraft.Primitives;
using NacreCraft.Registry.Enums;
using NacreCraft.Registry.State;
using NacreCraft.Storage;
using NacreCraft.Storage.Updates;
using NacreCraft.Util.Random;

namespace NacreCraft.Game.World.Level.Block;

//StructureBlockEntity structure block block entity, maps to vanilla net.minecraft.world.level.block.entity.StructureBlockEntity
//Holds the selection, the template name and the placement options; saving and loading go through the shared structure template manager
public sealed class StructureBlockEntity : BlockEntity
{
    //UpdateType what the client asked to do after writing the fields, maps to vanilla StructureBlockEntity.UpdateType
    public enum UpdateType
    {
        UpdateData,
        SaveArea,
        LoadArea,
        ScanArea,
    }

    //MaxOffsetPerAxis offset clamp per axis, maps to vanilla MAX_OFFSET_PER_AXIS
    public const int MaxOffsetPerAxis = 48;

    //MaxSizePerAxis size clamp per axis, maps to vanilla MAX_SIZE_PER_AXIS
    public const int MaxSizePerAxis = 48;

    //DetectRange the save-mode size detection range, maps to the hard coded 80 in vanilla detectSize
    private const int DetectRange = 80;

    private static readonly BlockPos DefaultPos = new(0, 1, 0);

    private Identifier? _structureName;

    public StructureBlockEntity(BlockPos pos, BlockState state)
        : base(BlockEntityTypes.STRUCTURE_BLOCK, pos)
        => Mode = ReadMode(state);

    //Name the template name; an empty string means unset, maps to vanilla getStructureName
    public string Name => _structureName?.ToString() ?? "";

    public bool HasStructureName => _structureName is not null;

    public string Author { get; private set; } = "";

    public string MetaData { get; private set; } = "";

    //Offset the selection origin relative to this block, maps to vanilla structurePos
    public BlockPos Offset { get; private set; } = DefaultPos;

    //Size the selection size, maps to vanilla structureSize
    public Vec3i Size { get; private set; } = Vec3i.Zero;

    public Mirror Mirror { get; private set; } = Mirror.None;

    public Rotation Rotation { get; private set; } = Rotation.None;

    public StructureMode Mode { get; private set; } = StructureMode.load;

    public bool IgnoreEntities { get; private set; } = true;

    public bool Strict { get; private set; }

    public bool Powered { get; private set; }

    public bool ShowAir { get; private set; }

    public bool ShowBoundingBox { get; private set; } = true;

    public float Integrity { get; private set; } = 1.0f;

    public long Seed { get; private set; }

    public void SetStructureName(string name)
        => _structureName = string.IsNullOrWhiteSpace(name) ? null : Identifier.TryParse(name);

    public void SetStructureName(Identifier? name) => _structureName = name;

    public void SetAuthor(string author) => Author = author;

    public void SetOffset(BlockPos offset) => Offset = offset;

    public void SetSize(Vec3i size) => Size = size;

    public void SetMirror(Mirror mirror) => Mirror = mirror;

    public void SetRotation(Rotation rotation) => Rotation = rotation;

    public void SetMetaData(string metaData) => MetaData = metaData;

    public void SetIgnoreEntities(bool value) => IgnoreEntities = value;

    public void SetStrict(bool value) => Strict = value;

    public void SetShowAir(bool value) => ShowAir = value;

    public void SetShowBoundingBox(bool value) => ShowBoundingBox = value;

    public void SetIntegrity(float value) => Integrity = Math.Clamp(value, 0f, 1f);

    public void SetSeed(long value) => Seed = value;

    public void SetPowered(bool value) => Powered = value;

    //SetMode writes the mode and mirrors it into the block state, maps to vanilla setMode
    public void SetMode(StructureMode mode)
    {
        Mode = mode;
        UpdateBlockState();
    }

    //UpdateBlockState writes the current mode back into the block state, maps to vanilla updateBlockState
    private void UpdateBlockState()
    {
        if (Level is null) return;
        if (Level.GetBlockState(Pos) is not { } state) return;
        if (state.Owner is not Blocks.StructureBlock) return;
        foreach (var value in state.GetValues())
        {
            if (value.Property.Name != Blocks.StructureBlock.ModePropertyName) continue;
            Level.SetBlock(Pos, state.SetValue(value.Property, Mode), BlockUpdateFlags.Clients);
            return;
        }
    }

    //ReadMode reads the mode property off the block state, downgrading to data when absent
    private static StructureMode ReadMode(BlockState state)
    {
        foreach (var value in state.GetValues())
        {
            if (value.Property.Name != Blocks.StructureBlock.ModePropertyName) continue;
            if (value.Value is StructureMode mode) return mode;
        }
        return StructureMode.data;
    }

    //DetectSize scans for matching corner blocks and derives offset and size, maps to vanilla detectSize
    public bool DetectSize()
    {
        if (Mode != StructureMode.save || Level is null) return false;
        var minX = Pos.X - DetectRange;
        var maxX = Pos.X + DetectRange;
        var minY = Level.MinBuildHeight;
        var maxY = Level.MaxBuildHeight - 1;
        var minZ = Pos.Z - DetectRange;
        var maxZ = Pos.Z + DetectRange;

        bool found = false;
        var boxMinX = int.MaxValue;
        var boxMinY = int.MaxValue;
        var boxMinZ = int.MaxValue;
        var boxMaxX = int.MinValue;
        var boxMaxY = int.MinValue;
        var boxMaxZ = int.MinValue;
        var corners = 0;
        for (var x = minX; x <= maxX; x++)
        for (var z = minZ; z <= maxZ; z++)
        for (var y = minY; y <= maxY; y++)
        {
            var cornerPos = new BlockPos(x, y, z);
            if (Level.GetBlockState(cornerPos) is not { } state) continue;
            if (state.Owner is not Blocks.StructureBlock) continue;
            if (Level.GetBlockEntity<StructureBlockEntity>(cornerPos) is not { } corner) continue;
            if (corner.Mode != StructureMode.corner || corner.Name != Name) continue;
            corners++;
            boxMinX = Math.Min(boxMinX, x);
            boxMinY = Math.Min(boxMinY, y);
            boxMinZ = Math.Min(boxMinZ, z);
            boxMaxX = Math.Max(boxMaxX, x);
            boxMaxY = Math.Max(boxMaxY, y);
            boxMaxZ = Math.Max(boxMaxZ, z);
            found = true;
        }
        //A single corner pairs with the block itself, maps to vanilla encapsulating the source position when only one corner exists
        if (corners == 1)
        {
            boxMinX = Math.Min(boxMinX, Pos.X);
            boxMinY = Math.Min(boxMinY, Pos.Y);
            boxMinZ = Math.Min(boxMinZ, Pos.Z);
            boxMaxX = Math.Max(boxMaxX, Pos.X);
            boxMaxY = Math.Max(boxMaxY, Pos.Y);
            boxMaxZ = Math.Max(boxMaxZ, Pos.Z);
        }
        if (!found) return false;

        var deltaX = boxMaxX - boxMinX;
        var deltaY = boxMaxY - boxMinY;
        var deltaZ = boxMaxZ - boxMinZ;
        if (deltaX <= 1 || deltaY <= 1 || deltaZ <= 1) return false;
        Offset = new BlockPos(boxMinX - Pos.X + 1, boxMinY - Pos.Y + 1, boxMinZ - Pos.Z + 1);
        Size = new Vec3i(deltaX - 1, deltaY - 1, deltaZ - 1);
        Level.BlockEntityChanged(Pos);
        return true;
    }

    //SaveStructure saves into the in-memory template and on request writes it to disk, maps to vanilla saveStructure
    public bool SaveStructure(bool saveToDisk)
    {
        if (Mode != StructureMode.save || Level is not { } level || _structureName is not { } name) return false;
        if (GameBootstrap.StructureTemplates is not { } manager) return false;
        if (manager.GetOrCreate(name) is not { } template) return false;

        var origin = Pos.Offset(Offset.X, Offset.Y, Offset.Z);
        var ignoreBlocks = new List<NacreCraft.Registry.Block>(1);
        if (BuiltInRegistries.BLOCK.GetValue(Identifier.WithDefaultNamespace("structure_void")) is { } structureVoid)
            ignoreBlocks.Add(structureVoid);
        template.FillFromWorld(level, origin, Size, !IgnoreEntities, ignoreBlocks);
        template.SetAuthor(Author);
        return !saveToDisk || manager.Save(name);
    }

    //PlaceStructureIfSameSize places only when the template size equals the configured size; otherwise it refreshes the size, maps to vanilla
    public bool PlaceStructureIfSameSize(ServerLevel level)
    {
        if (Mode != StructureMode.load || _structureName is not { } name) return false;
        if (GameBootstrap.StructureTemplates?.GetOrLoad(name) is not { } template) return false;
        if (template.Size == Size)
        {
            PlaceStructure(level, template);
            return true;
        }
        LoadStructureInfo(template);
        return false;
    }

    //PlaceStructure loads the named template and places it, maps to vanilla placeStructure
    public void PlaceStructure(ServerLevel level)
    {
        if (_structureName is not { } name) return;
        if (GameBootstrap.StructureTemplates?.GetOrLoad(name) is not { } template) return;
        PlaceStructure(level, template);
    }

    //LoadStructureInfo copies the template author and size onto this block, maps to vanilla loadStructureInfo
    public void LoadStructureInfo(ServerLevel level)
    {
        if (_structureName is not { } name) return;
        if (GameBootstrap.StructureTemplates?.GetOrLoad(name) is not { } template) return;
        LoadStructureInfo(template);
    }

    private void LoadStructureInfo(StructureTemplate template)
    {
        Author = template.Author;
        Size = template.Size;
        Level?.BlockEntityChanged(Pos);
    }

    //PlaceStructure places the template with the configured transform and integrity, maps to vanilla placeStructure
    private void PlaceStructure(ServerLevel level, StructureTemplate template)
    {
        LoadStructureInfo(template);
        var random = CreateRandom(Seed);
        var settings = new StructurePlaceSettings()
            .SetMirror(Mirror)
            .SetRotation(Rotation)
            .SetIgnoreEntities(IgnoreEntities)
            .SetKnownShape(Strict);
        if (Integrity < 1.0f)
        {
            settings.ClearProcessors()
                .AddProcessor(new BlockRotProcessor(Math.Clamp(Integrity, 0f, 1f)))
                .SetRandom(random);
        }
        var origin = Pos.Offset(Offset.X, Offset.Y, Offset.Z);
        //Clients see the result, but neighbors, drops and onPlace stay off, matching the vanilla 818 flags
        const int updateFlags = BlockUpdateFlags.Clients | BlockUpdateFlags.KnownShape | BlockUpdateFlags.SuppressDrops;
        template.PlaceInWorld(level, origin, origin, settings, random, updateFlags);
    }

    //IsStructureLoadable whether the named template exists, maps to vanilla isStructureLoadable
    public bool IsStructureLoadable()
    {
        if (Mode != StructureMode.load || Level is null || _structureName is not { } name) return false;
        return GameBootstrap.StructureTemplates?.GetOrLoad(name) is not null;
    }

    //UnloadStructure drops the cached template of this name, maps to vanilla unloadStructure
    public void UnloadStructure()
    {
        if (_structureName is not { } name) return;
        GameBootstrap.StructureTemplates?.Remove(name);
    }

    //CreateRandom seeds from system time when the seed is zero, maps to vanilla createRandom
    public static RandomSource CreateRandom(long seed)
        => RandomSource.Create(seed == 0L ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() : seed);

    public override void SaveAdditional(CompoundTag tag)
    {
        tag.PutString("name", Name);
        tag.PutString("author", Author);
        tag.PutString("metadata", MetaData);
        tag.PutInt("posX", Offset.X);
        tag.PutInt("posY", Offset.Y);
        tag.PutInt("posZ", Offset.Z);
        tag.PutInt("sizeX", Size.X);
        tag.PutInt("sizeY", Size.Y);
        tag.PutInt("sizeZ", Size.Z);
        tag.PutString("rotation", Rotation.Name());
        tag.PutString("mirror", Mirror.Name());
        tag.PutString("mode", Mode.ToString());
        tag.PutBoolean("ignoreEntities", IgnoreEntities);
        tag.PutBoolean("strict", Strict);
        tag.PutBoolean("powered", Powered);
        tag.PutBoolean("showair", ShowAir);
        tag.PutBoolean("showboundingbox", ShowBoundingBox);
        tag.PutFloat("integrity", Integrity);
        tag.PutLong("seed", Seed);
    }

    public override void LoadAdditional(CompoundTag tag)
    {
        SetStructureName(tag.GetString("name")?.Value ?? "");
        Author = tag.GetString("author")?.Value ?? "";
        MetaData = tag.GetString("metadata")?.Value ?? "";
        Offset = new BlockPos(
            Math.Clamp(tag.GetIntOr("posX", DefaultPos.X), -MaxOffsetPerAxis, MaxOffsetPerAxis),
            Math.Clamp(tag.GetIntOr("posY", DefaultPos.Y), -MaxOffsetPerAxis, MaxOffsetPerAxis),
            Math.Clamp(tag.GetIntOr("posZ", DefaultPos.Z), -MaxOffsetPerAxis, MaxOffsetPerAxis));
        Size = new Vec3i(
            Math.Clamp(tag.GetIntOr("sizeX", 0), 0, MaxSizePerAxis),
            Math.Clamp(tag.GetIntOr("sizeY", 0), 0, MaxSizePerAxis),
            Math.Clamp(tag.GetIntOr("sizeZ", 0), 0, MaxSizePerAxis));
        Rotation = StructureTransforms.TryParseRotation(tag.GetString("rotation")?.Value ?? "none") ?? Rotation.None;
        Mirror = StructureTransforms.TryParseMirror(tag.GetString("mirror")?.Value ?? "none") ?? Mirror.None;
        Mode = Enum.TryParse<StructureMode>(tag.GetString("mode")?.Value, true, out var mode) ? mode : StructureMode.data;
        IgnoreEntities = tag.GetBooleanOr("ignoreEntities", true);
        Strict = tag.GetBooleanOr("strict", false);
        Powered = tag.GetBooleanOr("powered", false);
        ShowAir = tag.GetBooleanOr("showair", false);
        ShowBoundingBox = tag.GetBooleanOr("showboundingbox", true);
        Integrity = tag.GetFloat("integrity")?.Value ?? 1.0f;
        Seed = tag.GetLongOr("seed", 0L);
        UpdateBlockState();
    }
}
