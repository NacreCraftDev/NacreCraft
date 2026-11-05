using NacreCraft.Game.Server;
using NacreCraft.Game.World.Items;
using NacreCraft.Registry.State;

namespace NacreCraft.Game.World.Inventory;

//SmithingMenu smithing table menu, maps to vanilla net.minecraft.world.inventory.SmithingMenu
//Slot layout: 0 template, 1 base, 2 addition, 3 result, 4-30 main inventory, 31-39 hotbar
//Smithing recipes are not implemented in the recipe manager, so the result stays empty
public sealed class SmithingMenu : ItemCombinerMenu
{
    public const int TemplateSlot = 0;
    public const int BaseSlot = 1;
    public const int AdditionalSlot = 2;
    public const int SmithingResultSlot = 3;

    private readonly DataSlot _hasRecipeError = DataSlot.Standalone();

    public SmithingMenu(int containerId, ServerPlayer player)
        : this(containerId, player, ContainerLevelAccess.Null)
    {
    }

    public SmithingMenu(int containerId, ServerPlayer player, ContainerLevelAccess access)
        : base(MenuTypes.SMITHING, containerId, player, access, CreateInputSlotDefinitions())
    {
        AddDataSlot(_hasRecipeError);
    }

    public bool HasRecipeError => _hasRecipeError.Get(0) > 0;

    private static ItemCombinerMenuSlotDefinition CreateInputSlotDefinitions()
        => ItemCombinerMenuSlotDefinition.Create()
            .WithSlot(0, 8, 48, _ => true)
            .WithSlot(1, 26, 48, _ => true)
            .WithSlot(2, 44, 48, _ => true)
            .WithResultSlot(3, 98, 48)
            .Build();

    //IsValidBlock smithing table only
    protected override bool IsValidBlock(BlockState state) => state.Owner.Id.Path == "smithing_table";

    protected override void OnTake(ServerPlayer player, ItemStack carried)
    {
        ShrinkStackInSlot(TemplateSlot);
        ShrinkStackInSlot(BaseSlot);
        ShrinkStackInSlot(AdditionalSlot);
    }

    //CreateResult looks up a smithing recipe, maps to vanilla createResult; no smithing recipe table is ported
    public override void CreateResult()
    {
        ResultSlots.SetItem(0, ItemStack.Empty);
        var hasRecipeError = GetSlot(TemplateSlot).HasItem()
            && GetSlot(BaseSlot).HasItem()
            && GetSlot(AdditionalSlot).HasItem()
            && !GetSlot(ResultSlotIndex).HasItem();
        _hasRecipeError.Set(0, hasRecipeError ? 1 : 0);
    }

    private void ShrinkStackInSlot(int slotIndex)
    {
        var stack = InputSlots.GetItem(slotIndex);
        if (stack.IsEmpty()) return;
        stack.SetCount(stack.GetCount() - 1);
        InputSlots.SetItem(slotIndex, stack);
    }
}
