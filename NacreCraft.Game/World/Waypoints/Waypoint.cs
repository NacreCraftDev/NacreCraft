using NacreCraft.Codec;
using NacreCraft.Game.World.Entity;
using NacreCraft.Network;
using NacreCraft.Registry;
using NacreCraft.Registry.Codec;

namespace NacreCraft.Game.World.Waypoints;

//Waypoint waypoint marker, maps to vanilla net.minecraft.world.waypoints.Waypoint
//Vanilla also exposes addHideAttribute for item properties; the item property builder is not ported, so that helper stays out
public interface Waypoint
{
    //Icon waypoint icon: style key plus optional RGB color, maps to vanilla Waypoint.Icon
    public sealed class Icon
    {
        //CODEC persistence codec, field names style/color, maps to vanilla CODEC
        public static readonly Codec<Icon> CODEC = RecordCodecBuilder.Of2(
            StyleKeyCodec.FieldOf("style").ForGetter((Icon icon) => icon.Style),
            Codecs.Int.OptionalFieldOf("color").ForGetter((Icon icon) => icon.Color),
            (style, color) => new Icon { Style = style, Color = color });

        //STREAM_CODEC network codec, style key then optional RGB color, maps to vanilla STREAM_CODEC
        public static readonly StreamCodec<FriendlyByteBuf, Icon> STREAM_CODEC = new IconStreamCodec();

        public static readonly Icon NULL = new();

        public ResourceKey<WaypointStyleAsset> Style = WaypointStyleAssets.DEFAULT;
        public Optional<int> Color = Optional<int>.Empty();

        //HasData whether the icon carries any override, maps to vanilla hasData
        public bool HasData() => Style != WaypointStyleAssets.DEFAULT || Color.IsPresent;

        //CloneAndAssignStyle resolves the style and color actually sent to a receiver, maps to vanilla cloneAndAssignStyle
        //Vanilla falls back to the entity's team color; teams are not wired into the entity layer yet, so the own color is all there is
        public Icon CloneAndAssignStyle(LivingEntity entity)
        {
            var overrideStyle = GetOverrideStyle();
            var colorOverride = Color;
            if (overrideStyle == Style && !colorOverride.IsPresent) return this;
            return new Icon { Style = overrideStyle, Color = colorOverride };
        }

        //CopyFrom copies another icon's fields in place, maps to vanilla copyFrom
        public void CopyFrom(Icon other)
        {
            Color = other.Color;
            Style = other.Style;
        }

        //GetOverrideStyle the style the entity transmits, maps to vanilla getOverrideStyle
        private ResourceKey<WaypointStyleAsset> GetOverrideStyle()
            => Style != WaypointStyleAssets.DEFAULT ? Style : WaypointStyleAssets.DEFAULT;

        //StyleKeyCodec serializes a waypoint style key as an identifier, maps to vanilla ResourceKey.codec
        private static readonly Codec<ResourceKey<WaypointStyleAsset>> StyleKeyCodec =
            IdentifierCodec.Instance.ComapFlatMap(
                id => DataResult<ResourceKey<WaypointStyleAsset>>.Success(
                    ResourceKey<WaypointStyleAsset>.Create(WaypointStyleAssets.ROOT_ID, id)),
                key => key.Identifier);
    }
}

//IconStreamCodec encodes an icon as style key plus an optional three-byte RGB color, maps to vanilla Icon.STREAM_CODEC
internal sealed class IconStreamCodec : StreamCodec<FriendlyByteBuf, Waypoint.Icon>
{
    public Waypoint.Icon Decode(FriendlyByteBuf buf)
    {
        var style = ResourceKey<WaypointStyleAsset>.Create(WaypointStyleAssets.ROOT_ID, buf.ReadIdentifier());
        var color = buf.ReadBoolean()
            ? Optional<int>.Of(((buf.ReadByte() & 0xFF) << 16) | ((buf.ReadByte() & 0xFF) << 8) | (buf.ReadByte() & 0xFF))
            : Optional<int>.Empty();
        return new Waypoint.Icon { Style = style, Color = color };
    }

    public void Encode(FriendlyByteBuf buf, Waypoint.Icon value)
    {
        buf.WriteIdentifier(value.Style.Identifier);
        buf.WriteBoolean(value.Color.IsPresent);
        if (value.Color.IsPresent)
        {
            var color = value.Color.Get();
            buf.WriteByte((byte)((color >> 16) & 0xFF));
            buf.WriteByte((byte)((color >> 8) & 0xFF));
            buf.WriteByte((byte)(color & 0xFF));
        }
    }
}
