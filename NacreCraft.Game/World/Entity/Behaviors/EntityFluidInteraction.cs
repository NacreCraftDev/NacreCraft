using NacreCraft.Game.World.Level;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Registry.State;
using NacreCraft.Storage;
using NacreCraft.Util;

namespace NacreCraft.Game.World.Entity;

//EntityFluidInteraction tracks how deep an entity is in each tracked fluid and applies the fluid current, maps to vanilla net.minecraft.world.entity.EntityFluidInteraction
//NC's FluidState has no flow vector yet, so the current is zero and only the height/eye flags are computed; the loaded-chunk precheck is omitted
public sealed class EntityFluidInteraction
{
    private readonly Dictionary<TagKey<Fluid>, Tracker> _trackerByFluid = new();

    public EntityFluidInteraction(IReadOnlySet<TagKey<Fluid>> fluids)
    {
        foreach (var fluid in fluids) _trackerByFluid[fluid] = new Tracker();
    }

    //Update scans the entity's box and updates each tracker, maps to vanilla update
    public void Update(ServerLevel level, NacreCraft.Registry.Entity entity, bool ignoreCurrent)
    {
        foreach (var tracker in _trackerByFluid.Values) tracker.Reset();
        var box = entity.BoundingBox;
        var x0 = Mth.Floor(box.Min.X);
        var y0 = Mth.Floor(box.Min.Y);
        var z0 = Mth.Floor(box.Min.Z);
        var x1 = Mth.Ceil(box.Max.X) - 1;
        var y1 = Mth.Ceil(box.Max.Y) - 1;
        var z1 = Mth.Ceil(box.Max.Z) - 1;
        var entityY = box.Min.Y;
        var eyeBlockX = Mth.Floor(entity.Pos.X);
        var eyeBlockZ = Mth.Floor(entity.Pos.Z);
        var eyeY = entity.GetEyePosition().Y;
        for (var x = x0; x <= x1; x++)
        for (var y = y0; y <= y1; y++)
        for (var z = z0; z <= z1; z++)
        {
            var pos = new BlockPos(x, y, z);
            var fluidState = level.GetFluidState(pos);
            if (fluidState.IsEmpty) continue;
            var fluidBottom = pos.Y;
            var fluidTop = fluidBottom + fluidState.OwnHeight;
            if (fluidTop < box.Min.Y) continue;
            var tracker = GetTrackerFor(fluidState);
            if (tracker is null) continue;
            if (x == eyeBlockX && z == eyeBlockZ && eyeY >= fluidBottom && eyeY <= fluidTop)
                tracker.EyesInside = true;
            tracker.Height = Math.Max(fluidTop - entityY, tracker.Height);
            //The flow vector is not available; ignoreCurrent only skips a current that would be zero anyway
        }
    }

    private Tracker? GetTrackerFor(FluidState fluidState)
    {
        foreach (var (tag, tracker) in _trackerByFluid)
        {
            if (tag == FluidTags.Water && fluidState.IsWater) return tracker;
            if (tag == FluidTags.Lava && fluidState.IsLava) return tracker;
        }
        return null;
    }

    public void ApplyCurrentTo(TagKey<Fluid> fluid, NacreCraft.Registry.Entity entity, double scale)
    {
        //Current is zero in NC, so there is nothing to add
        _ = fluid;
        _ = entity;
        _ = scale;
    }

    public double GetFluidHeight(TagKey<Fluid> fluid)
        => _trackerByFluid.TryGetValue(fluid, out var tracker) ? tracker.Height : 0;

    public bool IsInFluid(TagKey<Fluid> fluid) => GetFluidHeight(fluid) > 0;

    public bool IsEyeInFluid(TagKey<Fluid> fluid)
        => _trackerByFluid.TryGetValue(fluid, out var tracker) && tracker.EyesInside;

    private sealed class Tracker
    {
        public double Height;
        public bool EyesInside;

        public void Reset()
        {
            Height = 0;
            EyesInside = false;
        }
    }
}
