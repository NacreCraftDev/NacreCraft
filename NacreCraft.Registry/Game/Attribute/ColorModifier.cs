using NacreCraft.Codec;
using NacreCraft.Util;

namespace NacreCraft.Registry.Environment;

//ColorModifier color modifier, maps to vanilla ColorModifier
public static class ColorModifier
{
    public static readonly AttributeModifier<int, int> AlphaBlend = new AlphaBlendColorModifier();

    public static readonly AttributeModifier<int, int> Add = new RgbColorModifier(Argb.AddRgb);

    public static readonly AttributeModifier<int, int> Subtract = new RgbColorModifier(Argb.SubtractRgb);

    public static readonly AttributeModifier<int, int> MultiplyRgb = new RgbColorModifier(Argb.Multiply);

    public static readonly AttributeModifier<int, int> MultiplyArgb = new ArgbColorModifier(Argb.Multiply);

    public static readonly AttributeModifier<int, BlendToGray> BlendToGray = new BlendToGrayColorModifier();

    //AlphaBlendColorModifier composites color by the argument's alpha
    private sealed class AlphaBlendColorModifier : AttributeModifier<int, int>
    {
        public int Apply(int subject, int argument) => Argb.AlphaBlend(subject, argument);

        public Codec<int> ArgumentCodec(EnvironmentAttribute<int> attribute) => HexColorCodec.StringArgb;

        public LerpFunction<int> ArgumentKeyframeLerp(EnvironmentAttribute<int> attribute) => LerpFunction<int>.OfColor();
    }

    //RgbColorModifier modifier that touches only rgb channels
    private sealed class RgbColorModifier : AttributeModifier<int, int>
    {
        private readonly Func<int, int, int> _function;

        public RgbColorModifier(Func<int, int, int> function) { _function = function; }

        public int Apply(int subject, int argument) => _function(subject, argument);

        public Codec<int> ArgumentCodec(EnvironmentAttribute<int> attribute) => HexColorCodec.StringRgb;

        public LerpFunction<int> ArgumentKeyframeLerp(EnvironmentAttribute<int> attribute) => LerpFunction<int>.OfColor();
    }

    //ArgbColorModifier modifier that also handles alpha
    private sealed class ArgbColorModifier : AttributeModifier<int, int>
    {
        private readonly Func<int, int, int> _function;

        public ArgbColorModifier(Func<int, int, int> function) { _function = function; }

        public int Apply(int subject, int argument) => _function(subject, argument);

        public Codec<int> ArgumentCodec(EnvironmentAttribute<int> attribute) => ArgbArgumentCodec.Instance;

        public LerpFunction<int> ArgumentKeyframeLerp(EnvironmentAttribute<int> attribute) => LerpFunction<int>.OfColor();
    }

    //BlendToGrayColorModifier blends color toward gray by brightness
    private sealed class BlendToGrayColorModifier : AttributeModifier<int, BlendToGray>
    {
        public int Apply(int subject, BlendToGray argument)
            => Argb.SrgbLerp(argument.Factor, subject, Argb.ScaleRgb(Argb.Greyscale(subject), argument.Brightness));

        public Codec<BlendToGray> ArgumentCodec(EnvironmentAttribute<int> attribute) => NacreCraft.Registry.Environment.BlendToGray.Codec;

        //Both blend parameters move together, maps to vanilla ColorModifier.BLEND_TO_GRAY
        public LerpFunction<BlendToGray> ArgumentKeyframeLerp(EnvironmentAttribute<int> attribute)
            => new BlendToGrayLerp();
    }

    //BlendToGrayLerp interpolates the brightness and the blend factor separately
    private sealed class BlendToGrayLerp : LerpFunction<BlendToGray>
    {
        public BlendToGray Apply(float alpha, BlendToGray from, BlendToGray to)
            => new(Mth.Lerp(alpha, from.Brightness, to.Brightness), Mth.Lerp(alpha, from.Factor, to.Factor));
    }
}

//ArgbArgumentCodec a fully opaque color uses the integer form and everything else the argb string, maps to vanilla ArgbModifier
internal sealed class ArgbArgumentCodec : ScalarCodec<int>
{
    public static readonly ArgbArgumentCodec Instance = new();

    public override DataResult<int> Parse<U>(DynamicOps<U> ops, U input)
    {
        var argb = HexColorCodec.StringArgb.Parse(ops, input);
        return argb.Result().IsPresent ? argb : Codecs.Int.Parse(ops, input);
    }

    public override DataResult<U> EncodeStart<U>(DynamicOps<U> ops, int value)
        => Argb.Alpha(value) == 255
            ? Codecs.Int.EncodeStart(ops, value)
            : HexColorCodec.StringArgb.EncodeStart(ops, value);
}

//BlendToGray parameters for blending toward gray, maps to vanilla ColorModifier.BlendToGray
public sealed class BlendToGray
{
    private static readonly Codec<float> UnitFloatCodec = AttributeValueCodecs.UnitFloat;

    public static readonly Codec<BlendToGray> Codec = RecordCodecBuilder.Of2(
        UnitFloatCodec.FieldOf("brightness").ForGetter((BlendToGray v) => v.Brightness),
        UnitFloatCodec.FieldOf("factor").ForGetter((BlendToGray v) => v.Factor),
        (brightness, factor) => new BlendToGray(brightness, factor));

    public float Brightness { get; }

    public float Factor { get; }

    public BlendToGray(float brightness, float factor)
    {
        Brightness = brightness;
        Factor = factor;
    }
}
