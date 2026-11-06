using NacreCraft.Network;

namespace NacreCraft.Game.World.Pathfinder;

//Target one pathfinding goal cell, maps to vanilla net.minecraft.world.level.pathfinder.Target
public sealed class Target : Node
{
    private float _bestHeuristic = float.MaxValue;
    private Node? _bestNode;
    private bool _reached;

    public Target(Node node) : base(node.X, node.Y, node.Z)
    {
    }

    public Target(int x, int y, int z) : base(x, y, z)
    {
    }

    public void UpdateBest(float heuristic, Node node)
    {
        if (heuristic >= _bestHeuristic) return;
        _bestHeuristic = heuristic;
        _bestNode = node;
    }

    public Node? GetBestNode() => _bestNode;

    public void SetReached() => _reached = true;

    public bool IsReached() => _reached;

    public static Target CreateFromStream(FriendlyByteBuf buffer)
    {
        var node = new Target(buffer.ReadInt(), buffer.ReadInt(), buffer.ReadInt());
        ReadContents(buffer, node);
        return node;
    }
}
