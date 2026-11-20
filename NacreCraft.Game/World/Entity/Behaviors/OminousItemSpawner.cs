using NacreCraft.Game.World.Items;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Entity;

//OminousItemSpawner a delayed item spawner, maps to vanilla net.minecraft.world.entity.OminousItemSpawner
//After a random delay the stored item is dropped as an item entity; NC has no particle trail or about-to-spawn sound yet
public sealed class OminousItemSpawner : NacreCraft.Registry.Entity
{
    private long _spawnItemAfterTicks = 60;

    public OminousItemSpawner()
    {
        Item = ItemStack.Empty;
    }

    public override Identifier Id => Identifier.WithDefaultNamespace("ominous_item_spawner");
    public override bool CanBeHitByProjectile => false;
    public override bool IsPickable => false;

    public ItemStack Item { get; set; }

    //Create builds a spawner for an item with the vanilla 60..120 tick delay, maps to vanilla create
    public static OminousItemSpawner Create(ItemStack item, int delay)
    {
        var spawner = new OminousItemSpawner { Item = item, _spawnItemAfterTicks = delay };
        return spawner;
    }

    public override void Tick()
    {
        TickBase();
        if (Level is not ServerLevel level) return;
        if (TickCount < _spawnItemAfterTicks) return;
        if (!Item.IsEmpty())
        {
            var spawned = new ItemEntity(EntityTypes.ITEM, Pos.X, Pos.Y, Pos.Z, Item);
            level.AddEntity(spawned);
        }
        Discard();
    }
}
