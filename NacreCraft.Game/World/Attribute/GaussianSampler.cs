using NacreCraft.Primitives;
using NacreCraft.Util;

namespace NacreCraft.Game.World.Attribute;

//GaussianSampler trilinear sample weighting over a 6x6x6 neighbourhood, maps to vanilla net.minecraft.world.attribute.GaussianSampler
//The kernel is applied separately per axis and multiplied, so the weight is 0 for the outer shell and 6 at the centre
public static class GaussianSampler
{
    private const int SampleRadius = 2;
    private const int SampleBreadth = 6;

    private static readonly double[] Kernel = { 0.0, 1.0, 4.0, 6.0, 4.0, 1.0, 0.0 };

    //Sample walks the neighbourhood and reports every weighted sample, maps to vanilla GaussianSampler.sample
    public static void Sample<T>(Vec3 position, Func<int, int, int, T> sampler, Action<double, T> accumulator)
    {
        position = position.Subtract(new Vec3(0.5, 0.5, 0.5));
        var integralX = Mth.Floor(position.X);
        var integralY = Mth.Floor(position.Y);
        var integralZ = Mth.Floor(position.Z);
        var relativeX = position.X - integralX;
        var relativeY = position.Y - integralY;
        var relativeZ = position.Z - integralZ;
        for (var z = 0; z < SampleBreadth; z++)
        {
            var weightZ = Mth.Lerp(relativeZ, Kernel[z + 1], Kernel[z]);
            var sampleZ = integralZ - SampleRadius + z;
            for (var x = 0; x < SampleBreadth; x++)
            {
                var weightX = Mth.Lerp(relativeX, Kernel[x + 1], Kernel[x]);
                var sampleX = integralX - SampleRadius + x;
                for (var y = 0; y < SampleBreadth; y++)
                {
                    var weightY = Mth.Lerp(relativeY, Kernel[y + 1], Kernel[y]);
                    var sampleY = integralY - SampleRadius + y;
                    accumulator(weightX * weightY * weightZ, sampler(sampleX, sampleY, sampleZ));
                }
            }
        }
    }
}
