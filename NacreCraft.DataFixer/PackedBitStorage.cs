using NacreCraft.Util;

namespace NacreCraft.DataFixer;

//PackedBitStorage packs fixed-width integers into a long array, maps to vanilla net.minecraft.util.datafix.PackedBitStorage
//Values may straddle two longs; the high bits land in the next entry
public sealed class PackedBitStorage
{
    private const int BitToLongShift = 6;

    private readonly long[] _data;
    private readonly int _bits;
    private readonly long _mask;
    private readonly int _size;

    public PackedBitStorage(int bits, int size)
        : this(bits, size, new long[Mth.RoundToward(size * bits, 64) / 64]) { }

    public PackedBitStorage(int bits, int size, long[] data)
    {
        if (bits < 1 || bits > 32)
            throw new ArgumentOutOfRangeException(nameof(bits), bits, "bits must be between 1 and 32 inclusive");
        _size = size;
        _bits = bits;
        _data = data;
        _mask = (1L << bits) - 1L;
        var requiredLength = Mth.RoundToward(size * bits, 64) / 64;
        if (data.Length != requiredLength)
            throw new ArgumentException($"Invalid length given for storage, got: {data.Length} but expected: {requiredLength}", nameof(data));
    }

    public void Set(int index, int value)
    {
        if (index < 0 || index >= _size)
            throw new ArgumentOutOfRangeException(nameof(index), index, $"index must be between 0 and {_size - 1} inclusive");
        if (value < 0 || value > _mask)
            throw new ArgumentOutOfRangeException(nameof(value), value, $"value must be between 0 and {_mask} inclusive");
        var position = index * _bits;
        var startData = position >> BitToLongShift;
        var endData = (index + 1) * _bits - 1 >> BitToLongShift;
        var startBit = position ^ startData << BitToLongShift;
        _data[startData] = _data[startData] & (_mask << startBit ^ -1L) | ((long)value & _mask) << startBit;
        if (startData == endData) return;
        var shiftBits = 64 - startBit;
        var wantedBits = _bits - shiftBits;
        _data[endData] = _data[endData] >>> wantedBits << wantedBits | ((long)value & _mask) >> shiftBits;
    }

    public int Get(int index)
    {
        if (index < 0 || index >= _size)
            throw new ArgumentOutOfRangeException(nameof(index), index, $"index must be between 0 and {_size - 1} inclusive");
        var position = index * _bits;
        var startData = position >> BitToLongShift;
        var endData = (index + 1) * _bits - 1 >> BitToLongShift;
        var startBit = position ^ startData << BitToLongShift;
        if (startData == endData)
            return (int)(_data[startData] >>> startBit & _mask);
        var shiftBits = 64 - startBit;
        return (int)((_data[startData] >>> startBit | _data[endData] << shiftBits) & _mask);
    }

    public long[] Raw => _data;

    public int Bits => _bits;
}
