namespace NacreCraft.Util;

//Ease easing function library, maps to vanilla net.minecraft.util.Ease
//Every function maps the 0..1 alpha onto an eased 0..1 value; the out-of-range overshoot variants are intentional
public static class Ease
{
    public static float InBack(float x) => Mth.Square(x) * (2.70158f * x - 1.70158f);

    public static float InBounce(float x) => 1.0f - OutBounce(1.0f - x);

    public static float InCubic(float x) => Mth.Cube(x);

    public static float InElastic(float x)
    {
        if (x == 0.0f) return 0.0f;
        if (x == 1.0f) return 1.0f;
        return (float)(-Math.Pow(2.0, 10.0 * x - 10.0) * Math.Sin((x * 10.0 - 10.75) * 2.094395160675049));
    }

    public static float InExpo(float x) => x == 0.0f ? 0.0f : (float)Math.Pow(2.0, 10.0 * x - 10.0);

    public static float InQuart(float x) => Mth.Square(Mth.Square(x));

    public static float InQuint(float x) => Mth.Square(Mth.Square(x)) * x;

    public static float InSine(float x) => 1.0f - Mth.Cos(x * 1.5707964f);

    public static float InOutBounce(float x)
        => x < 0.5f ? (1.0f - OutBounce(1.0f - 2.0f * x)) / 2.0f : (1.0f + OutBounce(2.0f * x - 1.0f)) / 2.0f;

    public static float InOutCirc(float x)
        => x < 0.5f
            ? (float)((1.0 - Math.Sqrt(1.0 - Math.Pow(2.0 * x, 2.0))) / 2.0)
            : (float)((Math.Sqrt(1.0 - Math.Pow(-2.0 * x + 2.0, 2.0)) + 1.0) / 2.0);

    public static float InOutCubic(float x)
        => x < 0.5f ? 4.0f * Mth.Cube(x) : (float)(1.0 - Math.Pow(-2.0 * x + 2.0, 3.0) / 2.0);

    public static float InOutQuad(float x)
        => x < 0.5f ? 2.0f * Mth.Square(x) : (float)(1.0 - Math.Pow(-2.0 * x + 2.0, 2.0) / 2.0);

    public static float InOutQuart(float x)
        => x < 0.5f ? 8.0f * Mth.Square(Mth.Square(x)) : (float)(1.0 - Math.Pow(-2.0 * x + 2.0, 4.0) / 2.0);

    public static float InOutQuint(float x)
        => x < 0.5f ? 16.0f * x * x * x * x * x : (float)(1.0 - Math.Pow(-2.0 * x + 2.0, 5.0) / 2.0);

    public static float OutBounce(float x)
    {
        if (x < 0.36363637f) return 7.5625f * Mth.Square(x);
        if (x < 0.72727275f) return 7.5625f * Mth.Square(x - 0.54545456f) + 0.75f;
        if (x < 0.9090909090909091) return 7.5625f * Mth.Square(x - 0.8181818f) + 0.9375f;
        return 7.5625f * Mth.Square(x - 0.95454544f) + 0.984375f;
    }

    public static float OutElastic(float x)
    {
        if (x == 0.0f) return 0.0f;
        if (x == 1.0f) return 1.0f;
        return (float)(Math.Pow(2.0, -10.0 * x) * Math.Sin((x * 10.0 - 0.75) * 2.094395160675049) + 1.0);
    }

    public static float OutExpo(float x) => x == 1.0f ? 1.0f : 1.0f - (float)Math.Pow(2.0, -10.0 * x);

    public static float OutQuad(float x) => 1.0f - Mth.Square(1.0f - x);

    public static float OutQuint(float x) => 1.0f - (float)Math.Pow(1.0 - x, 5.0);

    public static float OutSine(float x) => Mth.Sin(x * 1.5707964f);

    public static float InOutSine(float x) => -(Mth.Cos((float)Math.PI * x) - 1.0f) / 2.0f;

    public static float OutBack(float x) => 1.0f + (2.70158f * Mth.Cube(x - 1.0f)) + (1.70158f * Mth.Square(x - 1.0f));

    public static float OutQuart(float x) => 1.0f - Mth.Square(Mth.Square(1.0f - x));

    public static float OutCubic(float x) => 1.0f - Mth.Cube(1.0f - x);

    public static float InOutExpo(float x)
    {
        if (x < 0.5f) return x == 0.0f ? 0.0f : (float)(Math.Pow(2.0, 20.0 * x - 10.0) / 2.0);
        return x == 1.0f ? 1.0f : (float)((2.0 - Math.Pow(2.0, -20.0 * x + 10.0)) / 2.0);
    }

    public static float InQuad(float x) => x * x;

    public static float OutCirc(float x) => (float)Math.Sqrt(1.0f - Mth.Square(x - 1.0f));

    public static float InOutElastic(float x)
    {
        if (x == 0.0f) return 0.0f;
        if (x == 1.0f) return 1.0f;
        var sine = Math.Sin(((20.0 * x) - 11.125) * 1.3962634801864624);
        return x < 0.5f
            ? (float)(-(Math.Pow(2.0, 20.0 * x - 10.0) * sine) / 2.0)
            : (float)((Math.Pow(2.0, (-20.0 * x) + 10.0) * sine / 2.0) + 1.0);
    }

    public static float InCirc(float x) => -(float)Math.Sqrt(1.0f - (x * x)) + 1.0f;

    public static float InOutBack(float x)
    {
        if (x < 0.5f) return 4.0f * x * x * (7.189819f * x - 2.5949094f) / 2.0f;
        var delta = (2.0f * x) - 2.0f;
        return ((delta * delta * ((3.5949094f * delta) + 2.5949094f)) + 2.0f) / 2.0f;
    }
}
