using NacreCraft.Network;
using NacreCraft.Primitives;
using NacreCraft.Util;

namespace NacreCraft.Game.World.Pathfinder;

//Node one cell in the path graph, maps to vanilla net.minecraft.world.level.pathfinder.Node
public class Node
{
    public readonly int X;
    public readonly int Y;
    public readonly int Z;
    private readonly int _hash;
    public int HeapIndex = -1;
    public float G;
    public float H;
    public float F;
    public Node? CameFrom;
    public bool Closed;
    public float WalkedDistance;
    public float CostMalus;
    public PathType Type = PathType.BLOCKED;

    public Node(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
        _hash = CreateHash(x, y, z);
    }

    //CloneAndMove copies the search state onto a node at new coordinates, maps to vanilla cloneAndMove
    public Node CloneAndMove(int x, int y, int z)
    {
        var node = new Node(x, y, z)
        {
            HeapIndex = HeapIndex,
            G = G,
            H = H,
            F = F,
            CameFrom = CameFrom,
            Closed = Closed,
            WalkedDistance = WalkedDistance,
            CostMalus = CostMalus,
            Type = Type
        };
        return node;
    }

    //CreateHash the vanilla packed hash, maps to vanilla createHash
    public static int CreateHash(int x, int y, int z)
        => (y & 0xFF) | ((x & short.MaxValue) << 8) | ((z & short.MaxValue) << 24)
           | (x < 0 ? int.MinValue : 0) | (z < 0 ? 32768 : 0);

    public float DistanceTo(Node to)
    {
        var xd = to.X - X;
        var yd = to.Y - Y;
        var zd = to.Z - Z;
        return Mth.Sqrt(xd * xd + yd * yd + zd * zd);
    }

    public float DistanceToXZ(Node to)
    {
        var xd = to.X - X;
        var zd = to.Z - Z;
        return Mth.Sqrt(xd * xd + zd * zd);
    }

    public float DistanceTo(BlockPos pos)
    {
        var xd = pos.X - X;
        var yd = pos.Y - Y;
        var zd = pos.Z - Z;
        return Mth.Sqrt(xd * xd + yd * yd + zd * zd);
    }

    public float DistanceToSqr(Node to)
    {
        var xd = to.X - X;
        var yd = to.Y - Y;
        var zd = to.Z - Z;
        return xd * xd + yd * yd + zd * zd;
    }

    public float DistanceToSqr(BlockPos pos)
    {
        var xd = pos.X - X;
        var yd = pos.Y - Y;
        var zd = pos.Z - Z;
        return xd * xd + yd * yd + zd * zd;
    }

    public float DistanceManhattan(Node to)
        => Math.Abs(to.X - X) + Math.Abs(to.Y - Y) + Math.Abs(to.Z - Z);

    public float DistanceManhattan(BlockPos pos)
        => Math.Abs(pos.X - X) + Math.Abs(pos.Y - Y) + Math.Abs(pos.Z - Z);

    public BlockPos AsBlockPos() => new(X, Y, Z);

    public Vec3 AsVec3() => new(X, Y, Z);

    public bool InOpenSet() => HeapIndex >= 0;

    public void WriteToStream(FriendlyByteBuf buffer)
    {
        buffer.WriteInt(X);
        buffer.WriteInt(Y);
        buffer.WriteInt(Z);
        buffer.WriteFloat(WalkedDistance);
        buffer.WriteFloat(CostMalus);
        buffer.WriteBoolean(Closed);
        buffer.WriteVarInt((int)Type);
        buffer.WriteFloat(F);
    }

    public static Node CreateFromStream(FriendlyByteBuf buffer)
    {
        var node = new Node(buffer.ReadInt(), buffer.ReadInt(), buffer.ReadInt());
        ReadContents(buffer, node);
        return node;
    }

    protected static void ReadContents(FriendlyByteBuf buffer, Node node)
    {
        node.WalkedDistance = buffer.ReadFloat();
        node.CostMalus = buffer.ReadFloat();
        node.Closed = buffer.ReadBoolean();
        node.Type = (PathType)buffer.ReadVarInt();
        node.F = buffer.ReadFloat();
    }

    public override bool Equals(object? obj)
        => obj is Node node && _hash == node._hash && X == node.X && Y == node.Y && Z == node.Z;

    public override int GetHashCode() => _hash;

    public override string ToString() => $"Node{{x={X}, y={Y}, z={Z}}}";
}
