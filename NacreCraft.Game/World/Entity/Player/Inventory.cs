using NacreCraft.Game.Server;
using NacreCraft.Game.World.Items;
using NacreCraft.Network.Chat;
using NacreCraft.Registry;
using ContainerType = NacreCraft.Game.World.Inventory.Container;

namespace NacreCraft.Game.World.Entity;

//Inventory the 36 main slots plus the equipment slots of a player, maps to vanilla net.minecraft.world.entity.player.Inventory
//Slots 0-8 are the hotbar, 9-35 the main grid, 36-42 map onto the equipment view (armor, offhand, body, saddle)
//NC has no container menu yet, so the carried stack and the set-slot packet are not part of this class
public sealed class Inventory : ContainerType
{
    public const int InventorySize = 36;
    public const int SelectionSize = 9;
    public const int SlotOffhand = 40;
    public const int SlotBodyArmor = 41;
    public const int SlotSaddle = 42;
    public const int NotFoundIndex = -1;
    public const int PopTimeDuration = 5;

    //EquipmentSlotMapping container slot number to equipment slot, maps to vanilla EQUIPMENT_SLOT_MAPPING
    public static readonly IReadOnlyDictionary<int, EquipmentSlot> EquipmentSlotMapping =
        new Dictionary<int, EquipmentSlot>
        {
            [36] = EquipmentSlot.FEET,
            [37] = EquipmentSlot.LEGS,
            [38] = EquipmentSlot.CHEST,
            [39] = EquipmentSlot.HEAD,
            [SlotOffhand] = EquipmentSlot.OFFHAND,
            [SlotBodyArmor] = EquipmentSlot.BODY,
            [SlotSaddle] = EquipmentSlot.SADDLE,
        };

    private static readonly Component DefaultName = Component.Translatable("container.inventory");

    private readonly ItemStack[] _items = new ItemStack[InventorySize];
    private readonly EntityEquipment _equipment;
    private int _selected;
    private int _timesChanged;

    public Inventory(Player player, EntityEquipment equipment)
    {
        Player = player;
        _equipment = equipment;
        for (var i = 0; i < _items.Length; i++) _items[i] = ItemStack.Empty;
    }

    public Player Player { get; }

    public int SelectedSlot
    {
        get => _selected;
        set
        {
            if (!IsHotbarSlot(value)) throw new ArgumentOutOfRangeException(nameof(value), "Invalid selected slot");
            _selected = value;
        }
    }

    public ItemStack SelectedItem
    {
        get => _items[_selected];
        set => _items[_selected] = value;
    }

    //NonEquipmentItems the 36 main slots, maps to vanilla getNonEquipmentItems
    public IReadOnlyList<ItemStack> NonEquipmentItems => _items;

    public static int GetSelectionSize() => SelectionSize;

    public static bool IsHotbarSlot(int slot) => slot >= 0 && slot < SelectionSize;

    //GetFreeSlot first empty main slot, -1 when full, maps to vanilla getFreeSlot
    public int GetFreeSlot()
    {
        for (var i = 0; i < _items.Length; i++)
        {
            if (_items[i].IsEmpty()) return i;
        }
        return NotFoundIndex;
    }

    //AddAndPickItem selects a hotbar slot and puts the stack there, maps to vanilla addAndPickItem
    public void AddAndPickItem(ItemStack stack)
    {
        SelectedSlot = GetSuitableHotbarSlot();
        if (!_items[_selected].IsEmpty())
        {
            var freeSlot = GetFreeSlot();
            if (freeSlot != NotFoundIndex) _items[freeSlot] = _items[_selected];
        }
        _items[_selected] = stack;
    }

    //PickSlot swaps a slot with the selected hotbar slot, maps to vanilla pickSlot
    public void PickSlot(int slot)
    {
        SelectedSlot = GetSuitableHotbarSlot();
        (_items[_selected], _items[slot]) = (_items[slot], _items[_selected]);
    }

