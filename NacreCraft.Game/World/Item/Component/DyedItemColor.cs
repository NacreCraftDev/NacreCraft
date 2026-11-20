using NacreCraft.Codec;
using NacreCraft.Network;

namespace NacreCraft.Game.World.Items.Component;

//DyedItemColor the dye color of a leather item, maps to vanilla net.minecraft.world.item.component.DyedItemColor
//The dye mixing helpers wait for DyeColor and the ARGB helper, so only the stored value and codecs are ported
public sealed record DyedItemColor(int Rgb)
{
    public const int LeatherColor = -6265536;

    public static readonly Codec<DyedItemColor> Codec =
        Codecs.Int.Xmap(rgb => new DyedItemColor(rgb), value => value.Rgb);

    public static readonly StreamCodec<FriendlyByteBuf, DyedItemColor> StreamCodec =
        ByteBufCodecs.Int.Map(rgb => new DyedItemColor(rgb), value => value.Rgb);
}
