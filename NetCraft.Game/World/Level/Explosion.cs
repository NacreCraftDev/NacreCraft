using NetCraft.Game.World.Entity;
using NetCraft.Primitives;
using NetCraft.Registry;
using NetCraft.Storage;

namespace NetCraft.Game.World.Level;

//Explosion one explosion in progress, maps to vanilla net.minecraft.world.level.Explosion
//Both the server driven explosion and the client prediction implement this, so the damage calculator can stay
//ignorant of which side it is running on
public interface Explosion
{
    //Level the level the explosion is happening in
    ServerLevel Level { get; }

    //BlockInteraction what the explosion does to blocks
    BlockInteraction Interaction { get; }

    //IndirectSourceEntity the entity credited for the explosion, such as the owner of the TNT
    LivingEntity? IndirectSourceEntity { get; }

    //DirectSourceEntity the entity that actually went off
    NetCraft.Registry.Entity? DirectSourceEntity { get; }

    //Radius blast radius in blocks
    float Radius { get; }

    //Center where the blast is centred
    Vec3 Center { get; }

    //CanTriggerBlocks whether the blast may trigger blocks like TNT or sculk sensors
    bool CanTriggerBlocks { get; }

    //ShouldAffectBlocklikeEntities whether blocks that behave like entities are caught in the blast
    bool ShouldAffectBlocklikeEntities { get; }
}

//BlockInteraction what an explosion does to the blocks it covers, maps to vanilla Explosion.BlockInteraction
public enum BlockInteraction
{
    Keep,

    Destroy,

    DestroyWithDecay,

    TriggerBlock
}

//BlockInteractionExtensions helpers on the block interaction mode
public static class BlockInteractionExtensions
{
    //ShouldAffectBlocklikeEntities whether this mode catches block-like entities, maps to vanilla shouldAffectBlocklikeEntities
    public static bool ShouldAffectBlocklikeEntities(this BlockInteraction interaction)
        => interaction is BlockInteraction.Destroy or BlockInteraction.DestroyWithDecay;
}

//ExplosionSupport shared helpers, maps to the static methods on the vanilla Explosion interface
public static class ExplosionSupport
{
    //GetIndirectSourceEntity the entity credited for an explosion caused by source
    //maps to vanilla getIndirectSourceEntity, reduced to the living entity case: primed TNT and projectiles resolve
    //their owner the same way once those entity types exist
    public static LivingEntity? GetIndirectSourceEntity(NetCraft.Registry.Entity? source)
        => source as LivingEntity;
}
