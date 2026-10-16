using NacreCraft.Game.World.Items.Component;
using NacreCraft.Game.World.Level.Block;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Items;

//BlockItem block item, maps to vanilla net.minecraft.world.item.BlockItem
//Holds the associated block; use_item_on places the default state of PlacedBlock
public sealed class BlockItem : Item
{
    private readonly string _name;

    //PlacedBlock the block placed on placement
    public NacreCraft.Registry.Block PlacedBlock { get; }

    //WallBlock wall variant placed when clicking a horizontal side, maps to the wall variant of vanilla StandingAndWallBlockItem
    public NacreCraft.Registry.Block? WallBlock { get; init; }

    public BlockItem(string name, NacreCraft.Registry.Block placedBlock)
    {
        _name = name;
        PlacedBlock = placedBlock;
    }

    public override Identifier Id => Identifier.WithDefaultNamespace(_name);

    //UpdateCustomBlockEntityTag flushes the block entity data carried by the item into the just-placed block entity, maps to the vanilla static method of the same name
    //None of the block entity types registered here need an op restriction, so the vanilla onlyOpCanSetNbt check is skipped
    public static bool UpdateCustomBlockEntityTag(PersistentServerLevel level, BlockPos pos, ItemStack itemStack)
    {
        if (itemStack.Get(DataComponents.BLOCK_ENTITY_DATA)
            is not TypedEntityData<Holder<NacreCraft.Registry.BlockEntityType<object>>> customData) return false;
        if (level.GetBlockEntity<BlockEntity>(pos) is not { } blockEntity) return false;
        //The data type on the item must match the placed block's block entity type, otherwise the data must not be applied
        if (!Equals(blockEntity.Type, customData.Type.Value)) return false;
        return customData.LoadInto(blockEntity);
    }
}
