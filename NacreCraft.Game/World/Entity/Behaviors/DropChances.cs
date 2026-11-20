namespace NacreCraft.Game.World.Entity;

//DropChances the per-slot equipment drop chance of a mob, maps to vanilla net.minecraft.world.entity.DropChances
//A chance above 1 marks the item as preserved (the vanilla PRESERVE_ITEM_DROP_CHANCE value); missing slots use the default
public sealed class DropChances
{
    public const float DefaultEquipmentDropChance = 0.085f;
    public const float PreserveItemDropChanceThreshold = 1f;
    public const int PreserveItemDropChance = 2;

    public static readonly DropChances Default = new(Enum.GetValues<EquipmentSlot>()
        .ToDictionary(slot => slot, _ => DefaultEquipmentDropChance));

    public DropChances(IReadOnlyDictionary<EquipmentSlot, float> byEquipment) => ByEquipment = byEquipment;

    public IReadOnlyDictionary<EquipmentSlot, float> ByEquipment { get; }

    public float ByEquipmentChance(EquipmentSlot slot)
        => ByEquipment.TryGetValue(slot, out var chance) ? chance : DefaultEquipmentDropChance;

    public bool IsPreserved(EquipmentSlot slot) => ByEquipmentChance(slot) > PreserveItemDropChanceThreshold;

    public DropChances WithGuaranteedDrop(EquipmentSlot slot) => WithEquipmentChance(slot, PreserveItemDropChance);

    public DropChances WithEquipmentChance(EquipmentSlot slot, float chance)
    {
        if (chance < 0f)
            throw new ArgumentException($"Tried to set invalid equipment chance {chance} for {slot}");
        if (ByEquipmentChance(slot) == chance) return this;
        var copy = Enum.GetValues<EquipmentSlot>()
            .ToDictionary(other => other, other => other == slot ? chance : ByEquipmentChance(other));
        return new DropChances(copy);
    }
}
