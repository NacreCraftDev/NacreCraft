using NacreCraft.Codec;
using NacreCraft.Registry;
using NacreCraft.Registry.Codec;

namespace NacreCraft.Game.World.Clock;

//PackedClockStates clock states keyed by clock, maps to vanilla net.minecraft.world.clock.PackedClockStates
//The codec form is the same registry name -> ClockState map stored in world_clocks.dat
public sealed record PackedClockStates(IReadOnlyDictionary<Holder<WorldClock>, ClockState> Clocks)
{
    public static readonly PackedClockStates Empty =
        new(new Dictionary<Holder<WorldClock>, ClockState>());

    public static readonly Codec<PackedClockStates> Codec = Codecs
        .UnboundedMap(WorldClockHolderCodec.Instance, ClockState.Codec)
        .ComapFlatMap(
            clocks => DataResult<PackedClockStates>.Success(new PackedClockStates(clocks)),
            states => new Dictionary<Holder<WorldClock>, ClockState>(states.Clocks));
}

//WorldClockHolderCodec resolves a clock holder by registry name, maps to vanilla RegistryFixedCodec for WORLD_CLOCK
internal sealed class WorldClockHolderCodec : ScalarCodec<Holder<WorldClock>>
{
    public static readonly WorldClockHolderCodec Instance = new();

    public override DataResult<Holder<WorldClock>> Parse<U>(DynamicOps<U> ops, U input)
        => IdentifierCodec.Instance.Parse(ops, input).FlatMap(id =>
        {
            var holder = BuiltInRegistries.WORLD_CLOCK.Get(id);
            return holder is null
                ? DataResult<Holder<WorldClock>>.Error(() => $"Unknown world clock: {id}")
                : DataResult<Holder<WorldClock>>.Success(holder);
        });

    public override DataResult<U> EncodeStart<U>(DynamicOps<U> ops, Holder<WorldClock> value)
    {
        var id = BuiltInRegistries.WORLD_CLOCK.GetKey(value.Value);
        return id is null
            ? DataResult<U>.Error(() => "World clock is not registered")
            : DataResult<U>.Success(ops.CreateString(id.Value.ToString()));
    }
}
