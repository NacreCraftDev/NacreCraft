using System.Numerics;

namespace NacreCraft.Client.Animation;

//KeyframeAnimations helpers for building animation constants, maps to vanilla net.minecraft.client.animation.KeyframeAnimations
public static class KeyframeAnimations
{
    private const float DegreesToRadians = MathF.PI / 180f;

    //PosVec model space offset, Y is inverted because model Y points down
    public static Vector3 PosVec(float x, float y, float z) => new(x, -y, z);

    public static Vector3 DegreeVec(float x, float y, float z)
        => new(x * DegreesToRadians, y * DegreesToRadians, z * DegreesToRadians);

    //ScaleVec converts a scale multiplier into the offset the part adds on top of 1
    public static Vector3 ScaleVec(double x, double y, double z)
        => new((float)(x - 1.0), (float)(y - 1.0), (float)(z - 1.0));
}
