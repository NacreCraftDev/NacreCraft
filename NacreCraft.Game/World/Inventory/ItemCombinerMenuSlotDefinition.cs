using NacreCraft.Game.World.Items;

namespace NacreCraft.Game.World.Inventory;

//ItemCombinerMenuSlotDefinition slot layout of an anvil/smithing style menu, maps to vanilla net.minecraft.world.inventory.ItemCombinerMenuSlotDefinition
//Input slots are numbered continuously from zero, the result slot always follows the last input
public sealed class ItemCombinerMenuSlotDefinition
{
    private readonly IReadOnlyList<SlotDefinition> _slots;

    private ItemCombinerMenuSlotDefinition(IReadOnlyList<SlotDefinition> inputSlots, SlotDefinition resultSlot)
    {
        if (inputSlots.Count == 0 || resultSlot.Equals(SlotDefinition.Empty))
            throw new ArgumentException("Need to define both inputSlots and resultSlot");
        _slots = inputSlots;
        ResultSlot = resultSlot;
    }

    public SlotDefinition ResultSlot { get; }

    public IReadOnlyList<SlotDefinition> Slots => _slots;

    public int NumOfInputSlots => _slots.Count;

    public int ResultSlotIndex => NumOfInputSlots;

    public SlotDefinition GetSlot(int index) => _slots[index];

    public static Builder Create() => new();

    //SlotDefinition one slot's index, GUI position and placement rule, maps to vanilla SlotDefinition
    public sealed record SlotDefinition(int SlotIndex, int X, int Y, Func<ItemStack, bool> MayPlace)
    {
        internal static readonly SlotDefinition Empty = new(0, 0, 0, _ => true);
    }

    public sealed class Builder
    {
        private readonly List<SlotDefinition> _inputSlots = new();
        private SlotDefinition _resultSlot = SlotDefinition.Empty;

        public Builder WithSlot(int slotIndex, int x, int y, Func<ItemStack, bool> mayPlace)
        {
            _inputSlots.Add(new SlotDefinition(slotIndex, x, y, mayPlace));
            return this;
        }

        public Builder WithResultSlot(int slotIndex, int x, int y)
        {
            _resultSlot = new SlotDefinition(slotIndex, x, y, _ => false);
            return this;
        }

        public ItemCombinerMenuSlotDefinition Build()
        {
            for (var i = 0; i < _inputSlots.Count; i++)
            {
                if (_inputSlots[i].SlotIndex != i)
                    throw new ArgumentException("Expected input slots to have continuous indexes");
            }
            if (_resultSlot.SlotIndex != _inputSlots.Count)
                throw new ArgumentException("Expected result slot index to follow last input slot");
            return new ItemCombinerMenuSlotDefinition(_inputSlots, _resultSlot);
        }
    }
}
