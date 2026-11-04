using NacreCraft.Network;

namespace NacreCraft.Game.World.Clock;

//ClockNetworkState clock state sent over the network, maps to vanilla net.minecraft.world.clock.ClockNetworkState
public sealed record ClockNetworkState(long TotalTicks, float PartialTick, float Rate)
{
    //StreamCodec wire form: VAR_LONG total ticks followed by the partial tick and rate floats
    public static readonly StreamCodec<FriendlyByteBuf, ClockNetworkState> StreamCodec = StreamCodecs.Composite(
        ByteBufCodecs.VarLong, (ClockNetworkState value) => value.TotalTicks,
        ByteBufCodecs.Float, (ClockNetworkState value) => value.PartialTick,
        ByteBufCodecs.Float, (ClockNetworkState value) => value.Rate,
        (totalTicks, partialTick, rate) => new ClockNetworkState(totalTicks, partialTick, rate));
}
