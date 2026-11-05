using NacreCraft.Game.Server;
using NacreCraft.Game.World.Items;

namespace NacreCraft.Game.World.Inventory;

//LecternMenu lectern menu, maps to vanilla net.minecraft.world.inventory.LecternMenu
//One book slot and one data slot holding the current page; the buttons page the book or take it back
public sealed class LecternMenu : AbstractContainerMenu
{
    public const int ButtonPrevPage = 1;
    public const int ButtonNextPage = 2;
    public const int ButtonTakeBook = 3;
    public const int ButtonPageJumpRangeStart = 100;

    private readonly Container _lectern;
    private readonly ContainerData _lecternData;

    public LecternMenu(int containerId, Container lectern, ContainerData lecternData)
        : base(MenuTypes.LECTERN, containerId)
    {
        _lectern = lectern;
        _lecternData = lecternData;
        AddSlot(new Slot(lectern, 0, 0, 0));
        AddDataSlots(lecternData);
    }

    public ItemStack Book => _lectern.GetItem(0);

    public int Page => _lecternData.Get(0);

    public override bool StillValid(ServerPlayer player) => true;

    //ClickMenuButton pages the book or takes it, maps to vanilla clickMenuButton
    public override bool ClickMenuButton(ServerPlayer player, int buttonId)
    {
        if (buttonId >= ButtonPageJumpRangeStart)
        {
            SetDataValue(0, buttonId - ButtonPageJumpRangeStart);
            return true;
        }
        switch (buttonId)
        {
            case ButtonNextPage:
                SetDataValue(0, _lecternData.Get(0) + 1);
                return true;
            case ButtonPrevPage:
                SetDataValue(0, _lecternData.Get(0) - 1);
                return true;
            case ButtonTakeBook:
                var book = _lectern.RemoveItemNoUpdate(0);
                _lectern.SetChanged();
                if (!player.Inventory.Add(book)) player.Drop(book, randomly: false, thrownFromHand: false);
                return true;
            default:
                return false;
        }
    }

    //QuickMoveStack the lectern slot is not part of quick move, maps to vanilla
    public override ItemStack QuickMoveStack(ServerPlayer player, int slotIndex) => ItemStack.Empty;
}
