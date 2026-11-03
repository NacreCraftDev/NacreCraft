using NacreCraft.Codec;

namespace NacreCraft.Util;

//EasingType curve applied to the alpha of a keyframe segment, maps to vanilla net.minecraft.util.EasingType
//A type is either one of the named simple curves or a cubic bezier; the codec writes the name or the control points
public abstract class EasingType
{
    private static readonly Dictionary<string, EasingType> SimpleRegistry = new();

    //Codec writes named curves as a string and cubic beziers as {"cubic_bezier":[x1,y1,x2,y2]}, maps to vanilla EasingType.CODEC
    public static readonly Codec<EasingType> Codec = new EasingTypeCodec();

    public static readonly EasingType ConstantType = RegisterSimple("constant", static _ => 0.0f);
    public static readonly EasingType Linear = RegisterSimple("linear", static x => x);
    public static readonly EasingType InBack = RegisterSimple("in_back", Ease.InBack);
    public static readonly EasingType InBounce = RegisterSimple("in_bounce", Ease.InBounce);
    public static readonly EasingType InCirc = RegisterSimple("in_circ", Ease.InCirc);
    public static readonly EasingType InCubic = RegisterSimple("in_cubic", Ease.InCubic);
    public static readonly EasingType InElastic = RegisterSimple("in_elastic", Ease.InElastic);
    public static readonly EasingType InExpo = RegisterSimple("in_expo", Ease.InExpo);
    public static readonly EasingType InQuad = RegisterSimple("in_quad", Ease.InQuad);
    public static readonly EasingType InQuart = RegisterSimple("in_quart", Ease.InQuart);
    public static readonly EasingType InQuint = RegisterSimple("in_quint", Ease.InQuint);
    public static readonly EasingType InSine = RegisterSimple("in_sine", Ease.InSine);
    public static readonly EasingType InOutBack = RegisterSimple("in_out_back", Ease.InOutBack);
    public static readonly EasingType InOutBounce = RegisterSimple("in_out_bounce", Ease.InOutBounce);
    public static readonly EasingType InOutCirc = RegisterSimple("in_out_circ", Ease.InOutCirc);
    public static readonly EasingType InOutCubic = RegisterSimple("in_out_cubic", Ease.InOutCubic);
    public static readonly EasingType InOutElastic = RegisterSimple("in_out_elastic", Ease.InOutElastic);
    public static readonly EasingType InOutExpo = RegisterSimple("in_out_expo", Ease.InOutExpo);
    public static readonly EasingType InOutQuad = RegisterSimple("in_out_quad", Ease.InOutQuad);
    public static readonly EasingType InOutQuart = RegisterSimple("in_out_quart", Ease.InOutQuart);
    public static readonly EasingType InOutQuint = RegisterSimple("in_out_quint", Ease.InOutQuint);
    public static readonly EasingType InOutSine = RegisterSimple("in_out_sine", Ease.InOutSine);
    public static readonly EasingType OutBack = RegisterSimple("out_back", Ease.OutBack);
    public static readonly EasingType OutBounce = RegisterSimple("out_bounce", Ease.OutBounce);
    public static readonly EasingType OutCirc = RegisterSimple("out_circ", Ease.OutCirc);
    public static readonly EasingType OutCubic = RegisterSimple("out_cubic", Ease.OutCubic);
    public static readonly EasingType OutElastic = RegisterSimple("out_elastic", Ease.OutElastic);
    public static readonly EasingType OutExpo = RegisterSimple("out_expo", Ease.OutExpo);
    public static readonly EasingType OutQuad = RegisterSimple("out_quad", Ease.OutQuad);
    public static readonly EasingType OutQuart = RegisterSimple("out_quart", Ease.OutQuart);
    public static readonly EasingType OutQuint = RegisterSimple("out_quint", Ease.OutQuint);
    public static readonly EasingType OutSine = RegisterSimple("out_sine", Ease.OutSine);

    public abstract float Apply(float alpha);

    //RegisterSimple adds a named curve to the codec registry, maps to vanilla EasingType.registerSimple
    public static EasingType RegisterSimple(string id, Func<float, float> easing)
    {
        var instance = new SimpleEasing(easing);
        SimpleRegistry[id] = instance;
        return instance;
    }

    public static EasingType CubicBezierCurve(float x1, float y1, float x2, float y2)
        => new CubicBezier(new CubicBezierControls(x1, y1, x2, y2));

    public static EasingType SymmetricCubicBezier(float x1, float y1)
        => CubicBezierCurve(x1, y1, 1.0f - x1, 1.0f - y1);

    internal static bool TryGetSimple(string id, out EasingType easing) => SimpleRegistry.TryGetValue(id, out easing!);

    internal static string? GetSimpleId(EasingType easing)
    {
        foreach (var (id, registered) in SimpleRegistry)
            if (ReferenceEquals(registered, easing)) return id;
        return null;
    }

    //SimpleEasing one of the named curves
    private sealed class SimpleEasing(Func<float, float> easing) : EasingType
    {
        public override float Apply(float alpha) => easing(alpha);
    }

