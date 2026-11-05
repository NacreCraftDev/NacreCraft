using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Level;

//PotentialCalculator inverse-square spawn potential, maps to vanilla net.minecraft.world.level.PotentialCalculator
//Mob spawn costs accumulate charges and the energy change decides whether one more mob may spawn
public sealed class PotentialCalculator
{
    private readonly List<PointCharge> _charges = new();

    //PointCharge one charged position, maps to vanilla PotentialCalculator.PointCharge
    private sealed class PointCharge
    {
        private readonly BlockPos _pos;
        private readonly double _charge;

        public PointCharge(BlockPos pos, double charge)
        {
            _pos = pos;
            _charge = charge;
        }

        public double GetPotentialChange(BlockPos pos)
        {
            var dx = _pos.X - pos.X;
            var dy = _pos.Y - pos.Y;
            var dz = _pos.Z - pos.Z;
            var distanceSqr = (double)(dx * dx + dy * dy + dz * dz);
            return distanceSqr == 0.0 ? double.PositiveInfinity : _charge / Math.Sqrt(distanceSqr);
        }
    }

    public void AddCharge(BlockPos pos, double charge)
    {
        if (charge != 0.0) _charges.Add(new PointCharge(pos, charge));
    }

    //GetPotentialEnergyChange total potential change for a charge placed at pos, maps to vanilla getPotentialEnergyChange
    public double GetPotentialEnergyChange(BlockPos pos, double charge)
    {
        if (charge == 0.0) return 0.0;
        var potentialChange = 0.0;
        foreach (var point in _charges)
            potentialChange += point.GetPotentialChange(pos);
        return potentialChange * charge;
    }
}
