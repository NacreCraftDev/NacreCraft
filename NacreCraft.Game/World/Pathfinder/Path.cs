using NacreCraft.Network;
using NacreCraft.Primitives;
using NacreCraft.Util;

namespace NacreCraft.Game.World.Pathfinder;

//Path one computed path, maps to vanilla net.minecraft.world.level.pathfinder.Path
//The node list is the waypoint chain from start to target; the debug data is only filled when debug capture is on
public sealed class Path
{
    //StreamCodec wire form, maps to vanilla STREAM_CODEC
    public static readonly StreamCodec<FriendlyByteBuf, Path> StreamCodec = new PathCodec();

    private readonly List<Node> _nodes;
    private DebugData? _debugData;
    private int _nextNodeIndex;
    private readonly BlockPos _target;
    private readonly float _distToTarget;
    private readonly bool _reached;

    public Path(List<Node> nodes, BlockPos target, bool reached)
    {
        _nodes = nodes;
        _target = target;
        _distToTarget = nodes.Count == 0 ? float.MaxValue : nodes[^1].DistanceManhattan(target);
        _reached = reached;
    }

    public void Advance() => _nextNodeIndex++;

    public bool NotStarted() => _nextNodeIndex <= 0;

    public bool IsDone() => _nextNodeIndex >= _nodes.Count;

    public Node? GetEndNode() => _nodes.Count > 0 ? _nodes[^1] : null;

    public Node GetNode(int i) => _nodes[i];

    public void TruncateNodes(int index)
    {
        if (_nodes.Count > index) _nodes.RemoveRange(index, _nodes.Count - index);
    }

    public void ReplaceNode(int index, Node replaceWith) => _nodes[index] = replaceWith;

    public int GetNodeCount() => _nodes.Count;

    public int GetNextNodeIndex() => _nextNodeIndex;

    public void SetNextNodeIndex(int nextNodeIndex) => _nextNodeIndex = nextNodeIndex;

    //GetEntityPosAtNode centers the node cell on the entity width, maps to vanilla getEntityPosAtNode
    public Vec3 GetEntityPosAtNode(NacreCraft.Registry.Entity entity, int index)
    {
        var node = _nodes[index];
        var center = Mth.Floor((float)entity.Width + 1.0f) * 0.5;
        return new Vec3(node.X + center, node.Y, node.Z + center);
    }

    public BlockPos GetNodePos(int index) => _nodes[index].AsBlockPos();

    public Vec3 GetNextEntityPos(NacreCraft.Registry.Entity entity) => GetEntityPosAtNode(entity, _nextNodeIndex);

    public BlockPos GetNextNodePos() => _nodes[_nextNodeIndex].AsBlockPos();

    public Node GetNextNode() => _nodes[_nextNodeIndex];

    public Node? GetPreviousNode() => _nextNodeIndex > 0 ? _nodes[_nextNodeIndex - 1] : null;

    public bool SameAs(Path? path) => path is not null && _nodes.SequenceEqual(path._nodes);

    public bool CanReach() => _reached;

    //SetDebug stores the open/closed sets and targets captured during the search, maps to vanilla setDebug
    public void SetDebug(Node[] openSet, Node[] closedSet, IReadOnlySet<NacreCraft.Game.World.Pathfinder.Target> targets)
        => _debugData = new DebugData(openSet, closedSet, targets);

    public DebugData? GetDebugData() => _debugData;

    public BlockPos Target => _target;

    public float DistToTarget => _distToTarget;

    //WriteToStream writes the path including its debug data, maps to vanilla writeToStream
    //A path without debug data cannot be serialized; the debug payload is only sent when capture is enabled, same as vanilla
    public void WriteToStream(FriendlyByteBuf buffer)
    {
        if (_debugData is null || _debugData.TargetNodes.Count == 0)
            throw new InvalidOperationException("Missing debug data");
        buffer.WriteBoolean(_reached);
        buffer.WriteInt(_nextNodeIndex);
        buffer.WriteBlockPos(_target);
        buffer.WriteVarInt(_nodes.Count);
        foreach (var node in _nodes) node.WriteToStream(buffer);
        _debugData.Write(buffer);
    }

    public static Path CreateFromStream(FriendlyByteBuf buffer)
    {
        var reached = buffer.ReadBoolean();
        var indexStream = buffer.ReadInt();
        var target = buffer.ReadBlockPos();
        var count = buffer.ReadVarInt();
        var nodes = new List<Node>(Math.Min(count, ByteBufCodecs.MaxInitialCollectionSize));
        for (var i = 0; i < count; i++) nodes.Add(Node.CreateFromStream(buffer));
        var debugData = DebugData.Read(buffer);
        return new Path(nodes, target, reached)
        {
            _debugData = debugData,
            _nextNodeIndex = indexStream
        };
    }

    //Copy shares the node list with the original, matching vanilla copy
    public Path Copy()
        => new(_nodes, _target, _reached)
        {
            _debugData = _debugData,
            _nextNodeIndex = _nextNodeIndex
        };

    private static Node[] ReadNodeArray(FriendlyByteBuf input)
    {
        var nodes = new Node[input.ReadVarInt()];
        for (var i = 0; i < nodes.Length; i++) nodes[i] = Node.CreateFromStream(input);
        return nodes;
    }

    private static void WriteNodeArray(FriendlyByteBuf output, Node[] nodes)
    {
        output.WriteVarInt(nodes.Length);
        foreach (var node in nodes) node.WriteToStream(output);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Path path) return false;
        return _nextNodeIndex == path._nextNodeIndex
               && ReferenceEquals(_debugData, path._debugData)
               && _reached == path._reached
               && _target.Equals(path._target)
               && _nodes.SequenceEqual(path._nodes);
    }

    public override int GetHashCode()
    {
        var hash = _nextNodeIndex;
        foreach (var node in _nodes) hash = hash * 31 + node.GetHashCode();
        return hash;
    }

    public override string ToString() => $"Path(length={_nodes.Count})";

    //DebugData the open/closed sets and targets captured for the pathfinding debug payload, maps to vanilla Path$DebugData
    //Target is fully qualified because the Path.Target property would otherwise shadow the node type inside the nested record
    public sealed record DebugData(Node[] OpenSet, Node[] ClosedSet,
        IReadOnlySet<NacreCraft.Game.World.Pathfinder.Target> TargetNodes)
    {
        public void Write(FriendlyByteBuf output)
        {
            output.WriteVarInt(TargetNodes.Count);
            foreach (var target in TargetNodes) target.WriteToStream(output);
            WriteNodeArray(output, OpenSet);
            WriteNodeArray(output, ClosedSet);
        }

        public static DebugData Read(FriendlyByteBuf input)
        {
            var targetCount = input.ReadVarInt();
            var targets = new HashSet<NacreCraft.Game.World.Pathfinder.Target>(targetCount);
            for (var i = 0; i < targetCount; i++)
                targets.Add(NacreCraft.Game.World.Pathfinder.Target.CreateFromStream(input));
            var openSet = ReadNodeArray(input);
            var closedSet = ReadNodeArray(input);
            return new DebugData(openSet, closedSet, targets);
        }
    }

    //PathCodec delegates to the write/read pair, maps to vanilla STREAM_CODEC
    private sealed class PathCodec : StreamCodec<FriendlyByteBuf, Path>
    {
        public Path Decode(FriendlyByteBuf buf) => CreateFromStream(buf);

        public void Encode(FriendlyByteBuf buf, Path value) => value.WriteToStream(buf);
    }
}
