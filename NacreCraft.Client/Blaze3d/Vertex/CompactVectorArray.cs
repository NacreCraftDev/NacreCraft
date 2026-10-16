using System.Numerics;

namespace NacreCraft.Client.Blaze3d.Vertex;

//CompactVectorArray a tightly packed array of 3D vectors, maps to vanilla CompactVectorArray
//Three floats per entry in one array, so a quad centroid list needs no per-element object
public sealed class CompactVectorArray
{
    private readonly float[] _values;

    public CompactVectorArray(int count)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        _values = new float[count * 3];
    }

    public int Count => _values.Length / 3;

    public void Set(int index, float x, float y, float z)
    {
        var offset = index * 3;
        _values[offset] = x;
        _values[offset + 1] = y;
        _values[offset + 2] = z;
    }

    public void Set(int index, Vector3 value) => Set(index, value.X, value.Y, value.Z);

    public Vector3 Get(int index)
    {
        var offset = index * 3;
        return new Vector3(_values[offset], _values[offset + 1], _values[offset + 2]);
    }

    public float GetX(int index) => _values[index * 3];
    public float GetY(int index) => _values[index * 3 + 1];
    public float GetZ(int index) => _values[index * 3 + 2];
}
