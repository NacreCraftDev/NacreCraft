namespace NacreCraft.Network.Chat.Numbers;

using NacreCraft.Network;

//Number format stream codecs, maps to vanilla net.minecraft.network.chat.numbers.NumberFormatTypes
public static class NumberFormatTypes
{
    //Registry id order from vanilla bootstrap: blank, styled, fixed
    public const int BlankTypeId = 0;
    public const int StyledTypeId = 1;
    public const int FixedTypeId = 2;

    //StreamCodec reads the NUMBER_FORMAT_TYPE registry id then that type's payload, maps to vanilla STREAM_CODEC
    public static readonly StreamCodec<FriendlyByteBuf, NumberFormat> StreamCodec = new NumberFormatStreamCodec();

    //OptionalStreamCodec prepends the optional flag, maps to vanilla OPTIONAL_STREAM_CODEC
    public static readonly StreamCodec<FriendlyByteBuf, NumberFormat?> OptionalStreamCodec = new OptionalNumberFormatStreamCodec();

    private sealed class NumberFormatStreamCodec : StreamCodec<FriendlyByteBuf, NumberFormat>
    {
        public NumberFormat Decode(FriendlyByteBuf buf)
        {
            var typeId = buf.ReadVarInt();
            return typeId switch
            {
                BlankTypeId => BlankFormat.Instance,
                StyledTypeId => new StyledFormat(StyleSerialization.StreamCodec.Decode(buf)),
                FixedTypeId => new FixedFormat(ComponentSerialization.StreamCodec.Decode(buf)),
                _ => throw new InvalidOperationException($"Unknown number format type {typeId}"),
            };
        }

        public void Encode(FriendlyByteBuf buf, NumberFormat value)
        {
            buf.WriteVarInt(value.TypeId);
            switch (value)
            {
                case StyledFormat styled:
                    StyleSerialization.StreamCodec.Encode(buf, styled.Style);
                    break;
                case FixedFormat fixedFormat:
                    ComponentSerialization.StreamCodec.Encode(buf, fixedFormat.Value);
                    break;
            }
        }
    }

    private sealed class OptionalNumberFormatStreamCodec : StreamCodec<FriendlyByteBuf, NumberFormat?>
    {
        public NumberFormat? Decode(FriendlyByteBuf buf)
            => buf.ReadBoolean() ? NumberFormatTypes.StreamCodec.Decode(buf) : null;

        public void Encode(FriendlyByteBuf buf, NumberFormat? value)
        {
            buf.WriteBoolean(value is not null);
            if (value is not null) NumberFormatTypes.StreamCodec.Encode(buf, value);
        }
    }
}
