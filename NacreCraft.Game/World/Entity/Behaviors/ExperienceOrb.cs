using NacreCraft.Game.Server;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Entity;

//ExperienceOrb an experience orb, maps to vanilla net.minecraft.world.entity.ExperienceOrb
//NC has no orb rendering and no level-based XP merge; the orb is picked up by the nearest player within one block and adds its
//value straight to the player's total experience
public sealed class ExperienceOrb : NacreCraft.Registry.Entity
{
    private const int MaxAge = 6000;

    public ExperienceOrb()
    {
        Age = 0;
        Value = 1;
    }

    public ExperienceOrb(int value) : this() => Value = value;

    public override Identifier Id => Identifier.WithDefaultNamespace("experience_orb");
    public override bool CanBeHitByProjectile => false;
    public override bool IsPickable => false;

    public int Value { get; set; }
    public int Age { get; private set; }

    public override void Tick()
    {
        TickBase();
        Age++;
        if (Age >= MaxAge)
        {
            Discard();
            return;
        }
        if (Level is not ServerLevel level) return;
        var box = new AABB(Pos.X - 1, Pos.Y - 1, Pos.Z - 1, Pos.X + 1, Pos.Y + 1, Pos.Z + 1);
        foreach (var player in level.GetEntitiesOfClass<ServerPlayer>(box))
        {
            if (player.IsDeadOrDying) continue;
            player.XpTotal += Value;
            Discard();
            return;
        }
    }
}
