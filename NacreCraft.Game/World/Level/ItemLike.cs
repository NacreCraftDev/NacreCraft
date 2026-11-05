using NacreCraft.Registry;

namespace NacreCraft.Game.World.Level;

//ItemLike something that can be taken as an item, maps to vanilla net.minecraft.world.level.ItemLike
public interface ItemLike
{
    Item AsItem();
}