    //FindSlotMatchingItem first slot with the same item and components, maps to vanilla findSlotMatchingItem
    public int FindSlotMatchingItem(ItemStack stack)
    {
        for (var i = 0; i < _items.Length; i++)
        {
            if (!_items[i].IsEmpty() && _items[i].IsSameItemAndComponentsAs(stack)) return i;
        }
        return NotFoundIndex;
    }

    //IsUsableForCrafting whether the stack is clean enough for the recipe book, maps to vanilla isUsableForCrafting
    //NC has no CUSTOM_NAME component yet, so only damage and enchantments disqualify the stack
    public static bool IsUsableForCrafting(ItemStack stack) => !stack.IsDamaged() && !stack.IsEnchanted();

    //FindSlotMatchingCraftingIngredient slot holding a usable stack of the ingredient, maps to vanilla findSlotMatchingCraftingIngredient
    public int FindSlotMatchingCraftingIngredient(Holder<Item> item, ItemStack existingItem)
    {
        for (var i = 0; i < _items.Length; i++)
        {
            var inventoryItem = _items[i];
            if (inventoryItem.IsEmpty() || !IsSameItem(inventoryItem, item) || !IsUsableForCrafting(inventoryItem))
                continue;
            if (existingItem.IsEmpty() || inventoryItem.IsSameItemAndComponentsAs(existingItem)) return i;
        }
        return NotFoundIndex;
    }

    //GetSuitableHotbarSlot the empty slot nearest the selection, then a non-enchanted one, maps to vanilla getSuitableHotbarSlot
    public int GetSuitableHotbarSlot()
    {
        for (var slot = 0; slot < SelectionSize; slot++)
        {
            var index = (_selected + slot) % SelectionSize;
            if (_items[index].IsEmpty()) return index;
        }
        for (var slot = 0; slot < SelectionSize; slot++)
        {
            var index = (_selected + slot) % SelectionSize;
            if (!_items[index].IsEnchanted()) return index;
        }
        return _selected;
    }

    //ClearOrCountMatchingItems counts or removes matching items across the main grid and the free craft slots
    //The carried menu stack is not included yet, maps to vanilla clearOrCountMatchingItems without the menu part
    public int ClearOrCountMatchingItems(Predicate<ItemStack> predicate, int amountToRemove, ContainerType? craftSlots)
    {
        var countOnly = amountToRemove == 0;
        var count = ClearOrCount(this, predicate, amountToRemove, countOnly);
        if (craftSlots is not null)
            count += ClearOrCount(craftSlots, predicate, amountToRemove - count, countOnly);
        return count;
    }

    //Add adds the stack into free/stackable slots, maps to vanilla add(ItemStack)
    public bool Add(ItemStack stack) => Add(NotFoundIndex, stack);

    //Add adds into a preferred slot, -1 picks automatically, maps to vanilla add(int, ItemStack)
    public bool Add(int slot, ItemStack stack)
    {
        if (stack.IsEmpty()) return false;
        if (!stack.IsDamaged())
        {
            int previousCount;
            do
            {
                previousCount = stack.Count;
                stack.SetCount(slot == NotFoundIndex ? AddResource(stack) : AddResource(slot, stack));
                if (stack.IsEmpty()) break;
            } while (stack.Count < previousCount);

            if (stack.Count != previousCount || !Player.HasInfiniteMaterials()) return stack.Count < previousCount;

            stack.SetCount(0);
            return true;
        }

        if (slot == NotFoundIndex) slot = GetFreeSlot();
        if (slot >= 0)
        {
            _items[slot] = stack.CopyAndClear();
            _items[slot].SetPopTime(PopTimeDuration);
            return true;
        }
        if (Player.HasInfiniteMaterials())
        {
            stack.SetCount(0);
            return true;
        }
        return false;
    }

    //PlaceItemBackInInventory puts a stack back, dropping the remainder, maps to vanilla placeItemBackInInventory without the packet
    public void PlaceItemBackInInventory(ItemStack stack) => PlaceItemBackInInventory(stack, true);

