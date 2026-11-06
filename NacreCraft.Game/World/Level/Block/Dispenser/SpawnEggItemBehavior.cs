using NacreCraft.Game.World.Entity;
using NacreCraft.Game.World.Items;
using NacreCraft.Primitives;
using NacreCraft.Registry;
//Both Primitives and Registry.Enums define Direction; this behavior only needs the primitive one
using Direction = NacreCraft.Primitives.Direction;

namespace NacreCraft.Game.World.Level.Block.Dispenser;

//SpawnEggItemBehavior spawns the mob a spawn egg stands for, maps to vanilla net.minecraft.core.dispenser.SpawnEggItemBehavior
//NC has no SpawnEggItem class yet; the item registry installs TypeResolver once spawn eggs land, until then nothing is spawned and the egg is left alone
public class SpawnEggItemBehavior : DefaultDispenseItemBehavior
{
    public static readonly SpawnEggItemBehavior Instance = new();

    //TypeResolver maps a spawn egg stack to its entity type, injected by the item registry
    public static Func<ItemStack, EntityType<object>?>? TypeResolver { get; set; }

    protected override ItemStack Execute(BlockSource source, ItemStack dispensed)
    {
        var direction = FacingOf(source.State).ToPrimitive();
        var type = TypeResolver?.Invoke(dispensed);
        if (type is null) return dispensed;
        var spawned = type.Create(source.Level);
        if (spawned is null) return dispensed;
        var pos = source.Pos.Offset(direction);
        spawned.Pos = new Vec3(pos.X + 0.5, pos.Y, pos.Z + 0.5);
        if (!source.Level.AddEntity(spawned)) return dispensed;
        dispensed.Shrink(1);
        source.Level.GameEvent(null, GameEvent.EntityPlace, source.Pos);
        return dispensed;
    }
}
