using System.Numerics;
using NacreCraft.Client.Blaze3d.Vertex;

namespace NacreCraft.Client.Blaze3d;

//ProjectionType the two camera projections, maps to vanilla com.mojang.blaze3d.ProjectionType
public enum ProjectionType
{
    Perspective,
    Orthographic
}

//ProjectionTypes carries the data the enum cannot hold, maps to the vanilla enum fields
public static class ProjectionTypes
{
    //Sorting the vertex sorting this projection pairs with
    //Named Sorting rather than VertexSorting so the method cannot shadow the type it returns
    public static VertexSorting Sorting(this ProjectionType type)
        => type == ProjectionType.Perspective ? VertexSorting.DistanceToOrigin : VertexSorting.OrthographicZ;

    //ApplyLayeringTransform shifts the projection matrix by a depth bias, maps to vanilla applyLayeringTransform
    //System.Numerics has no mutating matrix operations, so the transform has to come back through the ref parameter
    public static void ApplyLayeringTransform(this ProjectionType type, ref Matrix4x4 matrix, float bias)
    {
        if (type == ProjectionType.Perspective)
            matrix *= Matrix4x4.CreateScale(1f - bias / 4096f);
        else
            matrix *= Matrix4x4.CreateTranslation(0f, 0f, bias / 512f);
    }
}
