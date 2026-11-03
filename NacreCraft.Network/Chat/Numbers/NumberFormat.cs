namespace NacreCraft.Network.Chat.Numbers;

//Number format for scoreboard values, maps to vanilla net.minecraft.network.chat.numbers.NumberFormat
public interface NumberFormat
{
    //TypeId registry id used on the wire, maps to the NUMBER_FORMAT_TYPE registry order blank, styled, fixed
    int TypeId { get; }

    //Format renders a score value, maps to vanilla format(int)
    Component Format(int value);
}
