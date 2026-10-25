using NacreCraft.Nbt;
using NacreCraft.Primitives;
using NacreCraft.Registry;

namespace NacreCraft.Game.World.Level.Block;

//BlockEntityTypes block entity type registration, maps to vanilla net.minecraft.world.level.block.entity.BlockEntityTypes
//Registered into the BuiltInRegistries.BLOCK_ENTITY_TYPE registry
//RawId taken from the static field declaration order of vanilla BlockEntityTypes (counting from furnace); the client dispatches by index so it cannot be renumbered
//Currently only the comparator has a block entity here, more are added as containers and other blocks land
public static class BlockEntityTypes
{
    //FurnaceType furnace type, index 0 for furnace in vanilla BlockEntityTypes
    public sealed class FurnaceType : BlockEntityType
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("furnace");
        public override int RawId => 0;
        public override BlockEntity Create(BlockPos pos) => new FurnaceBlockEntity(pos);
    }

    //DispenserType dispenser type, index 5 for dispenser in vanilla BlockEntityTypes
    public sealed class DispenserType : BlockEntityType
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("dispenser");
        public override int RawId => 5;
        public override BlockEntity Create(BlockPos pos) => new DispenserBlockEntity(pos);
    }

    //DropperType dropper type, index 6 for dropper in vanilla BlockEntityTypes
    public sealed class DropperType : BlockEntityType
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("dropper");
        public override int RawId => 6;
        public override BlockEntity Create(BlockPos pos) => new DropperBlockEntity(pos);
    }

    //ComparatorType comparator type, index 19 for comparator in vanilla BlockEntityTypes
    public sealed class ComparatorType : BlockEntityType
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("comparator");
        public override int RawId => 19;
        public override BlockEntity Create(BlockPos pos) => new ComparatorBlockEntity(pos);
    }

    //CommandBlockType command block type, index 23 in vanilla BlockEntityTypes declaration order
    public sealed class CommandBlockType : BlockEntityType
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("command_block");
        public override int RawId => 23;
        public override BlockEntity Create(BlockPos pos) => new CommandBlockEntity(pos);
    }

    //StructureBlockType structure block type, index 21 in vanilla BlockEntityTypes declaration order
    public sealed class StructureBlockType : BlockEntityType
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("structure_block");
        public override int RawId => 21;
        public override BlockEntity Create(BlockPos pos) => new StructureBlockEntity(pos, default);
    }

    //JigsawType jigsaw type, index 31 in vanilla BlockEntityTypes declaration order
    public sealed class JigsawType : BlockEntityType
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("jigsaw");
        public override int RawId => 31;
        public override BlockEntity Create(BlockPos pos) => new JigsawBlockEntity(pos);
    }

    //PistonMovingType moving piston type, index 11 for piston in vanilla BlockEntityTypes
    public sealed class PistonMovingType : BlockEntityType
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("piston");
        public override int RawId => 11;
        public override BlockEntity Create(BlockPos pos) => new Piston.PistonMovingBlockEntity(pos);
    }

    //ChestType chest type, index 1 for chest in vanilla BlockEntityTypes
    public sealed class ChestType : BlockEntityType
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("chest");
        public override int RawId => 1;
        public override BlockEntity Create(BlockPos pos) => new ChestBlockEntity(pos);
    }

    //BarrelType barrel type, index 26 for barrel in vanilla BlockEntityTypes
    public sealed class BarrelType : BlockEntityType
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("barrel");
        public override int RawId => 26;
        public override BlockEntity Create(BlockPos pos) => new BarrelBlockEntity(pos);
    }

    //SmokerType smoker type, index 27 for smoker in vanilla BlockEntityTypes
    public sealed class SmokerType : BlockEntityType
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("smoker");
        public override int RawId => 27;
        public override BlockEntity Create(BlockPos pos) => new SmokerBlockEntity(pos);
    }

    //BlastFurnaceType blast furnace type, index 28 for blast_furnace in vanilla BlockEntityTypes
    public sealed class BlastFurnaceType : BlockEntityType
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("blast_furnace");
        public override int RawId => 28;
        public override BlockEntity Create(BlockPos pos) => new BlastFurnaceBlockEntity(pos);
    }

    //CampfireType campfire type, index 32 for campfire in vanilla BlockEntityTypes
    public sealed class CampfireType : BlockEntityType
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("campfire");
        public override int RawId => 32;
        public override BlockEntity Create(BlockPos pos) => new CampfireBlockEntity(pos);
    }

    public static readonly FurnaceType FURNACE = new();

    public static readonly ComparatorType COMPARATOR = new();

    public static readonly CommandBlockType COMMAND_BLOCK = new();

    public static readonly StructureBlockType STRUCTURE_BLOCK = new();

    public static readonly JigsawType JIGSAW = new();

    public static readonly ChestType CHEST = new();

    public static readonly BarrelType BARREL = new();

    public static readonly SmokerType SMOKER = new();

    public static readonly BlastFurnaceType BLAST_FURNACE = new();

    public static readonly CampfireType CAMPFIRE = new();

    public static readonly PistonMovingType PISTON = new();

    public static readonly DispenserType DISPENSER = new();

    public static readonly DropperType DROPPER = new();

    //SculkSensorType sculk sensor block entity type, maps to vanilla BlockEntityTypes.SCULK_SENSOR
    public sealed class SculkSensorType : BlockEntityType
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("sculk_sensor");
        public override int RawId => 34;
        public override BlockEntity Create(BlockPos pos) => new SculkSensorBlockEntity(pos);
    }

    public static readonly SculkSensorType SCULK_SENSOR = new();

    //SculkCatalystType sculk catalyst block entity type, maps to vanilla BlockEntityTypes.SCULK_CATALYST
    public sealed class SculkCatalystType : BlockEntityType
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("sculk_catalyst");
        public override int RawId => 36;
        public override BlockEntity Create(BlockPos pos) => new SculkCatalystBlockEntity(pos);
    }

    //SculkShriekerType sculk shrieker block entity type, maps to vanilla BlockEntityTypes.SCULK_SHRIEKER
    public sealed class SculkShriekerType : BlockEntityType
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("sculk_shrieker");
        public override int RawId => 37;
        public override BlockEntity Create(BlockPos pos) => new SculkShriekerBlockEntity(pos);
    }

    public static readonly SculkCatalystType SCULK_CATALYST = new();

    public static readonly SculkShriekerType SCULK_SHRIEKER = new();

    //ByRawId reverse map from network index, used by sync packet decoding and client dispatch
    private static readonly Dictionary<int, BlockEntityType> ByRawId = new();

    //KeyToType reverse map from registry key, used to restore the save id field
    private static readonly Dictionary<string, BlockEntityType> KeyToType = new();

    //Bootstrap registers all built-in block entity types, must be called before the registry is frozen
    public static void Bootstrap()
    {
        Register(FURNACE);
        Register(COMPARATOR);
        Register(COMMAND_BLOCK);
        Register(STRUCTURE_BLOCK);
        Register(JIGSAW);
        Register(CHEST);
        Register(BARREL);
        Register(SMOKER);
        Register(BLAST_FURNACE);
        Register(CAMPFIRE);
        Register(PISTON);
        Register(DISPENSER);
        Register(DROPPER);
        Register(SCULK_SENSOR);
        Register(SCULK_CATALYST);
        Register(SCULK_SHRIEKER);
    }

    //ById returns a type by network index, null when unregistered
    public static BlockEntityType? ById(int rawId)
        => ByRawId.TryGetValue(rawId, out var type) ? type : null;

    //ByKey returns a type by registry key, null when unregistered
    public static BlockEntityType? ByKey(string key)
        => KeyToType.TryGetValue(key, out var type) ? type : null;

    //Load restores a block entity from a save tag
    //Returns null when the id is missing or unregistered, the caller decides whether to skip or error
    public static BlockEntity? Load(CompoundTag tag)
    {
        if (tag.GetString("id")?.Value is not { } key) return null;
        if (!KeyToType.TryGetValue(key, out var type)) return null;
        var pos = new BlockPos(tag.GetIntOr("x", 0), tag.GetIntOr("y", 0), tag.GetIntOr("z", 0));
        var entity = type.Create(pos);
        entity.LoadCustomOnly(tag);
        return entity;
    }

    //Register writes the registry and records both reverse maps
    private static void Register(BlockEntityType type)
    {
        Registry<BlockEntityType<object>>.Register(BuiltInRegistries.BLOCK_ENTITY_TYPE, type.Id, type);
        ByRawId[type.RawId] = type;
        KeyToType[type.Id.ToString()] = type;
    }
}
