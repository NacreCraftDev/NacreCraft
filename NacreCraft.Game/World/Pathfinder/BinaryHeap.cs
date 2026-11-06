namespace NacreCraft.Game.World.Pathfinder;

//BinaryHeap the open set min-heap, maps to vanilla net.minecraft.world.level.pathfinder.BinaryHeap
public sealed class BinaryHeap
{
    private Node?[] _heap = new Node[128];
    private int _size;

    public Node Insert(Node node)
    {
        if (node.HeapIndex >= 0) throw new InvalidOperationException("OW KNOWS!");
        if (_size == _heap.Length)
        {
            var newHeap = new Node?[_size << 1];
            Array.Copy(_heap, 0, newHeap, 0, _size);
            _heap = newHeap;
        }
        _heap[_size] = node;
        node.HeapIndex = _size;
        UpHeap(_size++);
        return node;
    }

    public void Clear() => _size = 0;

    public Node Peek() => _heap[0]!;

    public Node Pop()
    {
        var popped = _heap[0]!;
        _heap[0] = _heap[--_size];
        _heap[_size] = null;
        if (_size > 0) DownHeap(0);
        popped.HeapIndex = -1;
        return popped;
    }

    public void Remove(Node node)
    {
        _heap[node.HeapIndex] = _heap[--_size];
        _heap[_size] = null;
        if (_size > node.HeapIndex)
        {
            if (_heap[node.HeapIndex]!.F < node.F) UpHeap(node.HeapIndex);
            else DownHeap(node.HeapIndex);
        }
        node.HeapIndex = -1;
    }

    public void ChangeCost(Node node, float newCost)
    {
        var oldCost = node.F;
        node.F = newCost;
        if (newCost < oldCost) UpHeap(node.HeapIndex);
        else DownHeap(node.HeapIndex);
    }

    public int Size() => _size;

    public bool IsEmpty() => _size == 0;

    public Node[] GetHeap() => _heap.Take(_size).OfType<Node>().ToArray();

    private void UpHeap(int index)
    {
        var node = _heap[index]!;
        var cost = node.F;
        while (index > 0)
        {
            var parentIndex = (index - 1) >> 1;
            var parent = _heap[parentIndex]!;
            if (cost >= parent.F) break;
            _heap[index] = parent;
            parent.HeapIndex = index;
            index = parentIndex;
        }
        _heap[index] = node;
        node.HeapIndex = index;
    }

    private void DownHeap(int index)
    {
        var node = _heap[index]!;
        var cost = node.F;
        while (true)
        {
            var leftIndex = 1 + (index << 1);
            var rightIndex = leftIndex + 1;
            if (leftIndex >= _size) break;
            var leftNode = _heap[leftIndex]!;
            var leftCost = leftNode.F;
            Node? rightNode;
            float rightCost;
            if (rightIndex >= _size)
            {
                rightNode = null;
                rightCost = float.PositiveInfinity;
            }
            else
            {
                rightNode = _heap[rightIndex];
                rightCost = rightNode!.F;
            }
            if (leftCost < rightCost)
            {
                if (leftCost >= cost) break;
                _heap[index] = leftNode;
                leftNode.HeapIndex = index;
                index = leftIndex;
                continue;
            }
            if (rightCost >= cost) break;
            _heap[index] = rightNode;
            rightNode!.HeapIndex = index;
            index = rightIndex;
        }
        _heap[index] = node;
        node.HeapIndex = index;
    }
}
