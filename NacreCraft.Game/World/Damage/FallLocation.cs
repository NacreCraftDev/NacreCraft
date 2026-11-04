using NacreCraft.Game.World.Entity;
using NacreCraft.Game.World.Level.Block;
using NacreCraft.Registry.State;

namespace NacreCraft.Game.World.Damage;

//FallLocation the climbable that contributed to a fall, maps to vanilla net.minecraft.world.damagesource.FallLocation
//Each value has its own death.fell.accident.<id> translation
public sealed record FallLocation(string Id)
{
    public static readonly FallLocation Generic = new("generic");
    public static readonly FallLocation Ladder = new("ladder");
    public static readonly FallLocation Vines = new("vines");
    public static readonly FallLocation WeepingVines = new("weeping_vines");
    public static readonly FallLocation TwistingVines = new("twisting_vines");
    public static readonly FallLocation Scaffolding = new("scaffolding");
    public static readonly FallLocation OtherClimbable = new("other_climbable");
    public static readonly FallLocation Water = new("water");

    //BlockToFallLocation maps a climbable block to its fall location
    //NC has no ladder/vine blocks and no trapdoor tag yet, so only scaffolding is recognized and anything else is OTHER_CLIMBABLE
    public static FallLocation BlockToFallLocation(BlockState blockState)
        => ReferenceEquals(blockState.Owner, Blocks.SCAFFOLDING) ? Scaffolding : OtherClimbable;

    //GetCurrentFallLocation the fall location of a mob right now
    //NC has no last-climbable tracking yet, so only water is detected and the rest reports nothing
    public static FallLocation? GetCurrentFallLocation(LivingEntity mob)
        => mob.IsInWater ? Water : null;

    //LanguageKey the death message key of this fall location
    public string LanguageKey() => "death.fell.accident." + Id;
}