    public void PlaceItemBackInInventory(ItemStack stack, bool shouldSendSetSlotPacket)
    {
        while (!stack.IsEmpty())
        {
            var slot = GetSlotWithRemainingSpace(stack);
            if (slot == NotFoundIndex) slot = GetFreeSlot();
            if (slot == NotFoundIndex)
            {
                DropStack(stack);
                return;
            }
            var space = stack.GetMaxStackSize() - GetItem(slot).Count;
            if (Add(slot, stack.Split(space)) && shouldSendSetSlotPacket)
            {
                //The ClientboundSetPlayerInventoryPacket sync is wired once the container menu exists
            }
        }
    }

    //GetSlotWithRemainingSpace slot that can take more of the stack, maps to vanilla getSlotWithRemainingSpace
    public int GetSlotWithRemainingSpace(ItemStack stack)
    {
        if (HasRemainingSpaceForItem(_items[_selected], stack)) return _selected;
        if (HasRemainingSpaceForItem(_equipment.Get(EquipmentSlot.OFFHAND), stack)) return SlotOffhand;
        for (var i = 0; i < _items.Length; i++)
        {
            if (HasRemainingSpaceForItem(_items[i], stack)) return i;
        }
        return NotFoundIndex;
    }

    //Tick ticks every held stack; NC items have no inventory tick hook yet
    public void Tick()
    {
    }

    //RemoveFromSelected removes one or all from the selected slot, maps to vanilla removeFromSelected
    public ItemStack RemoveFromSelected(bool all)
    {
        var selectedItem = SelectedItem;
        if (selectedItem.IsEmpty()) return ItemStack.Empty;
        return RemoveItem(_selected, all ? selectedItem.Count : 1);
    }

    //DropAll drops the main grid and the equipment, maps to vanilla dropAll
    public void DropAll()
    {
        foreach (var stack in _items)
        {
            if (!stack.IsEmpty()) DropStack(stack);
        }
        foreach (var slot in EquipmentSlotMapping.Values)
        {
            var stack = _equipment.Get(slot);
            if (!stack.IsEmpty()) DropStack(stack);
            _equipment.Set(slot, ItemStack.Empty);
        }
        ClearContent();
    }

    //ReplaceWith copies another inventory including the selection, maps to vanilla replaceWith
    public void ReplaceWith(Inventory other)
    {
        for (var i = 0; i < Size; i++) SetItem(i, other.GetItem(i));
        SelectedSlot = other.SelectedSlot;
    }

    //FillStackedContents accounts every clean stack for recipe matching, maps to vanilla fillStackedContents
    public void FillStackedContents(StackedItemContents contents)
    {
        foreach (var stack in _items) contents.AccountSimpleStack(stack);
    }

    //Contains whether a stack with the same item and components is present, maps to vanilla contains(ItemStack)
    public bool Contains(ItemStack stack)
    {
        foreach (var item in _items)
        {
            if (!item.IsEmpty() && item.IsSameItemAndComponentsAs(stack)) return true;
        }
        return false;
    }

    public bool Contains(TagKey<Item> tag)
    {
        foreach (var item in _items)
        {
            if (!item.IsEmpty() && item.GetTypeHolder() is { } holder && holder.Is(tag)) return true;
        }
        return false;
    }

    public bool Contains(Predicate<ItemStack> predicate)
    {
        foreach (var item in _items)
        {
            if (predicate(item)) return true;
        }
        return false;
    }

    //StillValid a player inventory is always valid for its owner, maps to vanilla stillValid
    public bool StillValid(Player player) => true;

    public int TimesChanged => _timesChanged;

    public Component GetName() => DefaultName;

    //Container
    public int Size => InventorySize + EquipmentSlotMapping.Count;

    public ItemStack GetItem(int slot)
    {
        if (slot < InventorySize) return _items[slot];
        return EquipmentSlotMapping.TryGetValue(slot, out var equipmentSlot)
            ? _equipment.Get(equipmentSlot)
            : ItemStack.Empty;
    }

    public void SetItem(int slot, ItemStack stack)
    {
        if (slot < InventorySize) _items[slot] = stack;
        if (EquipmentSlotMapping.TryGetValue(slot, out var equipmentSlot)) _equipment.Set(equipmentSlot, stack);
    }

