using System.Numerics;
using NacreCraft.Util;

namespace NacreCraft.Storage.Light;

//SpatialLongSet linked set of block positions compressed into 4x4x4 bit masks, maps to vanilla net.minecraft.world.level.lighting.SpatialLongSet
//Each outer key owns one long and every set bit is one inner position; iteration order is insertion order
public sealed class SpatialLongSet
{
    private readonly InternalMap _map;

    public SpatialLongSet(int expected, float loadFactor) => _map = new InternalMap(expected / 64);

    public bool Add(long value) => _map.AddBit(value);

    public bool Remove(long value) => _map.RemoveBit(value);

    public long RemoveFirstLong() => _map.RemoveFirstBit();

    //The logical size is the popcount of every mask, so vanilla refuses the query
    public int Size => throw new NotSupportedException("SpatialLongSet does not support size()");

    public bool IsEmpty => _map.IsEmpty;

    //InternalMap outer key to bit mask, maps to the linked hash map vanilla builds on
    private sealed class InternalMap
    {
        private static readonly int XBits = Mth.Log2(60000000);
        private static readonly int ZBits = Mth.Log2(60000000);
        private static readonly int YBits = 64 - XBits - ZBits;
        private static readonly int ZOffset = YBits;
        private static readonly int XOffset = YBits + ZBits;
        private static readonly long OuterMask = 3L << XOffset | 3L | 3L << ZOffset;

        private readonly Dictionary<long, long> _masks = new();
        private readonly LinkedList<long> _order = new();
        private readonly Dictionary<long, LinkedListNode<long>> _nodes = new();

        public bool IsEmpty => _masks.Count == 0;

        public bool AddBit(long key)
        {
            var outerKey = GetOuterKey(key);
            var bitMask = 1L << GetInnerKey(key);
            if (_masks.TryGetValue(outerKey, out var mask))
            {
                var alreadySet = (mask & bitMask) != 0;
                _masks[outerKey] = mask | bitMask;
                return alreadySet;
            }
            _masks[outerKey] = bitMask;
            _nodes[outerKey] = _order.AddLast(outerKey);
            return false;
        }

        public bool RemoveBit(long key)
        {
            var outerKey = GetOuterKey(key);
            var bitMask = 1L << GetInnerKey(key);
            if (!_masks.TryGetValue(outerKey, out var mask)) return false;
            if ((mask & bitMask) == 0) return false;
            var newMask = mask & (bitMask ^ -1L);
            if (newMask == 0) RemoveEntry(outerKey);
            else _masks[outerKey] = newMask;
            return true;
        }

        public long RemoveFirstBit()
        {
            var first = _order.First ?? throw new InvalidOperationException("No such element");
            var outerKey = first.Value;
            var mask = _masks[outerKey];
            var innerKey = BitOperations.TrailingZeroCount(mask);
            var newMask = mask & (1L << innerKey ^ -1L);
            if (newMask == 0) RemoveEntry(outerKey);
            else _masks[outerKey] = newMask;
            return GetFullKey(outerKey, innerKey);
        }

        private void RemoveEntry(long outerKey)
        {
            _masks.Remove(outerKey);
            if (_nodes.Remove(outerKey, out var node)) _order.Remove(node);
        }

        private static long GetOuterKey(long key) => key & (OuterMask ^ -1L);

        private static int GetInnerKey(long key)
        {
            var innerX = (int)(key >>> XOffset & 3L);
            var innerY = (int)(key & 3L);
            var innerZ = (int)(key >>> ZOffset & 3L);
            return innerX << 4 | innerZ << 2 | innerY;
        }

        private static long GetFullKey(long outerKey, int innerKey)
        {
            outerKey |= (long)(innerKey >> 4 & 3) << XOffset;
            outerKey |= (long)(innerKey >> 2 & 3) << ZOffset;
            return outerKey | (long)(innerKey & 3);
        }
    }
}
