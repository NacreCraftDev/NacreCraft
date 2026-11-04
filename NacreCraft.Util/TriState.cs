using NacreCraft.Codec;

namespace NacreCraft.Util;

//TriState three-valued boolean, maps to vanilla net.minecraft.util.TriState
//The codec accepts a plain boolean or the string "default"
public enum TriState
{
    True,
    False,
    Default
}

//TriStateCodec tri-state codec, maps to vanilla TriState.CODEC
public static class TriStates
{
    public static readonly Codec<TriState> Codec = new TriStateCodec();

    //From turns a plain boolean into TRUE or FALSE
    public static TriState From(bool value) => value ? TriState.True : TriState.False;

    //ToBoolean resolves DEFAULT through the caller's fallback
    public static bool ToBoolean(this TriState state, bool defaultValue)
        => state switch
        {
            TriState.True => true,
            TriState.False => false,
            _ => defaultValue
        };

    //TriStateCodec boolean form for the concrete values and the string form for DEFAULT
    private sealed class TriStateCodec : ScalarCodec<TriState>
    {
        public override DataResult<TriState> Parse<U>(DynamicOps<U> ops, U input)
        {
            var boolean = ops.GetBooleanValue(input);
            if (boolean.Result().IsPresent)
                return DataResult<TriState>.Success(From(boolean.GetOrThrow()));
            return ops.GetStringValue(input).FlatMap(name => name switch
            {
                "true" => DataResult<TriState>.Success(TriState.True),
                "false" => DataResult<TriState>.Success(TriState.False),
                "default" => DataResult<TriState>.Success(TriState.Default),
                _ => DataResult<TriState>.Error(() => $"Unknown tri state: {name}")
            });
        }

        public override DataResult<U> EncodeStart<U>(DynamicOps<U> ops, TriState value)
            => value switch
            {
                TriState.True => DataResult<U>.Success(ops.CreateBoolean(true)),
                TriState.False => DataResult<U>.Success(ops.CreateBoolean(false)),
                _ => DataResult<U>.Success(ops.CreateString("default"))
            };
    }
}
