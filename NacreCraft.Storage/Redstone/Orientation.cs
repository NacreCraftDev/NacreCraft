using NacreCraft.Primitives;
using NacreCraft.Util.Collection;
using NacreCraft.Util.Random;

namespace NacreCraft.Storage.Redstone;

//Orientation the redstone orientation frame, maps to vanilla net.minecraft.world.level.redstone.Orientation
//Up/front/sideBias select one of 48 precomputed frames; the frame fixes the neighbor direction order
public sealed class Orientation
{
    private static readonly Orientation[] Orientations = BuildOrientations();
    private readonly Dictionary<Direction, Orientation> _withFront = new();
    private readonly Dictionary<Direction, Orientation> _withUp = new();
    private readonly Dictionary<SideBias, Orientation> _withSideBias = new();

    private Orientation(Direction up, Direction front, SideBias sideBias)
    {
        Up = up;
        Front = front;
        SideBiasValue = sideBias;
        Index = GenerateIndex(up, front, sideBias);
        var rightVector = front.UnitVec3i.Cross(up.UnitVec3i);
        var side = Direction.GetNearest(rightVector);
        Side = sideBias == SideBias.RIGHT ? side : side.Opposite;
        Neighbors = new List<Direction>
        {
            Front.Opposite, Front, Side, Side.Opposite, Up.Opposite, Up
        };
        HorizontalNeighbors = Neighbors.Where(d => d.AxisValue != Up.AxisValue).ToList();
        VerticalNeighbors = Neighbors.Where(d => d.AxisValue == Up.AxisValue).ToList();
    }

    public Direction Up { get; }

    public Direction Front { get; }

    public Direction Side { get; }

    public SideBias SideBiasValue { get; }

    public int Index { get; }

    public IReadOnlyList<Direction> Neighbors { get; }

    public IReadOnlyList<Direction> HorizontalNeighbors { get; }

    public IReadOnlyList<Direction> VerticalNeighbors { get; }

    //Of the precomputed orientation for the frame, maps to vanilla of
    public static Orientation Of(Direction up, Direction front, SideBias sideBias)
        => Orientations[GenerateIndex(up, front, sideBias)];

    public Orientation WithUp(Direction up) => _withUp[up];

    public Orientation WithFront(Direction front) => _withFront[front];

    //WithFrontPreserveUp keeps the current frame when the new front shares the up axis, maps to vanilla withFrontPreserveUp
    public Orientation WithFrontPreserveUp(Direction front)
        => front.AxisValue == Up.AxisValue ? this : _withFront[front];

    public Orientation WithFrontAdjustSideBias(Direction front)
    {
        var withFront = WithFront(front);
        return Front == withFront.Side ? withFront.WithMirror() : withFront;
    }

    public Orientation WithSideBias(SideBias sideBias) => _withSideBias[sideBias];

    public Orientation WithMirror() => WithSideBias(SideBiasValue.GetOpposite());

    public static Orientation FromIndex(int index) => Orientations[index];

    public static Orientation Random(RandomSource rand) => RandomCollections.GetRandom(Orientations, rand);

    public override string ToString() => $"[up={Up},front={Front},sideBias={SideBiasValue}]";

    private static Orientation[] BuildOrientations()
    {
        var orientations = new Orientation[48];
        GenerateContext(new Orientation(Direction.Up, Direction.North, SideBias.LEFT), orientations);
        return orientations;
    }

    private static Orientation GenerateContext(Orientation self, Orientation[] lookup)
    {
        if (lookup[self.Index] is not null) return lookup[self.Index];
        lookup[self.Index] = self;
        foreach (var sideBias in Enum.GetValues<SideBias>())
            self._withSideBias[sideBias] = GenerateContext(
                new Orientation(self.Up, self.Front, sideBias), lookup);
        foreach (var direction in Direction.Values)
        {
            var up = self.Up;
            if (direction == self.Up) up = self.Front.Opposite;
            if (direction == self.Up.Opposite) up = self.Front;
            self._withFront[direction] = GenerateContext(
                new Orientation(up, direction, self.SideBiasValue), lookup);
        }
        foreach (var direction in Direction.Values)
        {
            var front = self.Front;
            if (direction == self.Front) front = self.Up.Opposite;
            if (direction == self.Front.Opposite) front = self.Up;
            self._withUp[direction] = GenerateContext(
                new Orientation(direction, front, self.SideBiasValue), lookup);
        }
        return self;
    }

    //GenerateIndex the packing of up/front/sideBias into 0..47, maps to vanilla generateIndex
    private static int GenerateIndex(Direction up, Direction front, SideBias sideBias)
    {
        if (up.AxisValue == front.AxisValue)
            throw new InvalidOperationException("Up-vector and front-vector can not be on the same axis");
        var frontAxisKey = up.AxisValue == Direction.Axis.Y
            ? (front.AxisValue == Direction.Axis.X ? 1 : 0)
            : (front.AxisValue == Direction.Axis.Y ? 1 : 0);
        var frontKey = (frontAxisKey << 1) | (int)front.AxisDir;
        return (((up.Id3D << 2) + frontKey) << 1) + (int)sideBias;
    }

    public enum SideBias
    {
        LEFT,
        RIGHT
    }
}

public static class SideBiasExtensions
{
    public static Orientation.SideBias GetOpposite(this Orientation.SideBias sideBias)
        => sideBias == Orientation.SideBias.LEFT ? Orientation.SideBias.RIGHT : Orientation.SideBias.LEFT;

    public static string GetName(this Orientation.SideBias sideBias)
        => sideBias == Orientation.SideBias.LEFT ? "left" : "right";
}
