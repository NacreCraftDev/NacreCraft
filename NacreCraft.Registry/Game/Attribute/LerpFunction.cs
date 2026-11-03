using NacreCraft.Util;

namespace NacreCraft.Registry.Environment;

//LerpFunction blends two values by an eased alpha, maps to vanilla net.minecraft.world.attribute.LerpFunction
//Every factory lives on the closed interface because C# interfaces cannot hold the shared singletons as fields
public interface LerpFunction<T>
{
    T Apply(float alpha, T from, T to);

    //OfConstant steps at the end of the segment, maps to vanilla LerpFunction.ofConstant
    static LerpFunction<T> OfConstant() => ConstantLerp<T>.Instance;

    //OfStep switches to the target once alpha passes the threshold, maps to vanilla LerpFunction.ofStep
    static LerpFunction<T> OfStep(float threshold) => new StepLerp<T>(threshold);

    //OfFloat linear interpolation of floats, maps to vanilla LerpFunction.ofFloat
    static LerpFunction<float> OfFloat() => FloatLerp.Instance;

    //OfInteger rounded linear interpolation of integers, maps to vanilla LerpFunction.ofInteger
    static LerpFunction<int> OfInteger() => IntegerLerp.Instance;

    //OfColor interpolates packed colors in sRGB space, maps to vanilla LerpFunction.ofColor
    static LerpFunction<int> OfColor() => ColorLerp.Instance;

    //OfDegrees interpolates angles along the shorter arc, jumping to the target past the maximum delta
    static LerpFunction<float> OfDegrees(float maxDelta) => new DegreesLerp(maxDelta);
}

//ConstantLerp keeps the source value until the segment ends
internal sealed class ConstantLerp<T> : LerpFunction<T>
{
    public static readonly ConstantLerp<T> Instance = new();

    public T Apply(float alpha, T from, T to) => alpha >= 1.0f ? to : from;
}

//StepLerp switches at a configurable alpha
internal sealed class StepLerp<T>(float threshold) : LerpFunction<T>
{
    public T Apply(float alpha, T from, T to) => alpha >= threshold ? to : from;
}

//FloatLerp linear interpolation of floats
internal sealed class FloatLerp : LerpFunction<float>
{
    public static readonly FloatLerp Instance = new();

    public float Apply(float alpha, float from, float to) => Mth.Lerp(alpha, from, to);
}

//IntegerLerp rounded linear interpolation of integers
internal sealed class IntegerLerp : LerpFunction<int>
{
    public static readonly IntegerLerp Instance = new();

    public int Apply(float alpha, int from, int to) => Mth.LerpInt(alpha, from, to);
}

//ColorLerp sRGB interpolation of packed colors
internal sealed class ColorLerp : LerpFunction<int>
{
    public static readonly ColorLerp Instance = new();

    public int Apply(float alpha, int from, int to) => Argb.SrgbLerp(alpha, from, to);
}

//DegreesLerp interpolates angles across the shorter arc
internal sealed class DegreesLerp(float maxDelta) : LerpFunction<float>
{
    public float Apply(float alpha, float from, float to)
    {
        var delta = Mth.WrapDegrees(to - from);
        return Math.Abs(delta) >= maxDelta ? to : from + (alpha * delta);
    }
}
