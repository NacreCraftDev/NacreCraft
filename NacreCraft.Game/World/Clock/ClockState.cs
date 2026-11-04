using NacreCraft.Codec;
using NacreCraft.Nbt;

namespace NacreCraft.Game.World.Clock;

//ClockState single-clock persisted state, maps to vanilla net.minecraft.world.clock.ClockState
//NBT fields total_ticks/partial_tick/rate/paused match the vanilla Codec
public sealed record ClockState(long TotalTicks, float PartialTick, float Rate, bool Paused)
{
    public const float DefaultRate = 1f;

    //Codec data pack form, maps to vanilla ClockState.CODEC
    //The rate is not validated as positive the way vanilla POSITIVE_FLOAT is; a non-positive rate simply freezes the clock
    public static readonly Codec<ClockState> Codec = RecordCodecBuilder.Of4<ClockState, long, float, float, bool>(
        Codecs.Long.FieldOf("total_ticks").ForGetter<ClockState, long>(state => state.TotalTicks),
        Codecs.Float.OptionalFieldOf("partial_tick", 0f).ForGetter<ClockState, float>(state => state.PartialTick),
        Codecs.Float.OptionalFieldOf("rate", DefaultRate).ForGetter<ClockState, float>(state => state.Rate),
        Codecs.Bool.OptionalFieldOf("paused", false).ForGetter<ClockState, bool>(state => state.Paused),
        (totalTicks, partialTick, rate, paused) => new ClockState(totalTicks, partialTick, rate, paused));

    //Save writes to NBT; missing fields are filled with defaults like vanilla optionalFieldOf
    public CompoundTag Save(CompoundTag tag)
    {
        tag.PutLong("total_ticks", TotalTicks);
        tag.PutFloat("partial_tick", PartialTick);
        tag.PutFloat("rate", Rate);
        tag.PutBoolean("paused", Paused);
        return tag;
    }

    //Load restores from NBT; missing fields default to 0/0/1/false like vanilla
    public static ClockState Load(CompoundTag tag)
        => new(
            tag.GetLongValue("total_ticks"),
            tag.GetFloat("partial_tick")?.Value ?? 0f,
            tag.GetFloat("rate")?.Value ?? DefaultRate,
            tag.GetBooleanOr("paused", false));
}
