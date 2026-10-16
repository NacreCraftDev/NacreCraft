using System.Numerics;

namespace NacreCraft.Client.Blaze3d.Vertex;

//VertexSorting orders quads by a key derived from their centroid, maps to vanilla VertexSorting
//Sort returns quad indices in submission order; the caller turns that into a sorted index buffer
public sealed class VertexSorting
{
    //DISTANCE_TO_ORIGIN the perspective default, maps to vanilla DISTANCE_TO_ORIGIN
    public static readonly VertexSorting DistanceToOrigin = ByDistance(0f, 0f, 0f);

    //ORTHOGRAPHIC_Z the orthographic default, maps to vanilla ORTHOGRAPHIC_Z
    public static readonly VertexSorting OrthographicZ = ByFunction(v => -v.Z);

    private readonly Func<Vector3, float> _keyFunction;

    private VertexSorting(Func<Vector3, float> keyFunction) => _keyFunction = keyFunction;

    public static VertexSorting ByDistance(float x, float y, float z) => ByDistance(new Vector3(x, y, z));

    public static VertexSorting ByDistance(Vector3 origin)
        => ByFunction(v => Vector3.DistanceSquared(v, origin));

    //ByFunction sorts by an arbitrary key, maps to vanilla byDistance(DistanceFunction)
    public static VertexSorting ByFunction(Func<Vector3, float> keyFunction) => new(keyFunction);

    //Sort returns quad indices ordered by descending key so far quads are submitted first
    //Ties fall back to the original index, which keeps the order deterministic the way vanilla's stable merge sort does
    public int[] Sort(CompactVectorArray values)
    {
        var count = values.Count;
        var indices = new int[count];
        for (var i = 0; i < count; i++) indices[i] = i;
        Array.Sort(indices, (a, b) =>
        {
            var comparison = _keyFunction(values.Get(b)).CompareTo(_keyFunction(values.Get(a)));
            return comparison != 0 ? comparison : a.CompareTo(b);
        });
        return indices;
    }
}