    //CubicBezier a cubic bezier curve defined by two control points, maps to vanilla EasingType.CubicBezier
    public sealed class CubicBezier(CubicBezierControls controls) : EasingType
    {
        private const int NewtonRaphsonIterations = 4;
        private const float MaxStep = 0.25f;

        private readonly CubicCurve _xCurve = CurveFromControls(controls.X1, controls.X2);
        private readonly CubicCurve _yCurve = CurveFromControls(controls.Y1, controls.Y2);

        public CubicBezierControls Controls => controls;

        public override float Apply(float alpha) => _yCurve.Sample(SolveT(alpha));

        private static CubicCurve CurveFromControls(float v1, float v2)
            => new((3.0f * v1) - (3.0f * v2) + 1.0f, (-6.0f * v1) + (3.0f * v2), 3.0f * v1);

        private float SolveT(float x)
        {
            var t = x;
            for (var i = 0; i < NewtonRaphsonIterations; i++)
            {
                var error = _xCurve.Sample(t) - x;
                if (Math.Abs(error) < 1.0E-5f) return t;
                var gradient = _xCurve.SampleGradient(t);
                if (gradient < 1.0E-5f) break;
                t -= Mth.Clamp(error / gradient, -MaxStep, MaxStep);
            }
            return SolveTBisect(x, t);
        }

        private float SolveTBisect(float x, float initialT)
        {
            var low = 0.0f;
            var high = 1.0f;
            var t = initialT;
            while (low < high)
            {
                var error = _xCurve.Sample(t) - x;
                if (Math.Abs(error) < 1.0E-5f) return t;
                if (error < 0.0f) low = t;
                else high = t;
                t = (high + low) / 2.0f;
            }
            return t;
        }

        internal static DataResult<CubicBezier> Decode<U>(DynamicOps<U> ops, MapLike<U> map)
        {
            var raw = map.Get("cubic_bezier");
            if (!raw.IsPresent) return DataResult<CubicBezier>.Error(() => "Missing cubic_bezier");
            var stream = ops.GetStream(raw.Get());
            if (!stream.IsSuccess) return DataResult<CubicBezier>.Error(() => "cubic_bezier must be an array");
            var values = new List<float>();
            foreach (var element in stream.GetOrThrow())
            {
                var number = ops.GetNumberValue(element);
                if (!number.IsSuccess) return DataResult<CubicBezier>.Error(() => "cubic_bezier entries must be numbers");
                values.Add((float)number.GetOrThrow());
            }
            if (values.Count != 4) return DataResult<CubicBezier>.Error(() => "cubic_bezier must have 4 values");
            if (values[0] is < 0.0f or > 1.0f) return DataResult<CubicBezier>.Error(() => "x1 must be in range [0; 1]");
            if (values[2] is < 0.0f or > 1.0f) return DataResult<CubicBezier>.Error(() => "x2 must be in range [0; 1]");
            return DataResult<CubicBezier>.Success(new CubicBezier(new CubicBezierControls(values[0], values[1], values[2], values[3])));
        }

        internal DataResult<U> Encode<U>(DynamicOps<U> ops)
        {
            var points = ops.CreateList(new[]
            {
                ops.CreateFloat(controls.X1), ops.CreateFloat(controls.Y1),
                ops.CreateFloat(controls.X2), ops.CreateFloat(controls.Y2)
            });
            return DataResult<U>.Success(ops.CreateMap(new[]
            {
                Pair<U, U>.Of(ops.CreateString("cubic_bezier"), points)
            }));
        }

        //CubicCurve one axis of the bezier, evaluated as ((a * t + b) * t + c) * t
        private readonly record struct CubicCurve(float A, float B, float C)
        {
            public float Sample(float t) => (((A * t) + B) * t + C) * t;

            public float SampleGradient(float t) => (((3.0f * A) * t) + (2.0f * B)) * t + C;
        }
    }

    //CubicBezierControls the two control points; only x has to stay inside the unit range
    public sealed record CubicBezierControls(float X1, float Y1, float X2, float Y2);

    //EasingTypeCodec string for a named curve, object for a bezier
    private sealed class EasingTypeCodec : ScalarCodec<EasingType>
    {
        public override DataResult<EasingType> Parse<U>(DynamicOps<U> ops, U input)
        {
            var asString = ops.GetStringValue(input);
            if (asString.IsSuccess)
            {
                var name = asString.GetOrThrow();
                return TryGetSimple(name, out var easing)
                    ? DataResult<EasingType>.Success(easing)
                    : DataResult<EasingType>.Error(() => $"Unknown easing type: {name}");
            }
            return ops.GetMap(input).FlatMap(map => CubicBezier.Decode(ops, map).Map(bezier => (EasingType)bezier));
        }

        public override DataResult<U> EncodeStart<U>(DynamicOps<U> ops, EasingType value)
        {
            if (value is CubicBezier bezier) return bezier.Encode(ops);
            var id = GetSimpleId(value);
            return id is null
                ? DataResult<U>.Error(() => "Unknown easing type")
                : DataResult<U>.Success(ops.CreateString(id));
        }
    }
}
