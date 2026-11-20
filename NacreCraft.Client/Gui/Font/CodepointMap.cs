namespace NacreCraft.Client.Gui.Font;

//CodepointMap a sparse codepoint-indexed map split into 256-entry blocks, maps to vanilla net.minecraft.client.gui.font.CodepointMap
public sealed class CodepointMap<T> where T : class
{
    private const int BlockBits = 8;
    private const int BlockSize = 1 << BlockBits;
    private const int InBlockMask = BlockSize - 1;
    private const int BlockCount = 4352;

    private readonly T?[] _empty = new T?[BlockSize];
    private readonly T?[][] _blockMap;
    private readonly Func<int, T?[]> _blockConstructor;

    public CodepointMap(Func<int, T?[]> blockConstructor)
    {
        _blockConstructor = blockConstructor;
        _blockMap = new T?[BlockCount][];
        Array.Fill(_blockMap, _empty);
    }

    public void Clear() => Array.Fill(_blockMap, _empty);

    public T? Get(int codepoint) => _blockMap[codepoint >> BlockBits][codepoint & InBlockMask];

    public T? Put(int codepoint, T value)
    {
        var block = codepoint >> BlockBits;
        var offset = codepoint & InBlockMask;
        var blockData = _blockMap[block];
        if (ReferenceEquals(blockData, _empty))
        {
            blockData = _blockConstructor(BlockSize);
            _blockMap[block] = blockData;
        }

        var previous = blockData[offset];
        blockData[offset] = value;
        return previous;
    }

    public T ComputeIfAbsent(int codepoint, Func<int, T> mapper)
    {
        var block = codepoint >> BlockBits;
        var offset = codepoint & InBlockMask;
        var blockData = _blockMap[block];
        var current = blockData[offset];
        if (current is not null) return current;

        if (ReferenceEquals(blockData, _empty))
        {
            blockData = _blockConstructor(BlockSize);
            _blockMap[block] = blockData;
        }

        var result = mapper(codepoint);
        blockData[offset] = result;
        return result;
    }

    public T? Remove(int codepoint)
    {
        var block = codepoint >> BlockBits;
        var blockData = _blockMap[block];
        if (ReferenceEquals(blockData, _empty)) return null;

        var offset = codepoint & InBlockMask;
        var previous = blockData[offset];
        blockData[offset] = null;
        return previous;
    }

    public void ForEach(Action<int, T> output)
    {
        for (var block = 0; block < _blockMap.Length; block++)
        {
            var blockData = _blockMap[block];
            if (ReferenceEquals(blockData, _empty)) continue;
            for (var offset = 0; offset < blockData.Length; offset++)
            {
                var value = blockData[offset];
                if (value is not null) output(block << BlockBits | offset, value);
            }
        }
    }

    public IReadOnlySet<int> KeySet()
    {
        var result = new HashSet<int>();
        ForEach((codepoint, _) => result.Add(codepoint));
        return result;
    }
}
