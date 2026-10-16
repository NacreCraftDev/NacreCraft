namespace NacreCraft.Resources;

//PackSelectionConfig selection rules of a pack, maps to vanilla net.minecraft.server.packs.PackSelectionConfig
//Required packs cannot be disabled, FixedPosition packs cannot be moved, DefaultPosition decides which end a required pack inserts at
public sealed record PackSelectionConfig(bool Required, PackPosition DefaultPosition, bool FixedPosition);

//PackPosition insert end of a pack inside the selection, maps to vanilla Pack.Position
//The selection list is stored low priority first and reversed for display, so the top of the pack screen is the highest priority
public enum PackPosition
{
    Top,
    Bottom
}

//PackPositionRules holds the insert rule, kept in the same file as the enum it operates on
public static class PackPositionRules
{
    //Insert places value into list after skipping packs pinned to the same end, maps to vanilla Position.insert
    //The branch picks the end with reverse applied, while the skip test keeps using the original end, matching vanilla
    public static void Insert<T>(this PackPosition position, List<T> list, T value, Func<T, PackSelectionConfig> converter, bool reverse)
    {
        if ((reverse ? position.Opposite() : position) == PackPosition.Bottom)
        {
            var insertAt = 0;
            while (insertAt < list.Count && PinnedTo(converter(list[insertAt]), position)) insertAt++;
            list.Insert(insertAt, value);
            return;
        }

        var last = list.Count - 1;
        while (last >= 0 && PinnedTo(converter(list[last]), position)) last--;
        list.Insert(last + 1, value);
    }

    //PinnedTo a pack is pinned when its position is fixed and it declares this end as its default
    private static bool PinnedTo(PackSelectionConfig config, PackPosition position)
        => config.FixedPosition && config.DefaultPosition == position;

    //Opposite the other end, used when a caller asks to insert from the reversed side
    public static PackPosition Opposite(this PackPosition position)
        => position == PackPosition.Top ? PackPosition.Bottom : PackPosition.Top;
}
