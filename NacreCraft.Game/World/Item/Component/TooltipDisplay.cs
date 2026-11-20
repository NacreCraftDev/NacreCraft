using NacreCraft.Codec;
using NacreCraft.Registry;

namespace NacreCraft.Game.World.Items.Component;

//TooltipDisplay which components are hidden from the tooltip, maps to vanilla net.minecraft.world.item.component.TooltipDisplay
public sealed record TooltipDisplay(bool HideTooltip, IReadOnlySet<DataComponentType<object>> HiddenComponents)
{
    public static readonly TooltipDisplay Default = new(false, new HashSet<DataComponentType<object>>());

    private static readonly Codec<IReadOnlySet<DataComponentType<object>>> ComponentSetCodec =
        DataComponentType<object>.CODEC.ListOf().Xmap(
            list => (IReadOnlySet<DataComponentType<object>>)new HashSet<DataComponentType<object>>(list),
            set => set.ToList());

    public static readonly Codec<TooltipDisplay> Codec = RecordCodecBuilder.Of2(
        Codecs.Bool.OptionalFieldOf("hide_tooltip", false).ForGetter((TooltipDisplay value) => value.HideTooltip),
        ComponentSetCodec.OptionalFieldOf("hidden_components", Default.HiddenComponents)
            .ForGetter((TooltipDisplay value) => value.HiddenComponents),
        (hide, hidden) => new TooltipDisplay(hide, hidden));

    //WithHidden returns a copy with the component hidden or shown, maps to vanilla withHidden
    public TooltipDisplay WithHidden(DataComponentType<object> component, bool hidden)
    {
        var set = new HashSet<DataComponentType<object>>(HiddenComponents);
        if (hidden) set.Add(component);
        else set.Remove(component);
        return new TooltipDisplay(HideTooltip, set);
    }

    //Shows whether the component is visible in the tooltip, maps to vanilla shows
    public bool Shows(DataComponentType<object> component) => !HideTooltip && !HiddenComponents.Contains(component);
}
