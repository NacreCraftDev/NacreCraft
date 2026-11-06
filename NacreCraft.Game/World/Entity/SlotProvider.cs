using NacreCraft.Game.World.Inventory;
using NacreCraft.Game.World.Items.Slots;

namespace NacreCraft.Game.World.Entity;

//SlotProvider an object that exposes numbered slots, maps to vanilla net.minecraft.world.entity.SlotProvider
//Entities implement this to back slot sources that reference "this" or a block entity
public interface SlotProvider
{
    SlotAccess? GetSlot(int slot);

    //GetSlotsFromRange resolves a range and drops slots the provider does not have, maps to the vanilla default method
    SlotCollection GetSlotsFromRange(IReadOnlyList<int> slots)
    {
        var resolved = new List<SlotAccess>(slots.Count);
        foreach (var slot in slots)
        {
            var access = GetSlot(slot);
            if (access is not null) resolved.Add(access);
        }
        return SlotCollections.Of(resolved);
    }
}
