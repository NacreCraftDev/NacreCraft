using NacreCraft.Game.Server;
using NacreCraft.Game.World.Items;
using NacreCraft.Registry.State;

namespace NacreCraft.Game.World.Inventory;

//AnvilMenu anvil menu, maps to vanilla net.minecraft.world.inventory.AnvilMenu
//Slot layout: 0 input, 1 addition, 2 result, 3-29 main inventory, 30-38 hotbar
//The anvil combine rules need the damage and enchantment components; those are not ported, so the result stays empty
public sealed class AnvilMenu : ItemCombinerMenu
{
    public const int InputSlot = 0;
    public const int AdditionalSlot = 1;
    public const int AnvilResultSlot = 2;
    public const int MaxNameLength = 50;

    private readonly DataSlot _cost = DataSlot.Standalone();

    public AnvilMenu(int containerId, ServerPlayer player)
        : this(containerId, player, ContainerLevelAccess.Null)
    {
    }

    public AnvilMenu(int containerId, ServerPlayer player, ContainerLevelAccess access)
        : base(MenuTypes.ANVIL, containerId, player, access, CreateInputSlotDefinitions())
    {
        AddDataSlot(_cost);
    }

    public int Cost => _cost.Get(0);

    private static ItemCombinerMenuSlotDefinition CreateInputSlotDefinitions()
        => ItemCombinerMenuSlotDefinition.Create()
            .WithSlot(0, 27, 47, _ => true)
            .WithSlot(1, 76, 47, _ => true)
            .WithResultSlot(2, 134, 47)
            .Build();

    //IsValidBlock anvil states only; the anvil tag is not ported, so any block counts
    protected override bool IsValidBlock(BlockState state) => true;

    //MayPickup requires enough experience, maps to vanilla mayPickup
    protected override bool MayPickup(ServerPlayer player, bool hasItem)
        => (player.GameType == NacreCraft.Game.World.Level.GameType.Creative || player.XpLevel >= Cost) && Cost > 0;

    protected override void OnTake(ServerPlayer player, ItemStack carried)
    {
        if (player.GameType != NacreCraft.Game.World.Level.GameType.Creative)
            player.XpLevel -= Cost;
        InputSlots.ClearContent();
        _cost.Set(0, 0);
    }

    //CreateResult builds the anvil output, maps to vanilla createResult; component merging is not ported
    public override void CreateResult()
    {
        ResultSlots.SetItem(0, ItemStack.Empty);
        _cost.Set(0, 0);
    }

    //SetItemName renames the result, maps to vanilla setItemName; renaming needs the custom name component, which is not ported
    public bool SetItemName(string name) => false;

    //CalculateIncreasedRepairCost repair cost escalation, maps to vanilla calculateIncreasedRepairCost
    public static int CalculateIncreasedRepairCost(int baseCost)
        => (int)Math.Min((long)baseCost * 2L + 1L, int.MaxValue);
}