    public ItemStack RemoveItem(int slot, int count)
    {
        if (slot < InventorySize)
        {
            var stack = _items[slot];
            if (stack.IsEmpty()) return ItemStack.Empty;
            return count >= stack.Count ? TakeSlot(slot) : SplitInto(slot, count);
        }
        if (!EquipmentSlotMapping.TryGetValue(slot, out var equipmentSlot)) return ItemStack.Empty;
        var equipmentStack = _equipment.Get(equipmentSlot);
        return equipmentStack.IsEmpty() ? ItemStack.Empty : equipmentStack.Split(count);
    }

    public ItemStack RemoveItemNoUpdate(int slot)
    {
        if (slot < InventorySize) return TakeSlot(slot);
        if (!EquipmentSlotMapping.TryGetValue(slot, out var equipmentSlot)) return ItemStack.Empty;
        var stack = _equipment.Get(equipmentSlot);
        _equipment.Set(equipmentSlot, ItemStack.Empty);
        return stack;
    }

    public void SetChanged() => _timesChanged++;

    public bool IsEmpty()
    {
        foreach (var stack in _items)
        {
            if (!stack.IsEmpty()) return false;
        }
        return _equipment.IsEmpty();
    }

    public void ClearContent()
    {
        for (var i = 0; i < _items.Length; i++) _items[i] = ItemStack.Empty;
        _equipment.Clear();
    }

    public bool CanPlaceItem(int slot, ItemStack stack) => true;

    private ItemStack TakeSlot(int slot)
    {
        var stack = _items[slot];
        _items[slot] = ItemStack.Empty;
        return stack;
    }

    private ItemStack SplitInto(int slot, int count)
    {
        var stack = _items[slot];
        var taken = stack.Split(count);
        if (stack.IsEmpty()) _items[slot] = ItemStack.Empty;
        else _items[slot] = stack;
        return taken;
    }

    private static bool HasRemainingSpaceForItem(ItemStack slotItem, ItemStack newItem)
        => !slotItem.IsEmpty()
           && slotItem.IsSameItemAndComponentsAs(newItem)
           && slotItem.IsStackable()
           && slotItem.Count < slotItem.GetMaxStackSize();

    private static bool IsSameItem(ItemStack stack, Holder<Item> item)
    {
        var holder = stack.GetTypeHolder();
        if (holder is null) return false;
        return ReferenceEquals(holder, item) || ReferenceEquals(holder.Value, item.Value);
    }

    private int AddResource(ItemStack stack)
    {
        var slot = GetSlotWithRemainingSpace(stack);
        if (slot == NotFoundIndex) slot = GetFreeSlot();
        return slot == NotFoundIndex ? stack.Count : AddResource(slot, stack);
    }

    private int AddResource(int slot, ItemStack stack)
    {
        var count = stack.Count;
        var slotItem = GetItem(slot);
        if (slotItem.IsEmpty())
        {
            slotItem = stack.CopyWithCount(0);
            SetItem(slot, slotItem);
        }
        var maxToAdd = slotItem.GetMaxStackSize() - slotItem.Count;
        var toAdd = Math.Min(count, maxToAdd);
        if (toAdd == 0) return count;
        slotItem.Grow(toAdd);
        slotItem.SetPopTime(PopTimeDuration);
        return count - toAdd;
    }

    private static int ClearOrCount(ContainerType container, Predicate<ItemStack> predicate, int maxAmount, bool countOnly)
    {
        var total = 0;
        for (var slot = 0; slot < container.Size && (countOnly || total < maxAmount); slot++)
        {
            var stack = container.GetItem(slot);
            if (stack.IsEmpty() || !predicate(stack)) continue;
            var amount = countOnly ? stack.Count : Math.Min(stack.Count, maxAmount - total);
            if (!countOnly)
            {
                stack.Shrink(amount);
                if (stack.IsEmpty()) container.SetItem(slot, ItemStack.Empty);
            }
            total += amount;
        }
        return total;
    }

    private void DropStack(ItemStack stack)
    {
        if (Player is ServerPlayer serverPlayer) serverPlayer.Drop(stack, true, false);
        stack.SetCount(0);
    }
}
