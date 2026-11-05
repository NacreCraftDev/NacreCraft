namespace NacreCraft.Game.World.Inventory;

//SlotRange named slot index range, maps to vanilla net.minecraft.world.inventory.SlotRange
//The name is the serialized form used by the SlotsPredicate codec, the indices come from SlotRanges
public sealed record SlotRange(string Name, IReadOnlyList<int> Slots)
{
    public int Size => Slots.Count;

    public static SlotRange Of(string name, IReadOnlyList<int> slots) => new(name, slots);

    public override string ToString() => Name;
}
