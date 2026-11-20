using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Entity;

//Leashable an entity that can be tied to a leash holder, maps to vanilla net.minecraft.world.entity.Leashable
//The holder state lives on Entity; the elastic physics helpers are reduced to the snap/elastic thresholds because NC has no
//leash rope simulation yet
public interface Leashable
{
    const string LeashTag = "leash";
    const double LeashTooFarDist = 12.0;
    const double LeashElasticDist = 6.0;
    const double MaximumAllowedLeashedDist = 16.0;

    NacreCraft.Registry.Entity? GetLeashHolder()
        => this is NacreCraft.Registry.Entity entity ? entity.GetLeashHolder() : null;

    void SetLeashedTo(NacreCraft.Registry.Entity holder, bool synchronize = true)
    {
        if (this is NacreCraft.Registry.Entity entity) entity.SetLeashedTo(holder, synchronize);
    }

    void DropLeash()
    {
        if (this is NacreCraft.Registry.Entity entity) entity.DropLeash();
    }

    bool IsLeashed() => GetLeashHolder() is not null;

    bool MayBeLeashed() => true;

    bool CanBeLeashed() => !IsLeashed() && MayBeLeashed();

    bool CanHaveALeashAttachedTo(NacreCraft.Registry.Entity entity) => CanBeLeashed();

    double LeashDistanceTo(NacreCraft.Registry.Entity entity)
        => this is NacreCraft.Registry.Entity self ? self.Pos.DistanceTo(entity.Pos) : 0;

    double LeashElasticDistance() => LeashElasticDist;

    double LeashSnapDistance() => LeashTooFarDist;

    void WhenLeashedTo(NacreCraft.Registry.Entity leashHolder)
    {
    }

    void OnLeashRemoved()
    {
    }

    void OnElasticLeashPull()
    {
    }

    void LeashTooFarBehaviour()
    {
        DropLeash();
        OnLeashRemoved();
    }

    void CloseRangeLeashBehaviour(NacreCraft.Registry.Entity leashHolder)
    {
    }

    bool SupportQuadLeash() => false;

    Vec3 GetLeashOffset() => Vec3.Zero;

    //TickLeash checks the distance thresholds, maps to vanilla tickLeash (without the elastic force simulation)
    static void TickLeash(NacreCraft.Storage.ServerLevel level, NacreCraft.Registry.Entity entity)
    {
        if (entity is not Leashable leashable) return;
        var holder = leashable.GetLeashHolder();
        if (holder is null) return;
        var distance = leashable.LeashDistanceTo(holder);
        if (distance > MaximumAllowedLeashedDist)
        {
            leashable.LeashTooFarBehaviour();
        }
        else if (distance > leashable.LeashSnapDistance())
        {
            leashable.OnElasticLeashPull();
        }
        else if (distance > leashable.LeashElasticDistance())
        {
            leashable.CloseRangeLeashBehaviour(holder);
        }
    }
}
