using NacreCraft.Codec;
using NacreCraft.Network;
using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Items.Component;

//MapPostProcessing how a map is post-processed after creation, maps to vanilla net.minecraft.world.item.component.MapPostProcessing
public enum MapPostProcessing
{
    Lock,
    Scale,
}

//MapPostProcessingExtensions id mapping and stream codec, maps to vanilla ID_MAP/STREAM_CODEC
public static class MapPostProcessingExtensions
{
    public static int Id(this MapPostProcessing value) => (int)value;

    public static MapPostProcessing ById(int id) => id is 0 or 1 ? (MapPostProcessing)id : MapPostProcessing.Lock;

    public static readonly StreamCodec<FriendlyByteBuf, MapPostProcessing> StreamCodec =
        ByteBufCodecs.VarInt.Map(ById, value => value.Id());
}

//MapItemColor the RGB tint of a filled map, maps to vanilla net.minecraft.world.item.component.MapItemColor
public sealed record MapItemColor(int Rgb)
{
    public static readonly MapItemColor Default = new(4603950);

    public static readonly Codec<MapItemColor> Codec = Codecs.Int.Xmap(rgb => new MapItemColor(rgb), color => color.Rgb);

    public static readonly StreamCodec<FriendlyByteBuf, MapItemColor> StreamCodec =
        ByteBufCodecs.Int.Map(rgb => new MapItemColor(rgb), color => color.Rgb);
}
