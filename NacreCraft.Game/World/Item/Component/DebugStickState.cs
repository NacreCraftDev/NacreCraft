using NacreCraft.Codec;
using NacreCraft.Registry;
using NacreCraft.Registry.State;

namespace NacreCraft.Game.World.Items.Component;

//TooltipFlag tooltip context flags, maps to vanilla net.minecraft.world.item.TooltipFlag
public sealed record TooltipFlag(bool Advanced, bool Creative)
{
    public static readonly TooltipFlag Default = new(false, false);
    public static readonly TooltipFlag AdvancedFlag = new(true, false);
}

//TooltipProvider a component that contributes tooltip lines, maps to vanilla net.minecraft.world.item.component.TooltipProvider
//NC passes the target line list and the item stack directly instead of the vanilla TooltipContext/DataComponentGetter pair
public interface TooltipProvider
{
    void AddToTooltip(IList<NacreCraft.Network.Chat.Component> tooltip, TooltipFlag flag, NacreCraft.Game.World.Items.ItemStack stack);
}

//DebugStickState the block properties a debug stick last changed, maps to vanilla net.minecraft.world.item.component.DebugStickState
//The property value object is not stored: the state definition lookup that turns a property name back into a Property is not ported
public sealed record DebugStickState(IReadOnlyDictionary<Holder<Block>, string> Properties)
{
    public static readonly DebugStickState Empty = new(new Dictionary<Holder<Block>, string>());

    public static readonly Codec<DebugStickState> Codec =
        Codecs.UnboundedMap(HolderSetCodecs.BlockRef, Codecs.String).Xmap(
            map => new DebugStickState(map),
            value => value.Properties.ToDictionary(pair => pair.Key, pair => pair.Value));

    //With records the property name chosen for the block
    public DebugStickState With(Holder<Block> block, PropertyBase property)
    {
        var map = Properties.ToDictionary(pair => pair.Key, pair => pair.Value);
        map[block] = property.Name;
        return new DebugStickState(map);
    }

    public string? GetPropertyName(Holder<Block> block)
        => Properties.TryGetValue(block, out var name) ? name : null;
}
