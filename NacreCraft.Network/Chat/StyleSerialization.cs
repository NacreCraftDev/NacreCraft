namespace NacreCraft.Network.Chat;

using NacreCraft.Network;
using NacreCraft.Nbt;
using NacreCraft.Registry;

//Style network serialization, maps to vanilla net.minecraft.network.chat.Style.Serializer
//The wire form is a rootless NBT compound of the style fields, matching ByteBufCodecs.fromCodecWithRegistriesTrusted
//Click and hover events are not implemented yet, those two fields are skipped on read instead of failing
public static class StyleSerialization
{
    //StreamCodec<FriendlyByteBuf, Style>, maps to vanilla Style.Serializer.TRUSTED_STREAM_CODEC
    public static readonly StreamCodec<FriendlyByteBuf, Style> StreamCodec = new StyleStreamCodec();

    //ToTag writes the non-null style fields, maps to Style.Serializer.CODEC encoding through NbtOps
    public static CompoundTag ToTag(Style style)
    {
        var tag = new CompoundTag();
        if (style.Color is { } color && color.Serialize() is { } colorText) tag.PutString("color", colorText);
        if (style.ShadowColor is { } shadowColor) tag.PutInt("shadow_color", shadowColor);
        if (style.IsBold) tag.PutBoolean("bold", true);
        if (style.IsItalic) tag.PutBoolean("italic", true);
        if (style.IsUnderlined) tag.PutBoolean("underlined", true);
        if (style.IsStrikethrough) tag.PutBoolean("strikethrough", true);
        if (style.IsObfuscated) tag.PutBoolean("obfuscated", true);
        if (style.Insertion is { } insertion) tag.PutString("insertion", insertion);
        if (!style.Font.Equals(FontDescription.Default) && style.Font.ToString() is { } fontText)
            tag.PutString("font", fontText);
        return tag;
    }

    //FromTag rebuilds the style, unknown fields are ignored
    public static Style FromTag(CompoundTag tag)
    {
        var style = Style.Empty;
        if (tag.GetString("color") is { } colorTag && TextColor.ParseColor(colorTag.Value) is { } color)
            style = style.WithColor(color);
        if (tag.Contains("shadow_color")) style = style.WithShadowColor(tag.GetIntValue("shadow_color"));
        if (tag.GetBooleanOr("bold", false)) style = style.WithBold(true);
        if (tag.GetBooleanOr("italic", false)) style = style.WithItalic(true);
        if (tag.GetBooleanOr("underlined", false)) style = style.WithUnderlined(true);
        if (tag.GetBooleanOr("strikethrough", false)) style = style.WithStrikethrough(true);
        if (tag.GetBooleanOr("obfuscated", false)) style = style.WithObfuscated(true);
        if (tag.GetString("insertion") is { } insertionTag) style = style.WithInsertion(insertionTag.Value);
        if (tag.GetString("font") is { } fontTag && Identifier.TryParse(fontTag.Value) is { } fontId)
            style = style.WithFont(new FontDescription.Resource(fontId));
        return style;
    }

    //StyleStreamCodec reads and writes the compound as a rootless NBT tag
    private sealed class StyleStreamCodec : StreamCodec<FriendlyByteBuf, Style>
    {
        public Style Decode(FriendlyByteBuf buf) => buf.ReadNbt() is CompoundTag tag ? FromTag(tag) : Style.Empty;

        public void Encode(FriendlyByteBuf buf, Style value) => buf.WriteNbt(ToTag(value));
    }
}
