using NacreCraft.Primitives;

namespace NacreCraft.Util;

//BlockUtil block placement helpers, maps to vanilla net.minecraft.util.BlockUtil
//Only the found-rectangle result type the portal code returns is ported; the vanilla placement search helpers are not
public static class BlockUtil
{
    //FoundRectangle a found free rectangle: minimum corner plus the two side lengths, maps to vanilla BlockUtil.FoundRectangle
    public sealed record FoundRectangle(BlockPos MinCorner, int Axis1Size, int Axis2Size);
}
